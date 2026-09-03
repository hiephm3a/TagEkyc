using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B32AuthorizationIdentityFunctionTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string Migration = "20260722163505_Tip88B32RawExportAuthorizationIdempotencyFunctions";
    private const string PreviousMigration = "20260722084729_Tip88B31RawExportAuthorizationSchema";
    private const string LockFunction = "raw_export_lock_verification_session_for_authorization";
    private const string ClaimFunction = "raw_export_claim_or_read_authorization_idempotency";

    private static readonly string[] Functions = [LockFunction, ClaimFunction];
    private static readonly string[] Triggers = [];
    private static readonly string[] Constraints = [];
    private static readonly IReadOnlyDictionary<string, string[]> Parameters = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [LockFunction] = ["verification_session_id", "ClientApplicationId", "SubjectRef", "State"],
        [ClaimFunction] =
        [
            "principal_id", "client_application_id", "requested_verification_session_id",
            "idempotency_key", "fingerprint_hash", "prospective_fresh_export_decision_id",
            "outcome", "export_decision_id",
        ],
    };

    public async Task InitializeAsync()
    {
        await using var db = postgres.CreateDbContext();
        await db.GetService<IMigrator>().MigrateAsync(Migration);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task H1_intended_b32_identifiers_are_within_63_bytes_and_round_trip_exactly()
    {
        var intendedNames = Functions.Concat(Triggers).Concat(Constraints).Concat(Parameters.Values.SelectMany(names => names)).ToArray();
        var longestFunctionBytes = Functions.Max(name => Encoding.UTF8.GetByteCount(name));

        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT p.proname, p.proargnames
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname = 'tagekyc' AND p.proname = ANY(@functions)
            ORDER BY p.proname;
            """, connection);
        command.Parameters.AddWithValue("functions", Functions);
        await using var reader = await command.ExecuteReaderAsync();
        var actual = new Dictionary<string, string[]>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            actual.Add(reader.GetString(0), reader.GetFieldValue<string[]>(1));
        }

        Assert.Multiple(
            () => Assert.All(intendedNames, name => Assert.True(Encoding.UTF8.GetByteCount(name) <= 63, $"Intended identifier exceeds 63 bytes: {name}")),
            () => Assert.Equal(9, 63 - longestFunctionBytes),
            () => Assert.Equal(Functions.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal)),
            () => Assert.All(Parameters, expected => Assert.Equal(expected.Value, actual[expected.Key])));
    }

    [Fact]
    public async Task H2_function_manifest_matches_signature_security_owner_search_path_and_acl()
    {
        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT p.proname,
                   pg_catalog.oidvectortypes(p.proargtypes),
                   pg_catalog.pg_get_function_result(p.oid),
                   p.prosecdef,
                   owner.rolname,
                   p.proconfig = ARRAY['search_path=pg_catalog']::text[],
                   pg_catalog.has_function_privilege('public', p.oid, 'EXECUTE'),
                   pg_catalog.has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc' AND p.proname = ANY(@functions)
            ORDER BY p.proname;
            """, connection);
        command.Parameters.AddWithValue("functions", Functions);
        await using var reader = await command.ExecuteReaderAsync();
        var manifests = new List<FunctionManifest>();
        while (await reader.ReadAsync())
        {
            manifests.Add(new FunctionManifest(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3),
                reader.GetString(4), reader.GetBoolean(5), reader.GetBoolean(6), reader.GetBoolean(7)));
        }

        Assert.Equal(2, manifests.Count);
        AssertManifest(
            manifests.Single(item => item.Name == LockFunction),
            "uuid",
            "TABLE(\"ClientApplicationId\" uuid, \"SubjectRef\" text, \"State\" text)");
        AssertManifest(
            manifests.Single(item => item.Name == ClaimFunction),
            "uuid, uuid, uuid, text, bytea, uuid",
            "TABLE(outcome text, export_decision_id uuid)");
    }

    [Fact]
    public async Task H3_lock_function_blocks_competing_session_lock_until_transaction_end()
    {
        var sessionId = await SeedSessionAsync();
        await using var login = await CreateDedicatedLoginAsync(includeDeployer: false);
        try
        {
            await using var first = await login.OpenAsync();
            await using var second = await login.OpenAsync();

            await using (var firstTransaction = await first.BeginTransactionAsync())
            await using (var secondTransaction = await second.BeginTransactionAsync())
            {
                await SetRuntimeRoleAsync(first, firstTransaction);
                await SetRuntimeRoleAsync(second, secondTransaction);
                var locked = await LockSessionAsync(first, firstTransaction, sessionId);
                Assert.Equal("b32-lock-subject", locked.SubjectRef);

                var competingLock = LockSessionAsync(second, secondTransaction, sessionId);
                await Task.Delay(250);
                Assert.False(competingLock.IsCompleted, "The competing session lock did not block before COMMIT.");

                await firstTransaction.CommitAsync();
                Assert.Equal("b32-lock-subject", (await competingLock.WaitAsync(TimeSpan.FromSeconds(5))).SubjectRef);
                await secondTransaction.RollbackAsync();
            }

            await using (var firstTransaction = await first.BeginTransactionAsync())
            await using (var secondTransaction = await second.BeginTransactionAsync())
            {
                await SetRuntimeRoleAsync(first, firstTransaction);
                await SetRuntimeRoleAsync(second, secondTransaction);
                await LockSessionAsync(first, firstTransaction, sessionId);

                var competingLock = LockSessionAsync(second, secondTransaction, sessionId);
                await Task.Delay(250);
                Assert.False(competingLock.IsCompleted, "The competing session lock did not block before ROLLBACK.");

                await firstTransaction.RollbackAsync();
                Assert.Equal("b32-lock-subject", (await competingLock.WaitAsync(TimeSpan.FromSeconds(5))).SubjectRef);
                await secondTransaction.RollbackAsync();
            }
        }
        finally
        {
            await DeleteSessionAsync(sessionId);
        }
    }

    [Fact]
    public async Task H4_claim_state_machine_returns_new_existing_and_conflict_without_raising()
    {
        var actor = Guid.NewGuid();
        var client = Guid.NewGuid();
        var session = Guid.NewGuid();
        var prospective = Guid.NewGuid();
        var fingerprint = Enumerable.Repeat((byte)0x11, 32).ToArray();
        await using var login = await CreateDedicatedLoginAsync(includeDeployer: false);
        await using var connection = await login.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetRuntimeActorAsync(connection, transaction, actor);

        var created = await ClaimAsync(connection, transaction, actor, client, session, "h4-key", fingerprint, prospective);
        var replay = await ClaimAsync(connection, transaction, actor, client, session, "h4-key", fingerprint, Guid.NewGuid());
        var conflict = await ClaimAsync(connection, transaction, actor, client, session, "h4-key", Enumerable.Repeat((byte)0x22, 32).ToArray(), Guid.NewGuid());

        Assert.Equal(new ClaimResult("NewClaim", prospective), created);
        Assert.Equal(new ClaimResult("ExistingMatch", prospective), replay);
        Assert.Equal(new ClaimResult("FingerprintConflict", null), conflict);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task H5_actor_binding_rejects_mismatch_and_missing_context()
    {
        await using var login = await CreateDedicatedLoginAsync(includeDeployer: false);

        await using (var connection = await login.OpenAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await SetRuntimeRoleAsync(connection, transaction);
            var missing = await Assert.ThrowsAsync<PostgresException>(() => ClaimAsync(
                connection, transaction, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "h5-missing",
                new byte[32], Guid.NewGuid()));
            Assert.Equal("P0001", missing.SqlState);
            Assert.Equal("RAW_EXPORT_ACTOR_CONTEXT_MISSING", missing.MessageText);
            await transaction.RollbackAsync();
        }

        await using (var connection = await login.OpenAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            var actor = Guid.NewGuid();
            await SetRuntimeActorAsync(connection, transaction, actor);
            var mismatch = await Assert.ThrowsAsync<PostgresException>(() => ClaimAsync(
                connection, transaction, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "h5-mismatch",
                new byte[32], Guid.NewGuid()));
            Assert.Equal("P0001", mismatch.SqlState);
            Assert.Equal("RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH", mismatch.MessageText);
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task H6_decision_id_reuse_across_distinct_tuples_fails_on_named_unique_constraint()
    {
        var actor = Guid.NewGuid();
        var reusedDecisionId = Guid.NewGuid();
        await using var login = await CreateDedicatedLoginAsync(includeDeployer: false);
        await using var connection = await login.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetRuntimeActorAsync(connection, transaction, actor);

        await ClaimAsync(connection, transaction, actor, Guid.NewGuid(), Guid.NewGuid(), "h6-first", new byte[32], reusedDecisionId);
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => ClaimAsync(
            connection, transaction, actor, Guid.NewGuid(), Guid.NewGuid(), "h6-second",
            Enumerable.Repeat((byte)0x33, 32).ToArray(), reusedDecisionId));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        Assert.Equal("UQ_raw_export_authorization_idempotency_ExportDecisionId", duplicate.ConstraintName);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task H7_concurrent_claim_waits_then_observes_commit_or_retries_after_rollback()
    {
        var actor = Guid.NewGuid();
        var client = Guid.NewGuid();
        var session = Guid.NewGuid();
        var fingerprint = Enumerable.Repeat((byte)0x44, 32).ToArray();
        await using var login = await CreateDedicatedLoginAsync(includeDeployer: true);

        await using (var winnerConnection = await login.OpenAsync())
        await using (var loserConnection = await login.OpenAsync())
        await using (var winnerTransaction = await winnerConnection.BeginTransactionAsync())
        await using (var loserTransaction = await loserConnection.BeginTransactionAsync())
        {
            await SetRuntimeActorAsync(winnerConnection, winnerTransaction, actor);
            await SetRuntimeActorAsync(loserConnection, loserTransaction, actor);
            var winnerId = Guid.NewGuid();
            var loserId = Guid.NewGuid();
            Assert.Equal(new ClaimResult("NewClaim", winnerId), await ClaimAsync(
                winnerConnection, winnerTransaction, actor, client, session, "h7-commit", fingerprint, winnerId));

            var loserClaim = ClaimAsync(loserConnection, loserTransaction, actor, client, session, "h7-commit", fingerprint, loserId);
            await Task.Delay(250);
            Assert.False(loserClaim.IsCompleted, "The losing claim did not block behind the uncommitted winner.");

            await SeedDeniedDecisionAsync(winnerConnection, winnerTransaction, actor, client, session, fingerprint, winnerId);
            await winnerTransaction.CommitAsync();
            Assert.Equal(new ClaimResult("ExistingMatch", winnerId), await loserClaim.WaitAsync(TimeSpan.FromSeconds(5)));
            await loserTransaction.RollbackAsync();
        }

        await using (var winnerConnection = await login.OpenAsync())
        await using (var loserConnection = await login.OpenAsync())
        await using (var winnerTransaction = await winnerConnection.BeginTransactionAsync())
        await using (var loserTransaction = await loserConnection.BeginTransactionAsync())
        {
            await SetRuntimeActorAsync(winnerConnection, winnerTransaction, actor);
            await SetRuntimeActorAsync(loserConnection, loserTransaction, actor);
            var winnerId = Guid.NewGuid();
            var loserId = Guid.NewGuid();
            Assert.Equal(new ClaimResult("NewClaim", winnerId), await ClaimAsync(
                winnerConnection, winnerTransaction, actor, client, session, "h7-rollback", fingerprint, winnerId));

            var loserClaim = ClaimAsync(loserConnection, loserTransaction, actor, client, session, "h7-rollback", fingerprint, loserId);
            await Task.Delay(250);
            Assert.False(loserClaim.IsCompleted, "The losing claim did not block behind the winner before rollback.");

            await winnerTransaction.RollbackAsync();
            Assert.Equal(new ClaimResult("NewClaim", loserId), await loserClaim.WaitAsync(TimeSpan.FromSeconds(5)));
            await loserTransaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task H9_migration_apply_rollback_reapply_preserves_acl_and_b31_objects()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        var b31Before = await B31CatalogSnapshotAsync();
        var b32Before = await B32CatalogSnapshotAsync();
        Assert.Equal(2, b32Before.Length);

        await migrator.MigrateAsync(PreviousMigration);
        Assert.Empty(await B32CatalogSnapshotAsync());
        Assert.Equal(b31Before, await B31CatalogSnapshotAsync());

        await migrator.MigrateAsync(Migration);
        Assert.Equal(b32Before, await B32CatalogSnapshotAsync());
        Assert.Equal(b31Before, await B31CatalogSnapshotAsync());

        await migrator.MigrateAsync(PreviousMigration);
        Assert.Empty(await B32CatalogSnapshotAsync());
        Assert.Equal(b31Before, await B31CatalogSnapshotAsync());

        await migrator.MigrateAsync(Migration);
        Assert.Equal(b32Before, await B32CatalogSnapshotAsync());
        Assert.Equal(b31Before, await B31CatalogSnapshotAsync());
    }

    private static void AssertManifest(FunctionManifest actual, string argumentTypes, string result)
    {
        Assert.Multiple(
            () => Assert.Equal(argumentTypes, actual.ArgumentTypes),
            () => Assert.Equal(result, actual.Result),
            () => Assert.True(actual.SecurityDefiner),
            () => Assert.Equal("tagekyc_raw_export_deployer", actual.Owner),
            () => Assert.True(actual.SearchPathIsPinned),
            () => Assert.False(actual.PublicExecute),
            () => Assert.True(actual.RuntimeExecute));
    }

    private async Task<Guid> SeedSessionAsync()
    {
        await using var db = postgres.CreateDbContext();
        var session = VerificationSession.Create(
            Guid.NewGuid(), "b32-lock-subject", VerificationProfile.StandardEkycProfile, "raw-export",
            [RequiredCheckType.DocumentNfc], DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);
        await new EfVerificationSessionRepository(db).AddAsync(session);
        return session.Id;
    }

    private async Task DeleteSessionAsync(Guid sessionId)
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM tagekyc.verification_sessions WHERE \"Id\"={sessionId};");
    }

    private async Task<TestLogin> CreateDedicatedLoginAsync(bool includeDeployer)
    {
        var role = $"tagekyc_b32_{Guid.NewGuid():N}";
        var password = $"B32_{Guid.NewGuid():N}";
        await using var connection = await OpenAdminAsync();
        await ExecuteAsync(connection, null, $"CREATE ROLE \"{role}\" WITH LOGIN PASSWORD '{password}' NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;");
        await ExecuteAsync(connection, null, $"GRANT tagekyc_runtime TO \"{role}\";");
        if (includeDeployer)
        {
            await ExecuteAsync(connection, null, $"GRANT tagekyc_raw_export_deployer TO \"{role}\";");
        }

        var builder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Username = role,
            Password = password,
            Pooling = false,
        };
        return new TestLogin(postgres.ConnectionString, builder.ConnectionString, role);
    }

    private async Task<NpgsqlConnection> OpenAdminAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task SetRuntimeRoleAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE tagekyc_runtime;");
    }

    private static async Task SetRuntimeActorAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid actor)
    {
        await SetRuntimeRoleAsync(connection, transaction);
        await ExecuteAsync(connection, transaction, $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id', '{actor}', true);");
    }

    private static async Task<LockedSession> LockSessionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid sessionId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_lock_verification_session_for_authorization(@session_id);",
            connection, transaction);
        command.Parameters.AddWithValue("session_id", sessionId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new LockedSession(reader.GetGuid(0), reader.GetString(1), reader.GetString(2));
    }

    private static async Task<ClaimResult> ClaimAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid principalId,
        Guid clientApplicationId,
        Guid requestedVerificationSessionId,
        string idempotencyKey,
        byte[] fingerprintHash,
        Guid prospectiveDecisionId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT outcome, export_decision_id
            FROM tagekyc.raw_export_claim_or_read_authorization_idempotency(
                @principal_id, @client_application_id, @requested_session_id,
                @idempotency_key, @fingerprint_hash, @prospective_decision_id);
            """, connection, transaction);
        command.Parameters.AddWithValue("principal_id", principalId);
        command.Parameters.AddWithValue("client_application_id", clientApplicationId);
        command.Parameters.AddWithValue("requested_session_id", requestedVerificationSessionId);
        command.Parameters.AddWithValue("idempotency_key", idempotencyKey);
        command.Parameters.AddWithValue("fingerprint_hash", fingerprintHash);
        command.Parameters.AddWithValue("prospective_decision_id", prospectiveDecisionId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new ClaimResult(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetGuid(1));
    }

    private static async Task SeedDeniedDecisionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid principalId,
        Guid clientApplicationId,
        Guid requestedVerificationSessionId,
        byte[] fingerprintHash,
        Guid decisionId)
    {
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(connection, transaction, "SELECT pg_catalog.set_config('tagekyc.raw_export_authorization_insert_context', 'decision', true);");
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tagekyc.raw_export_authorization_decisions
                ("ExportDecisionId", "PrincipalId", "ClientApplicationId", "ApiKeyId",
                 "RequestedVerificationSessionId", "PolicyId", "PolicyVersion", "FingerprintHash",
                 "RawClassSelectionMode", "Outcome", "PrimaryCause", "DecidedAtUtc")
            VALUES
                (@decision_id, @principal_id, @client_id, @api_key_id,
                 @session_id, @policy_id, 1, @fingerprint_hash,
                 'DefaultPolicySet', 'Denied', 'SESSION_NOT_FOUND', pg_catalog.transaction_timestamp());
            """, connection, transaction);
        command.Parameters.AddWithValue("decision_id", decisionId);
        command.Parameters.AddWithValue("principal_id", principalId);
        command.Parameters.AddWithValue("client_id", clientApplicationId);
        command.Parameters.AddWithValue("api_key_id", Guid.NewGuid());
        command.Parameters.AddWithValue("session_id", requestedVerificationSessionId);
        command.Parameters.AddWithValue("policy_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fingerprint_hash", fingerprintHash);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string[]> B32CatalogSnapshotAsync()
    {
        await using var connection = await OpenAdminAsync();
        return await QueryStringsAsync(connection,
            """
            SELECT p.proname || ':owner=' || owner.rolname || ':definer=' || p.prosecdef::text ||
                   ':config=' || COALESCE(array_to_string(p.proconfig, ','), '') ||
                   ':public=' || pg_catalog.has_function_privilege('public', p.oid, 'EXECUTE')::text ||
                   ':runtime=' || pg_catalog.has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')::text
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc' AND p.proname = ANY(@functions)
            ORDER BY p.proname;
            """, new NpgsqlParameter("functions", Functions));
    }

    private async Task<string[]> B31CatalogSnapshotAsync()
    {
        await using var connection = await OpenAdminAsync();
        return await QueryStringsAsync(connection,
            """
            SELECT value FROM (
                SELECT 'table:' || c.relname || ':owner=' || owner.rolname ||
                       ':select=' || pg_catalog.has_table_privilege('tagekyc_runtime', c.oid, 'SELECT')::text ||
                       ':insert=' || pg_catalog.has_table_privilege('tagekyc_runtime', c.oid, 'INSERT')::text ||
                       ':update=' || pg_catalog.has_table_privilege('tagekyc_runtime', c.oid, 'UPDATE')::text ||
                       ':delete=' || pg_catalog.has_table_privilege('tagekyc_runtime', c.oid, 'DELETE')::text AS value
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_roles owner ON owner.oid = c.relowner
                WHERE n.nspname = 'tagekyc' AND c.relname = ANY(@tables)
                UNION ALL
                SELECT 'function:' || p.proname || ':owner=' || owner.rolname ||
                       ':public=' || pg_catalog.has_function_privilege('public', p.oid, 'EXECUTE')::text ||
                       ':runtime=' || pg_catalog.has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')::text
                FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                JOIN pg_roles owner ON owner.oid = p.proowner
                WHERE n.nspname = 'tagekyc' AND p.proname = ANY(@internal_functions)
                UNION ALL
                SELECT 'trigger:' || t.tgname
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'tagekyc' AND c.relname = ANY(@tables) AND NOT t.tgisinternal
                UNION ALL
                SELECT 'constraint:' || con.conname
                FROM pg_constraint con
                JOIN pg_class c ON c.oid = con.conrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'tagekyc' AND c.relname = ANY(@tables) AND con.contype <> 't'
            ) snapshot
            ORDER BY value;
            """,
            new NpgsqlParameter("tables", new[]
            {
                "raw_export_authorization_idempotency", "raw_export_authorization_decisions",
                "raw_export_decision_eligibility_causes", "raw_export_decision_fulfillment_refs",
                "raw_export_decision_classes", "raw_export_authorization_permits", "raw_export_permit_classes",
            }),
            new NpgsqlParameter("internal_functions", new[]
            {
                "enforce_raw_export_authorization_insert", "enforce_raw_export_decision_child_same_transaction",
                "enforce_raw_export_permit_child_same_transaction", "enforce_raw_export_permit_has_classes",
            }));
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string[]> QueryStringsAsync(NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values.ToArray();
    }

    private sealed class TestLogin(string adminConnectionString, string connectionString, string role) : IAsyncDisposable
    {
        public async Task<NpgsqlConnection> OpenAsync()
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await ExecuteAsync(connection, null, $"REVOKE tagekyc_runtime, tagekyc_raw_export_deployer FROM \"{role}\"; DROP ROLE \"{role}\";");
        }
    }

    private sealed record FunctionManifest(
        string Name,
        string ArgumentTypes,
        string Result,
        bool SecurityDefiner,
        string Owner,
        bool SearchPathIsPinned,
        bool PublicExecute,
        bool RuntimeExecute);

    private sealed record LockedSession(Guid ClientApplicationId, string SubjectRef, string State);
    private sealed record ClaimResult(string Outcome, Guid? ExportDecisionId);
}
