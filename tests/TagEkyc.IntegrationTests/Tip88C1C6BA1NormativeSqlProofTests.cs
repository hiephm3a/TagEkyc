using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1NormativeSqlProofTests(PostgresPersistenceFixture postgres, ITestOutputHelper output)
{
    [Theory]
    [InlineData("core_identity")]
    [InlineData("runtime")]
    [InlineData("expiry")]
    public async Task CompleteNormativeSqlFences_ExecuteAgainstCurrentMigration_AndLeaveNoSharedResidue(string family)
    {
        // These historical proofs own fixed synthetic identities. Never clone residue
        // from another integration test and accidentally exercise a different fixture.
        await postgres.ResetDatabaseAsync();
        var scripts = family switch
        {
            "core_identity" => CombineCoreAndIdentity(),
            "runtime" => ReadSql("tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md", 2),
            "expiry" => ReadSql("tip_88c1_c6b_a1_expiry_on_denied_proof.md", 1),
            _ => throw new InvalidOperationException("Unknown finite proof family.")
        };
        string isolatedName;
        await using (var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_normative_" + family))
        {
            await using var db = isolated.CreateDbContext();
            var connectionString = db.Database.GetConnectionString()!;
            isolatedName = new NpgsqlConnectionStringBuilder(connectionString).Database!;
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var version = new NpgsqlCommand("SHOW server_version_num", connection))
                Assert.InRange(int.Parse((string)(await version.ExecuteScalarAsync())!), 160000, 169999);
            var before = await Counts(connection);
            foreach (var script in scripts)
            {
                // No SQL mutation, assertion substitution or selective statement skipping.
                // ExecuteNonQuery drains every result; the first database error fails the test.
                await using var command = new NpgsqlCommand(script, connection) { CommandTimeout = 60 };
                await command.ExecuteNonQueryAsync();
                if (script.TrimEnd().EndsWith("ROLLBACK;", StringComparison.Ordinal))
                    Assert.Equal(before, await Counts(connection));
            }
            if (family == "runtime")
            {
                // The complete second fence deliberately commits its race fixture.
                // Its own four named assertions run above; disposal, not a fictitious
                // outer rollback, removes that independently committed test database.
                await using var raceCount = new NpgsqlCommand("SELECT count(*) FROM tagekyc.capture_runtime_management_operations WHERE \"OperationKind\"='CredentialRevoke'", connection);
                Assert.Equal(1L, (long)(await raceCount.ExecuteScalarAsync())!);
            }
            else Assert.Equal(before, await Counts(connection));
        }
        await using var source = postgres.CreateDbContext();
        Assert.False(await source.Database.SqlQueryRaw<bool>(
            "SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_database WHERE datname={0}) AS \"Value\"", isolatedName).SingleAsync());
        Assert.Equal(0, await source.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.platform_operator_credentials").SingleAsync());
        output.WriteLine("MEASURED: {0}; all SQL fences executed; rollback assertions checked; isolated database discarded; shared source has no credential residue.", family);
    }

    private string[] CombineCoreAndIdentity()
    {
        var core = Assert.Single(ReadSql("tip_88c1_c6b_a1_postgresql16_mutation_suite.md", 1));
        var identity = Assert.Single(ReadSql("tip_88c1_c6b_a1_identity_audit_mutation_proof.md", 1));
        var terminal = core.LastIndexOf("ROLLBACK;", StringComparison.Ordinal);
        Assert.True(terminal >= 0);
        Assert.Equal("ROLLBACK;", core[terminal..].Trim());
        // This is the insertion point specified by the identity artifact itself.
        return [core[..terminal] + identity + "\n" + core[terminal..]];
    }

    private string[] ReadSql(string file, int expectedFences)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var path = Path.Combine(root.FullName, "docs/tips/tip_88c1_secure_raw_source_sealed_assembly", file);
        var bytes = File.ReadAllBytes(path);
        output.WriteLine("CONSUMED {0} SHA256={1} BYTES={2}", file, Convert.ToHexString(SHA256.HashData(bytes)), bytes.Length);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        var matches = Regex.Matches(text, @"^```sql\r?\n(?<sql>.*?)^```\s*$", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.Equal(expectedFences, matches.Count);
        return matches.Select(match => match.Groups["sql"].Value).ToArray();
    }

    private static async Task<string[]> Counts(NpgsqlConnection connection)
    {
        var rows = new List<string>();
        foreach (var table in A1CatalogueProof.Tables.Concat(["verification_sessions", "audit_events"]).Order(StringComparer.Ordinal))
        {
            await using var command = new NpgsqlCommand($"SELECT count(*) FROM tagekyc.\"{table}\"", connection);
            rows.Add(table + "|" + (long)(await command.ExecuteScalarAsync())!);
        }
        return rows.ToArray();
    }
}
