namespace TagEkyc.ArchTests;

public sealed class Tip13AuthorizationBoundaryTests
{
    [Fact]
    public void Application_authorization_boundary_does_not_reference_signflow_runtime()
    {
        var references = typeof(TagEkyc.Application.AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.DoesNotContain("TagEkyc.SignFlow", references);
    }
}
