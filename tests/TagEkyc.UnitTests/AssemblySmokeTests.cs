using ApplicationMarker = TagEkyc.Application.AssemblyMarker;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void Domain_enums_expose_expected_placeholder_members()
    {
        Assert.Contains(VerificationProfile.StandardEkycProfile, Enum.GetValues<VerificationProfile>());
        Assert.Contains(VerificationSessionState.Created, Enum.GetValues<VerificationSessionState>());
        Assert.Contains(VerificationResult.NotAvailable, Enum.GetValues<VerificationResult>());
    }

    [Fact]
    public void Application_boundary_marker_is_instantiable()
    {
        var marker = new ApplicationMarker();

        Assert.NotNull(marker);
    }
}
