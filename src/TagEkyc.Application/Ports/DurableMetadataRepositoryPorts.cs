using TagEkyc.Application;
using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public sealed record DurableActorCredentialMetadata(
    PrincipalId PrincipalId,
    AuthenticatedCallerCategory ActorCategory,
    CredentialRef CredentialRef,
    CredentialType CredentialType,
    CredentialStatus CredentialStatus,
    ScopeGrantSetId ScopeGrantSetId);

public sealed record DurableSessionMetadata(
    Guid VerificationSessionId,
    Guid ClientApplicationId,
    string? TenantId,
    VerificationSessionState State,
    PolicySnapshotId PolicySnapshotId,
    DurableActorCredentialMetadata ActorCredential,
    RetentionClass RetentionClass,
    LegalHoldStatus LegalHoldStatus,
    DeletionEligibility DeletionEligibility,
    PurgeBlockReason PurgeBlockReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record DurableAuditIdentityMetadata(
    Guid? VerificationSessionId,
    Guid ClientApplicationId,
    PrincipalId PrincipalId,
    AuthenticatedCallerCategory ActorCategory,
    CredentialRef CredentialRef,
    CredentialType CredentialType,
    ScopeGrantSetId ScopeGrantSetId,
    string Operation,
    string AuthorizationDecision,
    string? ReasonCode,
    string RequestId,
    string CorrelationId,
    DateTimeOffset OccurredAt);

public sealed record DurableEvidencePackageMetadata(
    Guid EvidencePackageId,
    Guid VerificationSessionId,
    string PackageVersion,
    string PackageRef,
    PolicySnapshotId PolicySnapshotId,
    DateTimeOffset CreatedAt);

public sealed record DurableCompletionAuthorityMetadata(
    Guid VerificationSessionId,
    PrincipalId RequestedByPrincipalId,
    AuthenticatedCallerCategory RequestedByActorCategory,
    PrincipalId FinalizedByPrincipalId,
    AuthenticatedCallerCategory FinalizedByActorCategory,
    DateTimeOffset CompletedAt);

public sealed record DurableMetadataWriteSet(
    DurableSessionMetadata Session,
    DurableAuditIdentityMetadata AuditIdentity,
    DurableEvidencePackageMetadata? EvidencePackage,
    DurableCompletionAuthorityMetadata? CompletionAuthority);

public interface IDurableMetadataRepository
{
    Task SaveSessionMetadataAsync(
        DurableSessionMetadata session,
        CancellationToken cancellationToken = default);

    Task AppendAuditIdentityAsync(
        DurableAuditIdentityMetadata auditIdentity,
        CancellationToken cancellationToken = default);

    Task SaveEvidencePackageMetadataAsync(
        DurableEvidencePackageMetadata evidencePackage,
        CancellationToken cancellationToken = default);

    Task SaveCompletionAuthorityMetadataAsync(
        DurableCompletionAuthorityMetadata completionAuthority,
        CancellationToken cancellationToken = default);

    Task SaveMetadataWriteSetAsync(
        DurableMetadataWriteSet writeSet,
        CancellationToken cancellationToken = default);
}
