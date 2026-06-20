using System.Reflection;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.ArchTests;

public sealed class Tip60LocalDevMetadataReferenceRegistryBoundaryTests
{
    [Fact]
    public void Exactly_one_localdev_metadata_reference_registry_implementation_is_allowed()
    {
        var implementationTypes = CandidateAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && typeof(IMetadataReferenceRegistry).IsAssignableFrom(type))
            .ToArray();

        var implementationType = Assert.Single(implementationTypes);
        Assert.Equal(typeof(LocalDevInMemoryMetadataReferenceRegistry), implementationType);
        Assert.Equal("TagEkyc.Application.LocalDev", implementationType.Namespace);
    }

    [Fact]
    public void Metadata_reference_registry_has_no_non_localdev_or_public_surface_implementation()
    {
        var forbiddenAssemblies = new[]
        {
            typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly,
            typeof(TagEkyc.Adapters.AssemblyMarker).Assembly,
            typeof(TagEkyc.Api.AssemblyMarker).Assembly,
            typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
        };
        var forbiddenImplementations = forbiddenAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && typeof(IMetadataReferenceRegistry).IsAssignableFrom(type))
            .ToArray();

        Assert.Empty(forbiddenImplementations);
    }

    [Fact]
    public void LocalDev_registry_does_not_reference_persistence_provider_raw_or_artifact_storage_dependencies()
    {
        var implementationType = typeof(LocalDevInMemoryMetadataReferenceRegistry);
        var forbiddenAssemblyReferences = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "Microsoft.Data.SqlClient",
            "MongoDB",
            "Dapper",
            "Azure.Storage",
            "Azure.Security.KeyVault",
            "AWSSDK",
        };
        var applicationReferences = implementationType.Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.DoesNotContain(applicationReferences, name =>
            forbiddenAssemblyReferences.Any(fragment => name.Contains(fragment, StringComparison.Ordinal)));

        var memberTypeNames = implementationType
            .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(MemberTypeNames)
            .ToArray();
        var forbiddenTypeFragments = new[]
        {
            "DbContext",
            "Database",
            "Migration",
            "Provider",
            "Storage",
            "Resolver",
            "Tool",
            "Payload",
            "Artifact",
            "Raw",
            "Bytes",
            "Stream",
        };

        Assert.DoesNotContain(memberTypeNames, name =>
            forbiddenTypeFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Metadata_reference_contracts_remain_unexposed_from_api_and_contracts()
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
    public void LocalDev_registry_names_do_not_drift_into_readiness_or_capability_claims()
    {
        var names = typeof(LocalDevInMemoryMetadataReferenceRegistry)
            .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(member => member.Name)
            .Append(typeof(LocalDevInMemoryMetadataReferenceRegistry).Name)
            .ToArray();
        var forbiddenNameFragments = new[]
        {
            "Production",
            "Ready",
            "Readiness",
            "Legal",
            "Audit",
            "Security",
            "Certification",
            "Capability",
            "CompletePackage",
            "PackageComplete",
        };

        Assert.DoesNotContain(names, name =>
            forbiddenNameFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    private static Assembly[] CandidateAssemblies() =>
    [
        typeof(TagEkyc.Application.AssemblyMarker).Assembly,
        typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly,
        typeof(TagEkyc.Adapters.AssemblyMarker).Assembly,
        typeof(TagEkyc.Api.AssemblyMarker).Assembly,
        typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
    ];

    private static IEnumerable<string> MemberTypeNames(MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            yield return field.FieldType.FullName ?? field.FieldType.Name;
        }

        if (member is PropertyInfo property)
        {
            yield return property.PropertyType.FullName ?? property.PropertyType.Name;
        }

        if (member is MethodInfo method)
        {
            yield return method.ReturnType.FullName ?? method.ReturnType.Name;

            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
            }
        }

        if (member is ConstructorInfo constructor)
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
            }
        }
    }
}
