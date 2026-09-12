using Npgsql;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public static class CaptureRuntimeDatabaseOptionsValidator
{
    public static CaptureRuntimeResolvedDatabaseOptions Resolve(
        CaptureRuntimeDatabaseOptions options,
        string ordinaryClientConnectionString,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(ordinaryClientConnectionString))
        {
            throw new InvalidOperationException("CAPTURE_RUNTIME_CLIENT_DATABASE_CONNECTION_REQUIRED");
        }

        var online = ResolveOne(
            options.OnlineConnectionString,
            options.OnlineConnectionStringSecretRef,
            isProduction,
            "CAPTURE_RUNTIME_ONLINE_DATABASE_CONFIGURATION_INVALID");
        var operatorConnection = ResolveOne(
            options.OperatorConnectionString,
            options.OperatorConnectionStringSecretRef,
            isProduction,
            "CAPTURE_RUNTIME_OPERATOR_DATABASE_CONFIGURATION_INVALID");

        var clientIdentity = ParseIdentity(ordinaryClientConnectionString);
        var onlineIdentity = ParseIdentity(online);
        var operatorIdentity = ParseIdentity(operatorConnection);
        if (clientIdentity == onlineIdentity || clientIdentity == operatorIdentity ||
            onlineIdentity == operatorIdentity)
        {
            throw new InvalidOperationException("CAPTURE_RUNTIME_DATABASE_IDENTITIES_MUST_BE_DISTINCT");
        }

        return new CaptureRuntimeResolvedDatabaseOptions(online, operatorConnection);
    }

    private static string ResolveOne(
        string? direct,
        string? secretRef,
        bool isProduction,
        string errorCode)
    {
        if (isProduction && !string.IsNullOrWhiteSpace(direct))
        {
            throw new InvalidOperationException(errorCode);
        }

        if (!string.IsNullOrWhiteSpace(secretRef))
        {
            if (!string.IsNullOrWhiteSpace(direct))
            {
                throw new InvalidOperationException(errorCode);
            }

            try
            {
                var resolved = SecretRefResolver.Resolve(secretRef).Value;
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    _ = ParseIdentity(resolved);
                    return resolved;
                }
            }
            catch (SecretRefResolutionException)
            {
            }

            throw new InvalidOperationException(errorCode);
        }

        if (isProduction || string.IsNullOrWhiteSpace(direct))
        {
            throw new InvalidOperationException(errorCode);
        }

        _ = ParseIdentity(direct);
        return direct;
    }

    private static DatabaseIdentity ParseIdentity(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.Host) || builder.Port <= 0 ||
                string.IsNullOrWhiteSpace(builder.Database) || string.IsNullOrWhiteSpace(builder.Username))
            {
                throw new InvalidOperationException("CAPTURE_RUNTIME_DATABASE_IDENTITY_INVALID");
            }

            return new DatabaseIdentity(builder.Host, builder.Port, builder.Database, builder.Username);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("CAPTURE_RUNTIME_DATABASE_IDENTITY_INVALID", exception);
        }
    }

    private sealed record DatabaseIdentity(string Host, int Port, string Database, string Username)
    {
        public bool Equals(DatabaseIdentity? other) =>
            other is not null && Port == other.Port &&
            string.Equals(Host, other.Host, StringComparison.Ordinal) &&
            string.Equals(Database, other.Database, StringComparison.Ordinal) &&
            string.Equals(Username, other.Username, StringComparison.Ordinal);

        public override int GetHashCode() => HashCode.Combine(Host, Port, Database, Username);
    }
}

public sealed record CaptureRuntimeResolvedDatabaseOptions(
    string OnlineConnectionString,
    string OperatorConnectionString);
