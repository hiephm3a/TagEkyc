namespace TagEkyc.Domain;

public readonly record struct MetadataReferenceId
{
    private static readonly string[] ForbiddenValuePrefixes =
    [
        "raw:",
        "payload:",
        "provider-payload:",
        "bytes:",
        "byte:",
        "artifact-bytes:",
        "artifact-raw:",
        "raw-artifact:",
        "content:",
        "secret:",
        "token:"
    ];

    public MetadataReferenceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Metadata reference id is required.", nameof(value));
        }

        if (ForbiddenValuePrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Metadata reference id must not carry raw payload or artifact byte values.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "[metadata-reference-id]";
}
