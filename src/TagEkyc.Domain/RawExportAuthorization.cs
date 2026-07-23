namespace TagEkyc.Domain;

using System.Text.Json.Serialization;

public enum RawExportRawClassSelectionMode
{
    DefaultPolicySet = 0,
    ExplicitSubset = 1,
}

public enum RawExportAuthorizationOutcome
{
    Authorized = 0,
    Denied = 1,
}

public enum RawExportAuthorizationPrimaryCause
{
    SESSION_NOT_FOUND = 0,
    SESSION_NOT_OWNED = 1,
    SESSION_NOT_COMPLETED = 2,
    EXPORT_ELIGIBILITY_INACTIVE = 3,
    POLICY_PERMIT_TTL_INVALID = 4,
    REQUESTED_RAW_CLASSES_NOT_ALLOWED = 5,
    SUBJECT_CONSENT_NOT_EFFECTIVE = 6,
    SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT = 7,
}

public enum RawExportAuthorizationClassKind
{
    PolicyAllowed = 0,
    Requested = 1,
    Effective = 2,
    Consented = 3,
    Authorized = 4,
}

public sealed record AuthenticatedRawExportActor(
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid ApiKeyId);

public sealed record AuthorizeRawExportCommand(
    AuthenticatedRawExportActor Actor,
    Guid RequestedVerificationSessionId,
    Guid PolicyId,
    int PolicyVersion,
    IReadOnlyList<RawExportRawClass>? RequestedRawClasses,
    string IdempotencyKey);

public sealed record RawExportAuthorizationDecision(
    Guid ExportDecisionId,
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid ApiKeyId,
    Guid RequestedVerificationSessionId,
    Guid PolicyId,
    int PolicyVersion,
    byte[] FingerprintHash,
    RawExportRawClassSelectionMode RawClassSelectionMode,
    RawExportAuthorizationOutcome Outcome,
    RawExportAuthorizationPrimaryCause? PrimaryCause,
    Guid? ResolvedVerificationSessionId,
    Guid? SessionOwnerClientApplicationId,
    string? SessionSubjectRef,
    VerificationSessionState? SessionState,
    int? BoundRuleSetVersion,
    int? CurrentRuleSetVersion,
    RawExportEligibilityCause? EligibilityPrimaryCause,
    DateTimeOffset? EligibilityEvaluatedAtUtc,
    RawExportGrantRef? GrantRef,
    RawExportLifecycleRef? LifecycleRef,
    string? PurposeCode,
    Guid? RecipientClientApplicationId,
    RawExportSubjectConsentCause? SubjectConsentCause,
    string? ConsentScopeHash,
    RawExportSubjectConsentRef? SubjectConsentRef,
    DateTimeOffset? ConsentValidFromUtc,
    DateTimeOffset? ConsentValidUntilUtc,
    DateTimeOffset? ConsentEvaluatedAtUtc,
    int? PolicyPermitTtlSeconds,
    DateTimeOffset? DecisionExpiresAtUtc,
    DateTimeOffset DecidedAtUtc);

public sealed record RawExportAuthorizationEligibilityCause(
    int Ordinal,
    RawExportEligibilityCause Cause);

public sealed record RawExportAuthorizationFulfillmentRef(
    int Ordinal,
    RawExportRequirementType RequirementType,
    Guid FulfillmentEventId,
    int Revision,
    string ArtifactRef,
    string ArtifactVersion,
    DateTimeOffset? ValidUntilUtc);

public sealed record RawExportAuthorizationClassSnapshot(
    RawExportAuthorizationClassKind ClassKind,
    RawExportRawClass RawClass,
    int Ordinal);

public sealed record RawExportAuthorizationPermit(
    Guid PermitId,
    Guid AuthorizationDecisionId,
    Guid ResolvedVerificationSessionId,
    string SubjectRef,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    Guid RecipientClientApplicationId,
    DateTimeOffset DecisionExpiresAtUtc,
    int SchemaVersion,
    DateTimeOffset CreatedAt);

public sealed record RawExportAuthorizationPermitClass(
    RawExportRawClass RawClass,
    int Ordinal);

public sealed record RawExportAuthorizationResult(
    RawExportAuthorizationDecision Decision,
    IReadOnlyList<RawExportAuthorizationEligibilityCause> EligibilityCauses,
    IReadOnlyList<RawExportAuthorizationFulfillmentRef> FulfillmentRefs,
    IReadOnlyList<RawExportAuthorizationClassSnapshot> Classes,
    RawExportAuthorizationPermit? Permit,
    IReadOnlyList<RawExportAuthorizationPermitClass> PermitClasses);

public class RawExportAuthorizationException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportAuthorizationInputException()
    : RawExportAuthorizationException("REQUEST_VALIDATION_FAILED");

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
