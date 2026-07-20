namespace TagEkyc.Domain;

public enum RawExportSubjectConsentEventType
{
    Granted = 0,
    Withdrawn = 1,
}

public enum RawExportSubjectConsentAuthorityType
{
    SubjectConsentRecorder = 0,
    SubjectConsentWithdrawer = 1,
}

public enum RawExportSubjectConsentAuthorityEventType
{
    Granted = 0,
    Revoked = 1,
}

public enum RawExportSubjectConsentState
{
    Effective = 0,
    NonEffective = 1,
}

public enum RawExportSubjectConsentCause
{
    Missing = 0,
    Withdrawn = 1,
    Expired = 2,
}

public sealed record RawExportSubjectConsentRef(
    Guid SubjectConsentRecordId,
    string ConsentScopeHash,
    int Revision);

public sealed record RawExportSubjectConsentSnapshot(
    RawExportSubjectConsentState State,
    RawExportSubjectConsentCause? Cause,
    RawExportSubjectConsentRef? ConsentRef,
    Guid VerificationSessionId,
    string SubjectRef,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    Guid RecipientClientApplicationId,
    IReadOnlyList<RawExportRawClass> ConsentedRawClasses,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    DateTimeOffset EvaluatedAtUtc,
    string? ConsentTextVersion,
    string? ConsentTextContentHash,
    string? ExternalConsentArtifactRef,
    string? DecisionRef);

public sealed record RawExportSubjectConsentGrantCommand(
    Guid ActorPrincipalId,
    Guid VerificationSessionId,
    Guid PolicyId,
    int PolicyVersion,
    IReadOnlySet<RawExportRawClass> ConsentedRawClasses,
    string ConsentTextVersion,
    string ConsentTextContentHash,
    string ExternalConsentArtifactRef,
    string? DecisionRef,
    DateTimeOffset? ValidUntilUtc = null);

public sealed record RawExportSubjectConsentWithdrawCommand(
    Guid ActorPrincipalId,
    Guid VerificationSessionId,
    Guid PolicyId,
    int PolicyVersion,
    int ExpectedRevision,
    int TargetRevision,
    string? DecisionRef,
    string? ExternalConsentArtifactRef = null);

public sealed record RawExportSubjectConsentAuthorityCommand(
    Guid ActorPrincipalId,
    Guid AuthorityPrincipalId,
    Guid ClientApplicationId,
    RawExportSubjectConsentAuthorityType AuthorityType,
    int ExpectedRevision,
    string? DecisionRef,
    int? TargetRevision = null,
    DateTimeOffset? ValidUntilUtc = null);

public static class RawExportSubjectConsentConstants
{
    public const string PurposeCode = "SubjectRawBiometricExport";
}
