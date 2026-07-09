namespace TagEkyc.Infrastructure.Retention;

public sealed class RetentionOptions
{
    public const string SectionName = "TagEkyc:Retention";

    public const int MaxRegulatedEvidenceRetentionDays = 36500;

    public int? RegulatedEvidenceRetentionDays { get; init; }
}
