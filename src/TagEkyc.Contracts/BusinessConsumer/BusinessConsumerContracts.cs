using TagEkyc.Contracts.Common;

namespace TagEkyc.Contracts.BusinessConsumer;

public sealed record RequiredCheckRequestDto(
    RequiredCheckTypeDto CheckType,
    bool Required,
    decimal? MinimumConfidence);

public sealed record CreateVerificationSessionRequestDto(
    string? ExternalSessionId,
    string SubjectRef,
    string Purpose,
    VerificationProfileDto Profile,
    IReadOnlyList<RequiredCheckRequestDto> RequiredChecks,
    DateTimeOffset ExpiresAt,
    string? ExternalTransactionId = null,
    string? BindingNonceHash = null,
    string? RequestId = null,
    string? CorrelationId = null);

public sealed record CreateVerificationSessionResponseDto(
    string VerificationSessionId,
    VerificationProfileDto Profile,
    VerificationSessionStateDto State,
    VerificationResultDto Result,
    string RequestId,
    string CorrelationId,
    DateTimeOffset ExpiresAt);

public sealed record VerificationSessionSummaryDto(
    string VerificationSessionId,
    VerificationProfileDto Profile,
    string? ExternalSessionId,
    string Purpose,
    VerificationSessionStateDto State,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    string? EvidencePackageId,
    string? EvidencePackageHash,
    string? ManifestHash,
    string RequestId,
    string CorrelationId,
    DateTimeOffset? CompletedAt);

public sealed record EvidenceRefSummaryDto(
    string ResultType,
    string EvidenceResultId,
    string Type,
    string Id,
    string? ArtifactHash);

public sealed record CompleteVerificationSessionRequestDto(
    bool ForceReview = false,
    string? RequestId = null,
    string? CorrelationId = null);

public sealed record CompleteVerificationSessionResponseDto(
    string VerificationSessionId,
    VerificationSessionStateDto State,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    string FinalDecisionId,
    string EvidencePackageId,
    string EvidencePackageHash,
    string ManifestHash,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CompletedAt,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus);

public sealed record EvidencePackageSummaryDto(
    string EvidencePackageId,
    string VerificationSessionId,
    string PackageVersion,
    string PackageHash,
    string ManifestHash,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    IReadOnlyList<EvidenceRefSummaryDto> EvidenceRefs,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CompletedAt);

public sealed record VerificationCompletedEventDto(
    string EventType,
    string DeliveryId,
    DateTimeOffset SentAt,
    string VerificationSessionId,
    VerificationProfileDto Profile,
    string? ExternalSessionId,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    string EvidencePackageId,
    string EvidencePackageHash,
    string CorrelationId,
    SignaturePlaceholderStatusDto WebhookSignatureStatus,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus);
