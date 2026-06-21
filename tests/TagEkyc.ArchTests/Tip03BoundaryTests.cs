using System.Reflection;
using TagEkyc.Application.Ports;

namespace TagEkyc.ArchTests;

public sealed class Tip03BoundaryTests
{
    [Fact]
    public void Tip03_does_not_reference_persistence_frameworks()
    {
        var assemblies = new[]
        {
            typeof(TagEkyc.Domain.AssemblyMarker).Assembly,
            typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
            typeof(TagEkyc.Application.AssemblyMarker).Assembly,
        };

        var forbiddenReferences = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "Microsoft.Data.SqlClient",
            "MongoDB",
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain(references, name => forbiddenReferences.Contains(name));
        }
    }

    [Fact]
    public void Tip03_does_not_define_dbcontext_or_migration_types()
    {
        var typeNames = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("TagEkyc.", StringComparison.Ordinal) == true)
            .SelectMany(LoadableTypes)
            .Where(type => type.Assembly != typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly)
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain(typeNames, name => name.Contains("DbContext", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, name => name.Contains("Migration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Append_only_repository_ports_do_not_expose_update_or_delete()
    {
        var appendOnlyPorts = new[]
        {
            typeof(ICaptureArtifactRepository),
            typeof(IEvidenceResultRepository),
            typeof(IVerificationDecisionRepository),
            typeof(IEvidencePackageRepository),
            typeof(IAuditEventRepository),
        };

        foreach (var port in appendOnlyPorts)
        {
            var methodNames = port.GetMethods().Select(method => method.Name).ToArray();

            Assert.Contains("AppendAsync", methodNames);
            Assert.DoesNotContain(methodNames, name => name.Contains("Update", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(methodNames, name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(methodNames, name => name.Contains("Remove", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }
}
