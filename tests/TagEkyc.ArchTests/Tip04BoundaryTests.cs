using System.Reflection;
using TagEkyc.Application.LocalDev;

namespace TagEkyc.ArchTests;

public sealed class Tip04BoundaryTests
{
    [Fact]
    public void Tip06_api_does_not_define_deferred_runtime_surfaces()
    {
        var apiTypeNames = typeof(TagEkyc.Api.AssemblyMarker).Assembly
            .GetTypes()
            .Select(type => type.Name)
            .ToArray();

        var futureRuntimeTerms = new[]
        {
            "Webhook",
            "CaptureQualityResult",
            "DocumentResult",
            "NfcResult",
            "FaceResult",
            "LivenessResult",
            "FingerprintResult",
        };

        foreach (var term in futureRuntimeTerms)
        {
            Assert.DoesNotContain(apiTypeNames, name => name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Tip04_local_development_components_are_named_as_localdev()
    {
        var localDevelopmentTypes = typeof(LocalDevRuntimePolicySource).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Application.LocalDev")
            .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToArray();

        Assert.NotEmpty(localDevelopmentTypes);
        Assert.All(localDevelopmentTypes, name => Assert.Contains("LocalDev", name, StringComparison.Ordinal));
    }

    [Fact]
    public void Tip04_api_keeps_existing_dependency_direction()
    {
        var references = typeof(TagEkyc.Api.AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && name.StartsWith("TagEkyc.", StringComparison.Ordinal))
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(["TagEkyc.Application", "TagEkyc.Contracts", "TagEkyc.Infrastructure"], references);
    }
}
