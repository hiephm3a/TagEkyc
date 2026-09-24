using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Api;

public static partial class RawExportSourceIngressEndpoints
{
    public const string Scope = "capture.raw-export.source.ingress";

    // Selected by the Activated route set only; never map alongside the legacy route.
    public static IEndpointRouteBuilder MapCaptureRuntimeRawIngressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/raw-export/source-ingress", RuntimeIngressAsync);
        return endpoints;
    }

    private static async Task<IResult> RuntimeIngressAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        context.Response.Headers.CacheControl = "no-store";
        var siteGate = context.RequestServices
            .GetService<ISiteRawIngressTransportQualificationRuntimeGate>();
        var site = siteGate?.Evaluate(DateTimeOffset.UtcNow);
        if (site is null || !site.AllowsRawIngress)
            return RuntimeError(context, 503,
                string.IsNullOrWhiteSpace(site?.Code)
                    ? CaptureRuntimeSiteTransportQualificationPolicy.InvalidCode : site.Code);
        if (request.Headers.ContainsKey("X-TagEkyc-Api-Key") ||
            request.Headers.ContainsKey("X-TagEkyc-Platform-Operator-Key"))
            return RuntimeError(context, 403, CaptureRuntimeErrorCodes.AccessDenied);

        if (!TryParse(request, out var metadata) ||
            !CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(request, out var digest))
            return RuntimeError(context, 400, CaptureRuntimeErrorCodes.RequestInvalid);

        if (!CaptureRuntimeCrt1RequestParser.TryCreate(request, "RawIngress", metadata.MediaType,
                metadata.ClaimedPlaintextLength, metadata.ClaimedPlaintextDigest,
                "ingress=IngressMetadataSha256=" + digest, out var signed))
            return RuntimeError(context, 403, CaptureRuntimeErrorCodes.AccessDenied);

        try
        {
            // Dependency construction can itself fail; keep it in the closed 503 boundary.
            // Do not construct a body reader or invoke A3 while resolving services.
            var authenticator = context.RequestServices.GetService<ICaptureRuntimeRequestAuthenticator>();
            var admission = context.RequestServices.GetService<ICaptureRuntimeRawIngressAdmission>();
            if (authenticator is null || admission is null)
                return RuntimeError(context, 503, CaptureRuntimeErrorCodes.NotReady);

            var authentication = await authenticator.AuthenticateAsync(signed!, cancellationToken);
            if (!authentication.IsSuccess)
                return authentication.Error?.StatusCode == 503
                    ? RuntimeError(context, 503, CaptureRuntimeErrorCodes.NotReady)
                    : RuntimeError(context, 403, CaptureRuntimeErrorCodes.AccessDenied);

            // AuthenticateAsync returns only after its nonce transaction has committed.
            // No Binding/session/acceptance lookup belongs to this boundary.
            var actor = authentication.Value!;
            if (request.Headers.ContainsKey("Content-Encoding") || request.Headers.ContainsKey("Trailer"))
                return MapRuntimeResult(context, new(
                    CaptureRuntimeRawIngressOutcome.TransportProtocolInvalid,
                    null, null, null, null));
            var handoff = new CaptureRuntimeRawIngressAdmissionContext(
                actor.CaptureAgentId, actor.DeviceInstallationId, actor.CredentialId,
                actor.CredentialGeneration, actor.RolePolicyId, actor.RolePolicyRevision,
                actor.SignedAtUtc, actor.Nonce.ToArray(), actor.SignedEnvelopeFingerprint.ToArray(),
                metadata.AgentConfigurationRevision, metadata.VerificationSessionId,
                metadata.CaptureArtifactId, metadata.CaptureRevision, metadata.RawClass,
                metadata.IngressIdempotencyKey, metadata.MediaType, metadata.ClaimedPlaintextLength,
                metadata.ClaimedPlaintextDigest, metadata.CapturedAtUtc,
                metadata.PlaintextRetentionStartedAtUtc, metadata.PlaintextRetentionExpiresAtUtc,
                metadata.PlaintextRetentionBudgetSeconds);
            var result = await admission.AdmitAsync(handoff, request.Body, cancellationToken);
            return MapRuntimeResult(context, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // No exception detail, body consumption or retry of the A3 port.
            return RuntimeError(context, 503, CaptureRuntimeErrorCodes.NotReady);
        }
    }

    private static IResult MapRuntimeResult(HttpContext context, CaptureRuntimeRawIngressAdmissionResult result)
    {
        if (result is null) return RuntimeError(context, 503, CaptureRuntimeErrorCodes.NotReady);
        var empty = result.SourceArtifactId is null && result.CurrentSourceState is null &&
                    result.CurrentDisposition is null && result.RetryNotBeforeUtc is null;
        (int Status, string Code)? mapping = result.Outcome switch
        {
            CaptureRuntimeRawIngressOutcome.Available when result.SourceArtifactId is { } id && id != Guid.Empty &&
                result.CurrentSourceState == "Available" && result.CurrentDisposition == "Available" && result.RetryNotBeforeUtc is null =>
                (200, RawExportSourceIngressCodes.Available),
            CaptureRuntimeRawIngressOutcome.AlreadyAvailable when result.SourceArtifactId is { } id && id != Guid.Empty &&
                result.CurrentSourceState == "Available" && result.CurrentDisposition == "Available" && result.RetryNotBeforeUtc is null =>
                (200, RawExportSourceIngressCodes.AlreadyAvailable),
            CaptureRuntimeRawIngressOutcome.EvaluationInProgress when result.SourceArtifactId is null &&
                result.CurrentSourceState is null && result.CurrentDisposition is null &&
                result.RetryNotBeforeUtc is { Offset: { } offset } && offset == TimeSpan.Zero =>
                (409, RawExportSourceIngressCodes.EvaluationInProgress),
            CaptureRuntimeRawIngressOutcome.BindingInvalid when empty => (403, RawExportSourceIngressCodes.BindingInvalid),
            CaptureRuntimeRawIngressOutcome.NotFoundOrNotAllowed when empty => (403, RawExportSourceIngressCodes.NotFoundOrNotAllowed),
            CaptureRuntimeRawIngressOutcome.TransportProtocolInvalid when empty => (400, RawExportSourceIngressCodes.TransportProtocolInvalid),
            CaptureRuntimeRawIngressOutcome.CapabilityUnavailable when empty => (503, RawExportSourceIngressCodes.CapabilityUnavailable),
            CaptureRuntimeRawIngressOutcome.ArtifactSizeLimitExceeded when empty => (413, RawExportSourceIngressCodes.ArtifactSizeLimitExceeded),
            CaptureRuntimeRawIngressOutcome.PlaintextRetentionInvalid when empty => (422, RawExportSourceIngressCodes.PlaintextRetentionInvalid),
            CaptureRuntimeRawIngressOutcome.CapacityUnavailable when empty => (503, RawExportSourceIngressCodes.CapacityUnavailable),
            CaptureRuntimeRawIngressOutcome.IdempotencyBusy when empty => (409, RawExportSourceIngressCodes.IdempotencyBusy),
            CaptureRuntimeRawIngressOutcome.ClaimTokenInvalid when empty => (403, RawExportSourceIngressCodes.ClaimTokenInvalid),
            CaptureRuntimeRawIngressOutcome.ClaimRestartRequired when empty => (409, RawExportSourceIngressCodes.ClaimRestartRequired),
            CaptureRuntimeRawIngressOutcome.SourceRetentionNotAuthorized when empty => (403, RawExportSourceIngressCodes.SourceRetentionNotAuthorized),
            CaptureRuntimeRawIngressOutcome.HistoricCommitmentKeyUnavailable when empty => (503, RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable),
            CaptureRuntimeRawIngressOutcome.FingerprintConflict when empty => (409, RawExportSourceIngressCodes.FingerprintConflict),
            CaptureRuntimeRawIngressOutcome.ReservationBusy when empty => (409, RawExportSourceIngressCodes.ReservationBusy),
            CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable when empty => (503, RawExportSourceIngressCodes.TemporarilyUnavailable),
            CaptureRuntimeRawIngressOutcome.ContentCommitmentMismatch when empty => (422, RawExportSourceIngressCodes.ContentCommitmentMismatch),
            CaptureRuntimeRawIngressOutcome.RecaptureRequired when empty => (409, RawExportSourceIngressCodes.RecaptureRequired),
            CaptureRuntimeRawIngressOutcome.ResumePending when empty => (202, RawExportSourceIngressCodes.ResumePending),
            _ => null
        };
        if (mapping is not { } mapped)
            return RuntimeError(context, 503, CaptureRuntimeErrorCodes.NotReady);
        return FixedRawJson(new CaptureAgentFinalResult(mapped.Code, result.SourceArtifactId,
            result.CurrentSourceState, result.CurrentDisposition, result.RetryNotBeforeUtc), mapped.Status);
    }

    private static IResult RuntimeError(HttpContext context, int status, string code) =>
        FixedRawJson(new { code, correlationId = context.TraceIdentifier }, status);

    // The strict one-operation Agent reads a bounded, fixed-length final response.
    // Keep ASP.NET's configured JSON options and existing wire fields/statuses.
    private static IResult FixedRawJson<T>(T value, int status) => new FixedRawJsonResult<T>(value, status);

    private sealed class FixedRawJsonResult<T>(T value, int status) : IResult
    {
        public async Task ExecuteAsync(HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
            // A3 O/E/S forbids explicit null optional members. Preserve the
            // host's JSON options, but scope omission to this business DTO.
            var writerOptions = value is CaptureAgentFinalResult
                ? new JsonSerializerOptions(options)
                    { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
                : options;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, writerOptions);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength = bytes.Length;
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        }
    }

    public static IEndpointRouteBuilder MapRawExportSourceIngressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/raw-export/source-ingress", IngressAsync);
        return endpoints;
    }

    private static async Task<IResult> IngressAsync(
        HttpContext context,
        IApiKeyAuthenticator authenticator,
        CancellationToken cancellationToken)
    {
        var authentication = await authenticator.AuthenticateAsync(context, Scope, cancellationToken);
        if (!authentication.IsSuccess) return Error(authentication.Error!.StatusCode, authentication.Error.Code);
        var caller = authentication.Value!;
        if (caller.CallerCategory != AuthenticatedCallerCategory.CaptureAgent)
            return Error(StatusCodes.Status403Forbidden, "ACCESS_DENIED");

        if (!TryParse(context.Request, out var metadata))
            return Results.Json(new CaptureAgentFinalResult(RawExportSourceIngressCodes.BindingInvalid));

        RawExportSourceIngressApplicationService service;
        try
        {
            service = context.RequestServices.GetRequiredService<RawExportSourceIngressApplicationService>();
        }
        catch (Exception)
        {
            // Dependency construction only: no business execution, R1 or body
            // consumption has occurred. Do not remap post-R1 pipeline failures.
            return Results.Json(new CaptureAgentFinalResult("RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE"),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await service.ExecuteAsync(caller, metadata, () => context.Request.Body, cancellationToken);
        return Results.Ok(result);
    }

    internal static bool TryParse(HttpRequest request, out RawExportSourceIngressMetadata metadata)
    {
        metadata = null!;
        if (request.Headers.ContainsKey("X-TagEkyc-Client-Application-Id") ||
            request.Headers.ContainsKey("X-TagEkyc-Producer-Id") ||
            request.Headers.ContainsKey("X-TagEkyc-Capture-Agent-Instance-Id") ||
            request.ContentLength is not > 0 || request.Headers.ContainsKey("Transfer-Encoding") ||
            request.Headers.ContainsKey("Transfer-Encoding")) return false;

        if (!One(request, "X-TagEkyc-Agent-Configuration-Revision", out var configText) || !long.TryParse(configText, NumberStyles.None, CultureInfo.InvariantCulture, out var configRevision) || configRevision <= 0 ||
            !One(request, "X-TagEkyc-Verification-Session-Id", out var sessionText) || !Guid.TryParse(sessionText, out var session) ||
            !One(request, "X-TagEkyc-Capture-Artifact-Id", out var artifactText) || !Guid.TryParse(artifactText, out var artifact) ||
            !One(request, "X-TagEkyc-Capture-Revision", out var revisionText) || !int.TryParse(revisionText, NumberStyles.None, CultureInfo.InvariantCulture, out var revision) || revision <= 0 ||
            !One(request, "X-TagEkyc-Raw-Class", out var rawClass) || !CanonicalRawClass().IsMatch(rawClass) ||
            !One(request, "Idempotency-Key", out var keyText) || !CanonicalUuid().IsMatch(keyText) || !Guid.TryParseExact(keyText, "N", out var key) || key == Guid.Empty || key.ToByteArray()[7] >> 4 != 4 || (key.ToByteArray()[8] & 0xc0) != 0x80 ||
            !One(request, "X-TagEkyc-Plaintext-Sha256", out var digest) || !LowerSha256().IsMatch(digest) ||
            !One(request, "X-TagEkyc-Captured-At-Utc", out var capturedText) || !DateTimeOffset.TryParseExact(capturedText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var captured) || captured.Offset != TimeSpan.Zero ||
            !One(request, "X-TagEkyc-Retention-Started-At-Utc", out var startedText) || !DateTimeOffset.TryParseExact(startedText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var started) || started.Offset != TimeSpan.Zero ||
            !One(request, "X-TagEkyc-Retention-Expires-At-Utc", out var expiresText) || !DateTimeOffset.TryParseExact(expiresText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expires) || expires.Offset != TimeSpan.Zero ||
            !One(request, "X-TagEkyc-Retention-Budget-Seconds", out var budgetText) || !long.TryParse(budgetText, NumberStyles.None, CultureInfo.InvariantCulture, out var budget) || budget <= 0 ||
            !string.Equals(request.ContentType, "image/jpeg", StringComparison.Ordinal)) return false;

        metadata = new(Guid.Empty, string.Empty, string.Empty, configRevision, session, artifact, revision, rawClass, key,
            request.ContentType!, request.ContentLength.Value, digest, captured, started, expires, budget);
        return true;
    }

    private static bool One(HttpRequest request, string name, out string value)
    {
        StringValues values = request.Headers[name];
        value = values.Count == 1 ? values[0]! : string.Empty;
        return values.Count == 1;
    }

    private static IResult Error(int status, string code) => Results.Json(new { error = new { code } }, statusCode: status);

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)] private static partial Regex CanonicalUuid();
    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)] private static partial Regex LowerSha256();
    [GeneratedRegex("^[A-Z][A-Za-z0-9]{0,63}$", RegexOptions.CultureInvariant)] private static partial Regex CanonicalRawClass();
}
