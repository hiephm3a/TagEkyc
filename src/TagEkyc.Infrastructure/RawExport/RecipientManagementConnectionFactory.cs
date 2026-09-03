using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientManagementConnectionFactory(RecipientManagementOptions options)
    : IRecipientManagementConnectionFactory
{
    public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (options.Topology != RecipientManagementTopology.PostgresDurable
            || !options.IsSyntacticallyValid
            || string.IsNullOrWhiteSpace(options.DatabaseConnectionString))
            throw new InvalidOperationException(
                "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CONFIG_INVALID");

        var connection = new NpgsqlConnection(options.DatabaseConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT current_user";
            var currentUser = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(currentUser, RecipientManagementOptions.ManagerLogin, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID");
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal static RecipientManagementOptions Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(RecipientManagementOptions.SectionPath);
        var topology = section["Topology"];
        if (topology is null or "" or "Disabled")
        {
            var clean = section.GetChildren().All(child => child.Key == "Topology");
            return new(RecipientManagementTopology.Disabled, null, clean);
        }
        if (!string.Equals(topology, "PostgresDurable", StringComparison.Ordinal)) return Invalid();

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Topology", "DatabaseConnectionString", "DatabaseConnectionStringSecretRef",
        };
        if (section.GetChildren().Any(child => !allowed.Contains(child.Key))) return Invalid();

        var plain = section["DatabaseConnectionString"];
        var secretRef = section["DatabaseConnectionStringSecretRef"];
        if (isProduction && !string.IsNullOrWhiteSpace(plain)) return Invalid();
        if (!string.IsNullOrWhiteSpace(secretRef))
        {
            try
            {
                var secret = SecretRefResolver.Resolve(secretRef).Value;
                return string.IsNullOrWhiteSpace(secret)
                    ? Invalid()
                    : new(RecipientManagementTopology.PostgresDurable, secret, true);
            }
            catch (SecretRefResolutionException)
            {
                return Invalid();
            }
        }
        if (isProduction || string.IsNullOrWhiteSpace(plain)) return Invalid();
        return new(RecipientManagementTopology.PostgresDurable, plain, true);
    }

    private static RecipientManagementOptions Invalid() =>
        new(RecipientManagementTopology.Invalid, null, false);
}
