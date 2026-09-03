namespace TagEkyc.Infrastructure.RawExport;

internal abstract record KekProvisioningResolution
{
    internal sealed record WrappedResultRecovered(KekWrappedMaterial Material) : KekProvisioningResolution;
    internal sealed record NoProviderResult(string AbsenceProofReceipt) : KekProvisioningResolution;
    internal sealed record ProviderOutcomeUnknown : KekProvisioningResolution;
    internal sealed record ProviderResourceCleanupRequired(string CleanupReference) : KekProvisioningResolution;
    internal sealed record ProviderUnavailable : KekProvisioningResolution;
    internal sealed record CorruptOrUnverifiable : KekProvisioningResolution;
}

internal abstract record KekProvisioningCleanupResult
{
    internal sealed record Cleaned(string Receipt) : KekProvisioningCleanupResult;
    internal sealed record AlreadyAbsent(string Receipt) : KekProvisioningCleanupResult;
    internal sealed record CleanupUnavailable : KekProvisioningCleanupResult;
    internal sealed record CleanupOutcomeUnknown : KekProvisioningCleanupResult;
    internal sealed record CleanupFailed : KekProvisioningCleanupResult;
}

internal interface IKekProvisioningRecoveryOperation
{
    Task<KekProvisioningResolution> ResolveProvisioningOperationAsync(ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken);
    Task<KekProvisioningCleanupResult> CleanupProvisioningOperationAsync(string providerCleanupReference,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken);
}
