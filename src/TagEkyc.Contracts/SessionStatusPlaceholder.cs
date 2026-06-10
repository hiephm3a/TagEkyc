namespace TagEkyc.Contracts;

public sealed record SessionStatusPlaceholder(
    string VerificationSessionId,
    string Profile,
    string State,
    string Result);
