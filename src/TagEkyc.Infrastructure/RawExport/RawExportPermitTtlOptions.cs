namespace TagEkyc.Infrastructure.RawExport;

public sealed record RawExportPermitTtlBoundsState(bool IsValid, int MinSeconds, int MaxSeconds)
{
    public const string InvalidCode = "PROD_RAW_EXPORT_PERMIT_TTL_BOUNDS_INVALID";

    public static RawExportPermitTtlBoundsState Valid(int minSeconds, int maxSeconds) =>
        new(true, minSeconds, maxSeconds);

    public static RawExportPermitTtlBoundsState Invalid { get; } = new(false, 0, 0);

    public bool Allows(int ttlSeconds) => IsValid && ttlSeconds >= MinSeconds && ttlSeconds <= MaxSeconds;
}

public static class RawExportPermitTtlOptions
{
    public const string SectionName = "TagEkyc:RawExport";
    public const string PermitTtlMinSecondsKey = "PermitTtlMinSeconds";
    public const string PermitTtlMaxSecondsKey = "PermitTtlMaxSeconds";
    public const int DefaultMinSeconds = 60;
    public const int DefaultMaxSeconds = 900;
    public const int AbsoluteMaxSeconds = 3600;

    public static RawExportPermitTtlBoundsState Resolve(string? minSeconds, string? maxSeconds)
    {
        if (string.IsNullOrWhiteSpace(minSeconds) && string.IsNullOrWhiteSpace(maxSeconds))
        {
            return RawExportPermitTtlBoundsState.Valid(DefaultMinSeconds, DefaultMaxSeconds);
        }

        if (string.IsNullOrWhiteSpace(minSeconds) || string.IsNullOrWhiteSpace(maxSeconds))
        {
            return RawExportPermitTtlBoundsState.Invalid;
        }

        if (!int.TryParse(minSeconds, out var min) || !int.TryParse(maxSeconds, out var max))
        {
            return RawExportPermitTtlBoundsState.Invalid;
        }

        if (min <= 0 || max < min || max > AbsoluteMaxSeconds)
        {
            return RawExportPermitTtlBoundsState.Invalid;
        }

        return RawExportPermitTtlBoundsState.Valid(min, max);
    }
}
