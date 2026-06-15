using System.Reflection;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.BusinessConsumer;

namespace TagEkyc.ArchTests;

public sealed class Tip17DurableMetadataBoundaryTests
{
    [Fact]
    public void Tip17_does_not_add_forbidden_persistence_or_provider_dependencies()
    {
        var assemblies = new[]
        {
            typeof(TagEkyc.Domain.AssemblyMarker).Assembly,
            typeof(TagEkyc.Application.AssemblyMarker).Assembly,
            typeof(TagEkyc.Infrastructure.AssemblyMarker).Assembly,
            typeof(TagEkyc.Adapters.AssemblyMarker).Assembly,
            typeof(TagEkyc.SignFlow.AssemblyMarker).Assembly,
        };
        var forbiddenReferences = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "Microsoft.Data.SqlClient",
            "MongoDB",
            "Dapper",
            "Azure.Security.KeyVault",
            "Microsoft.IdentityModel.JsonWebTokens",
            "System.IdentityModel.Tokens.Jwt",
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain(references, name => forbiddenReferences.Contains(name));
        }
    }

    [Fact]
    public void Durable_metadata_repository_contract_stays_in_application_ports()
    {
        Assert.Equal("TagEkyc.Application.Ports", typeof(IDurableMetadataRepository).Namespace);
        Assert.True(typeof(IDurableMetadataRepository).IsInterface);
        Assert.DoesNotContain(
            typeof(IDurableMetadataRepository).Assembly.GetTypes(),
            type => type.IsClass &&
                type.Name.Contains("DurableMetadataRepository", StringComparison.Ordinal) &&
                type.Namespace?.Contains("LocalDev", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Business_consumer_contracts_do_not_expose_tip17_durable_metadata()
    {
        var forbiddenTerms = new[]
        {
            "PrincipalId",
            "CredentialRef",
            "CredentialType",
            "CredentialStatus",
            "ScopeGrantSet",
            "TenantId",
            "PolicySnapshot",
            "Retention",
            "LegalHold",
            "Purge",
            "DeletionEligibility",
        };
        var businessProperties = typeof(CreateVerificationSessionRequestDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Contracts.BusinessConsumer")
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .ToArray();

        Assert.DoesNotContain(
            businessProperties,
            property => forbiddenTerms.Any(term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Application_does_not_reference_signflow_runtime()
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
