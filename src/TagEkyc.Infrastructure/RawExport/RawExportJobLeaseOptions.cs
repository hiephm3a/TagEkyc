namespace TagEkyc.Infrastructure.RawExport;

public sealed record RawExportJobLeaseState(int LeaseSeconds, bool IsValid, string? InvalidCode);

public static class RawExportJobLeaseOptions
{
    public const string Key = "TagEkyc:RawExport:JobLeaseSeconds";
    public const string InvalidCode = "PROD_RAW_EXPORT_JOB_LEASE_CONFIG_INVALID";
    public const int DefaultSeconds = 60;

    public static RawExportJobLeaseState Resolve(string? value)
    {
        if (value is null)
        {
            return new(DefaultSeconds, true, null);
        }

        return int.TryParse(value, out var seconds) && seconds is >= 10 and <= 300
            ? new(seconds, true, null)
            : new(0, false, InvalidCode);
    }
}
