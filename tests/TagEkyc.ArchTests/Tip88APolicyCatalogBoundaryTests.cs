using System.Reflection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.ArchTests;

public sealed class Tip88APolicyCatalogBoundaryTests
{
    [Fact]
    public void Policy_ttl_contract_remains_repository_internal_and_is_not_http_or_dto_bound()
    {
        var forbiddenTypes = new[]
        {
            typeof(RawExportPolicyVersion),
            typeof(AddRawExportPolicyVersionCommand),
        };
        var apiTypes = typeof(VerificationSessionEndpoints).Assembly.GetTypes();
        var contractTypes = typeof(TagEkyc.Contracts.BusinessConsumer.CreateVerificationSessionRequestDto).Assembly.GetTypes();
        var endpointSurface = apiTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => new[] { method.ReturnType }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType)));
        var dtoSurface = contractTypes
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(property => property.PropertyType);

        foreach (var forbidden in forbiddenTypes)
        {
            Assert.DoesNotContain(endpointSurface, type => IsOrContains(type, forbidden));
            Assert.DoesNotContain(dtoSurface, type => IsOrContains(type, forbidden));
            Assert.True(forbidden.IsPublic);
        }
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
