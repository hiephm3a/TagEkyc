using System.Security.Cryptography;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Api;

public static partial class CaptureRuntimeHttpRoutes
{
    public static IEndpointRouteBuilder MapCaptureRuntimeRotationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete",
            CompleteRuntimeRotationAsync).AddEndpointFilter<ClosedFailureFilter>();
        return endpoints;
    }

    private static async Task<IResult> CompleteRuntimeRotationAsync(HttpContext context, CancellationToken ct)
    {
        var request = context.Request;
        if (request.Headers.ContainsKey("X-TagEkyc-Api-Key") ||
            request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key")) return Denied(context);
        if (!TryRouteId(context, "rotationId", out var rotationId) ||
            !TryIdempotency(request, out var idempotencyKey)) return Invalid(context);
        using var body = await ReadBodyAsync<CaptureRuntimeRotationCompleteRequest>(request, 16384, ct);
        if (body is null) return Invalid(context);
        var bodyDigest = Convert.ToHexString(SHA256.HashData(body.Bytes.Span)).ToLowerInvariant();
        // R13 is an ordinary closed-JSON CRT1 operation: its binding line is empty.
        // RotationId is already in the exact path; business Idempotency-Key is
        // bound by the R13 fingerprint and the successor's ROTATE1 proof.
        if (!CaptureRuntimeCrt1RequestParser.TryCreate(request, "CredentialRotation", "application/json",
                body.Bytes.Length, bodyDigest, string.Empty, out var signedRequest)) return Invalid(context);

        // The service owns the ONLY R13 authentication/classification call. Calling
        // the ordinary runtime authenticator here would claim N a second time and
        // would incorrectly require the predecessor role for successor replay.
        var result = await context.RequestServices.GetRequiredService<ICaptureRuntimeRotationService>()
            .CompleteRotationAsync(signedRequest!, rotationId, body.Value, idempotencyKey, body.Bytes, ct);
        return Respond(context, result);
    }
}
