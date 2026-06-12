using System.Reflection;
using TagEkyc.Contracts.BusinessConsumer;

namespace TagEkyc.ArchTests;

public sealed class Tip11BoundaryTests
{
    [Fact]
    public void Tip11_does_not_add_forbidden_runtime_dependencies()
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
            "Azure.Messaging",
            "RabbitMQ",
            "Jose",
            "Microsoft.IdentityModel.JsonWebTokens",
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain(references, name => forbiddenReferences.Contains(name));
        }
    }

    [Fact]
    public void Business_consumer_contracts_do_not_expose_tip11_metadata()
    {
        var forbiddenTerms = new[]
        {
            "PolicySnapshot",
            "Retention",
            "LegalHold",
            "Purge",
            "DeletionEligibility",
            "AccessAudit",
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
}
