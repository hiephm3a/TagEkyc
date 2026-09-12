namespace TagEkyc.Contracts.CaptureRuntime;

public sealed record CaptureRuntimeTrustProfilePublicationRequest(
    Guid CatalogId, long ExpectedHeadRevision, DateTimeOffset EffectiveAtUtc,
    DateTimeOffset ExpiresAtUtc, string RuntimeType, bool RetainedRawEnabled,
    bool AllowTrustedEvidence, bool RequireHandoffAttestation);

public sealed record CaptureRuntimeRolePolicyPublicationRequest(
    Guid CatalogId, long ExpectedHeadRevision, DateTimeOffset EffectiveAtUtc,
    IReadOnlyList<string> Roles);

public sealed record CaptureRuntimeConfigurationPublicationRequest(
    Guid CatalogId, long ExpectedHeadRevision, DateTimeOffset EffectiveAtUtc,
    DateTimeOffset ExpiresAtUtc, bool RawExportEnabled, int PlaintextBudgetSeconds,
    int RawExportSourceClaimSafetyMarginMilliseconds,
    int CaptureAgentConfigurationPollingIntervalSeconds,
    int RawExportSourceMaximumChipDg2PortraitBytes,
    int RawExportSourceMaximumLiveSelfieImageBytes,
    long RawExportCaptureMaximumAggregatePlaintextBytesPerHost,
    int RawExportCustodyMaximumPlaintextWindowBytesPerStream,
    long RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment,
    int RawExportIngressMaximumPreAdmissionBufferedBytes);

public sealed record CaptureRuntimeCatalogPublicationResponse(
    Guid CatalogId, long Revision, long HeadRevision);
