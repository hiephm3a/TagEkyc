using System.Reflection;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.InternalAudit.Manifest;

namespace TagEkyc.ArchTests;

public sealed class Tip06BoundaryTests
{
    [Fact]
    public void Tip06_completion_uses_visible_localdev_finalization_boundary()
    {
        Assert.True(typeof(IVerificationFinalizationBoundary)
            .IsAssignableFrom(typeof(LocalDevInMemoryVerificationFinalizationBoundary)));

        var method = typeof(IVerificationFinalizationBoundary).GetMethod(nameof(IVerificationFinalizationBoundary.TryFinalizeAsync));
        Assert.NotNull(method);
        Assert.Contains("Finalization", typeof(LocalDevInMemoryVerificationFinalizationBoundary).Name, StringComparison.Ordinal);
        Assert.Contains("LocalDev", typeof(LocalDevInMemoryVerificationFinalizationBoundary).Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Business_consumer_tip06_dtos_do_not_reference_internal_manifest_types()
    {
        var businessProperties = typeof(CompleteVerificationSessionResponseDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Contracts.BusinessConsumer")
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .ToArray();

        Assert.DoesNotContain(businessProperties, property =>
            property.Name.Contains("ArtifactHash", StringComparison.Ordinal) ||
            property.Name.Contains("PayloadHash", StringComparison.Ordinal));
        Assert.DoesNotContain(businessProperties, property => property.PropertyType == typeof(EvidenceManifestDto));
        Assert.DoesNotContain(businessProperties, property => property.PropertyType.FullName?.Contains("InternalAudit", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Tip06_does_not_add_persistence_or_webhook_framework_references()
    {
        var assemblies = new[]
        {
            typeof(TagEkyc.Api.AssemblyMarker).Assembly,
            typeof(TagEkyc.Application.AssemblyMarker).Assembly,
        };
        var forbiddenReferences = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "Microsoft.Data.SqlClient",
            "MongoDB",
            "Azure.Security.KeyVault",
            "Azure.Messaging",
            "RabbitMQ",
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain(references, name => forbiddenReferences.Contains(name));
        }
    }
}
