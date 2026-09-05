using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportAuthoritySnapshotReadinessException(string code)
    : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed record RawExportAuthoritySnapshotProfileState(
    string? Profile,
    bool IsProduction)
{
    public const string ConfigurationPath =
        "TagEkyc:RawExport:AuthoritySnapshot:Profile";

    public static RawExportAuthoritySnapshotProfileState Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new(configuration[ConfigurationPath], isProduction);
    }
}

public sealed class RawExportAuthoritySnapshotReadinessValidator(
    RawExportAuthoritySnapshotProfileState state)
{
    public const string ProfileMissing =
        "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_MISSING";

    public const string ProfileInvalid =
        "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_INVALID";

    public const string FixtureActive =
        "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE";

    public Task ValidateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(state.Profile))
        {
            throw new RawExportAuthoritySnapshotReadinessException(
                ProfileMissing);
        }

        var isFixture = string.Equals(
            state.Profile,
            "Fixture",
            StringComparison.Ordinal);
        var isProduction = string.Equals(
            state.Profile,
            "Production",
            StringComparison.Ordinal);

        if (!isFixture && !isProduction)
        {
            throw new RawExportAuthoritySnapshotReadinessException(
                ProfileInvalid);
        }

        if (state.IsProduction && isFixture)
        {
            throw new RawExportAuthoritySnapshotReadinessException(
                FixtureActive);
        }

        if (!state.IsProduction && isProduction)
        {
            throw new RawExportAuthoritySnapshotReadinessException(
                ProfileInvalid);
        }

        return Task.CompletedTask;
    }
}
