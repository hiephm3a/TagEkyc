namespace TagEkyc.Domain;

public enum CaptureArtifactQualityState
{
    Pending = 0,
    RetryRequired = 1,
    FailedCaptureQuality = 2,
    AcceptedForVerification = 3,
    TechnicalError = 4,
}
