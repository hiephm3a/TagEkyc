using System.Reflection;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.ArchTests;

public sealed class Tip55MetadataReferenceBoundaryTests
{
    [Fact]
    public void Metadata_reference_foundation_stays_in_domain_and_application_ports()
    {
        Assert.Equal("TagEkyc.Domain", typeof(MetadataReferenceId).Namespace);
        Assert.Equal("TagEkyc.Domain", typeof(MetadataReferenceState).Namespace);
        Assert.Equal("TagEkyc.Application.Ports", typeof(IMetadataReferenceRegistry).Namespace);
        Assert.True(typeof(IMetadataReferenceRegistry).IsInterface);
    }

    [Fact]
    public void Metadata_reference_registry_has_no_runtime_implementation()
    {
        var implementationTypes = typeof(IMetadataReferenceRegistry).Assembly
            .GetTypes()
            .Where(type => type.IsClass && typeof(IMetadataReferenceRegistry).IsAssignableFrom(type))
            .ToArray();

        Assert.Empty(implementationTypes);
    }

    [Fact]
    public void Metadata_reference_contracts_are_not_exposed_through_public_api_or_external_contracts()
    {
        var publicAssemblies = new[]
        {
            typeof(TagEkyc.Api.AssemblyMarker).Assembly,
            typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
        };
        var tip55Types = new[]
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
                property => tip55Types.Contains(property.PropertyType));
        }
    }

    [Fact]
    public void Metadata_reference_contracts_do_not_define_raw_artifact_access_properties()
    {
        var contractTypes = new[]
        {
            typeof(MetadataReferenceRegistration),
            typeof(MetadataReferenceRecord),
            typeof(MetadataReferenceQueryResult),
        };
        var forbiddenNameFragments = new[]
        {
            "Raw",
            "Payload",
            "Bytes",
            "Byte",
            "Content",
            "Download",
            "Access",
            "Read",
        };
        var publicProperties = contractTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .ToArray();

        Assert.DoesNotContain(
            publicProperties,
            property => forbiddenNameFragments.Any(fragment =>
                property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(
            publicProperties,
            property => property.PropertyType == typeof(byte[]) ||
                property.PropertyType == typeof(Stream));
    }
}
