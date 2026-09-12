using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class CaptureRuntimeEnrollmentApplicationService(
    ICaptureRuntimeEnrollmentGateway gateway,
    ICaptureRuntimeVerifierPepperSource peppers,
    TimeProvider? timeProvider = null) : ICaptureRuntimeEnrollmentService
{
    public async Task<SessionOperationResult<CaptureRuntimeEnrollmentResponse>> RedeemAsync(
        CaptureRuntimeEnrollmentRedeemRequest request, Guid idempotencyKey,
        ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsUuidV4(idempotencyKey) || !IsUuidV4(request.BootstrapIssuanceId) ||
            !IsUuidV4(request.CandidateKeyId) || exactRequestBody.IsEmpty || exactRequestBody.Length > 16384 ||
            request.SignedAtUtc.Offset != TimeSpan.Zero ||
            request.BootstrapSecret is not { Length: 43 } ||
            !TryDecode(request.PublicVerifierSpki, 91, out var spki) ||
            !TryHex(request.PublicKeyThumbprint, out var thumbprint) ||
            !TryDecode(request.Nonce, 32, out var nonce) ||
            !TryDecode(request.CandidateProof, 64, out var proof))
            return Failure(CaptureRuntimeErrorCodes.RequestInvalid, 400);

        var secret = new byte[32];
        byte[] digest = [], secretCommitment = [], preimage = [], fingerprint = [];
        try
        {
            // The resolver intentionally does not filter lifecycle or expiry.
            // Locked redemption owns replay and durable expiry-on-denied.
            var version = await gateway.ResolveBootstrapVerifierVersionAsync(
                request.BootstrapIssuanceId, cancellationToken).ConfigureAwait(false);
            if (!version.IsSuccess)
                return Failure(version.Error!.Code, version.Error.StatusCode);
            if (version.Value is null) return Failure(CaptureRuntimeErrorCodes.AccessDenied, 403);
            if (version.Value <= 0) return Failure(CaptureRuntimeErrorCodes.NotReady, 503);

            using (var lease = await peppers.TryResolveAsync(version.Value.Value,
                CaptureRuntimeVerifierPepperDomain.BootstrapDigest, cancellationToken).ConfigureAwait(false))
            {
                if (lease is null || lease.Version != version.Value ||
                    lease.Domain != CaptureRuntimeVerifierPepperDomain.BootstrapDigest || lease.Key.Length != 32)
                    return Failure(CaptureRuntimeErrorCodes.NotReady, 503);
                if (!CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(request.BootstrapSecret, secret))
                    return Failure(CaptureRuntimeErrorCodes.RequestInvalid, 400);

                digest = CaptureRuntimeVerifierCryptography.ComputeDigest(lease.Key.Span,
                    CaptureRuntimeVerifierPepperDomain.BootstrapDigest, secret);
                secretCommitment = SHA256.HashData(secret);
                preimage = Encoding.UTF8.GetBytes(string.Join('\n',
                    "TAG-EKYC-ENROLL1", request.BootstrapIssuanceId.ToString("N"),
                    request.CandidateKeyId.ToString("N"), request.PublicVerifierSpki,
                    request.PublicKeyThumbprint,
                    request.SignedAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
                    request.Nonce, idempotencyKey.ToString("N"),
                    Convert.ToHexString(secretCommitment).ToLowerInvariant()) + "\n");
                if (!Verify(spki, thumbprint, preimage, proof))
                    return Failure(CaptureRuntimeErrorCodes.AccessDenied, 403);
            }

            fingerprint = CaptureRuntimeHttpFingerprint.Compute("R05",
                "/api/ekyc/capture-runtime/enrollments/redeem", exactRequestBody,
                idempotencyKey, request.BootstrapIssuanceId.ToByteArray(bigEndian: true), secret);
            // Secret and ceremony bytes never cross into Infrastructure.
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(preimage);
            return await gateway.RedeemBootstrapAsync(new CaptureRuntimeEnrollmentCommand(
                request.BootstrapIssuanceId, idempotencyKey, fingerprint,
                request.CandidateKeyId, spki, thumbprint, request.SignedAtUtc,
                nonce, proof, digest, (timeProvider ?? TimeProvider.System).GetUtcNow()),
                cancellationToken).ConfigureAwait(false);
        }
        catch (CryptographicException)
        {
            return Failure(CaptureRuntimeErrorCodes.AccessDenied, 403);
        }
        finally
        {
            foreach (var buffer in new[] { secret, digest, secretCommitment, preimage, fingerprint, spki, thumbprint, nonce, proof })
                CryptographicOperations.ZeroMemory(buffer);
        }
    }

    private static bool Verify(byte[] spki, byte[] thumbprint, byte[] preimage, byte[] proof)
    {
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(spki), thumbprint)) return false;
        using var verifier = ECDsa.Create();
        verifier.ImportSubjectPublicKeyInfo(spki, out var consumed);
        return consumed == 91 && verifier.KeySize == 256 &&
            verifier.ExportParameters(false).Curve.Oid.Value == "1.2.840.10045.3.1.7" &&
            verifier.VerifyData(preimage, proof, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static bool IsUuidV4(Guid value)
    {
        var text = value.ToString("N");
        return text[12] == '4' && text[16] is '8' or '9' or 'a' or 'b';
    }

    private static bool TryHex(string? text, out byte[] bytes)
    {
        bytes = [];
        if (text is not { Length: 64 } || text.Any(c => c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
            return false;
        bytes = Convert.FromHexString(text);
        return true;
    }

    private static bool TryDecode(string? text, int length, out byte[] bytes)
    {
        bytes = [];
        if (text is null || text.Length != (length * 8 + 5) / 6 ||
            text.Any(c => c is not (>= 'A' and <= 'Z') and not (>= 'a' and <= 'z') and
                not (>= '0' and <= '9') and not '-' and not '_')) return false;
        try
        {
            bytes = Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/') + new string('=', (4 - text.Length % 4) % 4));
            if (bytes.Length == length && Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') == text)
                return true;
        }
        catch (FormatException) { }
        CryptographicOperations.ZeroMemory(bytes);
        bytes = [];
        return false;
    }

    private static SessionOperationResult<CaptureRuntimeEnrollmentResponse> Failure(string code, int status) =>
        SessionOperationResult<CaptureRuntimeEnrollmentResponse>.Failure(code,
            status == 503 ? "Capture Runtime is not ready." : "Enrollment request was denied.", status);
}
