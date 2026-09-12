using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using System.Security.Cryptography;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.LocalDev;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Secrets;

if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Length == 0)
{
    Usage();
    return 1;
}

try
{
    if (args.Length >= 2 && string.Equals(args[0], "platform-operator", StringComparison.Ordinal)
        && string.Equals(args[1], "provision", StringComparison.Ordinal))
    {
        return await RunPlatformCommandAsync(() => ProvisionPlatformOperatorAsync(ParseArgs(args[2..])));
    }

    if (args.Length >= 2 && string.Equals(args[0], "platform-operator", StringComparison.Ordinal)
        && string.Equals(args[1], "revoke", StringComparison.Ordinal))
    {
        return await RunPlatformCommandAsync(() => RevokePlatformOperatorAsync(ParseArgs(args[2..])));
    }

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

static async Task<int> RunPlatformCommandAsync(Func<Task<int>> command)
{
    try
    {
        return await command();
    }
    catch (SecretRefResolutionException exception)
    {
        Console.Error.WriteLine("Capture Runtime operator secret reference could not be resolved.");
        return exception.ErrorKind == SecretRefErrorKind.Missing ? 40 : 30;
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
    {
        Console.Error.WriteLine("Invalid Capture Runtime platform-operator command.");
        return 30;
    }
}

static async Task<int> ProvisionPlatformOperatorAsync(IReadOnlyDictionary<string, string> parsed)
{
    RequireExactKeys(parsed,
        "--operation-id",
        "--principal-id",
        "--expires-at",
        "--capture-runtime-verifier-pepper-version",
        "--capture-runtime-verifier-pepper-secret-ref",
        "--connection-string-secret-ref");
    var operationId = ParseRequiredGuid(parsed, "--operation-id");
    var principalId = ParseRequiredGuid(parsed, "--principal-id");
    var expiresAtUtc = ParseRequiredUtc(parsed, "--expires-at");
    var pepperVersion = ParseRequiredPositiveInt(parsed, "--capture-runtime-verifier-pepper-version");
    var pepperReference = Require(parsed, "--capture-runtime-verifier-pepper-secret-ref");
    var resolution = CaptureRuntimeVerifierPepperSecretRefResolver.Resolve(pepperReference);
    if (resolution.Status != CaptureRuntimeVerifierPepperSecretRefStatus.Success)
    {
        Console.Error.WriteLine("Capture Runtime verifier pepper could not be resolved.");
        return resolution.Status == CaptureRuntimeVerifierPepperSecretRefStatus.MaterialUnavailable ? 40 : 30;
    }
    using var material = resolution.Material!;

    var master = new byte[CaptureRuntimeVerifierCryptography.SecretByteLength];
    if (!CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(material.CanonicalAscii.Span, master))
    {
        throw new InvalidOperationException("Capture Runtime verifier pepper material is invalid.");
    }

    var secret = RandomNumberGenerator.GetBytes(CaptureRuntimeVerifierCryptography.SecretByteLength);
    var domainKey = CaptureRuntimeVerifierCryptography.DeriveDomainKey(
        master, CaptureRuntimeVerifierPepperDomain.PlatformCredential);
    var digest = CaptureRuntimeVerifierCryptography.ComputeDigest(
        domainKey, CaptureRuntimeVerifierPepperDomain.PlatformCredential, secret);
    var payload = new char[CaptureRuntimeVerifierCryptography.CanonicalTextLength];
    CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret, payload);
    var prefix = new string(payload, 0, 12);
    var fingerprint = CaptureRuntimeVerifierCryptography.ComputePlatformProvisionRequestFingerprint(
        operationId, principalId, expiresAtUtc);

    try
    {
        await using var connection = new NpgsqlConnection(ResolveConnectionString(parsed));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.c6ba_root_provision_platform_credential($1,$2,$3,$4,$5,$6,$7,$8)",
            connection, transaction);
        command.Parameters.AddWithValue(operationId);
        command.Parameters.AddWithValue(principalId);
        command.Parameters.AddWithValue(expiresAtUtc);
        command.Parameters.AddWithValue(prefix);
        command.Parameters.AddWithValue(digest);
        command.Parameters.AddWithValue(pepperVersion);
        command.Parameters.AddWithValue(fingerprint);
        command.Parameters.AddWithValue(DateTimeOffset.UtcNow);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return 40;
        }

        var resultCode = reader.GetString(0);
        var credentialId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
        var keyLookupPrefix = reader.IsDBNull(2) ? null : reader.GetString(2);
        var secretAvailable = reader.IsDBNull(3) ? (bool?)null : reader.GetBoolean(3);
        if (await reader.ReadAsync()) return 40;
        await reader.DisposeAsync();
        // No output, including the one-time secret, precedes the actual commit.
        // Deferred constraints can still reject a function's returned Created row.
        await transaction.CommitAsync();
        Console.WriteLine($"result={resultCode}");
        if (string.Equals(resultCode, "Created", StringComparison.Ordinal))
        {
            if (credentialId is null || keyLookupPrefix is null || secretAvailable != true)
            {
                return 40;
            }

            Console.WriteLine($"credentialId={credentialId:D}");
            Console.WriteLine($"keyLookupPrefix={keyLookupPrefix}");
            Console.Write("platformOperatorKey=teo_");
            Console.Out.Write(payload);
            Console.WriteLine();
            return 0;
        }

        if (string.Equals(resultCode, "ExistingMatchSecretUnavailable", StringComparison.Ordinal))
        {
            if (credentialId is null || keyLookupPrefix is null || secretAvailable != false)
            {
                return 40;
            }

            Console.WriteLine($"credentialId={credentialId:D}");
            Console.WriteLine($"keyLookupPrefix={keyLookupPrefix}");
            return 10;
        }

        return resultCode switch
        {
            "Conflict" => 20,
            "InvalidInput" => 30,
            _ => 40
        };
    }
    catch (NpgsqlException)
    {
        Console.Error.WriteLine("Capture Runtime operator database is not ready.");
        return 40;
    }
    finally
    {
        CryptographicOperations.ZeroMemory(master);
        CryptographicOperations.ZeroMemory(secret);
        CryptographicOperations.ZeroMemory(domainKey);
        CryptographicOperations.ZeroMemory(digest);
        CryptographicOperations.ZeroMemory(fingerprint);
        Array.Clear(payload);
    }
}

static async Task<int> RevokePlatformOperatorAsync(IReadOnlyDictionary<string, string> parsed)
{
    RequireExactKeys(parsed,
        "--operation-id",
        "--credential-id",
        "--expected-revision",
        "--connection-string-secret-ref");
    var operationId = ParseRequiredGuid(parsed, "--operation-id");
    var credentialId = ParseRequiredGuid(parsed, "--credential-id");
    var expectedRevision = ParseRequiredPositiveLong(parsed, "--expected-revision");
    var fingerprint = CaptureRuntimeVerifierCryptography.ComputePlatformRevokeRequestFingerprint(
        operationId, credentialId, expectedRevision);
    try
    {
        await using var connection = new NpgsqlConnection(ResolveConnectionString(parsed));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.c6ba_root_revoke_platform_credential($1,$2,$3,$4,$5,$6)",
            connection);
        await using var transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction;
        command.Parameters.AddWithValue(operationId);
        command.Parameters.AddWithValue(credentialId);
        command.Parameters.AddWithValue(expectedRevision);
        command.Parameters.AddWithValue("DeploymentRevocation");
        command.Parameters.AddWithValue(fingerprint);
        command.Parameters.AddWithValue(DateTimeOffset.UtcNow);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return 40;
        }

        var resultCode = reader.GetString(0);
        if (await reader.ReadAsync()) return 40;
        await reader.DisposeAsync();
        await transaction.CommitAsync();
        Console.WriteLine($"result={resultCode}");
        return resultCode switch
        {
            "Revoked" => 0,
            "Conflict" => 20,
            "InvalidInput" => 30,
            "ResourceNotAvailable" => 50,
            _ => 40
        };
    }
    catch (NpgsqlException)
    {
        Console.Error.WriteLine("Capture Runtime operator database is not ready.");
        return 40;
    }
    finally
    {
        CryptographicOperations.ZeroMemory(fingerprint);
    }
}

static string ResolveConnectionString(IReadOnlyDictionary<string, string> parsed) =>
    SecretRefResolver.Resolve(Require(parsed, "--connection-string-secret-ref")).Value;

static Guid ParseRequiredGuid(IReadOnlyDictionary<string, string> parsed, string key)
{
    var text = Require(parsed, key);
    return Guid.TryParseExact(text, "N", out var value) && value != Guid.Empty
        && string.Equals(value.ToString("N", CultureInfo.InvariantCulture), text, StringComparison.Ordinal)
            ? value
            : throw new InvalidOperationException($"Invalid required argument {key}.");
}

static DateTimeOffset ParseRequiredUtc(IReadOnlyDictionary<string, string> parsed, string key)
{
    var value = DateTimeOffset.ParseExact(Require(parsed, key), "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    return value;
}

static int ParseRequiredPositiveInt(IReadOnlyDictionary<string, string> parsed, string key)
{
    var text = Require(parsed, key);
    return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
        && string.Equals(value.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal)
            ? value
            : throw new InvalidOperationException($"Invalid required argument {key}.");
}

static long ParseRequiredPositiveLong(IReadOnlyDictionary<string, string> parsed, string key)
{
    var text = Require(parsed, key);
    return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
        && string.Equals(value.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal)
            ? value
            : throw new InvalidOperationException($"Invalid required argument {key}.");
}

static void RequireExactKeys(IReadOnlyDictionary<string, string> parsed, params string[] expectedKeys)
{
    if (parsed.Count != expectedKeys.Length || expectedKeys.Any(key => !parsed.ContainsKey(key)))
    {
        throw new InvalidOperationException("Unexpected Capture Runtime platform-operator argument.");
    }
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

        if (!parsed.TryAdd(args[index], args[index + 1]))
        {
            throw new InvalidOperationException($"Duplicate argument {args[index]}.");
        }
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
    Console.Error.WriteLine("       TagEkyc.ApiKeyProvisioner platform-operator provision --connection-string-secret-ref env:DB --operation-id <uuid-n> --principal-id <uuid-n> --expires-at <utc-timestamp> --capture-runtime-verifier-pepper-version <positive-int> --capture-runtime-verifier-pepper-secret-ref <secret-ref>");
    Console.Error.WriteLine("       TagEkyc.ApiKeyProvisioner platform-operator revoke --connection-string-secret-ref env:DB --operation-id <uuid-n> --credential-id <uuid-n> --expected-revision <positive-int>");
}
