using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public enum RecipientPackageTopology
{
    Disabled,
    S3CompatibleDurable,
    Invalid,
}

public sealed record RecipientPackageCredential(string AccessKeyId, string SecretAccessKey)
{
    public override string ToString() => "RecipientPackageCredential { AccessKeyId = [REDACTED], SecretAccessKey = [REDACTED] }";
}

public sealed record RecipientPackageProviderConfiguration(
    string ProviderConfigurationId,
    Uri ServiceUrl,
    string BucketName,
    bool ForcePathStyle,
    string RegionIdentifier,
    RecipientPackageCredential Writer,
    RecipientPackageCredential Reconciler,
    RecipientPackageCredential Lifecycle,
    RecipientPackageCredential PostureProbe,
    bool AllowLoopbackHttp)
{
    public const string ProviderKind = "s3-compatible-single-part-v1";

    public override string ToString() =>
        $"RecipientPackageProviderConfiguration {{ ProviderConfigurationId = {ProviderConfigurationId}, ServiceUrl = {ServiceUrl.GetLeftPart(UriPartial.Authority)}, BucketName = {BucketName}, Credentials = [REDACTED] }}";
}

public sealed record RecipientPackageOptions(
    RecipientPackageTopology Topology,
    RecipientPackageProviderConfiguration? Provider,
    bool IsSyntacticallyValid)
{
    public const string SectionPath = "TagEkyc:RawExport:RecipientPackage";
    public const string PackageProfile = "tip-88c1-c2-package-profile-v1";
    public const int FramePlaintextBytes = 1_048_576;
    public const int MaximumDataFrameCount = 32;
    public const int MaximumHeaderLength = 1_848;
    public const long MaximumCompleteAssemblyLength = 33_554_432;
    public const long MaximumEncryptedPackageLength = 33_557_106;
    public static readonly TimeSpan OperationTimeout = TimeSpan.FromMinutes(5);

    public static RecipientPackageOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionPath);
        var topologyRaw = section["Topology"];
        if (topologyRaw is null or "" or "Disabled")
        {
            var noResidue = !section.GetChildren().Any(child => child.Key != "Topology");
            return new(RecipientPackageTopology.Disabled, null, noResidue);
        }

        if (!string.Equals(topologyRaw, "S3CompatibleDurable", StringComparison.Ordinal))
            return new(RecipientPackageTopology.Invalid, null, false);

        var id = section["ProviderConfigurationId"];
        var provider = id is null ? null : section.GetSection("Providers").GetSection(id);
        var uriValid = Uri.TryCreate(provider?["ServiceUrl"], UriKind.Absolute, out var uri);
        var forceValid = bool.TryParse(provider?["ForcePathStyle"], out var forcePathStyle) && forcePathStyle;
        var loopbackValid = bool.TryParse(provider?["AllowLoopbackHttp"], out var allowLoopback);
        var bucket = provider?["BucketName"];
        var region = provider?["RegionIdentifier"];
        var writer = Credential(provider, "Writer");
        var reconciler = Credential(provider, "Reconciler");
        var lifecycle = Credential(provider, "Lifecycle");
        var posture = Credential(provider, "PostureProbe");
        var endpointValid = uriValid && uri is not null
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath is "" or "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && (uri.Scheme == Uri.UriSchemeHttps
                || loopbackValid && allowLoopback && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        var valid = IsId(id) && endpointValid && forceValid && loopbackValid
            && IsBucket(bucket) && IsRegion(region)
            && writer is not null && reconciler is not null && lifecycle is not null && posture is not null
            && new[] { writer?.AccessKeyId, reconciler?.AccessKeyId, lifecycle?.AccessKeyId, posture?.AccessKeyId }
                .Distinct(StringComparer.Ordinal).Count() == 4;
        if (!valid)
            return new(RecipientPackageTopology.S3CompatibleDurable, null, false);

        return new(
            RecipientPackageTopology.S3CompatibleDurable,
            new(id!, Normalize(uri!), bucket!, true, region!, writer!, reconciler!, lifecycle!, posture!, allowLoopback),
            true);
    }

    private static RecipientPackageCredential? Credential(IConfigurationSection? provider, string name)
    {
        var section = provider?.GetSection(name);
        var access = section?["AccessKeyId"];
        var secret = section?["SecretAccessKey"];
        return string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret)
            ? null
            : new(access, secret);
    }

    internal static Uri Normalize(Uri value)
    {
        var builder = new UriBuilder(value)
        {
            Scheme = value.Scheme.ToLowerInvariant(),
            Host = value.Host.ToLowerInvariant(),
            Path = "/",
            Query = string.Empty,
            Fragment = string.Empty,
        };
        if (builder.Port < 0)
            builder.Port = builder.Scheme == Uri.UriSchemeHttps ? 443 : 80;
        return builder.Uri;
    }

    private static bool IsId(string? value) => value is { Length: >= 1 and <= 128 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');

    private static bool IsRegion(string? value) => value is { Length: >= 1 and <= 64 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-');

    private static bool IsBucket(string? value) => value is { Length: >= 3 and <= 63 }
        && value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '.')
        && value[0] is >= 'a' and <= 'z' or >= '0' and <= '9'
        && value[^1] is >= 'a' and <= 'z' or >= '0' and <= '9'
        && !value.Contains("..", StringComparison.Ordinal);
}
