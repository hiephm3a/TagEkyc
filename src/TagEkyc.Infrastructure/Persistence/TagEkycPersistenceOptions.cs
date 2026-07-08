namespace TagEkyc.Infrastructure.Persistence;

public sealed class TagEkycPersistenceOptions
{
    public const string SectionName = "TagEkyc:Persistence";

    public string Provider { get; set; } = "InMemory";

    public string? ConnectionString { get; set; }

    public string? ConnectionStringSecretRef { get; set; }

    public bool IsPostgres => string.Equals(Provider, "Postgres", StringComparison.OrdinalIgnoreCase);

    public bool IsInMemory => string.Equals(Provider, "InMemory", StringComparison.OrdinalIgnoreCase);
}
