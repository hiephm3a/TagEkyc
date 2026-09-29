using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RecipientPackageDatabaseOptions(
    string? PreparerConnectionString,
    string? ReconcilerConnectionString,
    string? LifecycleConnectionString,
    bool IsValid)
{
    internal const string SectionPath = "TagEkyc:RawExport:RecipientPackage:Database";
    internal const string PreparerLogin = "tagekyc_raw_export_package_preparer_login";
    internal const string ReconcilerLogin = "tagekyc_raw_export_package_reconciler_login";
    internal const string LifecycleLogin = "tagekyc_raw_export_package_lifecycle_login";

    internal static RecipientPackageDatabaseOptions Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionPath);
        var preparer = Connection(section, "Preparer", PreparerLogin, isProduction);
        var reconciler = Connection(section, "Reconciler", ReconcilerLogin, isProduction);
        var lifecycle = Connection(section, "Lifecycle", LifecycleLogin, isProduction);
        var values = new[] { preparer.Value, reconciler.Value, lifecycle.Value };
        var identities = values.Where(value => value is not null)
            .Select(value => Identity(value!)).ToArray();
        var sameDatabase = identities.Length == 3
            && identities.Select(value => (value.Host, value.Port, value.Database))
                .Distinct().Count() == 1
            && identities.Select(value => value.Username).Distinct(StringComparer.Ordinal).Count() == 3;
        return new(preparer.Value, reconciler.Value, lifecycle.Value,
            preparer.Valid && reconciler.Valid && lifecycle.Valid && sameDatabase);
    }

    private static (string? Value, bool Valid) Connection(
        IConfigurationSection section,
        string name,
        string expectedLogin,
        bool isProduction)
    {
        var direct = section[$"{name}ConnectionString"];
        var secretRef = section[$"{name}ConnectionStringSecretRef"];
        if (!string.IsNullOrWhiteSpace(direct) && !string.IsNullOrWhiteSpace(secretRef))
            return (null, false);
        string? value = null;
        if (!string.IsNullOrWhiteSpace(secretRef))
        {
            try { value = SecretRefResolver.Resolve(secretRef).Value; }
            catch (SecretRefResolutionException) { return (null, false); }
        }
        else if (!isProduction && !string.IsNullOrWhiteSpace(direct))
        {
            value = direct;
        }
        if (string.IsNullOrWhiteSpace(value)) return (null, false);
        try
        {
            var identity = Identity(value);
            return string.Equals(identity.Username, expectedLogin, StringComparison.Ordinal)
                ? (value, true)
                : (null, false);
        }
        catch (ArgumentException) { return (null, false); }
    }

    private static DatabaseIdentity Identity(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Host)
            || builder.Port <= 0
            || string.IsNullOrWhiteSpace(builder.Database)
            || string.IsNullOrWhiteSpace(builder.Username)
            || !string.IsNullOrEmpty(builder.Options))
            throw new ArgumentException("Recipient package database identity is invalid.");
        return new(builder.Host, builder.Port, builder.Database, builder.Username);
    }

    public override string ToString() =>
        $"RecipientPackageDatabaseOptions {{ IsValid = {IsValid}, Connections = [REDACTED] }}";

    private sealed record DatabaseIdentity(
        string Host,
        int Port,
        string Database,
        string Username);
}

internal sealed class RecipientPackageConnectionFactory(RecipientPackageDatabaseOptions options)
    : IRecipientPackageConnectionFactory
{
    public async Task<NpgsqlConnection> OpenAsync(
        RecipientPackageDatabaseCapability capability,
        CancellationToken cancellationToken)
    {
        var (connectionString, expectedLogin) = capability switch
        {
            RecipientPackageDatabaseCapability.Preparer =>
                (options.PreparerConnectionString, RecipientPackageDatabaseOptions.PreparerLogin),
            RecipientPackageDatabaseCapability.Reconciler =>
                (options.ReconcilerConnectionString, RecipientPackageDatabaseOptions.ReconcilerLogin),
            RecipientPackageDatabaseCapability.Lifecycle =>
                (options.LifecycleConnectionString, RecipientPackageDatabaseOptions.LifecycleLogin),
            _ => throw new ArgumentOutOfRangeException(nameof(capability)),
        };
        if (!options.IsValid || string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_ROLE_TOPOLOGY_INVALID");
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT session_user, current_user";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                || !string.Equals(reader.GetString(0), expectedLogin, StringComparison.Ordinal)
                || !string.Equals(reader.GetString(1), expectedLogin, StringComparison.Ordinal)
                || await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException(
                    "PROD_RAW_EXPORT_RECIPIENT_PACKAGE_ROLE_TOPOLOGY_INVALID");
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
