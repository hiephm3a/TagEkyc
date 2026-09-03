using System.Globalization;

namespace TagEkyc.Infrastructure.RawExport;

internal static class RawExportR3StagedCiphertextFingerprintCodec
{
    internal const int SchemaVersion = 2;
    internal const string Domain = "tip-88c1-staged-ciphertext-v2";

    internal static byte[] Compute(RawExportR3FingerprintInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        RequireDigest(input.EncryptionAttemptFingerprint, nameof(input.EncryptionAttemptFingerprint));
        RequireDigest(input.ObjectBindingDigest, nameof(input.ObjectBindingDigest));
        RequirePositive(input.VerifiedPlaintextLength, nameof(input.VerifiedPlaintextLength));
        RequireDigest(input.ContentCommitment, nameof(input.ContentCommitment));
        RequirePositive(input.StagedCiphertextLength, nameof(input.StagedCiphertextLength));
        RequireDigest(input.StagedCiphertextDigest, nameof(input.StagedCiphertextDigest));
        RequireDigest(input.StagedProviderReceiptDigest, nameof(input.StagedProviderReceiptDigest));
        RequireDigest(input.StagedVerificationEvidenceDigest, nameof(input.StagedVerificationEvidenceDigest));
        if (input.ObjectCustodyId == Guid.Empty)
            throw new ArgumentException("Object custody id is required.", nameof(input));

        return C1HashCanonical.Compute(
            Domain,
            new C1HashCanonical.Scalar(Hex(input.EncryptionAttemptFingerprint)),
            new C1HashCanonical.Scalar(input.ObjectCustodyId.ToString("N")),
            new C1HashCanonical.Scalar(Hex(input.ObjectBindingDigest)),
            new C1HashCanonical.Scalar(Decimal(input.VerifiedPlaintextLength)),
            new C1HashCanonical.Scalar(Hex(input.ContentCommitment)),
            new C1HashCanonical.Scalar(Decimal(input.StagedCiphertextLength)),
            new C1HashCanonical.Scalar(Hex(input.StagedCiphertextDigest)),
            new C1HashCanonical.Scalar(Hex(input.StagedProviderReceiptDigest)),
            new C1HashCanonical.Scalar(Hex(input.StagedVerificationEvidenceDigest)));
    }

    private static string Hex(byte[] value) => Convert.ToHexString(value).ToLowerInvariant();

    private static string Decimal(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static void RequireDigest(byte[] value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Length != 32)
            throw new ArgumentException("Digest must be exactly 32 bytes.", parameterName);
    }

    private static void RequirePositive(long value, string parameterName)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
