using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C6BA3CompositionArchTests
{
    [Fact]
    public void BrokerProjectDependsOnlyOnExistingModules()
    {
        var root = Root();
        var project = XDocument.Load(Path.Combine(root, "src/TagEkyc.RawIngressBroker/TagEkyc.RawIngressBroker.csproj"));
        Assert.Equal("Microsoft.NET.Sdk.Web", (string?)project.Root!.Attribute("Sdk"));
        Assert.Equal("net8.0", Assert.Single(project.Descendants("TargetFramework")).Value);
        Assert.Empty(project.Descendants("PackageReference"));
        Assert.Equal(new[]
        {
            "../TagEkyc.Application/TagEkyc.Application.csproj",
            "../TagEkyc.Contracts/TagEkyc.Contracts.csproj",
            "../TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj"
        }, project.Descendants("ProjectReference").Select(x => (string)x.Attribute("Include")!).Order());
        Assert.DoesNotContain(typeof(RawIngressBrokerTransactionFacade).Assembly.GetReferencedAssemblies(),
            a => a.Name is "TagEkyc.Api" or "TagEkyc.RawIngressBroker");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories))
        {
            if (file.Contains("TagEkyc.RawIngressBroker", StringComparison.Ordinal)) continue;
            Assert.DoesNotContain(XDocument.Load(file).Descendants("ProjectReference"),
                e => ((string?)e.Attribute("Include"))?.Contains("TagEkyc.RawIngressBroker", StringComparison.Ordinal) == true);
        }
    }

    [Fact]
    public void BrokerBusinessPortHasNoRawStreamOrCallerSelectedPrincipal()
    {
        var method = Assert.Single(typeof(IRawIngressMetadataBroker).GetMethods());
        Assert.Equal(new[] { typeof(CaptureRuntimeRawIngressAdmissionContext), typeof(CancellationToken) },
            method.GetParameters().Select(x => x.ParameterType));
        var names = typeof(CaptureRuntimeRawIngressAdmissionContext).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("PrincipalId", names); Assert.DoesNotContain("ClientApplicationId", names);
        Assert.DoesNotContain("BindingId", names); Assert.DoesNotContain("Subject", names);
        Assert.DoesNotContain(typeof(CaptureRuntimeRawIngressAdmissionContext).GetProperties(), p => typeof(Stream).IsAssignableFrom(p.PropertyType));
        Assert.Equal(11, typeof(RawIngressBrokerHandoff).GetProperties().Length);
        Assert.DoesNotContain(typeof(RawIngressBrokerHandoff).GetProperties(), p => p.PropertyType == typeof(byte[]) || typeof(Stream).IsAssignableFrom(p.PropertyType));
    }

    [Fact]
    public void ActivatedCompositionExportsOnlyHostPortsAndOwnerConfiguration()
    {
        var extension = Assert.Single(typeof(CaptureRuntimeRawIngressComposition).GetMethods()
            .Where(x => x.Name == "AddTagEkycCaptureRuntimeRawIngress"));
        Assert.Equal(new[] { typeof(IServiceCollection), typeof(RawIngressBrokerOptions),
            typeof(CaptureRuntimeRawIngressComposition.RuntimeOwners) },
            extension.GetParameters().Select(x => x.ParameterType));
        Assert.DoesNotContain(extension.GetParameters(), p => p.ParameterType.Name.Contains("Kek", StringComparison.Ordinal)
            || p.ParameterType.Name.Contains("Provider", StringComparison.Ordinal));
        Assert.Equal(new[] { typeof(CancellationToken) },
            Assert.Single(typeof(ICaptureRuntimeA3Worker).GetMethods()).GetParameters().Select(x => x.ParameterType));
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository not found");
    }
}
