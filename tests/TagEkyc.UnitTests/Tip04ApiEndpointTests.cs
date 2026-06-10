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
    public void Tip04_maps_only_create_and_get_verification_session_api_routes()
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
        builder.Services.AddSingleton<VerificationSessionApplicationService>();
        builder.Services.AddSingleton<IVerificationSessionCommands>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
        builder.Services.AddSingleton<IVerificationSessionQueries>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
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
                Assert.Equal("/api/ekyc/verification-sessions", route.Pattern);
                Assert.Equal(["POST"], route.Methods);
            },
            route =>
            {
                Assert.Equal("/api/ekyc/verification-sessions/{id}", route.Pattern);
                Assert.Equal(["GET"], route.Methods);
            });
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
