using System.Reflection;

namespace TagEkyc.ArchTests;

public sealed class ProjectDependencyTests
{
    [Theory]
    [MemberData(nameof(AllowedReferences))]
    public void Project_references_stay_within_tip01_boundary(Assembly assembly, string[] allowedAssemblyNames)
    {
        var references = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("TagEkyc.", StringComparison.Ordinal))
            .Cast<string>()
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(allowedAssemblyNames.OrderBy(name => name), references);
    }

    public static IEnumerable<object[]> AllowedReferences()
    {
        yield return [typeof(TagEkyc.Api.AssemblyMarker).Assembly, new[] { "TagEkyc.Application", "TagEkyc.Contracts", "TagEkyc.Infrastructure" }];
        yield return [typeof(TagEkyc.Application.AssemblyMarker).Assembly, new[] { "TagEkyc.Contracts", "TagEkyc.Domain" }];
        yield return [typeof(TagEkyc.Adapters.AssemblyMarker).Assembly, new[] { "TagEkyc.Application", "TagEkyc.Contracts", "TagEkyc.Domain" }];
        yield return [typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly, new[] { "TagEkyc.Application", "TagEkyc.Contracts", "TagEkyc.Domain" }];
        yield return [typeof(TagEkyc.SignFlow.AssemblyMarker).Assembly, new[] { "TagEkyc.Contracts" }];
        yield return [typeof(TagEkyc.Contracts.AssemblyMarker).Assembly, Array.Empty<string>()];
        yield return [typeof(TagEkyc.Domain.AssemblyMarker).Assembly, Array.Empty<string>()];
    }
}
