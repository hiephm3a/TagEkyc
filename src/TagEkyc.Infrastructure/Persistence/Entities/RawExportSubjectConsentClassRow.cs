namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSubjectConsentClassRow
{
    public Guid SubjectConsentRecordId { get; set; }

    public string RawClass { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
