using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawIngressBrokerOptions
{
    public const string SectionName = "TagEkyc:RawExport:IngressBroker";
    public const string AdmitPath = "/internal/capture-runtime/v1/source-ingress/admit";
    public Uri BaseUri { get; }
    public IPAddress ListenAddress { get; }
    public int RequestTimeoutMilliseconds { get; }
    public int EvaluationTokenTtlSeconds { get; }
    public int IdempotencyLockTimeoutMilliseconds { get; }
    public Guid EvaluationOwnerId { get; }
    public string CommitmentSelectorId { get; }
    public int CommitmentSelectorVersion { get; }
    public string SubjectTokenSelectorId { get; }
    public int SubjectTokenSelectorVersion { get; }
    public int ContinuationPollIntervalMilliseconds { get; }
    private readonly HashSet<string> allowedPeers;

    private RawIngressBrokerOptions(IConfiguration c)
    {
        var uriText = Required(c, "BaseUri");
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri) || uri.Scheme != "http"
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || uri.AbsolutePath != "/" || uri.Port < 1) throw Invalid();
        // Uri.Authority omits default port 80. Require an explicit input port,
        // while comparing actual HTTP Host using the canonical URI authority.
        var authority = uriText[7..].Split('/')[0];
        if (authority.StartsWith('[') ? !authority.Contains("]:", StringComparison.Ordinal)
            : authority.LastIndexOf(':') <= 0) throw Invalid();
        BaseUri = uri;
        ListenAddress = ParseAddress(Required(c, "ListenAddress"));
        if (!IsPrivate(ListenAddress)) throw Invalid();
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var baseAddress)
            && !Normalize(baseAddress).Equals(ListenAddress)) throw Invalid();
        allowedPeers = c.GetSection("AllowedApiAddresses").GetChildren()
            .Select(x => Normalize(ParseAddress(x.Value ?? "")).ToString()).ToHashSet(StringComparer.Ordinal);
        var peerCount = c.GetSection("AllowedApiAddresses").GetChildren().Count();
        if (peerCount is < 1 or > 32 || allowedPeers.Count != peerCount) throw Invalid();
        RequestTimeoutMilliseconds = Number(c, "RequestTimeoutMilliseconds", 30000);
        EvaluationTokenTtlSeconds = Number(c, "EvaluationTokenTtlSeconds", 3600);
        IdempotencyLockTimeoutMilliseconds = Number(c, "IdempotencyLockTimeoutMilliseconds", 30000);
        if (IdempotencyLockTimeoutMilliseconds >= RequestTimeoutMilliseconds
            || RequestTimeoutMilliseconds >= EvaluationTokenTtlSeconds * 1000L) throw Invalid();
        if (!Guid.TryParse(Required(c, "EvaluationOwnerId"), out var owner) || owner == Guid.Empty) throw Invalid();
        EvaluationOwnerId = owner;
        CommitmentSelectorId = Selector(c, "CommitmentSelectorId");
        CommitmentSelectorVersion = Number(c, "CommitmentSelectorVersion", int.MaxValue);
        SubjectTokenSelectorId = Selector(c, "SubjectTokenSelectorId");
        SubjectTokenSelectorVersion = Number(c, "SubjectTokenSelectorVersion", int.MaxValue);
        if (Number(c, "ApiReplicaCount", int.MaxValue) != 1) throw Invalid();
        ContinuationPollIntervalMilliseconds = Number(c, "ContinuationPollIntervalMilliseconds", 60000);
    }

    public static RawIngressBrokerOptions Read(IConfiguration configuration) =>
        new(configuration.GetSection(SectionName));
    public bool IsAllowedPeer(IPAddress? peer) => peer is not null && allowedPeers.Contains(Normalize(peer).ToString());
    public bool IsBoundEndpoint(IPAddress? address, int port, string host) => address is not null
        && Normalize(address).Equals(ListenAddress) && port == BaseUri.Port
        && string.Equals(host, BaseUri.Authority, StringComparison.Ordinal);
    internal RawIngressBrokerTransactionSettings TransactionSettings() => new(EvaluationOwnerId,
        EvaluationTokenTtlSeconds, IdempotencyLockTimeoutMilliseconds,
        new(CommitmentSelectorId, CommitmentSelectorVersion), new(SubjectTokenSelectorId, SubjectTokenSelectorVersion),
        RequestTimeoutMilliseconds);

    private static string Required(IConfiguration c, string key) => c[key] is { Length: > 0 } value ? value : throw Invalid();
    private static int Number(IConfiguration c, string key, int max) =>
        int.TryParse(Required(c, key), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number >= 1 && number <= max ? number : throw Invalid();
    private static string Selector(IConfiguration c, string key)
    {
        var value = Required(c, key);
        return value.Length <= 128 && value.All(x => x is >= '!' and <= '~') ? value : throw Invalid();
    }
    private static IPAddress ParseAddress(string text) => IPAddress.TryParse(text, out var address)
        ? Normalize(address) : throw Invalid();
    private static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;
        var b = address.GetAddressBytes();
        return address.AddressFamily == AddressFamily.InterNetwork
            ? b[0] == 10 || b[0] == 172 && b[1] is >= 16 and <= 31 || b[0] == 192 && b[1] == 168
            : (b[0] & 0xfe) == 0xfc && address.ScopeId == 0;
    }
    private static InvalidOperationException Invalid() => new("RAW_INGRESS_BROKER_CONFIGURATION_INVALID");
}
