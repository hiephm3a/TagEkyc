using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawExportCustodyProfileReadinessException(string code)
    : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

internal sealed record RawExportCustodyProfileState(
    string? Profile,
    bool IsProduction)
{
    internal const string ConfigurationPath =
        "TagEkyc:RawExport:CustodyProfile:Profile";

    internal static RawExportCustodyProfileState Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new(configuration[ConfigurationPath], isProduction);
    }
}

internal sealed record CustodyTimeBoundsState(CustodyTimeBounds? Value)
{
    internal const string SafetyMarginKey =
        "RawExportSourceClaimSafetyMarginMilliseconds";
    internal const string MaxRemainingContinuationWindowKey =
        "RawExportSourceMaximumRemainingContinuationWindowSeconds";
    internal const string EncryptionAttemptDeadlineKey =
        "RawExportSourceEncryptionAttemptDeadlineSeconds";
    internal const string OwnershipLeaseDurationKey =
        "RawExportSourceOwnershipLeaseDurationSeconds";

    internal static CustodyTimeBoundsState Resolve(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!TryReadBoundedInteger(
                configuration[SafetyMarginKey],
                minimum: 1,
                maximum: 30_000,
                out var safetyMarginMilliseconds)
            || !TryReadBoundedInteger(
                configuration[MaxRemainingContinuationWindowKey],
                minimum: 1,
                maximum: 3_600,
                out var maxRemainingContinuationWindowSeconds)
            || !TryReadBoundedInteger(
                configuration[EncryptionAttemptDeadlineKey],
                minimum: 1,
                maximum: 3_600,
                out var encryptionAttemptDeadlineSeconds)
            || !TryReadBoundedInteger(
                configuration[OwnershipLeaseDurationKey],
                minimum: 1,
                maximum: 3_600,
                out var ownershipLeaseDurationSeconds))
        {
            return new((CustodyTimeBounds?)null);
        }

        return new(
            new CustodyTimeBounds(
                TimeSpan.FromMilliseconds(safetyMarginMilliseconds),
                TimeSpan.FromSeconds(
                    maxRemainingContinuationWindowSeconds),
                TimeSpan.FromSeconds(encryptionAttemptDeadlineSeconds),
                TimeSpan.FromSeconds(ownershipLeaseDurationSeconds)));
    }

    private static bool TryReadBoundedInteger(
        string? raw,
        int minimum,
        int maximum,
        out int value) =>
        int.TryParse(
            raw,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value)
        && value >= minimum
        && value <= maximum;
}

public sealed class RawExportCustodyProfileReadinessValidator
{
    public const string ProfileMissing =
        "PROD_RAW_EXPORT_CUSTODY_PROFILE_PROFILE_MISSING";
    public const string ProfileInvalid =
        "PROD_RAW_EXPORT_CUSTODY_PROFILE_PROFILE_INVALID";
    public const string FixtureActive =
        "PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE";
    public const string TimeBoundsInvalid =
        "RAW_EXPORT_SOURCE_TIME_BOUNDS_INVALID";

    private readonly RawExportCustodyProfileState profileState;
    private readonly CustodyTimeBoundsState timeBoundsState;

    public RawExportCustodyProfileReadinessValidator(
        IConfiguration configuration,
        bool isProduction)
    {
        profileState = RawExportCustodyProfileState.Resolve(
            configuration,
            isProduction);
        timeBoundsState = CustodyTimeBoundsState.Resolve(configuration);
    }

    public Task ValidateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(profileState.Profile))
        {
            throw new RawExportCustodyProfileReadinessException(
                ProfileMissing);
        }

        if (!string.Equals(
                profileState.Profile,
                "Fixture",
                StringComparison.Ordinal))
        {
            throw new RawExportCustodyProfileReadinessException(
                ProfileInvalid);
        }

        if (profileState.IsProduction)
        {
            throw new RawExportCustodyProfileReadinessException(
                FixtureActive);
        }

        if (timeBoundsState.Value is null)
        {
            throw new RawExportCustodyProfileReadinessException(
                TimeBoundsInvalid);
        }

        return Task.CompletedTask;
    }
}
