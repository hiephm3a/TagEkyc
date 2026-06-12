namespace TagEkyc.Domain;

public sealed record PolicySnapshotId
{
    public const string LocalDevS1Value = "LOCALDEV-S1-POLICY-V1";

    public static readonly PolicySnapshotId LocalDevS1 = new(LocalDevS1Value);

    public PolicySnapshotId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Policy snapshot id is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
