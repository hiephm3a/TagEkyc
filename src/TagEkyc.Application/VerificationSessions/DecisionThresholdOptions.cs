namespace TagEkyc.Application.VerificationSessions;

public sealed class DecisionThresholdOptions
{
    public const string SectionName = "TagEkyc:DecisionThresholds";

    public const decimal DefaultFaceMatch = 0.80m;

    public const decimal DefaultLiveness = 0.80m;

    public decimal FaceMatch { get; init; } = DefaultFaceMatch;

    public decimal Liveness { get; init; } = DefaultLiveness;
}
