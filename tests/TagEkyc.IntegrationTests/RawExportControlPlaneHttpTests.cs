using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportControlPlaneHttpTests
{
    [Fact]
    public async Task Raw_export_control_plane_joins_authorization_job_creation_and_job_read_routes()
    {
        var authenticator = new AcceptingAuthenticator();
        var service = new CountingService();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiKeyAuthenticator>(authenticator);
        builder.Services.AddSingleton<IRawExportControlPlaneApplicationService>(service);
        await using var app = builder.Build();
        app.MapRawExportControlPlaneEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();

        using var authorization = new HttpRequestMessage(
            HttpMethod.Post, "/api/ekyc/raw-export/authorizations")
        {
            Content = JsonContent.Create(new AuthorizeRawExportRequestDto(
                Guid.NewGuid(), Guid.NewGuid(), 1,
                ["ChipDg2Portrait", "LiveSelfieImage"])),
        };
        authorization.Headers.Add("Idempotency-Key", "http-route-authorization");
        using var authorizationResponse = await client.SendAsync(authorization);
        Assert.Equal(HttpStatusCode.OK, authorizationResponse.StatusCode);

        using var bind = new HttpRequestMessage(HttpMethod.Post, "/api/ekyc/raw-export/jobs")
        {
            Content = JsonContent.Create(new BindRawExportJobRequestDto(service.PermitId)),
        };
        bind.Headers.Add("Idempotency-Key", "http-route-job");
        using var bindResponse = await client.SendAsync(bind);
        Assert.Equal(HttpStatusCode.Created, bindResponse.StatusCode);
        Assert.Equal($"/api/ekyc/raw-export/jobs/{service.JobId:D}", bindResponse.Headers.Location?.ToString());

        using var readResponse = await client.GetAsync($"/api/ekyc/raw-export/jobs/{service.JobId:D}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var job = await readResponse.Content.ReadFromJsonAsync<RawExportJobStatusDto>();
        Assert.NotNull(job);
        Assert.Equal(service.JobId, job.JobId);
        Assert.Equal(service.PackageId, job.PackageId);
        Assert.Equal(
            [RawExportControlPlaneApplicationService.AuthorizeScope,
             RawExportControlPlaneApplicationService.JobScope,
             RawExportControlPlaneApplicationService.JobScope],
            authenticator.RequiredScopes);
        Assert.Equal(3, service.Calls);
    }

    [Fact]
    public async Task Raw_export_control_plane_rejects_unauthenticated_request_before_service()
    {
        var authenticator = new RejectingAuthenticator();
        var service = new CountingService();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiKeyAuthenticator>(authenticator);
        builder.Services.AddSingleton<IRawExportControlPlaneApplicationService>(service);
        await using var app = builder.Build();
        app.MapRawExportControlPlaneEndpoints();
        await app.StartAsync();

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/api/ekyc/raw-export/authorizations")
        {
            Content = JsonContent.Create(new AuthorizeRawExportRequestDto(
                Guid.NewGuid(), Guid.NewGuid(), 1,
                ["ChipDg2Portrait", "LiveSelfieImage"])),
        };
        request.Headers.Add("Idempotency-Key", "http-auth-negative");
        using var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("RAW_EXPORT_TEST_AUTH_DENIED", await response.Content.ReadAsStringAsync());
        Assert.Equal(RawExportControlPlaneApplicationService.AuthorizeScope, authenticator.RequiredScope);
        Assert.Equal(0, service.Calls);
    }

    private sealed class RejectingAuthenticator : IApiKeyAuthenticator
    {
        public string? RequiredScope { get; private set; }

        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext httpContext,
            string? requiredScope = null,
            CancellationToken cancellationToken = default)
        {
            RequiredScope = requiredScope;
            return Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Failure(
                "RAW_EXPORT_TEST_AUTH_DENIED", "Denied by test authenticator.", 403));
        }
    }

    private sealed class AcceptingAuthenticator : IApiKeyAuthenticator
    {
        public List<string?> RequiredScopes { get; } = [];

        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext httpContext,
            string? requiredScope = null,
            CancellationToken cancellationToken = default)
        {
            RequiredScopes.Add(requiredScope);
            return Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(
                Guid.Parse("88c10000-0000-5000-8000-00000000c501"),
                Guid.Parse("88c10000-0000-5000-8000-00000000c502"),
                "c5-successor",
                AuthenticatedCallerCategory.BusinessConsumer,
                new HashSet<string>(RecipientManagementCodec.ActivationScopes),
                PrincipalId: Guid.Parse("88c10000-0000-5000-8000-00000000c503"))));
        }
    }

    private sealed class CountingService : IRawExportControlPlaneApplicationService
    {
        public int Calls { get; private set; }
        public Guid PermitId { get; } = Guid.Parse("88c10000-0000-5000-8000-00000000c511");
        public Guid JobId { get; } = Guid.Parse("88c10000-0000-5000-8000-00000000c512");
        public Guid PackageId { get; } = Guid.Parse("88c10000-0000-5000-8000-00000000c513");

        public Task<SessionOperationResult<RawExportAuthorizationDecisionDto>> AuthorizeAsync(
            AuthenticatedClientContext actor,
            AuthorizeRawExportRequestDto request,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(SessionOperationResult<RawExportAuthorizationDecisionDto>.Success(
                new(Guid.NewGuid(), "Authorized", null, PermitId,
                    DateTimeOffset.UtcNow.AddMinutes(1),
                    ["ChipDg2Portrait", "LiveSelfieImage"])));
        }

        public Task<SessionOperationResult<RawExportJobBindingDto>> BindJobAsync(
            AuthenticatedClientContext actor,
            BindRawExportJobRequestDto request,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal(PermitId, request.PermitId);
            Assert.Equal("http-route-job", idempotencyKey);
            return Task.FromResult(SessionOperationResult<RawExportJobBindingDto>.Success(
                new(JobId, "NewJob")));
        }

        public Task<SessionOperationResult<RawExportJobStatusDto>> ReadJobAsync(
            AuthenticatedClientContext actor,
            Guid jobId,
            CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal(JobId, jobId);
            return Task.FromResult(SessionOperationResult<RawExportJobStatusDto>.Success(new(
                JobId,
                Guid.Parse("88c10000-0000-5000-8000-00000000c514"),
                actor.ClientApplicationId,
                "Ready",
                7,
                DateTimeOffset.UtcNow.AddMinutes(5),
                ["ChipDg2Portrait", "LiveSelfieImage"],
                PackageId,
                "Published",
                DateTimeOffset.UtcNow)));
        }
    }
}
