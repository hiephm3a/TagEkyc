using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawIngressBrokerHttpClient : IRawIngressMetadataBroker, IDisposable
{
    private readonly HttpClient client;
    private readonly RawIngressBrokerOptions options;
    public RawIngressBrokerHttpClient(RawIngressBrokerOptions options) : this(options, Handler(options)) { }
    internal RawIngressBrokerHttpClient(RawIngressBrokerOptions options, HttpMessageHandler handler)
    {
        this.options = options;
        client = new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }
    public async Task<RawIngressBrokerResult> AdmitAsync(CaptureRuntimeRawIngressAdmissionContext c, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.RequestTimeoutMilliseconds);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUri, RawIngressBrokerOptions.AdmitPath))
        {
            Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ByteArrayContent(RawIngressBrokerProtocol.WriteRequest(c))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.ConnectionClose = true;
        // Exactly one send. Never repeat an uncertain committed B.
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK
            || response.Content.Headers.ContentType?.ToString() != "application/json"
            || response.Content.Headers.ContentEncoding.Count != 0
            || response.Headers.TransferEncoding.Count != 0
            || response.Content.Headers.ContentLength is not (>= 1 and <= RawIngressBrokerProtocol.ResponseLimit))
            throw RawIngressBrokerProtocol.Invalid();
        await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        var bytes = await RawIngressBrokerProtocol.ReadExactlyBounded(stream,
            checked((int)response.Content.Headers.ContentLength.Value), deadline.Token);
        return RawIngressBrokerProtocol.ReadResponse(bytes);
    }
    public void Dispose() => client.Dispose();

    private static SocketsHttpHandler Handler(RawIngressBrokerOptions options) => new()
    {
        AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false, UseProxy = false, PooledConnectionLifetime = TimeSpan.Zero,
        MaxResponseDrainSize = 0, ResponseDrainTimeout = TimeSpan.Zero,
        ConnectTimeout = TimeSpan.FromMilliseconds(options.RequestTimeoutMilliseconds),
        ConnectCallback = async (_, ct) =>
        {
            // Use the qualified endpoint, not DNS/discovery/proxy substitution.
            var socket = new Socket(options.ListenAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(options.ListenAddress, options.BaseUri.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch { socket.Dispose(); throw; }
        }
    };
}

// Both ends use this transport codec. It does not build or verify CRT1 bytes.
internal static class RawIngressBrokerProtocol
{
    internal const int RequestLimit = 16384, ResponseLimit = 4096;
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly string[] RequestFields =
    [
        "protocolVersion", "captureAgentId", "deviceInstallationId", "credentialId", "credentialGeneration",
        "rolePolicyId", "rolePolicyRevision", "signedAtUtc", "nonce", "signedEnvelopeFingerprint",
        "agentConfigurationRevision", "verificationSessionId", "captureArtifactId", "captureRevision", "rawClass",
        "ingressIdempotencyKey", "mediaType", "claimedPlaintextLength", "claimedPlaintextDigest", "capturedAtUtc",
        "plaintextRetentionStartedAtUtc", "plaintextRetentionExpiresAtUtc", "plaintextRetentionBudgetSeconds"
    ];
    private static readonly string[] HandoffFields =
    [
        "sourceArtifactId", "attemptKeyReservationId", "attemptId", "expectedEncryptionAttemptRevision", "expectedFence",
        "custodyActorPrincipalId", "clientApplicationId", "bindingId", "retentionAuthorityId", "retentionAuthorityRevision",
        "executionExpiresAtUtc"
    ];
    private static readonly HashSet<string> PreAdmissionCodes = new(StringComparer.Ordinal)
    {
        "RAW_EXPORT_SOURCE_BINDING_INVALID", "NOT_FOUND_OR_NOT_ALLOWED", "RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID",
        "RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE", "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED",
        "RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID", "RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE",
        "RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY", "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS",
        "RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID", "RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED", "SOURCE_RETENTION_NOT_AUTHORIZED",
        "RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE", "RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT",
        "RAW_EXPORT_SOURCE_RESERVATION_BUSY"
    };

    internal static byte[] WriteRequest(CaptureRuntimeRawIngressAdmissionContext c)
    {
        var bytes = Write(w =>
        {
            w.WriteNumber("protocolVersion", 1);
            Id(w, "captureAgentId", c.CaptureAgentId); Id(w, "deviceInstallationId", c.DeviceInstallationId);
            Id(w, "credentialId", c.CredentialId); w.WriteNumber("credentialGeneration", c.CredentialGeneration);
            Id(w, "rolePolicyId", c.RolePolicyId); w.WriteNumber("rolePolicyRevision", c.RolePolicyRevision);
            Date(w, "signedAtUtc", c.SignedAtUtc); w.WriteString("nonce", Base64(c.Nonce));
            w.WriteString("signedEnvelopeFingerprint", Base64(c.SignedEnvelopeFingerprint));
            w.WriteNumber("agentConfigurationRevision", c.AgentConfigurationRevision);
            Id(w, "verificationSessionId", c.VerificationSessionId); Id(w, "captureArtifactId", c.CaptureArtifactId);
            w.WriteNumber("captureRevision", c.CaptureRevision); w.WriteString("rawClass", c.RawClass);
            Id(w, "ingressIdempotencyKey", c.IngressIdempotencyKey); w.WriteString("mediaType", c.MediaType);
            w.WriteNumber("claimedPlaintextLength", c.ClaimedPlaintextLength);
            w.WriteString("claimedPlaintextDigest", c.ClaimedPlaintextDigest);
            Date(w, "capturedAtUtc", c.CapturedAtUtc); Date(w, "plaintextRetentionStartedAtUtc", c.PlaintextRetentionStartedAtUtc);
            Date(w, "plaintextRetentionExpiresAtUtc", c.PlaintextRetentionExpiresAtUtc);
            w.WriteNumber("plaintextRetentionBudgetSeconds", checked((int)c.PlaintextRetentionBudgetSeconds));
        });
        _ = ReadRequest(bytes); // Invalid local observations never cause a send.
        return bytes;
    }

    internal static CaptureRuntimeRawIngressAdmissionContext ReadRequest(byte[] bytes)
    {
        using var document = Parse(bytes, RequestLimit, 2);
        var p = Object(document.RootElement, RequestFields);
        if (Integer(p["protocolVersion"], 1) != 1) throw Invalid();
        var rawClass = Text(p["rawClass"]);
        var media = Text(p["mediaType"]);
        var digest = Text(p["claimedPlaintextDigest"]);
        if (rawClass is not ("ChipDg2Portrait" or "LiveSelfieImage") || media.Length is < 1 or > 128
            || media.Any(c => c is < ' ' or > '~') || digest.Length != 64
            || digest.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))) throw Invalid();
        return new(Id(p["captureAgentId"]), Id(p["deviceInstallationId"]), Id(p["credentialId"]),
            Integer(p["credentialGeneration"]), Id(p["rolePolicyId"]), Integer(p["rolePolicyRevision"]),
            Date(p["signedAtUtc"]), Base64(p["nonce"]), Base64(p["signedEnvelopeFingerprint"]),
            Integer(p["agentConfigurationRevision"]), Id(p["verificationSessionId"]), Id(p["captureArtifactId"]),
            checked((int)Integer(p["captureRevision"], int.MaxValue)), rawClass, Id(p["ingressIdempotencyKey"]),
            media, Integer(p["claimedPlaintextLength"]), digest, Date(p["capturedAtUtc"]),
            Date(p["plaintextRetentionStartedAtUtc"]), Date(p["plaintextRetentionExpiresAtUtc"]),
            Integer(p["plaintextRetentionBudgetSeconds"], int.MaxValue));
    }

    internal static byte[] WriteResponse(RawIngressBrokerResult result)
    {
        var bytes = Write(w =>
        {
            w.WriteNumber("protocolVersion", 1);
            if (result is RawIngressBrokerResult.Handoff h)
            {
                w.WriteString("kind", "Handoff"); w.WriteStartObject("handoff");
                Id(w, "sourceArtifactId", h.Value.SourceArtifactId); Id(w, "attemptKeyReservationId", h.Value.AttemptKeyReservationId);
                Id(w, "attemptId", h.Value.AttemptId); w.WriteNumber("expectedEncryptionAttemptRevision", h.Value.ExpectedEncryptionAttemptRevision);
                w.WriteNumber("expectedFence", h.Value.ExpectedFence); Id(w, "custodyActorPrincipalId", h.Value.CustodyActorPrincipalId);
                Id(w, "clientApplicationId", h.Value.ClientApplicationId); Id(w, "bindingId", h.Value.BindingId);
                Id(w, "retentionAuthorityId", h.Value.RetentionAuthorityId); w.WriteNumber("retentionAuthorityRevision", h.Value.RetentionAuthorityRevision);
                Date(w, "executionExpiresAtUtc", h.Value.ExecutionExpiresAtUtc); w.WriteEndObject();
            }
            else if (result is RawIngressBrokerResult.Final f)
            {
                ValidateFinal(f);
                w.WriteString("kind", "Final"); w.WriteStartObject("final"); w.WriteString("outcomeCode", f.Value.OutcomeCode);
                if (f.Value.SourceArtifactId is { } source)
                {
                    Id(w, "sourceArtifactId", source); w.WriteString("currentSourceState", f.Value.CurrentSourceState);
                    w.WriteString("currentDisposition", f.Value.CurrentDisposition);
                }
                if (f.Value.RetryNotBeforeUtc is { } retry) Date(w, "retryNotBeforeUtc", retry);
                w.WriteEndObject();
            }
            else throw Invalid();
        });
        _ = ReadResponse(bytes);
        return bytes;
    }

    internal static RawIngressBrokerResult ReadResponse(byte[] bytes)
    {
        using var document = Parse(bytes, ResponseLimit, 3);
        var root = Object(document.RootElement);
        if (!root.TryGetValue("protocolVersion", out var version) || Integer(version, 1) != 1
            || !root.TryGetValue("kind", out var kind)) throw Invalid();
        if (Text(kind) == "Handoff")
        {
            root = Object(document.RootElement, ["protocolVersion", "kind", "handoff"]);
            var h = Object(root["handoff"], HandoffFields);
            return new RawIngressBrokerResult.Handoff(new(Id(h["sourceArtifactId"]), Id(h["attemptKeyReservationId"]),
                Id(h["attemptId"]), Integer(h["expectedEncryptionAttemptRevision"]), Integer(h["expectedFence"]),
                Id(h["custodyActorPrincipalId"]), Id(h["clientApplicationId"]), Id(h["bindingId"]),
                Id(h["retentionAuthorityId"]), Integer(h["retentionAuthorityRevision"]), Date(h["executionExpiresAtUtc"])));
        }
        if (Text(kind) != "Final") throw Invalid();
        root = Object(document.RootElement, ["protocolVersion", "kind", "final"]);
        var p = Object(root["final"]);
        if (!p.TryGetValue("outcomeCode", out var codeValue)) throw Invalid();
        var code = Text(codeValue);
        var origin = code switch
        {
            "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE" => RawIngressBrokerFinalOrigin.PublishedReplay,
            "RAW_EXPORT_SOURCE_RESUME_PENDING" => RawIngressBrokerFinalOrigin.PreservedCiphertext,
            "CONTENT_COMMITMENT_MISMATCH" or "RECAPTURE_REQUIRED" => RawIngressBrokerFinalOrigin.PersistedTerminal,
            _ => RawIngressBrokerFinalOrigin.PreAdmission
        };
        // This is the reply to the single trusted private operation. The SQL
        // producer checks the actual origin; no extra evidence is invented on wire.
        CaptureAgentFinalResult value;
        if (code == "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE")
        {
            p = Object(root["final"], ["outcomeCode", "sourceArtifactId", "currentSourceState", "currentDisposition"]);
            value = new(code, Id(p["sourceArtifactId"]), Text(p["currentSourceState"]), Text(p["currentDisposition"]));
        }
        else if (code == "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS")
        {
            p = Object(root["final"], ["outcomeCode", "retryNotBeforeUtc"]);
            value = new(code, RetryNotBeforeUtc: Date(p["retryNotBeforeUtc"]));
        }
        else { _ = Object(root["final"], ["outcomeCode"]); value = new(code); }
        var result = new RawIngressBrokerResult.Final(value, origin);
        ValidateFinal(result);
        return result;
    }

    private static void ValidateFinal(RawIngressBrokerResult.Final f)
    {
        var v = f.Value;
        var validOrigin = f.Origin switch
        {
            RawIngressBrokerFinalOrigin.PreAdmission => PreAdmissionCodes.Contains(v.OutcomeCode),
            RawIngressBrokerFinalOrigin.PublishedReplay => v.OutcomeCode == "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE",
            RawIngressBrokerFinalOrigin.PreservedCiphertext => v.OutcomeCode == "RAW_EXPORT_SOURCE_RESUME_PENDING",
            RawIngressBrokerFinalOrigin.PersistedTerminal => v.OutcomeCode is "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED"
                or "CONTENT_COMMITMENT_MISMATCH" or "RECAPTURE_REQUIRED",
            _ => false
        };
        if (!validOrigin) throw Invalid();
        if (v.OutcomeCode == "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE")
        {
            if (v.SourceArtifactId is null || v.SourceArtifactId == Guid.Empty || v.CurrentSourceState != "Available"
                || v.CurrentDisposition != "Available" || v.RetryNotBeforeUtc is not null) throw Invalid();
        }
        else if (v.SourceArtifactId is not null || v.CurrentSourceState is not null || v.CurrentDisposition is not null
            || (v.OutcomeCode == "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS") != (v.RetryNotBeforeUtc is not null)) throw Invalid();
    }

    internal static async Task<byte[]> ReadExactlyBounded(Stream stream, int length, CancellationToken ct)
    {
        var bytes = new byte[checked(length + 1)];
        var offset = 0;
        while (offset < bytes.Length)
        {
            var n = await stream.ReadAsync(bytes.AsMemory(offset), ct);
            if (n == 0) break;
            offset += n;
        }
        if (offset != length) throw Invalid();
        return bytes[..length];
    }
    private static JsonDocument Parse(byte[] bytes, int limit, int depth)
    {
        if (bytes.Length < 1 || bytes.Length > limit) throw Invalid();
        _ = StrictUtf8.GetCharCount(bytes);
        return JsonDocument.Parse(bytes, new() { MaxDepth = depth, AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
    }
    private static Dictionary<string, JsonElement> Object(JsonElement element, string[]? fields = null)
    {
        if (element.ValueKind != JsonValueKind.Object) throw Invalid();
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var p in element.EnumerateObject())
            if (p.Value.ValueKind == JsonValueKind.Null || !properties.TryAdd(p.Name, p.Value)) throw Invalid();
        if (fields is not null && (properties.Count != fields.Length || fields.Any(x => !properties.ContainsKey(x)))) throw Invalid();
        return properties;
    }
    private static long Integer(JsonElement e, long max = long.MaxValue) => e.ValueKind == JsonValueKind.Number
        && e.TryGetInt64(out var value) && value >= 1 && value <= max ? value : throw Invalid();
    private static string Text(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString()! : throw Invalid();
    private static Guid Id(JsonElement e)
    {
        var s = Text(e);
        return Guid.TryParseExact(s, "N", out var value) && value != Guid.Empty && value.ToString("N") == s ? value : throw Invalid();
    }
    private static DateTimeOffset Date(JsonElement e) => DateTimeOffset.TryParseExact(Text(e), UtcFormat,
        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value)
        && value.ToString(UtcFormat, CultureInfo.InvariantCulture) == Text(e) ? value : throw Invalid();
    private static string Base64(byte[] bytes) => bytes.Length == 32
        ? Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') : throw Invalid();
    private static byte[] Base64(JsonElement e)
    {
        var s = Text(e);
        if (s.Length != 43 || s.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) throw Invalid();
        var bytes = Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + "=");
        return Base64(bytes) == s ? bytes : throw Invalid();
    }
    private static void Id(Utf8JsonWriter writer, string key, Guid value) => writer.WriteString(key, value.ToString("N"));
    private static void Date(Utf8JsonWriter writer, string key, DateTimeOffset value) =>
        writer.WriteString(key, value.ToUniversalTime().ToString(UtcFormat, CultureInfo.InvariantCulture));
    private static byte[] Write(Action<Utf8JsonWriter> body)
    {
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output)) { writer.WriteStartObject(); body(writer); writer.WriteEndObject(); }
        return output.ToArray();
    }
    internal static InvalidOperationException Invalid() => new("RAW_INGRESS_PRIVATE_PROTOCOL_INVALID");
}
