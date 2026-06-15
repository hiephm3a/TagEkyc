namespace TagEkyc.Domain;

public sealed record ScopeGrantSetId
{
    public ScopeGrantSetId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Scope grant set id is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "[scope-grant-set-id]";
}
