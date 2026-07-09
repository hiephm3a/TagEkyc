using Microsoft.Extensions.Options;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.Api;

public interface IReadinessCheck
{
    Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken);
}

public readonly record struct ReadinessIssue(int Order, string Code);

public static class ReadinessEndpoint
{
    public const string GenericFailureCode = "PROD_READINESS_CHECK_FAILED";

    private const int PostureOrder = 0;
    private const int DatabaseOrder = 1;
    private const int ApiKeyOrder = 2;
    private const int SignerOrder = 3;
    private const int GenericOrder = 4;
    private static readonly TimeSpan PerCheckTimeout = TimeSpan.FromSeconds(2);

    public static IEndpointRouteBuilder MapReadinessEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/readiness", CheckAsync);
        return endpoints;
    }

    private static async Task<IResult> CheckAsync(
        HttpContext httpContext,
        IWebHostEnvironment environment,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";

        if (!environment.IsProduction())
        {
            return Results.Ok(new { status = "ready" });
        }

        var issues = new List<ReadinessIssue>();
        await using var scope = scopeFactory.CreateAsyncScope();
        foreach (var check in scope.ServiceProvider.GetServices<IReadinessCheck>())
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(PerCheckTimeout);
                var result = await check.CheckAsync(timeout.Token)
                    .WaitAsync(PerCheckTimeout, cancellationToken);
                issues.AddRange(result);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                issues.Add(new ReadinessIssue(GenericOrder, GenericFailureCode));
            }
            catch (TimeoutException)
            {
                issues.Add(new ReadinessIssue(GenericOrder, GenericFailureCode));
            }
            catch
            {
                issues.Add(new ReadinessIssue(GenericOrder, GenericFailureCode));
            }
        }

        var codes = issues
            .OrderBy(issue => issue.Order)
            .Select(issue => issue.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return codes.Length == 0
            ? Results.Ok(new { status = "ready" })
            : Results.Json(new { status = "not-ready", codes }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    internal static ReadinessIssue PostureIssue(string code) => new(PostureOrder, code);

    internal static ReadinessIssue DatabaseIssue(string code) => new(DatabaseOrder, code);

    internal static ReadinessIssue ApiKeyIssue(string code) => new(ApiKeyOrder, code);

    internal static ReadinessIssue SignerIssue(string code) => new(SignerOrder, code);

    internal static ReadinessIssue GenericIssue() => new(GenericOrder, GenericFailureCode);
}

public sealed class ProductionPostureReadinessCheck(
    IOptions<TagEkycPersistenceOptions> persistence,
    IOptions<ApiKeyStoreOptions> apiKeyStore,
    IOptions<EvidenceSigningOptions> evidenceSigning) : IReadinessCheck
{
    public Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var issues = new List<ReadinessIssue>();

        if (!persistence.Value.IsPostgres)
        {
            issues.Add(persistence.Value.IsInMemory
                ? ReadinessEndpoint.PostureIssue("PROD_PERSISTENCE_INMEMORY_FORBIDDEN")
                : ReadinessEndpoint.GenericIssue());
        }

        if (!string.Equals(apiKeyStore.Value.Backend, ApiKeyStoreBackends.Postgres, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(ReadinessEndpoint.PostureIssue("PROD_APIKEY_LOCALDEV_STORE_FORBIDDEN"));
        }

        if (evidenceSigning.Value.IsLocalDevBackend)
        {
            issues.Add(ReadinessEndpoint.PostureIssue("PROD_SIGNING_LOCALDEV_BACKEND_FORBIDDEN"));
        }
        else if (evidenceSigning.Value.RequireHardwareSigner && !evidenceSigning.Value.IsPkcs11Backend)
        {
            issues.Add(ReadinessEndpoint.GenericIssue());
        }

        return Task.FromResult<IReadOnlyList<ReadinessIssue>>(issues);
    }
}

public sealed class PostgresReadinessCheck(PostgresProductionReadinessValidator validator) : IReadinessCheck
{
    public async Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await validator.ValidateAsync(cancellationToken);
            return [];
        }
        catch (PostgresProductionReadinessException exception)
        {
            return [ReadinessEndpoint.DatabaseIssue(exception.Code)];
        }
    }
}

public sealed class ApiKeyStoreReadinessCheck(ApiKeyStoreProductionReadinessValidator validator) : IReadinessCheck
{
    public async Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await validator.ValidateAsync(cancellationToken);
            return [];
        }
        catch (ApiKeyStoreProductionReadinessException exception)
        {
            return [ReadinessEndpoint.ApiKeyIssue(exception.Code)];
        }
    }
}

public sealed class SignerJwksReadinessCheck(
    IEvidenceSigner evidenceSigner,
    IEs256JwksProvider jwksProvider) : IReadinessCheck
{
    private static readonly HashSet<string> AllowlistedCodes = new(StringComparer.Ordinal)
    {
        "PROD_SIGNING_LOCALDEV_BACKEND_FORBIDDEN",
        "PROD_SIGNING_P12_PASSWORD_PLAINTEXT_FORBIDDEN",
        "PROD_SIGNING_KEY_ID_DEV_FORBIDDEN",
        "PROD_SIGNING_KEY_ID_INVALID",
        "PROD_SIGNING_KEY_ID_REQUIRED",
        "PROD_SIGNING_P12_FILE_NOT_FOUND",
        "PROD_SIGNING_P12_LOAD_FAILED",
        "PROD_SIGNING_P12_PASSWORD_SECRET_REF_INVALID",
        "PROD_SIGNING_P12_PASSWORD_SECRET_REQUIRED",
        "PROD_SIGNING_P12_PASSWORD_SECRET_UNAVAILABLE",
        "PROD_SIGNING_P12_PATH_NOT_ABSOLUTE",
        "PROD_SIGNING_P12_PATH_REQUIRED",
        "PROD_SIGNING_P12_PRIVATE_KEY_REQUIRED",
        "PROD_SIGNING_P12_PUBLIC_KEY_INVALID",
        "PROD_SIGNING_P12_PUBLIC_KEY_REQUIRED",
        "PROD_SIGNING_P12_SELF_TEST_FAILED",
        "JWKS_DUPLICATE_KID",
        "JWKS_PREVIOUS_KEY_MALFORMED",
        "JWKS_PREVIOUS_KEY_VALIDITY_INVALID",
        "JWKS_PREVIOUS_KEY_VALIDITY_REQUIRED",
        "JWKS_PREVIOUS_KEYS_FILE_INVALID",
        "JWKS_PRIVATE_KEY_MATERIAL_FORBIDDEN",
        "JWKS_PUBLIC_KEY_FINGERPRINT_MISMATCH",
        "JWKS_PUBLIC_KEY_INVALID",
    };

    public Task<IReadOnlyList<ReadinessIssue>> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (evidenceSigner is IEvidenceSignerStartupValidator validator)
            {
                validator.ValidateStartup(cancellationToken);
            }

            jwksProvider.ValidateStartup(cancellationToken);
            return Task.FromResult<IReadOnlyList<ReadinessIssue>>([]);
        }
        catch (InvalidOperationException exception) when (AllowlistedCodes.Contains(exception.Message))
        {
            return Task.FromResult<IReadOnlyList<ReadinessIssue>>([ReadinessEndpoint.SignerIssue(exception.Message)]);
        }
    }
}
