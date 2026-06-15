namespace TagEkyc.Domain;

public sealed record CredentialRef
{
    private static readonly string[] ForbiddenValuePrefixes =
    [
        "raw:",
        "secret:",
        "hashed:",
        "hash:",
        "sha256:",
        "sha512:",
        "token:",
        "bearer:",
        "password:",
        "private-key:",
        "client-secret:",
        "api-key:",
        "apikey:"
    ];

    public CredentialRef(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Credential reference is required.", nameof(value));
        }

        if (ForbiddenValuePrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Credential reference must be a safe non-secret reference.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "[credential-ref]";
}
