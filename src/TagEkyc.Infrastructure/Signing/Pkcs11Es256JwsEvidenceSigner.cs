using System.Security.Cryptography;
using System.Text;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Signing;

public sealed class Pkcs11Es256JwsEvidenceSigner : IEvidenceSigner, IEs256PublicJwkSource, IEvidenceSignerStartupValidator
{
    private static readonly byte[] EcPublicKeyParams = Convert.FromHexString("06082A8648CE3D030107");

    private readonly Pkcs11Es256JwsEvidenceSignerOptions options;
    private readonly Pkcs11InteropFactories factories = new();

    public Pkcs11Es256JwsEvidenceSigner(Pkcs11Es256JwsEvidenceSignerOptions options)
    {
        this.options = ValidateOptions(options);
    }

    public Task<EvidenceSignatureEnvelope> SignAsync(
        EvidenceSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var signedAt = Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow);
        var payloadJson = Es256JwsEvidenceSignatureBuilder.BuildSignedClaimJson(request, signedAt);
        var jws = SignPayload(payloadJson, cancellationToken);

        return Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            options.Kid!,
            signedAt,
            jws.SignatureValue));
    }

    public Task<EvidenceSignatureEnvelope> SignProofAsync(
        EvidenceProofSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var signedAt = Es256JwsEvidenceSignatureBuilder.TruncateToMicroseconds(DateTimeOffset.UtcNow);
        var payloadJson = Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(request, signedAt);
        var jws = SignPayload(payloadJson, cancellationToken);

        return Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            options.Kid!,
            signedAt,
            jws.SignatureValue,
            jws.PublicKeyJwk,
            Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(jws.PublicKeyJwk)));
    }

    public void ValidateToken(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = OpenSigningContext();

        var testInput = Encoding.ASCII.GetBytes("tagekyc-pkcs11-es256-startup-self-test-v1");
        var digest = SHA256.HashData(testInput);
        var signature = SignDigest(context.Session, context.PrivateKey, digest);
        using var publicKey = ImportPublicKey(context.PublicKeyJwk);

        if (!publicKey.VerifyHash(digest, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            throw new InvalidOperationException("PKCS#11 public key does not match the configured private signing key.");
        }
    }

    public void ValidateStartup(CancellationToken cancellationToken = default) => ValidateToken(cancellationToken);

    public string KeyId => options.Kid!;

    public string PublicKeyJwk
    {
        get
        {
            using var context = OpenSigningContext();
            return context.PublicKeyJwk;
        }
    }

    public string PublicKeyFingerprint => Es256JwsEvidenceSignatureBuilder.ComputePublicKeyFingerprint(PublicKeyJwk);

    private SignedPayload SignPayload(string payloadJson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = OpenSigningContext();

        var headerJson = Es256JwsEvidenceSignatureBuilder.BuildProtectedHeaderJson(options.Kid!);
        var signingInput = Es256JwsEvidenceSignatureBuilder.BuildSigningInput(headerJson, payloadJson);
        var digest = SHA256.HashData(Encoding.ASCII.GetBytes(signingInput));
        var signature = SignDigest(context.Session, context.PrivateKey, digest);

        using var publicKey = ImportPublicKey(context.PublicKeyJwk);
        if (!publicKey.VerifyHash(digest, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
        {
            throw new InvalidOperationException("PKCS#11 signature self-test failed for the configured key pair.");
        }

        return new SignedPayload(
            Es256JwsEvidenceSignatureBuilder.BuildCompactJws(signingInput, signature),
            context.PublicKeyJwk);
    }

    private SigningContext OpenSigningContext()
    {
        var library = factories.Pkcs11LibraryFactory.LoadPkcs11Library(
            factories,
            options.LibraryPath!,
            AppType.MultiThreaded);
        var slot = FindSlot(library);
        var session = slot.OpenSession(SessionType.ReadOnly);
        var loggedIn = false;

        try
        {
            session.Login(CKU.CKU_USER, options.Pin);
            loggedIn = true;
            var privateKey = FindOneObject(session, BuildPrivateKeyTemplate(), "private signing key");
            var publicKey = FindOneObject(session, BuildPublicKeyTemplate(), "public verification key");
            var publicKeyJwk = ExportPublicKeyJwk(session, publicKey);

            return new SigningContext(library, session, privateKey, publicKeyJwk, loggedIn);
        }
        catch
        {
            if (loggedIn)
            {
                SafeLogout(session);
            }

            session.Dispose();
            library.Dispose();
            throw;
        }
    }

    private ISlot FindSlot(IPkcs11Library library)
    {
        var slots = library.GetSlotList(SlotsType.WithTokenPresent);
        if (slots.Count == 0)
        {
            throw new InvalidOperationException("No PKCS#11 token is present.");
        }

        if (string.IsNullOrWhiteSpace(options.TokenLabel))
        {
            return slots[0];
        }

        var slot = slots.FirstOrDefault(candidate =>
            string.Equals(candidate.GetTokenInfo().Label.Trim(), options.TokenLabel, StringComparison.Ordinal));

        return slot ?? throw new InvalidOperationException("Configured PKCS#11 token label was not found.");
    }

    private List<IObjectAttribute> BuildPrivateKeyTemplate() =>
    [
        factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
        factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_EC),
        factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, options.KeyLabel!),
        factories.ObjectAttributeFactory.Create(CKA.CKA_ID, ParseKeyObjectId(options.KeyObjectId!)),
    ];

    private List<IObjectAttribute> BuildPublicKeyTemplate() =>
    [
        factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PUBLIC_KEY),
        factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_EC),
        factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, options.KeyLabel!),
        factories.ObjectAttributeFactory.Create(CKA.CKA_ID, ParseKeyObjectId(options.KeyObjectId!)),
    ];

    private static IObjectHandle FindOneObject(ISession session, List<IObjectAttribute> template, string objectDescription)
    {
        var objects = session.FindAllObjects(template);
        return objects.Count switch
        {
            1 => objects[0],
            0 => throw new InvalidOperationException($"Configured PKCS#11 {objectDescription} was not found."),
            _ => throw new InvalidOperationException($"Configured PKCS#11 {objectDescription} is ambiguous."),
        };
    }

    private byte[] SignDigest(ISession session, IObjectHandle privateKey, byte[] digest)
    {
        if (digest.Length != SHA256.HashSizeInBytes)
        {
            throw new InvalidOperationException("PKCS#11 ES256 signing requires a SHA-256 digest.");
        }

        var mechanism = factories.MechanismFactory.Create(CKM.CKM_ECDSA);
        var signature = session.Sign(mechanism, privateKey, digest);
        return NormalizeP1363Signature(signature);
    }

    private string ExportPublicKeyJwk(ISession session, IObjectHandle publicKey)
    {
        var attributes = session.GetAttributeValue(publicKey, [CKA.CKA_EC_PARAMS, CKA.CKA_EC_POINT]);
        var ecParams = attributes.Single(attribute => attribute.Type == (ulong)CKA.CKA_EC_PARAMS).GetValueAsByteArray();
        if (!ecParams.SequenceEqual(EcPublicKeyParams))
        {
            throw new InvalidOperationException("Configured PKCS#11 public key must be P-256.");
        }

        var ecPoint = attributes.Single(attribute => attribute.Type == (ulong)CKA.CKA_EC_POINT).GetValueAsByteArray();
        var xy = ParseUncompressedEcPoint(ecPoint);
        return Es256JwsEvidenceSignatureBuilder.BuildPublicJwk(xy.X, xy.Y);
    }

    private static ECDsa ImportPublicKey(string publicKeyJwk)
    {
        using var document = System.Text.Json.JsonDocument.Parse(publicKeyJwk);
        var root = document.RootElement;
        var key = ECDsa.Create();
        key.ImportParameters(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64UrlDecode(root.GetProperty("x").GetString()!),
                Y = Base64UrlDecode(root.GetProperty("y").GetString()!),
            },
        });
        return key;
    }

    private static byte[] NormalizeP1363Signature(byte[] signature)
    {
        if (signature.Length == 64)
        {
            return signature;
        }

        var der = DecodeDerEcdsaSignature(signature);
        if (der.Length == 64)
        {
            return der;
        }

        throw new InvalidOperationException("PKCS#11 ECDSA signature must be a 64-byte P-256 r||s value.");
    }

    private static byte[] DecodeDerEcdsaSignature(byte[] signature)
    {
        if (signature.Length < 8 || signature[0] != 0x30)
        {
            throw new InvalidOperationException("PKCS#11 ECDSA signature was neither P1363 nor DER.");
        }

        var offset = 1;
        _ = ReadDerLength(signature, ref offset);
        var r = ReadDerInteger(signature, ref offset);
        var s = ReadDerInteger(signature, ref offset);
        return [.. LeftPadTo32(r), .. LeftPadTo32(s)];
    }

    private static byte[] ReadDerInteger(byte[] der, ref int offset)
    {
        if (offset >= der.Length || der[offset++] != 0x02)
        {
            throw new InvalidOperationException("Invalid DER ECDSA integer.");
        }

        var length = ReadDerLength(der, ref offset);
        if (offset + length > der.Length)
        {
            throw new InvalidOperationException("Invalid DER ECDSA integer length.");
        }

        var value = der[offset..(offset + length)];
        offset += length;
        return TrimLeadingZero(value);
    }

    private static (byte[] X, byte[] Y) ParseUncompressedEcPoint(byte[] ecPoint)
    {
        var point = TryReadDerOctetString(ecPoint, out var derPoint) ? derPoint : ecPoint;
        if (point.Length != 65 || point[0] != 0x04)
        {
            throw new InvalidOperationException("PKCS#11 public EC point must be uncompressed P-256.");
        }

        return (point[1..33], point[33..65]);
    }

    private static bool TryReadDerOctetString(byte[] value, out byte[] octets)
    {
        octets = [];
        if (value.Length < 2 || value[0] != 0x04)
        {
            return false;
        }

        var offset = 1;
        int length;
        try
        {
            length = ReadDerLength(value, ref offset);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (offset + length != value.Length)
        {
            return false;
        }

        octets = value[offset..];
        return true;
    }

    private static int ReadDerLength(byte[] der, ref int offset)
    {
        if (offset >= der.Length)
        {
            throw new InvalidOperationException("Invalid DER length.");
        }

        var first = der[offset++];
        if ((first & 0x80) == 0)
        {
            return first;
        }

        var byteCount = first & 0x7F;
        if (byteCount is 0 or > 2 || offset + byteCount > der.Length)
        {
            throw new InvalidOperationException("Unsupported DER length.");
        }

        var length = 0;
        for (var i = 0; i < byteCount; i++)
        {
            length = (length << 8) | der[offset++];
        }

        return length;
    }

    private static byte[] ParseKeyObjectId(string value)
    {
        var normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : value.Replace(":", string.Empty, StringComparison.Ordinal);

        return normalized.Length % 2 == 0 && normalized.All(Uri.IsHexDigit)
            ? Convert.FromHexString(normalized)
            : Encoding.UTF8.GetBytes(value);
    }

    private static byte[] LeftPadTo32(byte[] value)
    {
        var trimmed = TrimLeadingZero(value);
        if (trimmed.Length > 32)
        {
            throw new InvalidOperationException("ECDSA integer is longer than P-256 size.");
        }

        var output = new byte[32];
        Buffer.BlockCopy(trimmed, 0, output, output.Length - trimmed.Length, trimmed.Length);
        return output;
    }

    private static byte[] TrimLeadingZero(byte[] value)
    {
        var offset = 0;
        while (offset < value.Length - 1 && value[offset] == 0)
        {
            offset++;
        }

        return value[offset..];
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private static Pkcs11Es256JwsEvidenceSignerOptions ValidateOptions(Pkcs11Es256JwsEvidenceSignerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LibraryPath))
        {
            throw new InvalidOperationException("PKCS#11 signing requires a library path.");
        }

        if (string.IsNullOrWhiteSpace(options.Pin))
        {
            throw new InvalidOperationException("PKCS#11 signing requires a configured PIN.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyLabel))
        {
            throw new InvalidOperationException("PKCS#11 signing requires a key label.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyObjectId))
        {
            throw new InvalidOperationException("PKCS#11 signing requires a key object id.");
        }

        if (string.IsNullOrWhiteSpace(options.Kid))
        {
            throw new InvalidOperationException("PKCS#11 signing requires a JWS kid.");
        }

        if (!options.Kid.StartsWith("tagekyc-es256-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PKCS#11 signing kid must use the tagekyc-es256-<year>-v<n> scheme.");
        }

        return options;
    }

    private static void SafeLogout(ISession session)
    {
        try
        {
            session.Logout();
        }
        catch (Pkcs11Exception exception) when (exception.RV is CKR.CKR_USER_NOT_LOGGED_IN or CKR.CKR_SESSION_HANDLE_INVALID)
        {
        }
    }

    private sealed class SigningContext : IDisposable
    {
        private readonly IPkcs11Library library;
        private readonly ISession session;
        private readonly bool loggedIn;

        public SigningContext(
            IPkcs11Library library,
            ISession session,
            IObjectHandle privateKey,
            string publicKeyJwk,
            bool loggedIn)
        {
            this.library = library;
            this.session = session;
            this.loggedIn = loggedIn;
            PrivateKey = privateKey;
            PublicKeyJwk = publicKeyJwk;
        }

        public ISession Session => session;

        public IObjectHandle PrivateKey { get; }

        public string PublicKeyJwk { get; }

        public void Dispose()
        {
            if (loggedIn)
            {
                SafeLogout(Session);
            }

            session.Dispose();
            library.Dispose();
        }
    }

    private sealed record SignedPayload(string SignatureValue, string PublicKeyJwk);
}
