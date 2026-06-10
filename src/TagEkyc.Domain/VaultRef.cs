namespace TagEkyc.Domain;

public readonly record struct VaultRef
{
    public VaultRef(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("VaultRef value is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
