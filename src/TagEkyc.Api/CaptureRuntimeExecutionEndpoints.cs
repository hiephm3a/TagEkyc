using System.Security.Cryptography;
using System.Text.Json;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Api;

public static partial class CaptureRuntimeHttpRoutes
{
    // Host cutover owns registration: these producer routes are Activated-only.
    public static IEndpointRouteBuilder MapCaptureRuntimeExecutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/verification-sessions/{sessionId}/capture-capabilities", IssueRuntimeCapabilityAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/capture-runtime/executions/bind", BindRuntimeExecutionAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/capture-runtime/executions/reconcile", ReconcileRuntimeExecutionAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapGet("/api/ekyc/capture-runtime/self/configuration", ReadRuntimeConfigurationAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/capture-runtime/executions/{bindingId}/capture-artifacts", AppendRuntimeCaptureAsync).AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/verification-sessions/{id}/evidence-results", AppendRuntimeEvidenceAsync).AddEndpointFilter<ClosedFailureFilter>();
        return endpoints;
    }

    private static async Task<IResult> IssueRuntimeCapabilityAsync(HttpContext context, CancellationToken ct)
    {
        if (context.Request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key") ||
            context.Request.Headers.Keys.Any(x => x.StartsWith("X-TagEkyc-Capture-Runtime-", StringComparison.OrdinalIgnoreCase))) return Denied(context);
        var auth = await context.RequestServices.GetRequiredService<IApiKeyAuthenticator>().AuthenticateAsync(context, cancellationToken: ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (auth.Value!.CallerCategory != AuthenticatedCallerCategory.BusinessConsumer ||
            auth.Value.ClientApplicationId == Guid.Empty || auth.Value.PrincipalId == Guid.Empty) return Denied(context);
        if (!TryRouteId(context, "sessionId", out var session) || !TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadCapabilityBodyAsync(context.Request, ct);
        if (body is null) return Invalid(context);
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeExecutionService>()
            .IssueOrReplaceCapabilityAsync(auth.Value, session, body.Value, key, body.Bytes, ct), 201);
    }

    // CP11's three closed camelCase sets are wire grammar, not inferred from
    // the PascalCase CLR record or A1's generic required-member convention.
    private static async Task<Body<CaptureCapabilityRequest>?> ReadCapabilityBodyAsync(HttpRequest request, CancellationToken ct)
    {
        if (request.QueryString.HasValue || request.ContentType != "application/json" ||
            request.ContentLength is not > 0 or > 16384 ||
            request.Headers.ContainsKey("Transfer-Encoding") || request.Headers.ContainsKey("Content-Encoding")) return null;
        var bytes = new byte[(int)request.ContentLength.Value];
        var owned = false;
        try
        {
            await request.Body.ReadExactlyAsync(bytes, ct);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !MembersValid(root) ||
                !root.TryGetProperty("action", out var action) || action.ValueKind != JsonValueKind.String) return null;
            var count = root.EnumerateObject().Count();
            CaptureCapabilityRequest value;
            if (action.GetString() == "Issue" && count == 1) value = new("Issue");
            else if (action.GetString() == "Issue" && count == 2 &&
                root.TryGetProperty("consentBindingId", out var binding) && binding.ValueKind == JsonValueKind.String &&
                binding.Deserialize<Guid>(ClosedJson) is var bindingId && bindingId != Guid.Empty)
                value = new("Issue", ConsentBindingId: bindingId);
            else if (action.GetString() == "Replace" && count == 3 &&
                root.TryGetProperty("currentCapabilityId", out var current) && current.ValueKind == JsonValueKind.String &&
                current.Deserialize<Guid>(ClosedJson) is var currentId && currentId != Guid.Empty &&
                root.TryGetProperty("expectedRevision", out var revision) && revision.TryGetInt64(out var revisionValue) && revisionValue > 0)
                value = new("Replace", currentId, revisionValue);
            else return null;
            owned = true;
            return new(value, bytes);
        }
        catch (Exception error) when (error is JsonException or EndOfStreamException or FormatException or InvalidOperationException)
        { return null; }
        finally { if (!owned) CryptographicOperations.ZeroMemory(bytes); }
    }

    private static async Task<IResult> BindRuntimeExecutionAsync(HttpContext context, CancellationToken ct)
    {
        if (HasForeignRuntimeAuthentication(context.Request)) return Denied(context);
        if (!TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeBindRequest>(context.Request, 16384, ct);
        if (body is null || body.Value.BindOperationId != key || body.Value.CaptureCapabilityId == Guid.Empty) return Invalid(context);
        if (!TryDecodeBindingSecret(body.Value.CaptureCapabilitySecret, out var secret)) return Invalid(context);
        string binding;
        try
        {
            binding = $"CaptureCapabilityId={body.Value.CaptureCapabilityId:N};BindOperationId={key:N};SecretSha256={Convert.ToHexString(SHA256.HashData(secret)).ToLowerInvariant()}";
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
        var auth = await AuthenticateExecutionJsonAsync(context, "Bind", body.Bytes, binding, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeExecutionService>()
            .BindAsync(auth.Value!, body.Value, key, body.Bytes, ct), 201);
    }

    private static async Task<IResult> ReconcileRuntimeExecutionAsync(HttpContext context, CancellationToken ct)
    {
        if (HasForeignRuntimeAuthentication(context.Request)) return Denied(context);
        if (context.Request.Headers.ContainsKey("Idempotency-Key")) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeReconcileRequest>(context.Request, 16384, ct);
        if (body is null || body.Value.CaptureCapabilityId == Guid.Empty || body.Value.BindOperationId == Guid.Empty) return Invalid(context);
        var binding = $"CaptureCapabilityId={body.Value.CaptureCapabilityId:N};BindOperationId={body.Value.BindOperationId:N}";
        var auth = await AuthenticateExecutionJsonAsync(context, "Bind", body.Bytes, binding, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeExecutionService>()
            .ReconcileAsync(auth.Value!, body.Value, ct));
    }

    private static async Task<IResult> ReadRuntimeConfigurationAsync(HttpContext context, CancellationToken ct)
    {
        if (HasForeignRuntimeAuthentication(context.Request)) return Denied(context);
        if (!Bodyless(context.Request)) return Invalid(context);
        if (context.Request.Headers.ContainsKey("If-None-Match") &&
            !Single(context.Request, "If-None-Match", out _)) return Invalid(context);
        if (!CaptureRuntimeCrt1RequestParser.TryCreate(context.Request, "Configuration", string.Empty, 0,
                Convert.ToHexString(SHA256.HashData(Array.Empty<byte>())).ToLowerInvariant(), string.Empty, out var signed)) return Invalid(context);
        var auth = await context.RequestServices.GetRequiredService<ICaptureRuntimeRequestAuthenticator>().AuthenticateAsync(signed!, ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        // Even an exact conditional hit must pass current authentication, nonce
        // admission and configuration freshness resolution. There is no cache shortcut.
        var result = await context.RequestServices.GetRequiredService<ICaptureRuntimeExecutionService>().ResolveConfigurationAsync(auth.Value!, ct);
        if (!result.IsSuccess) return Respond(context, result);
        var (bytes, etag) = CaptureRuntimeExecutionApplicationService.SerializeConfiguration(result.Value!);
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "private, no-cache";
        return string.Equals(context.Request.Headers.IfNoneMatch.ToString(), etag, StringComparison.Ordinal)
            ? Results.StatusCode(304)
            : Results.Bytes(bytes, "application/json");
    }

    private static async Task<IResult> AppendRuntimeCaptureAsync(HttpContext context, CancellationToken ct)
    {
        if (HasForeignRuntimeAuthentication(context.Request)) return Denied(context);
        if (!TryRouteId(context, "bindingId", out var bindingId) || !TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeCaptureArtifactRequest>(context.Request, 16384, ct);
        if (body is null || body.Value.BindingId != bindingId || body.Value.Payload is null) return Invalid(context);
        var auth = await AuthenticateExecutionJsonAsync(context, "CaptureObservation", body.Bytes,
            $"BindingId={bindingId:N};IdempotencyKey={key:N}", ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeExecutionService>()
            .AppendCaptureArtifactAsync(auth.Value!, bindingId, body.Value, key, ct));
    }

    private static async Task<IResult> AppendRuntimeEvidenceAsync(HttpContext context, CancellationToken ct)
    {
        if (HasForeignRuntimeAuthentication(context.Request)) return Denied(context);
        if (!TryRouteId(context, "id", out var sessionId) || !TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeEvidenceResultRequest>(context.Request, 16384, ct);
        if (body is null || body.Value.BindingId == Guid.Empty || body.Value.Payload is null) return Invalid(context);
        var auth = await AuthenticateExecutionJsonAsync(context, "TrustedEvidence", body.Bytes,
            $"BindingId={body.Value.BindingId:N};IdempotencyKey={key:N}", ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        // Pass the closed runtime contract unchanged; no legacy producer DTO or
        // fabricated AuthenticatedClientContext is constructed at this boundary.
        return Respond(context, await context.RequestServices.GetRequiredService<ICaptureRuntimeExecutionService>()
            .AppendEvidenceResultAsync(auth.Value!, sessionId, body.Value, key, ct));
    }

    private static bool HasForeignRuntimeAuthentication(HttpRequest request) =>
        request.Headers.ContainsKey("X-TagEkyc-Api-Key") || request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key");

    private static async Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateExecutionJsonAsync(
        HttpContext context, string role, ReadOnlyMemory<byte> bytes, string binding, CancellationToken ct)
    {
        var digest = Convert.ToHexString(SHA256.HashData(bytes.Span)).ToLowerInvariant();
        if (!CaptureRuntimeCrt1RequestParser.TryCreate(context.Request, role, "application/json", bytes.Length, digest, binding, out var signed))
            return SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Invalid request.", 400);
        return await context.RequestServices.GetRequiredService<ICaptureRuntimeRequestAuthenticator>().AuthenticateAsync(signed!, ct);
    }

    private static bool TryDecodeBindingSecret(string? encoded, out byte[] secret)
    {
        secret = [];
        if (encoded is null || encoded.Length != 43 || encoded.Any(c => !(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))) return false;
        try
        {
            secret = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + "=");
            if (secret.Length == 32 && Convert.ToBase64String(secret).TrimEnd('=').Replace('+', '-').Replace('/', '_') == encoded) return true;
            CryptographicOperations.ZeroMemory(secret);
            secret = [];
            return false;
        }
        catch (FormatException) { return false; }
    }
}
