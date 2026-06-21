using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresPersistenceCollection : ICollectionFixture<PostgresPersistenceFixture>
{
    public const string Name = "PostgresPersistence";
}

public sealed class PostgresPersistenceFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("TAGEKYC_POSTGRES_TEST_CONNECTION_STRING") ??
        "Host=localhost;Port=55432;Database=tagekyc_persistence_tests;Username=tagekyc;Password=tagekyc;Include Error Detail=true";

    public async Task InitializeAsync()
    {
        await RunDockerComposeAsync("up -d --wait");
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync() => await RunDockerComposeAsync("down");

    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public TagEkycDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new TagEkycDbContext(options);
    }

    private static async Task RunDockerComposeAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo("docker", $"compose -f docker-compose.persistence-tests.yml {arguments}")
        {
            WorkingDirectory = FindRepoRoot(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start docker compose.");
        var completed = await Task.Run(() => process.WaitForExit(120_000));
        if (!completed)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException("Timed out while starting Postgres test container.");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"docker compose failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docker-compose.persistence-tests.yml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
