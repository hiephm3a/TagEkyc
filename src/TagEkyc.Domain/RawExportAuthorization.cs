namespace TagEkyc.Domain;

using System.Text.Json.Serialization;

public enum RawExportRawClassSelectionMode
{
    DefaultPolicySet = 0,
    ExplicitSubset = 1,
}

public sealed record RawExportAuthorizationPersistencePayload(
    int PayloadSchemaVersion,
    Guid ExportDecisionId,
    RawExportAuthorizationIdempotencyIdentityPayload IdempotencyIdentity,
    RawExportAuthorizationDecisionPayload Decision,
    IReadOnlyList<RawExportAuthorizationEligibilityCausePayload> EligibilityCauses,
    IReadOnlyList<RawExportAuthorizationFulfillmentRefPayload> FulfillmentRefs,
    IReadOnlyList<RawExportAuthorizationClassPayload> Classes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RawExportAuthorizationPermitPayload? Permit,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<RawExportAuthorizationPermitClassPayload>? PermitClasses);

public sealed record RawExportAuthorizationIdempotencyIdentityPayload(
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid RequestedVerificationSessionId,
    string IdempotencyKey);

public sealed record RawExportAuthorizationDecisionPayload(
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid ApiKeyId,
    Guid RequestedVerificationSessionId,
    Guid PolicyId,
    int PolicyVersion,
    string FingerprintHash,
    string RawClassSelectionMode,
    string Outcome,
    string? PrimaryCause,
    Guid? ResolvedVerificationSessionId,
    Guid? SessionOwnerClientApplicationId,
    string? SessionSubjectRef,
    string? SessionState,
    int? BoundRuleSetVersion,
    int? CurrentRuleSetVersion,
    string? EligibilityPrimaryCause,
    DateTimeOffset? EligibilityEvaluatedAtUtc,
    Guid? GrantPrincipalId,
    Guid? GrantPolicyId,
    int? GrantPolicyVersion,
    int? GrantRevision,
    Guid? LifecyclePolicyId,
    int? LifecyclePolicyVersion,
    int? LifecycleRevision,
    string? PurposeCode,
    Guid? RecipientClientApplicationId,
    string? SubjectConsentCause,
    string? ConsentScopeHash,
    Guid? SubjectConsentRecordId,
    int? ConsentRevision,
    DateTimeOffset? ConsentValidFromUtc,
    DateTimeOffset? ConsentValidUntilUtc,
    DateTimeOffset? ConsentEvaluatedAtUtc,
    int? PolicyPermitTtlSeconds,
    DateTimeOffset? DecisionExpiresAtUtc);

public sealed record RawExportAuthorizationEligibilityCausePayload(int Ordinal, string Cause);

public sealed record RawExportAuthorizationFulfillmentRefPayload(
    int Ordinal,
    string RequirementType,
    Guid FulfillmentEventId,
    int Revision,
    string ArtifactRef,
    string ArtifactVersion,
    DateTimeOffset? ValidUntilUtc);

public sealed record RawExportAuthorizationClassPayload(string ClassKind, string RawClass, int Ordinal);

public sealed record RawExportAuthorizationPermitPayload(
    Guid PermitId,
    Guid ResolvedVerificationSessionId,
    string SubjectRef,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    Guid RecipientClientApplicationId,
    DateTimeOffset DecisionExpiresAtUtc,
    int SchemaVersion);

public sealed record RawExportAuthorizationPermitClassPayload(string RawClass, int Ordinal);
