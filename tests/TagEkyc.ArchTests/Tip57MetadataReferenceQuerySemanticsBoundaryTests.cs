using System.Reflection;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.ArchTests;

public sealed class Tip57MetadataReferenceQuerySemanticsBoundaryTests
{
    [Fact]
    public void Metadata_reference_query_semantics_do_not_introduce_forbidden_result_names()
    {
        var metadataReferenceTypes = new[]
        {
            typeof(MetadataReferenceState),
            typeof(MetadataReferenceStateExtensions),
            typeof(MetadataReferenceRegistration),
            typeof(MetadataReferenceRecord),
            typeof(MetadataReferenceQueryResult),
            typeof(IMetadataReferenceRegistry),
        };
        var forbiddenNameFragments = new[]
        {
            "Available",
            "EvidenceReady",
            "ArtifactReady",
            "PackageComplete",
            "ProviderReady",
            "ProductionReady",
            "SecurityReady",
            "LegalReady",
        };
        var publicNames = metadataReferenceTypes
            .SelectMany(type => type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            .Select(member => member.Name)
            .Where(name => !name.StartsWith("Allows", StringComparison.Ordinal))
            .Concat(Enum.GetNames<MetadataReferenceState>())
            .ToArray();

        Assert.DoesNotContain(
            publicNames,
            name => forbiddenNameFragments.Any(fragment =>
                name.Contains(fragment, StringComparison.Ordinal)));
    }

    [Fact]
    public void Metadata_reference_query_semantics_remain_unexposed_from_api_and_contracts()
    {
        var publicAssemblies = new[]
        {
            typeof(TagEkyc.Api.AssemblyMarker).Assembly,
            typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
        };
        var metadataReferenceTypes = new[]
        {
            typeof(MetadataReferenceId),
            typeof(MetadataReferenceState),
            typeof(MetadataReferenceRegistration),
            typeof(MetadataReferenceRecord),
            typeof(MetadataReferenceQueryResult),
        };

        foreach (var assembly in publicAssemblies)
        {
            var publicProperties = assembly
                .GetTypes()
                .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .ToArray();

            Assert.DoesNotContain(
                publicProperties,
                property => metadataReferenceTypes.Contains(property.PropertyType));
        }
    }

    [Fact]
    public void Metadata_reference_registry_allows_only_tip60_localdev_runtime_implementation()
    {
        var candidateAssemblies = new[]
        {
            typeof(TagEkyc.Application.AssemblyMarker).Assembly,
            typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly,
            typeof(TagEkyc.Adapters.AssemblyMarker).Assembly,
            typeof(TagEkyc.Api.AssemblyMarker).Assembly,
        };
        var implementationTypes = candidateAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && typeof(IMetadataReferenceRegistry).IsAssignableFrom(type))
            .ToArray();

        var implementationType = Assert.Single(implementationTypes);
        Assert.Equal(typeof(LocalDevInMemoryMetadataReferenceRegistry), implementationType);
        Assert.Equal("TagEkyc.Application.LocalDev", implementationType.Namespace);
    }
}
