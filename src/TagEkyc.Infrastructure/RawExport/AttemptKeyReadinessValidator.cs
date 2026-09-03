using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawExportAttemptKeyReadinessException(string code)
    : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

internal sealed record RawExportAttemptKeyProfileState(
    string? Profile,
    bool IsProduction)
{
    internal const string ConfigurationPath =
        "TagEkyc:RawExport:AttemptKey:Profile";

    internal static RawExportAttemptKeyProfileState Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new(configuration[ConfigurationPath], isProduction);
    }
}

public sealed class RawExportAttemptKeyReadinessValidator
{
    public const string ProfileMissing =
        "PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_MISSING";
    public const string ProfileInvalid =
        "PROD_RAW_EXPORT_ATTEMPT_KEY_PROFILE_INVALID";
    public const string FixtureActive =
        "PROD_RAW_EXPORT_ATTEMPT_KEY_FIXTURE_ACTIVE";

    private readonly RawExportAttemptKeyProfileState state;

    public RawExportAttemptKeyReadinessValidator(
        IConfiguration configuration,
        bool isProduction)
    {
        state = RawExportAttemptKeyProfileState.Resolve(
            configuration,
            isProduction);
    }

    public Task ValidateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(state.Profile))
        {
            throw new RawExportAttemptKeyReadinessException(ProfileMissing);
        }

        if (!string.Equals(
                state.Profile,
                "Fixture",
                StringComparison.Ordinal))
        {
            throw new RawExportAttemptKeyReadinessException(ProfileInvalid);
        }

        if (state.IsProduction)
        {
            throw new RawExportAttemptKeyReadinessException(FixtureActive);
        }

        return Task.CompletedTask;
    }
}
