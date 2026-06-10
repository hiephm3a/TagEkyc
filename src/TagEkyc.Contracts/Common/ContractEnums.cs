namespace TagEkyc.Contracts.Common;

public enum VerificationProfileDto
{
    StandardEkycProfile = 0,
    TransactionBoundEkycProfile = 1,
}

public enum VerificationSessionStateDto
{
    Created = 0,
    InProgress = 1,
    ReadyToComplete = 2,
    Completed = 3,
    Expired = 4,
    Cancelled = 5,
    TechnicalTerminal = 6,
}

public enum VerificationResultDto
{
    NotAvailable = 0,
    Passed = 1,
    RetryRequired = 2,
    FailedCaptureQuality = 3,
    FailedIdentity = 4,
    ReviewRequired = 5,
    TechnicalError = 6,
    NotSupported = 7,
}

public enum RequiredCheckTypeDto
{
    CaptureQuality = 0,
    DocumentOcr = 1,
    DocumentNfc = 2,
    FaceMatch = 3,
    Liveness = 4,
    Fingerprint = 5,
    RiskEvaluation = 6,
}

public enum AssuranceLevelDto
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Unknown = 4,
}

public enum SignaturePlaceholderStatusDto
{
    PlaceholderUnverified = 0,
}
