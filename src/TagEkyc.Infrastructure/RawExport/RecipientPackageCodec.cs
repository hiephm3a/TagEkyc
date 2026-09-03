using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientPackageCodec
{
    public const string MagicText = "TIP-88C1-C2-PACKAGE-V1";
    public const string ContentEncryptionAlgorithm = "A256GCM-FRAME-V1";
    public const string KeyWrapAlgorithm = "RSA-OAEP-256";
    public const string SignatureAlgorithm = "none";
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes(MagicText);

    public static byte[] PackageEqualityFingerprint(
        Guid c2PreparationId,
        Guid assemblyId,
        ReadOnlySpan<byte> assemblyFingerprint,
        ReadOnlySpan<byte> manifestDigest,
        ReadOnlySpan<byte> assemblyDigest,
        ReadOnlySpan<byte> assemblyAuthenticationValue,
        Guid recipientClientApplicationId,
        string recipientKeyId,
        int recipientKeyVersion,
        ReadOnlySpan<byte> recipientKeyFingerprint,
        long completeAssemblyLength)
    {
        RequireDigest(assemblyFingerprint, nameof(assemblyFingerprint));
        RequireDigest(manifestDigest, nameof(manifestDigest));
        RequireDigest(assemblyDigest, nameof(assemblyDigest));
        RequireDigest(assemblyAuthenticationValue, nameof(assemblyAuthenticationValue));
        RequireDigest(recipientKeyFingerprint, nameof(recipientKeyFingerprint));
        if (recipientKeyVersion < 1) throw new ArgumentOutOfRangeException(nameof(recipientKeyVersion));
        if (completeAssemblyLength is < 1 or > RecipientPackageOptions.MaximumCompleteAssemblyLength)
            throw new ArgumentOutOfRangeException(nameof(completeAssemblyLength));
        return Canonical(
            "tip-88c1-c2-package-equality-v1",
            G(c2PreparationId), G(assemblyId), H(assemblyFingerprint), H(manifestDigest), H(assemblyDigest),
            H(assemblyAuthenticationValue), G(recipientClientApplicationId), RequiredKeyId(recipientKeyId),
            recipientKeyVersion.ToString(CultureInfo.InvariantCulture), H(recipientKeyFingerprint),
            RecipientPackageOptions.PackageProfile, completeAssemblyLength.ToString(CultureInfo.InvariantCulture));
    }

    public static Guid PackageId(Guid c2PreparationId, ReadOnlySpan<byte> packageEqualityFingerprint)
    {
        RequireDigest(packageEqualityFingerprint, nameof(packageEqualityFingerprint));
        return EvidenceCanonicalization.DeterministicGuid(
            "tip-88c1-c2-package-id-v1",
            new
            {
                c2PreparationId = G(c2PreparationId),
                packageEqualityFingerprint = H(packageEqualityFingerprint),
            });
    }

    public static string ObjectKey(Guid packageId) =>
        $"raw-export/c2-package/v1/{packageId:N}";

    public static byte[] ProviderEndpointFingerprint(RecipientPackageProviderConfiguration provider) =>
        Canonical(
            "tip-88c1-c2-provider-endpoint-v1",
            RecipientPackageProviderConfiguration.ProviderKind,
            NormalizedServiceUri(provider.ServiceUrl),
            provider.ForcePathStyle ? "true" : "false",
            provider.RegionIdentifier);

    public static byte[] ObjectBindingDigest(
        string providerConfigurationId,
        ReadOnlySpan<byte> providerEndpointFingerprint,
        string bucketName,
        string objectKey)
    {
        RequireDigest(providerEndpointFingerprint, nameof(providerEndpointFingerprint));
        RequireText(providerConfigurationId, nameof(providerConfigurationId));
        RequireText(bucketName, nameof(bucketName));
        if (!objectKey.StartsWith("raw-export/c2-package/v1/", StringComparison.Ordinal)
            || objectKey.Length != "raw-export/c2-package/v1/".Length + 32)
            throw new ArgumentException("RAW_EXPORT_RECIPIENT_PACKAGE_OBJECT_KEY_INVALID", nameof(objectKey));
        return Canonical(
            "tip-88c1-c2-object-binding-v1",
            RecipientPackageProviderConfiguration.ProviderKind,
            providerConfigurationId,
            H(providerEndpointFingerprint),
            bucketName,
            objectKey);
    }

    public static byte[] ProviderOperationTokenDigest(ReadOnlySpan<byte> token)
    {
        if (token.Length != 32)
            throw new ArgumentException("Provider operation token must be exactly 32 bytes.", nameof(token));
        return SHA256.HashData(token);
    }

    internal static byte[] SerializeHeader(RecipientPackageHeader header)
    {
        ValidateHeader(header);
        var value = new
        {
            assemblyDigest = H(header.AssemblyDigest),
            assemblyFingerprint = H(header.AssemblyFingerprint),
            assemblyId = D(header.AssemblyId),
            c2PreparationId = D(header.C2PreparationId),
            completeAssemblyLength = header.CompleteAssemblyLength,
            contentEncryptionAlgorithm = ContentEncryptionAlgorithm,
            extensionAlgorithms = Array.Empty<string>(),
            framePlaintextBytes = RecipientPackageOptions.FramePlaintextBytes,
            keyWrapAlgorithm = KeyWrapAlgorithm,
            noncePrefix = B64Url(header.NoncePrefix),
            packageEqualityFingerprint = H(header.PackageEqualityFingerprint),
            packageId = D(header.PackageId),
            packageProfile = RecipientPackageOptions.PackageProfile,
            providerOperationTokenDigest = H(header.ProviderOperationTokenDigest),
            recipientClientApplicationId = D(header.RecipientClientApplicationId),
            recipientKeyFingerprint = H(header.RecipientKeyFingerprint),
            recipientKeyId = header.RecipientKeyId.Normalize(NormalizationForm.FormC),
            recipientKeyVersion = header.RecipientKeyVersion,
            signatureAlgorithm = SignatureAlgorithm,
            wrappedCek = B64Url(header.WrappedCek),
        };
        var bytes = Encoding.UTF8.GetBytes(EvidenceCanonicalization.Canonicalize(value));
        if (bytes.Length > RecipientPackageOptions.MaximumHeaderLength)
        {
            CryptographicOperations.ZeroMemory(bytes);
            throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_HEADER_LIMIT_EXCEEDED");
        }
        return bytes;
    }

    internal static byte[] Envelope(RecipientPackageHeader header, out byte[] envelopeDigest)
    {
        var headerBytes = SerializeHeader(header);
        try
        {
            var envelope = new byte[checked(Magic.Length + 4 + headerBytes.Length)];
            Magic.CopyTo(envelope, 0);
            BinaryPrimitives.WriteUInt32BigEndian(envelope.AsSpan(Magic.Length, 4), checked((uint)headerBytes.Length));
            headerBytes.CopyTo(envelope, Magic.Length + 4);
            envelopeDigest = SHA256.HashData(envelope);
            return envelope;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(headerBytes);
        }
    }

    internal static byte[] FrameAad(
        ReadOnlySpan<byte> envelopeDigest,
        int ordinal,
        int plaintextLength) =>
        Payload(
            "tip-88c1-c2-frame-aad-v1",
            H(envelopeDigest),
            ordinal.ToString(CultureInfo.InvariantCulture),
            plaintextLength.ToString(CultureInfo.InvariantCulture),
            "data");

    internal static byte[] CompletionAad(
        ReadOnlySpan<byte> envelopeDigest,
        int frameCount,
        long completeAssemblyLength) =>
        Payload(
            "tip-88c1-c2-completion-aad-v1",
            H(envelopeDigest),
            frameCount.ToString(CultureInfo.InvariantCulture),
            completeAssemblyLength.ToString(CultureInfo.InvariantCulture));

    public static byte[] ConditionalCreateEvidenceDigest(
        ReadOnlySpan<byte> objectBindingDigest,
        long encryptedPackageLength,
        ReadOnlySpan<byte> packageCiphertextDigest,
        ReadOnlySpan<byte> providerEntityTagDigest) =>
        Canonical(
            "tip-88c1-c2-conditional-create-evidence-v1",
            H32(objectBindingDigest, nameof(objectBindingDigest)),
            encryptedPackageLength.ToString(CultureInfo.InvariantCulture),
            H32(packageCiphertextDigest, nameof(packageCiphertextDigest)),
            H32(providerEntityTagDigest, nameof(providerEntityTagDigest)),
            "Unversioned",
            "PresentExact");

    public static byte[] ProviderReceiptDigest(
        C2AssemblyPreparationRequest request,
        Guid packageId,
        ReadOnlySpan<byte> packageEqualityFingerprint,
        string recipientKeyId,
        int recipientKeyVersion,
        ReadOnlySpan<byte> recipientKeyFingerprint,
        ReadOnlySpan<byte> objectBindingDigest,
        long encryptedPackageLength,
        ReadOnlySpan<byte> packageCiphertextDigest,
        ReadOnlySpan<byte> envelopeDigest,
        ReadOnlySpan<byte> providerOperationTokenDigest,
        ReadOnlySpan<byte> conditionalCreateEvidenceDigest) =>
        Canonical(
            "tip-88c1-c2-provider-receipt-v1",
            G(request.C2PreparationId), G(packageId), H32(packageEqualityFingerprint, nameof(packageEqualityFingerprint)),
            G(request.AssemblyId), H32(request.AssemblyFingerprint, nameof(request.AssemblyFingerprint)),
            H32(request.ManifestDigest, nameof(request.ManifestDigest)), H32(request.AssemblyDigest, nameof(request.AssemblyDigest)),
            H32(request.AssemblyAuthenticationValue, nameof(request.AssemblyAuthenticationValue)),
            G(request.RecipientClientApplicationId), RequiredKeyId(recipientKeyId),
            recipientKeyVersion.ToString(CultureInfo.InvariantCulture), H32(recipientKeyFingerprint, nameof(recipientKeyFingerprint)),
            RecipientPackageOptions.PackageProfile, H32(objectBindingDigest, nameof(objectBindingDigest)),
            encryptedPackageLength.ToString(CultureInfo.InvariantCulture), H32(packageCiphertextDigest, nameof(packageCiphertextDigest)),
            H32(envelopeDigest, nameof(envelopeDigest)), H32(providerOperationTokenDigest, nameof(providerOperationTokenDigest)),
            H32(conditionalCreateEvidenceDigest, nameof(conditionalCreateEvidenceDigest)));

    public static byte[] ProviderObservationEvidenceDigest(
        Guid packageId,
        ReadOnlySpan<byte> objectBindingDigest,
        ReadOnlySpan<byte> providerOperationTokenDigest,
        string observationKind,
        long? observedLength,
        byte[]? observedPackageDigest,
        byte[]? observedEnvelopeDigest) =>
        Canonical(
            "tip-88c1-c2-provider-observation-v1",
            G(packageId), H32(objectBindingDigest, nameof(objectBindingDigest)),
            H32(providerOperationTokenDigest, nameof(providerOperationTokenDigest)),
            RequireText(observationKind, nameof(observationKind)),
            observedLength.HasValue ? "1" : "0", observedLength?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            observedPackageDigest is null ? "0" : "1", observedPackageDigest is null ? string.Empty : H32(observedPackageDigest, nameof(observedPackageDigest)),
            observedEnvelopeDigest is null ? "0" : "1", observedEnvelopeDigest is null ? string.Empty : H32(observedEnvelopeDigest, nameof(observedEnvelopeDigest)));

    public static byte[] PositiveAbsenceEvidenceDigest(
        Guid packageId,
        ReadOnlySpan<byte> objectBindingDigest,
        ReadOnlySpan<byte> providerOperationTokenDigest,
        DateTimeOffset firstObservedAtUtc,
        DateTimeOffset secondObservedAtUtc) =>
        Canonical(
            "tip-88c1-c2-positive-absence-v1",
            G(packageId), H32(objectBindingDigest, nameof(objectBindingDigest)),
            H32(providerOperationTokenDigest, nameof(providerOperationTokenDigest)),
            C1HashCanonical.CanonicalTimestamp(firstObservedAtUtc),
            C1HashCanonical.CanonicalTimestamp(secondObservedAtUtc),
            "PositivelyAbsent");

    public static byte[] CleanupProgressEvidenceDigest(
        Guid packageId,
        ReadOnlySpan<byte> objectBindingDigest,
        ReadOnlySpan<byte> providerOperationTokenDigest,
        string resultKind,
        ReadOnlySpan<byte> observationDigest) =>
        Canonical(
            "tip-88c1-c2-cleanup-progress-v1", G(packageId),
            H32(objectBindingDigest, nameof(objectBindingDigest)),
            H32(providerOperationTokenDigest, nameof(providerOperationTokenDigest)),
            RequireText(resultKind, nameof(resultKind)), H32(observationDigest, nameof(observationDigest)));

    public static byte[] QuarantineEvidenceDigest(
        Guid packageId,
        ReadOnlySpan<byte> packageEqualityFingerprint,
        ReadOnlySpan<byte> objectBindingDigest,
        ReadOnlySpan<byte> providerOperationTokenDigest,
        string reason,
        ReadOnlySpan<byte> observationDigest) =>
        Canonical(
            "tip-88c1-c2-quarantine-v1", G(packageId),
            H32(packageEqualityFingerprint, nameof(packageEqualityFingerprint)),
            H32(objectBindingDigest, nameof(objectBindingDigest)),
            H32(providerOperationTokenDigest, nameof(providerOperationTokenDigest)),
            RequireText(reason, nameof(reason)), H32(observationDigest, nameof(observationDigest)));

    internal static bool TryReadEnvelopeDigest(Stream package, out byte[] envelopeDigest)
    {
        envelopeDigest = [];
        Span<byte> prefix = stackalloc byte[Magic.Length + 4];
        if (!ReadExactly(package, prefix) || !prefix[..Magic.Length].SequenceEqual(Magic)) return false;
        var length = BinaryPrimitives.ReadUInt32BigEndian(prefix[Magic.Length..]);
        if (length > RecipientPackageOptions.MaximumHeaderLength) return false;
        var header = new byte[checked((int)length)];
        try
        {
            if (!ReadExactly(package, header)) return false;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(prefix);
            hash.AppendData(header);
            envelopeDigest = hash.GetHashAndReset();
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(header);
        }
    }

    internal static string NormalizedServiceUri(Uri value)
    {
        var normalized = RecipientPackageOptions.Normalize(value);
        return $"{normalized.Scheme.ToLowerInvariant()}://{normalized.IdnHost.ToLowerInvariant()}:{normalized.Port}/";
    }

    private static byte[] Canonical(string domain, params string[] fields) =>
        C1HashCanonical.Compute(domain, fields.Select(field => (C1HashCanonical.Component)new C1HashCanonical.Scalar(field)).ToArray());

    private static byte[] Payload(string domain, params string[] fields) =>
        C1HashCanonical.EncodeLengthPrefixedPayload(domain, fields);

    private static string H32(ReadOnlySpan<byte> value, string name)
    {
        RequireDigest(value, name);
        return H(value);
    }

    private static string H(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();
    private static string G(Guid value) => value != Guid.Empty ? value.ToString("N") : throw new ArgumentException("GUID cannot be empty.");
    private static string D(Guid value) => value != Guid.Empty ? value.ToString("D").ToLowerInvariant() : throw new ArgumentException("GUID cannot be empty.");
    private static string B64Url(ReadOnlySpan<byte> value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string RequiredKeyId(string value)
    {
        if (value is not { Length: >= 1 and <= 128 }
            || value.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-')))
            throw new ArgumentException("RAW_EXPORT_RECIPIENT_KEY_ID_INVALID", nameof(value));
        return value;
    }

    private static string RequireText(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value.Normalize(NormalizationForm.FormC);
    }

    private static void RequireDigest(ReadOnlySpan<byte> value, string name)
    {
        if (value.Length != 32) throw new ArgumentException($"{name} must be exactly 32 bytes.", name);
    }

    private static void ValidateHeader(RecipientPackageHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        RequireDigest(header.AssemblyDigest, nameof(header.AssemblyDigest));
        RequireDigest(header.AssemblyFingerprint, nameof(header.AssemblyFingerprint));
        RequireDigest(header.PackageEqualityFingerprint, nameof(header.PackageEqualityFingerprint));
        RequireDigest(header.ProviderOperationTokenDigest, nameof(header.ProviderOperationTokenDigest));
        RequireDigest(header.RecipientKeyFingerprint, nameof(header.RecipientKeyFingerprint));
        if (header.NoncePrefix.Length != 8) throw new ArgumentException("Nonce prefix must be 8 bytes.");
        if (header.WrappedCek.Length is < 384 or > 512) throw new ArgumentException("Wrapped CEK length invalid.");
        if (header.RecipientKeyVersion < 1) throw new ArgumentOutOfRangeException(nameof(header.RecipientKeyVersion));
        RequiredKeyId(header.RecipientKeyId);
    }

    private static bool ReadExactly(Stream stream, Span<byte> destination)
    {
        var offset = 0;
        while (offset < destination.Length)
        {
            var read = stream.Read(destination[offset..]);
            if (read == 0) return false;
            offset += read;
        }
        return true;
    }
}
