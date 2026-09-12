namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class PlatformOperatorCredentialsRow
{
    public Guid CredentialId { get; set; }
    public string KeyLookupPrefix { get; set; } = null!;
    public byte[] SecretDigest { get; set; } = null!;
    public int VerifierPepperVersion { get; set; }
    public Guid PrincipalId { get; set; }
    public string[] Scopes { get; set; } = null!;
    public string State { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
}

public sealed class CaptureRuntimeRolePolicyRevisionsRow
{
    public Guid CatalogId { get; set; }
    public long Revision { get; set; }
    public string[] Roles { get; set; } = null!;
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public Guid PublishedByCredentialId { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
}

public sealed class CaptureRuntimeRolePolicyHeadsRow
{
    public Guid CatalogId { get; set; }
    public long CurrentRevision { get; set; }
    public long HeadRevision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CaptureRuntimeTrustProfileRevisionsRow
{
    public Guid CatalogId { get; set; }
    public long Revision { get; set; }
    public string RuntimeType { get; set; } = null!;
    public bool RetainedRawEnabled { get; set; }
    public bool AllowTrustedEvidence { get; set; }
    public bool RequireHandoffAttestation { get; set; }
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public Guid PublishedByCredentialId { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
}

public sealed class CaptureRuntimeTrustProfileHeadsRow
{
    public Guid CatalogId { get; set; }
    public long CurrentRevision { get; set; }
    public long HeadRevision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CaptureRuntimeConfigurationRevisionsRow
{
    public Guid CatalogId { get; set; }
    public long Revision { get; set; }
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool RawExportEnabled { get; set; }
    public int PlaintextBudgetSeconds { get; set; }
    public int RawExportSourceClaimSafetyMarginMilliseconds { get; set; }
    public int CaptureAgentConfigurationPollingIntervalSeconds { get; set; }
    public int RawExportSourceMaximumChipDg2PortraitBytes { get; set; }
    public int RawExportSourceMaximumLiveSelfieImageBytes { get; set; }
    public long RawExportCaptureMaximumAggregatePlaintextBytesPerHost { get; set; }
    public int RawExportCustodyMaximumPlaintextWindowBytesPerStream { get; set; }
    public long RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment { get; set; }
    public int RawExportIngressMaximumPreAdmissionBufferedBytes { get; set; }
    public Guid PublishedByCredentialId { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
}

public sealed class CaptureRuntimeConfigurationHeadsRow
{
    public Guid CatalogId { get; set; }
    public long CurrentRevision { get; set; }
    public long HeadRevision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CaptureRuntimeRegistrationsRow
{
    public Guid CaptureAgentId { get; set; }
    public string RuntimeType { get; set; } = null!;
    public Guid TrustProfileId { get; set; }
    public long TrustProfileRevision { get; set; }
    public Guid ConfigurationId { get; set; }
    public long ConfigurationRevision { get; set; }
    public Guid? ConfigurationOverrideId { get; set; }
    public Guid? NextRolePolicyId { get; set; }
    public long? NextRolePolicyRevision { get; set; }
    public string LifecycleState { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? RetiredAtUtc { get; set; }
    public string? LifecycleReason { get; set; }
}

public sealed class CaptureRuntimeInstallationsRow
{
    public Guid DeviceInstallationId { get; set; }
    public Guid CaptureAgentId { get; set; }
    public Guid? CurrentCredentialId { get; set; }
    public long? CurrentCredentialGeneration { get; set; }
    public string LifecycleState { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset EnrolledAtUtc { get; set; }
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? RetiredAtUtc { get; set; }
    public string? LifecycleReason { get; set; }
}

public sealed class CaptureRuntimeCredentialGenerationsRow
{
    public Guid CredentialId { get; set; }
    public long Generation { get; set; }
    public Guid DeviceInstallationId { get; set; }
    public Guid CandidateKeyId { get; set; }
    public byte[] PublicVerifierSpki { get; set; } = null!;
    public string Algorithm { get; set; } = null!;
    public byte[] PublicKeyThumbprint { get; set; } = null!;
    public Guid RolePolicyId { get; set; }
    public long RolePolicyRevision { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ValidUntilUtc { get; set; }
    public string State { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset? RotatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? RetiredAtUtc { get; set; }
    public string? TerminalReason { get; set; }
}

public sealed class CaptureRuntimeRequestNoncesRow
{
    public Guid CredentialId { get; set; }
    public long CredentialGeneration { get; set; }
    public byte[] Nonce { get; set; } = null!;
    public DateTimeOffset SignedTimestampUtc { get; set; }
    public DateTimeOffset AdmittedAtUtc { get; set; }
    public DateTimeOffset PurgeAfterUtc { get; set; }
}

public sealed class CaptureRuntimeBootstrapIssuancesRow
{
    public Guid BootstrapIssuanceId { get; set; }
    public string KeyLookupPrefix { get; set; } = null!;
    public byte[] SecretDigest { get; set; } = null!;
    public int VerifierPepperVersion { get; set; }
    public string RuntimeType { get; set; } = null!;
    public Guid TrustProfileId { get; set; }
    public long TrustProfileRevision { get; set; }
    public Guid RolePolicyId { get; set; }
    public long RolePolicyRevision { get; set; }
    public Guid ConfigurationId { get; set; }
    public long ConfigurationRevision { get; set; }
    public byte[] AttestationRequirementDigest { get; set; } = null!;
    public byte[] RequestFingerprint { get; set; } = null!;
    public Guid IssueOperationId { get; set; }
    public Guid IssuedByCredentialId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string State { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset? RedeemedAtUtc { get; set; }
    public Guid? CaptureAgentId { get; set; }
    public Guid? DeviceInstallationId { get; set; }
    public Guid? CredentialId { get; set; }
    public long? CredentialGeneration { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? ExpiredAtUtc { get; set; }
    public string? TerminalReason { get; set; }
}

public sealed class CaptureCapabilitiesRow
{
    public Guid CaptureCapabilityId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string KeyLookupPrefix { get; set; } = null!;
    public byte[] SecretDigest { get; set; } = null!;
    public int VerifierPepperVersion { get; set; }
    public string Audience { get; set; } = null!;
    public string Challenge { get; set; } = null!;
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string State { get; set; } = null!;
    public long Revision { get; set; }
    public Guid? PredecessorCapabilityId { get; set; }
    public Guid? SuccessorCapabilityId { get; set; }
    public DateTimeOffset? BoundAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? ExpiredAtUtc { get; set; }
    public string? TerminalReason { get; set; }
}

public sealed class CaptureExecutionBindingsRow
{
    public Guid CaptureExecutionBindingId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid CaptureCapabilityId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid CaptureAgentId { get; set; }
    public Guid DeviceInstallationId { get; set; }
    public Guid CredentialId { get; set; }
    public long CredentialGeneration { get; set; }
    public byte[] PublicKeyThumbprint { get; set; } = null!;
    public long RuntimeRevision { get; set; }
    public long InstallationRevision { get; set; }
    public long CredentialRevision { get; set; }
    public Guid TrustProfileId { get; set; }
    public long TrustProfileRevision { get; set; }
    public Guid RolePolicyId { get; set; }
    public long RolePolicyRevision { get; set; }
    public Guid ConfigurationId { get; set; }
    public long ConfigurationRevision { get; set; }
    public string Challenge { get; set; } = null!;
    public Guid BindOperationId { get; set; }
    public DateTimeOffset BoundAtUtc { get; set; }
    public DateTimeOffset ExecutionExpiresAtUtc { get; set; }
}

public sealed class CaptureRuntimeConfigurationOverridesRow
{
    public Guid ConfigurationOverrideId { get; set; }
    public Guid CaptureAgentId { get; set; }
    public Guid BaseConfigurationId { get; set; }
    public long BaseConfigurationRevision { get; set; }
    public bool? RawExportEnabled { get; set; }
    public int? PlaintextBudgetSeconds { get; set; }
    public int? RawExportSourceClaimSafetyMarginMilliseconds { get; set; }
    public int? CaptureAgentConfigurationPollingIntervalSeconds { get; set; }
    public int? RawExportSourceMaximumChipDg2PortraitBytes { get; set; }
    public int? RawExportSourceMaximumLiveSelfieImageBytes { get; set; }
    public long? RawExportCaptureMaximumAggregatePlaintextBytesPerHost { get; set; }
    public int? RawExportCustodyMaximumPlaintextWindowBytesPerStream { get; set; }
    public long? RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment { get; set; }
    public int? RawExportIngressMaximumPreAdmissionBufferedBytes { get; set; }
    public long Revision { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CaptureRuntimeRotationAuthorizationsRow
{
    public Guid RotationAuthorizationId { get; set; }
    public Guid CaptureAgentId { get; set; }
    public Guid DeviceInstallationId { get; set; }
    public Guid CredentialId { get; set; }
    public long CurrentGeneration { get; set; }
    public Guid AuthorizeOperationId { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public Guid AuthorizedByCredentialId { get; set; }
    public DateTimeOffset AuthorizedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string State { get; set; } = null!;
    public long Revision { get; set; }
    public Guid? CandidateKeyId { get; set; }
    public byte[]? SuccessorPublicVerifierSpki { get; set; }
    public byte[]? SuccessorPublicKeyThumbprint { get; set; }
    public long? SuccessorGeneration { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? ExpiredAtUtc { get; set; }
    public string? TerminalReason { get; set; }
}

public sealed class CaptureRuntimeManagementOperationsRow
{
    public Guid ActorCredentialId { get; set; }
    public string OperationKind { get; set; } = null!;
    public Guid IdempotencyKey { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public string TargetKind { get; set; } = null!;
    public Guid TargetId { get; set; }
    public string ResultCode { get; set; } = null!;
    public long? ResultRevision { get; set; }
    public Guid? ResultId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public sealed class CaptureRuntimeManagementEventsRow
{
    public Guid EventId { get; set; }
    public Guid ActorCredentialId { get; set; }
    public string OperationKind { get; set; } = null!;
    public Guid IdempotencyKey { get; set; }
    public string EventType { get; set; } = null!;
    public string TargetKind { get; set; } = null!;
    public Guid TargetId { get; set; }
    public long? BeforeRevision { get; set; }
    public long? AfterRevision { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class CaptureCapabilityOperationsRow
{
    public Guid ClientApplicationId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string OperationKind { get; set; } = null!;
    public Guid IdempotencyKey { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public Guid? RuntimeCaptureAgentId { get; set; }
    public Guid? RuntimeInstallationId { get; set; }
    public Guid? RuntimeCredentialId { get; set; }
    public long? RuntimeCredentialGeneration { get; set; }
    public string ResultCode { get; set; } = null!;
    public Guid? ResultCapabilityId { get; set; }
    public Guid? ResultBindingId { get; set; }
    public long? ResultRevision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public sealed class CaptureCapabilityEventsRow
{
    public Guid EventId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string OperationKind { get; set; } = null!;
    public Guid IdempotencyKey { get; set; }
    public Guid? CaptureCapabilityId { get; set; }
    public string EventType { get; set; } = null!;
    public Guid? RuntimeCaptureAgentId { get; set; }
    public Guid? RuntimeInstallationId { get; set; }
    public long? BeforeRevision { get; set; }
    public long? AfterRevision { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class CaptureRuntimeBootstrapRedemptionOperationsRow
{
    public Guid BootstrapIssuanceId { get; set; }
    public Guid RedeemOperationId { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public Guid CandidateKeyId { get; set; }
    public string ResultCode { get; set; } = null!;
    public Guid? CaptureAgentId { get; set; }
    public Guid? DeviceInstallationId { get; set; }
    public Guid? CredentialId { get; set; }
    public long? Generation { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public sealed class CaptureRuntimeRotationCompletionOperationsRow
{
    public Guid RotationAuthorizationId { get; set; }
    public Guid BusinessIdempotencyKey { get; set; }
    public byte[] RequestFingerprint { get; set; } = null!;
    public Guid DeviceInstallationId { get; set; }
    public Guid CredentialId { get; set; }
    public long PredecessorGeneration { get; set; }
    public long? SuccessorGeneration { get; set; }
    public Guid CandidateKeyId { get; set; }
    public string ResultCode { get; set; } = null!;
    public long? CredentialRevision { get; set; }
    public long? InstallationRevision { get; set; }
    public long? RotationRevision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public sealed class CaptureRuntimeBootstrapRedemptionEventsRow
{
    public Guid EventId { get; set; }
    public Guid BootstrapIssuanceId { get; set; }
    public Guid RedeemOperationId { get; set; }
    public string EventType { get; set; } = null!;
    public Guid? CaptureAgentId { get; set; }
    public Guid? DeviceInstallationId { get; set; }
    public Guid? CredentialId { get; set; }
    public long? Generation { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class CaptureRuntimeRotationCompletionEventsRow
{
    public Guid EventId { get; set; }
    public Guid RotationAuthorizationId { get; set; }
    public Guid BusinessIdempotencyKey { get; set; }
    public string EventType { get; set; } = null!;
    public Guid CredentialId { get; set; }
    public long PredecessorGeneration { get; set; }
    public long? SuccessorGeneration { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class PlatformOperatorRootOperationsRow
{
    public Guid OperationId { get; set; }
    public string OperationKind { get; set; } = null!;
    public byte[] RequestFingerprint { get; set; } = null!;
    public Guid OperatorPrincipalId { get; set; }
    public Guid? TargetCredentialId { get; set; }
    public string ResultCode { get; set; } = null!;
    public long? ResultRevision { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}

public sealed class PlatformOperatorRootEventsRow
{
    public Guid EventId { get; set; }
    public Guid OperationId { get; set; }
    public string EventType { get; set; } = null!;
    public Guid OperatorPrincipalId { get; set; }
    public Guid? TargetCredentialId { get; set; }
    public long? BeforeRevision { get; set; }
    public long? AfterRevision { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class CaptureRuntimeCutoverStateRow
{
    public string Profile { get; set; } = null!;
    public string State { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset PreparedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public Guid? ActivatedByCredentialId { get; set; }
}

