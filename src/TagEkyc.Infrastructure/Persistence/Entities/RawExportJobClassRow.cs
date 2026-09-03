namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportJobClassRow
{
    public Guid JobId { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public int Ordinal { get; set; }
}
