using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TagEkyc.Api;

public static partial class CaptureRuntimeHttpRoutes
{
    public static IEndpointRouteBuilder MapCaptureRuntimeManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/bootstrap-issuances", IssueBootstrapAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/bootstrap-issuances/revoke", RevokeBootstrapAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/suspend", SuspendRuntimeAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/reactivate", ReactivateRuntimeAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/revoke", RevokeRuntimeAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/retire", RetireRuntimeAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/credentials/revoke", RevokeCredentialAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/credential-rotations/authorize", AuthorizeRotationAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/credential-rotations/revoke", RevokeRotationAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/role-policies/assign", AssignRolePolicyAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtimes/configurations/assign", AssignConfigurationAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtime-control/trust-profiles/publish", PublishTrustProfileAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtime-control/role-policies/publish", PublishRolePolicyAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/operator/capture-runtime-control/configurations/publish", PublishConfigurationAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/capture-runtime/enrollments/redeem", RedeemAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapGet("/api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness", ReadReadinessAsync).AddEndpointFilter<ClosedFailureFilter>();
        return endpoints;
    }

    private static async Task<IResult> IssueBootstrapAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeBootstrapIssueRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.IssueBootstrapAsync(auth.Value!, body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> RevokeBootstrapAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeBootstrapRevokeRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.RevokeBootstrapAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> SuspendRuntimeAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<RuntimeLifecycleRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.SuspendRuntimeAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> ReactivateRuntimeAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<RuntimeLifecycleRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.ReactivateRuntimeAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> RevokeRuntimeAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<RuntimeLifecycleRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.RevokeRuntimeAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> RetireRuntimeAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<RuntimeLifecycleRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.RetireRuntimeAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> RevokeCredentialAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeCredentialRevokeRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.RevokeCredentialAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> AuthorizeRotationAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeRotationAuthorizeRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.AuthorizeRotationAsync(auth.Value!, body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> RevokeRotationAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeRotationRevokeRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.RevokeRotationAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> AssignRolePolicyAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeRolePolicyAssignmentRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.AssignRolePolicyAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> AssignConfigurationAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeConfigurationAssignmentRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>();
        return Respond(context, await service.AssignConfigurationAsync(auth.Value!, body.Value, key, body.Bytes, ct), 200);
    }

    private static async Task<IResult> PublishTrustProfileAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeTrustProfilePublicationRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeControlService>();
        return Respond(context, await service.PublishTrustProfileAsync(auth.Value!, body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> PublishRolePolicyAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeRolePolicyPublicationRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeControlService>();
        return Respond(context, await service.PublishRolePolicyAsync(auth.Value!, body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> PublishConfigurationAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeConfigurationPublicationRequest>(context.Request, 32768, ct);
        if (body is null) return Invalid(context);
        var service = context.RequestServices.GetRequiredService<ICaptureRuntimeControlService>();
        return Respond(context, await service.PublishConfigurationAsync(auth.Value!, body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> RedeemAsync(HttpContext context, CancellationToken ct)
    {
        if (context.Request.Headers.Keys.Any(x => x.StartsWith("X-TagEkyc-Capture-Runtime-", StringComparison.OrdinalIgnoreCase))
            || context.Request.Headers.ContainsKey("X-TagEkyc-Api-Key")
            || context.Request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key")) return Denied(context);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeEnrollmentRedeemRequest>(context.Request, 16384, ct);
        if (body is null) return Invalid(context);
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeEnrollmentService>()
            .RedeemAsync(body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> ReadReadinessAsync(HttpContext context, CancellationToken ct)
    {
        var auth = await PlatformAsync(context, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (!TryRouteId(context, "captureAgentId", out var id) || !Bodyless(context.Request)) return Invalid(context);
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeManagementService>()
            .ReadReadinessAsync(auth.Value!, id, ct));
    }

    private static async Task<SessionOperationResult<AuthenticatedPlatformOperatorContext>> PlatformAsync(HttpContext context, CancellationToken ct)
    {
        if (context.Request.Headers.ContainsKey("X-TagEkyc-Api-Key") ||
            context.Request.Headers.Keys.Any(x => x.StartsWith("X-TagEkyc-Capture-Runtime-", StringComparison.OrdinalIgnoreCase)) ||
            !Single(context.Request, "X-TagEkyc-Platform-Operator-Key", out var key))
            return SessionOperationResult<AuthenticatedPlatformOperatorContext>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403);
        return await context.RequestServices.GetRequiredService<IPlatformOperatorCredentialAuthenticator>().AuthenticateAsync(key, ct);
    }

    private static IResult Respond<T>(HttpContext context, SessionOperationResult<T> result, int successStatus = 200)
    {
        context.Response.Headers.CacheControl = "no-store";
        // New A1 DTOs use the ratified U/T atoms. Landed capture/evidence DTOs
        // retain the host's existing response contract.
        var options = typeof(T).Namespace == typeof(CaptureRuntimeEnrollmentResponse).Namespace ? ClosedJson : null;
        return result.IsSuccess ? Results.Json(result.Value, options, statusCode: result.IsReplay ? 200 : successStatus)
            : Error(context, result.Error!.StatusCode, result.Error.Code);
    }
    private static IResult Invalid(HttpContext c) => Error(c, 400, CaptureRuntimeErrorCodes.RequestInvalid);
    private static IResult Denied(HttpContext c) => Error(c, 403, CaptureRuntimeErrorCodes.AccessDenied);
    private static IResult Error(HttpContext c, int status, string code) =>
        Results.Json(new { code, correlationId = c.TraceIdentifier }, statusCode: status);
    private static bool Single(HttpRequest r, string name, out string value)
    {
        var values = r.Headers[name];
        value = values.Count == 1 ? values[0] ?? "" : "";
        return value.Length > 0 && value == value.Trim() && !value.Contains(',') && values.Count == 1;
    }
    private static bool Uuid(string value, out Guid id) =>
        Guid.TryParseExact(value, "N", out id) && id.ToString("N") == value && value[12] == '4' && value[16] is '8' or '9' or 'a' or 'b';
    private static bool TryRouteId(HttpContext c, string name, out Guid id) =>
        Uuid(c.Request.RouteValues[name]?.ToString() ?? "", out id);
    private static bool TryIdempotency(HttpRequest r, out Guid id)
    {
        id = default;
        return Single(r, "Idempotency-Key", out var value) && Uuid(value, out id);
    }
    private static bool Bodyless(HttpRequest r) => !r.QueryString.HasValue && (r.ContentLength ?? 0) == 0 &&
        !r.Headers.ContainsKey("Idempotency-Key") && !r.Headers.ContainsKey("Transfer-Encoding");

    private sealed class Body<T>(T value, byte[] bytes) : IDisposable
    {
        public T Value { get; } = value;
        public ReadOnlyMemory<byte> Bytes => bytes;
        public void Dispose() => CryptographicOperations.ZeroMemory(bytes);
    }
    private static readonly JsonSerializerOptions ClosedJson = new()
    {
        PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new UuidConverter(), new TimestampConverter(), new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    private static async Task<Body<T>?> ReadBodyAsync<T>(HttpRequest r, int limit, CancellationToken ct) where T : class
    {
        if (r.QueryString.HasValue || r.ContentType != "application/json" || r.ContentLength is not > 0 ||
            r.ContentLength > limit || r.Headers.ContainsKey("Transfer-Encoding") || r.Headers.ContainsKey("Content-Encoding")) return null;
        var bytes = new byte[(int)r.ContentLength.Value];
        try
        {
            await r.Body.ReadExactlyAsync(bytes, ct);
            using var json = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
            if (!MembersValid(json.RootElement) || !RequiredMembersPresent(json.RootElement, typeof(T)))
            { CryptographicOperations.ZeroMemory(bytes); return null; }
            var value = JsonSerializer.Deserialize<T>(bytes, ClosedJson);
            if (value is null) { CryptographicOperations.ZeroMemory(bytes); return null; }
            return new(value, bytes);
        }
        catch (Exception e) when (e is JsonException or EndOfStreamException or FormatException)
        { CryptographicOperations.ZeroMemory(bytes); return null; }
    }
    // System.Text.Json on net8 does not require positional-record constructor
    // arguments. Respect the closed DTO's non-nullable members before defaults
    // such as false/0 can silently become caller authority. Nullable members
    // remain genuinely optional; the R20 action union has its own exact check.
    private static bool RequiredMembersPresent(JsonElement element, Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (element.ValueKind == JsonValueKind.Array)
        {
            var itemType = type.IsArray ? type.GetElementType() : type.GetInterfaces().Append(type)
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
            return itemType is not null && element.EnumerateArray().All(item => RequiredMembersPresent(item, itemType));
        }
        if (element.ValueKind != JsonValueKind.Object) return true;
        var nullability = new System.Reflection.NullabilityInfoContext();
        foreach (var property in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (property.GetMethod is null || property.GetIndexParameters().Length != 0) continue;
            var required = property.PropertyType.IsValueType
                ? Nullable.GetUnderlyingType(property.PropertyType) is null
                : nullability.Create(property).ReadState == System.Reflection.NullabilityState.NotNull;
            var present = element.TryGetProperty(property.Name, out var value);
            if (required && (!present || value.ValueKind == JsonValueKind.Null)) return false;
            if (present && value.ValueKind != JsonValueKind.Null && !RequiredMembersPresent(value, property.PropertyType)) return false;
        }
        return true;
    }
    private static bool MembersValid(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in e.EnumerateObject())
                if (!names.Add(p.Name) || p.Name.Any(IsControl) || !MembersValid(p.Value)) return false;
        }
        else if (e.ValueKind == JsonValueKind.Array)
        { foreach (var item in e.EnumerateArray()) if (!MembersValid(item)) return false; }
        else if (e.ValueKind == JsonValueKind.String && e.GetString()!.Any(IsControl)) return false;
        return true;
    }
    private static bool IsControl(char c) => c < 0x20 || (c >= 0x7f && c <= 0x9f);
    private sealed class UuidConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        { if (reader.TokenType != JsonTokenType.String || !Uuid(reader.GetString()!, out var id)) throw new JsonException(); return id; }
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("N"));
    }
    private sealed class TimestampConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String || !DateTimeOffset.TryParseExact(reader.GetString(),
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value)) throw new JsonException();
            return value;
        }
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
    }
    private sealed class ClosedFailureFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            try { return await next(context); }
            catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested) { throw; }
            catch (Exception) { return Error(context.HttpContext, 503, CaptureRuntimeErrorCodes.NotReady); }
        }
    }
}

public static class CaptureRuntimeCrt1RequestParser
{
    private const string RawIngressPath = "/api/ekyc/raw-export/source-ingress";
    private const string IngressBindingPrefix = "ingress=IngressMetadataSha256=";
    private static readonly string[] IngressMetadataHeaders =
    [
        "X-TagEkyc-Agent-Configuration-Revision",
        "X-TagEkyc-Verification-Session-Id",
        "X-TagEkyc-Capture-Artifact-Id",
        "X-TagEkyc-Capture-Revision",
        "X-TagEkyc-Raw-Class",
        "Idempotency-Key",
        "X-TagEkyc-Captured-At-Utc",
        "X-TagEkyc-Retention-Started-At-Utc",
        "X-TagEkyc-Retention-Expires-At-Utc",
        "X-TagEkyc-Retention-Budget-Seconds",
    ];

    private static readonly HashSet<string> AllowedRuntimeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        CredentialIdHeader,
        CredentialGenerationHeader,
        TimestampHeader,
        NonceHeader,
        SignatureHeader,
    };

    public const string CredentialIdHeader = "X-TagEkyc-Capture-Runtime-Credential-Id";
    public const string CredentialGenerationHeader = "X-TagEkyc-Capture-Runtime-Credential-Generation";
    public const string TimestampHeader = "X-TagEkyc-Capture-Runtime-Timestamp";
    public const string NonceHeader = "X-TagEkyc-Capture-Runtime-Nonce";
    public const string SignatureHeader = "X-TagEkyc-Capture-Runtime-Signature";

    public static bool TryCreate(
        HttpRequest request,
        string requiredRole,
        string canonicalMediaType,
        long contentLength,
        string lowercaseBodySha256,
        string operationBindingLine,
        out CaptureRuntimeSignedRequest? signedRequest)
    {
        signedRequest = null;
        ArgumentNullException.ThrowIfNull(request);

        if (request.QueryString.HasValue || string.IsNullOrWhiteSpace(requiredRole) ||
            contentLength < 0 || !IsLowercaseMediaType(canonicalMediaType) ||
            !IsLowercaseSha256(lowercaseBodySha256) ||
            (request.ContentLength ?? 0) != contentLength ||
            !string.Equals(request.ContentType ?? string.Empty, canonicalMediaType, StringComparison.Ordinal) ||
            operationBindingLine.Contains('\r', StringComparison.Ordinal) ||
            operationBindingLine.Contains('\n', StringComparison.Ordinal) ||
            request.Headers.ContainsKey("X-TagEkyc-Api-Key") ||
            request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key") ||
            request.Headers.Keys.Any(name =>
                name.StartsWith("X-TagEkyc-Capture-Runtime-", StringComparison.OrdinalIgnoreCase) &&
                !AllowedRuntimeHeaders.Contains(name)))
        {
            return false;
        }

        if (string.Equals(request.Path.Value, RawIngressPath, StringComparison.Ordinal))
        {
            if (!TryComputeIngressMetadataDigest(request, out var metadataDigest) ||
                !string.Equals(operationBindingLine, IngressBindingPrefix + metadataDigest, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (operationBindingLine.StartsWith("ingress=", StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryGetSingleHeader(request, CredentialIdHeader, out var credentialText) ||
            !Guid.TryParseExact(credentialText, "N", out var credentialId) ||
            !string.Equals(credentialText, credentialId.ToString("N"), StringComparison.Ordinal) ||
            !TryGetSingleHeader(request, CredentialGenerationHeader, out var generationText) ||
            !long.TryParse(generationText, NumberStyles.None, CultureInfo.InvariantCulture, out var generation) ||
            generation <= 0 ||
            !string.Equals(generationText, generation.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ||
            !TryGetSingleHeader(request, TimestampHeader, out var timestampText) ||
            !DateTimeOffset.TryParseExact(
                timestampText,
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var signedAtUtc) ||
            !TryGetSingleHeader(request, NonceHeader, out var nonceText) ||
            !TryDecodeBase64Url(nonceText, 32, out var nonce) ||
            !TryGetSingleHeader(request, SignatureHeader, out var signatureText) ||
            !TryDecodeBase64Url(signatureText, 64, out var signature))
        {
            return false;
        }

        var method = request.Method;
        var path = request.Path.Value;
        if (string.IsNullOrEmpty(path) ||
            !string.Equals(method, method.ToUpperInvariant(), StringComparison.Ordinal))
        {
            return false;
        }

        var preimage = Encoding.UTF8.GetBytes(string.Join('\n',
            "TAG-EKYC-CRT1",
            method,
            path,
            credentialText,
            generationText,
            timestampText,
            nonceText,
            canonicalMediaType,
            contentLength.ToString(CultureInfo.InvariantCulture),
            lowercaseBodySha256,
            operationBindingLine) + "\n");

        signedRequest = new CaptureRuntimeSignedRequest(
            credentialId,
            generation,
            signedAtUtc,
            nonce,
            signature,
            requiredRole,
            new ReadOnlyMemory<byte>(preimage));
        return true;
    }

    public static bool TryComputeIngressMetadataDigest(HttpRequest request, out string digest)
    {
        ArgumentNullException.ThrowIfNull(request);
        digest = string.Empty;

        // Transport headers and the five CRT1 headers are not ingress metadata.
        // All other TagEkyc headers are forbidden, including caller-identity selectors.
        if (request.Headers.Keys.Any(name =>
                name.StartsWith("X-TagEkyc-", StringComparison.OrdinalIgnoreCase) &&
                !AllowedRuntimeHeaders.Contains(name) &&
                !string.Equals(name, "X-TagEkyc-Plaintext-Sha256", StringComparison.OrdinalIgnoreCase) &&
                !IngressMetadataHeaders.Contains(name, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        var values = new string[IngressMetadataHeaders.Length];
        for (var index = 0; index < IngressMetadataHeaders.Length; index++)
        {
            if (!TryGetSingleHeader(request, IngressMetadataHeaders[index], out values[index]))
            {
                return false;
            }
        }

        if (!IsCanonicalPositiveInteger(values[0], long.MaxValue) ||
            !IsCanonicalUuidV4(values[1]) ||
            !IsCanonicalUuidV4(values[2]) ||
            !IsCanonicalPositiveInteger(values[3], int.MaxValue) ||
            values[4] is not ("ChipDg2Portrait" or "LiveSelfieImage") ||
            !IsCanonicalUuidV4(values[5]) ||
            !IsCanonicalUtcOffsetTimestamp(values[6]) ||
            !IsCanonicalUtcOffsetTimestamp(values[7]) ||
            !IsCanonicalUtcOffsetTimestamp(values[8]) ||
            !IsCanonicalPositiveInteger(values[9], long.MaxValue))
        {
            return false;
        }

        // Compute only after the complete closed set passed validation. Never sort
        // received headers or normalize received values into a different request.
        var preimage = new StringBuilder();
        for (var index = 0; index < IngressMetadataHeaders.Length; index++)
        {
            preimage.Append(IngressMetadataHeaders[index]).Append('=').Append(values[index]).Append('\n');
        }
        digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(preimage.ToString()))).ToLowerInvariant();
        return true;
    }

    private static bool IsCanonicalPositiveInteger(string value, long maximum) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) &&
        parsed > 0 && parsed <= maximum &&
        string.Equals(value, parsed.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static bool IsCanonicalUuidV4(string value) =>
        Guid.TryParseExact(value, "N", out var parsed) &&
        string.Equals(value, parsed.ToString("N"), StringComparison.Ordinal) &&
        value[12] == '4' && value[16] is '8' or '9' or 'a' or 'b';

    private static bool IsCanonicalUtcOffsetTimestamp(string value) =>
        DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fffffffzzz",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) &&
        parsed.Offset == TimeSpan.Zero && value.EndsWith("+00:00", StringComparison.Ordinal);

    private static bool TryGetSingleHeader(HttpRequest request, string name, out string value)
    {
        value = string.Empty;
        if (!request.Headers.TryGetValue(name, out StringValues values) || values.Count != 1)
        {
            return false;
        }

        var candidate = values[0];
        if (string.IsNullOrEmpty(candidate) || candidate.Contains(',', StringComparison.Ordinal) ||
            !string.Equals(candidate, candidate.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        value = candidate;
        return true;
    }

    private static bool TryDecodeBase64Url(string value, int expectedBytes, out byte[] bytes)
    {
        bytes = [];
        var expectedTextLength = expectedBytes == 32 ? 43 : 86;
        if (value.Length != expectedTextLength || value.Any(character =>
                character is not (>= 'A' and <= 'Z') and not (>= 'a' and <= 'z') and
                not (>= '0' and <= '9') and not '-' and not '_'))
        {
            return false;
        }

        var padded = value.Replace('-', '+').Replace('_', '/') +
                     new string('=', (4 - value.Length % 4) % 4);
        try
        {
            bytes = Convert.FromBase64String(padded);
            var canonical = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (bytes.Length == expectedBytes && string.Equals(canonical, value, StringComparison.Ordinal))
            {
                return true;
            }
        }
        catch (FormatException)
        {
        }

        if (bytes.Length > 0)
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        }
        bytes = [];
        return false;
    }

    private static bool IsLowercaseSha256(string value) =>
        value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool IsLowercaseMediaType(string value) =>
        value.Length == 0 ||
        (string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal) &&
         value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '/' or '+' or '-' or '.'));
}
