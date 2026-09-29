using System.Globalization;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record CaptureRuntimeRawIngressProductionOptions(
    CaptureRuntimeRawIngressComposition.RuntimeOwners? Owners,
    bool IsConfigured)
{
    internal const string SectionPath = "TagEkyc:RawExport:RuntimeOwners";
    internal const string InvalidCode = "CAPTURE_RUNTIME_RAW_INGRESS_OWNER_CONFIGURATION_INVALID";

    internal static CaptureRuntimeRawIngressProductionOptions Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionPath);
        if (!section.GetChildren().Any())
            return new(null, false);

        try
        {
            var writer = Owner(section.GetSection("Writer"),
                ProvisionalObjectCapability.Writer,
                CaptureRuntimeCustodyProviderScopes.WriterLogin,
                isProduction);
            var reconciler = Owner(section.GetSection("Reconciler"),
                ProvisionalObjectCapability.Reconciler,
                CaptureRuntimeCustodyProviderScopes.ReconcilerLogin,
                isProduction);
            var lifecycle = Owner(section.GetSection("Lifecycle"),
                ProvisionalObjectCapability.Lifecycle,
                CaptureRuntimeCustodyProviderScopes.LifecycleLogin,
                isProduction);
            var maximumText = configuration[
                "TagEkyc:RawExport:RawExportCustodyMaximumPlaintextWindowBytesPerStream"];
            if (!long.TryParse(maximumText, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var maximum) || maximum <= 0)
                throw Invalid();

            return new(new(
                writer.ConnectionString, writer.Object,
                reconciler.ConnectionString, reconciler.Object,
                lifecycle.ConnectionString, lifecycle.Object,
                maximum), true);
        }
        catch (Exception exception) when (exception is not InvalidOperationException
            || exception.Message != InvalidCode)
        {
            throw Invalid(exception);
        }
    }

    private static OwnerConfiguration Owner(
        IConfigurationSection section,
        ProvisionalObjectCapability capability,
        string expectedLogin,
        bool isProduction)
    {
        var connection = Connection(
            section["DatabaseConnectionString"],
            section["DatabaseConnectionStringSecretRef"],
            isProduction);
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (!string.Equals(parsed.Username, expectedLogin, StringComparison.Ordinal)
            || !string.IsNullOrEmpty(parsed.Options))
            throw Invalid();

        var objectOptions = ProvisionalObjectCustodyOptions.Resolve(
            section.GetSection("ObjectCustody"));
        if (!objectOptions.IsSyntacticallyValid
            || objectOptions.Topology != ProvisionalObjectTopology.S3CompatibleDurable
            || objectOptions.Capability != capability)
            throw Invalid();
        return new(connection, objectOptions);
    }

    private static string Connection(string? direct, string? secretRef, bool isProduction)
    {
        if (!string.IsNullOrWhiteSpace(direct) && !string.IsNullOrWhiteSpace(secretRef))
            throw Invalid();
        if (isProduction && !string.IsNullOrWhiteSpace(direct))
            throw Invalid();
        if (!string.IsNullOrWhiteSpace(secretRef))
        {
            try
            {
                var resolved = SecretRefResolver.Resolve(secretRef).Value;
                return string.IsNullOrWhiteSpace(resolved) ? throw Invalid() : resolved;
            }
            catch (SecretRefResolutionException exception)
            {
                throw Invalid(exception);
            }
        }
        return !isProduction && !string.IsNullOrWhiteSpace(direct)
            ? direct
            : throw Invalid();
    }

    private static InvalidOperationException Invalid(Exception? inner = null) =>
        new(InvalidCode, inner);

    private sealed record OwnerConfiguration(
        string ConnectionString,
        ProvisionalObjectCustodyOptions Object);
}
