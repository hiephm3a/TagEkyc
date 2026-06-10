namespace TagEkyc.Domain;

public enum VerificationSessionState
{
    Created = 0,
    InProgress = 1,
    ReadyToComplete = 2,
    Completed = 3,
}