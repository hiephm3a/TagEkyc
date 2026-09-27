using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Reflection;
using System.Text.RegularExpressions;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.Persistence.Migrations;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3MigrationTests(PostgresPersistenceFixture postgres)
{
    internal const string MigrationId = "20260913120000_Tip88C1C6BA3RetainedIngressComposition";
    private const string CurrentMigrationId = "20260926120000_RawExportAssemblyPostSealRecovery";
    private const string PredecessorId = "20260908120000_Tip88C1C6BA1Foundation";
    private const string R20 = "tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz,uuid,uuid,jsonb)";
    private const string CompletionSignature = "tagekyc.complete_raw_export_source_ingress_claim(uuid,text,text,text,uuid,bigint,bigint,text,timestamptz,text,bytea,integer,text,integer,bytea,integer,text,integer,bytea,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,text,integer,text,integer,text,bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)";
    private const string RetainedBeginSignature = "tagekyc.raw_export_begin_retained_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamptz,timestamptz,timestamptz,integer,text,integer,uuid,integer,integer,uuid,bigint)";
    private const string BoundReaderSignature = "tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz)";

    [Fact]
    public async Task A3_MigrationDiscovery_FromEmptyMatchesCurrentModelAndHistory()
    {
        // Reset uses EnsureDeleted followed by the real EF migration chain, never EnsureCreated.
        await postgres.ResetDatabaseAsync();
        await using var db = postgres.CreateDbContext();
        var discovered = db.Database.GetMigrations().ToArray();
        Assert.Equal(1, discovered.Count(id => id == MigrationId));
        Assert.Equal(CurrentMigrationId, discovered.Last());
        Assert.Equal(discovered, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.False(db.Database.HasPendingModelChanges());
        // Materialize every column, not COUNT(*), so missing mapped fields fail on PostgreSQL.
        Assert.Empty(await db.Set<RawSourceConsentReferenceRow>().AsNoTracking().ToListAsync());
        Assert.Empty(await db.Set<RawSourceConsentReferenceEventRow>().AsNoTracking().ToListAsync());
        Assert.Empty(await db.Set<RawSourceConsentBindingRow>().AsNoTracking().ToListAsync());
        Assert.Empty(await db.Set<RawSourceRetentionPermitRow>().AsNoTracking().ToListAsync());
        Assert.Empty(await db.Set<RawSourceRetentionPermitClassRow>().AsNoTracking().ToListAsync());
        Assert.Empty(await db.Set<RawExportSourceEncryptionAttemptRow>().AsNoTracking().ToListAsync());
        var modelIndexes = db.Model.FindEntityType(typeof(RawExportAuthoritySnapshotRow))!
            .GetIndexes().Select(index => index.GetDatabaseName()).Order(StringComparer.Ordinal).ToArray();
        var databaseIndexes = await db.Database.SqlQueryRaw<string>("""
            SELECT indexname AS "Value" FROM pg_catalog.pg_indexes
            WHERE schemaname='tagekyc' AND tablename='raw_export_authority_snapshots'
              AND indexname NOT IN (SELECT c.conname FROM pg_catalog.pg_constraint c
                WHERE c.conrelid='tagekyc.raw_export_authority_snapshots'::regclass
                  AND c.contype IN ('p','u'))
            ORDER BY indexname COLLATE "C"
            """).ToArrayAsync();
        Assert.Equal(modelIndexes, databaseIndexes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_MigrationPopulationGuard_WaitsForWriterThenRejectsWithoutLoss(bool down)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_population_race");
        if (down) await Execute(isolated, MigrationId);
        else await Execute(isolated, PredecessorId);
        Assert.Equal(down ? MigrationId : PredecessorId, (await History(isolated)).Last());
        await using var writer = isolated.CreateDbContext();
        var client = Tip88C1C6BA3ConsentRetentionTests.Client;
        var principal = Tip88C1C6BA3ConsentRetentionTests.Principal;
        var now = DateTimeOffset.UtcNow;
        var session = VerificationSession.Create(client, "synthetic-subject", VerificationProfile.ChallengeBoundEkycProfile,
            "synthetic-migration", [RequiredCheckType.DocumentNfc], now.AddHours(1), now, challenge: "synthetic-challenge");
        await new EfVerificationSessionRepository(writer).AddAsync(session);
        if (down) await Tip88C1C6BA3ConsentRetentionTests.Grant(writer, "SubjectConsentRecorder");
        var before = await Bodies(isolated);
        var beforeHistory = await History(isolated);
        var beforeCatalog = down ? await GuardCatalog(isolated) : null;
        await using var transaction = await writer.Database.BeginTransactionAsync();
        if (down)
        {
            await writer.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{principal.ToString("D")},true)");
            Assert.Equal("Bound", await writer.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_record_consent_reference(
                 {principal},{client},{session.Id},'synthetic-existing-consent','source-v1',0,'text-v1','source-hash',
                 {now.AddMinutes(-1)},{now.AddHours(1)},{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync());
        }
        else
        {
            Assert.Equal("CREATED", await writer.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
                 {client},{session.Id},'Issue',NULL::uuid,NULL::bigint,{Guid.NewGuid()},{Guid.NewGuid()},
                 'abcdefgh1234',{new byte[32]},1,{new byte[32]},{now})
                """).SingleAsync());
        }
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = Execute(isolated, down ? PredecessorId : MigrationId, pid);
        await using var observer = isolated.CreateDbContext();
        try
        {
            var backend = await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting = false;
            for (var i = 0; i < 100 && !waiting; i++)
            {
                waiting = await observer.Database.SqlQuery<bool>($"""
                    SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks WHERE pid={backend}
                     AND relation='tagekyc.verification_sessions'::regclass
                     AND mode='AccessExclusiveLock' AND NOT granted) AS "Value"
                    """).SingleAsync();
                if (!waiting) await Task.Delay(50);
            }
            Assert.True(waiting, "Migration must wait at the real session table BEFORE its population check.");
            Assert.False(running.IsCompleted);
            await transaction.CommitAsync();
            var failure = await Assert.ThrowsAsync<PostgresException>(() => running);
            Assert.Equal(down ? "A3_RETENTION_DOWN_POPULATED" : "A3_CAPTURE_LINEAGE_CUTOVER_ACTIVE", failure.MessageText);
            Assert.Equal(before, await Bodies(isolated));
            Assert.Equal(beforeHistory, await History(isolated));
            if (down) Assert.Equal(beforeCatalog, await GuardCatalog(isolated));
            if (down)
            {
                foreach (var table in new[] { "raw_source_consent_references", "raw_source_consent_reference_events", "raw_source_consent_bindings" })
                    Assert.Equal(1, await observer.Database.SqlQueryRaw<int>($"SELECT count(*)::integer AS \"Value\" FROM tagekyc.{table}").SingleAsync());
            }
            else
            {
                Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capabilities").SingleAsync());
                Assert.False(await observer.Database.SqlQueryRaw<bool>("SELECT to_regclass('tagekyc.raw_source_consent_references') IS NOT NULL AS \"Value\"").SingleAsync());
            }
        }
        finally
        {
            if (transaction.GetDbTransaction().Connection is not null) await transaction.RollbackAsync();
            try { await running.WaitAsync(TimeSpan.FromSeconds(15)); } catch (PostgresException) { }
        }
    }

    // Actual EF history roundtrip. CP10's independent checkout representations
    // and the final complete A3 function catalogue remain separate obligations.
    [Fact]
    public async Task A3_CaptureLineageMigration_EmptyRoundTripRestoresPredecessor()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_empty_roundtrip");
        await Execute(isolated, PredecessorId);
        var before = await Bodies(isolated);
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var installed = await Bodies(isolated);
        var terminalFunctions = await TerminalFunctions(isolated);
        Assert.Equal(4, terminalFunctions.Length);
        await using (var observer = isolated.CreateDbContext())
        {
            Assert.Equal(new[] {
                "R2TerminalIntentAtUtc|timestamp with time zone|YES|",
                "R2TerminalIntentCode|character varying(64)|YES|",
                "R2TerminalIntentDisposition|character varying(32)|YES|",
                "R2TerminalOutcomeCode|text|YES|" },
                await observer.Database.SqlQueryRaw<string>("""
                    SELECT a.attname||'|'||pg_catalog.format_type(a.atttypid,a.atttypmod)||'|'||
                     CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END||'|'||
                     COALESCE(pg_catalog.pg_get_expr(d.adbin,d.adrelid),'') AS "Value"
                    FROM pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_attrdef d
                     ON d.adrelid=a.attrelid AND d.adnum=a.attnum
                    WHERE a.attrelid='tagekyc.raw_export_source_encryption_attempts'::regclass
                     AND a.attname IN ('R2TerminalIntentAtUtc','R2TerminalIntentCode',
                      'R2TerminalIntentDisposition','R2TerminalOutcomeCode')
                    ORDER BY a.attname
                    """).ToArrayAsync());
        }
        Assert.NotEqual(before["capture_runtime_issue_or_replace_capability"], installed["capture_runtime_issue_or_replace_capability"]);
        await Execute(isolated, PredecessorId);
        Assert.Equal(before, await Bodies(isolated));
        Assert.Empty(await TerminalFunctions(isolated));
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        Assert.Equal(installed, await Bodies(isolated));
        Assert.Equal(terminalFunctions, await TerminalFunctions(isolated));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\r")]
    public async Task A3_CaptureLineageMigration_BodyDriftRejectsDownAtomically(string drift)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_down_drift");
        await Execute(isolated, MigrationId);
        Assert.Equal(MigrationId, (await History(isolated)).Last());
        Assert.All(await GuardCatalog(isolated), row => Assert.Equal(row.ExpectedHash, row.ActualHash));
        await using (var db = isolated.CreateDbContext())
        {
            var body = await db.Database.SqlQuery<string>($"SELECT prosrc AS \"Value\" FROM pg_catalog.pg_proc WHERE oid=pg_catalog.to_regprocedure({R20})").SingleAsync();
            var definition = await db.Database.SqlQuery<string>($"SELECT pg_catalog.pg_get_functiondef(pg_catalog.to_regprocedure({R20})) AS \"Value\"").SingleAsync();
            Assert.Contains(body, definition, StringComparison.Ordinal);
            var mutation = definition.Replace(body, body + drift, StringComparison.Ordinal);
            Assert.NotEqual(definition, mutation);
            await db.Database.OpenConnectionAsync();
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = mutation;
            await command.ExecuteNonQueryAsync();
        }
        var beforeAttempt = await Bodies(isolated);
        var beforeHistory = await History(isolated);
        var beforeCatalog = await GuardCatalog(isolated);
        var mismatch = Assert.Single(beforeCatalog, row => row.ExpectedHash != row.ActualHash);
        Assert.Equal(R20, mismatch.Signature);
        var failure = await Assert.ThrowsAsync<PostgresException>(() => Execute(isolated, PredecessorId));
        Assert.Equal("A3_CAPTURE_CURRENT_BODY_MISMATCH", failure.MessageText);
        Assert.Equal(beforeAttempt, await Bodies(isolated));
        Assert.Equal(beforeHistory, await History(isolated));
        Assert.Equal(beforeCatalog, await GuardCatalog(isolated));
        await using var observer = isolated.CreateDbContext();
        var retainedDrift = await observer.Database.SqlQuery<string>($"SELECT prosrc AS \"Value\" FROM pg_catalog.pg_proc WHERE oid=pg_catalog.to_regprocedure({R20})").SingleAsync();
        Assert.EndsWith(drift, retainedDrift, StringComparison.Ordinal);
        Assert.True(await observer.Database.SqlQueryRaw<bool>(
            "SELECT to_regclass('tagekyc.raw_source_consent_references') IS NOT NULL AS \"Value\"").SingleAsync());
    }

    [Fact]
    public async Task A3_CaptureCurrentGuard_ExactBoundaryAllEntriesMatch()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_guard_30_30");
        await Execute(isolated, MigrationId);
        var catalog = await GuardCatalog(isolated);
        Assert.Equal(30, catalog.Length);
        foreach (var row in catalog)
        {
            Assert.True(row.ExpectedHash == row.ActualHash,
                $"Guard mismatch: {row.Signature} expected={row.ExpectedHash} actual={row.ActualHash}");
            Console.WriteLine($"A3_GUARD_MATCH\t{row.Signature}\t{row.ExpectedHash}\t{row.ActualHash}");
        }
    }

    [Theory]
    [InlineData(CompletionSignature)]
    [InlineData(RetainedBeginSignature)]
    [InlineData(BoundReaderSignature)]
    public async Task A3_CaptureCurrentGuard_MismatchReportsExactSignature(string signature)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_guard_diagnostic");
        await Execute(isolated, MigrationId);
        Assert.All(await GuardCatalog(isolated), row => Assert.Equal(row.ExpectedHash, row.ActualHash));

        await AppendBodyDrift(isolated, signature, " ");
        var beforeBodies = await Bodies(isolated);
        var beforeHistory = await History(isolated);
        var beforeCatalog = await GuardCatalog(isolated);
        var mismatch = Assert.Single(beforeCatalog, row => row.ExpectedHash != row.ActualHash);
        Assert.Equal(signature, mismatch.Signature);

        var failure = await Assert.ThrowsAsync<PostgresException>(() => Execute(isolated, PredecessorId));
        Assert.Equal("A3_CAPTURE_CURRENT_BODY_MISMATCH", failure.MessageText);
        Assert.Equal(signature, failure.Detail);
        Assert.Equal(beforeBodies, await Bodies(isolated));
        Assert.Equal(beforeHistory, await History(isolated));
        Assert.Equal(beforeCatalog, await GuardCatalog(isolated));
        Console.WriteLine($"A3_GUARD_DIAGNOSTIC\t{failure.MessageText}\t{failure.Detail}");
    }

    [Fact]
    public async Task A3_LatestToPredecessor_GuardFailureStopsAtExactA3Boundary()
    {
        await using var latest = await postgres.CreateDisposableCurrentDatabaseAsync("a3_latest_partial_progress");
        await using var control = await postgres.CreateDisposableCurrentDatabaseAsync("a3_exact_boundary_control");
        await Execute(control, MigrationId);
        Assert.Equal(MigrationId, (await History(control)).Last());
        await AppendBodyDrift(latest, R20, " ");
        await AppendBodyDrift(control, R20, " ");

        var latestHistory = await History(latest);
        var latestCatalog = await GuardCatalog(latest);
        var expectedHistory = await History(control);
        var expectedBodies = await Bodies(control);
        var expectedCatalog = await GuardCatalog(control);
        var expectedMismatch = Assert.Single(expectedCatalog, row => row.ExpectedHash != row.ActualHash);
        Assert.Equal(R20, expectedMismatch.Signature);

        var failure = await Assert.ThrowsAsync<PostgresException>(() => Execute(latest, PredecessorId));
        Assert.Equal("A3_CAPTURE_CURRENT_BODY_MISMATCH", failure.MessageText);
        Assert.Equal(expectedHistory, await History(latest));
        Assert.Equal(expectedBodies, await Bodies(latest));
        Assert.Equal(expectedCatalog, await GuardCatalog(latest));
        Assert.Equal(MigrationId, (await History(latest)).Last());

        var afterCatalog = await GuardCatalog(latest);
        var removedMigrations = latestHistory.Except(expectedHistory, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[]
        {
            "20260923090000_RawExportAssemblyDurableWorkSource",
            "20260924120000_RawExportDeliveryRecipientCredential",
            "20260924130000_RawExportLegacyConsentClassFence",
            "20260925090000_RawExportAssemblyRetainedModeWorkSource",
            "20260926120000_RawExportAssemblyPostSealRecovery"
        }, removedMigrations);
        var changedPairs = latestCatalog.Zip(afterCatalog)
            .Where(pair => pair.First != pair.Second)
            .ToArray();
        foreach (var pair in changedPairs)
        {
            Assert.Equal(pair.First.Signature, pair.Second.Signature);
            Assert.Equal(pair.First.ExpectedHash, pair.Second.ExpectedHash);
            Assert.Equal(pair.First.Owner, pair.Second.Owner);
            Assert.Equal(pair.First.SecurityDefiner, pair.Second.SecurityDefiner);
            Assert.Equal(pair.First.Configuration, pair.Second.Configuration);
            Assert.Equal(pair.First.Acl, pair.Second.Acl);
        }
        var changed = changedPairs.Select(pair => pair.First.Signature).ToArray();
        Assert.Equal(new[]
        {
            "tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)",
            "tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)",
            "tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)"
        }, changed);
        Console.WriteLine($"A3_LATEST_HISTORY_BEFORE={string.Join(',', latestHistory)}");
        Console.WriteLine($"A3_HISTORY_AFTER_REJECTION={string.Join(',', expectedHistory)}");
        Console.WriteLine($"A3_MIGRATIONS_DOWN_COMPLETED={string.Join(',', removedMigrations)}");
        Console.WriteLine($"A3_GUARD_CATALOG_CHANGED={string.Join(',', changed)}");
        foreach (var pair in changedPairs)
            Console.WriteLine($"A3_CATALOG_DELTA\t{pair.First.Signature}\t{pair.First.ActualHash}\t{pair.Second.ActualHash}\tmetadata-unchanged");
    }

    private static async Task<SortedDictionary<string, string>> Bodies(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        await using var observer = isolated.CreateDbContext();
        await observer.Database.OpenConnectionAsync();
        await using var command = observer.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT p.proname,p.prosrc FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname IN ('capture_runtime_issue_or_replace_capability','capture_runtime_bind_capability','raw_export_append_subject_consent_withdrawn',
             'enforce_raw_export_authority_snapshot_insert','raw_export_append_authority_snapshot','raw_export_withdraw_authority_snapshot',
             'raw_export_revoke_authority_snapshot','raw_export_resolve_current_authority_for_source',
             'raw_export_stage_verified_source_ciphertext','raw_export_commit_staged_source','raw_export_publish_available_source',
             'complete_raw_export_source_ingress_claim','enforce_raw_export_source_core_write',
             'complete_raw_export_source_ingress_claim_with_r2_handoff','enforce_raw_export_source_head_write',
             'raw_export_prepare_attempt_key_reservation','raw_export_begin_provisional_object_custody',
             'raw_export_terminate_source_encryption_attempt','raw_export_record_r2_terminal_outcome',
             'raw_export_read_source_encryption_context','raw_export_freeze_job_source_bindings',
             'raw_export_seal_authenticated_assembly','raw_export_c3_current_authority_eligible',
             'raw_export_begin_recipient_package_delivery_stream')
            ORDER BY p.proname
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync()) values.Add(reader.GetString(0), reader.GetString(1).Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal(24, values.Count); return values;
    }

    private static async Task<string[]> History(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        await using var db = isolated.CreateDbContext();
        return (await db.Database.GetAppliedMigrationsAsync()).ToArray();
    }

    private static async Task AppendBodyDrift(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        string signature,
        string drift)
    {
        await using var db = isolated.CreateDbContext();
        var body = await db.Database.SqlQuery<string>($"SELECT prosrc AS \"Value\" FROM pg_catalog.pg_proc WHERE oid=pg_catalog.to_regprocedure({signature})").SingleAsync();
        var definition = await db.Database.SqlQuery<string>($"SELECT pg_catalog.pg_get_functiondef(pg_catalog.to_regprocedure({signature})) AS \"Value\"").SingleAsync();
        Assert.Contains(body, definition, StringComparison.Ordinal);
        var mutation = definition.Replace(body, body + drift, StringComparison.Ordinal);
        Assert.NotEqual(definition, mutation);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = mutation;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<GuardObservation[]> GuardCatalog(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        var guard = Assert.IsType<string>(typeof(Tip88C1C6BA3RetainedIngressComposition)
            .GetField("CaptureCurrentGuard", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue());
        var expected = Regex.Matches(guard, "\\('(?<signature>[^']+)','(?<hash>[0-9a-f]{64})'\\)")
            .Select(match => (Signature: match.Groups["signature"].Value, Hash: match.Groups["hash"].Value))
            .ToArray();
        Assert.Equal(30, expected.Length);

        var values = new List<GuardObservation>(expected.Length);
        await using var db = isolated.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        foreach (var entry in expected)
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT encode(tagekyc_extensions.digest(convert_to(replace(p.prosrc,chr(13)||chr(10),chr(10)),'UTF8'),'sha256'),'hex'),
                 pg_catalog.pg_get_userbyid(p.proowner),p.prosecdef::text,coalesce(p.proconfig::text,''),coalesce(p.proacl::text,'')
                FROM pg_catalog.pg_proc p WHERE p.oid=pg_catalog.to_regprocedure(@signature)
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "signature";
            parameter.Value = entry.Signature;
            command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), $"Missing guard function {entry.Signature}");
            values.Add(new GuardObservation(entry.Signature, entry.Hash, reader.GetString(0), reader.GetString(1),
                reader.GetString(2), reader.GetString(3), reader.GetString(4)));
            Assert.False(await reader.ReadAsync(), $"Ambiguous guard function {entry.Signature}");
        }
        return values.ToArray();
    }

    private sealed record GuardObservation(
        string Signature,
        string ExpectedHash,
        string ActualHash,
        string Owner,
        string SecurityDefiner,
        string Configuration,
        string Acl);

    private static async Task<string[]> TerminalFunctions(PostgresPersistenceFixture.DisposableCurrentDatabase isolated)
    {
        await using var db = isolated.CreateDbContext();
        return await db.Database.SqlQueryRaw<string>("""
            SELECT p.oid::regprocedure::text||'|'||replace(p.prosrc,chr(13)||chr(10),chr(10))||'|'||
             pg_get_userbyid(p.proowner)||'|'||p.prosecdef::text||'|'||p.proconfig::text||'|'||p.proacl::text AS "Value"
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname IN
             ('raw_export_record_retained_r2_terminal_intent','raw_export_finalize_retained_r2_terminal',
              'raw_export_read_retained_source_continuation','raw_export_list_retained_source_continuations')
            ORDER BY p.proname
            """).ToArrayAsync();
    }

    private static async Task Execute(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, string target,
        TaskCompletionSource<int>? pid = null)
    {
        await using var db = isolated.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        if (pid is not null) pid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
        await db.GetService<IMigrator>().MigrateAsync(target);
        Assert.Equal(target, (await db.Database.GetAppliedMigrationsAsync()).Last());
    }
}
