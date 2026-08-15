using System.Globalization;

namespace TagEkyc.Infrastructure.RawExport;

internal static class RawExportSourceFinalizationEvidenceCodec
{
    internal static byte[] ComputeCommit(SourceCommitEvidenceInput value) => C1HashCanonical.Compute(
        "tip-88c1-source-commit-v1",
        S(value.SourceArtifactId), S(value.AttemptId), S(value.ObjectCustodyId), S(value.AttemptKeyReservationId),
        D(value.StagedCiphertextFingerprintSchemaVersion), H(value.StagedCiphertextFingerprint),
        D(value.AuthoritySnapshotSchemaVersion), S(value.AuthoritySnapshotId), D(value.AuthorityRevision),
        S(value.ConsentPolicyId), D(value.ConsentPolicyVersion), T(value.CommittedAtUtc));

    internal static byte[] ComputeAvailable(SourceAvailableEvidenceInput value) => C1HashCanonical.Compute(
        "tip-88c1-source-available-v1",
        S(value.SourcePublicationId), S(value.SourceArtifactId), S(value.AttemptId), S(value.ObjectCustodyId),
        S(value.AttemptKeyReservationId), S(value.OpaqueCommittedLocatorId), H(value.CommitEvidenceDigest),
        D(value.AuthoritySnapshotSchemaVersion), S(value.AuthoritySnapshotId), D(value.AuthorityRevision),
        S(value.ConsentPolicyId), D(value.ConsentPolicyVersion), T(value.AvailableAtUtc));

    internal static byte[] ComputeCleanup(SourceCleanupEvidenceInput value) => C1HashCanonical.Compute(
        "tip-88c1-source-finalization-cleanup-v1",
        S(value.SourcePublicationId), S(value.SourceArtifactId), Text(value.CleanupDisposition),
        D(value.CompletedCleanupItemCount), T(value.FinalizedAtUtc));

    internal static byte[] ComputeObjectItem(SourceCleanupItemEvidenceInput value) => ComputeItem(
        "tip-88c1-source-object-cleanup-item-v1", value);

    internal static byte[] ComputeKeyItem(SourceCleanupItemEvidenceInput value) => ComputeItem(
        "tip-88c1-source-key-cleanup-item-v1", value);

    private static byte[] ComputeItem(string domain, SourceCleanupItemEvidenceInput value) => C1HashCanonical.Compute(
        domain, S(value.SourcePublicationId), S(value.ResourceAttemptId), S(value.ResourceId),
        Text(value.CompletionDisposition), H(value.UnderlyingTerminalEvidenceDigest), T(value.CompletedAtUtc));

    private static C1HashCanonical.Scalar S(Guid value) => new(value.ToString("N"));
    private static C1HashCanonical.Scalar H(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 32) throw new ArgumentException("RAW_EXPORT_FINALIZATION_DIGEST_INVALID");
        return new(Convert.ToHexString(value).ToLowerInvariant());
    }
    private static C1HashCanonical.Scalar D(long value) => new(value.ToString(CultureInfo.InvariantCulture));
    private static C1HashCanonical.Scalar T(DateTimeOffset value) => new(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture));
    private static C1HashCanonical.Scalar Text(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }
}
