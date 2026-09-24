using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Api;

// Operational evidence is data, not a Markdown assertion. The activation seal
// pins the SHA-256 of these exact bytes before a zero-open build can start.
public sealed class SiteRawIngressTransportQualificationFileProvider
    : ICaptureRuntimeSiteTransportQualificationProvider,
      ICaptureRuntimeSiteTransportQualificationSettingsProvider
{
    public const string RecordPathConfigurationKey =
        "TagEkyc:CaptureRuntime:SiteRawIngressTransportQualificationRecordPath";
    public const string SiteIdConfigurationKey =
        "TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:SiteId";
    public const string EndpointOriginConfigurationKey =
        "TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:EndpointOrigin";
    public const string DeploymentRevisionConfigurationKey =
        "TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:DeploymentRevision";
    public const string ExpiryWarningMinutesConfigurationKey =
        "TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:ExpiryWarningMinutes";

    private static readonly HashSet<string> ExactFields = new(StringComparer.Ordinal)
    {
        "formatVersion", "qualificationId", "siteId", "endpointOrigin", "deploymentRevision",
        "status", "observedAtUtc", "validUntilUtc", "agentBodySendsWhileBOrR1Held",
        "serverApplicationBodyReadsWhileBOrR1Held", "rawPostCount",
        "kestrelContinueRelayedAfterCommit", "earlyOrIntermediaryContinueObserved",
        "applicationPrebufferObserved", "hiddenRetryObserved"
    };

    private readonly IConfiguration configuration;

    public SiteRawIngressTransportQualificationFileProvider(IConfiguration configuration) =>
        this.configuration = configuration;

    // Read on every evaluation. An operator can atomically replace a renewed
    // record without restarting an Activated host.
    public CaptureRuntimeSiteTransportQualification? Current =>
        Read(configuration[RecordPathConfigurationKey]);

    CaptureRuntimeSiteTransportQualificationSettings?
        ICaptureRuntimeSiteTransportQualificationSettingsProvider.Current => ReadSettings(configuration);

    private static CaptureRuntimeSiteTransportQualificationSettings? ReadSettings(IConfiguration configuration)
    {
        var siteId = configuration[SiteIdConfigurationKey];
        var origin = configuration[EndpointOriginConfigurationKey];
        var revision = configuration[DeploymentRevisionConfigurationKey];
        if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(origin) ||
            string.IsNullOrWhiteSpace(revision)) return null;
        var warningMinutes = int.TryParse(configuration[ExpiryWarningMinutesConfigurationKey],
            NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed is >= 0 and <= 10080
            ? parsed : 1440;
        return new(siteId, origin, revision, TimeSpan.FromMinutes(warningMinutes));
    }

    private static CaptureRuntimeSiteTransportQualification? Read(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !File.Exists(path))
                return null;
            var bytes = File.ReadAllBytes(path);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 8
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            var properties = document.RootElement.EnumerateObject().ToArray();
            if (properties.Length != ExactFields.Count ||
                properties.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != ExactFields.Count ||
                !properties.Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(ExactFields))
                return null;
            var root = document.RootElement;
            static string Text(JsonElement root, string name) => root.GetProperty(name).GetString() ?? string.Empty;
            if (!DateTimeOffset.TryParseExact(Text(root, "observedAtUtc"), "O",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var observed) ||
                !DateTimeOffset.TryParseExact(Text(root, "validUntilUtc"), "O",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var validUntil) ||
                observed.Offset != TimeSpan.Zero || validUntil.Offset != TimeSpan.Zero)
                return null;
            return new CaptureRuntimeSiteTransportQualification(
                root.GetProperty("formatVersion").GetInt32(),
                Text(root, "qualificationId"), Text(root, "siteId"), Text(root, "endpointOrigin"),
                Text(root, "deploymentRevision"), Text(root, "status"), observed, validUntil,
                root.GetProperty("agentBodySendsWhileBOrR1Held").GetInt32(),
                root.GetProperty("serverApplicationBodyReadsWhileBOrR1Held").GetInt32(),
                root.GetProperty("rawPostCount").GetInt32(),
                root.GetProperty("kestrelContinueRelayedAfterCommit").GetBoolean(),
                root.GetProperty("earlyOrIntermediaryContinueObserved").GetBoolean(),
                root.GetProperty("applicationPrebufferObserved").GetBoolean(),
                root.GetProperty("hiddenRetryObserved").GetBoolean(),
                Convert.ToHexString(SHA256.HashData(bytes)));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
            or InvalidOperationException or FormatException or OverflowException)
        {
            return null;
        }
    }
}
