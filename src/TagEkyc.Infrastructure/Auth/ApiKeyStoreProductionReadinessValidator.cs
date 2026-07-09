using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.Auth;

public sealed class ApiKeyStoreProductionReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class ApiKeyStoreProductionReadinessValidator(TagEkycDbContext dbContext, ApiKeyStorePepper pepper)
{
    public const string NoActiveKeys = "PROD_APIKEY_NO_ACTIVE_KEYS";

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pepper);
        var hasActiveKey = await dbContext.ApiKeys
            .AsNoTracking()
            .AnyAsync(row => row.CredentialStatus == "Active", cancellationToken);
        if (!hasActiveKey)
        {
            throw new ApiKeyStoreProductionReadinessException(NoActiveKeys);
        }
    }
}
