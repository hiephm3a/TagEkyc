using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1RouteRegistrationTests
{
    private static readonly string[] Common =
    [
        "POST /api/ekyc/verification-sessions", "GET /api/ekyc/verification-sessions/{id}",
        "GET /api/ekyc/verification-sessions/{id}/evidence-ledger",
        "POST /api/ekyc/verification-sessions/{id}/complete", "POST /api/ekyc/verification-sessions/{id}/cancel",
        "GET /api/ekyc/evidence-packages/{id}", "GET /api/ekyc/evidence-packages/{id}/verification-view",
        "POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances",
        "POST /api/ekyc/operator/capture-runtimes/bootstrap-issuances/revoke",
        "POST /api/ekyc/operator/capture-runtimes/suspend", "POST /api/ekyc/operator/capture-runtimes/reactivate",
        "POST /api/ekyc/operator/capture-runtimes/revoke", "POST /api/ekyc/operator/capture-runtimes/retire",
        "POST /api/ekyc/operator/capture-runtimes/credentials/revoke",
        "POST /api/ekyc/operator/capture-runtimes/credential-rotations/authorize",
        "POST /api/ekyc/operator/capture-runtimes/credential-rotations/revoke",
        "POST /api/ekyc/operator/capture-runtimes/role-policies/assign",
        "POST /api/ekyc/operator/capture-runtimes/configurations/assign",
        "POST /api/ekyc/operator/capture-runtime-control/trust-profiles/publish",
        "POST /api/ekyc/operator/capture-runtime-control/role-policies/publish",
        "POST /api/ekyc/operator/capture-runtime-control/configurations/publish",
        "POST /api/ekyc/capture-runtime/enrollments/redeem",
        "GET /api/ekyc/operator/capture-runtimes/{captureAgentId}/readiness",
        "POST /api/ekyc/capture-runtime/credential-rotations/{rotationId}/complete"
    ];
    private static readonly string[] PreparedOnly =
    [
        "POST /api/ekyc/verification-sessions/{id}/capture-artifacts",
        "POST /api/ekyc/verification-sessions/{id}/evidence-results",
        "GET /api/ekyc/capture-agents/self/configuration",
        "POST /api/ekyc/raw-export/source-ingress"
    ];
    private static readonly string[] ActivatedOnly =
    [
        "POST /api/ekyc/verification-sessions/{sessionId}/capture-capabilities",
        "POST /api/ekyc/capture-runtime/executions/bind",
        "POST /api/ekyc/capture-runtime/executions/reconcile",
        "GET /api/ekyc/capture-runtime/self/configuration",
        "POST /api/ekyc/capture-runtime/executions/{bindingId}/capture-artifacts",
        "POST /api/ekyc/verification-sessions/{id}/evidence-results",
        "POST /api/ekyc/raw-export/source-ingress"
    ];

    [Theory]
    [InlineData(CaptureRuntimeRouteState.Prepared)]
    [InlineData(CaptureRuntimeRouteState.Activated)]
    public async Task ActualEndpointDataSources_AreExactExclusiveRouteSets(CaptureRuntimeRouteState state)
    {
        await using var app = Build();
        app.MapCaptureRuntimeSelectedEndpoints(new(state, 1));
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>().ToArray();
        var keys = endpoints.Select(e => Assert.Single(e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods) + " " + e.RoutePattern.RawText).ToArray();
        var expected = Common.Concat(state == CaptureRuntimeRouteState.Prepared ? PreparedOnly : ActivatedOnly).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        var raw = Assert.Single(endpoints, e => e.RoutePattern.RawText == "/api/ekyc/raw-export/source-ingress");
        Assert.Equal(state == CaptureRuntimeRouteState.Prepared ? "IngressAsync" : "RuntimeIngressAsync",
            raw.Metadata.GetMetadata<MethodInfo>()!.Name);
        var evidence = Assert.Single(endpoints, e => e.RoutePattern.RawText == "/api/ekyc/verification-sessions/{id}/evidence-results");
        Assert.Equal(state == CaptureRuntimeRouteState.Prepared ? "AppendEvidenceResultAsync" : "AppendRuntimeEvidenceAsync",
            evidence.Metadata.GetMetadata<MethodInfo>()!.Name);
        Assert.Equal("CancelAsync", Assert.Single(endpoints, e => e.RoutePattern.RawText ==
            "/api/ekyc/verification-sessions/{id}/cancel").Metadata.GetMetadata<MethodInfo>()!.Name);
    }

    [Fact]
    public async Task InvalidSelection_RegistersNothing()
    {
        await using var app = Build();
        Assert.Throws<InvalidOperationException>(() => app.MapCaptureRuntimeSelectedEndpoints(new((CaptureRuntimeRouteState)99, 1)));
        Assert.Throws<InvalidOperationException>(() => app.MapCaptureRuntimeSelectedEndpoints(new(CaptureRuntimeRouteState.Prepared, 0)));
        Assert.Empty(((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints));
    }

    private static WebApplication Build()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<IApiKeyAuthenticator>(_ =>
            throw new InvalidOperationException("Route registration must not execute authentication."));
        // Register only handler-injected service shapes. Their factories throw if
        // invoked: constructing route metadata must not perform DB/auth/body work.
        foreach (var type in new[] { typeof(VerificationSessionEndpoints), typeof(CaptureRuntimeHttpRoutes),
            typeof(CaptureAgentConfigurationEndpoints), typeof(RawExportSourceIngressEndpoints) }
            .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            .SelectMany(m => m.GetParameters()).Select(p => p.ParameterType).Distinct()
            .Where(t => t.Namespace?.StartsWith("TagEkyc.Application", StringComparison.Ordinal) == true))
            builder.Services.AddScoped(type, _ => throw new InvalidOperationException("Route registration must not execute service factories."));
        return builder.Build();
    }
}
