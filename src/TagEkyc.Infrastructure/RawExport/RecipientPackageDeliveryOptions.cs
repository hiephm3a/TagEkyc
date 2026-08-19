using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public enum RecipientPackageDeliveryTopology
{
    Disabled,
    S3CompatibleDurable,
    Invalid,
}

public sealed record RecipientPackageDeliveryOptions(
    RecipientPackageDeliveryTopology Topology,
    RecipientPackageProviderConfiguration? Provider,
    RecipientPackageCredential? DeliveryReader,
    string? DatabaseConnectionString,
    bool IsSyntacticallyValid)
{
    public const string SectionPath = "TagEkyc:RawExport:RecipientPackageDelivery";
    public const int MaximumConcurrentSpools = 2;
    public const long MaximumEncryptedPackageLength = 33_557_106;
    public static readonly TimeSpan AuthorizationLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan StreamOperationLimit = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan StreamLeaseDuration = TimeSpan.FromMinutes(35);

    public static RecipientPackageDeliveryOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionPath);
        var raw = section["Topology"];
        if (raw is null or "" or "Disabled")
        {
            var clean = !section.GetChildren().Any(child => child.Key != "Topology");
            return new(RecipientPackageDeliveryTopology.Disabled, null, null, null, clean);
        }
        if (!string.Equals(raw, "S3CompatibleDurable", StringComparison.Ordinal))
            return new(RecipientPackageDeliveryTopology.Invalid, null, null, null, false);

        var c2 = RecipientPackageOptions.Resolve(configuration);
        var providerId = section["ProviderConfigurationId"];
        var access = section["DeliveryReader:AccessKeyId"];
        var secret = section["DeliveryReader:SecretAccessKey"];
        var database = section["DatabaseConnectionString"];
        var reader = string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret)
            ? null : new RecipientPackageCredential(access, secret);
        var valid = c2.Topology == RecipientPackageTopology.S3CompatibleDurable
            && c2.IsSyntacticallyValid && c2.Provider is not null
            && string.Equals(providerId, c2.Provider.ProviderConfigurationId, StringComparison.Ordinal)
            && reader is not null && !string.IsNullOrWhiteSpace(database)
            && new[]
            {
                c2.Provider.Writer.AccessKeyId, c2.Provider.Reconciler.AccessKeyId,
                c2.Provider.Lifecycle.AccessKeyId, c2.Provider.PostureProbe.AccessKeyId,
                reader?.AccessKeyId,
            }.Distinct(StringComparer.Ordinal).Count() == 5;
        return new(RecipientPackageDeliveryTopology.S3CompatibleDurable,
            valid ? c2.Provider : null, valid ? reader : null, valid ? database : null, valid);
    }

    public override string ToString() =>
        $"RecipientPackageDeliveryOptions {{ Topology = {Topology}, Provider = {Provider?.ProviderConfigurationId ?? "[NONE]"}, Credentials = [REDACTED] }}";
}
