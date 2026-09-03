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

        if (!string.Equals(
                state.Profile,
                "Fixture",
                StringComparison.Ordinal))
        {
            throw new RawExportAuthoritySnapshotReadinessException(
                ProfileInvalid);
        }

        if (state.IsProduction)
        {
            throw new RawExportAuthoritySnapshotReadinessException(
                FixtureActive);
        }

        return Task.CompletedTask;
    }
}
