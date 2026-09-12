namespace TagEkyc.ArchTests;

public sealed class DockerTestResourceLifecycleTests
{
    [Fact]
    public void PostgreSql_test_harnesses_do_not_leave_anonymous_data_volumes()
    {
        var repo = FindRepoRoot();
        var integrationRoot = Path.Combine(repo, "tests", "TagEkyc.IntegrationTests");
        var composeFixture = File.ReadAllText(
            Path.Combine(integrationRoot, "PostgresPersistenceFixture.cs"));

        Assert.Contains(
            "RunDockerComposeAsync(\"down -v --remove-orphans\"",
            composeFixture,
            StringComparison.Ordinal);
        Assert.Equal(
            2,
            CountOccurrences(
                composeFixture,
                "RunDockerComposeAsync(\"down -v --remove-orphans\""));

        var postgresContainerSources = Directory
            .EnumerateFiles(integrationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("\"postgres:", StringComparison.Ordinal))
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "IsolatedMigrationPostgres.cs",
                "Tip83E1ReadinessEndpointTests.cs",
                "Tip88B1E3ResolverReadBoundaryTests.cs",
            ],
            postgresContainerSources.Select(Path.GetFileName));

        foreach (var path in postgresContainerSources)
        {
            var source = File.ReadAllText(path);
            var runStart = source.IndexOf("\"run\"", StringComparison.Ordinal);
            var image = source.IndexOf("\"postgres:", runStart, StringComparison.Ordinal);
            Assert.True(runStart >= 0 && image > runStart, $"PostgreSQL docker run was not found in {path}.");
            var dockerRun = source[runStart..image];
            Assert.Contains("\"--rm\"", dockerRun, StringComparison.Ordinal);
            Assert.Contains("\"--tmpfs\"", dockerRun, StringComparison.Ordinal);
            Assert.Contains("\"/var/lib/postgresql/data\"", dockerRun, StringComparison.Ordinal);
            // Both success disposal and startup failure remove the exact isolated container.
            var cleanupCall = Path.GetFileName(path) == "IsolatedMigrationPostgres.cs"
                ? "DockerAsync(\"rm\", \"-f\""
                : "RunDockerAsync(allowFailure: true, \"rm\", \"-f\"";
            Assert.Equal(
                2,
                CountOccurrences(
                    source,
                    cleanupCall));
        }
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
