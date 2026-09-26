using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class RawExportAssemblyDurableWorkSourceTests(PostgresPersistenceFixture postgres)
    : IAsyncLifetime
{
    private const string PreviousMigration = "20260913120000_Tip88C1C6BA3RetainedIngressComposition";
    private const string CurrentMigration = "20260926120000_RawExportAssemblyPostSealRecovery";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Durable_source_owns_candidate_concurrency_recovery_result_and_acl_contracts()
    {
        await using (var migrationDb = postgres.CreateDbContext())
        {
            var migrator = migrationDb.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            await migrator.MigrateAsync(CurrentMigration);
        }

        await AssertFunctionAclAsync(postgres.ConnectionString);

        await using var setup = postgres.CreateDbContext();
        var permit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPacketPermitAsync(
            setup,
            [RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage]);
        var job = await Tip88B4RawExportJobFoundationTests.CreateJobRepository(setup).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            permit.PermitId,
            $"assembly-work-source-{Guid.NewGuid():N}"));

        await using var firstDb = postgres.CreateDbContext();
        var first = Source(firstDb, Guid.NewGuid());
        var firstRequest = await first.TryAcquireAsync();
        Assert.NotNull(firstRequest);
        Assert.Equal(job.JobId, firstRequest.JobId);
        Assert.Equal(Tip88B34AuthorizationEngineTests.Actor.PrincipalId, firstRequest.ActorPrincipalId);
        Assert.True(firstRequest.ExpectedFence >= 1);

        await using var contenderDb = postgres.CreateDbContext();
        var contender = Source(contenderDb, Guid.NewGuid());
        Assert.Null(await contender.TryAcquireAsync());

        await ExpireLeaseAsync(job.JobId);
        var reclaimed = await contender.TryAcquireAsync();
        Assert.NotNull(reclaimed);
        Assert.Equal(job.JobId, reclaimed.JobId);
        Assert.NotEqual(firstRequest.AttemptId, reclaimed.AttemptId);

        await contender.RecordAsync(reclaimed, Result(RawExportAssemblyExecutionOutcome.ProviderUnavailable));
        await using var retryReadDb = postgres.CreateDbContext();
        var afterRetryable = await Tip88B4RawExportJobFoundationTests.CreateJobRepository(retryReadDb)
            .ReadAsync(new(Tip88B34AuthorizationEngineTests.Actor, job.JobId));
        Assert.Equal(
            RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE.ToString(),
            afterRetryable.Job!.LatestTransition.LatestFailureCode);

        var terminalRequest = await contender.TryAcquireAsync();
        Assert.NotNull(terminalRequest);
        await contender.RecordAsync(terminalRequest, Result(RawExportAssemblyExecutionOutcome.AuthorityInvalid));
        await using var terminalReadDb = postgres.CreateDbContext();
        var afterTerminal = await Tip88B4RawExportJobFoundationTests.CreateJobRepository(terminalReadDb)
            .ReadAsync(new(Tip88B34AuthorizationEngineTests.Actor, job.JobId));
        Assert.Equal(RawExportJobState.TerminalFailed, afterTerminal.Job!.Head.CurrentState);
        Assert.Equal(
            RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED.ToString(),
            afterTerminal.Job.LatestTransition.LatestFailureCode);

        var noOpPermit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPacketPermitAsync(
            setup,
            [RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage]);
        var noOpJob = await Tip88B4RawExportJobFoundationTests.CreateJobRepository(setup).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            noOpPermit.PermitId,
            $"assembly-work-source-noop-{Guid.NewGuid():N}"));
        var noOpRequest = await contender.TryAcquireAsync();
        Assert.NotNull(noOpRequest);
        Assert.Equal(noOpJob.JobId, noOpRequest.JobId);
        await contender.RecordAsync(noOpRequest, Result(RawExportAssemblyExecutionOutcome.Sealed));
        await using var noOpReadDb = postgres.CreateDbContext();
        var afterNoOp = await Tip88B4RawExportJobFoundationTests.CreateJobRepository(noOpReadDb)
            .ReadAsync(new(Tip88B34AuthorizationEngineTests.Actor, noOpJob.JobId));
        Assert.Equal(RawExportJobState.Assembling, afterNoOp.Job!.Head.CurrentState);

        var expiredPermit = await Tip88B4RawExportJobFoundationTests.CreateAuthorizedPacketPermitAsync(
            setup,
            [RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage]);
        var expiredJob = await Tip88B4RawExportJobFoundationTests.CreateJobRepository(setup).BindAsync(new(
            Tip88B34AuthorizationEngineTests.Actor,
            expiredPermit.PermitId,
            $"assembly-work-source-expired-{Guid.NewGuid():N}"));
        await ExpireJobAsync(expiredJob.JobId);
        Assert.Null(await NextCandidateIdAsync());
        Assert.Null(await contender.TryAcquireAsync());

        await AssertMigrationRoundTripAsync();
    }

    private DurableRawExportAssemblyWorkSource Source(TagEkycDbContext db, Guid workerId) => new(
        new TestConnectionFactory(postgres.ConnectionString),
        Tip88B4RawExportJobFoundationTests.CreateJobRepository(db),
        new RawExportAssemblyWorkerIdentity(workerId),
        new RawExportAssemblyRepository(new TestConnectionFactory(postgres.ConnectionString)));

    private static RawExportAssemblyExecutionResult Result(RawExportAssemblyExecutionOutcome outcome) =>
        new(outcome, null, null, null, null);

    private async Task ExpireLeaseAsync(Guid jobId) => await MutateWithTriggersDisabledAsync(
        "UPDATE tagekyc.raw_export_job_operational_heads SET \"LeaseExpiresAt\"=clock_timestamp()-interval '1 minute' WHERE \"JobId\"=@job;",
        jobId);

    private async Task ExpireJobAsync(Guid jobId) => await MutateWithTriggersDisabledAsync(
        "UPDATE tagekyc.raw_export_job_identities SET \"CreatedAt\"=statement_timestamp()-interval '2 minutes', \"PermitExpiresAt\"=statement_timestamp()-interval '1 minute', \"JobExpiresAt\"=statement_timestamp()-interval '1 minute' WHERE \"JobId\"=@job;",
        jobId);

    private async Task MutateWithTriggersDisabledAsync(string sql, Guid jobId)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var replica = new NpgsqlCommand("SET LOCAL session_replication_role=replica;", connection, transaction))
            await replica.ExecuteNonQueryAsync();
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("job", jobId);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }
        await transaction.CommitAsync();
    }

    private async Task<Guid?> NextCandidateIdAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT \"JobId\" FROM tagekyc.raw_export_next_assembly_candidate();",
            connection);
        return await command.ExecuteScalarAsync() is Guid jobId ? jobId : null;
    }

    private static async Task AssertFunctionAclAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.rolname,p.prosecdef,p.proconfig,
                   pg_catalog.has_function_privilege('tagekyc_raw_export_assembly_resolver','tagekyc.raw_export_next_assembly_candidate()','EXECUTE'),
                   pg_catalog.has_function_privilege('tagekyc_runtime','tagekyc.raw_export_next_assembly_candidate()','EXECUTE'),
                   pg_catalog.has_function_privilege('tagekyc_raw_export_assembly_sealer','tagekyc.raw_export_next_assembly_candidate()','EXECUTE'),
                   pg_catalog.has_function_privilege('public','tagekyc.raw_export_next_assembly_candidate()','EXECUTE')
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            JOIN pg_catalog.pg_roles r ON r.oid=p.proowner
            WHERE n.nspname='tagekyc' AND p.proname='raw_export_next_assembly_candidate';
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("tagekyc_raw_export_deployer", reader.GetString(0));
        Assert.True(reader.GetBoolean(1));
        Assert.Contains("search_path=pg_catalog", reader.GetFieldValue<string[]>(2));
        Assert.True(reader.GetBoolean(3));
        Assert.False(reader.GetBoolean(4));
        Assert.False(reader.GetBoolean(5));
        Assert.False(reader.GetBoolean(6));
        Assert.False(await reader.ReadAsync());
    }

    private async Task AssertMigrationRoundTripAsync()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("assembly_work_source");
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        Assert.False(await FunctionExistsAsync(db));
        await migrator.MigrateAsync(CurrentMigration);
        Assert.True(await FunctionExistsAsync(db));
        await migrator.MigrateAsync(PreviousMigration);
        Assert.False(await FunctionExistsAsync(db));
        await migrator.MigrateAsync(CurrentMigration);
        Assert.True(await FunctionExistsAsync(db));
    }

    private static async Task<bool> FunctionExistsAsync(TagEkycDbContext db) =>
        await db.Database.SqlQueryRaw<bool>(
            "SELECT pg_catalog.to_regprocedure('tagekyc.raw_export_next_assembly_candidate()') IS NOT NULL AS \"Value\"")
            .SingleAsync();

    private sealed class TestConnectionFactory(string connectionString) : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RawExportAssemblyDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            Assert.Contains(capability, new[]
            {
                RawExportAssemblyDatabaseCapability.Resolver,
                RawExportAssemblyDatabaseCapability.Sealer
            });
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
