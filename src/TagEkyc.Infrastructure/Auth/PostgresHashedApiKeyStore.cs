using System.Text.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Infrastructure.Auth;

public sealed class PostgresHashedApiKeyStore(TagEkycDbContext dbContext, ApiKeyStorePepper pepper) : IApiKeyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ResolvedApiKey?> FindByPresentedKeyAsync(
        string presentedApiKey,
        CancellationToken cancellationToken = default)
    {
        var parsed = ManagedApiKeyParser.Parse(presentedApiKey);
        if (parsed is null)
        {
            return null;
        }

        var row = await dbContext.ApiKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.KeyPrefix == parsed.Prefix, cancellationToken);
        if (row is null || row.KeyHash.Length != ManagedApiKeyConstants.HashLength)
        {
            return null;
        }

        var presentedHash = ApiKeyHasher.Hash(pepper.Value, presentedApiKey);
        if (!ApiKeyHasher.FixedTimeEquals(presentedHash, row.KeyHash))
        {
            return null;
        }

        var scopes = DeserializeSet<string>(row.ScopesJson);
        var hasActivationScope = scopes.Overlaps(RecipientManagementCodec.DownloadOnlyScopes)
            || scopes.Overlaps(RecipientManagementCodec.DeliveryOperatorScopes);
        if (hasActivationScope)
        {
            if (!RecipientManagementCodec.TryResolveCredentialProfile(
                    scopes, out var activationProfile, out var activationScopes)
                || !string.Equals(row.CallerCategory, "BusinessConsumer", StringComparison.Ordinal))
                return null;

            var expectedDigest = RecipientManagementCodec.ScopeSetDigest(
                activationScopes!);
            try
            {
                var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var command = new NpgsqlCommand(
                    "SELECT tagekyc.raw_export_managed_recipient_scope_matches($1,$2,$3,$4,$5)",
                    connection);
                command.Parameters.AddWithValue(row.ApiKeyId);
                command.Parameters.AddWithValue(row.ClientApplicationId);
                command.Parameters.AddWithValue(row.PrincipalId);
                command.Parameters.AddWithValue(activationProfile!);
                command.Parameters.AddWithValue(expectedDigest);
                if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
                    return null;
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(expectedDigest);
            }
        }

        return ToResolved(row, scopes);
    }

    private static ResolvedApiKey ToResolved(ApiKeyRow row, IReadOnlySet<string> scopes)
    {
        var status = MapStatus(row.CredentialStatus);
        if (!Enum.TryParse<AuthenticatedCallerCategory>(row.CallerCategory, ignoreCase: false, out var category))
        {
            status = ApiKeyStatus.Revoked;
            category = AuthenticatedCallerCategory.BusinessConsumer;
        }

        return new(
            row.ApiKeyId,
            row.ClientApplicationId,
            row.KeyPrefix,
            scopes,
            status,
            row.ExpiresAt,
            category,
            DeserializeNullableSet<Guid>(row.AllowedClientApplicationIdsJson),
            DeserializeNullableSet<string>(row.AllowedCaptureAgentIdsJson),
            row.PrincipalId);
    }

    private static ApiKeyStatus MapStatus(string status) =>
        string.Equals(status, "Active", StringComparison.Ordinal)
            ? ApiKeyStatus.Active
            : string.Equals(status, "Expired", StringComparison.Ordinal)
                ? ApiKeyStatus.Expired
                : ApiKeyStatus.Revoked;

    private static IReadOnlySet<T> DeserializeSet<T>(string json) =>
        new HashSet<T>(JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions) ?? []);

    private static IReadOnlySet<T>? DeserializeNullableSet<T>(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : new HashSet<T>(JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions) ?? []);
}
