using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RawIngressBrokerDatabaseOptions(
    string? ConnectionString,
    bool IsValid)
{
    internal const string InvalidCode = "RAW_INGRESS_BROKER_DATABASE_CONFIGURATION_INVALID";

    internal static RawIngressBrokerDatabaseOptions Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(RawIngressBrokerOptions.SectionName);
        var direct = section["DatabaseConnectionString"];
        var secretRef = section["DatabaseConnectionStringSecretRef"];
        if (!string.IsNullOrWhiteSpace(direct) && !string.IsNullOrWhiteSpace(secretRef))
            return new(null, false);
        string? value = null;
        if (!string.IsNullOrWhiteSpace(secretRef))
        {
            try { value = SecretRefResolver.Resolve(secretRef).Value; }
            catch (SecretRefResolutionException) { return new(null, false); }
        }
        else if (!isProduction && !string.IsNullOrWhiteSpace(direct))
        {
            value = direct;
        }
        if (string.IsNullOrWhiteSpace(value)) return new(null, false);
        try
        {
            var parsed = new NpgsqlConnectionStringBuilder(value);
            var valid = !string.IsNullOrWhiteSpace(parsed.Host)
                && parsed.Port > 0
                && !string.IsNullOrWhiteSpace(parsed.Database)
                && string.Equals(parsed.Username, QualifiedRawIngressBroker.Login,
                    StringComparison.Ordinal)
                && string.IsNullOrEmpty(parsed.Options);
            return new(valid ? value : null, valid);
        }
        catch (ArgumentException)
        {
            return new(null, false);
        }
    }

    public override string ToString() =>
        $"RawIngressBrokerDatabaseOptions {{ IsValid = {IsValid}, Connection = [REDACTED] }}";
}

public static class RawIngressBrokerProductionComposition
{
    public static IServiceCollection AddTagEkycRawIngressBroker(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddTagEkycRawExportSourceClaimComparison(
            configuration, isProduction);
        var transport = RawIngressBrokerOptions.Read(configuration);
        var database = RawIngressBrokerDatabaseOptions.Resolve(configuration, isProduction);
        if (!database.IsValid || string.IsNullOrWhiteSpace(database.ConnectionString))
            throw new InvalidOperationException(RawIngressBrokerDatabaseOptions.InvalidCode);
        services.TryAddSingleton(database);
        services.TryAddSingleton(_ => NpgsqlDataSource.Create(database.ConnectionString));
        services.AddDbContext<TagEkycDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));
        return services.AddTagEkycRawIngressBrokerTransport(transport);
    }
}
