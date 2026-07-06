using Microsoft.AspNetCore.Http;
using TagEkyc.Api.LocalDev;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip04SessionApplicationTests
{
    [Fact]
    public void Localdev_auth_uses_tag_ekyc_api_key_header_only()
    {
        var authenticator = new LocalDevApiKeyAuthenticator(new LocalDevApiKeyValidator(new LocalDevRuntimePolicySource()));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer localdev-business-key";

        var missingHeader = authenticator.Authenticate(httpContext, "business.session.create");

        Assert.False(missingHeader.IsSuccess);
        Assert.Equal("INVALID_API_KEY", missingHeader.Error?.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, missingHeader.Error?.StatusCode);

        httpContext.Request.Headers[LocalDevApiKeyAuthenticator.HeaderName] = "localdev-business-key";

        var authenticated = authenticator.Authenticate(httpContext, "business.session.create");

        Assert.True(authenticated.IsSuccess);
        Assert.Equal(LocalDevRuntimePolicySource.BusinessClientId, authenticated.Value?.ClientApplicationId);
    }

    [Fact]
    public void Disabled_client_is_auth_failure()
    {
        var authenticator = new LocalDevApiKeyAuthenticator(new LocalDevApiKeyValidator(new LocalDevRuntimePolicySource()));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[LocalDevApiKeyAuthenticator.HeaderName] = "localdev-disabled-client-key";

        var result = authenticator.Authenticate(httpContext, "business.session.create");

        Assert.False(result.IsSuccess);
        Assert.Equal("CLIENT_APPLICATION_DISABLED", result.Error?.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Error?.StatusCode);
    }

    [Fact]
    public async Task Create_session_sets_created_state_not_available_result_and_audits_success()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(
            BusinessCaller(),
            StandardRequest("external-1"),
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Created, result.Value?.State);
        Assert.Equal(VerificationResultDto.NotAvailable, result.Value?.Result);
        Assert.Single(fixture.Audit.Events, audit => audit.EventType == "SESSION_CREATED");
    }

    [Fact]
    public async Task Duplicate_external_session_wins_over_idempotency_key()
    {
        var fixture = CreateFixture();
        var caller = BusinessCaller();
        var request = StandardRequest("duplicate-external-session");

        var first = await fixture.Service.CreateAsync(caller, request, "idem-1", CancellationToken.None);
        var second = await fixture.Service.CreateAsync(caller, request, "idem-1", CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("DUPLICATE_EXTERNAL_SESSION", second.Error?.Code);
        Assert.Equal(StatusCodes.Status409Conflict, second.Error?.StatusCode);
    }

    [Fact]
    public async Task Required_checks_reject_empty_duplicate_required_false_and_unauthorized_checks()
    {
        var fixture = CreateFixture();
        var caller = BusinessCaller();

        var empty = await fixture.Service.CreateAsync(
            caller,
            StandardRequest("empty") with { RequiredChecks = [] },
            cancellationToken: CancellationToken.None);

        var duplicate = await fixture.Service.CreateAsync(
            caller,
            StandardRequest("duplicate") with
            {
                RequiredChecks =
                [
                    RequiredCheck(RequiredCheckTypeDto.CaptureQuality),
                    RequiredCheck(RequiredCheckTypeDto.CaptureQuality),
                ],
            },
            cancellationToken: CancellationToken.None);

        var notRequired = await fixture.Service.CreateAsync(
            caller,
            StandardRequest("not-required") with
            {
                RequiredChecks = [RequiredCheck(RequiredCheckTypeDto.CaptureQuality, required: false)],
            },
            cancellationToken: CancellationToken.None);

        var unauthorized = await fixture.Service.CreateAsync(
            caller,
            StandardRequest("unauthorized") with
            {
                RequiredChecks = [RequiredCheck(RequiredCheckTypeDto.Fingerprint)],
            },
            cancellationToken: CancellationToken.None);

        Assert.Equal("INVALID_REQUIRED_CHECKS", empty.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, empty.Error?.StatusCode);
        Assert.Equal("INVALID_REQUIRED_CHECKS", duplicate.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, duplicate.Error?.StatusCode);
        Assert.Equal("INVALID_REQUIRED_CHECKS", notRequired.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, notRequired.Error?.StatusCode);
        Assert.Equal("INVALID_REQUIRED_CHECKS", unauthorized.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, unauthorized.Error?.StatusCode);
    }

    [Fact]
    public async Task Challenge_bound_profile_requires_opaque_challenge_and_echoes_values()
    {
        var fixture = CreateFixture();
        var caller = BusinessCaller();

        var missingChallenge = await fixture.Service.CreateAsync(
            caller,
            ChallengeBoundRequest() with { Challenge = null },
            cancellationToken: CancellationToken.None);

        var invalidControl = await fixture.Service.CreateAsync(
            caller,
            ChallengeBoundRequest() with { Challenge = "opaque\u007Fchallenge" },
            cancellationToken: CancellationToken.None);

        var tooLong = await fixture.Service.CreateAsync(
            caller,
            ChallengeBoundRequest() with { Challenge = new string('a', 129) },
            cancellationToken: CancellationToken.None);

        var conflictingFields = await fixture.Service.CreateAsync(
            caller,
            ChallengeBoundRequest() with { HasConflictingChallengeFields = true },
            cancellationToken: CancellationToken.None);

        var valid = await fixture.Service.CreateAsync(
            caller,
            ChallengeBoundRequest() with { Challenge = "opaque challenge, not a hash", ClientReference = "client-ref-1" },
            cancellationToken: CancellationToken.None);

        Assert.Equal("MISSING_CHALLENGE", missingChallenge.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, missingChallenge.Error?.StatusCode);
        Assert.Equal("INVALID_CHALLENGE", invalidControl.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, invalidControl.Error?.StatusCode);
        Assert.Equal("CHALLENGE_TOO_LONG", tooLong.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, tooLong.Error?.StatusCode);
        Assert.Equal("CONFLICTING_CHALLENGE_FIELDS", conflictingFields.Error?.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, conflictingFields.Error?.StatusCode);
        Assert.True(valid.IsSuccess);
        Assert.Equal("opaque challenge, not a hash", valid.Value?.Challenge);
        Assert.Equal("client-ref-1", valid.Value?.ClientReference);
    }

    [Fact]
    public async Task Get_session_returns_owner_summary_defaults_and_denies_other_client()
    {
        var fixture = CreateFixture();
        var create = await fixture.Service.CreateAsync(
            BusinessCaller(),
            StandardRequest("read-owned"),
            cancellationToken: CancellationToken.None);

        var own = await fixture.Service.GetSummaryAsync(
            BusinessCaller(),
            create.Value!.VerificationSessionId,
            CancellationToken.None);

        var otherClient = await fixture.Service.GetSummaryAsync(
            OtherBusinessCaller(),
            create.Value!.VerificationSessionId,
            CancellationToken.None);

        Assert.True(own.IsSuccess);
        Assert.Equal(AssuranceLevelDto.None, own.Value?.AssuranceLevel);
        Assert.Null(own.Value?.EvidencePackageId);
        Assert.Null(own.Value?.EvidencePackageHash);
        Assert.Null(own.Value?.CompletedAt);
        Assert.Equal(VerificationSessionStateDto.Created, own.Value?.State);
        Assert.Equal(VerificationResultDto.NotAvailable, own.Value?.Result);

        Assert.False(otherClient.IsSuccess);
        Assert.Equal("FORBIDDEN_CLIENT_APPLICATION", otherClient.Error?.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, otherClient.Error?.StatusCode);
        Assert.Contains(fixture.Audit.Events, audit => audit.EventType == "SESSION_ACCESS_DENIED");
    }

    [Fact]
    public async Task Get_does_not_mutate_expired_session()
    {
        var fixture = CreateFixture();
        var expiredSession = VerificationSession.Create(
            LocalDevRuntimePolicySource.BusinessClientId,
            "subject-expired",
            VerificationProfile.StandardEkycProfile,
            "PATIENT_REGISTRATION",
            [RequiredCheckType.CaptureQuality],
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(-30),
            externalSessionId: "expired-session");

        await fixture.Sessions.AddAsync(expiredSession, CancellationToken.None);

        var result = await fixture.Service.GetSummaryAsync(
            BusinessCaller(),
            expiredSession.Id.ToString("N"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationSessionStateDto.Created, result.Value?.State);
        Assert.Equal(VerificationResultDto.NotAvailable, result.Value?.Result);
    }

    private static TestFixture CreateFixture()
    {
        var sessions = new LocalDevInMemoryVerificationSessionRepository();
        var artifacts = new LocalDevInMemoryCaptureArtifactRepository();
        var evidence = new LocalDevInMemoryEvidenceResultRepository();
        var audit = new LocalDevInMemoryAuditEventRepository();
        var service = new VerificationSessionApplicationService(
            sessions,
            artifacts,
            evidence,
            audit,
            new LocalDevRuntimePolicySource());

        return new TestFixture(service, sessions, audit);
    }

    private static AuthenticatedClientContext BusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            LocalDevRuntimePolicySource.BusinessClientId,
            "ldev_biz",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read" });

    private static AuthenticatedClientContext OtherBusinessCaller() =>
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            LocalDevRuntimePolicySource.OtherBusinessClientId,
            "ldev_other",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.create", "business.session.read" });

    private static CreateVerificationSessionRequestDto StandardRequest(string externalSessionId) =>
        new(
            externalSessionId,
            "subject-ref",
            "PATIENT_REGISTRATION",
            VerificationProfileDto.StandardEkycProfile,
            [RequiredCheck(RequiredCheckTypeDto.CaptureQuality)],
            DateTimeOffset.UtcNow.AddMinutes(30),
            RequestId: "req-test",
            CorrelationId: "corr-test");

    private static CreateVerificationSessionRequestDto ChallengeBoundRequest() =>
        new(
            "transaction-session",
            "subject-ref",
            "SIGNING_AUTH",
            VerificationProfileDto.ChallengeBoundEkycProfile,
            [
                RequiredCheck(RequiredCheckTypeDto.CaptureQuality),
                RequiredCheck(RequiredCheckTypeDto.DocumentNfc),
                RequiredCheck(RequiredCheckTypeDto.FaceMatch),
                RequiredCheck(RequiredCheckTypeDto.Liveness),
            ],
            DateTimeOffset.UtcNow.AddMinutes(30),
            ClientReference: "sf-ref-1",
            Challenge: "opaque-challenge",
            RequestId: "req-test",
            CorrelationId: "corr-test");

    private static RequiredCheckRequestDto RequiredCheck(RequiredCheckTypeDto checkType, bool required = true) =>
        new(checkType, required, MinimumConfidence: null);

    private sealed record TestFixture(
        VerificationSessionApplicationService Service,
        LocalDevInMemoryVerificationSessionRepository Sessions,
        LocalDevInMemoryAuditEventRepository Audit);
}


