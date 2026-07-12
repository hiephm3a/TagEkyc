using System.Reflection;
using TagEkyc.Application.Ports;

namespace TagEkyc.ArchTests;

public sealed class Tip88B1ControlPlaneBoundaryTests
{
    [Fact]
    public void Raw_export_control_plane_port_does_not_expose_b2_surfaces()
    {
        var forbiddenTokens = new[]
        {
            "EkycSession",
            "SubjectConsent",
            "Idempotency",
            "Evidence",
            "Permit",
            "RawBio",
            "Package",
        };

        var surface = typeof(IRawExportControlPlaneRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(method => new[] { method.Name, method.ReturnType.FullName ?? string.Empty }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType.FullName ?? string.Empty)))
            .ToArray();

        foreach (var token in forbiddenTokens)
        {
            Assert.DoesNotContain(surface, value => value.Contains(token, StringComparison.Ordinal));
        }
    }
}
