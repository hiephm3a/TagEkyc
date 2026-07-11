namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportPolicyAllowedClassRow
{
    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public string RawClass { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
