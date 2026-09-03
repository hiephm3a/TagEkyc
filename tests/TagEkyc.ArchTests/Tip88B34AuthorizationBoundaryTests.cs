using System.Reflection;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.ArchTests;

public sealed class Tip88B34AuthorizationBoundaryTests
{
    [Fact]
    public void L10_authorization_records_are_public_but_not_http_or_dto_bound()
    {
        var records = new[]
        {
            typeof(AuthenticatedRawExportActor),
            typeof(AuthorizeRawExportCommand),
            typeof(RawExportAuthorizationResult),
            typeof(RawExportAuthorizationDecision),
            typeof(RawExportAuthorizationEligibilityCause),
            typeof(RawExportAuthorizationFulfillmentRef),
            typeof(RawExportAuthorizationClassSnapshot),
            typeof(RawExportAuthorizationPermit),
            typeof(RawExportAuthorizationPermitClass),
        };
        var apiTypes = typeof(VerificationSessionEndpoints).Assembly.GetTypes();
        var contractTypes =
            typeof(TagEkyc.Contracts.BusinessConsumer.CreateVerificationSessionRequestDto).Assembly.GetTypes();
        var endpointSurface = apiTypes
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => new[] { method.ReturnType }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType)));
        var dtoSurface = contractTypes
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(property => property.PropertyType);

        Assert.Equal(
            typeof(Task<RawExportAuthorizationResult>),
            typeof(IRawExportAuthorizationRepository)
                .GetMethod(nameof(IRawExportAuthorizationRepository.AuthorizeExportAsync))!
                .ReturnType);
        foreach (var record in records)
        {
            Assert.True(record.IsPublic);
            Assert.DoesNotContain(endpointSurface, type => IsOrContains(type, record));
            Assert.DoesNotContain(dtoSurface, type => IsOrContains(type, record));
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

        return candidate.IsGenericType &&
               candidate.GetGenericArguments().Any(argument => IsOrContains(argument, forbidden));
    }
}
