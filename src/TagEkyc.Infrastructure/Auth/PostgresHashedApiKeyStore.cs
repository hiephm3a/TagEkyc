using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
        var hasActivationScope = scopes.Contains("business.raw-export.package.download")
            || scopes.Contains("business.raw-export.package.references.read");
        if (hasActivationScope)
        {
            if (!scopes.SetEquals(RecipientManagementCodec.ActivationScopes)
                || !string.Equals(row.CallerCategory, "BusinessConsumer", StringComparison.Ordinal))
                return null;

            var expectedDigest = RecipientManagementCodec.ScopeSetDigest(
                RecipientManagementCodec.ActivationScopes);
            try
            {
                var companion = await (
                    from credential in dbContext.RawExportManagedRecipientCredentials.AsNoTracking()
                    join identity in dbContext.RawExportManagedRecipientIdentities.AsNoTracking()
                        on new { credential.RecipientClientApplicationId, credential.PrincipalId }
                        equals new { identity.RecipientClientApplicationId, identity.PrincipalId }
                    join policy in dbContext.RawExportManagedRecipientPolicies.AsNoTracking()
                        on credential.RecipientClientApplicationId equals policy.RecipientClientApplicationId
                    where credential.ApiKeyId == row.ApiKeyId
                        && credential.RecipientClientApplicationId == row.ClientApplicationId
                        && credential.PrincipalId == row.PrincipalId
                        && credential.State == "Active"
                        && identity.State == "Active"
                        && policy.State == "Active"
                        && policy.ActivationProfile == "C3C4RecipientV1"
                    select policy.ActivationScopesDigest)
                    .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (companion is null
                    || !ApiKeyHasher.FixedTimeEquals(companion, expectedDigest)) return null;
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
