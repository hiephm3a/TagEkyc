using System.Reflection;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2DurableKeyFixtureProofArchTests
{
    [Fact]
    public void ProductionCompositionAndRoles_CannotSelectOrExecuteFixtureProvider()
    {
        var provider = typeof(FixtureDurableKekOperationProvider);
        Assert.False(provider.IsPublic);
        Assert.False(provider.IsNestedPublic);

        var root = RepoRoot();
        var productionFiles = Directory.EnumerateFiles(
            Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(
                "FixtureDurableKekOperationProvider.cs", StringComparison.OrdinalIgnoreCase));
        foreach (var path in productionFiles)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain(nameof(FixtureDurableKekOperationProvider), text, StringComparison.Ordinal);
            if (Path.GetFileName(path).Equals("Program.cs", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).EndsWith("ServiceCollectionExtensions.cs", StringComparison.OrdinalIgnoreCase))
                Assert.DoesNotContain("fixture-kek-provider-v1", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FixtureProvider_InternalSurface_DoesNotExposeDekOrHandles()
    {
        var fixtureTypes = new[]
        {
            typeof(FixtureDurableKekOperationProvider),
            typeof(PostgresFixtureKekJournal),
            typeof(IFixtureKekWrapJournal),
            typeof(IFixtureKekLookupJournal),
            typeof(FixtureKekJournalResult),
        };
        Assert.All(fixtureTypes, type => Assert.False(type.IsPublic || type.IsNestedPublic));

        var publicSurface = typeof(FixtureDurableKekOperationProvider).Assembly.ExportedTypes
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.DoesNotContain(publicSurface, member => member switch
        {
            MethodInfo method => IsFixtureInternal(method.ReturnType)
                || method.GetParameters().Any(parameter => IsFixtureInternal(parameter.ParameterType)),
            PropertyInfo property => IsFixtureInternal(property.PropertyType),
            FieldInfo field => IsFixtureInternal(field.FieldType),
            _ => false,
        });

    }

    private static bool IsFixtureInternal(Type type) =>
        type == typeof(FixtureDurableKekOperationProvider)
        || type == typeof(PostgresFixtureKekJournal)
        || type == typeof(FixtureKekJournalResult)
        || type == typeof(IFixtureKekWrapJournal)
        || type == typeof(IFixtureKekLookupJournal);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
