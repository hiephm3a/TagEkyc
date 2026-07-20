using TagEkyc.Domain;
using TagEkyc.Contracts.InternalAudit.Manifest;

namespace TagEkyc.Application.Ports;

public interface IVerificationSessionRepository
{
    Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default);

    Task<VerificationSession?> GetAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);

    Task<VerificationSession?> GetByExternalSessionIdAsync(
        Guid clientApplicationId,
        string externalSessionId,
        CancellationToken cancellationToken = default);

    Task SetStateAsync(
        Guid verificationSessionId,
        VerificationSessionState state,
        CancellationToken cancellationToken = default);
}

public interface ICaptureArtifactRepository
{
    Task AppendAsync(CaptureArtifact artifact, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaptureArtifact>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IEvidenceResultRepository
{
    Task AppendAsync(EvidenceResult evidenceResult, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceResult>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IVerificationDecisionRepository
{
    Task AppendAsync(VerificationDecision decision, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VerificationDecision>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);

    Task<VerificationDecision?> GetAsync(Guid verificationDecisionId, CancellationToken cancellationToken = default);
}

public interface IEvidencePackageRepository
{
    Task AppendAsync(EvidencePackage evidencePackage, CancellationToken cancellationToken = default);

    Task<EvidencePackage?> GetAsync(Guid evidencePackageId, CancellationToken cancellationToken = default);

    Task<EvidencePackage?> GetBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public interface IInternalEvidenceManifestRepository
{
    Task AppendAsync(EvidenceManifestDto manifest, CancellationToken cancellationToken = default);

    Task<EvidenceManifestDto?> GetByPackageAsync(Guid evidencePackageId, CancellationToken cancellationToken = default);
}

public interface IAuditEventRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default);
}

public sealed record AppendIdempotencyRecord(
    Guid VerificationSessionId,
    string IdempotencyKey,
    string EndpointKind,
    string SubmissionSlot,
    Guid MintedId,
    string Fingerprint,
    DateTimeOffset CreatedAt);

public enum AppendIdempotencyApplyStatus
{
    Applied = 0,
    Deduplicated = 1,
    SlotMismatch = 2,
    PayloadMismatch = 3,
    SessionTerminal = 4,
}

public sealed record AppendIdempotencyApplyResult(
    AppendIdempotencyApplyStatus Status,
    AppendIdempotencyRecord Record);

public sealed record AppendCaptureArtifactWrite(
    AppendIdempotencyRecord Idempotency,
    CaptureArtifact Artifact,
    VerificationSession Session,
    VerificationSessionState FinalState,
    IReadOnlyList<AuditEvent> AuditEvents);

public sealed record AppendEvidenceResultWrite(
    AppendIdempotencyRecord Idempotency,
    EvidenceResult EvidenceResult,
    VerificationSession Session,
    VerificationSessionState FinalState,
    IReadOnlyList<AuditEvent> AuditEvents);

public interface IAppendIdempotencyRepository
{
    Task<AppendIdempotencyRecord?> GetAsync(
        Guid verificationSessionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public interface IAppendIdempotencyBoundary
{
    Task<AppendIdempotencyApplyResult> TryApplyCaptureArtifactAsync(
        AppendCaptureArtifactWrite write,
        CancellationToken cancellationToken = default);

    Task<AppendIdempotencyApplyResult> TryApplyEvidenceResultAsync(
        AppendEvidenceResultWrite write,
        CancellationToken cancellationToken = default);
}

public interface IClientApplicationPolicyReader
{
    Task<ClientApplication?> GetClientApplicationAsync(Guid clientApplicationId, CancellationToken cancellationToken = default);

    Task<ApiKeyMetadata?> GetApiKeyMetadataAsync(string keyPrefix, CancellationToken cancellationToken = default);
}

public sealed record AddRawExportPolicyVersionCommand(
    Guid PolicyId,
    int ExpectedLatestVersion,
    RawExportMode Mode,
    string Purpose,
    string? RetentionProfileRef,
    string? RetentionPurposeCode,
    RawExportConsentRequirement ConsentRequirement,
    string? RecipientCategory,
    string? RecipientAssuranceRequirement,
    string? ControllerRole,
    string? ControllerEntityRef,
    string? ControllerJurisdiction,
    string? RecipientJurisdiction,
    string? ProcessingInfrastructureJurisdiction,
    string? TransferScenarioCode,
    string? TransferLegalBasisCode,
    IReadOnlySet<RawExportRawClass> AllowedClasses);

public sealed record CloseRawExportPolicyVersionCommand(
    Guid PolicyId,
    int PolicyVersion,
    string ClosedByPrincipalId,
    string DecisionRef);

public interface IRawExportPolicyRepository
{
    Task<RawExportPolicyVersion> AddVersionAsync(
        AddRawExportPolicyVersionCommand command,
        CancellationToken cancellationToken = default);

    Task<RawExportPolicyVersion> CatalogApproveAsync(
        CloseRawExportPolicyVersionCommand command,
        CancellationToken cancellationToken = default);

    Task<RawExportPolicyVersion> AbandonDraftAsync(
        CloseRawExportPolicyVersionCommand command,
        CancellationToken cancellationToken = default);

    Task<RawExportPolicyVersion?> GetVersionAsync(
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default);

    Task<RawExportPolicyVersion?> GetLatestVersionAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    Task<RawExportPolicyVersion?> GetLatestCatalogApprovedVersionAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawExportPolicyVersion>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed record RawExportGrantCommand(
    Guid ActorPrincipalId,
    Guid PrincipalId,
    Guid PolicyId,
    int PolicyVersion,
    int ExpectedRevision,
    Guid? ClientApplicationId,
    string DecisionRef);

public sealed record RawExportAuthorityCommand(
    Guid ActorPrincipalId,
    Guid PrincipalId,
    RawExportAuthorityType AuthorityType,
    RawExportAuthorityScopeType ScopeType,
    Guid? ScopeId,
    RawExportRequirementType? RequirementType,
    int ExpectedRevision,
    string DecisionRef);

public sealed record RawExportFulfillmentAcceptCommand(
    Guid ActorPrincipalId,
    Guid PolicyId,
    int PolicyVersion,
    RawExportRequirementType RequirementType,
    int ExpectedRevision,
    int? SupersedesRevision,
    string ArtifactRef,
    string ArtifactVersion,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    string DecisionRef);

public sealed record RawExportFulfillmentWithdrawCommand(
    Guid ActorPrincipalId,
    Guid PolicyId,
    int PolicyVersion,
    RawExportRequirementType RequirementType,
    int ExpectedRevision,
    int TargetRevision,
    string DecisionRef);

public sealed record RawExportLifecycleCommand(
    Guid ActorPrincipalId,
    Guid PolicyId,
    int PolicyVersion,
    int ExpectedRevision,
    string DecisionRef);

public interface IRawExportControlPlaneRepository
{
    Task<int> GrantExportPolicyAsync(RawExportGrantCommand command, CancellationToken cancellationToken = default);

    Task<int> RevokeExportPolicyGrantAsync(RawExportGrantCommand command, CancellationToken cancellationToken = default);

    Task<int> GrantControlAuthorityAsync(RawExportAuthorityCommand command, CancellationToken cancellationToken = default);

    Task<int> RevokeControlAuthorityAsync(RawExportAuthorityCommand command, CancellationToken cancellationToken = default);

    Task<int> AcceptFulfillmentAsync(RawExportFulfillmentAcceptCommand command, CancellationToken cancellationToken = default);

    Task<int> WithdrawFulfillmentAsync(RawExportFulfillmentWithdrawCommand command, CancellationToken cancellationToken = default);

    Task<int> ActivatePolicyAsync(RawExportLifecycleCommand command, CancellationToken cancellationToken = default);

    Task<int> SuspendPolicyAsync(RawExportLifecycleCommand command, CancellationToken cancellationToken = default);

    Task<int> RevokePolicyAsync(RawExportLifecycleCommand command, CancellationToken cancellationToken = default);

    Task<RawExportEligibilitySnapshot> ResolveExportEligibilityForAuthorizationAsync(
        Guid principalId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default);
}

public interface IRawExportSubjectConsentRepository
{
    Task<int> GrantConsentAuthorityAsync(
        RawExportSubjectConsentAuthorityCommand command,
        CancellationToken cancellationToken = default);

    Task<int> RevokeConsentAuthorityAsync(
        RawExportSubjectConsentAuthorityCommand command,
        CancellationToken cancellationToken = default);

    Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentGrantedAsync(
        RawExportSubjectConsentGrantCommand command,
        CancellationToken cancellationToken = default);

    Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentWithdrawnAsync(
        RawExportSubjectConsentWithdrawCommand command,
        CancellationToken cancellationToken = default);

    Task<RawExportSubjectConsentSnapshot> ResolveSubjectExportConsentForAuthorizationAsync(
        Guid verificationSessionId,
        Guid policyId,
        int policyVersion,
        CancellationToken cancellationToken = default);
}

public sealed record VerificationFinalizationWrite(
    VerificationSession ExpectedSession,
    VerificationSession CompletedSession,
    VerificationDecision Decision,
    EvidencePackage EvidencePackage,
    EvidenceManifestDto Manifest,
    AuditEvent CompletionAuditEvent);

public sealed record VerificationCancellationWrite(
    VerificationSession ExpectedSession,
    VerificationSession CancelledSession,
    AuditEvent CancellationAuditEvent);

public enum VerificationFinalizationWriteStatus
{
    Applied = 0,
    AlreadyCompleted = 1,
    StateMismatch = 2,
    NotFound = 3,
}

public sealed record VerificationFinalizationWriteResult(
    VerificationFinalizationWriteStatus Status,
    VerificationSession? Session);

public interface IVerificationFinalizationBoundary
{
    Task<VerificationFinalizationWriteResult> TryFinalizeAsync(
        VerificationFinalizationWrite write,
        CancellationToken cancellationToken = default);

    Task<VerificationFinalizationWriteResult> TryCancelAsync(
        VerificationCancellationWrite write,
        CancellationToken cancellationToken = default);
}
