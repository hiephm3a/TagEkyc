using System.Reflection;
using TagEkyc.Api;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C3RecipientPackageDeliveryArchTests
{
    [Fact]
    public void C323_delivery_assembly_has_no_private_key_unwrap_or_plaintext_contract()
    {
        var names = typeof(RecipientPackageDeliveryOptions).Assembly.GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Infrastructure.RawExport" && type.Name.Contains("Delivery", StringComparison.Ordinal))
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Select(member => member.Name).ToArray();
        Assert.DoesNotContain(names, name => name.Contains("Unwrap", StringComparison.OrdinalIgnoreCase)
            || name.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase));
    }

}
