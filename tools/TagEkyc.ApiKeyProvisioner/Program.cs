using Microsoft.EntityFrameworkCore;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Secrets;

if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Length == 0)
{
    Usage();
    return 1;
}

try
{
    var parsed = ParseArgs(args);
    var connectionString = SecretRefResolver.Resolve(Require(parsed, "--connection-string-secret-ref")).Value;
    var pepper = ApiKeyStorePepperResolver.Resolve(Require(parsed, "--pepper-secret-ref"));
    var options = new DbContextOptionsBuilder<TagEkycDbContext>()
        .UseNpgsql(connectionString)
        .Options;

    await using var db = new TagEkycDbContext(options);
    var service = new ApiKeyProvisioningService(
        db,
        pepper,
        new LocalDevRuntimePolicySource(),
        new RandomManagedApiKeyGenerator());
    var result = await service.ProvisionAsync(new ApiKeyProvisioningCommand(
        Guid.Parse(Require(parsed, "--client-application-id")),
        Enum.Parse<AuthenticatedCallerCategory>(Require(parsed, "--caller-category"), ignoreCase: false),
        Require(parsed, "--scopes").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal),
        TryParseDate(parsed.GetValueOrDefault("--expires-at")),
        TryParseGuid(parsed.GetValueOrDefault("--principal-id")),
        parsed.GetValueOrDefault("--credential-ref")));

    Console.WriteLine($"apiKeyId={result.ApiKeyId}");
    Console.WriteLine($"clientApplicationId={result.ClientApplicationId}");
    Console.WriteLine($"keyPrefix={result.KeyPrefix}");
    Console.WriteLine($"apiKey={result.PresentedKey}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var index = 0; index < args.Length; index += 2)
    {
        if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid argument list.");
        }

        parsed[args[index]] = args[index + 1];
    }

    return parsed;
}

static string Require(IReadOnlyDictionary<string, string> parsed, string key) =>
    parsed.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new InvalidOperationException($"Missing required argument {key}.");

static DateTimeOffset? TryParseDate(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : DateTimeOffset.Parse(value);

static Guid? TryParseGuid(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);

static void Usage()
{
    Console.Error.WriteLine("Usage: TagEkyc.ApiKeyProvisioner --connection-string-secret-ref env:DB --pepper-secret-ref env:PEPPER --client-application-id <guid> --caller-category BusinessConsumer --scopes business.session.create,session.complete");
}
