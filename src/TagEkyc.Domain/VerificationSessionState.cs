namespace TagEkyc.Domain;

public enum VerificationSessionState
{
    Created = 0,
    InProgress = 1,
    ReadyToComplete = 2,
    Completed = 3,
    Expired = 4,
    Cancelled = 5,
    TechnicalTerminal = 6,
}
