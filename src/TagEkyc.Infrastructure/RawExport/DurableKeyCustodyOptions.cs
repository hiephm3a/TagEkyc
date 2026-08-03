using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public sealed record DurableKeyCustodyOptions(
    TimeSpan PreparationLeaseDuration,
    TimeSpan ResolutionInitialRetryDelay,
    double ResolutionRetryMultiplier,
    TimeSpan ResolutionMaxBackoff,
    TimeSpan ResolutionDeadline,
    long ResolutionMaxAttemptCount,
    TimeSpan CleanupInitialRetryDelay,
    double CleanupRetryMultiplier,
    TimeSpan CleanupMaxBackoff,
    TimeSpan CleanupDeadline,
    TimeSpan BoundedAeadOperationDuration,
    string? CsprngExpectedOwner,
    bool ConfigurationConformsToFixedProfile)
{
    public const string SectionPath = "TagEkyc:RawExport:AttemptKey";
    public const string CsprngExpectedOwnerPath = SectionPath + ":CsprngExpectedOwner";

    public static readonly TimeSpan FixedPreparationLeaseDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan FixedResolutionInitialRetryDelay = TimeSpan.FromSeconds(30);
    public const double FixedResolutionRetryMultiplier = 2.0;
    public static readonly TimeSpan FixedResolutionMaxBackoff = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan FixedResolutionDeadline = TimeSpan.FromHours(24);
    public const long FixedResolutionMaxAttemptCount = 0;
    public static readonly TimeSpan FixedCleanupInitialRetryDelay = TimeSpan.FromMinutes(1);
    public const double FixedCleanupRetryMultiplier = 2.0;
    public static readonly TimeSpan FixedCleanupMaxBackoff = TimeSpan.FromHours(1);
    public static readonly TimeSpan FixedCleanupDeadline = TimeSpan.FromDays(7);
    public static readonly TimeSpan FixedBoundedAeadOperationDuration = TimeSpan.FromSeconds(30);

    public static DurableKeyCustodyOptions Resolve(IConfiguration configuration)
    {
        var conforms =
            MatchesTime(configuration, "PreparationLeaseDuration", FixedPreparationLeaseDuration)
            && MatchesTime(configuration, "ResolutionInitialRetryDelay", FixedResolutionInitialRetryDelay)
            && MatchesDouble(configuration, "ResolutionRetryMultiplier", FixedResolutionRetryMultiplier)
            && MatchesTime(configuration, "ResolutionMaxBackoff", FixedResolutionMaxBackoff)
            && MatchesTime(configuration, "ResolutionDeadline", FixedResolutionDeadline)
            && MatchesLong(configuration, "ResolutionMaxAttemptCount", FixedResolutionMaxAttemptCount)
            && MatchesTime(configuration, "CleanupInitialRetryDelay", FixedCleanupInitialRetryDelay)
            && MatchesDouble(configuration, "CleanupRetryMultiplier", FixedCleanupRetryMultiplier)
            && MatchesTime(configuration, "CleanupMaxBackoff", FixedCleanupMaxBackoff)
            && MatchesTime(configuration, "CleanupDeadline", FixedCleanupDeadline)
            && MatchesTime(configuration, "BoundedAeadOperationDuration", FixedBoundedAeadOperationDuration);

        return new(
            FixedPreparationLeaseDuration,
            FixedResolutionInitialRetryDelay,
            FixedResolutionRetryMultiplier,
            FixedResolutionMaxBackoff,
            FixedResolutionDeadline,
            FixedResolutionMaxAttemptCount,
            FixedCleanupInitialRetryDelay,
            FixedCleanupRetryMultiplier,
            FixedCleanupMaxBackoff,
            FixedCleanupDeadline,
            FixedBoundedAeadOperationDuration,
            configuration[CsprngExpectedOwnerPath]?.Trim(),
            conforms);
    }

    private static string Key(string name) => $"{SectionPath}:{name}";
    private static bool MatchesTime(IConfiguration configuration, string name, TimeSpan fixedValue)
    {
        var raw = configuration[Key(name)];
        return raw is null
            || TimeSpan.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var value)
            && value == fixedValue;
    }

    private static bool MatchesDouble(IConfiguration configuration, string name, double fixedValue)
    {
        var raw = configuration[Key(name)];
        return raw is null
            || double.TryParse(
                raw,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
            && value.Equals(fixedValue);
    }

    private static bool MatchesLong(IConfiguration configuration, string name, long fixedValue)
    {
        var raw = configuration[Key(name)];
        return raw is null
            || long.TryParse(
                raw,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
            && value == fixedValue;
    }

    public bool IsValid => ConfigurationConformsToFixedProfile;
}

internal static class DurableKeyText
{
    internal static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (!string.Equals(trimmed, value, StringComparison.Ordinal)) return false;
        if (!string.Equals(value.Normalize(), value, StringComparison.Ordinal)) return false;
        if (System.Text.Encoding.UTF8.GetByteCount(value) > 512) return false;
        return value.All(character => character > '\u001f' && character != '\u007f');
    }
}
