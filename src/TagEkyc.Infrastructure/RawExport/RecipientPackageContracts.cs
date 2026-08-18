using Npgsql;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal enum RecipientPackageDatabaseCapability
{
    Preparer,
    Reconciler,
    Lifecycle,
}

internal interface IRecipientPackageConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(
        RecipientPackageDatabaseCapability capability,
        CancellationToken cancellationToken);
}

internal sealed record RecipientKeyCandidate(
    string Outcome,
    Guid? RecipientClientApplicationId,
    string? RecipientKeyId,
    int? RecipientKeyVersion,
    byte[]? RecipientKeyFingerprint,
    byte[]? RecipientPublicKeySpki,
    long? RecipientKeyRevision,
    DateTimeOffset? RecipientKeyValidFromUtc,
    DateTimeOffset? RecipientKeyValidUntilUtc);

internal sealed record RecipientPackageReserveRequest(
    Guid C2PreparationId,
    Guid PackageId,
    Guid AssemblyId,
    Guid JobId,
    Guid AttemptId,
    long FencingToken,
    byte[] AssemblyFingerprint,
    byte[] ManifestDigest,
    byte[] AssemblyDigest,
    byte[] AssemblyAuthenticationValue,
    long CompleteAssemblyLength,
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    byte[] RecipientKeyFingerprint,
    long RecipientKeyRevision,
    byte[] PackageEqualityFingerprint,
    byte[] ProviderOperationTokenDigest,
    string ProviderKind,
    string ProviderConfigurationId,
    byte[] ProviderEndpointFingerprint,
    string BucketName,
    string ObjectKey,
    byte[] ObjectBindingDigest,
    string PackageProfile);

internal sealed record RecipientPackageReserveResult(
    string Outcome,
    long? RowRevision,
    string? State,
    Guid? PackageId,
    byte[]? PackageEqualityFingerprint,
    string? RecipientKeyId,
    int? RecipientKeyVersion,
    byte[]? RecipientKeyFingerprint,
    byte[]? RecipientPublicKeySpki,
    long? RecipientKeyRevision,
    DateTimeOffset? RecipientKeyValidFromUtc,
    DateTimeOffset? RecipientKeyValidUntilUtc,
    byte[]? ProviderOperationTokenDigest,
    byte[]? ObjectBindingDigest,
    byte[]? ProviderReceiptDigest);

internal sealed record RecipientPackageMutation(
    string Outcome,
    long? RowRevision,
    string? State,
    byte[]? ProviderReceiptDigest = null);

internal sealed record RecipientPackageRecoveryContext(
    string Outcome,
    long RowRevision,
    string State,
    Guid C2PreparationId,
    Guid PackageId,
    byte[] PackageEqualityFingerprint,
    Guid AssemblyId,
    Guid JobId,
    Guid AttemptId,
    long FencingToken,
    byte[] AssemblyFingerprint,
    byte[] ManifestDigest,
    byte[] AssemblyDigest,
    byte[] AssemblyAuthenticationValue,
    long CompleteAssemblyLength,
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    byte[] RecipientKeyFingerprint,
    byte[] RecipientPublicKeySpki,
    long RecipientKeyRevision,
    DateTimeOffset RecipientKeyValidFromUtc,
    DateTimeOffset RecipientKeyValidUntilUtc,
    string PackageProfile,
    byte[] ProviderOperationTokenDigest,
    string ProviderKind,
    string ProviderConfigurationId,
    byte[] ProviderEndpointFingerprint,
    string BucketName,
    string ObjectKey,
    byte[] ObjectBindingDigest,
    byte[]? EnvelopeDigest,
    long? EncryptedPackageLength,
    byte[]? PackageCiphertextDigest,
    byte[]? ConditionalCreateEvidenceDigest,
    byte[]? ProviderReceiptDigest,
    byte[]? AbortAuthorizationDigest,
    byte[]? PositiveAbsenceEvidenceDigest,
    byte[]? CleanupProgressEvidenceDigest,
    byte[]? QuarantineEvidenceDigest);

internal sealed record RecipientPackageLocator(
    string ProviderKind,
    string ProviderConfigurationId,
    byte[] ProviderEndpointFingerprint,
    string BucketName,
    string ObjectKey,
    byte[] ObjectBindingDigest);

internal enum RecipientPackagePutOutcome
{
    Created,
    ConditionalConflict,
    Unavailable,
    OutcomeUnknown,
}

internal sealed record RecipientPackagePutResult(
    RecipientPackagePutOutcome Outcome,
    int? StatusCode,
    string? EntityTag,
    long? ContentLength);

internal enum RecipientPackageInspectionOutcome
{
    Present,
    PositivelyAbsent,
    Unavailable,
    OutcomeUnknown,
}

internal sealed record RecipientPackageInspection(
    RecipientPackageInspectionOutcome Outcome,
    long? ContentLength,
    byte[]? PackageCiphertextDigest,
    byte[]? EnvelopeDigest,
    string? EntityTag);

internal enum RecipientPackageDeleteOutcome
{
    DeletedAcknowledged,
    Unavailable,
    OutcomeUnknown,
}

internal interface IRecipientPackageObjectWriter
{
    Task<RecipientPackagePutResult> PutIfAbsentAsync(
        RecipientPackageLocator locator,
        RecipientPackageEncryptedSpool spool,
        CancellationToken cancellationToken);
}

internal interface IRecipientPackageObjectReader
{
    Task<RecipientPackageInspection> InspectAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken);
}

internal interface IRecipientPackageObjectLifecycle : IRecipientPackageObjectReader
{
    new Task<RecipientPackageInspection> InspectAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken);

    new Task<Stream> OpenReadAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken);

    Task<RecipientPackageDeleteOutcome> DeleteAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken);
}

internal sealed record RecipientPackageBucketPosture(
    bool BucketAvailable,
    bool ObjectLockAbsent,
    bool VersioningNeverEnabled,
    bool LifecycleAbsent,
    bool PublicAccessAbsent);

internal interface IRecipientPackagePostureProbe
{
    Task<RecipientPackageBucketPosture> InspectAsync(CancellationToken cancellationToken);
}

internal sealed record RecipientPackageEncryptionResult(
    RecipientPackageEncryptedSpool Spool,
    byte[] EnvelopeDigest,
    byte[] PackageCiphertextDigest,
    long EncryptedPackageLength);

internal sealed record RecipientPackageHeader(
    Guid AssemblyId,
    byte[] AssemblyDigest,
    byte[] AssemblyFingerprint,
    Guid C2PreparationId,
    long CompleteAssemblyLength,
    byte[] NoncePrefix,
    byte[] PackageEqualityFingerprint,
    Guid PackageId,
    byte[] ProviderOperationTokenDigest,
    Guid RecipientClientApplicationId,
    byte[] RecipientKeyFingerprint,
    string RecipientKeyId,
    int RecipientKeyVersion,
    byte[] WrappedCek);

internal static class RecipientPackageOutcomeMapper
{
    internal static C2AssemblyInspectionOutcome Inspection(string state) => state switch
    {
        "Reserved" or "PutInFlight" or "PutOutcomeUnknown" or "AbortAuthorized" or "CleanupPending"
            => C2AssemblyInspectionOutcome.Preparing,
        "Prepared" => C2AssemblyInspectionOutcome.Prepared,
        "Finalized" => C2AssemblyInspectionOutcome.Finalized,
        "Aborted" => C2AssemblyInspectionOutcome.Aborted,
        "Quarantined" => C2AssemblyInspectionOutcome.Conflict,
        _ => C2AssemblyInspectionOutcome.OutcomeUnknown,
    };
}
