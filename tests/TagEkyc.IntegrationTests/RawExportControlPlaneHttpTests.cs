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

namespace TagEkyc.IntegrationTests;

public sealed class RawExportControlPlaneHttpTests
{
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

    private sealed class CountingService : IRawExportControlPlaneApplicationService
    {
        public int Calls { get; private set; }

        public Task<SessionOperationResult<RawExportAuthorizationDecisionDto>> AuthorizeAsync(
            AuthenticatedClientContext actor,
            AuthorizeRawExportRequestDto request,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(SessionOperationResult<RawExportAuthorizationDecisionDto>.Success(
                new(Guid.NewGuid(), "Authorized", null, Guid.NewGuid(),
                    DateTimeOffset.UtcNow.AddMinutes(1),
                    ["ChipDg2Portrait", "LiveSelfieImage"])));
        }

        public Task<SessionOperationResult<RawExportJobBindingDto>> BindJobAsync(
            AuthenticatedClientContext actor,
            BindRawExportJobRequestDto request,
            string? idempotencyKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<SessionOperationResult<RawExportJobStatusDto>> ReadJobAsync(
            AuthenticatedClientContext actor,
            Guid jobId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
