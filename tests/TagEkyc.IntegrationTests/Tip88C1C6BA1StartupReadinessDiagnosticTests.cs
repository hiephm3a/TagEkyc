using Npgsql;
using NpgsqlTypes;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1StartupReadinessDiagnosticTests(
    PostgresPersistenceFixture postgres, ITestOutputHelper output)
{
    [Fact]
    public async Task R28_GlobalProjection_DoesNotRequireRuntime_AndR19KeepsAbsentSelectorBehavior()
    {
        // R28 owns global startup selection; R19 remains explicitly agent-scoped.
        // Synthetic seed follows the existing R27 proof and is rolled back in full.
        // The existing clone helper copies data, not just schema. Reset the exclusive
        // disposable test fixture first so the zero-runtime premise is actually true.
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_global_census");
        await using var isolatedContext = isolated.CreateDbContext();
        await using var connection = new NpgsqlConnection(isolatedContext.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var operatorId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var trustId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT count(*) FROM tagekyc.capture_runtime_registrations";
        Assert.Equal(0L, await command.ExecuteScalarAsync());
        command.CommandText = """
            INSERT INTO tagekyc.platform_operator_credentials VALUES
              (@operator,@prefix,decode(repeat('11',32),'hex'),777,@principal,
               ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 hour',
               now()+interval '1 day',NULL,NULL);
            """;
        command.Parameters.AddWithValue("operator", operatorId);
        command.Parameters.AddWithValue("prefix", Guid.NewGuid().ToString("N")[..12]);
        command.Parameters.AddWithValue("principal", Guid.NewGuid());
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        var now = DateTimeOffset.UtcNow;
        await SetRoleAsync(command, "tagekyc_capture_runtime_application");
        command.CommandText = "SELECT required_pepper_versions FROM tagekyc.capture_runtime_read_cutover_state('Managed',@now)";
        command.Parameters.AddWithValue("now", now);
        var withoutAgent = Assert.IsType<int[]>(await command.ExecuteScalarAsync());
        Assert.Contains(777, withoutAgent);
        command.Parameters.Clear();
        await ResetRoleAsync(command);
        command.CommandText = """
            SELECT count(*) FROM tagekyc.platform_operator_credentials
            WHERE "CredentialId"=@operator AND "State"='Active'
              AND "VerifierPepperVersion"=777 AND "ExpiresAtUtc">now()
            """;
        command.Parameters.AddWithValue("operator", operatorId);
        Assert.Equal(1L, await command.ExecuteScalarAsync());
        command.Parameters.Clear();

        foreach (Guid? absentSelector in new Guid?[] { null, Guid.Empty, agentId })
        {
            await SetRoleAsync(command, "tagekyc_capture_runtime_operator");
            command.CommandText = """
                SELECT result_code,required_pepper_versions
                FROM tagekyc.capture_runtime_read_readiness(@agent,now())
                """;
            command.Parameters.Add(new NpgsqlParameter("agent", NpgsqlDbType.Uuid)
                { Value = absentSelector.HasValue ? absentSelector.Value : DBNull.Value });
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal("RESOURCE_NOT_AVAILABLE", reader.GetString(0));
                Assert.Empty(reader.GetFieldValue<int[]>(1));
                Assert.False(await reader.ReadAsync());
            }
            command.Parameters.Clear();
            await ResetRoleAsync(command);
        }

        // Positive control: the same function/version union becomes reachable with an
        // actual registered runtime selector, even without an installation yet.
        command.CommandText = """
            INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES
              (@trust,1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day',@operator,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES (@trust,1,1,now());
            INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
              (@config,1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,
               1000,1000,10000,10000,10000,1024,@operator,now());
            INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES (@config,1,1,now());
            INSERT INTO tagekyc.capture_runtime_registrations VALUES
              (@agent,'Managed',@trust,1,@config,1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
            """;
        command.Parameters.AddWithValue("trust", trustId);
        command.Parameters.AddWithValue("config", configurationId);
        command.Parameters.AddWithValue("operator", operatorId);
        command.Parameters.AddWithValue("agent", agentId);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        await SetRoleAsync(command, "tagekyc_capture_runtime_operator");
        command.CommandText = """
            SELECT result_code,required_pepper_versions
            FROM tagekyc.capture_runtime_read_readiness(@agent,now())
            """;
        command.Parameters.AddWithValue("agent", agentId);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal("AVAILABLE", reader.GetString(0));
            Assert.Contains(777, reader.GetFieldValue<int[]>(1));
            Assert.False(await reader.ReadAsync());
        }
        command.Parameters.Clear();
        await ResetRoleAsync(command);

        await SetRoleAsync(command, "tagekyc_capture_runtime_application");
        command.CommandText = "SELECT * FROM tagekyc.capture_runtime_read_cutover_state('Managed',@now)";
        command.Parameters.AddWithValue("now", now);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.Equal(new[] { "profile", "state", "revision", "prepared_at_utc",
                "activated_at_utc", "activated_by_credential_id", "required_pepper_versions" },
                Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray());
            Assert.True(await reader.ReadAsync());
            Assert.Equal(withoutAgent, reader.GetFieldValue<int[]>(6));
            Assert.Equal("Managed", reader.GetString(0));
            Assert.Equal("Prepared", reader.GetString(1));
            Assert.False(await reader.ReadAsync());
        }
        command.Parameters.Clear();
        await ResetRoleAsync(command);
        command.CommandText = """
            INSERT INTO tagekyc.capture_runtime_registrations VALUES
              (@agent,'Managed',@trust,1,@config,1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL)
            """;
        command.Parameters.AddWithValue("agent", Guid.NewGuid());
        command.Parameters.AddWithValue("trust", trustId);
        command.Parameters.AddWithValue("config", configurationId);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        command.CommandText = "SELECT count(*) FROM tagekyc.capture_runtime_registrations";
        Assert.Equal(2L, await command.ExecuteScalarAsync());
        await SetRoleAsync(command, "tagekyc_capture_runtime_application");
        command.CommandText = "SELECT required_pepper_versions FROM tagekyc.capture_runtime_read_cutover_state('Managed',@now)";
        command.Parameters.AddWithValue("now", now);
        Assert.Equal(withoutAgent, Assert.IsType<int[]>(await command.ExecuteScalarAsync()));
        command.Parameters.Clear();
        await ResetRoleAsync(command);
        foreach (var role in new[] { "tagekyc_capture_runtime_operator", "tagekyc_runtime", "tagekyc_capture_runtime_application" })
        {
            await SetRoleAsync(command, role);
            await transaction.SaveAsync("denied_read");
            command.CommandText = "SELECT \"CaptureAgentId\" FROM tagekyc.capture_runtime_registrations LIMIT 1";
            var failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteScalarAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, failure.SqlState);
            await transaction.RollbackAsync("denied_read");
            await transaction.SaveAsync("denied_version_read");
            command.CommandText = "SELECT \"VerifierPepperVersion\" FROM tagekyc.platform_operator_credentials LIMIT 1";
            failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteScalarAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, failure.SqlState);
            await transaction.RollbackAsync("denied_version_read");
            await ResetRoleAsync(command);
        }
        await transaction.RollbackAsync();
        output.WriteLine("MEASURED: R28 sees 777 without Agent and after unrelated runtime creation; absent R19 selectors remain empty; real R19 sees 777; startup/operator/default direct enumeration denied; synthetic transaction rolled back.");
    }

    [Fact]
    public async Task RequiredPepperVersions_Parity_AllBranchesHaveIndependentRedControls()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var baseline = await db.Database.SqlQueryRaw<int[]>(
            "SELECT required_pepper_versions AS \"Value\" FROM tagekyc.capture_runtime_read_cutover_state('Managed',@now)",
            new NpgsqlParameter("now", now)).SingleAsync();
        Assert.DoesNotContain(777, baseline);
        Assert.DoesNotContain(778, baseline);
        Assert.DoesNotContain(779, baseline);
        var expected = baseline.Concat(new[] { 777, 778, 779 }).Distinct().Order().ToArray();
        Assert.Equal(baseline.Length + 3, expected.Length);
        await Tip88C1C6BA1AppendAuthorityTests.SeedBindingAsync(db, 777, 779);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO tagekyc.capture_runtime_bootstrap_issuances
             ("BootstrapIssuanceId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion",
              "RuntimeType","TrustProfileId","TrustProfileRevision","RolePolicyId","RolePolicyRevision",
              "ConfigurationId","ConfigurationRevision","AttestationRequirementDigest","RequestFingerprint",
              "IssueOperationId","IssuedByCredentialId","IssuedAtUtc","ExpiresAtUtc","State","Revision")
            VALUES('a0000000-0000-4000-8000-000000000001','zyxwvutsrqpo',decode(repeat('11',32),'hex'),778,
              'Managed','20000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,
              '30000000-0000-4000-8000-000000000001',1,decode(repeat('22',32),'hex'),decode(repeat('33',32),'hex'),
              'a0000000-0000-4000-8000-000000000002','00000000-0000-4000-8000-000000000001',
              now()-interval '1 hour',now()+interval '1 day','Active',1);
            """);
        const string cutover = "tagekyc.capture_runtime_read_cutover_state(text,timestamptz)";
        const string readiness = "tagekyc.capture_runtime_read_readiness(uuid,timestamptz)";
        async Task<int[]> ReadAsync(bool startup)
        {
            await db.Database.ExecuteSqlRawAsync(startup
                ? "SET LOCAL ROLE tagekyc_capture_runtime_application"
                : "SET LOCAL ROLE tagekyc_capture_runtime_operator");
            var query = startup
                ? "SELECT required_pepper_versions AS \"Value\" FROM tagekyc.capture_runtime_read_cutover_state('Managed',@now)"
                : "SELECT required_pepper_versions AS \"Value\" FROM tagekyc.capture_runtime_read_readiness('40000000-0000-4000-8000-000000000001',@now)";
            var result = await db.Database.SqlQueryRaw<int[]>(query, new NpgsqlParameter("now", now)).SingleAsync();
            await db.Database.ExecuteSqlRawAsync("RESET ROLE");
            return result;
        }
        async Task AssertParityAsync()
        {
            var left = await ReadAsync(true);
            var right = await ReadAsync(false);
            Assert.Equal(expected.Length, left.Length);
            Assert.Equal(expected.Length, right.Length);
            Assert.Equal(expected.Length, left.Distinct().Count());
            Assert.Equal(expected.Length, right.Distinct().Count());
            Assert.Equal(expected, left);
            Assert.True(left.ToHashSet().SetEquals(right));
        }
        await AssertParityAsync();
        // Removing any branch from either reader must fail this SAME parity/count
        // assertion. Independent versions prevent another branch masking the omission.
        foreach (var identity in new[] { cutover, readiness })
        {
            var original = await db.Database.SqlQueryRaw<string>(
                $"SELECT pg_catalog.pg_get_functiondef('{identity}'::regprocedure) AS \"Value\"").SingleAsync();
            foreach (var table in new[] { "platform_operator_credentials p", "capture_runtime_bootstrap_issuances b", "capture_capabilities c" })
            {
                var branch = original.Split('\n').Single(line =>
                    line.Contains("FROM tagekyc." + table, StringComparison.Ordinal)
                    && line.Contains("VerifierPepperVersion", StringComparison.Ordinal));
                // A false membership predicate is the executable removal mutation;
                // no table, ACL, row guard or other function is changed.
                await db.Database.ExecuteSqlRawAsync(original.Replace(branch, branch.TrimEnd('\r') + " AND false", StringComparison.Ordinal));
                await Assert.ThrowsAnyAsync<Exception>(AssertParityAsync);
                await db.Database.ExecuteSqlRawAsync(original);
                await AssertParityAsync();
            }
        }
        Assert.True(await db.Database.SqlQueryRaw<bool>(
            "SELECT pg_catalog.to_regprocedure('tagekyc.capture_runtime_read_cutover_state(text)') IS NULL AS \"Value\"").SingleAsync());
        await transaction.RollbackAsync();
        output.WriteLine($"MEASURED: same-time set parity/counts {expected.Length}/{expected.Length}; three independent branch versions plus measured baseline; six branch-removal mutations RED; each restoration GREEN.");
    }

    private static async Task SetRoleAsync(NpgsqlCommand command, string role)
    {
        command.CommandText = role switch
        {
            "tagekyc_capture_runtime_operator" => "SET LOCAL ROLE tagekyc_capture_runtime_operator",
            "tagekyc_capture_runtime_application" => "SET LOCAL ROLE tagekyc_capture_runtime_application",
            "tagekyc_runtime" => "SET LOCAL ROLE tagekyc_runtime",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ResetRoleAsync(NpgsqlCommand command)
    {
        command.CommandText = "RESET ROLE";
        await command.ExecuteNonQueryAsync();
    }
}
