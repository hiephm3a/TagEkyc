using System.Diagnostics;
using System.Globalization;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

/// <summary>A separate cluster is required when Down owns cluster-wide capability roles.</summary>
internal sealed class IsolatedMigrationPostgres : IAsyncDisposable
{
    private readonly string containerName;
    private readonly PostgresPersistenceFixture fixture;
    private bool disposed;

    private IsolatedMigrationPostgres(string name, string connectionString)
    {
        containerName = name;
        ConnectionString = connectionString;
        fixture = new PostgresPersistenceFixture(connectionString);
    }

    public string ConnectionString { get; }
    public string DatabaseName => new NpgsqlConnectionStringBuilder(ConnectionString).Database!;
    public string AdminConnectionString => new NpgsqlConnectionStringBuilder(ConnectionString)
        { Database = "postgres", Pooling = false }.ConnectionString;
    public TagEkycDbContext CreateDbContext() => fixture.CreateDbContext();

    public static async Task<IsolatedMigrationPostgres> CreateAsync()
    {
        var name = $"tagekyc-migration-isolated-{Guid.NewGuid():N}";
        var database = $"tagekyc_migration_{Guid.NewGuid():N}";
        try
        {
            await DockerAsync("run", "-d", "--rm", "--tmpfs", "/var/lib/postgresql/data",
                "--name", name, "-e", $"POSTGRES_DB={database}", "-e", "POSTGRES_USER=tagekyc",
                "-e", "POSTGRES_PASSWORD=tagekyc", "-p", "127.0.0.1::5432", "postgres:16");
            var portText = await DockerAsync("port", name, "5432/tcp");
            var port = int.Parse(portText.Trim().Split(':')[^1], CultureInfo.InvariantCulture);
            var connectionString = $"Host=127.0.0.1;Port={port};Database={database};" +
                "Username=tagekyc;Password=tagekyc;Pooling=false;Include Error Detail=true";
            var ready = false;
            for (var attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    await using var probe = new NpgsqlConnection(connectionString);
                    await probe.OpenAsync();
                    await using var command = new NpgsqlCommand("SELECT 1", probe);
                    await command.ExecuteScalarAsync();
                    await Task.Delay(500);
                    await command.ExecuteScalarAsync();
                    ready = true;
                    break;
                }
                catch (NpgsqlException) { await Task.Delay(500); }
            }
            if (!ready) throw new InvalidOperationException("Isolated migration cluster did not become ready.");
            var isolated = new IsolatedMigrationPostgres(name, connectionString);
            await isolated.fixture.ResetDatabaseAsync();
            return isolated;
        }
        catch
        {
            await DockerAsync("rm", "-f", name);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await DockerAsync("rm", "-f", containerName);
        disposed = true;
    }

    public async Task<bool> IsAbsentAsync()
    {
        var names = await DockerAsync("ps", "-a", "--filter", $"name=^/{containerName}$", "--format", "{{.Names}}");
        return string.IsNullOrWhiteSpace(names);
    }

    private static async Task<string> DockerAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo("docker")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Docker did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var result = await output;
        var failure = await error;
        if (process.ExitCode != 0) throw new InvalidOperationException($"Isolated Docker command failed: {failure}");
        return result;
    }
}
