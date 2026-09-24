namespace TagEkyc.Contracts.RawExport;

public sealed record AuthorizeRawExportRequestDto(
    Guid VerificationSessionId,
    Guid PolicyId,
    int PolicyVersion,
    IReadOnlyList<string>? RequestedRawClasses = null);

public sealed record RawExportAuthorizationDecisionDto(
    Guid DecisionId,
    string Outcome,
    string? PrimaryCause,
    Guid? PermitId,
    DateTimeOffset? PermitExpiresAtUtc,
    IReadOnlyList<string> AuthorizedRawClasses);

public sealed record BindRawExportJobRequestDto(Guid PermitId);

public sealed record RawExportJobBindingDto(
    Guid JobId,
    string BindStatus);

public sealed record RawExportJobStatusDto(
    Guid JobId,
    Guid VerificationSessionId,
    Guid RecipientClientApplicationId,
    string State,
    long Revision,
    DateTimeOffset JobExpiresAtUtc,
    IReadOnlyList<string> RawClasses,
    Guid? PackageId,
    string? PackageState,
    DateTimeOffset? PackageFinalizedAtUtc);

public static class RawExportControlPlaneErrorCodes
{
    public const string Forbidden = "RAW_EXPORT_CONTROL_FORBIDDEN";
    public const string RequestInvalid = "RAW_EXPORT_CONTROL_REQUEST_INVALID";
    public const string NotFound = "RAW_EXPORT_JOB_NOT_FOUND";
    public const string Unavailable = "RAW_EXPORT_CONTROL_UNAVAILABLE";
}
