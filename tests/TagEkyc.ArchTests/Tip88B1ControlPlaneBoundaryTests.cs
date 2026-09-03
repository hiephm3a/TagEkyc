using System.Reflection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.ArchTests;

public sealed class Tip88B1ControlPlaneBoundaryTests
{
    [Fact]
    public void Raw_export_control_plane_port_does_not_expose_b2_surfaces()
    {
        var forbiddenTokens = new[]
        {
            "EkycSession",
            "SubjectConsent",
            "Idempotency",
            "Evidence",
            "Permit",
            "RawBio",
            "Package",
        };

        var surface = typeof(IRawExportControlPlaneRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(method => new[] { method.Name, method.ReturnType.FullName ?? string.Empty }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType.FullName ?? string.Empty)))
            .ToArray();

        foreach (var token in forbiddenTokens)
        {
            Assert.DoesNotContain(surface, value => value.Contains(token, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Raw_export_fulfillment_ref_remains_service_internal_and_is_not_http_or_dto_bound()
    {
        var forbidden = typeof(RawExportFulfillmentRef);
        var apiTypes = typeof(VerificationSessionEndpoints).Assembly.GetTypes();
        var contractTypes = typeof(TagEkyc.Contracts.BusinessConsumer.CreateVerificationSessionRequestDto).Assembly.GetTypes();
        var controllerOrEndpointSurface = apiTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => new[] { method.ReturnType }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType)));
        var dtoSurface = contractTypes
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(property => property.PropertyType);

        Assert.DoesNotContain(controllerOrEndpointSurface, type => IsOrContains(type, forbidden));
        Assert.DoesNotContain(dtoSurface, type => IsOrContains(type, forbidden));
        Assert.True(forbidden.IsPublic);
    }

    private static bool IsOrContains(Type candidate, Type forbidden)
    {
        if (candidate == forbidden)
        {
            return true;
        }

        if (candidate.IsArray)
        {
            return IsOrContains(candidate.GetElementType()!, forbidden);
        }

        if (candidate.IsGenericType)
        {
            return candidate.GetGenericArguments().Any(argument => IsOrContains(argument, forbidden));
        }

        return false;
    }
}
