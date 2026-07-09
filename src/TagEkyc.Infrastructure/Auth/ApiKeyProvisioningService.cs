using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Auth;

public sealed record ManagedApiKeyMaterial(string PresentedKey, string Prefix);

public interface IManagedApiKeyGenerator
{
    ManagedApiKeyMaterial Generate();
}

public sealed class RandomManagedApiKeyGenerator : IManagedApiKeyGenerator
{
    public ManagedApiKeyMaterial Generate()
    {
        var prefix = Base64Url(RandomNumberGenerator.GetBytes(12))[..ManagedApiKeyConstants.KeyPrefixLength];
        var secret = Base64Url(RandomNumberGenerator.GetBytes(32));
        return new ManagedApiKeyMaterial($"tek_{prefix}_{secret}", prefix);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

public sealed record ApiKeyProvisioningCommand(
    Guid ClientApplicationId,
    AuthenticatedCallerCategory CallerCategory,
    IReadOnlySet<string> Scopes,
    DateTimeOffset? ExpiresAt = null,
    Guid? PrincipalId = null,
    string? CredentialRef = null,
    IReadOnlySet<Guid>? AllowedClientApplicationIds = null,
    IReadOnlySet<string>? AllowedCaptureAgentIds = null);

public sealed record ApiKeyProvisioningResult(
    Guid ApiKeyId,
    Guid ClientApplicationId,
    string KeyPrefix,
    string PresentedKey);

public sealed class ApiKeyProvisioningService(
    TagEkycDbContext dbContext,
    ApiKeyStorePepper pepper,
    ILocalDevClientPolicyProvider policies,
    IManagedApiKeyGenerator keyGenerator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiKeyProvisioningResult> ProvisionAsync(
        ApiKeyProvisioningCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var material = keyGenerator.Generate();
            var parsed = ManagedApiKeyParser.Parse(material.PresentedKey);
            if (parsed is null || parsed.Prefix != material.Prefix)
            {
                throw new InvalidOperationException("APIKEY_PROVISIONING_GENERATOR_INVALID");
            }

            var row = ToRow(command, material);
            dbContext.ApiKeys.Add(row);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return new ApiKeyProvisioningResult(
                    row.ApiKeyId,
                    row.ClientApplicationId,
                    row.KeyPrefix,
                    material.PresentedKey);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.Entry(row).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("APIKEY_PROVISIONING_DUPLICATE_PREFIX_RETRY_EXHAUSTED");
    }

    private async Task ValidateAsync(ApiKeyProvisioningCommand command, CancellationToken cancellationToken)
    {
        if (command.Scopes.Count == 0)
        {
            throw new InvalidOperationException("APIKEY_PROVISIONING_SCOPE_REQUIRED");
        }

        var policy = await policies.GetPolicyAsync(command.ClientApplicationId, cancellationToken);
        if (policy is null || policy.Status == ClientApplicationStatus.Disabled)
        {
            throw new InvalidOperationException("APIKEY_PROVISIONING_CLIENT_POLICY_INVALID");
        }

        if (!command.Scopes.All(scope => policy.AllowedCallerScopes.Contains(scope)))
        {
            throw new InvalidOperationException("APIKEY_PROVISIONING_SCOPE_NOT_ALLOWED");
        }

        if (!ScopesMatchCategory(command.CallerCategory, command.Scopes))
        {
            throw new InvalidOperationException("APIKEY_PROVISIONING_CALLER_CATEGORY_SCOPE_MISMATCH");
        }
    }

    private ApiKeyRow ToRow(ApiKeyProvisioningCommand command, ManagedApiKeyMaterial material)
    {
        var apiKeyId = Guid.NewGuid();
        return new ApiKeyRow
        {
            ApiKeyId = apiKeyId,
            ClientApplicationId = command.ClientApplicationId,
            PrincipalId = command.PrincipalId ?? command.ClientApplicationId,
            CredentialRef = string.IsNullOrWhiteSpace(command.CredentialRef)
                ? $"managed-api-key:{apiKeyId:N}"
                : command.CredentialRef,
            CredentialType = ManagedApiKeyConstants.CredentialType,
            CredentialStatus = "Active",
            KeyPrefix = material.Prefix,
            KeyHash = ApiKeyHasher.Hash(pepper.Value, material.PresentedKey),
            ScopesJson = JsonSerializer.Serialize(command.Scopes.Order(StringComparer.Ordinal), JsonOptions),
            ExpiresAt = command.ExpiresAt,
            CallerCategory = command.CallerCategory.ToString(),
            AllowedClientApplicationIdsJson = command.AllowedClientApplicationIds is null
                ? null
                : JsonSerializer.Serialize(command.AllowedClientApplicationIds.OrderBy(id => id), JsonOptions),
            AllowedCaptureAgentIdsJson = command.AllowedCaptureAgentIds is null
                ? null
                : JsonSerializer.Serialize(command.AllowedCaptureAgentIds.Order(StringComparer.Ordinal), JsonOptions),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static bool ScopesMatchCategory(AuthenticatedCallerCategory callerCategory, IReadOnlySet<string> scopes) =>
        callerCategory switch
        {
            AuthenticatedCallerCategory.BusinessConsumer => scopes.All(scope =>
                scope.StartsWith("business.", StringComparison.Ordinal) ||
                scope.StartsWith("session.", StringComparison.Ordinal)),
            AuthenticatedCallerCategory.CaptureAgent => scopes.All(scope =>
                scope.StartsWith("capture.", StringComparison.Ordinal)),
            AuthenticatedCallerCategory.TrustedAdapter => scopes.All(scope =>
                scope.StartsWith("trusted.", StringComparison.Ordinal)),
            _ => false,
        };

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        var current = exception.InnerException;
        while (current is not null)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
