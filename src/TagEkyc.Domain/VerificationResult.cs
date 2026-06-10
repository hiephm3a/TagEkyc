namespace TagEkyc.Domain;

public enum VerificationResult
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
