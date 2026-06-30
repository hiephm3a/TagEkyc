using System.Reflection;

namespace TagEkyc.ArchTests;

public sealed class Tip72LivenessBoundaryTests
{
    [Fact]
    public void Tip72_server_build_does_not_take_liveness_engine_or_decode_dependencies()
    {
        var forbiddenFragments = new[]
        {
            "Onnx",
            "ViewFace",
            "MiniFAS",
            "SilentFace",
            "Dlib",
            "OpenCv",
        };

        foreach (var assembly in ServerAssemblies())
        {
            var references = assembly
                .GetReferencedAssemblies()
                .Select(name => name.Name ?? string.Empty)
                .ToArray();
            var typeNames = assembly
                .GetTypes()
                .Select(type => type.FullName ?? type.Name)
                .ToArray();

            Assert.DoesNotContain(references, reference =>
                forbiddenFragments.Any(fragment => reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
            Assert.DoesNotContain(typeNames, typeName =>
                forbiddenFragments.Any(fragment => typeName.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private static Assembly[] ServerAssemblies() =>
    [
        typeof(TagEkyc.Api.AssemblyMarker).Assembly,
        typeof(TagEkyc.Application.AssemblyMarker).Assembly,
        typeof(TagEkyc.Contracts.AssemblyMarker).Assembly,
        typeof(TagEkyc.Domain.AssemblyMarker).Assembly,
        typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly,
    ];
}
