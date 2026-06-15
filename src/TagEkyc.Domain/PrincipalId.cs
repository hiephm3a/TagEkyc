namespace TagEkyc.Domain;

public sealed record PrincipalId
{
    public PrincipalId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Principal id is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "[principal-id]";
}
