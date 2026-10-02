using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

public sealed class SiteQualificationMeasurementPlanePostgresTests
{
    private const string Previous = "20260926120000_RawExportAssemblyPostSealRecovery";
    private const string Current = "20261002120000_ProductionApplicationPersistencePrincipal";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Consume_rejects_each_null_binding_arm_without_consuming_the_run(int nullArm)
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var store = new PostgresSiteRawIngressQualificationRunStore(db);
        var now = DateTimeOffset.UtcNow;
        var settings = new CaptureRuntimeSiteTransportQualificationSettings(
            "synthetic-site", "https://qualification.example.test", "revision-null",
            TimeSpan.FromMinutes(5));
        var binding = new SiteRawIngressQualificationRunBinding(Guid.NewGuid(), 3, Guid.NewGuid(),
            new string('a', 64), "image/jpeg", 19, new string('b', 64));
        var owner = Guid.NewGuid();
        Assert.True(await store.EnrollSyntheticCredentialAsync(new(settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding.CredentialId,
            binding.CredentialGeneration, now.AddMinutes(5), owner), now, CancellationToken.None));
        var run = await store.RegisterAsync(new(Guid.NewGuid(), settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding,
            SiteRawIngressQualificationRunMode.FullBodyHeldCommit, now.AddMinutes(1), owner),
            now, CancellationToken.None);
        Assert.NotNull(run);
        object[] arms = [binding.CredentialId, binding.CredentialGeneration,
            binding.IngressIdempotencyKey, binding.IngressMetadataSha256, binding.MediaType,
            binding.ContentLength, binding.PlaintextSha256];
        arms[nullArm] = DBNull.Value;

        Assert.False(await ScalarAsync<bool>(db,
            "SELECT tagekyc.site_qualification_consume_authenticated($1,$2,$3,$4,$5,$6,$7,$8)",
            run!.QualificationRunId, arms[0], arms[1], arms[2], arms[3], arms[4], arms[5], arms[6]));
        Assert.Equal("Registered", await ScalarAsync<string>(db,
            "SELECT \"State\" FROM tagekyc.site_raw_ingress_qualification_runs WHERE \"QualificationRunId\"=$1",
            run.QualificationRunId));
    }

    [Fact]
    public async Task Consume_rechecks_database_clock_after_waiting_past_expiry()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var registrationDb = isolated.CreateDbContext();
        var registrationStore = new PostgresSiteRawIngressQualificationRunStore(registrationDb);
        var settings = new CaptureRuntimeSiteTransportQualificationSettings(
            "synthetic-site", "https://qualification.example.test", "revision-ttl",
            TimeSpan.FromMinutes(5));
        var binding = new SiteRawIngressQualificationRunBinding(Guid.NewGuid(), 1, Guid.NewGuid(),
            new string('a', 64), "image/jpeg", 19, new string('b', 64));
        var owner = Guid.NewGuid();
        var staleCallerTime = DateTimeOffset.UtcNow;
        Assert.True(await registrationStore.EnrollSyntheticCredentialAsync(new(settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding.CredentialId,
            binding.CredentialGeneration, staleCallerTime.AddMinutes(5), owner),
            staleCallerTime, CancellationToken.None));
        var run = await registrationStore.RegisterAsync(new(Guid.NewGuid(), settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding,
            SiteRawIngressQualificationRunMode.FullBodyHeldCommit,
            staleCallerTime.AddMilliseconds(900), owner), staleCallerTime, CancellationToken.None);
        Assert.NotNull(run);

        await using var blocker = new NpgsqlConnection(isolated.ConnectionString);
        await blocker.OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand(
            "SELECT 1 FROM tagekyc.site_raw_ingress_qualification_runs WHERE \"QualificationRunId\"=$1 FOR UPDATE",
            blocker, transaction))
        {
            command.Parameters.Add(new NpgsqlParameter { Value = run!.QualificationRunId });
            Assert.Equal(1, await command.ExecuteScalarAsync());
        }

        await using var consumerDb = isolated.CreateDbContext();
        var consumer = new PostgresSiteRawIngressQualificationRunStore(consumerDb);
        var consume = consumer.ConsumeAuthenticatedAsync(run!.QualificationRunId, binding,
            staleCallerTime, CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        await transaction.CommitAsync();

        Assert.False(await consume);
    }

    [Fact]
    public async Task Two_independent_consumers_can_consume_the_same_run_only_once()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        var settings = new CaptureRuntimeSiteTransportQualificationSettings(
            "synthetic-site", "https://qualification.example.test", "revision-race",
            TimeSpan.FromMinutes(5));
        var binding = new SiteRawIngressQualificationRunBinding(Guid.NewGuid(), 1, Guid.NewGuid(),
            new string('a', 64), "image/jpeg", 19, new string('b', 64));
        var now = DateTimeOffset.UtcNow;
        await using var registrationDb = isolated.CreateDbContext();
        var registration = new PostgresSiteRawIngressQualificationRunStore(registrationDb);
        var owner = Guid.NewGuid();
        Assert.True(await registration.EnrollSyntheticCredentialAsync(new(settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding.CredentialId,
            binding.CredentialGeneration, now.AddMinutes(5), owner), now, CancellationToken.None));
        var run = await registration.RegisterAsync(new(Guid.NewGuid(), settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding,
            SiteRawIngressQualificationRunMode.FullBodyHeldCommit, now.AddMinutes(1),
            owner), now, CancellationToken.None);
        Assert.NotNull(run);

        await using var firstDb = isolated.CreateDbContext();
        await using var secondDb = isolated.CreateDbContext();
        var first = new PostgresSiteRawIngressQualificationRunStore(firstDb);
        var second = new PostgresSiteRawIngressQualificationRunStore(secondDb);
        var results = await Task.WhenAll(
            first.ConsumeAuthenticatedAsync(run!.QualificationRunId, binding, now,
                CancellationToken.None),
            second.ConsumeAuthenticatedAsync(run.QualificationRunId, binding, now,
                CancellationToken.None));

        Assert.Single(results, value => value);
        Assert.Single(results, value => !value);
    }

    [Fact]
    public async Task Exact_binding_single_use_causal_handshake_and_security_metadata_round_trip()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var store = new PostgresSiteRawIngressQualificationRunStore(db);
        var now = DateTimeOffset.UtcNow;
        var credential = Guid.NewGuid();
        var idempotency = Guid.NewGuid();
        var binding = new SiteRawIngressQualificationRunBinding(credential, 9, idempotency,
            new string('a', 64), "image/jpeg", 19, new string('b', 64));
        var owner = Guid.NewGuid();
        var settings = new CaptureRuntimeSiteTransportQualificationSettings(
            "synthetic-site", "https://qualification.example.test", "revision-1",
            TimeSpan.FromMinutes(5));
        var access = new SiteRawIngressQualificationRunAccess(owner, settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision);
        await Assert.ThrowsAsync<PostgresException>(() => store.RegisterAsync(new(Guid.NewGuid(),
            settings.SiteId, settings.EndpointOrigin, settings.DeploymentRevision, binding,
            SiteRawIngressQualificationRunMode.FullBodyHeldCommit, now.AddMinutes(1), owner),
            now, CancellationToken.None));
        Assert.True(await store.EnrollSyntheticCredentialAsync(new(settings.SiteId,
            settings.EndpointOrigin, settings.DeploymentRevision, binding.CredentialId,
            binding.CredentialGeneration, now.AddMinutes(5), owner), now, CancellationToken.None));
        var run = await store.RegisterAsync(new(Guid.NewGuid(), settings.SiteId, settings.EndpointOrigin,
            settings.DeploymentRevision, binding, SiteRawIngressQualificationRunMode.FullBodyHeldCommit,
            now.AddMinutes(1), owner), now, CancellationToken.None);
        Assert.NotNull(run);
        Assert.True(await ScalarAsync<bool>(db, """
            SELECT i.indisunique FROM pg_catalog.pg_index i
            JOIN pg_catalog.pg_class c ON c.oid=i.indexrelid
            WHERE c.relname='ix_site_raw_ingress_qualification_binding'
            """));

        Assert.Null(await store.ObserveRawPostAsync(settings,
            binding with { CredentialId = Guid.NewGuid() }, now.AddMilliseconds(1),
            CancellationToken.None));
        Assert.Null(await store.ObserveRawPostAsync(settings,
            binding with { CredentialGeneration = 10 }, now.AddMilliseconds(1),
            CancellationToken.None));
        Assert.Null(await store.ObserveRawPostAsync(settings,
            binding with { IngressIdempotencyKey = Guid.NewGuid() }, now.AddMilliseconds(1),
            CancellationToken.None));
        Assert.Null(await store.ObserveRawPostAsync(settings,
            binding with { IngressMetadataSha256 = new string('c', 64) }, now.AddMilliseconds(1),
            CancellationToken.None));
        Assert.Null(await store.ObserveRawPostAsync(settings,
            binding with { PlaintextSha256 = new string('d', 64) }, now.AddMilliseconds(1),
            CancellationToken.None));
        var observed = await store.ObserveRawPostAsync(settings, binding, now.AddMilliseconds(2),
            CancellationToken.None);
        Assert.Equal(run!.QualificationRunId, observed?.QualificationRunId);
        Assert.True(observed?.IsActive);
        Assert.True(await store.ConsumeAuthenticatedAsync(run.QualificationRunId, binding,
            now.AddMilliseconds(3), CancellationToken.None));
        Assert.False(await store.ConsumeAuthenticatedAsync(run.QualificationRunId, binding,
            now.AddMilliseconds(4), CancellationToken.None));

        Assert.Equal(run.QualificationRunId, await ScalarAsync<Guid?>(db,
            "SELECT tagekyc.site_qualification_broker_mark_held($1)",
            run.QualificationRunId));
        var wrongAccess = access with { ApiKeyId = Guid.NewGuid() };
        Assert.False(await store.ReleaseBrokerAsync(run.QualificationRunId,
            now.AddMilliseconds(6), wrongAccess, CancellationToken.None));
        Assert.True(await store.ReleaseBrokerAsync(run.QualificationRunId,
            now.AddMilliseconds(6), access, CancellationToken.None));
        Assert.True(await ScalarAsync<bool>(db,
            "SELECT tagekyc.site_qualification_broker_is_released($1)", run.QualificationRunId));
        Assert.True(await ScalarAsync<bool>(db,
            "SELECT tagekyc.site_qualification_broker_mark_committed($1)",
            run.QualificationRunId));
        Assert.False(await store.AcknowledgeBrokerCommitAsync(run.QualificationRunId,
            now.AddMilliseconds(9), wrongAccess, CancellationToken.None));
        Assert.True(await store.AcknowledgeBrokerCommitAsync(run.QualificationRunId,
            now.AddMilliseconds(9), access, CancellationToken.None));
        Assert.True(await ScalarAsync<bool>(db,
            "SELECT tagekyc.site_qualification_broker_is_commit_acknowledged($1)",
            run.QualificationRunId));

        Assert.False(await store.RecordAgentObservationAsync(run.QualificationRunId,
            new(1, 19, 0, true, false, false, true), now.AddMilliseconds(11), wrongAccess,
            CancellationToken.None));
        Assert.True(await store.RecordAgentObservationAsync(run.QualificationRunId,
            new(1, 19, 0, true, false, false, true), now.AddMilliseconds(11), access,
            CancellationToken.None));
        Assert.True(await store.RecordServerBodyReadsAsync(run.QualificationRunId, 0,
            now.AddMilliseconds(12), CancellationToken.None));
        var report = await store.ReadAsync(run.QualificationRunId, now.AddMilliseconds(13),
            access, CancellationToken.None);
        Assert.Null(await store.ReadAsync(run.QualificationRunId, now.AddMilliseconds(13),
            wrongAccess, CancellationToken.None));
        Assert.NotNull(report);
        Assert.Equal("BrokerCommitted", report!.State);
        Assert.Equal(1, report.RawPostCount);
        Assert.Equal(0, report.ServerApplicationBodyReadsWhileBrokerHeld);
        Assert.Equal(1, report.ClientTransportEntryCount);
        Assert.Equal(19, report.ContentBytesCopied);
        Assert.Equal(0, report.AgentBodyBytesSentWhileBrokerHeld);
        Assert.True(report.ObservedContinue);
        Assert.False(report.ApplicationPrebufferObserved);
        Assert.True(report.FinalResponseObserved);
        Assert.True(report.AgentObservationCompleted);
        Assert.True(report.ServerObservationCompleted);
        Assert.True(report.EvidenceComplete);
        Assert.False(report.HiddenRetryObserved);
        Assert.True(report.KestrelContinueRelayedAfterCommit);
        Assert.False(await store.RecordAgentObservationAsync(run.QualificationRunId,
            new(1, 19, 1, true, true, true, true), now.AddMilliseconds(14), access,
            CancellationToken.None));
        var hostileOrdering = await store.ReadAsync(run.QualificationRunId,
            now.AddMilliseconds(15), access, CancellationToken.None);
        Assert.True(hostileOrdering!.KestrelContinueRelayedAfterCommit);
        Assert.False(hostileOrdering.ApplicationPrebufferObserved);
        Assert.Equal(0, hostileOrdering.AgentBodyBytesSentWhileBrokerHeld);

        var retryBinding = binding with { IngressIdempotencyKey = Guid.NewGuid() };
        var retryRun = await store.RegisterAsync(new(Guid.NewGuid(), settings.SiteId, settings.EndpointOrigin,
            settings.DeploymentRevision, retryBinding,
            SiteRawIngressQualificationRunMode.LostFinalNoRetry,
            now.AddMinutes(1), owner), now.AddMilliseconds(16), CancellationToken.None);
        Assert.NotNull(retryRun);
        Assert.True((await store.ObserveRawPostAsync(settings, retryBinding,
            now.AddMilliseconds(17), CancellationToken.None))?.IsActive);
        Assert.True((await store.ObserveRawPostAsync(settings, retryBinding,
            now.AddMilliseconds(18), CancellationToken.None))?.IsActive);
        Assert.True(await store.ConsumeAuthenticatedAsync(retryRun!.QualificationRunId,
            retryBinding, now.AddMilliseconds(19), CancellationToken.None));
        Assert.True(await store.RecordAgentObservationAsync(retryRun.QualificationRunId,
            new(1, 19, 0, true, false, false, false), now.AddMilliseconds(20), access,
            CancellationToken.None));
        var retryReport = await store.ReadAsync(retryRun.QualificationRunId,
            now.AddMilliseconds(21), access, CancellationToken.None);
        Assert.Equal(2, retryReport!.RawPostCount);
        Assert.Equal(1, retryReport.ClientTransportEntryCount);
        Assert.True(retryReport.HiddenRetryObserved);

        var metadata = await ReadSecurityMetadataAsync(db);
        Assert.Equal(13, metadata.FunctionCount);
        Assert.Equal(13, metadata.SecurityDefinerCount);
        Assert.Equal(13, metadata.RestrictedSearchPathCount);
        Assert.Equal(0, metadata.PublicExecuteCount);
        Assert.Equal(0, metadata.RuntimeTablePrivilegeCount);

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(Previous);
        Assert.False(await TableExistsAsync(db));
        await migrator.MigrateAsync(Current);
        Assert.True(await TableExistsAsync(db));
        Assert.Equal(metadata, await ReadSecurityMetadataAsync(db));
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task<T> ScalarAsync<T>(TagEkycDbContext db, string sql,
        params object[] values)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)db.Database.GetDbConnection());
        foreach (var value in values) command.Parameters.Add(new NpgsqlParameter { Value = value });
        var result = await command.ExecuteScalarAsync();
        return result is T typed ? typed : throw new InvalidOperationException("Unexpected scalar result.");
    }

    private static async Task<bool> TableExistsAsync(TagEkycDbContext db) =>
        await ScalarAsync<bool>(db,
            "SELECT pg_catalog.to_regclass('tagekyc.site_raw_ingress_qualification_runs') IS NOT NULL");

    private static async Task<SecurityMetadata> ReadSecurityMetadataAsync(TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*)::integer,
              count(*) FILTER (WHERE p.prosecdef)::integer,
              count(*) FILTER (WHERE p.proconfig=ARRAY['search_path=pg_catalog']::text[])::integer,
              count(*) FILTER (WHERE pg_catalog.has_function_privilege('public',p.oid,'EXECUTE'))::integer,
              (SELECT count(*)::integer FROM (VALUES ('tagekyc_runtime'),('tagekyc_raw_export_claim_broker')) r(role_name)
                WHERE pg_catalog.has_table_privilege(r.role_name,
                  'tagekyc.site_raw_ingress_qualification_runs','SELECT,INSERT,UPDATE,DELETE'))
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname LIKE 'site_qualification_%'
            """, (NpgsqlConnection)db.Database.GetDbConnection());
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
            reader.GetInt32(3), reader.GetInt32(4));
    }

    private sealed record SecurityMetadata(int FunctionCount, int SecurityDefinerCount,
        int RestrictedSearchPathCount, int PublicExecuteCount, int RuntimeTablePrivilegeCount);
}
