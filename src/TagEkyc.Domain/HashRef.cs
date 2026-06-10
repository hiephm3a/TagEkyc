namespace TagEkyc.Domain;

public readonly record struct HashRef
{
    public HashRef(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Hash value is required.", nameof(value));
        }

        if (!value.StartsWith("sha256:", StringComparison.Ordinal))
        {
            throw new ArgumentException("Hash value must use the sha256: prefix.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
