using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.IntegrationTests;

public sealed class SiteQualificationMeasurementAdminEndpointTests
{
    [Fact]
    public async Task Synthetic_credential_enrollment_requires_operator_authority_and_binds_server_site()
    {
        var store = new Store();
        var authenticator = new Authenticator(true, false, category: AuthenticatedCallerCategory.OperatorAdmin);
        await using var app = await StartAsync(store, authenticator);
        using var client = app.GetTestClient();
        var credential = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/ekyc/site-transport-qualification/runs/synthetic-credentials", new
            {
                credentialId = credential,
                credentialGeneration = 7,
                ttlSeconds = 300
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(SiteRawIngressQualificationMeasurementEndpoints.EnrollmentScope,
            authenticator.RequiredScope);
        Assert.Equal(1, store.EnrollmentCalls);
        Assert.Equal(credential, store.Enrollment!.CredentialId);
        Assert.Equal("integration-site", store.Enrollment.SiteId);
        Assert.Equal("integration-deployment-1", store.Enrollment.DeploymentRevision);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Registration_requires_dedicated_scope_and_capture_agent_category(
        bool hasScope, bool captureAgent)
    {
        var store = new Store();
        var authenticator = new Authenticator(hasScope, captureAgent);
        await using var app = await StartAsync(store, authenticator);
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync(
            "/api/ekyc/site-transport-qualification/runs", new
            {
                qualificationSuiteId = Guid.NewGuid(),
                credentialId = Guid.NewGuid(),
                credentialGeneration = 3,
                ingressIdempotencyKey = Guid.NewGuid(),
                ingressMetadataSha256 = new string('a', 64),
                mediaType = "image/jpeg",
                contentLength = 19,
                plaintextSha256 = new string('b', 64),
                mode = "FullBodyHeldCommit",
                ttlSeconds = 60
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(SiteRawIngressQualificationMeasurementEndpoints.Scope,
            authenticator.RequiredScope);
        Assert.Equal(0, store.RegisterCalls);
    }

    [Fact]
    public async Task Registration_binds_server_site_and_returns_no_subject_or_biometric_identity()
    {
        var store = new Store();
        var authenticator = new Authenticator(true, true);
        await using var app = await StartAsync(store, authenticator);
        using var client = app.GetTestClient();
        var credential = Guid.NewGuid();
        var idempotency = Guid.NewGuid();
        var suite = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            "/api/ekyc/site-transport-qualification/runs", new
            {
                qualificationSuiteId = suite,
                credentialId = credential,
                credentialGeneration = 3,
                ingressIdempotencyKey = idempotency,
                ingressMetadataSha256 = new string('a', 64),
                mediaType = "image/jpeg",
                contentLength = 19,
                plaintextSha256 = new string('b', 64),
                mode = "FullBodyHeldCommit",
                ttlSeconds = 60
            });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, store.RegisterCalls);
        Assert.Equal("integration-site", store.Registration!.SiteId);
        Assert.Equal("https://127.0.0.1:8443", store.Registration.EndpointOrigin);
        Assert.Equal("integration-deployment-1", store.Registration.DeploymentRevision);
        Assert.Equal(credential, store.Registration.Binding.CredentialId);
        Assert.Equal(idempotency, store.Registration.Binding.IngressIdempotencyKey);
        Assert.Equal(suite, store.Registration.QualificationSuiteId);
        Assert.DoesNotContain("subject", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("session", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("artifact", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("biometric", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Every_control_operation_carries_the_authenticated_api_key_owner()
    {
        var owner = Guid.Parse("10000000-0000-4000-8000-000000000001");
        var otherOwner = Guid.Parse("10000000-0000-4000-8000-000000000002");
        var store = new Store { ExpectedOwner = owner };
        var authenticator = new Authenticator(true, true, otherOwner);
        await using var app = await StartAsync(store, authenticator);
        using var client = app.GetTestClient();
        var runId = Guid.NewGuid();

        using var read = await client.GetAsync($"/api/ekyc/site-transport-qualification/runs/{runId}");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(otherOwner, store.LastAccess!.ApiKeyId);

        using var release = await client.PostAsync(
            $"/api/ekyc/site-transport-qualification/runs/{runId}/release", null);
        Assert.Equal(HttpStatusCode.Conflict, release.StatusCode);
        Assert.Equal(otherOwner, store.LastAccess!.ApiKeyId);

        using var acknowledgement = await client.PostAsync(
            $"/api/ekyc/site-transport-qualification/runs/{runId}/commit-acknowledgement", null);
        Assert.Equal(HttpStatusCode.Conflict, acknowledgement.StatusCode);
        Assert.Equal(otherOwner, store.LastAccess!.ApiKeyId);

        using var observation = await client.PostAsJsonAsync(
            $"/api/ekyc/site-transport-qualification/runs/{runId}/agent-observation", new
            {
                transportEntryCount = 1,
                contentBytesCopied = 19,
                bodyBytesSentWhileBrokerHeld = 0,
                observedContinue = true,
                continueObservedBeforeBrokerCommit = false,
                applicationPrebufferObserved = false,
                finalResponseObserved = true
            });
        Assert.Equal(HttpStatusCode.Conflict, observation.StatusCode);
        Assert.Equal(otherOwner, store.LastAccess!.ApiKeyId);
    }

    private static async Task<WebApplication> StartAsync(Store store, Authenticator authenticator)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiKeyAuthenticator>(authenticator);
        builder.Services.AddSingleton<ISiteRawIngressQualificationRunStore>(store);
        builder.Services.AddSingleton<ICaptureRuntimeSiteTransportQualificationSettingsProvider>(
            new ActivationEvidenceTestSeals.QualificationSettingsProvider());
        builder.Services.AddSingleton<ICaptureRuntimeActivationEvidenceSealProvider>(
            new ActivationEvidenceTestSeals.Provider(ActivationEvidenceTestSeals.Valid(0)));
        var app = builder.Build();
        app.MapSiteRawIngressQualificationMeasurementEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class Authenticator(bool hasScope, bool captureAgent,
        Guid apiKeyId = default, AuthenticatedCallerCategory? category = null) : IApiKeyAuthenticator
    {
        internal string? RequiredScope;
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext httpContext, string? requiredScope = null,
            CancellationToken cancellationToken = default)
        {
            RequiredScope = requiredScope;
            var expectedScope = category == AuthenticatedCallerCategory.OperatorAdmin
                ? SiteRawIngressQualificationMeasurementEndpoints.EnrollmentScope
                : SiteRawIngressQualificationMeasurementEndpoints.Scope;
            if (!hasScope || requiredScope != expectedScope)
                return Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Failure(
                    "ACCESS_DENIED", "Denied.", 403));
            return Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(
                apiKeyId == Guid.Empty ? Guid.Parse("10000000-0000-4000-8000-000000000001") : apiKeyId,
                Guid.NewGuid(), "qualification",
                category ?? (captureAgent ? AuthenticatedCallerCategory.CaptureAgent : AuthenticatedCallerCategory.BusinessConsumer),
                new HashSet<string> { expectedScope })));
        }
    }

    private sealed class Store : ISiteRawIngressQualificationRunStore
    {
        internal int RegisterCalls;
        internal int EnrollmentCalls;
        internal SiteRawIngressQualificationSyntheticCredentialEnrollment? Enrollment;
        internal SiteRawIngressQualificationRunRegistration? Registration;
        internal Guid? ExpectedOwner;
        internal SiteRawIngressQualificationRunAccess? LastAccess;
        public Task<bool> EnrollSyntheticCredentialAsync(
            SiteRawIngressQualificationSyntheticCredentialEnrollment enrollment,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            EnrollmentCalls++;
            Enrollment = enrollment;
            return Task.FromResult(true);
        }
        public Task<SiteRawIngressQualificationRunHandle?> RegisterAsync(
            SiteRawIngressQualificationRunRegistration registration, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RegisterCalls++;
            Registration = registration;
            return Task.FromResult<SiteRawIngressQualificationRunHandle?>(new(
                Guid.Parse("20000000-0000-4000-8000-000000000001"),
                registration.QualificationSuiteId, registration.SiteId,
                registration.EndpointOrigin, registration.DeploymentRevision,
                registration.ExpiresAtUtc));
        }
        public Task<SiteRawIngressQualificationRawPost?> ObserveRawPostAsync(
            CaptureRuntimeSiteTransportQualificationSettings settings,
            SiteRawIngressQualificationRunBinding binding, DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ConsumeAuthenticatedAsync(Guid qualificationRunId,
            SiteRawIngressQualificationRunBinding binding, DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ReleaseBrokerAsync(Guid qualificationRunId, DateTimeOffset now,
            SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken)
        {
            LastAccess = access;
            return Task.FromResult(access.ApiKeyId == ExpectedOwner);
        }
        public Task<bool> AcknowledgeBrokerCommitAsync(Guid qualificationRunId, DateTimeOffset now,
            SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken)
        {
            LastAccess = access;
            return Task.FromResult(access.ApiKeyId == ExpectedOwner);
        }
        public Task<bool> RecordAgentObservationAsync(Guid qualificationRunId,
            SiteRawIngressQualificationAgentObservation observation, DateTimeOffset now,
            SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken)
        {
            LastAccess = access;
            return Task.FromResult(access.ApiKeyId == ExpectedOwner);
        }
        public Task<bool> RecordServerBodyReadsAsync(Guid qualificationRunId, int readsWhileBrokerHeld,
            DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SiteRawIngressQualificationRunReport?> ReadAsync(Guid qualificationRunId,
            DateTimeOffset now, SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken)
        {
            LastAccess = access;
            return Task.FromResult<SiteRawIngressQualificationRunReport?>(null);
        }
    }
}
