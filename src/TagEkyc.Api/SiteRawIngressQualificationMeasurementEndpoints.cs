using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data.Common;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Api;

public static class SiteRawIngressQualificationMeasurementEndpoints
{
    public const string Scope = "capture.raw-export.site-qualification";
    public const string EnrollmentScope = "operator.site-qualification.enroll";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static IEndpointRouteBuilder MapSiteRawIngressQualificationMeasurementEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ekyc/site-transport-qualification/runs");
        group.MapPost("/synthetic-credentials", EnrollSyntheticCredentialAsync);
        group.MapPost("", RegisterAsync);
        group.MapGet("/{qualificationRunId:guid}", ReadRunAsync);
        group.MapPost("/{qualificationRunId:guid}/release", ReleaseAsync);
        group.MapPost("/{qualificationRunId:guid}/commit-acknowledgement", AcknowledgeCommitAsync);
        group.MapPost("/{qualificationRunId:guid}/agent-observation", RecordAgentAsync);
        return endpoints;
    }

    private static async Task<IResult> EnrollSyntheticCredentialAsync(HttpContext context,
        IApiKeyAuthenticator authenticator,
        ISiteRawIngressQualificationRunStore store,
        ICaptureRuntimeSiteTransportQualificationSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var authentication = await authenticator.AuthenticateAsync(
            context, EnrollmentScope, cancellationToken).ConfigureAwait(false);
        var actor = authentication.IsSuccess ? authentication.Value : null;
        var settings = settingsProvider.Current;
        if (actor is not { CallerCategory: AuthenticatedCallerCategory.OperatorAdmin } ||
            actor.ApiKeyId == Guid.Empty || settings is null)
            return Forbidden(context);
        var request = await ReadJsonAsync<SyntheticCredentialEnrollmentRequest>(context, cancellationToken);
        if (request is null || request.CredentialId == Guid.Empty ||
            request.CredentialGeneration < 1 || request.TtlSeconds is < 60 or > 86400)
            return Invalid(context);
        var now = DateTimeOffset.UtcNow;
        return await store.EnrollSyntheticCredentialAsync(new(settings.SiteId,
                settings.EndpointOrigin, settings.DeploymentRevision,
                request.CredentialId, request.CredentialGeneration,
                now.AddSeconds(request.TtlSeconds), actor.ApiKeyId), now,
                cancellationToken).ConfigureAwait(false)
            ? Results.NoContent() : Conflict(context);
    }

    private static async Task<IResult> RegisterAsync(HttpContext context,
        IApiKeyAuthenticator authenticator,
        ISiteRawIngressQualificationRunStore store,
        ICaptureRuntimeSiteTransportQualificationSettingsProvider settingsProvider,
        ICaptureRuntimeActivationEvidenceSealProvider sealProvider,
        CancellationToken cancellationToken)
    {
        var actor = await AuthenticateAsync(context, authenticator, cancellationToken);
        if (actor is null) return Forbidden(context);
        var seal = sealProvider.Current;
        var settings = settingsProvider.Current;
        if (seal is null || seal.ApprovedAuthorityOpenRowCount != 0 ||
            !seal.ApprovedSiteTransportQualificationRequired ||
            seal.ApprovedSiteTransportQualificationPolicyVersion !=
                CaptureRuntimeSiteTransportQualificationPolicy.CurrentVersion || settings is null)
            return Unavailable(context);

        var request = await ReadJsonAsync<RegisterRequest>(context, cancellationToken);
        if (request is null || request.QualificationSuiteId == Guid.Empty ||
            request.CredentialId == Guid.Empty ||
            request.CredentialGeneration < 1 || request.IngressIdempotencyKey == Guid.Empty ||
            !IsSha256(request.IngressMetadataSha256) || request.MediaType != "image/jpeg" ||
            request.ContentLength < 1 || !IsSha256(request.PlaintextSha256) ||
            request.TtlSeconds is < 10 or > 600 ||
            !Enum.TryParse<SiteRawIngressQualificationRunMode>(request.Mode, false, out var mode) ||
            !Enum.IsDefined(mode))
            return Invalid(context);
        var now = DateTimeOffset.UtcNow;
        try
        {
            var result = await store.RegisterAsync(new(request.QualificationSuiteId,
                settings.SiteId, settings.EndpointOrigin, settings.DeploymentRevision,
                new(request.CredentialId, request.CredentialGeneration,
                    request.IngressIdempotencyKey, request.IngressMetadataSha256,
                    request.MediaType, request.ContentLength, request.PlaintextSha256),
                mode, now.AddSeconds(request.TtlSeconds), actor.ApiKeyId),
                now, cancellationToken).ConfigureAwait(false);
            return result is null ? Unavailable(context) : Results.Ok(new
            {
                qualificationRunId = result.QualificationRunId,
                qualificationSuiteId = result.QualificationSuiteId,
                siteId = result.SiteId,
                endpointOrigin = result.EndpointOrigin,
                deploymentRevision = result.DeploymentRevision,
                expiresAtUtc = result.ExpiresAtUtc
            });
        }
        catch (Exception error) when (error is InvalidOperationException or DbException)
        {
            return Conflict(context);
        }
    }

    private static async Task<IResult> ReadRunAsync(HttpContext context, Guid qualificationRunId,
        IApiKeyAuthenticator authenticator, ISiteRawIngressQualificationRunStore store,
        ICaptureRuntimeSiteTransportQualificationSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var actor = await AuthenticateAsync(context, authenticator, cancellationToken);
        var access = Access(actor, settingsProvider.Current);
        if (access is null)
            return Forbidden(context);
        var report = await store.ReadAsync(qualificationRunId, DateTimeOffset.UtcNow, access, cancellationToken)
            .ConfigureAwait(false);
        return report is null ? Results.NotFound() : Results.Ok(new
        {
            report.QualificationRunId,
            report.QualificationSuiteId,
            report.SiteId,
            report.EndpointOrigin,
            report.DeploymentRevision,
            report.State,
            Mode = report.Mode.ToString(),
            report.CreatedAtUtc,
            report.ExpiresAtUtc,
            report.ConsumedAtUtc,
            report.BrokerHeldAtUtc,
            report.BrokerCommittedAtUtc,
            report.RawPostCount,
            report.ServerApplicationBodyReadsWhileBrokerHeld,
            report.ClientTransportEntryCount,
            report.ContentBytesCopied,
            report.AgentBodyBytesSentWhileBrokerHeld,
            report.ObservedContinue,
            report.ContinueObservedBeforeBrokerCommit,
            report.ApplicationPrebufferObserved,
            report.FinalResponseObserved,
            report.AgentObservationCompleted,
            report.ServerObservationCompleted,
            report.EvidenceComplete,
            report.HiddenRetryObserved,
            report.KestrelContinueRelayedAfterCommit
        });
    }

    private static async Task<IResult> ReleaseAsync(HttpContext context, Guid qualificationRunId,
        IApiKeyAuthenticator authenticator, ISiteRawIngressQualificationRunStore store,
        ICaptureRuntimeSiteTransportQualificationSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var actor = await AuthenticateAsync(context, authenticator, cancellationToken);
        var access = Access(actor, settingsProvider.Current);
        if (access is null)
            return Forbidden(context);
        return await store.ReleaseBrokerAsync(qualificationRunId, DateTimeOffset.UtcNow, access, cancellationToken)
            .ConfigureAwait(false) ? Results.NoContent() : Conflict(context);
    }

    private static async Task<IResult> RecordAgentAsync(HttpContext context, Guid qualificationRunId,
        IApiKeyAuthenticator authenticator, ISiteRawIngressQualificationRunStore store,
        ICaptureRuntimeSiteTransportQualificationSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var actor = await AuthenticateAsync(context, authenticator, cancellationToken);
        var access = Access(actor, settingsProvider.Current);
        if (access is null)
            return Forbidden(context);
        var request = await ReadJsonAsync<AgentObservationRequest>(context, cancellationToken);
        if (request is null || request.TransportEntryCount is < 0 or > 16 ||
            request.ContentBytesCopied is < 0 or > 2_147_483_647 ||
            request.BodyBytesSentWhileBrokerHeld is < 0 or > 2_147_483_647)
            return Invalid(context);
        var observation = new SiteRawIngressQualificationAgentObservation(
            request.TransportEntryCount, request.ContentBytesCopied,
            request.BodyBytesSentWhileBrokerHeld, request.ObservedContinue,
            request.ContinueObservedBeforeBrokerCommit, request.ApplicationPrebufferObserved,
            request.FinalResponseObserved);
        return await store.RecordAgentObservationAsync(qualificationRunId, observation,
            DateTimeOffset.UtcNow, access, cancellationToken).ConfigureAwait(false)
            ? Results.NoContent() : Conflict(context);
    }

    private static async Task<IResult> AcknowledgeCommitAsync(HttpContext context,
        Guid qualificationRunId, IApiKeyAuthenticator authenticator,
        ISiteRawIngressQualificationRunStore store,
        ICaptureRuntimeSiteTransportQualificationSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var actor = await AuthenticateAsync(context, authenticator, cancellationToken);
        var access = Access(actor, settingsProvider.Current);
        if (access is null)
            return Forbidden(context);
        return await store.AcknowledgeBrokerCommitAsync(qualificationRunId,
            DateTimeOffset.UtcNow, access, cancellationToken).ConfigureAwait(false)
            ? Results.NoContent() : Conflict(context);
    }

    private static async Task<AuthenticatedClientContext?> AuthenticateAsync(HttpContext context,
        IApiKeyAuthenticator authenticator, CancellationToken cancellationToken)
    {
        var result = await authenticator.AuthenticateAsync(context, Scope, cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess && result.Value is { CallerCategory: AuthenticatedCallerCategory.CaptureAgent }
            ? result.Value : null;
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpContext context, CancellationToken cancellationToken)
    {
        try { return await context.Request.ReadFromJsonAsync<T>(JsonOptions, cancellationToken); }
        catch (Exception error) when (error is JsonException or BadHttpRequestException) { return default; }
    }

    private static IResult Invalid(HttpContext context) => Error(context, 400, "SITE_QUALIFICATION_REQUEST_INVALID");
    private static IResult Forbidden(HttpContext context) => Error(context, 403, CaptureRuntimeErrorCodes.AccessDenied);
    private static IResult Conflict(HttpContext context) => Error(context, 409, "SITE_QUALIFICATION_RUN_CONFLICT");
    private static IResult Unavailable(HttpContext context) => Error(context, 503, CaptureRuntimeErrorCodes.NotReady);
    private static IResult Error(HttpContext context, int status, string code) => Results.Json(
        new { code, correlationId = context.TraceIdentifier }, statusCode: status);

    private static SiteRawIngressQualificationRunAccess? Access(AuthenticatedClientContext? actor,
        CaptureRuntimeSiteTransportQualificationSettings? settings) => actor is null || settings is null
        ? null : new(actor.ApiKeyId, settings.SiteId, settings.EndpointOrigin,
            settings.DeploymentRevision);

    private static bool IsSha256(string? value) => value is { Length: 64 } &&
        value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private sealed record RegisterRequest(Guid QualificationSuiteId, Guid CredentialId,
        long CredentialGeneration, Guid IngressIdempotencyKey, string IngressMetadataSha256,
        string MediaType, long ContentLength, string PlaintextSha256, string Mode, int TtlSeconds);
    private sealed record SyntheticCredentialEnrollmentRequest(Guid CredentialId,
        long CredentialGeneration, int TtlSeconds);
    private sealed record AgentObservationRequest(int TransportEntryCount,
        long ContentBytesCopied, long BodyBytesSentWhileBrokerHeld, bool ObservedContinue,
        bool ContinueObservedBeforeBrokerCommit,
        bool ApplicationPrebufferObserved, bool FinalResponseObserved);
}
