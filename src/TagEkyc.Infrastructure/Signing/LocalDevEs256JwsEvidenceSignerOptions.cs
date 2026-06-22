namespace TagEkyc.Infrastructure.Signing;

public sealed class LocalDevEs256JwsEvidenceSignerOptions
{
    public const string SectionName = "TagEkyc:EvidenceSigning";

    public string? P12Path { get; init; }

    public string? P12Password { get; init; }

    public string? KeyId { get; init; }
}
