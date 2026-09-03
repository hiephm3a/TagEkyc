using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageDeliveryConnectionFactory(RecipientPackageDeliveryOptions options)
    : IRecipientPackageDeliveryConnectionFactory
{
    public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (!options.IsSyntacticallyValid || string.IsNullOrWhiteSpace(options.DatabaseConnectionString))
            throw new InvalidOperationException("PROD_RAW_EXPORT_PACKAGE_DELIVERY_CONFIG_INVALID");
        var connection = new NpgsqlConnection(options.DatabaseConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
