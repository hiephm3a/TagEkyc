using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1RootCliTests(PostgresPersistenceFixture postgres)
{
    private const string CanonicalPepper = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";

    [Fact]
    public async Task R01_ActualCli_CommitsBeforeSecret_AndReplayDoesNotReemit()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_root_cli");
        await using var db = isolated.CreateDbContext();
        var operation = Guid.NewGuid(); var principal = Guid.NewGuid();
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        var first = await Invoke(db.Database.GetConnectionString()!, operation, principal, expiry, CanonicalPepper);
        Assert.Equal(0, first.ExitCode);
        var keyLine = Assert.Single(first.Stdout.Split('\n'), x => x.StartsWith("platformOperatorKey=teo_", StringComparison.Ordinal));
        Assert.Equal("platformOperatorKey=teo_".Length + 43, keyLine.TrimEnd('\r').Length);
        Assert.Equal(1, await OperationCount(db, operation));
        var replay = await Invoke(db.Database.GetConnectionString()!, operation, principal, expiry, CanonicalPepper);
        Assert.Equal(10, replay.ExitCode);
        Assert.Contains("ExistingMatchSecretUnavailable", replay.Stdout);
        Assert.DoesNotContain("platformOperatorKey=", replay.Stdout);
        Assert.Equal(1, await OperationCount(db, operation));
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.platform_operator_root_events
            WHERE "OperationId"='{operation}'
            """).SingleAsync());
        var credential = Guid.Parse(Assert.Single(first.Stdout.Split('\n'), x => x.StartsWith("credentialId=", StringComparison.Ordinal)).Split('=')[1].Trim());
        var revokeOperation = Guid.NewGuid();
        var revoked = await Invoke(db.Database.GetConnectionString()!, revokeOperation, principal, expiry, CanonicalPepper, credential);
        var revokeReplay = await Invoke(db.Database.GetConnectionString()!, revokeOperation, principal, expiry, CanonicalPepper, credential);
        Assert.Equal(0, revoked.ExitCode); Assert.Equal(0, revokeReplay.ExitCode);
        Assert.Equal(revoked.Stdout, revokeReplay.Stdout);
        Assert.Contains("result=Revoked", revoked.Stdout);
        Assert.DoesNotContain("platformOperatorKey=", revoked.Stdout + revokeReplay.Stdout);
        Assert.Equal(1, await OperationCount(db, revokeOperation));
        Assert.Equal("Revoked", await db.Database.SqlQueryRaw<string>($"SELECT \"State\"::text AS \"Value\" FROM tagekyc.platform_operator_credentials WHERE \"CredentialId\"='{credential}'").SingleAsync());
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData(" ")]
    public async Task R01_ActualCli_RejectsExactMalformedFileWithoutMutation(string suffix)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_root_invalid");
        await using var db = isolated.CreateDbContext();
        var operation = Guid.NewGuid();
        var result = await Invoke(db.Database.GetConnectionString()!, operation, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(1), CanonicalPepper + suffix);
        Assert.Equal(30, result.ExitCode);
        Assert.DoesNotContain(CanonicalPepper, result.Stdout + result.Stderr);
        Assert.DoesNotContain("platformOperatorKey=", result.Stdout);
        Assert.Equal(0, await OperationCount(db, operation));
    }

    [Fact]
    public async Task R01_ActualCli_DeferredCommitFailure_EmitsNoSuccessOrSecret()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_root_commit_fail");
        await using var db = isolated.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a1_test_fail_root_commit() RETURNS trigger LANGUAGE plpgsql AS
            $body$ BEGIN RAISE EXCEPTION 'SYNTHETIC_ROOT_DEFERRED_COMMIT_FAILURE'; END $body$;
            CREATE CONSTRAINT TRIGGER a1_test_root_commit_failure
            AFTER INSERT ON tagekyc.platform_operator_root_operations
            DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
            EXECUTE FUNCTION tagekyc.a1_test_fail_root_commit();
            """);
        var operation = Guid.NewGuid(); var principal = Guid.NewGuid();
        var result = await Invoke(db.Database.GetConnectionString()!, operation, principal,
            DateTimeOffset.UtcNow.AddHours(1), CanonicalPepper);
        Assert.Equal(40, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.DoesNotContain(CanonicalPepper, result.Stderr);
        Assert.DoesNotContain("SYNTHETIC_ROOT_DEFERRED_COMMIT_FAILURE", result.Stderr);
        Assert.Equal(0, await OperationCount(db, operation));
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.platform_operator_credentials
            WHERE "PrincipalId"='{principal}'
            """).SingleAsync());
    }

    private static Task<int> OperationCount(TagEkyc.Infrastructure.Persistence.TagEkycDbContext db, Guid id)
        => db.Database.SqlQueryRaw<int>($"SELECT count(*)::integer AS \"Value\" FROM tagekyc.platform_operator_root_operations WHERE \"OperationId\"='{id}'").SingleAsync();

    private static async Task<CliResult> Invoke(string connectionString, Guid operation, Guid principal,
        DateTimeOffset expiry, string rawPepper, Guid? revokeCredential = null)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(root.FullName, "tools", "TagEkyc.ApiKeyProvisioner", "bin", configuration,
            "net8.0", "TagEkyc.ApiKeyProvisioner.dll");
        Assert.True(File.Exists(dll), "Build the existing CLI project before running actual-process proofs.");
        var directory = Path.Combine(Path.GetTempPath(), "tagekyc-a1-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var materialPath = Path.Combine(directory, "synthetic-pepper.txt");
        try
        {
            await File.WriteAllBytesAsync(materialPath, Encoding.UTF8.GetBytes(rawPepper));
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            var args = revokeCredential is { } credential
                ? new[] { dll, "platform-operator", "revoke", "--operation-id", operation.ToString("N"),
                    "--credential-id", credential.ToString("N"), "--expected-revision", "1",
                    "--connection-string-secret-ref", "env:TAGEKYC_SYNTHETIC_CLI_DB" }
                : new[] { dll, "platform-operator", "provision", "--operation-id", operation.ToString("N"),
                "--principal-id", principal.ToString("N"), "--expires-at", expiry.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
                "--capture-runtime-verifier-pepper-version", "1", "--capture-runtime-verifier-pepper-secret-ref", "file:" + materialPath,
                "--connection-string-secret-ref", "env:TAGEKYC_SYNTHETIC_CLI_DB" };
            foreach (var arg in args) start.ArgumentList.Add(arg);
            start.Environment["TAGEKYC_SYNTHETIC_CLI_DB"] = connectionString;
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
            return new(process.ExitCode, await stdout, await stderr);
        }
        finally
        {
            if (File.Exists(materialPath)) File.Delete(materialPath);
            Directory.Delete(directory);
        }
    }
    private sealed record CliResult(int ExitCode, string Stdout, string Stderr);
}
