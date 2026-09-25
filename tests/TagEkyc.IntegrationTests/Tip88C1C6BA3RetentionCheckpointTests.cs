using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Security.Cryptography;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;
using static TagEkyc.IntegrationTests.Tip88C1C6BA3ConsentRetentionTests;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3RetentionCheckpointTests(PostgresPersistenceFixture postgres)
{
    private static readonly Guid ExportAdmin = Guid.Parse("a3000000-0000-4000-8000-000000000003");
    private static readonly Guid ExportApiKey = Guid.Parse("a3000000-0000-4000-8000-000000000004");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_ExportRequiresIndependentAuthorityForRetainedSource(bool withdrawBeforeFreeze)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_export_independent");
        await Prepare(isolated);
        var minio = await DurableObjectMinioFixture.StartAsync();
        await using var cleanup = minio;
        var scope = await Seed(isolated);
        await MakeRetainedSourceAvailable(isolated, scope, minio);

        var actor = new AuthenticatedRawExportActor(Principal, Client, ExportApiKey);
        await using var db = isolated.CreateDbContext();
        var denied = await Tip88B34AuthorizationEngineTests.CreateRepository(db)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                scope.Session, scope.Policy, $"a3-export-early-{Guid.NewGuid():N}",
                [RawExportRawClass.LiveSelfieImage], actor));
        Assert.Equal(RawExportAuthorizationOutcome.Denied, denied.Decision.Outcome);
        Assert.Equal(RawExportAuthorizationPrimaryCause.SESSION_NOT_COMPLETED, denied.Decision.PrimaryCause);
        Assert.Null(denied.Permit);

        await new EfVerificationSessionRepository(db).SetStateAsync(scope.Session, VerificationSessionState.Completed);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT tagekyc.raw_export_bootstrap_global_authority({ExportAdmin},'GrantAdmin','synthetic-a3-export-root')");
        var control = new EfRawExportControlPlaneRepository(db);
        await control.GrantExportPolicyAsync(new(
            ExportAdmin, Principal, scope.Policy, 1, 0, null, "synthetic-a3-export-grant"));
        await new EfRawExportSubjectConsentRepository(db).RecordSubjectConsentGrantedAsync(new(
            Principal, scope.Session, scope.Policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null,
            DateTimeOffset.UtcNow.AddMinutes(5)));
        var authorized = await Tip88B34AuthorizationEngineTests.CreateRepository(db)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                scope.Session, scope.Policy, $"a3-export-after-completed-{Guid.NewGuid():N}",
                [RawExportRawClass.LiveSelfieImage], actor));
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, authorized.Decision.Outcome);
        Assert.NotNull(authorized.Permit);

        await using (var selectTx = await db.Database.BeginTransactionAsync())
        {
            await Actor(db, Principal);
            Assert.NotEqual(Guid.Empty, await db.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_select_session_capture_acceptance(
                    {scope.Session},'LiveSelfieImage',{scope.Acceptance}) AS "Value"
                """).SingleAsync());
            await selectTx.CommitAsync();
        }
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var bound = await jobs.BindAsync(new(actor, authorized.Permit!.PermitId, $"a3-c1-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(actor, bound.JobId, 0, 0, Guid.NewGuid()));
        if (withdrawBeforeFreeze)
        {
            await Grant(db, "SubjectConsentWithdrawer");
            await using var withdrawalTx = await db.Database.BeginTransactionAsync();
            await WithdrawReference(db, scope);
            await withdrawalTx.CommitAsync();
            Assert.Equal(1, await db.RawExportAuthorizationPermits.CountAsync(x => x.PermitId == authorized.Permit.PermitId));
        }
        await using (var freezeTx = await db.Database.BeginTransactionAsync())
        {
            await Actor(db, Principal);
            var frozen = await db.Database.SqlQuery<C1FreezeResult>($"""
                SELECT "Outcome","BindingCount" FROM tagekyc.raw_export_freeze_job_source_bindings(
                    {bound.JobId},{acquired.AttemptId!.Value},{acquired.Revision!.Value},
                    {acquired.FencingToken!.Value},{Principal})
                """).SingleAsync();
            Assert.Equal(withdrawBeforeFreeze ? "AuthorityInvalid" : "Frozen", frozen.Outcome);
            Assert.Equal(withdrawBeforeFreeze ? 0 : 1, frozen.BindingCount);
            await freezeTx.CommitAsync();
        }
        Assert.Equal(withdrawBeforeFreeze ? 0 : 1,
            await db.RawExportJobSourceBindings.CountAsync(x => x.JobId == bound.JobId));
        if (!withdrawBeforeFreeze)
        {
            var assembly = await ExecuteRetainedAssemblyAsync(isolated, minio, new(
                bound.JobId, acquired.AttemptId!.Value, acquired.Revision!.Value,
                acquired.FencingToken!.Value, Principal));
            Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, assembly.Result.Outcome);
            Assert.Equal(1, assembly.PrepareCount);
            Assert.True(assembly.Finalized);
            var package = await CreateFinalizedRecipientPackageAsync(isolated, assembly.Result);
            var delivery = new RecipientPackageDeliveryRepository(new A3DeliveryConnectionFactory(
                db.Database.GetConnectionString()!));
            async Task<(Guid Id, RecipientPackageDeliveryMutation Created)> CreateDelivery(string key)
            {
                var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
                var id = RecipientPackageDeliveryCodec.DeliveryId(package.RecipientId, digest);
                var created = await delivery.CreateAsync(package.RecipientId, package.PackageId, id, digest,
                    Guid.NewGuid(), package.PrincipalId, RandomNumberGenerator.GetBytes(32), CancellationToken.None);
                return (id, created);
            }
            var firstDelivery = await CreateDelivery("a3-retained-c3-current");
            Assert.Equal("Created", firstDelivery.Created.Outcome);
            var started = await delivery.BeginAsync(package.RecipientId, firstDelivery.Id,
                Guid.NewGuid(), package.PrincipalId, RandomNumberGenerator.GetBytes(32), CancellationToken.None);
            Assert.Equal("Started", started.Outcome);

            await Grant(db, "SubjectConsentWithdrawer");
            await using (var withdrawalTx = await db.Database.BeginTransactionAsync())
            {
                await WithdrawReference(db, scope);
                await withdrawalTx.CommitAsync();
            }
            var secondDelivery = await CreateDelivery("a3-retained-c3-withdrawn");
            Assert.Equal("Created", secondDelivery.Created.Outcome);
            var reader = new CountingDeliveryReader();
            using var spool = new RecipientPackageDeliverySpoolPool();
            var options = new RecipientPackageDeliveryOptions(
                RecipientPackageDeliveryTopology.S3CompatibleDurable,
                new("c2-integration-minio-v1", new Uri("http://127.0.0.1:9000/"),
                    "tagekyc-c2-integration", true, "us-east-1",
                    new("writer", "secret"), new("reader", "secret"),
                    new("lifecycle", "secret"), new("posture", "secret"), true),
                new("delivery", "secret"), db.Database.GetConnectionString()!, true);
            var coordinator = new RecipientPackageDeliveryCoordinator(options, delivery, reader, spool);
            var recipient = new AuthenticatedClientContext(Guid.NewGuid(), package.RecipientId,
                "a3-retained-c3-withdrawn", AuthenticatedCallerCategory.BusinessConsumer,
                new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: package.PrincipalId);
            var deniedStart = await coordinator.PrepareContentAsync(recipient, secondDelivery.Id,
                RandomNumberGenerator.GetBytes(32), CancellationToken.None);
            Assert.False(deniedStart.IsSuccess);
            Assert.Equal(RecipientPackageDeliveryErrorCodes.Ineligible, deniedStart.Error?.Code);
            Assert.Equal(0, reader.OpenCount);
            Assert.Equal(1, await db.RawExportRecipientPackageDeliveryEvents.CountAsync(
                x => x.DeliveryId == secondDelivery.Id));
        }
    }

    [Fact]
    public async Task A3_RetainedSeal_RechecksAuthorityAfterProviderPreparation()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_retained_seal_recheck");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var scope = await Seed(isolated);
        await MakeRetainedSourceAvailable(isolated, scope, minio);
        var actor = new AuthenticatedRawExportActor(Principal, Client, ExportApiKey);
        await using var db = isolated.CreateDbContext();
        await new EfVerificationSessionRepository(db).SetStateAsync(scope.Session, VerificationSessionState.Completed);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT tagekyc.raw_export_bootstrap_global_authority({ExportAdmin},'GrantAdmin','synthetic-a3-seal-root')");
        await new EfRawExportControlPlaneRepository(db).GrantExportPolicyAsync(new(
            ExportAdmin, Principal, scope.Policy, 1, 0, null, "synthetic-a3-seal-grant"));
        await new EfRawExportSubjectConsentRepository(db).RecordSubjectConsentGrantedAsync(new(
            Principal, scope.Session, scope.Policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null,
            DateTimeOffset.UtcNow.AddMinutes(5)));
        var authorized = await Tip88B34AuthorizationEngineTests.CreateRepository(db)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                scope.Session, scope.Policy, $"a3-seal-{Guid.NewGuid():N}",
                [RawExportRawClass.LiveSelfieImage], actor));
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, authorized.Decision.Outcome);
        await using (var selectTx = await db.Database.BeginTransactionAsync())
        {
            await Actor(db, Principal);
            Assert.NotEqual(Guid.Empty, await db.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_select_session_capture_acceptance(
                    {scope.Session},'LiveSelfieImage',{scope.Acceptance}) AS "Value"
                """).SingleAsync());
            await selectTx.CommitAsync();
        }
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var bound = await jobs.BindAsync(new(actor, authorized.Permit!.PermitId, $"a3-seal-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(actor, bound.JobId, 0, 0, Guid.NewGuid()));

        async Task WithdrawAfterPrepare()
        {
            await using var withdrawal = isolated.CreateDbContext();
            await Grant(withdrawal, "SubjectConsentWithdrawer");
            await using var tx = await withdrawal.Database.BeginTransactionAsync();
            await WithdrawReference(withdrawal, scope);
            await tx.CommitAsync();
        }

        var result = await ExecuteRetainedAssemblyAsync(isolated, minio, new(
            bound.JobId, acquired.AttemptId!.Value, acquired.Revision!.Value,
            acquired.FencingToken!.Value, Principal), WithdrawAfterPrepare);
        Assert.Equal(RawExportAssemblyExecutionOutcome.AuthorityInvalid, result.Result.Outcome);
        Assert.Equal(1, result.PrepareCount);
        Assert.False(result.Finalized);
        Assert.Equal(1, await db.RawExportAuthorizationPermits.CountAsync(x => x.PermitId == authorized.Permit.PermitId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_C1Freeze_ActualB2WithdrawalSessionOrder(bool withdrawalFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_c1_b2_order");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var fixture = await PrepareC1RaceJob(isolated, minio);
        await using var winner = isolated.CreateDbContext();
        await Grant(winner, "SubjectConsentWithdrawer");
        await using var observer = isolated.CreateDbContext();
        await using var hold = await winner.Database.BeginTransactionAsync();
        var winnerPid = await winner.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        if (withdrawalFirst)
            Assert.Equal(2, await ActualB2Withdraw(winner, fixture.Scope));
        else
            Assert.Equal("Frozen", (await Freeze(winner, fixture)).Outcome);

        var loserPid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loser = RunLoser();
        try
        {
            var backend = await loserPid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waitingAtSessionPrefix = false;
            for (var i = 0; i < 100 && !waitingAtSessionPrefix; i++)
            {
                waitingAtSessionPrefix = await observer.Database.SqlQuery<bool>($"""
                    SELECT {winnerPid}=ANY(pg_catalog.pg_blocking_pids({backend}))
                     AND EXISTS(SELECT 1 FROM pg_catalog.pg_locks l WHERE l.pid={backend}
                       AND l.locktype='transactionid' AND l.mode='ShareLock' AND NOT l.granted)
                     AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l CROSS JOIN LATERAL (
                       SELECT pg_catalog.hashtextextended('tip88c1:a3:consent-reference:'||CAST({Client} AS text)||':synthetic-existing-consent',0) k
                     ) reference_key WHERE l.pid={backend} AND l.locktype='advisory' AND l.granted
                       AND l.classid::bigint=((reference_key.k>>32)&4294967295)
                       AND l.objid::bigint=(reference_key.k&4294967295) AND l.objsubid=1) AS "Value"
                    """).SingleAsync();
                if (!waitingAtSessionPrefix) await Task.Delay(25);
            }
            Assert.True(waitingAtSessionPrefix,
                "The loser must wait at the actual verification_sessions row before the E01 reference key.");
            await hold.CommitAsync();
            var result = await loser.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(withdrawalFirst ? "AuthorityInvalid" : "Withdrawn", result);
            Assert.Equal(2L, await observer.Database.SqlQuery<long>($"""
                SELECT "CurrentRevision" AS "Value" FROM tagekyc.raw_source_consent_references
                 WHERE "ConsentReferenceId"={fixture.Scope.Reference}
                """).SingleAsync());
            Assert.Equal(withdrawalFirst ? 0 : 1,
                await observer.RawExportJobSourceBindings.CountAsync(x => x.JobId == fixture.JobId));
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            await loser.WaitAsync(TimeSpan.FromSeconds(15));
        }

        async Task<string> RunLoser()
        {
            await using var db = isolated.CreateDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            loserPid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
            string outcome;
            if (withdrawalFirst)
                outcome = (await Freeze(db, fixture)).Outcome;
            else
            {
                Assert.Equal(2, await ActualB2Withdraw(db, fixture.Scope));
                outcome = "Withdrawn";
            }
            await tx.CommitAsync();
            return outcome;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_C3Begin_ActualB2WithdrawalSessionOrder(bool withdrawalFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_c3_b2_order");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var c1 = await PrepareC1RaceJob(isolated, minio);
        var assembly = await ExecuteRetainedAssemblyAsync(isolated, minio, new(
            c1.JobId, c1.AttemptId, c1.Revision, c1.Fence, Principal));
        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, assembly.Result.Outcome);
        var package = await CreateFinalizedRecipientPackageAsync(isolated, assembly.Result);
        var fixture = await CreateC3RaceDelivery(isolated, c1.Scope, package);

        await using var winner = isolated.CreateDbContext();
        await Grant(winner, "SubjectConsentWithdrawer");
        await using var observer = isolated.CreateDbContext();
        await using var hold = await winner.Database.BeginTransactionAsync();
        var winnerPid = await winner.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        if (withdrawalFirst)
            Assert.Equal(2, await ActualB2Withdraw(winner, fixture.Scope));
        else
            Assert.Equal("Started", await BeginC3(winner, fixture));

        var loserPid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loser = RunLoser();
        try
        {
            var backend = await loserPid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waitingAtSessionPrefix = false;
            for (var i = 0; i < 100 && !waitingAtSessionPrefix; i++)
            {
                waitingAtSessionPrefix = await observer.Database.SqlQuery<bool>($"""
                    SELECT {winnerPid}=ANY(pg_catalog.pg_blocking_pids({backend}))
                     AND EXISTS(SELECT 1 FROM pg_catalog.pg_locks l WHERE l.pid={backend}
                       AND l.locktype='transactionid' AND l.mode='ShareLock' AND NOT l.granted)
                     AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l CROSS JOIN LATERAL (
                       SELECT pg_catalog.hashtextextended('tip88c1:a3:consent-reference:'||CAST({Client} AS text)||':synthetic-existing-consent',0) k
                     ) reference_key WHERE l.pid={backend} AND l.locktype='advisory' AND l.granted
                       AND l.classid::bigint=((reference_key.k>>32)&4294967295)
                       AND l.objid::bigint=(reference_key.k&4294967295) AND l.objsubid=1) AS "Value"
                    """).SingleAsync();
                if (!waitingAtSessionPrefix) await Task.Delay(25);
            }
            Assert.True(waitingAtSessionPrefix,
                "The loser must wait at the actual verification_sessions row before the E01 reference key.");
            await hold.CommitAsync();
            Assert.Equal(withdrawalFirst ? "Ineligible" : "Withdrawn",
                await loser.WaitAsync(TimeSpan.FromSeconds(15)));
            Assert.Equal(2L, await observer.Database.SqlQuery<long>($"""
                SELECT "CurrentRevision" AS "Value" FROM tagekyc.raw_source_consent_references
                 WHERE "ConsentReferenceId"={fixture.Scope.Reference}
                """).SingleAsync());
            Assert.Equal(1, await observer.RawExportRecipientPackageDeliveryEvents.CountAsync(
                x => x.DeliveryId == fixture.DeliveryId && x.EventType == "Authorized"));
            Assert.Equal(withdrawalFirst ? 0 : 1,
                await observer.RawExportRecipientPackageDeliveryEvents.CountAsync(
                    x => x.DeliveryId == fixture.DeliveryId && x.EventType == "StreamingStarted"));
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            await loser.WaitAsync(TimeSpan.FromSeconds(15));
        }

        async Task<string> RunLoser()
        {
            await using var db = isolated.CreateDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            loserPid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
            string outcome;
            if (withdrawalFirst)
                outcome = await BeginC3(db, fixture);
            else
            {
                Assert.Equal(2, await ActualB2Withdraw(db, fixture.Scope));
                outcome = "Withdrawn";
            }
            await tx.CommitAsync();
            return outcome;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_C1Seal_ActualB2WithdrawalSessionOrder(bool withdrawalFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_c1_seal_b2_order");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var fixture = await PrepareC1RaceJob(isolated, minio);
        await using var observer = isolated.CreateDbContext();
        await using var withdrawal = isolated.CreateDbContext();
        await Grant(withdrawal, "SubjectConsentWithdrawer");
        await using var withdrawalTx = await withdrawal.Database.BeginTransactionAsync();
        var withdrawalPid = await withdrawal.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        await using var blocker = isolated.CreateDbContext();
        await using var blockerTx = await blocker.Database.BeginTransactionAsync();
        var blockerPid = await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        var providerReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseProvider = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await withdrawal.Database.ExecuteSqlRawAsync("SET LOCAL statement_timeout='10s'");

        async Task AfterPrepare()
        {
            if (withdrawalFirst)
            {
                providerReached.TrySetResult();
                await releaseProvider.Task;
                return;
            }
            await blocker.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT 1 FROM tagekyc.raw_export_job_operational_heads
                 WHERE "JobId"={fixture.JobId} FOR UPDATE
                """);
            providerReached.TrySetResult();
        }

        var assembly = ExecuteRetainedAssemblyAsync(isolated, minio, new(
            fixture.JobId, fixture.AttemptId, fixture.Revision, fixture.Fence, Principal), AfterPrepare, stop.Token);
        await providerReached.Task.WaitAsync(TimeSpan.FromSeconds(20));
        Task<int>? withdrawing = null;
        try
        {
            if (withdrawalFirst)
            {
                Assert.Equal(2, await ActualB2Withdraw(withdrawal, fixture.Scope));
                releaseProvider.TrySetResult();
                var sealPid = await FindWaitingSealPid(blockerPid: withdrawalPid);
                Assert.True(await WaitsAtSessionBeforeReference(sealPid),
                    "A seal must wait at the actual session row before requesting the E01 reference lock.");
                await withdrawalTx.CommitAsync();
                var result = await assembly.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.Equal(RawExportAssemblyExecutionOutcome.AuthorityInvalid, result.Result.Outcome);
                Assert.Equal(1, result.PrepareCount);
                Assert.False(result.Finalized);
            }
            else
            {
                var sealPid = await FindWaitingSealPid(blockerPid);
                withdrawing = Withdraw();
                var waitingAtSession = false;
                for (var i = 0; i < 100 && !waitingAtSession; i++)
                {
                    waitingAtSession = await observer.Database.SqlQuery<bool>($"""
                        SELECT {sealPid}=ANY(pg_catalog.pg_blocking_pids({withdrawalPid}))
                         AND EXISTS(SELECT 1 FROM pg_catalog.pg_locks l WHERE l.pid={withdrawalPid}
                           AND l.locktype='transactionid' AND l.mode='ShareLock' AND NOT l.granted) AS "Value"
                        """).SingleAsync();
                    if (!waitingAtSession) await Task.Delay(25);
                }
                Assert.True(waitingAtSession,
                    "Actual B2 withdrawal must wait at the session row held by C1 seal.");
                Assert.False(await HoldsReference(withdrawalPid),
                    "B2 withdrawal must not reach the E01 reference before its session row.");
                await blockerTx.CommitAsync();
                var result = await assembly.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, result.Result.Outcome);
                Assert.True(result.Finalized);
                Assert.Equal(2, await withdrawing.WaitAsync(TimeSpan.FromSeconds(20)));
                await withdrawalTx.CommitAsync();
            }
            Assert.Equal(2L, await observer.Database.SqlQuery<long>($"""
                SELECT "CurrentRevision" AS "Value" FROM tagekyc.raw_source_consent_references
                 WHERE "ConsentReferenceId"={fixture.Scope.Reference}
                """).SingleAsync());
        }
        finally
        {
            releaseProvider.TrySetResult();
            if (blockerTx.GetDbTransaction().Connection is not null) await blockerTx.RollbackAsync();
            if (withdrawalTx.GetDbTransaction().Connection is not null) await withdrawalTx.RollbackAsync();
            try { await assembly.WaitAsync(TimeSpan.FromSeconds(20)); } catch { }
            if (withdrawing is not null) try { await withdrawing.WaitAsync(TimeSpan.FromSeconds(20)); } catch { }
        }

        async Task<int> FindWaitingSealPid(int blockerPid)
        {
            for (var i = 0; i < 200; i++)
            {
                var pid = await observer.Database.SqlQuery<int>($"""
                    SELECT COALESCE((SELECT a.pid FROM pg_catalog.pg_stat_activity a
                     WHERE a.datname=current_database() AND a.pid<>pg_backend_pid()
                       AND a.query LIKE '%raw_export_seal_authenticated_assembly%'
                       AND {blockerPid}=ANY(pg_catalog.pg_blocking_pids(a.pid))
                     ORDER BY a.query_start DESC LIMIT 1),0) AS "Value"
                    """).SingleAsync();
                if (pid != 0) return pid;
                await Task.Delay(25);
            }
            throw new Xunit.Sdk.XunitException("C1 seal never reached the expected durable lock boundary.");
        }

        async Task<bool> HoldsReference(int pid) => await observer.Database.SqlQuery<bool>($"""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l CROSS JOIN LATERAL (
              SELECT pg_catalog.hashtextextended('tip88c1:a3:consent-reference:'||CAST({Client} AS text)||':synthetic-existing-consent',0) k
            ) reference_key WHERE l.pid={pid} AND l.locktype='advisory' AND l.granted
              AND l.classid::bigint=((reference_key.k>>32)&4294967295)
              AND l.objid::bigint=(reference_key.k&4294967295) AND l.objsubid=1) AS "Value"
            """).SingleAsync();

        async Task<bool> WaitsAtSessionBeforeReference(int pid) => await observer.Database.SqlQuery<bool>($"""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l WHERE l.pid={pid}
              AND l.locktype='transactionid' AND l.mode='ShareLock' AND NOT l.granted)
             AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l CROSS JOIN LATERAL (
              SELECT pg_catalog.hashtextextended('tip88c1:a3:consent-reference:'||CAST({Client} AS text)||':synthetic-existing-consent',0) k
             ) reference_key WHERE l.pid={pid} AND l.locktype='advisory'
              AND l.classid::bigint=((reference_key.k>>32)&4294967295)
              AND l.objid::bigint=(reference_key.k&4294967295) AND l.objsubid=1) AS "Value"
            """).SingleAsync();

        async Task<int> Withdraw()
        {
            var value = await ActualB2Withdraw(withdrawal, fixture.Scope);
            return value;
        }
    }

    [Fact]
    public async Task A3_C1Seal_MultiSessionReverseInputLocksSessionsInUuidOrder()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_c1_seal_multi_session_order");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var first = await PrepareC1RaceJob(isolated, minio, "synthetic-existing-consent-a");
        var second = await PrepareC1RaceJob(isolated, minio, "synthetic-existing-consent-b", bootstrapRuntime: false);
        await using (var freezeDb = isolated.CreateDbContext())
        {
            await using var freezeTx = await freezeDb.Database.BeginTransactionAsync();
            Assert.Equal("Frozen", (await Freeze(freezeDb, first)).Outcome);
            Assert.Equal("Frozen", (await Freeze(freezeDb, second)).Outcome);
            await freezeTx.CommitAsync();
        }
        var firstIsHigh = string.CompareOrdinal(
            first.Scope.Session.ToString("N"), second.Scope.Session.ToString("N")) > 0;
        var high = firstIsHigh ? first : second;
        var low = firstIsHigh ? second : first;

        // Deliberately make input/ordinal order the reverse of canonical UUID order:
        // ordinal 0 = high session, ordinal 1 = low session. The CP04 prefix must
        // ignore this physical/input order and lock the low UUID first.
        await using (var setup = isolated.CreateDbContext())
        {
            await setup.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE tagekyc.raw_export_job_source_bindings
                   SET "JobId"={high.JobId},"Ordinal"=1,"RawClass"='ChipDg2Portrait'
                 WHERE "JobId"={low.JobId}
                """);
            var inputOrder = await setup.Database.SqlQuery<Guid>($"""
                SELECT "VerificationSessionId" AS "Value"
                  FROM tagekyc.raw_export_job_source_bindings
                 WHERE "JobId"={high.JobId}
                 ORDER BY "Ordinal"
                """).ToListAsync();
            Assert.Equal([high.Scope.Session, low.Scope.Session], inputOrder);
        }

        await using var observer = isolated.CreateDbContext();
        await using var highHolder = isolated.CreateDbContext();
        await using var highTx = await highHolder.Database.BeginTransactionAsync();
        await highHolder.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1 FROM tagekyc.verification_sessions
             WHERE "Id"={high.Scope.Session} FOR UPDATE
            """);
        var highPid = await highHolder.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var sealPid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lowerPid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sealing = RunSeal();
        Task? lowerWaiter = null;
        try
        {
            var actualSealPid = await sealPid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(await WaitUntilBlockedBy(actualSealPid, highPid),
                "Seal must reach the held high UUID only after acquiring the lower UUID row.");

            lowerWaiter = LockLower();
            var actualLowerPid = await lowerPid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(await WaitUntilBlockedBy(actualLowerPid, actualSealPid),
                "A third transaction must wait behind seal on the lower UUID row, proving lower-before-higher acquisition.");
            Assert.False(await observer.Database.SqlQuery<bool>($"""
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks
                 WHERE pid={actualSealPid} AND locktype='advisory' AND granted) AS "Value"
                """).SingleAsync(),
                "Seal must acquire every participating session row before any retained-reference advisory lock.");
        }
        finally
        {
            stop.Cancel();
            if (highTx.GetDbTransaction().Connection is not null) await highTx.RollbackAsync();
            try { await sealing.WaitAsync(TimeSpan.FromSeconds(10)); } catch { }
            if (lowerWaiter is not null) try { await lowerWaiter.WaitAsync(TimeSpan.FromSeconds(10)); } catch { }
        }

        async Task<bool> WaitUntilBlockedBy(int waiter, int blocker)
        {
            for (var i = 0; i < 200; i++)
            {
                if (await observer.Database.SqlQuery<bool>($"""
                    SELECT {blocker}=ANY(pg_catalog.pg_blocking_pids({waiter})) AS "Value"
                    """).SingleAsync()) return true;
                await Task.Delay(25);
            }
            return false;
        }

        async Task RunSeal()
        {
            await using var db = isolated.CreateDbContext();
            await using var tx = await db.Database.BeginTransactionAsync(stop.Token);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_assembly_sealer", stop.Token);
            sealPid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync(stop.Token));
            _ = await db.Database.SqlQuery<string>($"""
                SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_seal_authenticated_assembly(
                 {Guid.NewGuid()},{high.JobId},1,{high.Revision},{high.Fence},{high.AttemptId},
                 {new byte[32]},{new byte[32]},{new byte[32]},'synthetic-key',1,
                 {new byte[32]},1,2,'[]'::jsonb)
                """).SingleAsync(stop.Token);
        }

        async Task LockLower()
        {
            await using var db = isolated.CreateDbContext();
            await using var tx = await db.Database.BeginTransactionAsync(stop.Token);
            lowerPid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync(stop.Token));
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT 1 FROM tagekyc.verification_sessions
                 WHERE "Id"={low.Scope.Session} FOR UPDATE
                """, stop.Token);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task A3_RetainedCompletion_UsesExistingConsentWithoutEarlyExport(bool completed, bool withdrawn)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_retained_r1");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var candidate = await BeginCandidate(isolated, scope);
        await using var observer = isolated.CreateDbContext();
        if (completed)
            await new EfVerificationSessionRepository(observer).SetStateAsync(scope.Session, VerificationSessionState.Completed);
        if (withdrawn)
        {
            await Grant(observer, "SubjectConsentWithdrawer");
            await using var tx = await observer.Database.BeginTransactionAsync();
            await WithdrawReference(observer, scope);
            await tx.CommitAsync();
        }
        await using (var db = isolated.CreateDbContext())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var result = await CompleteCandidate(db, candidate);
            Assert.Equal(withdrawn ? "SOURCE_RETENTION_NOT_AUTHORIZED" : "NewReservation", result.OutcomeCode);
            Assert.Equal(withdrawn, result.SourceArtifactId is null);
            await tx.CommitAsync();
        }
        Assert.Equal(withdrawn ? 0 : 1, await observer.RawExportSourceReservations.CountAsync());
        Assert.Equal(withdrawn ? 0 : 1, await observer.RawExportSourceEncryptionAttempts.CountAsync());
        Assert.Equal(withdrawn ? 0 : 1, await observer.RawExportSourceHeads.CountAsync());
        Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_subject_consent_events").SingleAsync());
        if (!withdrawn)
        {
            Assert.True(await observer.Database.SqlQueryRaw<bool>("""
                SELECT a."OwnershipLeaseExpiresAtUtc"=r."ReservationExpiresAtUtc"
                 AND r."EffectivePlaintextRetentionExpiresAtUtc"<=s."ValidUntilUtc"
                 AND r."EffectivePlaintextRetentionExpiresAtUtc"<=b."ExecutionExpiresAtUtc"
                 AND c."AuthenticatedPrincipalId"=s."CustodyPrincipalId" AS "Value"
                FROM tagekyc.raw_export_source_reservations r
                JOIN tagekyc.raw_export_source_encryption_attempts a USING("SourceArtifactId")
                JOIN tagekyc.raw_export_source_ingress_claims c USING("IngressClaimId")
                JOIN tagekyc.raw_export_authority_snapshots s ON s."AuthoritySnapshotId"=r."AuthoritySnapshotId"
                JOIN tagekyc.capture_execution_bindings b ON b."CaptureExecutionBindingId"=s."RuntimeBindingId"
                """).SingleAsync());
        }
    }

    [Fact]
    public async Task A3_RetainedBegin_SameKeyLostResponseDoesNotRearmReservedAttempt()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_same_key_replay");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var first = await BeginCandidate(isolated,scope);
        Completion committed;
        await using (var db = isolated.CreateDbContext())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            committed=await CompleteCandidate(db,first);
            Assert.Equal("NewReservation",committed.OutcomeCode);
            Assert.NotNull(committed.AttemptId);
            Assert.NotNull(committed.AttemptKeyReservationId);
            await tx.CommitAsync();
        }
        await using var observer = isolated.CreateDbContext();
        var before=await CustodyRows(observer);
        var retry=await BeginCandidate(isolated,scope,first);
        Assert.Equal(first.Envelope,retry.Envelope);
        Assert.Equal(first.Token.Revision+1,retry.Token.Revision);
        Assert.Equal(first.Token.Fence+1,retry.Token.Fence);
        await using (var db = isolated.CreateDbContext())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var replay=await CompleteCandidate(db,retry);
            Assert.Equal("RAW_EXPORT_SOURCE_RESERVATION_BUSY",replay.OutcomeCode);
            Assert.Equal(committed.SourceArtifactId,replay.SourceArtifactId);
            Assert.Null(replay.AttemptId);
            Assert.Null(replay.AttemptKeyReservationId);
            Assert.Null(replay.ExpectedEncryptionAttemptRevision);
            Assert.Null(replay.ExpectedFence);
            await tx.CommitAsync();
        }
        Assert.Equal(before,await CustodyRows(observer));
        Assert.Equal(1,await observer.RawExportSourceReservations.CountAsync());
        Assert.Equal(1,await observer.RawExportSourceEncryptionAttempts.CountAsync());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public async Task A3_RetentionCheckpoint_WithdrawalSerializesWithR1R3R4R5(int checkpoint, bool withdrawalFirst)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_r1_withdrawal_race");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var candidate = await BeginCandidate(isolated, scope);
        await using var minio = checkpoint==1 ? null : await DurableObjectMinioFixture.StartAsync();
        VerifiedRetained? source = checkpoint==1 ? null : await VerifyRetainedCandidate(isolated,scope,candidate,minio!);
        Guid? publication=null;
        await using var winner = isolated.CreateDbContext();
        if (checkpoint>=4)
            Assert.Equal(RawExportR3StageDisposition.Staged,(await new RawExportR3StagingService(winner)
                .StageAsync(new(Principal,source!.AttemptId,source.ObjectId,1,1,1,source.ObjectRevision))).Disposition);
        if (checkpoint>=5)
        {
            var committed=await new RawExportSourceFinalizationService(winner)
                .CommitAsync(new(Principal,source!.AttemptId,2,1,1,source.ObjectRevision));
            Assert.Equal(SourceCommitDisposition.Committed,committed.Disposition);
            publication=committed.SourcePublicationId;
        }
        await Grant(winner, "SubjectConsentWithdrawer");
        await using var observer = isolated.CreateDbContext();
        var before=await CustodyRows(observer);
        await using var hold = await winner.Database.BeginTransactionAsync();
        if (withdrawalFirst) await WithdrawReference(winner, scope);
        else Assert.Equal(SuccessOutcome(), await Checkpoint(winner));
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loser = RunLoser();
        try
        {
            var backend = await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting = false;
            for (var i = 0; i < 100 && !waiting; i++)
            {
                waiting = await observer.Database.SqlQuery<bool>($"""
                    SELECT EXISTS(
                     SELECT 1 FROM pg_locks l CROSS JOIN LATERAL (
                      SELECT hashtextextended('tip88c1:a3:consent-reference:'||CAST({Client} AS text)||':synthetic-existing-consent',0) k
                     ) reference_key WHERE l.pid={backend} AND NOT l.granted AND l.locktype='advisory'
                      AND l.classid::bigint=((reference_key.k>>32)&4294967295)
                      AND l.objid::bigint=(reference_key.k&4294967295) AND l.objsubid=1
                      AND l.mode=CASE WHEN {withdrawalFirst} THEN 'ShareLock' ELSE 'ExclusiveLock' END
                    ) AND cardinality(pg_blocking_pids({backend}))>0 AS "Value"
                    """).SingleAsync();
                if (!waiting) await Task.Delay(50);
            }
            Assert.True(waiting, "The actual opposite operation must wait on this exact consent reference lock.");
            Assert.Equal(before,await CustodyRows(observer));
            Assert.Equal(1L, await observer.Database.SqlQuery<long>($"""
                SELECT "CurrentRevision" AS "Value" FROM tagekyc.raw_source_consent_references WHERE "ConsentReferenceId"={scope.Reference}
                """).SingleAsync());
            await hold.CommitAsync();
            var result = await loser.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(withdrawalFirst ? (checkpoint==1 ? "SOURCE_RETENTION_NOT_AUTHORIZED" : "SourceRetentionNotAuthorized") : "Withdrawn", result);
            if (withdrawalFirst) Assert.Equal(before,await CustodyRows(observer));
            else Assert.NotEqual(before,await CustodyRows(observer));
            Assert.Equal(checkpoint==1 && withdrawalFirst ? 0 : 1, await observer.RawExportSourceReservations.CountAsync());
            Assert.Equal(checkpoint==1 && withdrawalFirst ? 0 : 1, await observer.RawExportSourceEncryptionAttempts.CountAsync());
            Assert.Equal(2L, await observer.Database.SqlQuery<long>($"""
                SELECT "CurrentRevision" AS "Value" FROM tagekyc.raw_source_consent_references WHERE "ConsentReferenceId"={scope.Reference}
                """).SingleAsync());
            Assert.Equal(0, await Current(isolated, scope));
            Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_subject_consent_events").SingleAsync());
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            await loser.WaitAsync(TimeSpan.FromSeconds(15));
        }

        async Task<string> RunLoser()
        {
            await using var db = isolated.CreateDbContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            pid.SetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
            string result;
            if (withdrawalFirst) result = await Checkpoint(db);
            else { await WithdrawReference(db, scope); result = "Withdrawn"; }
            await tx.CommitAsync();
            return result;
        }

        string SuccessOutcome() => checkpoint switch { 1=>"NewReservation",3=>"Staged",4=>"Committed",5=>"Available",_=>throw new InvalidOperationException() };
        async Task<string> Checkpoint(TagEkycDbContext db)
        {
            if (checkpoint==1) return (await CompleteCandidate(db,candidate)).OutcomeCode;
            await Actor(db,Principal);
            return checkpoint switch {
                3=>await db.Database.SqlQuery<string>($"""
                    SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_stage_verified_source_ciphertext(
                     {source!.AttemptId},{source.ObjectId},1,1,1,{source.ObjectRevision})
                    """).SingleAsync(),
                4=>await db.Database.SqlQuery<string>($"""
                    SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_commit_staged_source(
                     {source!.AttemptId},2,1,1,{source.ObjectRevision})
                    """).SingleAsync(),
                5=>await db.Database.SqlQuery<string>($"""
                    SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_publish_available_source({publication!.Value},2,1)
                    """).SingleAsync(),
                _=>throw new InvalidOperationException()
            };
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task E01_RevisionInvalidation_UpdatedReferenceRejectsActualR1R3R4R5(int checkpoint)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_updated_checkpoint");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var candidate = await BeginCandidate(isolated, scope);
        await using var minio = checkpoint == 1 ? null : await DurableObjectMinioFixture.StartAsync();
        VerifiedRetained? source = checkpoint == 1 ? null :
            await VerifyRetainedCandidate(isolated, scope, candidate, minio!);
        Guid? publication = null;
        await using var db = isolated.CreateDbContext();
        if (checkpoint >= 4)
            Assert.Equal(RawExportR3StageDisposition.Staged,
                (await new RawExportR3StagingService(db).StageAsync(new(
                    Principal, source!.AttemptId, source.ObjectId, 1, 1, 1, source.ObjectRevision))).Disposition);
        if (checkpoint >= 5)
        {
            var committed = await new RawExportSourceFinalizationService(db).CommitAsync(new(
                Principal, source!.AttemptId, 2, 1, 1, source.ObjectRevision));
            Assert.Equal(SourceCommitDisposition.Committed, committed.Disposition);
            publication = committed.SourcePublicationId;
        }
        Assert.Equal(1, await Current(isolated, scope));
        var before = await CustodyRows(db);
        await UpdateReferenceFromAnotherOwnedSession(isolated, scope);
        Assert.True(await db.Database.SqlQuery<bool>($"""
            SELECT "ValidUntilUtc">clock_timestamp() AS "Value"
            FROM tagekyc.raw_source_consent_reference_events
            WHERE "ConsentReferenceId"={scope.Reference} AND "Revision"=1
            """).SingleAsync(),
            "The superseded revision-1 reference event must still be time-effective; only the latest-head rule may deny it.");
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            string outcome;
            if (checkpoint == 1)
                outcome = (await CompleteCandidate(db, candidate)).OutcomeCode;
            else
            {
                await Actor(db, Principal);
                outcome = checkpoint switch
                {
                    3 => await db.Database.SqlQuery<string>($"""
                        SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_stage_verified_source_ciphertext(
                         {source!.AttemptId},{source.ObjectId},1,1,1,{source.ObjectRevision})
                        """).SingleAsync(),
                    4 => await db.Database.SqlQuery<string>($"""
                        SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_commit_staged_source(
                         {source!.AttemptId},2,1,1,{source.ObjectRevision})
                        """).SingleAsync(),
                    5 => await db.Database.SqlQuery<string>($"""
                        SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_publish_available_source(
                         {publication!.Value},2,1)
                        """).SingleAsync(),
                    _ => throw new InvalidOperationException()
                };
            }
            Assert.Equal(checkpoint == 1 ? "SOURCE_RETENTION_NOT_AUTHORIZED" : "SourceRetentionNotAuthorized", outcome);
            await tx.CommitAsync();
        }
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(before, await CustodyRows(observer));
    }

    private static async Task UpdateReferenceFromAnotherOwnedSession(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated, Scope scope)
    {
        await using var db = isolated.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var session = VerificationSession.Create(Client, "synthetic-subject",
            VerificationProfile.ChallengeBoundEkycProfile, "synthetic-e01-update",
            [RequiredCheckType.DocumentNfc], now.AddHours(1), now, challenge: "synthetic-challenge");
        await new EfVerificationSessionRepository(db).AddAsync(session);
        await using var tx = await db.Database.BeginTransactionAsync();
        await Actor(db, Principal);
        Assert.Equal("Bound", await db.Database.SqlQuery<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.raw_source_record_consent_reference(
             {Principal},{Client},{session.Id},'synthetic-existing-consent','source-v2',1,
             'text-v2','source-hash-v2',{now.AddMinutes(-1)},{now.AddHours(1)},
             {Guid.NewGuid()},{new byte[32]})
            """).SingleAsync());
        await tx.CommitAsync();
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(2L, await observer.Database.SqlQuery<long>($"""
            SELECT "CurrentRevision" AS "Value" FROM tagekyc.raw_source_consent_references
             WHERE "ConsentReferenceId"={scope.Reference}
            """).SingleAsync());
    }

    [Fact]
    public async Task E01_ExactBothClassesRequired_CompletedExportFreezesBothAvailableSources()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_two_available");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var selfie = await Seed(isolated);
        await MakeRetainedSourceAvailable(isolated, selfie, minio);
        var dg2Artifact = Guid.NewGuid();
        Guid dg2Acceptance;
        await using (var db = isolated.CreateDbContext())
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var now = DateTimeOffset.UtcNow;
            db.Set<CaptureArtifactRow>().Add(new()
            {
                Id = dg2Artifact, VerificationSessionId = selfie.Session,
                ArtifactType = "NfcDg2Portrait", CaptureSource = "Nfc",
                CaptureAgentId = "40000000000040008000000000000001",
                DeviceId = "50000000000040008000000000000001",
                ArtifactHash = "sha256:" + new string('c', 64),
                MetadataHash = "sha256:" + new string('d', 64),
                QualityState = "Accepted", RequestId = "synthetic-dg2",
                CorrelationId = "synthetic-dg2-c", CreatedAt = now,
                ExpiresAt = now.AddHours(1)
            });
            await db.SaveChangesAsync();
            await Actor(db, Principal);
            dg2Acceptance = await db.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_append_capture_acceptance(
                 {selfie.Session},{Client},'ChipDg2Portrait',{dg2Artifact},1,
                 'synthetic-challenge','synthetic-evidence-dg2','synthetic-acceptance-policy',1) AS "Value"
                """).SingleAsync();
            await tx.CommitAsync();
        }
        await MakeRetainedSourceAvailable(isolated,
            selfie with { Acceptance = dg2Acceptance, Artifact = dg2Artifact },
            minio, "ChipDg2Portrait");

        var actor = new AuthenticatedRawExportActor(Principal, Client, ExportApiKey);
        await using var export = isolated.CreateDbContext();
        await new EfVerificationSessionRepository(export).SetStateAsync(
            selfie.Session, VerificationSessionState.Completed);
        await export.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT tagekyc.raw_export_bootstrap_global_authority(
             {ExportAdmin},'GrantAdmin','synthetic-e01-two-class-root')
            """);
        await new EfRawExportControlPlaneRepository(export).GrantExportPolicyAsync(new(
            ExportAdmin, Principal, selfie.Policy, 1, 0, null, "synthetic-e01-two-class-grant"));
        await new EfRawExportSubjectConsentRepository(export).RecordSubjectConsentGrantedAsync(new(
            Principal, selfie.Session, selfie.Policy, 1,
            new HashSet<RawExportRawClass>
            {
                RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage
            }, "existing-text-v1", "existing-source-hash", "synthetic-existing-consent",
            null, DateTimeOffset.UtcNow.AddMinutes(5)));
        var authorized = await Tip88B34AuthorizationEngineTests.CreateRepository(export)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                selfie.Session, selfie.Policy, $"a3-e01-both-{Guid.NewGuid():N}",
                [RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage], actor));
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, authorized.Decision.Outcome);
        await using (var tx = await export.Database.BeginTransactionAsync())
        {
            await Actor(export, Principal);
            foreach (var (rawClass, acceptance) in new[]
            {
                ("LiveSelfieImage", selfie.Acceptance),
                ("ChipDg2Portrait", dg2Acceptance)
            })
                Assert.NotEqual(Guid.Empty, await export.Database.SqlQuery<Guid>($"""
                    SELECT tagekyc.raw_export_select_session_capture_acceptance(
                     {selfie.Session},{rawClass},{acceptance}) AS "Value"
                    """).SingleAsync());
            await tx.CommitAsync();
        }
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(export);
        var bound = await jobs.BindAsync(new(actor, authorized.Permit!.PermitId,
            $"a3-e01-both-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(
            actor, bound.JobId, 0, 0, Guid.NewGuid()));
        await using (var tx = await export.Database.BeginTransactionAsync())
        {
            Assert.Equal("Frozen", (await Freeze(export, new(selfie, bound.JobId,
                acquired.AttemptId!.Value, acquired.Revision!.Value,
                acquired.FencingToken!.Value))).Outcome);
            await tx.CommitAsync();
        }
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(2, await observer.RawExportJobSourceBindings.CountAsync(x => x.JobId == bound.JobId));
        Assert.Equal(2, await observer.RawExportSourcePublications.CountAsync());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public async Task E01_RevisionInvalidation_ActualC1C3RejectsStaleReference(
        int checkpoint, bool withdraw)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_e01_export_checkpoint");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var c1 = await PrepareC1RaceJob(isolated, minio);
        C3RaceFixture? c3 = null;
        if (checkpoint == 3)
        {
            var assembly = await ExecuteRetainedAssemblyAsync(isolated, minio, new(
                c1.JobId, c1.AttemptId, c1.Revision, c1.Fence, Principal));
            Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, assembly.Result.Outcome);
            var package = await CreateFinalizedRecipientPackageAsync(isolated, assembly.Result);
            c3 = await CreateC3RaceDelivery(isolated, c1.Scope, package);
        }
        Assert.Equal(1, await Current(isolated, c1.Scope));
        await using var observer = isolated.CreateDbContext();
        var before = await CustodyRows(observer);
        if (withdraw)
        {
            await Grant(observer, "SubjectConsentWithdrawer");
            await using var withdrawTx = await observer.Database.BeginTransactionAsync();
            await WithdrawReference(observer, c1.Scope);
            await withdrawTx.CommitAsync();
        }
        else
            await UpdateReferenceFromAnotherOwnedSession(isolated, c1.Scope);
        await using var checkpointDb = isolated.CreateDbContext();
        await using var checkpointTx = await checkpointDb.Database.BeginTransactionAsync();
        if (checkpoint == 1)
        {
            Assert.Equal("AuthorityInvalid", (await Freeze(checkpointDb, c1)).Outcome);
            Assert.Equal(0, await observer.RawExportJobSourceBindings.CountAsync(x => x.JobId == c1.JobId));
        }
        else
        {
            Assert.Equal("Ineligible", await BeginC3(checkpointDb, c3!));
            Assert.Equal(0, await observer.RawExportRecipientPackageDeliveryEvents.CountAsync(
                x => x.DeliveryId == c3!.DeliveryId && x.EventType == "StreamingStarted"));
        }
        await checkpointTx.CommitAsync();
        Assert.Equal(before, await CustodyRows(observer));
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public async Task A3_RetainedStages_RealCiphertextPreservesWithdrawalResidue(int checkpoint, bool withdrawn)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_retained_stages");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var candidate = await BeginCandidate(isolated, scope);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await VerifyRetainedCandidate(isolated, scope, candidate, minio);
        await using var db = isolated.CreateDbContext();
        var stageCommand = new RawExportR3StageCommand(Principal, source.AttemptId, source.ObjectId, 1, 1, 1, source.ObjectRevision);
        var stage = new RawExportR3StagingService(db);
        var finalization = new RawExportSourceFinalizationService(db);
        var commitCommand = new CommitStagedSourceCommand(Principal, source.AttemptId, 2, 1, 1, source.ObjectRevision);
        Guid? publication = null;
        if (checkpoint>=4)
            Assert.Equal(RawExportR3StageDisposition.Staged, (await stage.StageAsync(stageCommand)).Disposition);
        if (checkpoint>=5)
        {
            var result = await finalization.CommitAsync(commitCommand);
            Assert.Equal(SourceCommitDisposition.Committed, result.Disposition);
            publication = result.SourcePublicationId;
        }
        if (withdrawn)
        {
            await Grant(db, "SubjectConsentWithdrawer");
            await using var tx = await db.Database.BeginTransactionAsync();
            await WithdrawReference(db, scope);
            await tx.CommitAsync();
        }
        await using var observer = isolated.CreateDbContext();
        var before = await CustodyRows(observer);
        if (checkpoint==3)
            Assert.Equal(withdrawn ? RawExportR3StageDisposition.SourceRetentionNotAuthorized : RawExportR3StageDisposition.Staged,
                (await stage.StageAsync(stageCommand)).Disposition);
        else if (checkpoint==4)
            Assert.Equal(withdrawn ? SourceCommitDisposition.SourceRetentionNotAuthorized : SourceCommitDisposition.Committed,
                (await finalization.CommitAsync(commitCommand)).Disposition);
        else
            Assert.Equal(withdrawn ? SourcePublishDisposition.SourceRetentionNotAuthorized : SourcePublishDisposition.Available,
                (await finalization.PublishAsync(new(Principal,publication!.Value,2,1))).Disposition);
        var after = await CustodyRows(observer);
        if (withdrawn) Assert.Equal(before, after);
        else Assert.NotEqual(before, after);
        Assert.Equal(0, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_subject_consent_events").SingleAsync());
        Assert.Equal("VerifiedCompleted", await observer.RawExportProvisionalObjects.Select(x=>x.State).SingleAsync());
        var attempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
        Assert.Null(attempt.R2TerminationDisposition);
        Assert.Null(attempt.R2TerminalIntentCode);
        Assert.Null(attempt.R2TerminalOutcomeCode);
        // CP08 consumes the actual post-stage tuple; head and attempt revision
        // deliberately diverge after staging. It must also remain readable
        // after withdrawal so that resource cleanup is not hidden.
        await using var continuationSource=NpgsqlDataSource.Create(observer.Database.GetConnectionString()!);
        var continuation=new RawSourceRetentionContinuationRepository(continuationSource);
        var readback=Assert.IsType<RawSourceRetentionContinuation>(await continuation.ReadAsync(attempt.SourceArtifactId,CancellationToken.None));
        var actualHead=await observer.RawExportSourceHeads.AsNoTracking().SingleAsync();
        Assert.Equal(actualHead.ReservationRevision,readback.ReservationRevision);
        Assert.Equal(attempt.EncryptionAttemptRevision,readback.EncryptionAttemptRevision);
        Assert.Equal(actualHead.CustodyState,readback.CustodyState);
        Assert.Equal(source.ObjectId,readback.ObjectCustodyId);
        Assert.Equal(source.ObjectRevision,readback.ObjectStateRevision);
        if(actualHead.CustodyState is "Staged" or "Available")
            Assert.NotEqual(readback.ReservationRevision,readback.EncryptionAttemptRevision);
        var storedPublication=await observer.RawExportSourcePublications.AsNoTracking().SingleOrDefaultAsync();
        Assert.Equal(storedPublication?.SourcePublicationId,readback.SourcePublicationId);
        Assert.Equal(storedPublication?.PublicationRevision,readback.PublicationRevision);
        Assert.Equal(storedPublication?.PublicationState,readback.PublicationState);
        Assert.Equal(storedPublication?.CleanupDisposition,readback.CleanupDisposition);
        var discovered=await continuation.ScanAsync(null,100,CancellationToken.None);
        Assert.Equal(actualHead.CustodyState!="Available"||storedPublication?.CleanupDisposition=="Pending",
            discovered.Contains(attempt.SourceArtifactId));
        if(checkpoint==5&&!withdrawn)
        {
            // Isolate the PendingR6 census branch from the existing real R3-R5
            // path. This fixture mutation is not a claim that cleanup ran.
            await using var pending=await observer.Database.BeginTransactionAsync();
            await observer.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role=replica");
            Assert.Equal(1,await observer.Database.ExecuteSqlRawAsync("""
                UPDATE tagekyc.raw_export_source_publications SET "CleanupDisposition"='Pending',
                 "CleanupEvidenceDigest"=NULL,"FinalizedAtUtc"=NULL,"PublicationRevision"=2
                """));
            Assert.Equal(new[]{attempt.SourceArtifactId},await observer.Database.SqlQueryRaw<Guid>("""
                SELECT "SourceArtifactId" AS "Value" FROM tagekyc.raw_export_list_retained_source_continuations(NULL,100)
                """).ToArrayAsync());
            Assert.Equal("Pending",await observer.Database.SqlQuery<string>($"""
                SELECT "CleanupDisposition" AS "Value" FROM tagekyc.raw_export_read_retained_source_continuation({attempt.SourceArtifactId})
                """).SingleAsync());
            await pending.RollbackAsync();
            Assert.Equal(readback,await continuation.ReadAsync(attempt.SourceArtifactId,CancellationToken.None));
        }
    }


    [Theory]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public async Task A3_RetainedStages_CrossBorderFulfillmentWaitUsesFreshClock(int checkpoint, bool expiresWhileWaiting)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_crossborder_clock");
        await Prepare(isolated);
        var scope = await Seed(isolated, crossBorder:true);
        var candidate = await BeginCandidate(isolated,scope);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await VerifyRetainedCandidate(isolated,scope,candidate,minio);
        await using var setup = isolated.CreateDbContext();
        Guid? publication = null;
        if (checkpoint>=4)
            Assert.Equal(RawExportR3StageDisposition.Staged,(await new RawExportR3StagingService(setup)
                .StageAsync(new(Principal,source.AttemptId,source.ObjectId,1,1,1,source.ObjectRevision))).Disposition);
        if (checkpoint>=5)
        {
            var committed = await new RawExportSourceFinalizationService(setup)
                .CommitAsync(new(Principal,source.AttemptId,2,1,1,source.ObjectRevision));
            Assert.Equal(SourceCommitDisposition.Committed,committed.Disposition);
            publication=committed.SourcePublicationId;
        }
        Assert.Equal(1,await setup.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_policy_requirements
             WHERE "PolicyId"={scope.Policy} AND "PolicyVersion"=1 AND "RequirementType"='CrossBorderAssessment'
            """).SingleAsync());
        var now = await setup.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var horizon = now.AddSeconds(expiresWhileWaiting ? 3 : 120);
        // A real control-plane revision, not UPDATE of protected fulfillment evidence.
        await new EfRawExportControlPlaneRepository(setup).AcceptFulfillmentAsync(new(Principal,scope.Policy,1,
            RawExportRequirementType.CrossBorderAssessment,1,1,"synthetic-cross-border","v2",
            now.AddMinutes(-1),horizon,"synthetic-cross-border-clock"));
        await using var blocker = isolated.CreateDbContext();
        await using var hold = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT pg_advisory_xact_lock(hashtext('tip88b1:fulfillment:'||{scope.Policy.ToString("D")}||':1:CrossBorderAssessment'))
            """);
        var blockerPid=await blocker.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        await using var observer = isolated.CreateDbContext();
        var before = await CustodyRows(observer);
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = RunCheckpoint();
        try
        {
            var backend = await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting = false;
            for(var i=0;i<100 && !waiting;i++)
            {
                waiting=await observer.Database.SqlQuery<bool>($"""
                    SELECT EXISTS(SELECT 1 FROM pg_locks l WHERE l.pid={backend} AND l.locktype='advisory'
                      AND l.mode='ShareLock' AND NOT l.granted AND l.objsubid=1
                      AND l.classid::bigint=((hashtext('tip88b1:fulfillment:'||CAST({scope.Policy} AS text)||':1:CrossBorderAssessment')::bigint>>32)&4294967295)
                      AND l.objid::bigint=(hashtext('tip88b1:fulfillment:'||CAST({scope.Policy} AS text)||':1:CrossBorderAssessment')::bigint&4294967295))
                      AND {blockerPid}=ANY(pg_blocking_pids({backend})) AS "Value"
                    """).SingleAsync();
                if (!waiting) await Task.Delay(25);
            }
            Assert.True(waiting,"Checkpoint must wait on the actual CrossBorderAssessment fulfillment domain.");
            Assert.True(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp()<{horizon} AS \"Value\"").SingleAsync(),
                "The caller must reach the blocking lock before the fulfillment expires.");
            Assert.Equal(before,await CustodyRows(observer));
            if (expiresWhileWaiting)
            {
                while(await observer.Database.SqlQuery<bool>($"SELECT clock_timestamp()<={horizon} AS \"Value\"").SingleAsync())
                    await Task.Delay(25);
            }
            await hold.CommitAsync();
            var result=await running.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(expiresWhileWaiting ? "SourceRetentionNotAuthorized" :
                checkpoint switch {3=>"Staged",4=>"Committed",5=>"Available",_=>throw new InvalidOperationException()},result);
            if (expiresWhileWaiting) Assert.Equal(before,await CustodyRows(observer));
            else Assert.NotEqual(before,await CustodyRows(observer));
            var attempt=await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
            Assert.Null(attempt.R2TerminalOutcomeCode);
            Assert.Null(attempt.R2TerminationDisposition);
            Assert.Equal("VerifiedCompleted",await observer.RawExportProvisionalObjects.Select(x=>x.State).SingleAsync());
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            await running.WaitAsync(TimeSpan.FromSeconds(15));
        }
        async Task<string> RunCheckpoint()
        {
            await using var db=isolated.CreateDbContext();
            await using var tx=await db.Database.BeginTransactionAsync();
            await Actor(db,Principal);
            pid.SetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
            var result=checkpoint switch {
                3=>await db.Database.SqlQuery<string>($"""
                    SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_stage_verified_source_ciphertext(
                     {source.AttemptId},{source.ObjectId},1,1,1,{source.ObjectRevision})
                    """).SingleAsync(),
                4=>await db.Database.SqlQuery<string>($"""
                    SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_commit_staged_source(
                     {source.AttemptId},2,1,1,{source.ObjectRevision})
                    """).SingleAsync(),
                5=>await db.Database.SqlQuery<string>($"""
                    SELECT "Outcome" AS "Value" FROM tagekyc.raw_export_publish_available_source({publication!.Value},2,1)
                    """).SingleAsync(),
                _=>throw new InvalidOperationException()
            };
            await tx.CommitAsync();
            return result;
        }
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_BoundReader_DerivesCustodyActorAndRequiresExactRawIngress(bool rawIngress)
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_bound_reader");
        await Prepare(isolated);
        var scope=await Seed(isolated,rawIngress:rawIngress);
        var baseline=await Read();
        Assert.Equal(rawIngress ? 1 : 0,baseline.Length);
        if (rawIngress)
        {
            using var row=System.Text.Json.JsonDocument.Parse(Assert.Single(baseline));
            Assert.Equal(15,row.RootElement.EnumerateObject().Count());
            Assert.Equal(scope.RuntimeBinding,row.RootElement.GetProperty("binding_id").GetGuid());
            Assert.Equal(Principal,row.RootElement.GetProperty("principal_id").GetGuid());
            Assert.Equal(Client,row.RootElement.GetProperty("client_application_id").GetGuid());
            Assert.Equal(scope.Acceptance,row.RootElement.GetProperty("capture_acceptance_id").GetGuid());
            Assert.Equal(scope.Permit,row.RootElement.GetProperty("source_retention_authority_id").GetGuid());
            Assert.Equal("synthetic-scope",row.RootElement.GetProperty("stable_data_scope_id").GetString());
            Assert.Equal("synthetic-controller",row.RootElement.GetProperty("controller_identity").GetString());
            Assert.Equal("synthetic-subject",row.RootElement.GetProperty("subject_ref").GetString());
        }
        foreach(var field in new[]{"runtime","installation","credential","generation","policy","roleRevision",
            "session","artifact","captureRevision","rawClass","configurationRevision"})
            Assert.Empty(await Read(field));
        await using var observer=isolated.CreateDbContext();
        Assert.Equal(0,await observer.RawExportAuthoritySnapshots.CountAsync());
        Assert.Equal(0,await observer.RawExportSourceReservations.CountAsync());
        const string signature="tagekyc.capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz)";
        Assert.True(await observer.Database.SqlQuery<bool>($"SELECT has_function_privilege('tagekyc_raw_export_claim_broker',{signature},'EXECUTE') AS \"Value\"").SingleAsync());
        foreach(var role in new[]{"tagekyc_runtime","tagekyc_capture_runtime_application","tagekyc_capture_runtime_authenticator","tagekyc_capture_runtime_operator"})
            Assert.False(await observer.Database.SqlQuery<bool>($"SELECT has_function_privilege({role},{signature},'EXECUTE') AS \"Value\"").SingleAsync());
        await Grant(observer,"SubjectConsentWithdrawer");
        await using(var tx=await observer.Database.BeginTransactionAsync())
        {
            await WithdrawReference(observer,scope);
            await tx.CommitAsync();
        }
        Assert.Empty(await Read());

        async Task<string[]> Read(string? changed=null)
        {
            var runtime=Guid.Parse("40000000-0000-4000-8000-000000000001");
            var installation=Guid.Parse("50000000-0000-4000-8000-000000000001");
            var credential=Guid.Parse("60000000-0000-4000-8000-000000000001");
            var policy=Guid.Parse("10000000-0000-4000-8000-000000000001");
            await using var db=isolated.CreateDbContext();
            await using var tx=await db.Database.BeginTransactionAsync();
            // B-R is the actor source: it must not require a fabricated Client/P context.
            await db.Database.ExecuteSqlRawAsync("SELECT set_config('tagekyc.actor_principal_id','',true)");
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
            var rows=await db.Database.SqlQuery<string>($"""
                SELECT to_jsonb(x)::text AS "Value" FROM tagekyc.capture_runtime_read_bound_raw_ingress(
                 {(changed=="runtime" ? Guid.NewGuid() : runtime)},
                 {(changed=="installation" ? Guid.NewGuid() : installation)},
                 {(changed=="credential" ? Guid.NewGuid() : credential)},
                 {(changed=="generation" ? 2L : 1L)},{(changed=="policy" ? Guid.NewGuid() : policy)},
                 {(changed=="roleRevision" ? 2L : 1L)},
                 {(changed=="session" ? Guid.NewGuid() : scope.Session)},
                 {(changed=="artifact" ? Guid.NewGuid() : scope.Artifact)},
                 {(changed=="captureRevision" ? 2 : 1)},
                 {(changed=="rawClass" ? "ChipDg2Portrait" : "LiveSelfieImage")},
                 {(changed=="configurationRevision" ? 2L : 1L)},clock_timestamp()) x
                """).ToArrayAsync();
            await tx.CommitAsync();
            return rows;
        }
    }


    [Fact]
    public async Task A3_BoundReader_AllA1AdvisoryDomainsPrecedeRuntimeRowLocks()
    {
        await using var isolated=await postgres.CreateDisposableCurrentDatabaseAsync("a3_bound_lock_order");
        await Prepare(isolated);
        var scope=await Seed(isolated,rawIngress:true);
        await using var blocker=isolated.CreateDbContext();
        await using var hold=await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({scope.Session.ToString("D")},70))");
        var pid=new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running=Reader();
        await using var observer=isolated.CreateDbContext();
        try
        {
            var backend=await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting=false;
            for(var n=0;n<100&&!waiting;n++)
            {
                waiting=await observer.Database.SqlQuery<bool>($"""
                    SELECT EXISTS(SELECT 1 FROM pg_locks l CROSS JOIN LATERAL
                     (SELECT hashtextextended(CAST({scope.Session} AS text),70) k) domain
                     WHERE l.pid={backend} AND l.locktype='advisory' AND l.mode='ExclusiveLock' AND NOT l.granted
                     AND l.classid::bigint=((domain.k>>32)&4294967295) AND l.objid::bigint=(domain.k&4294967295)
                     AND l.objsubid=1) AS "Value"
                    """).SingleAsync();
                if(!waiting) await Task.Delay(25);
            }
            Assert.True(waiting,"B-R must wait at A1's actual session advisory domain.");
            await using(var observation=await observer.Database.BeginTransactionAsync())
            {
                // A1 can own domain 70 before taking runtime rows. B-R must not hold
                // those rows while waiting for 70, otherwise the opposite route deadlocks.
                await observer.Database.ExecuteSqlRawAsync("""
                    SELECT "CaptureAgentId" FROM tagekyc.capture_runtime_registrations
                     WHERE "CaptureAgentId"='40000000-0000-4000-8000-000000000001' FOR UPDATE NOWAIT
                    """);
                await observation.RollbackAsync();
            }
            await hold.CommitAsync();
            Assert.Equal(1,await running.WaitAsync(TimeSpan.FromSeconds(15)));
        }
        finally
        {
            if(hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            await running.WaitAsync(TimeSpan.FromSeconds(15));
        }
        async Task<int> Reader()
        {
            await using var db=isolated.CreateDbContext();
            await using var tx=await db.Database.BeginTransactionAsync();
            pid.SetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
            var count=await db.Database.SqlQuery<int>($"""
                SELECT count(*)::integer AS "Value" FROM tagekyc.capture_runtime_read_bound_raw_ingress(
                 '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
                 '60000000-0000-4000-8000-000000000001',1,'10000000-0000-4000-8000-000000000001',1,
                 {scope.Session},{scope.Artifact},1,'LiveSelfieImage',1,clock_timestamp())
                """).SingleAsync();
            await tx.CommitAsync();
            return count;
        }
    }

    private static async Task<string[]> CustodyRows(TagEkycDbContext db) => await db.Database.SqlQueryRaw<string>("""
        SELECT 'reservation:'||to_jsonb(r)::text AS "Value" FROM tagekyc.raw_export_source_reservations r
        UNION ALL SELECT 'attempt:'||to_jsonb(a)::text FROM tagekyc.raw_export_source_encryption_attempts a
        UNION ALL SELECT 'head:'||to_jsonb(h)::text FROM tagekyc.raw_export_source_head h
        UNION ALL SELECT 'publication:'||to_jsonb(p)::text FROM tagekyc.raw_export_source_publications p
        UNION ALL SELECT 'key:'||to_jsonb(k)::text FROM tagekyc.raw_export_attempt_key_reservations k
        UNION ALL SELECT 'object:'||to_jsonb(o)::text FROM tagekyc.raw_export_provisional_objects o
        ORDER BY "Value"
        """).ToArrayAsync();

    internal static async Task<Guid> CreateVerifiedContinuationSource(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated, DurableObjectMinioFixture minio)
    {
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var candidate = await BeginCandidate(isolated, scope);
        var verified = await VerifyRetainedCandidate(isolated, scope, candidate, minio);
        await using var observer = isolated.CreateDbContext();
        return await observer.RawExportSourceEncryptionAttempts.Where(x => x.AttemptId == verified.AttemptId)
            .Select(x => x.SourceArtifactId).SingleAsync();
    }

    internal static async Task MakeRetainedSourceAvailable(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated, Scope scope, DurableObjectMinioFixture minio,
        string rawClass = "LiveSelfieImage")
    {
        var candidate = await BeginCandidate(isolated, scope, rawClass: rawClass);
        var verified = await VerifyRetainedCandidate(isolated, scope, candidate, minio, rawClass);
        await using var db = isolated.CreateDbContext();
        var attempt = await db.RawExportSourceEncryptionAttempts.AsNoTracking()
            .SingleAsync(x => x.AttemptId == verified.AttemptId);
        var staged = await new RawExportR3StagingService(db).StageAsync(new(
            Principal, attempt.AttemptId, verified.ObjectId, 1, 1, attempt.Fence, verified.ObjectRevision));
        Assert.Equal(RawExportR3StageDisposition.Staged, staged.Disposition);
        var committed = await new RawExportSourceFinalizationService(db).CommitAsync(new(
            Principal, attempt.AttemptId, staged.ReservationRevision!.Value, 1, attempt.Fence, verified.ObjectRevision));
        Assert.Equal(SourceCommitDisposition.Committed, committed.Disposition);
        var published = await new RawExportSourceFinalizationService(db).PublishAsync(new(
            Principal, committed.SourcePublicationId!.Value, staged.ReservationRevision.Value, attempt.Fence));
        Assert.Equal(SourcePublishDisposition.Available, published.Disposition);
    }

    private static async Task<C1RaceFixture> PrepareC1RaceJob(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        DurableObjectMinioFixture minio,
        string externalConsentArtifactRef = "synthetic-existing-consent",
        bool bootstrapRuntime = true)
    {
        var scope = await Seed(isolated, externalConsentArtifactRef: externalConsentArtifactRef,
            bootstrapRuntime: bootstrapRuntime);
        await MakeRetainedSourceAvailable(isolated, scope, minio);
        var actor = new AuthenticatedRawExportActor(Principal, Client, ExportApiKey);
        await using var db = isolated.CreateDbContext();
        await new EfVerificationSessionRepository(db).SetStateAsync(scope.Session, VerificationSessionState.Completed);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT tagekyc.raw_export_bootstrap_global_authority({ExportAdmin},'GrantAdmin','synthetic-a3-c1-race-root')");
        await new EfRawExportControlPlaneRepository(db).GrantExportPolicyAsync(new(
            ExportAdmin, Principal, scope.Policy, 1, 0, null, "synthetic-a3-c1-race-grant"));
        await new EfRawExportSubjectConsentRepository(db).RecordSubjectConsentGrantedAsync(new(
            Principal, scope.Session, scope.Policy, 1,
            new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage },
            "existing-text-v1", "existing-source-hash", "synthetic-existing-consent", null,
            DateTimeOffset.UtcNow.AddMinutes(5)));
        var authorized = await Tip88B34AuthorizationEngineTests.CreateRepository(db)
            .AuthorizeExportAsync(Tip88B34AuthorizationEngineTests.Command(
                scope.Session, scope.Policy, $"a3-c1-race-{Guid.NewGuid():N}",
                [RawExportRawClass.LiveSelfieImage], actor));
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, authorized.Decision.Outcome);
        await using (var selectTx = await db.Database.BeginTransactionAsync())
        {
            await Actor(db, Principal);
            Assert.NotEqual(Guid.Empty, await db.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_select_session_capture_acceptance(
                    {scope.Session},'LiveSelfieImage',{scope.Acceptance}) AS "Value"
                """).SingleAsync());
            await selectTx.CommitAsync();
        }
        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var bound = await jobs.BindAsync(new(actor, authorized.Permit!.PermitId, $"a3-c1-race-{Guid.NewGuid():N}"));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(new(actor, bound.JobId, 0, 0, Guid.NewGuid()));
        return new(scope, bound.JobId, acquired.AttemptId!.Value,
            acquired.Revision!.Value, acquired.FencingToken!.Value);
    }

    private static async Task<C1FreezeResult> Freeze(TagEkycDbContext db, C1RaceFixture fixture)
    {
        await Actor(db, Principal);
        return await db.Database.SqlQuery<C1FreezeResult>($"""
            SELECT "Outcome","BindingCount" FROM tagekyc.raw_export_freeze_job_source_bindings(
                {fixture.JobId},{fixture.AttemptId},{fixture.Revision},{fixture.Fence},{Principal})
            """).SingleAsync();
    }

    private static async Task<int> ActualB2Withdraw(TagEkycDbContext db, Scope scope)
    {
        await Actor(db, Principal);
        return await db.Database.SqlQuery<int>($"""
            SELECT tagekyc.raw_export_append_subject_consent_withdrawn(
                {scope.Session},{scope.Policy},1,1,1,'synthetic-a3-cp04-withdraw',NULL::text) AS "Value"
            """).SingleAsync();
    }

    private static async Task<A3AssemblyResult> ExecuteRetainedAssemblyAsync(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        DurableObjectMinioFixture minio,
        RawExportAssemblyExecutionRequest request,
        Func<Task>? afterPrepare = null,
        CancellationToken cancellationToken = default)
    {
        await using var verifyDb = isolated.CreateDbContext();
        await using var lookupDb = isolated.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(lookupDb));
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef",
            ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
                "abcdef0123456789abcdef0123456789",
            ["TagEkyc:RawExport:CustodyProfile:Profile"] = "Fixture",
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "30",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300",
        };
        await using var services = new ServiceCollection()
            .AddScoped(_ => isolated.CreateDbContext())
            .AddTagEkycRawExportSourceClaimComparison(configuration)
            .BuildServiceProvider();
        await using var contentScope = services.CreateAsyncScope();
        var repository = new RawExportAssemblyRepository(new A3AssemblyConnectionFactory(
            verifyDb.Database.GetConnectionString()!));
        var resolver = new RawExportAssemblySourceResolver(
            repository,
            new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
            new RawExportFramedSourceVerificationService(
                new AttemptAeadVerificationOperationService(
                    verifyDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                contentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>()));
        var provider = new A3AssemblyPreparationProvider(afterPrepare);
        var orchestrator = new RawExportAssemblyOrchestrator(
            repository,
            resolver,
            new RawExportAssemblyAuthenticationService(new A3AssemblyAuthenticator()),
            provider);
        var result = await orchestrator.ExecuteAsync(request, cancellationToken);
        return new(result, provider.PrepareCount, provider.Finalized);
    }

    private static async Task<A3FinalizedPackage> CreateFinalizedRecipientPackageAsync(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        RawExportAssemblyExecutionResult assemblyResult)
    {
        Assert.NotNull(assemblyResult.AssemblyId);
        await using var db = isolated.CreateDbContext();
        var assembly = await db.RawExportAssemblyIdentities.AsNoTracking()
            .SingleAsync(x => x.AssemblyId == assemblyResult.AssemblyId);
        using var recipientKey = RSA.Create(3072);
        var spki = recipientKey.ExportSubjectPublicKeyInfo();
        var keyFingerprint = SHA256.HashData(spki);
        var keyId = $"a3-c3-{Guid.NewGuid():N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"='Revoked',"Revision"="Revision"+1,
                "RevokedAtUtc"=clock_timestamp(),"RevocationReason"='A3_C3_TEST_REVOKE'
            WHERE "RecipientClientApplicationId"={assembly.RecipientClientApplicationId} AND "State"='Active'
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tagekyc.raw_export_recipient_key_registrations(
              "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
              "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
            VALUES({assembly.RecipientClientApplicationId},{keyId},1,'RSA-OAEP-256',{spki},{keyFingerprint},
              clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Active',1,clock_timestamp())
            """);
        var equality = RecipientPackageCodec.PackageEqualityFingerprint(
            assembly.C2PreparationId, assembly.AssemblyId, assembly.AssemblyFingerprint,
            assembly.ManifestDigest, assembly.AssemblyDigest, assembly.AssemblyAuthenticationValue,
            assembly.RecipientClientApplicationId, keyId, 1, keyFingerprint, assembly.CompleteAssemblyLength);
        var packageId = RecipientPackageCodec.PackageId(assembly.C2PreparationId, equality);
        var provider = A3RecipientPackageProvider();
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
        var objectKey = RecipientPackageCodec.ObjectKey(packageId);
        var binding = RecipientPackageCodec.ObjectBindingDigest(
            provider.ProviderConfigurationId, endpoint, provider.BucketName, objectKey);
        var repository = new RecipientPackageRepository(new A3RecipientPackageConnectionFactory(
            db.Database.GetConnectionString()!));
        var reserved = await repository.ReserveAsync(new(
            assembly.C2PreparationId, packageId, assembly.AssemblyId, assembly.JobId, assembly.AttemptId,
            assembly.FencingToken, assembly.AssemblyFingerprint, assembly.ManifestDigest,
            assembly.AssemblyDigest, assembly.AssemblyAuthenticationValue, assembly.CompleteAssemblyLength,
            assembly.RecipientClientApplicationId, keyId, 1, keyFingerprint, 1, equality,
            RecipientPackageCodec.ProviderOperationTokenDigest(RandomNumberGenerator.GetBytes(32)),
            RecipientPackageProviderConfiguration.ProviderKind, provider.ProviderConfigurationId,
            endpoint, provider.BucketName, objectKey, binding, RecipientPackageOptions.PackageProfile),
            CancellationToken.None);
        Assert.Equal("Reserved", reserved.Outcome);
        var envelopeBytes = "TIP-88C1-C2-PACKAGE-V1\0\0\0\u0002{}"u8.ToArray();
        var packageBytes = envelopeBytes;
        var envelopeDigest = SHA256.HashData(envelopeBytes);
        var ciphertextDigest = SHA256.HashData(packageBytes);
        var begun = await repository.BeginPutAsync(
            assembly.C2PreparationId, reserved.RowRevision!.Value,
            envelopeDigest, packageBytes.LongLength, ciphertextDigest, CancellationToken.None);
        Assert.Equal("PutInFlight", begun.Outcome);
        var prepared = await repository.RecordPreparedAsync(
            RecipientPackageDatabaseCapability.Reconciler, assembly.C2PreparationId,
            begun.RowRevision!.Value, RandomNumberGenerator.GetBytes(32), packageBytes.LongLength,
            ciphertextDigest, RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32),
            CancellationToken.None);
        Assert.Equal("Prepared", prepared.Outcome);
        var finalized = await repository.FinalizeAsync(
            assembly.C2PreparationId, prepared.RowRevision!.Value,
            assembly.AssemblyFingerprint, CancellationToken.None);
        Assert.Equal("Finalized", finalized.Outcome);
        return new(packageId, assembly.RecipientClientApplicationId, Principal);
    }

    private static async Task<C3RaceFixture> CreateC3RaceDelivery(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Scope scope,
        A3FinalizedPackage package)
    {
        await using var db = isolated.CreateDbContext();
        var repository = new RecipientPackageDeliveryRepository(new A3DeliveryConnectionFactory(
            db.Database.GetConnectionString()!));
        var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"a3-c3-race-{Guid.NewGuid():N}"));
        var delivery = RecipientPackageDeliveryCodec.DeliveryId(package.RecipientId, digest);
        var apiKey = Guid.NewGuid();
        var correlation = RandomNumberGenerator.GetBytes(32);
        var created = await repository.CreateAsync(package.RecipientId, package.PackageId, delivery,
            digest, apiKey, package.PrincipalId, correlation, CancellationToken.None);
        Assert.Equal("Created", created.Outcome);
        return new(scope, delivery, package.RecipientId, apiKey, package.PrincipalId, correlation);
    }

    private static async Task<string> BeginC3(TagEkycDbContext db, C3RaceFixture fixture)
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_package_delivery");
        return await db.Database.SqlQuery<string>($"""
            SELECT outcome AS "Value" FROM tagekyc.raw_export_begin_recipient_package_delivery_stream(
                {fixture.RecipientId},{fixture.DeliveryId},{fixture.ApiKeyId},
                {fixture.PrincipalId},{fixture.CorrelationDigest})
            """).SingleAsync();
    }

    private static RecipientPackageProviderConfiguration A3RecipientPackageProvider() => new(
        "a3-c3-synthetic-v1", new Uri("http://127.0.0.1:9000/"), "tagekyc-a3-c3-synthetic",
        true, "us-east-1", new("writer", "secret"), new("reader", "secret"),
        new("lifecycle", "secret"), new("posture", "secret"), true);

    private static async Task<VerifiedRetained> VerifyRetainedCandidate(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Scope scope, Candidate candidate, DurableObjectMinioFixture minio, string rawClass = "LiveSelfieImage")
    {
        byte[] plaintext = "synthetic-retainedsource"u8.ToArray();
        Assert.Equal(24, plaintext.Length);
        var configuration = new ConfigurationManager {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"]="0123456789abcdef0123456789abcdef" };
        await using var services = new ServiceCollection().AddTagEkycContentCommitment(configuration).BuildServiceProvider();
        var commitments = services.GetRequiredService<IContentCommitmentService>();
        var commitment = await commitments.ComputeAsync(new("fixture-content-commitment",1),
            C1HashCanonical.EncodeLengthPrefixedPayload("TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1",
             "synthetic-scope","synthetic-controller",scope.Session.ToString("N"),scope.Artifact.ToString("N"),"1",
             rawClass,Convert.ToHexString(SHA256.HashData(plaintext)).ToLowerInvariant(),"24",candidate.MediaType),CancellationToken.None);
        Assert.True(commitment.IsSuccess);
        Completion completion;
        await using (var db = isolated.CreateDbContext())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            completion = await CompleteCandidate(db,candidate,commitment.Mac.ToArray());
            Assert.Equal("NewReservation", completion.OutcomeCode);
            await tx.CommitAsync();
        }
        await using var observer = isolated.CreateDbContext();
        // A disposable clone may contain committed historical rows. Identify
        // this operation by its returned identity, never global table cardinality.
        var attempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking()
            .Where(x => x.AttemptId == completion.AttemptId).SingleAsync();
        Assert.Equal(completion.SourceArtifactId, attempt.SourceArtifactId);
        Assert.Equal(completion.AttemptKeyReservationId, attempt.AttemptKeyReservationId);
        await using var writerDb = isolated.CreateDbContext();
        await using var writerLookup = isolated.CreateDbContext();
        await using var recorderSource = NpgsqlDataSource.Create(writerDb.Database.GetConnectionString()!);
        var writerKek = new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(writerDb),new PostgresFixtureKekJournal(writerLookup));
        var writer = new RawExportR2EncryptionOrchestrator(new RawExportR2Repository(writerDb),
            new PostgresAttemptKeyReservationProvider(writerDb,new PostgresKeyProviderOperationMap(writerDb),writerKek),
            new AttemptAeadEncryptionOperationService(writerDb,writerKek,DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            commitments,new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)),
            new RawExportR2TerminalIntentRecorder(recorderSource,Principal,3000,CancellationToken.None),
            classMaximumBytes:24); // Explicit synthetic class bound; not API configuration resolution.
        await using var body = new MemoryStream(plaintext,writable:false);
        var written = await writer.ExecuteAsync(new(Principal,attempt.AttemptKeyReservationId,attempt.AttemptId,
            attempt.SourceArtifactId,1,1,body),CancellationToken.None);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification,written.Disposition);
        Assert.NotNull(written.ObjectCustodyId);
        await using var verifyDb = isolated.CreateDbContext();
        await using var verifyLookup = isolated.CreateDbContext();
        var verifyKek = new FixtureDurableKekOperationProvider(new PostgresFixtureKekJournal(verifyDb),new PostgresFixtureKekJournal(verifyLookup));
        var verified = await new RawExportR2CompletionVerifier(new RawExportR2Repository(verifyDb),
            new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
            new AttemptAeadVerificationOperationService(verifyDb,verifyKek,DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),commitments)
            .ExecuteAsync(new(Principal,attempt.AttemptId,1,1,written.ObjectCustodyId.Value),CancellationToken.None);
        Assert.Equal(RawExportR2VerificationDisposition.Verified,verified.Disposition);
        var objectRow = await observer.RawExportProvisionalObjects.AsNoTracking()
            .Where(x => x.ObjectCustodyId == written.ObjectCustodyId).SingleAsync();
        Assert.Equal(attempt.AttemptId, objectRow.AttemptId);
        Assert.Equal(attempt.SourceArtifactId, objectRow.SourceArtifactId);
        Assert.Equal("VerifiedCompleted",objectRow.State);
        return new(attempt.AttemptId,objectRow.ObjectCustodyId,objectRow.StateRevision);
    }

    private sealed record VerifiedRetained(Guid AttemptId,Guid ObjectId,long ObjectRevision);

    [Fact]
    public async Task A3_TaggedStageFunctions_PreserveRealLegacyR2ThroughAvailableAndReplay()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_stage_legacy");
        await Prepare(isolated);
        await using var observer = isolated.CreateDbContext();
        var fixture = new PostgresPersistenceFixture(observer.Database.GetConnectionString()!);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await new Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(fixture)
            .CreateR3VerifiedSourceAsync("synthetic-a3-legacy-stage"u8.ToArray(), minio);
        await using var db = isolated.CreateDbContext();
        var stageCommand = new RawExportR3StageCommand(source.ActorPrincipalId, source.AttemptId,
            source.ObjectCustodyId, source.ReservationRevision, source.EncryptionAttemptRevision,
            source.Fence, source.ObjectStateRevision);
        var stage = new RawExportR3StagingService(db);
        var staged = await stage.StageAsync(stageCommand);
        Assert.Equal(RawExportR3StageDisposition.Staged, staged.Disposition);
        var stageReplay = await stage.StageAsync(stageCommand);
        Assert.Equal(RawExportR3StageDisposition.ExistingMatch, stageReplay.Disposition);
        Assert.Equal(staged.StagedCiphertextFingerprint, stageReplay.StagedCiphertextFingerprint);
        Assert.Equal(staged.StagedAtUtc, stageReplay.StagedAtUtc);
        var finalization = new RawExportSourceFinalizationService(db);
        var commitCommand = new CommitStagedSourceCommand(source.ActorPrincipalId, source.AttemptId,
            staged.ReservationRevision!.Value, source.EncryptionAttemptRevision, source.Fence, source.ObjectStateRevision);
        var committed = await finalization.CommitAsync(commitCommand);
        Assert.Equal(SourceCommitDisposition.Committed, committed.Disposition);
        var commitReplay = await finalization.CommitAsync(commitCommand);
        Assert.Equal(SourceCommitDisposition.ExistingMatch, commitReplay.Disposition);
        Assert.Equal(committed.CommitEvidenceDigest, commitReplay.CommitEvidenceDigest);
        var publishCommand = new PublishAvailableSourceCommand(source.ActorPrincipalId, committed.SourcePublicationId!.Value,
            staged.ReservationRevision.Value, source.Fence);
        var published = await finalization.PublishAsync(publishCommand);
        Assert.Equal(SourcePublishDisposition.Available, published.Disposition);
        var publishReplay = await finalization.PublishAsync(publishCommand);
        Assert.Equal(SourcePublishDisposition.ExistingMatch, publishReplay.Disposition);
        Assert.Equal(published.AvailableEvidenceDigest, publishReplay.AvailableEvidenceDigest);
        Assert.Equal(published.AvailableAtUtc, publishReplay.AvailableAtUtc);
        Assert.Equal(1, await observer.RawExportSourcePublications.CountAsync());
        var attempt = await observer.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync();
        Assert.Null(attempt.R2TerminalIntentCode);
        Assert.Null(attempt.R2TerminalIntentDisposition);
        Assert.Null(attempt.R2TerminalIntentAtUtc);
        Assert.Null(attempt.R2TerminalOutcomeCode);
        Assert.Equal("Available", await observer.RawExportSourceHeads.Select(x => x.CustodyState).SingleAsync());
    }

    [Fact]
    public async Task A3_RetainedSnapshot_ExactReplayAndReferenceWithdrawal()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_snapshot");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        var first = await Append(isolated, scope);
        Assert.Equal(first, await Append(isolated, scope));
        Assert.Equal(1, await Current(isolated, scope));
        await using (var db = isolated.CreateDbContext())
            await new EfVerificationSessionRepository(db).SetStateAsync(scope.Session, VerificationSessionState.Completed);
        Assert.Equal(1, await Current(isolated, scope)); // Retention is not early B2 export consent.
        await using (var db = isolated.CreateDbContext())
        {
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_subject_consent_events").SingleAsync());
            await Grant(db, "SubjectConsentWithdrawer");
            await using var tx = await db.Database.BeginTransactionAsync();
            await Actor(db, Principal);
            Assert.Equal("Withdrawn", await db.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_withdraw_consent_reference(
                 {Principal},{Client},{scope.Reference},1,'source-v1','synthetic-withdrawal',{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync());
            await tx.CommitAsync();
        }
        Assert.Equal(0, await Current(isolated, scope));
        var failure = await Assert.ThrowsAsync<PostgresException>(() => Append(isolated, scope));
        Assert.Equal("A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED", failure.MessageText);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
        Assert.Equal(first, await observer.Database.SqlQueryRaw<Guid>("SELECT \"AuthoritySnapshotId\" AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
    }

    [Fact]
    public async Task A3_RetentionSnapshot_ExactPermitRevision()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_exact_permit_revision");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        await using var db = isolated.CreateDbContext();

        foreach (var missingRevision in new[] { false, true })
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await Actor(db, Principal);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
            var denied = missingRevision
                ? await Assert.ThrowsAsync<PostgresException>(() => db.Database.SqlQuery<Guid>($"""
                    SELECT "AuthoritySnapshotId" AS "Value" FROM tagekyc.raw_export_append_retained_authority_snapshot(
                     {scope.RuntimeBinding},{scope.Acceptance},'LiveSelfieImage',{scope.Permit},NULL::bigint)
                    """).SingleAsync())
                : await Assert.ThrowsAsync<PostgresException>(() => db.Database.SqlQuery<Guid>($"""
                    SELECT "AuthoritySnapshotId" AS "Value" FROM tagekyc.raw_export_append_retained_authority_snapshot(
                     {scope.RuntimeBinding},{scope.Acceptance},'LiveSelfieImage',{scope.Permit},2::bigint)
                    """).SingleAsync());
            Assert.Equal("A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED", denied.MessageText);
            await tx.RollbackAsync();
        }

        Assert.Equal(0, await db.Database.SqlQueryRaw<int>(
            "SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
        var snapshot = await Append(isolated, scope);
        Assert.Equal(1L, await db.Database.SqlQuery<long>($"""
            SELECT "RetentionAuthorityRevision" AS "Value" FROM tagekyc.raw_export_authority_snapshots
            WHERE "AuthoritySnapshotId"={snapshot}
            """).SingleAsync());
        Assert.Equal(1, await db.Database.SqlQuery<int>($"""
            SELECT "AuthoritySnapshotSchemaVersion" AS "Value" FROM tagekyc.raw_export_authority_snapshots
            WHERE "AuthoritySnapshotId"={snapshot}
            """).SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT atttypid='bigint'::regtype AS "Value" FROM pg_catalog.pg_attribute
            WHERE attrelid='tagekyc.raw_export_authority_snapshots'::regclass
              AND attname='RetentionAuthorityRevision' AND attnum>0
            """).SingleAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_RetainedSnapshot_TerminalKindCannotBeBypassed(bool revoke)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_snapshot_terminal");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        await Append(isolated, scope);
        await using (var db = isolated.CreateDbContext())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await Actor(db, Principal);
            var function = revoke ? "raw_export_revoke_authority_snapshot" : "raw_export_withdraw_authority_snapshot";
            Assert.Equal(2L, await db.Database.SqlQueryRaw<long>($"""
                SELECT tagekyc.{function}(@p0,@p1,@p2,'LiveSelfieImage',1,@p3) AS "Value"
                """, Client, scope.Session, scope.Acceptance, Principal).SingleAsync());
            await tx.CommitAsync();
        }
        Assert.Equal(0, await Current(isolated, scope));
        var failure = await Assert.ThrowsAsync<PostgresException>(() => Append(isolated, scope));
        Assert.Equal("A3_RETENTION_SNAPSHOT_NOT_AUTHORIZED", failure.MessageText);
        await using var observer = isolated.CreateDbContext();
        Assert.True(await observer.Database.SqlQueryRaw<bool>("""
            SELECT "AuthorityKind"='SourceRetention' AND "RetentionAuthorityId" IS NULL
             AND "RetentionAuthorityRevision" IS NULL AND "ConsentBindingId" IS NULL
             AND "CustodyPrincipalId" IS NULL AND "RuntimeBindingId" IS NULL AND "ApprovedPurpose" IS NULL AS "Value"
            FROM tagekyc.raw_export_authority_snapshots WHERE "Revision"=2
            """).SingleAsync());
        Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
    }

    [Theory]
    [InlineData("RuntimeBindingId")]
    [InlineData("ConsentBindingId")]
    [InlineData("RetentionAuthorityId")]
    [InlineData("ConsentPolicyId")]
    [InlineData("ControllerIdentity")]
    [InlineData("SupersededReference")]
    public async Task A3_RetainedSnapshot_InsertLineageMutationIsRejected(string field)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_snapshot_lineage");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        await Append(isolated, scope); // Same fully-valid baseline for every one-field mutation.
        var targetAcceptance = scope.Acceptance;
        long targetRevision = 2;
        if (field == "SupersededReference")
        {
            await using var withdrawal = isolated.CreateDbContext();
            await Grant(withdrawal, "SubjectConsentWithdrawer");
            await using var transaction = await withdrawal.Database.BeginTransactionAsync();
            await Actor(withdrawal, Principal);
            // A different valid acceptance avoids the duplicate-grant unique index:
            // removing the head equality must permit the stale INSERT, not merely
            // replace the expected named trigger error with another constraint.
            var artifact = Guid.NewGuid();
            await withdrawal.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.capture_artifacts
                SELECT (jsonb_populate_record(NULL::tagekyc.capture_artifacts,to_jsonb(a)||jsonb_build_object('Id',{artifact}))).*
                FROM tagekyc.capture_artifacts a WHERE a."VerificationSessionId"={scope.Session}
                """);
            targetAcceptance = await withdrawal.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_append_capture_acceptance({scope.Session},{Client},'LiveSelfieImage',{artifact},2,
                 'synthetic-challenge','synthetic-evidence','synthetic-acceptance-policy',1) AS "Value"
                """).SingleAsync();
            targetRevision = 1;
            Assert.Equal("Withdrawn", await withdrawal.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_withdraw_consent_reference(
                 {Principal},{Client},{scope.Reference},1,'source-v1','synthetic-withdrawal',{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync());
            await transaction.CommitAsync();
        }
        await using (var db = isolated.CreateDbContext())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await Actor(db, Principal);
            await PrelockRetentionRead(db, scope.Session, scope.Policy);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_deployer");
            await db.Database.ExecuteSqlRawAsync("SELECT set_config('tagekyc.raw_export_authority_snapshot_append_context','grant',true)");
            var replacement = field == "ControllerIdentity" ? "wrong-controller" : Guid.NewGuid().ToString("D");
            if (field == "SupersededReference") { field = "ControllerIdentity"; replacement = "synthetic-controller"; }
            var failure = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_authority_snapshots
                SELECT (jsonb_populate_record(NULL::tagekyc.raw_export_authority_snapshots,
                 to_jsonb(s)||jsonb_build_object('AuthoritySnapshotEventId',gen_random_uuid(),
                  'AuthoritySnapshotId',gen_random_uuid(),'Revision',{targetRevision},
                  'CaptureAcceptanceId',{targetAcceptance},{field},{replacement}))).*
                FROM tagekyc.raw_export_authority_snapshots s WHERE s."Revision"=1
                """));
            Assert.Equal("A3_RETENTION_SNAPSHOT_LINEAGE_MISMATCH", failure.MessageText);
            await tx.RollbackAsync();
        }
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
    }


    [Fact]
    public async Task A3_RetainedSnapshot_OnlyBrokerCanAppendAndHelperRemainsInternal()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_snapshot_acl");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        Assert.NotEqual(Guid.Empty, await Append(isolated, scope)); // Actual SET LOCAL ROLE broker.
        await using var db = isolated.CreateDbContext();
        foreach (var role in new[] { "tagekyc_runtime", "tagekyc_capture_runtime_application", "tagekyc_capture_runtime_authenticator",
                     "tagekyc_capture_runtime_operator", "tagekyc_raw_export_claim_broker" })
        {
            Assert.Equal(role == "tagekyc_raw_export_claim_broker", await db.Database.SqlQuery<bool>($"""
                SELECT has_function_privilege({role},
                 'tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint)','EXECUTE') AS "Value"
                """).SingleAsync());
            Assert.False(await db.Database.SqlQuery<bool>($"""
                SELECT has_function_privilege({role},
                 'tagekyc.raw_export_retained_snapshot_is_current(uuid,timestamptz)','EXECUTE') AS "Value"
                """).SingleAsync());
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task A3_RetainedSnapshot_TerminalUsesPostGrantClock(bool revoke, bool sameTransaction)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_snapshot_clock");
        await Prepare(isolated);
        var scope = await Seed(isolated);
        await using var db = isolated.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        var transactionStart = await db.Database.SqlQueryRaw<DateTimeOffset>("SELECT transaction_timestamp() AS \"Value\"").SingleAsync();
        if (sameTransaction) await AppendInTransaction(db, scope);
        else await Append(isolated, scope);
        var validFrom = await db.Database.SqlQueryRaw<DateTimeOffset>("SELECT \"ValidFromUtc\" AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync();
        Assert.True(transactionStart < validFrom, "Control must cross the old transaction-start boundary.");
        await Actor(db, Principal);
        Assert.Equal(2L, await Terminal(db, scope, revoke));
        await tx.CommitAsync();
        Assert.Equal(0, await Current(isolated, scope));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task A3_RetainedSnapshot_TerminalWaitRejectsInvisibleOrExpiredTarget(bool revoke, bool expire)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_snapshot_wait");
        await Prepare(isolated);
        var scope = await Seed(isolated, lifetimeSeconds: expire ? 8 : 300);
        if (expire) await Append(isolated, scope);
        await using var winner = isolated.CreateDbContext();
        await using var hold = await winner.Database.BeginTransactionAsync();
        if (expire)
            await winner.Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM tagekyc.verification_sessions WHERE \"Id\"={scope.Session} FOR UPDATE");
        else await AppendInTransaction(winner, scope); // Invisible uncommitted grant; holds real prefix and scope.
        var pid = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loser = RunTerminal(isolated, scope, revoke, pid);
        await using var observer = isolated.CreateDbContext();
        try
        {
            var backend = await pid.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var waiting = false;
            for (var i = 0; i < 100 && !waiting; i++)
            {
                waiting = await observer.Database.SqlQuery<bool>($"""
                    SELECT EXISTS(SELECT 1 FROM pg_locks WHERE pid={backend} AND NOT granted
                     AND (({expire} AND locktype='transactionid') OR (NOT {expire} AND locktype='advisory')))
                     AND cardinality(pg_blocking_pids({backend}))>0 AS "Value"
                    """).SingleAsync();
                if (!waiting) await Task.Delay(50);
            }
            Assert.True(waiting, "Actual PostgreSQL blocking, not elapsed-time speculation.");
            Assert.Equal(expire ? 1 : 0, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
            if (expire)
            {
                Assert.True(await observer.Database.SqlQueryRaw<bool>("SELECT clock_timestamp()<\"ValidUntilUtc\" AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
                var expired = false;
                for (var i = 0; i < 150 && !expired; i++)
                {
                    expired = await observer.Database.SqlQueryRaw<bool>("SELECT clock_timestamp()>=\"ValidUntilUtc\" AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync();
                    if (!expired) await Task.Delay(100);
                }
                Assert.True(expired);
            }
            await hold.CommitAsync();
            var failure = await Assert.ThrowsAsync<PostgresException>(() => loser);
            Assert.Equal("RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION", failure.MessageText);
            Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.raw_export_authority_snapshots").SingleAsync());
        }
        finally
        {
            if (hold.GetDbTransaction().Connection is not null) await hold.RollbackAsync();
            try { await loser.WaitAsync(TimeSpan.FromSeconds(15)); } catch (PostgresException) { }
        }
    }

    private static async Task<long> RunTerminal(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Scope scope, bool revoke, TaskCompletionSource<int> pid)
    {
        await using var db = isolated.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await Actor(db, Principal);
        pid.TrySetResult(await db.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync());
        var revision = await Terminal(db, scope, revoke);
        await tx.CommitAsync(); return revision;
    }

    private static Task<long> Terminal(TagEkycDbContext db, Scope scope, bool revoke)
    {
        var function = revoke ? "raw_export_revoke_authority_snapshot" : "raw_export_withdraw_authority_snapshot";
        return db.Database.SqlQueryRaw<long>($"""
            SELECT tagekyc.{function}(@p0,@p1,@p2,'LiveSelfieImage',1,@p3) AS "Value"
            """, Client, scope.Session, scope.Acceptance, Principal).SingleAsync();
    }

    private static async Task<Guid> Append(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, Scope scope)
    {
        await using var db = isolated.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await Actor(db, Principal);
        var result = await AppendInTransaction(db, scope);
        await tx.CommitAsync(); return result;
    }

    private static async Task<Guid> AppendInTransaction(TagEkycDbContext db, Scope scope)
    {
        await Actor(db, Principal);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
        var result = await db.Database.SqlQuery<Guid>($"""
            SELECT "AuthoritySnapshotId" AS "Value" FROM tagekyc.raw_export_append_retained_authority_snapshot(
             {scope.RuntimeBinding},{scope.Acceptance},'LiveSelfieImage',{scope.Permit},1)
            """).SingleAsync();
        await db.Database.ExecuteSqlRawAsync("RESET ROLE");
        return result;
    }

    private static async Task<int> Current(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, Scope scope)
    {
        await using var db = isolated.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await PrelockRetentionRead(db, scope.Session, scope.Policy);
        var value = await db.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_resolve_current_authority_for_source(
             {Client},{scope.Session},{scope.Acceptance},'LiveSelfieImage',clock_timestamp())
            """).SingleAsync();
        await tx.CommitAsync(); return value;
    }

    private static Task Actor(TagEkycDbContext db, Guid principal) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('tagekyc.actor_principal_id',{principal.ToString("D")},true)");

    internal static async Task<Scope> Seed(PostgresPersistenceFixture.DisposableCurrentDatabase isolated, int lifetimeSeconds = 300, bool crossBorder = false, bool rawIngress = false, string externalConsentArtifactRef = "synthetic-existing-consent", bool bootstrapRuntime = true, string rawClass = "LiveSelfieImage", Guid? clientApplicationId = null)
    {
        await using var db = isolated.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var client = clientApplicationId ?? Client;
        var session = VerificationSession.Create(client,"synthetic-subject",VerificationProfile.ChallengeBoundEkycProfile,
            "synthetic-snapshot",[RequiredCheckType.DocumentNfc],now.AddHours(1),now,challenge:"synthetic-challenge");
        await new EfVerificationSessionRepository(db).AddAsync(session);
        if (bootstrapRuntime || client != Client)
            await Grant(db, "SubjectConsentRecorder", clientApplicationId: client);
        var policy = await ApprovedRetentionPolicy(db, crossBorder);
        // Reuse the already-landed A1 fixture setup, then exercise real R20/R21;
        // no direct permit, binding or snapshot fixture INSERT.
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName,"TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName,
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = source.IndexOf("INSERT INTO tagekyc.platform_operator_credentials",StringComparison.Ordinal);
        var end = source.IndexOf("-- Publish newer heads",start,StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        await using var tx = await db.Database.BeginTransactionAsync();
        if (bootstrapRuntime)
            await db.Database.ExecuteSqlRawAsync(source[start..end].Replace("ARRAY['Configuration']",rawIngress
                ? "ARRAY['Bind','CaptureObservation','RawIngress','TrustedEvidence']"
                : "ARRAY['Bind','CaptureObservation','TrustedEvidence']",StringComparison.Ordinal));
        await Actor(db, Principal);
        var binding = await db.Database.SqlQuery<Guid>($"""
            SELECT consent_binding_id AS "Value" FROM tagekyc.raw_source_record_consent_reference(
             {Principal},{client},{session.Id},{externalConsentArtifactRef},'source-v1',0,'text-v1','source-hash',
             {now.AddMinutes(-1)},{now.AddHours(1)},{Guid.NewGuid()},{new byte[32]}) WHERE result_code='Bound'
            """).SingleAsync();
        var profile = System.Text.Json.JsonSerializer.Serialize(new {
            PolicyId=policy,PolicyVersion=1,RawClasses=new[]{"ChipDg2Portrait","LiveSelfieImage"},
            ControllerIdentity="synthetic-controller",StableDataScopeId="synthetic-scope",RetentionPolicyId="synthetic-retention",
            RetentionPolicyVersion=1,RetentionClass="synthetic-class",RevocationPolicyId="synthetic-revoke",
            PurgePolicyId="synthetic-purge",LegalHoldPolicyId="synthetic-hold",MaximumRetentionSeconds=lifetimeSeconds });
        var capability = Guid.NewGuid();
        var keyLookupPrefix = bootstrapRuntime ? "abcdefghijkl" : "mnopqrstuvwx";
        Assert.Equal("CREATED", await db.Database.SqlQuery<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
             {client},{session.Id},'Issue',NULL::uuid,NULL::bigint,{Guid.NewGuid()},{capability},{keyLookupPrefix},
             {new byte[32]},1,{new byte[32]},clock_timestamp(),{Principal},{binding},CAST({profile} AS jsonb))
            """).SingleAsync());
        var runtimeBinding = await db.Database.SqlQuery<Guid>($"""
            SELECT binding_id AS "Value" FROM tagekyc.capture_runtime_bind_capability(
             '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
             '60000000-0000-4000-8000-000000000001',1,{capability},true,{Guid.NewGuid()},{new byte[32]},clock_timestamp())
            """).SingleAsync();
        var artifact = Guid.NewGuid();
        db.Set<CaptureArtifactRow>().Add(new() {
            Id=artifact,VerificationSessionId=session.Id,
            ArtifactType=rawClass == "ChipDg2Portrait" ? "NfcDg2Portrait" : "SelfieImage",
            CaptureSource=rawClass == "ChipDg2Portrait" ? "Nfc" : "MobileSdk",
            CaptureAgentId="40000000000040008000000000000001",DeviceId="50000000000040008000000000000001",
            ArtifactHash="sha256:"+new string('a',64),MetadataHash="sha256:"+new string('b',64),QualityState="Accepted",
            RequestId="synthetic-r",CorrelationId="synthetic-c",CreatedAt=now,ExpiresAt=now.AddHours(1) });
        await db.SaveChangesAsync();
        var acceptance = await db.Database.SqlQuery<Guid>($"""
             SELECT tagekyc.raw_export_append_capture_acceptance({session.Id},{client},{rawClass},{artifact},1,
             'synthetic-challenge','synthetic-evidence','synthetic-acceptance-policy',1) AS "Value"
            """).SingleAsync();
        var permit = await db.Database.SqlQuery<Guid>($"SELECT \"RetentionAuthorityId\" AS \"Value\" FROM tagekyc.capture_capabilities WHERE \"CaptureCapabilityId\"={capability}").SingleAsync();
        var reference = await db.Database.SqlQuery<Guid>($"SELECT \"ConsentReferenceId\" AS \"Value\" FROM tagekyc.raw_source_consent_bindings WHERE \"ConsentBindingId\"={binding}").SingleAsync();
        await tx.CommitAsync();
        return new(session.Id,policy,reference,permit,runtimeBinding,acceptance,artifact);
    }

    internal static async Task<ExistingSessionRawIngressScope> SeedRawIngressForExistingSession(
        PostgresPersistenceFixture postgres,
        Guid verificationSessionId,
        Guid clientApplicationId)
    {
        await using var db = postgres.CreateDbContext();
        await Grant(db, "SubjectConsentRecorder", clientApplicationId: clientApplicationId);
        var rawIngressPolicyId = await ApprovedRetentionPolicy(db, crossBorder: false);
        var now = DateTimeOffset.UtcNow;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName,
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = source.IndexOf("INSERT INTO tagekyc.platform_operator_credentials", StringComparison.Ordinal);
        var end = source.IndexOf("-- Publish newer heads", start, StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(source[start..end].Replace(
            "ARRAY['Configuration']", "ARRAY['Bind','CaptureObservation','RawIngress','TrustedEvidence']",
            StringComparison.Ordinal));
        await Actor(db, Principal);
        var bindingJson = await db.Database.SqlQuery<string>($"""
            SELECT to_jsonb(result)::text AS "Value" FROM tagekyc.raw_source_record_consent_reference(
             {Principal},{clientApplicationId},{verificationSessionId},
             {"same-job-raw-ingress-" + Guid.NewGuid().ToString("N")},'source-v1',0,'text-v1','source-hash',
             {now.AddMinutes(-1)},{now.AddHours(1)},{Guid.NewGuid()},{new byte[32]}) AS result
            """).SingleAsync();
        using var bindingDocument = System.Text.Json.JsonDocument.Parse(bindingJson);
        Assert.Equal("Bound", bindingDocument.RootElement.GetProperty("result_code").GetString());
        var binding = bindingDocument.RootElement.GetProperty("consent_binding_id").GetGuid();
        var profile = System.Text.Json.JsonSerializer.Serialize(new
        {
            PolicyId = rawIngressPolicyId,
            PolicyVersion = 1,
            RawClasses = new[] { "ChipDg2Portrait", "LiveSelfieImage" },
            ControllerIdentity = "synthetic-controller",
            StableDataScopeId = "synthetic-scope",
            RetentionPolicyId = "synthetic-retention",
            RetentionPolicyVersion = 1,
            RetentionClass = "synthetic-class",
            RevocationPolicyId = "synthetic-revoke",
            PurgePolicyId = "synthetic-purge",
            LegalHoldPolicyId = "synthetic-hold",
            MaximumRetentionSeconds = 300
        });
        var capability = Guid.NewGuid();
        Assert.Equal("CREATED", await db.Database.SqlQuery<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
             {clientApplicationId},{verificationSessionId},'Issue',NULL::uuid,NULL::bigint,
             {Guid.NewGuid()},{capability},'abcdefghijkl',{new byte[32]},1,{new byte[32]},
             clock_timestamp(),{Principal},{binding},CAST({profile} AS jsonb))
            """).SingleAsync());
        var runtimeBinding = await db.Database.SqlQuery<Guid>($"""
            SELECT binding_id AS "Value" FROM tagekyc.capture_runtime_bind_capability(
             '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
             '60000000-0000-4000-8000-000000000001',1,{capability},true,{Guid.NewGuid()},
             {new byte[32]},clock_timestamp())
            """).SingleAsync();
        var executionBinding = await db.Set<CaptureExecutionBindingsRow>()
            .AsNoTracking()
            .SingleAsync(row => row.CaptureExecutionBindingId == runtimeBinding);
        var sessionChallengeHash = await db.Database.SqlQuery<string>($"""
            SELECT "BindingNonceHash" AS "Value"
            FROM tagekyc.verification_sessions
            WHERE "Id" = {verificationSessionId}
            """).SingleAsync();

        async Task<ExistingSessionRawIngressClass> AddClass(string rawClass)
        {
            var artifact = Guid.NewGuid();
            db.Set<CaptureArtifactRow>().Add(new()
            {
                Id = artifact,
                VerificationSessionId = verificationSessionId,
                ArtifactType = rawClass == "ChipDg2Portrait" ? "NfcDg2Portrait" : "SelfieImage",
                CaptureSource = rawClass == "ChipDg2Portrait" ? "Nfc" : "MobileSdk",
                CaptureAgentId = "40000000000040008000000000000001",
                DeviceId = "50000000000040008000000000000001",
                ArtifactHash = "sha256:" + new string('a', 64),
                MetadataHash = "sha256:" + new string('b', 64),
                QualityState = "Accepted",
                RequestId = "same-job-raw-ingress",
                CorrelationId = "same-job-raw-ingress",
                CreatedAt = now,
                ExpiresAt = now.AddHours(1)
            });
            await db.SaveChangesAsync();
            var acceptance = await db.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_append_capture_acceptance(
                 {verificationSessionId},{clientApplicationId},{rawClass},{artifact},1,
                 {sessionChallengeHash},'same-job-raw-ingress-evidence','synthetic-acceptance-policy',1) AS "Value"
                """).SingleAsync();
            return new(rawClass, artifact, acceptance);
        }

        var dg2 = await AddClass("ChipDg2Portrait");
        var selfie = await AddClass("LiveSelfieImage");
        await tx.CommitAsync();
        return new(
            Principal,
            rawIngressPolicyId,
            runtimeBinding,
            executionBinding.RolePolicyId,
            executionBinding.RolePolicyRevision,
            executionBinding.RuntimeRevision,
            executionBinding.InstallationRevision,
            executionBinding.CredentialRevision,
            executionBinding.PublicKeyThumbprint,
            dg2,
            selfie);
    }

    internal sealed record ExistingSessionRawIngressClass(
        string RawClass,
        Guid CaptureArtifactId,
        Guid CaptureAcceptanceId);

    internal sealed record ExistingSessionRawIngressScope(
        Guid ActorPrincipalId,
        Guid RawIngressPolicyId,
        Guid RuntimeBindingId,
        Guid RolePolicyId,
        long RolePolicyRevision,
        long RuntimeRevision,
        long InstallationRevision,
        long CredentialRevision,
        byte[] PublicKeyThumbprint,
        ExistingSessionRawIngressClass ChipDg2Portrait,
        ExistingSessionRawIngressClass LiveSelfieImage);

    // Test preconditions for the E01 issue-first race: reuse the permit minted by
    // its real HTTP Issue, bind that exact capability, then prepare an actual R1
    // candidate while the reference is still current. The returned action does
    // not run R1 until the caller commits the competing withdrawal.
    internal static async Task<Func<Task<string>>> PrepareR1ForIssuedPermit(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Guid session, Guid policy, Guid reference)
    {
        await using var db = isolated.CreateDbContext();
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName,
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = source.IndexOf("INSERT INTO tagekyc.platform_operator_credentials", StringComparison.Ordinal);
        var end = source.IndexOf("-- Publish newer heads", start, StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        Guid runtimeBinding;
        Guid acceptance;
        Guid artifact;
        Guid permit;
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync(source[start..end].Replace(
                "ARRAY['Configuration']", "ARRAY['Bind','CaptureObservation','RawIngress','TrustedEvidence']",
                StringComparison.Ordinal));
            var capability = await db.Database.SqlQuery<Guid>($"""
                SELECT "CaptureCapabilityId" AS "Value" FROM tagekyc.capture_capabilities
                WHERE "VerificationSessionId"={session}
                """).SingleAsync();
            permit = await db.Database.SqlQuery<Guid>($"""
                SELECT "RetentionAuthorityId" AS "Value" FROM tagekyc.capture_capabilities
                WHERE "CaptureCapabilityId"={capability}
                """).SingleAsync();
            await Actor(db, Principal);
            runtimeBinding = await db.Database.SqlQuery<Guid>($"""
                SELECT binding_id AS "Value" FROM tagekyc.capture_runtime_bind_capability(
                 '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
                 '60000000-0000-4000-8000-000000000001',1,{capability},true,{Guid.NewGuid()},
                 {new byte[32]},clock_timestamp()) WHERE result_code='CREATED'
                """).SingleAsync();
            artifact = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            db.Set<CaptureArtifactRow>().Add(new()
            {
                Id = artifact, VerificationSessionId = session,
                ArtifactType = "SelfieImage", CaptureSource = "MobileSdk",
                CaptureAgentId = "40000000000040008000000000000001",
                DeviceId = "50000000000040008000000000000001",
                ArtifactHash = "sha256:" + new string('a', 64),
                MetadataHash = "sha256:" + new string('b', 64),
                QualityState = "Accepted", RequestId = "synthetic-e01-r1",
                CorrelationId = "synthetic-e01-r1", CreatedAt = now,
                ExpiresAt = now.AddHours(1)
            });
            await db.SaveChangesAsync();
            acceptance = await db.Database.SqlQuery<Guid>($"""
                SELECT tagekyc.raw_export_append_capture_acceptance(
                 {session},{Client},'LiveSelfieImage',{artifact},1,
                 'synthetic-challenge','synthetic-e01-r1-evidence','synthetic-acceptance-policy',1) AS "Value"
                """).SingleAsync();
            await tx.CommitAsync();
        }
        var candidate = await BeginCandidate(isolated,
            new Scope(session, policy, reference, permit, runtimeBinding, acceptance, artifact));
        return async () =>
        {
            await using var r1 = isolated.CreateDbContext();
            await using var tx = await r1.Database.BeginTransactionAsync();
            var outcome = (await CompleteCandidate(r1, candidate)).OutcomeCode;
            await tx.CommitAsync();
            return outcome;
        };
    }

    private static async Task WithdrawReference(TagEkycDbContext db, Scope scope)
    {
        await Actor(db, Principal);
        Assert.Equal("Withdrawn", await db.Database.SqlQuery<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.raw_source_withdraw_consent_reference(
             {Principal},{Client},{scope.Reference},1,'source-v1','synthetic-withdrawal',{Guid.NewGuid()},{new byte[32]})
            """).SingleAsync());
    }

    private static async Task<Candidate> BeginCandidate(PostgresPersistenceFixture.DisposableCurrentDatabase isolated,
        Scope scope, Candidate? retry = null, string rawClass = "LiveSelfieImage")
    {
        await using var db = isolated.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await Actor(db, Principal);
        var now = retry?.Now ?? await db.Database.SqlQueryRaw<DateTimeOffset>("SELECT clock_timestamp() AS \"Value\"").SingleAsync();
        var ingress = retry?.Ingress ?? Guid.NewGuid().ToString("N");
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
        var mediaType = rawClass == "ChipDg2Portrait" ? "image/jp2" : "image/jpeg";
        var token = await db.Database.SqlQuery<BeginToken>($"""
            SELECT claim_evaluation_id AS "Id",claim_evaluation_revision AS "Revision",claim_evaluation_fence AS "Fence",
             token_variant AS "Variant",token_expires_at_utc AS "ExpiresAt",claim_evaluation_token AS "Token",
             producer_envelope_fingerprint AS "Envelope"
            FROM tagekyc.raw_export_begin_retained_source_ingress_with_authority(
             {Principal},{Client},'40000000000040008000000000000001','50000000000040008000000000000001',
             {ingress},{scope.Session},{scope.Acceptance},{scope.Artifact},1,{rawClass},'synthetic-challenge',
             24::bigint,{mediaType},{now.AddSeconds(-5)},{now.AddSeconds(-4)},
             {now.AddMinutes(5)},300,'fixture-content-commitment',1,{Guid.NewGuid()},300,100,{scope.Permit},1::bigint)
            """).SingleAsync();
        Assert.Equal(retry is null ? "NewClaimEvaluationToken" : "ExistingClaimComparisonToken", token.Variant);
        Assert.Equal(32, token.Envelope.Length);
        await tx.CommitAsync();
        return new(ingress, now, token, token.Envelope, mediaType);
    }

    private static async Task<Completion> CompleteCandidate(TagEkycDbContext db, Candidate candidate, byte[]? commitment = null)
    {
        await Actor(db, Principal);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_raw_export_claim_broker");
        var profile = new FixtureSourceEncryptionProfileCatalog().GetActive();
        var key = new FixtureKekReferenceCatalog().GetActive();
        var result = await db.Database.SqlQuery<Completion>($"""
            SELECT * FROM tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(
             {Client},'40000000000040008000000000000001','50000000000040008000000000000001',{candidate.Ingress},
             {candidate.Token.Id},{candidate.Token.Revision},{candidate.Token.Fence},{candidate.Token.Variant},
             {candidate.Token.ExpiresAt},{candidate.Token.Token},{candidate.Envelope},1,'fixture-content-commitment',1,{commitment ?? new byte[32]},
             1,'fixture-subject-token',1,{new byte[32]},24::bigint,{candidate.MediaType},
             {candidate.Now.AddSeconds(-5)},{candidate.Now.AddSeconds(-4)},{candidate.Now.AddMinutes(5)},300,
             {profile.StorageProfileId},{profile.SourceEncryptionProfileId},{profile.SourceEncryptionProfileVersion},
             {profile.EncryptionSuiteId},{profile.EncryptionFramingVersion},{profile.NonceStrategyId},
             {C1HashCanonical.ComputeNonceSeedCommitment(profile.NonceStrategyId)},{profile.ChunkSize},
             {C1HashCanonical.ComputeFramingParametersDigest(profile.EncryptionSuiteId,profile.EncryptionFramingVersion,profile.ChunkSize,profile.NonceStrategyId)},
             {key.KeyProviderId},{key.KekId},{key.KekVersion},{key.KekFingerprint},300,5,100,30)
            """).SingleAsync();
        await db.Database.ExecuteSqlRawAsync("RESET ROLE");
        return result;
    }

    private sealed class A3AssemblyConnectionFactory(string connectionString) : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RawExportAssemblyDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability switch
            {
                RawExportAssemblyDatabaseCapability.Resolver => "tagekyc_raw_export_assembly_resolver",
                RawExportAssemblyDatabaseCapability.Sealer => "tagekyc_raw_export_assembly_sealer",
                _ => throw new ArgumentOutOfRangeException(nameof(capability)),
            };
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Options = $"-c role={role}",
                Pooling = false,
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class A3RecipientPackageConnectionFactory(string connectionString) : IRecipientPackageConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RecipientPackageDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability switch
            {
                RecipientPackageDatabaseCapability.Preparer => "tagekyc_raw_export_package_preparer",
                RecipientPackageDatabaseCapability.Reconciler => "tagekyc_raw_export_package_reconciler",
                RecipientPackageDatabaseCapability.Lifecycle => "tagekyc_raw_export_package_lifecycle",
                _ => throw new ArgumentOutOfRangeException(nameof(capability)),
            };
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Options = $"-c role={role}",
                Pooling = false,
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class A3DeliveryConnectionFactory(string connectionString) : IRecipientPackageDeliveryConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var role = new NpgsqlCommand("SET ROLE tagekyc_raw_export_package_delivery", connection);
            await role.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class A3AssemblyAuthenticator : IRawExportAssemblyAuthenticationProvider
    {
        private static readonly byte[] Key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        public string KeyId => "fixture-assembly-authentication";
        public int KeyVersion => 1;
        public Task<byte[]> AuthenticateManifestAsync(
            ReadOnlyMemory<byte> authenticationPayload,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(HMACSHA256.HashData(Key, authenticationPayload.Span));
        }
    }

    private sealed class A3AssemblyPreparationProvider(Func<Task>? afterPrepare) : IC2AssemblyPreparationProvider
    {
        private Guid preparationId;
        private byte[]? fingerprint;
        private byte[]? receipt;
        private bool prepared;
        internal int PrepareCount { get; private set; }
        internal bool Finalized { get; private set; }

        public async Task<C2AssemblyPrepareResult> PrepareAsync(
            C2AssemblyPreparationRequest request,
            Func<Stream, CancellationToken, Task> boundedAssemblyWriter,
            CancellationToken cancellationToken)
        {
            if (prepared)
                return preparationId == request.C2PreparationId && Fixed(fingerprint, request.AssemblyFingerprint)
                    ? new(C2AssemblyPrepareOutcome.ExistingMatch, receipt?.ToArray())
                    : new(C2AssemblyPrepareOutcome.Conflict, null);
            PrepareCount++;
            await using var sink = new MemoryStream();
            await boundedAssemblyWriter(sink, cancellationToken);
            if (sink.Length != request.CompleteAssemblyLength)
                return new(C2AssemblyPrepareOutcome.Conflict, null);
            preparationId = request.C2PreparationId;
            fingerprint = request.AssemblyFingerprint.ToArray();
            receipt = SHA256.HashData(sink.GetBuffer().AsSpan(0, checked((int)sink.Length)));
            prepared = true;
            if (afterPrepare is not null) await afterPrepare();
            return new(C2AssemblyPrepareOutcome.Prepared, receipt.ToArray());
        }

        public Task<C2AssemblyInspection> GetPreparationAsync(
            Guid c2PreparationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(!prepared
                ? new C2AssemblyInspection(C2AssemblyInspectionOutcome.Missing, null, null)
                : c2PreparationId != preparationId
                    ? new C2AssemblyInspection(C2AssemblyInspectionOutcome.Conflict, null, null)
                    : new C2AssemblyInspection(
                        Finalized ? C2AssemblyInspectionOutcome.Finalized : C2AssemblyInspectionOutcome.Prepared,
                        fingerprint?.ToArray(), receipt?.ToArray()));

        public Task<C2AssemblyFinalizeResult> FinalizeAsync(
            Guid c2PreparationId,
            byte[] assemblyFingerprint,
            CancellationToken cancellationToken)
        {
            if (!prepared || c2PreparationId != preparationId || !Fixed(fingerprint, assemblyFingerprint))
                return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.Conflict));
            if (Finalized)
                return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.ExistingMatch));
            Finalized = true;
            return Task.FromResult(new C2AssemblyFinalizeResult(C2AssemblyFinalizeOutcome.Finalized));
        }

        public Task<C2AssemblyAbortResult> AbortAsync(
            Guid c2PreparationId,
            byte[] authorizationDigest,
            CancellationToken cancellationToken) =>
            Task.FromResult(new C2AssemblyAbortResult(C2AssemblyAbortOutcome.Conflict));

        private static bool Fixed(byte[]? left, byte[] right) =>
            left is not null && left.Length == right.Length &&
            CryptographicOperations.FixedTimeEquals(left, right);
    }

    private sealed record A3AssemblyResult(
        RawExportAssemblyExecutionResult Result,
        int PrepareCount,
        bool Finalized);
    private sealed record A3FinalizedPackage(Guid PackageId, Guid RecipientId, Guid PrincipalId);
    private sealed record C1RaceFixture(Scope Scope, Guid JobId, Guid AttemptId, long Revision, long Fence);
    private sealed record C3RaceFixture(
        Scope Scope, Guid DeliveryId, Guid RecipientId, Guid ApiKeyId,
        Guid PrincipalId, byte[] CorrelationDigest);
    internal sealed record Scope(Guid Session,Guid Policy,Guid Reference,Guid Permit,Guid RuntimeBinding,Guid Acceptance,Guid Artifact);

    private sealed class CountingDeliveryReader : IRecipientPackageDeliveryReader
    {
        public int OpenCount { get; private set; }

        public Task<RecipientPackageDeliveryReadResult> OpenExactAsync(
            RecipientPackageDeliveryLocator locator, CancellationToken cancellationToken)
        {
            OpenCount++;
            return Task.FromResult(new RecipientPackageDeliveryReadResult(
                RecipientPackageDeliveryReadOutcome.Unavailable, null));
        }
    }
    private sealed class C1FreezeResult
    {
        public string Outcome { get; set; } = "";
        public int BindingCount { get; set; }
    }
    private sealed record Candidate(string Ingress,DateTimeOffset Now,BeginToken Token,byte[] Envelope,string MediaType);
    private sealed class BeginToken
    {
        public Guid Id { get; set; }
        public long Revision { get; set; }
        public long Fence { get; set; }
        public string Variant { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
        public string Token { get; set; } = "";
        public byte[] Envelope { get; set; } = [];
    }
    private sealed class Completion
    {
        public string OutcomeCode { get; set; } = "";
        public Guid? SourceArtifactId { get; set; }
        public Guid? AttemptKeyReservationId { get; set; }
        public Guid? AttemptId { get; set; }
        public long? ExpectedEncryptionAttemptRevision { get; set; }
        public long? ExpectedFence { get; set; }
    }
}
