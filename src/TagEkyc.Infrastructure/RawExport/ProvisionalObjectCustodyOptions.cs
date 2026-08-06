using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public enum ProvisionalObjectTopology { Disabled, S3CompatibleDurable }

public sealed record ProvisionalObjectCustodyOptions(
    ProvisionalObjectTopology Topology,
    ProvisionalObjectCapability? Capability,
    Uri? ServiceUrl,
    string? BucketName,
    string? AccessKeyId,
    string? SecretAccessKey,
    bool AllowLoopbackHttp,
    long MaximumSinglePartCiphertextBytes,
    TimeSpan OperationTimeout,
    bool IsSyntacticallyValid)
{
    internal const string SectionPath = "TagEkyc:RawExport:ObjectCustody";
    internal const long FixedMaximumSinglePartCiphertextBytes = 134_217_728;
    internal static readonly TimeSpan FixedOperationTimeout = TimeSpan.FromSeconds(300);

    // Preserve the public constructor while retaining the exact validation
    // category selected by Resolve. Manually-constructed options keep the
    // historical aggregate behavior through these defaults.
    internal bool TopologyConfigurationValid { get; init; } = IsSyntacticallyValid;
    internal bool LimitsConfigurationValid { get; init; } = IsSyntacticallyValid;
    internal bool EndpointConfigurationValid { get; init; } = IsSyntacticallyValid;
    internal bool CredentialConfigurationValid { get; init; } = IsSyntacticallyValid;

    internal static ProvisionalObjectCustodyOptions Resolve(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionPath);
        var topologyRaw = section["Topology"];
        var capabilityRaw = section["Capability"];
        var topologyValid = Enum.TryParse<ProvisionalObjectTopology>(topologyRaw, false, out var topology)
            && Enum.IsDefined(topology);
        var capabilityValid = Enum.TryParse<ProvisionalObjectCapability>(capabilityRaw, false, out var capability)
            && Enum.IsDefined(capability);
        var serviceUrlValid = Uri.TryCreate(section["ServiceUrl"], UriKind.Absolute, out var serviceUrl);
        var allowLoopbackValid = bool.TryParse(section["AllowLoopbackHttp"], out var allowLoopback);
        var limitValid = long.TryParse(section["MaximumSinglePartCiphertextBytes"], out var limit)
            && limit == FixedMaximumSinglePartCiphertextBytes;
        var timeoutValid = int.TryParse(section["OperationTimeoutSeconds"], out var timeoutSeconds)
            && timeoutSeconds == (int)FixedOperationTimeout.TotalSeconds;
        var bucket = section["BucketName"];
        var access = section["AccessKeyId"];
        var secret = section["SecretAccessKey"];

        if (topologyValid && topology == ProvisionalObjectTopology.Disabled)
        {
            var limitsAbsent = section["MaximumSinglePartCiphertextBytes"] is null
                && section["OperationTimeoutSeconds"] is null;
            var endpointAbsent = section["ServiceUrl"] is null && bucket is null
                && section["AllowLoopbackHttp"] is null;
            var credentialsAbsent = capabilityRaw is null && access is null && secret is null;
            return new(topology, null, null, null, null, null, false,
                FixedMaximumSinglePartCiphertextBytes, FixedOperationTimeout,
                limitsAbsent && endpointAbsent && credentialsAbsent)
            {
                TopologyConfigurationValid = true,
                LimitsConfigurationValid = limitsAbsent,
                EndpointConfigurationValid = endpointAbsent,
                CredentialConfigurationValid = credentialsAbsent,
            };
        }

        var endpointValid = serviceUrlValid && serviceUrl is not null
            && (serviceUrl.Scheme == Uri.UriSchemeHttps
                || allowLoopbackValid && allowLoopback && serviceUrl.Scheme == Uri.UriSchemeHttp
                && serviceUrl.IsLoopback)
            && string.IsNullOrEmpty(serviceUrl.UserInfo)
            && serviceUrl.AbsolutePath is "" or "/"
            && string.IsNullOrEmpty(serviceUrl.Query)
            && string.IsNullOrEmpty(serviceUrl.Fragment);
        var valid = topologyValid && topology == ProvisionalObjectTopology.S3CompatibleDurable
            && capabilityValid && endpointValid && allowLoopbackValid && limitValid && timeoutValid
            && IsBucket(bucket) && !string.IsNullOrWhiteSpace(access) && !string.IsNullOrWhiteSpace(secret);
        var topologyConfigurationValid = topologyValid
            && topology == ProvisionalObjectTopology.S3CompatibleDurable;
        var limitsConfigurationValid = limitValid && timeoutValid;
        var endpointConfigurationValid = endpointValid && IsBucket(bucket);
        var credentialConfigurationValid = capabilityValid
            && !string.IsNullOrWhiteSpace(access) && !string.IsNullOrWhiteSpace(secret);
        return new(topology, capabilityValid ? capability : null, serviceUrlValid ? serviceUrl : null,
            bucket, access, secret, allowLoopback, FixedMaximumSinglePartCiphertextBytes,
            FixedOperationTimeout, valid)
        {
            TopologyConfigurationValid = topologyConfigurationValid,
            LimitsConfigurationValid = limitsConfigurationValid,
            EndpointConfigurationValid = endpointConfigurationValid,
            CredentialConfigurationValid = credentialConfigurationValid,
        };
    }

    private static bool IsBucket(string? value) => value is { Length: >= 3 and <= 63 }
        && value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '.')
        && value[0] is >= 'a' and <= 'z' or >= '0' and <= '9'
        && value[^1] is >= 'a' and <= 'z' or >= '0' and <= '9'
        && !value.Contains("..", StringComparison.Ordinal)
        && !IsIpv4Literal(value);

    private static bool IsIpv4Literal(string value)
    {
        var segments = value.Split('.');
        return segments.Length == 4
            && segments.All(segment => segment.Length > 0
                && segment.All(c => c is >= '0' and <= '9')
                && byte.TryParse(segment, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out _));
    }

    public override string ToString() =>
        $"ProvisionalObjectCustodyOptions {{ Topology = {Topology}, Capability = {Capability}, ServiceUrl = {ServiceUrl?.GetLeftPart(UriPartial.Authority)}, BucketName = {BucketName}, AccessKeyId = [REDACTED], SecretAccessKey = [REDACTED] }}";
}
