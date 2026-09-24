using System.Security.Cryptography;
using System.Text.Json;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Api;

public static partial class CaptureRuntimeHttpRoutes
{
    public static IEndpointRouteBuilder MapRawSourceConsentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/verification-sessions/{sessionId}/source-consent-reference", RecordSourceConsentAsync)
            .AddEndpointFilter<ClosedFailureFilter>();
        endpoints.MapPost("/api/ekyc/source-consent-references/{referenceId}/withdraw", WithdrawSourceConsentAsync)
            .AddEndpointFilter<ClosedFailureFilter>();
        return endpoints;
    }

    private static async Task<IResult> RecordSourceConsentAsync(HttpContext context, CancellationToken ct)
    {
        if (SourceConsentForeignAuthentication(context.Request)) return Denied(context);
        var auth = await context.RequestServices.GetRequiredService<IApiKeyAuthenticator>().AuthenticateAsync(context, cancellationToken: ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (auth.Value!.CallerCategory != AuthenticatedCallerCategory.BusinessConsumer ||
            auth.Value.PrincipalId == Guid.Empty || auth.Value.ClientApplicationId == Guid.Empty) return Denied(context);
        if (!TryRouteId(context, "sessionId", out var session) || !TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadSourceConsentBody<E01RecordRequest>(context.Request,
            ["externalConsentArtifactRef", "sourceVersion", "expectedReferenceRevision", "consentTextVersion", "consentTextContentHash", "validFromUtc", "validUntilUtc"], ct);
        if (body is null) return Invalid(context);
        return Respond(context, await context.RequestServices.GetRequiredService<IRawSourceConsentService>()
            .RecordAsync(auth.Value, session, body.Value, key, body.Bytes, ct));
    }

    private static async Task<IResult> WithdrawSourceConsentAsync(HttpContext context, CancellationToken ct)
    {
        if (SourceConsentForeignAuthentication(context.Request)) return Denied(context);
        var auth = await context.RequestServices.GetRequiredService<IApiKeyAuthenticator>().AuthenticateAsync(context, cancellationToken: ct);
        if (!auth.IsSuccess) return Respond(context, auth);
        if (auth.Value!.CallerCategory != AuthenticatedCallerCategory.BusinessConsumer ||
            auth.Value.PrincipalId == Guid.Empty || auth.Value.ClientApplicationId == Guid.Empty) return Denied(context);
        if (!TryRouteId(context, "referenceId", out var reference) || !TryIdempotency(context.Request, out var key)) return Invalid(context);
        using var body = await ReadSourceConsentBody<E01WithdrawRequest>(context.Request,
            ["expectedReferenceRevision", "sourceVersion", "decisionRef"], ct);
        if (body is null) return Invalid(context);
        return Respond(context, await context.RequestServices.GetRequiredService<IRawSourceConsentService>()
            .WithdrawAsync(auth.Value, reference, body.Value, key, body.Bytes, ct));
    }

    private static bool SourceConsentForeignAuthentication(HttpRequest request) =>
        request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key") ||
        request.Headers.Keys.Any(x => x.StartsWith("X-TagEkyc-Capture-Runtime-", StringComparison.OrdinalIgnoreCase));

    // E01 has a fixed camelCase grammar. Reuse control JSON/type converters, but
    // do not change A1's PascalCase RequiredMembersPresent convention globally.
    private static async Task<Body<T>?> ReadSourceConsentBody<T>(HttpRequest request, string[] members, CancellationToken ct) where T : class
    {
        if (request.QueryString.HasValue || request.ContentType != "application/json" || request.ContentLength is not > 0 ||
            request.ContentLength > 8192 || request.Headers.ContainsKey("Transfer-Encoding") || request.Headers.ContainsKey("Content-Encoding"))
            return null;
        var bytes = new byte[(int)request.ContentLength.Value];
        try
        {
            await request.Body.ReadExactlyAsync(bytes, ct);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !MembersValid(root) ||
                root.EnumerateObject().Count() != members.Length ||
                members.Any(name => !root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null))
            { CryptographicOperations.ZeroMemory(bytes); return null; }
            var value = JsonSerializer.Deserialize<T>(bytes, ClosedJson);
            if (value is null) { CryptographicOperations.ZeroMemory(bytes); return null; }
            return new(value, bytes);
        }
        catch (Exception error) when (error is JsonException or EndOfStreamException or FormatException)
        { CryptographicOperations.ZeroMemory(bytes); return null; }
        catch { CryptographicOperations.ZeroMemory(bytes); throw; }
    }
}
