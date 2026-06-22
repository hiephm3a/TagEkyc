using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Api.LocalDev;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.UnitTests;

public sealed class Tip04ApiEndpointTests
{
    [Fact]
    public void Tip05_maps_only_allowed_verification_session_api_routes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<LocalDevRuntimePolicySource>();
        builder.Services.AddSingleton<ILocalDevClientPolicyProvider>(sp => sp.GetRequiredService<LocalDevRuntimePolicySource>());
        builder.Services.AddSingleton<LocalDevApiKeyValidator>();
        builder.Services.AddSingleton<ILocalDevApiKeyAuthenticator, LocalDevApiKeyAuthenticator>();
        builder.Services.AddSingleton<LocalDevInMemoryVerificationSessionRepository>();
        builder.Services.AddSingleton<IVerificationSessionRepository>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationSessionRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryAuditEventRepository>();
        builder.Services.AddSingleton<IAuditEventRepository>(sp => sp.GetRequiredService<LocalDevInMemoryAuditEventRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryCaptureArtifactRepository>();
        builder.Services.AddSingleton<ICaptureArtifactRepository>(sp => sp.GetRequiredService<LocalDevInMemoryCaptureArtifactRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryEvidenceResultRepository>();
        builder.Services.AddSingleton<IEvidenceResultRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidenceResultRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryVerificationDecisionRepository>();
        builder.Services.AddSingleton<IVerificationDecisionRepository>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationDecisionRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryEvidencePackageRepository>();
        builder.Services.AddSingleton<IEvidencePackageRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidencePackageRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryEvidenceManifestRepository>();
        builder.Services.AddSingleton<IInternalEvidenceManifestRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidenceManifestRepository>());
        builder.Services.AddSingleton<LocalDevInMemoryVerificationFinalizationBoundary>();
        builder.Services.AddSingleton<IVerificationFinalizationBoundary>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationFinalizationBoundary>());
        builder.Services.AddSingleton<IEvidenceSigner, TestEvidenceSigner>();
        builder.Services.AddSingleton<VerificationSessionApplicationService>();
        builder.Services.AddSingleton<IVerificationSessionCommands>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
        builder.Services.AddSingleton<IVerificationSessionQueries>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
        builder.Services.AddSingleton<VerificationEvidenceApplicationService>();
        builder.Services.AddSingleton<ICaptureArtifactCommands>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
        builder.Services.AddSingleton<ITrustedEvidenceResultCommands>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
        builder.Services.AddSingleton<VerificationCompletionApplicationService>();
        builder.Services.AddSingleton<IVerificationSessionCompletionCommands>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
        builder.Services.AddSingleton<IEvidencePackageQueries>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
        var app = builder.Build();

        app.MapVerificationSessionEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Pattern = FormatPattern(endpoint.RoutePattern),
                Methods = endpoint.Metadata
                    .OfType<HttpMethodMetadata>()
                    .SelectMany(metadata => metadata.HttpMethods)
                    .OrderBy(method => method)
                    .ToArray(),
            })
            .OrderBy(route => route.Pattern)
            .ThenBy(route => string.Join(",", route.Methods))
            .ToArray();

        Assert.Collection(
            routes,
            route =>
            {
                Assert.Equal("/api/ekyc/evidence-packages/{id}", route.Pattern);
                Assert.Equal(["GET"], route.Methods);
            },
            route =>
            {
                Assert.Equal("/api/ekyc/verification-sessions", route.Pattern);
                Assert.Equal(["POST"], route.Methods);
            },
            route =>
            {
                Assert.Equal("/api/ekyc/verification-sessions/{id}", route.Pattern);
                Assert.Equal(["GET"], route.Methods);
            },
            route =>
            {
                Assert.Equal("/api/ekyc/verification-sessions/{id}/capture-artifacts", route.Pattern);
                Assert.Equal(["POST"], route.Methods);
            },
            route =>
            {
                Assert.Equal("/api/ekyc/verification-sessions/{id}/complete", route.Pattern);
                Assert.Equal(["POST"], route.Methods);
            },
            route =>
            {
                Assert.Equal("/api/ekyc/verification-sessions/{id}/evidence-results", route.Pattern);
                Assert.Equal(["POST"], route.Methods);
            });

        Assert.DoesNotContain(routes, route => route.Pattern.Contains("capture-quality-result", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.Pattern.Contains("document-result", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.Pattern.Contains("nfc-result", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.Pattern.Contains("face-result", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.Pattern.Contains("liveness-result", StringComparison.Ordinal));
        Assert.DoesNotContain(routes, route => route.Pattern.Contains("fingerprint-result", StringComparison.Ordinal));
    }

    private static string FormatPattern(RoutePattern pattern)
    {
        var segments = pattern.PathSegments.Select(segment =>
            string.Concat(segment.Parts.Select(part => part switch
            {
                RoutePatternLiteralPart literal => literal.Content,
                RoutePatternParameterPart parameter => "{" + parameter.Name + "}",
                _ => part.ToString(),
            })));

        return "/" + string.Join("/", segments);
    }
}
