namespace TagEkyc.ArchTests;

public sealed class PersistenceBoundaryTests
{
    [Fact]
    public void Ef_and_npgsql_references_stay_inside_infrastructure()
    {
        var forbiddenOutsideInfrastructure = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
        };
        var assemblies = new[]
        {
            typeof(TagEkyc.Domain.AssemblyMarker).Assembly,
            typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
            typeof(TagEkyc.Application.AssemblyMarker).Assembly,
            typeof(TagEkyc.Adapters.AssemblyMarker).Assembly,
            typeof(TagEkyc.SignFlow.AssemblyMarker).Assembly,
            typeof(TagEkyc.Api.AssemblyMarker).Assembly,
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

            Assert.DoesNotContain(references, name => forbiddenOutsideInfrastructure.Contains(name));
        }

        var infrastructureReferences = typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains("Microsoft.EntityFrameworkCore", infrastructureReferences);
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", infrastructureReferences);
    }
}
