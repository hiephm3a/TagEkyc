namespace TagEkyc.Domain;

public sealed record RequiredCheck(
    RequiredCheckType CheckType,
    bool Required,
    decimal? MinimumConfidence,
    string PolicyVersion,
    VerificationProfile Profile,
    DateTimeOffset CreatedAt);
