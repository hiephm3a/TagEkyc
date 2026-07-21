using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

#pragma warning disable EF1002

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B2SubjectExportConsentTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private static readonly Guid AdminPrincipal = Guid.Parse("88b20000-0000-5000-8000-000000000001");
    private static readonly Guid RecorderPrincipal = Guid.Parse("88b20000-0000-5000-8000-000000000002");
    private static readonly Guid WithdrawerPrincipal = Guid.Parse("88b20000-0000-5000-8000-000000000003");
    private static readonly Guid ClientApplicationId = Guid.Parse("88b20000-0000-5000-8000-000000000010");

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Session_state_matrix_grant_requires_completed_and_withdraw_survives_terminal_transitions()
    {
        await using (var authorityDb = postgres.CreateDbContext())
        {
            await GrantAuthoritiesAsync(authorityDb);
        }

        foreach (var state in Enum.GetValues<VerificationSessionState>())
        {
            await using var db = postgres.CreateDbContext();
            var policyId = Guid.Parse($"88b20000-0000-5000-8000-00000001{(int)state:0000}");
            await SeedPolicyAsync(db, policyId);
            var sessionId = await SeedSessionAsync(db, state, $"subject-{state}");
            var repository = new EfRawExportSubjectConsentRepository(db);
            var command = GrantCommand(sessionId, policyId);

            if (state == VerificationSessionState.Completed)
            {
                var snapshot = await repository.RecordSubjectConsentGrantedAsync(command);
                Assert.Equal(RawExportSubjectConsentState.Effective, snapshot.State);
                Assert.Equal([RawExportRawClass.LiveSelfieImage], snapshot.ConsentedRawClasses);
            }
            else
            {
                var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                    repository.RecordSubjectConsentGrantedAsync(command));
                Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_SESSION_NOT_COMPLETED", exception.MessageText, StringComparison.Ordinal);
            }
        }

        foreach (var terminal in new[]
                 {
                     VerificationSessionState.Expired,
                     VerificationSessionState.Cancelled,
                     VerificationSessionState.TechnicalTerminal,
                 })
        {
            await using var db = postgres.CreateDbContext();
            var policyId = Guid.Parse($"88b20000-0000-5000-8000-00000002{(int)terminal:0000}");
            await SeedPolicyAsync(db, policyId);
            var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, $"withdraw-{terminal}");
            var repository = new EfRawExportSubjectConsentRepository(db);
            await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
            await new EfVerificationSessionRepository(db).SetStateAsync(sessionId, terminal);

            var withdrawn = await repository.RecordSubjectConsentWithdrawnAsync(
                new RawExportSubjectConsentWithdrawCommand(
                    WithdrawerPrincipal,
                    sessionId,
                    policyId,
                    1,
                    ExpectedRevision: 1,
                    TargetRevision: 1,
                    "decision:withdraw"));

            Assert.Equal(RawExportSubjectConsentState.NonEffective, withdrawn.State);
            Assert.Equal(RawExportSubjectConsentCause.Withdrawn, withdrawn.Cause);
        }
    }

    [Fact]
    public async Task Latest_event_overall_no_fallback_for_consent_and_authority()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000030");
        await SeedPolicyAsync(db, policyId);
        await GrantAuthoritiesAsync(db);
        var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, "no-fallback");
        var repository = new EfRawExportSubjectConsentRepository(db);

        await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
        await repository.RecordSubjectConsentGrantedAsync(GrantCommand(
            sessionId,
            policyId,
            validUntil: DateTimeOffset.UtcNow.AddSeconds(2)));
        await Task.Delay(TimeSpan.FromSeconds(3));
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var snapshot = await repository.ResolveSubjectExportConsentForAuthorizationAsync(sessionId, policyId, 1);
            Assert.Equal(RawExportSubjectConsentState.NonEffective, snapshot.State);
            Assert.Equal(RawExportSubjectConsentCause.Expired, snapshot.Cause);
            Assert.Equal(2, snapshot.ConsentRef?.Revision);
            await tx.CommitAsync();
        }

        var newClient = Guid.Parse("88b20000-0000-5000-8000-000000000031");
        await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            RecorderPrincipal,
            newClient,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            0,
            "decision:authority-long"));
        await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            RecorderPrincipal,
            newClient,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            1,
            "decision:authority-short",
            ValidUntilUtc: DateTimeOffset.UtcNow.AddSeconds(2)));
        await Task.Delay(TimeSpan.FromSeconds(3));
        var authorityPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000032");
        await SeedPolicyAsync(db, authorityPolicy);
        var authoritySession = await SeedSessionAsync(db, VerificationSessionState.Completed, "authority-expired", newClient);

        var denied = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.RecordSubjectConsentGrantedAsync(GrantCommand(authoritySession, authorityPolicy)));
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED", denied.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stale_target_revision_conflicts_even_when_session_is_not_completed()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000040");
        await SeedPolicyAsync(db, policyId);
        await GrantAuthoritiesAsync(db);
        var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, "stale-target");
        var repository = new EfRawExportSubjectConsentRepository(db);
        await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
        await new EfVerificationSessionRepository(db).SetStateAsync(sessionId, VerificationSessionState.Expired);

        var stale = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.RecordSubjectConsentWithdrawnAsync(new RawExportSubjectConsentWithdrawCommand(
                WithdrawerPrincipal,
                sessionId,
                policyId,
                1,
                ExpectedRevision: 1,
                TargetRevision: 0,
                "decision:stale")));
        Assert.Contains("RAW_EXPORT_CONSENT_STALE_TARGET_REVISION", stale.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolver_requires_ambient_transaction_and_holds_session_row_lock()
    {
        await using var setup = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000050");
        await SeedPolicyAsync(setup, policyId);
        await GrantAuthoritiesAsync(setup);
        var sessionId = await SeedSessionAsync(setup, VerificationSessionState.Completed, "resolver-lock");
        var repository = new EfRawExportSubjectConsentRepository(setup);
        await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.ResolveSubjectExportConsentForAuthorizationAsync(sessionId, policyId, 1));

        await using var resolverDb = postgres.CreateDbContext();
        await using var resolverTx = await resolverDb.Database.BeginTransactionAsync();
        var resolverRepository = new EfRawExportSubjectConsentRepository(resolverDb);
        var snapshot = await resolverRepository.ResolveSubjectExportConsentForAuthorizationAsync(sessionId, policyId, 1);
        Assert.Equal(RawExportSubjectConsentState.Effective, snapshot.State);

        await using var stateDb = postgres.CreateDbContext();
        var stateTask = new EfVerificationSessionRepository(stateDb)
            .SetStateAsync(sessionId, VerificationSessionState.Expired);
        await Task.Delay(200);
        Assert.False(stateTask.IsCompleted);

        await resolverTx.CommitAsync();
        await stateTask;
    }

    [Fact]
    public async Task Resolver_shared_lock_blocks_withdraw_until_authorization_transaction_finishes()
    {
        await using var setup = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000060");
        await SeedPolicyAsync(setup, policyId);
        await GrantAuthoritiesAsync(setup);
        var sessionId = await SeedSessionAsync(setup, VerificationSessionState.Completed, "resolver-withdraw");
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));

        await using var resolverDb = postgres.CreateDbContext();
        await using var resolverTx = await resolverDb.Database.BeginTransactionAsync();
        await new EfRawExportSubjectConsentRepository(resolverDb)
            .ResolveSubjectExportConsentForAuthorizationAsync(sessionId, policyId, 1);

        var withdrawTask = WithdrawAsRuntimeInSeparateConnectionAsync(sessionId, policyId, 1, 1);
        await Task.Delay(200);
        Assert.False(withdrawTask.IsCompleted);

        await resolverTx.CommitAsync();
        var result = await withdrawTask;
        Assert.True(result.Succeeded, result.Message);
    }

    [Fact]
    public async Task Resolver_blocks_on_shared_consent_scope_lock_without_session_row_lock()
    {
        await using var setup = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000068");
        var subjectRef = "resolver-scope-lock";
        await SeedPolicyAsync(setup, policyId);
        await GrantAuthoritiesAsync(setup);
        var sessionId = await SeedSessionAsync(setup, VerificationSessionState.Completed, subjectRef);
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
        var key = ConsentScopeLockKey(sessionId, subjectRef, policyId);

        await using var blockerConnection = new NpgsqlConnection(postgres.ConnectionString);
        await blockerConnection.OpenAsync();
        await using var blockerTx = await blockerConnection.BeginTransactionAsync();
        await TakeConsentScopeLockAsync(blockerConnection, blockerTx, key, shared: false);
        await AssertAdvisoryLockHeldAsync(blockerConnection, blockerTx, key, "ExclusiveLock");

        var resolverTask = ResolveConsentAsRuntimeInSeparateConnectionAsync(sessionId, policyId);
        await AssertAdvisoryLockWaiterAsync(key);
        Assert.False(resolverTask.IsCompleted);

        await blockerTx.CommitAsync();
        var snapshot = await resolverTask;
        Assert.Equal(RawExportSubjectConsentState.Effective, snapshot.State);
        Assert.Equal([RawExportRawClass.LiveSelfieImage], snapshot.ConsentedRawClasses);
    }

    [Fact]
    public async Task Runtime_withdraw_blocks_on_shared_consent_scope_lock_without_session_row_lock()
    {
        await using var setup = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000064");
        var subjectRef = "runtime-scope-lock-withdraw";
        await SeedPolicyAsync(setup, policyId);
        await GrantAuthoritiesAsync(setup);
        var sessionId = await SeedSessionAsync(setup, VerificationSessionState.Completed, subjectRef);
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
        var key = ConsentScopeLockKey(sessionId, subjectRef, policyId);

        await using var blockerConnection = new NpgsqlConnection(postgres.ConnectionString);
        await blockerConnection.OpenAsync();
        await using var blockerTx = await blockerConnection.BeginTransactionAsync();
        await TakeConsentScopeLockAsync(blockerConnection, blockerTx, key, shared: true);
        await AssertAdvisoryLockHeldAsync(blockerConnection, blockerTx, key, "ShareLock");

        var withdrawTask = WithdrawAsRuntimeInSeparateConnectionAsync(sessionId, policyId, 1, 1);
        await AssertAdvisoryLockWaiterAsync(key);
        Assert.False(withdrawTask.IsCompleted);

        await blockerTx.CommitAsync();
        var result = await withdrawTask;
        Assert.True(result.Succeeded, result.Message);
    }

    [Fact]
    public async Task Normal_grant_and_privileged_direct_insert_block_on_exclusive_consent_scope_lock_without_session_row_lock()
    {
        await using var setup = postgres.CreateDbContext();
        await GrantAuthoritiesAsync(setup);

        var appendPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000065");
        var appendSubject = "scope-lock-append";
        await SeedPolicyAsync(setup, appendPolicy);
        var appendSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, appendSubject);
        var appendKey = ConsentScopeLockKey(appendSession, appendSubject, appendPolicy);

        await using (var blockerConnection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await blockerConnection.OpenAsync();
            await using var blockerTx = await blockerConnection.BeginTransactionAsync();
            await TakeConsentScopeLockAsync(blockerConnection, blockerTx, appendKey, shared: false);
            await AssertAdvisoryLockHeldAsync(blockerConnection, blockerTx, appendKey, "ExclusiveLock");

            var appendTask = RecordGrantAsRuntimeInSeparateConnectionAsync(appendSession, appendPolicy);
            await AssertAdvisoryLockWaiterAsync(appendKey);
            Assert.False(appendTask.IsCompleted);

            await blockerTx.CommitAsync();
            var append = await appendTask;
            Assert.True(append.Succeeded, append.Message);
        }

        var directPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000066");
        var directSubject = "scope-lock-direct";
        await SeedPolicyAsync(setup, directPolicy);
        var directSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, directSubject);
        var directKey = ConsentScopeLockKey(directSession, directSubject, directPolicy);

        await using var directBlockerConnection = new NpgsqlConnection(postgres.ConnectionString);
        await directBlockerConnection.OpenAsync();
        await using var directBlockerTx = await directBlockerConnection.BeginTransactionAsync();
        await TakeConsentScopeLockAsync(directBlockerConnection, directBlockerTx, directKey, shared: false);
        await AssertAdvisoryLockHeldAsync(directBlockerConnection, directBlockerTx, directKey, "ExclusiveLock");

        var directTask = DirectGrantInsertInSeparateConnectionAsync(directSession, directPolicy, directSubject, 1);
        await AssertAdvisoryLockWaiterAsync(directKey);
        Assert.False(directTask.IsCompleted);

        await directBlockerTx.CommitAsync();
        var direct = await directTask;
        Assert.False(direct.Succeeded);
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED", direct.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Consent_record_shared_authority_lock_blocks_authority_revoke_until_commit()
    {
        await using var setup = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000061");
        await SeedPolicyAsync(setup, policyId);
        await GrantAuthoritiesAsync(setup);
        var sessionId = await SeedSessionAsync(setup, VerificationSessionState.Completed, "record-authority-race");

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{RecorderPrincipal:D}';");
        await ExecuteScalarAsync(connection, transaction, $"""
            SELECT tagekyc.raw_export_append_subject_consent_granted(
                '{sessionId}', '{policyId}', 1, ARRAY['LiveSelfieImage'],
                'consent-text:v1','sha256:consent-text','external-consent:race','decision:grant-race', NULL);
            """);

        var revokeTask = RevokeAuthorityInSeparateConnectionAsync(
            RecorderPrincipal,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            expectedRevision: 1,
            targetRevision: 1);
        await Task.Delay(200);
        Assert.False(revokeTask.IsCompleted);

        await transaction.CommitAsync();
        var revoke = await revokeTask;
        Assert.True(revoke.Succeeded, revoke.Message);
    }

    [Fact]
    public async Task Consent_withdraw_shared_authority_lock_blocks_authority_revoke_until_commit()
    {
        await using var setup = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000062");
        await SeedPolicyAsync(setup, policyId);
        await GrantAuthoritiesAsync(setup);
        var sessionId = await SeedSessionAsync(setup, VerificationSessionState.Completed, "withdraw-authority-race");
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{WithdrawerPrincipal:D}';");
        await ExecuteScalarAsync(connection, transaction, $"""
            SELECT tagekyc.raw_export_append_subject_consent_withdrawn(
                '{sessionId}', '{policyId}', 1, 1, 1, 'decision:withdraw-race', NULL);
            """);

        var revokeTask = RevokeAuthorityInSeparateConnectionAsync(
            WithdrawerPrincipal,
            RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
            expectedRevision: 1,
            targetRevision: 1);
        await Task.Delay(200);
        Assert.False(revokeTask.IsCompleted);

        await transaction.CommitAsync();
        var revoke = await revokeTask;
        Assert.True(revoke.Succeeded, revoke.Message);
    }

    [Fact]
    public async Task Runtime_role_positive_matrix_uses_only_high_level_surfaces()
    {
        await using var setup = postgres.CreateDbContext();
        await GrantAuthoritiesAsync(setup);

        var grantPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000071");
        await SeedPolicyAsync(setup, grantPolicy);
        var grantSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, "runtime-grant");

        await using (var runtimeDb = postgres.CreateDbContext())
        {
            await UseRuntimeRoleAsync(runtimeDb);
            var snapshot = await new EfRawExportSubjectConsentRepository(runtimeDb)
                .RecordSubjectConsentGrantedAsync(GrantCommand(grantSession, grantPolicy, decisionRef: null));
            Assert.Equal(RawExportSubjectConsentState.Effective, snapshot.State);
            Assert.Null(snapshot.DecisionRef);
        }

        var resolverPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000072");
        await SeedPolicyAsync(setup, resolverPolicy);
        var resolverSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, "runtime-resolver");
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(resolverSession, resolverPolicy));
        await using (var runtimeDb = postgres.CreateDbContext())
        {
            await UseRuntimeRoleAsync(runtimeDb);
            await using var resolverTx = await runtimeDb.Database.BeginTransactionAsync();
            var snapshot = await new EfRawExportSubjectConsentRepository(runtimeDb)
                .ResolveSubjectExportConsentForAuthorizationAsync(resolverSession, resolverPolicy, 1);
            Assert.Equal(RawExportSubjectConsentState.Effective, snapshot.State);

            await using var stateDb = postgres.CreateDbContext();
            var stateTask = new EfVerificationSessionRepository(stateDb)
                .SetStateAsync(resolverSession, VerificationSessionState.Expired);
            await Task.Delay(200);
            Assert.False(stateTask.IsCompleted);

            await resolverTx.CommitAsync();
            await stateTask;
        }

        var withdrawPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000073");
        await SeedPolicyAsync(setup, withdrawPolicy);
        var withdrawSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, "runtime-withdraw");
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(withdrawSession, withdrawPolicy));
        await using (var runtimeDb = postgres.CreateDbContext())
        {
            await UseRuntimeRoleAsync(runtimeDb);
            var withdrawn = await new EfRawExportSubjectConsentRepository(runtimeDb)
                .RecordSubjectConsentWithdrawnAsync(new RawExportSubjectConsentWithdrawCommand(
                    WithdrawerPrincipal,
                    withdrawSession,
                    withdrawPolicy,
                    1,
                    ExpectedRevision: 1,
                    TargetRevision: 1,
                    DecisionRef: null));
            Assert.Equal(RawExportSubjectConsentState.NonEffective, withdrawn.State);
            Assert.Equal(RawExportSubjectConsentCause.Withdrawn, withdrawn.Cause);
        }

        await using var deniedConnection = new NpgsqlConnection(postgres.ConnectionString);
        await deniedConnection.OpenAsync();
        await ExecuteScalarAsync(deniedConnection, null, "GRANT tagekyc_runtime TO tagekyc; SET ROLE tagekyc_runtime;");
        await AssertB2RuntimeTablePrivilegesAsync(deniedConnection);
        foreach (var sql in new[]
                 {
                     """SELECT count(*) FROM tagekyc.raw_export_policy_versions;""",
                     """SELECT tagekyc.raw_export_consent_scope_hash('88b20000-0000-5000-8000-000000000073','runtime-withdraw','88b20000-0000-5000-8000-000000000073',1,'SubjectRawBiometricExport','88b20000-0000-5000-8000-000000000010');""",
                     """SELECT tagekyc.raw_export_consent_lock_key(decode('0000000000000000','hex'));""",
                     """SELECT * FROM tagekyc.raw_export_lock_verification_session_for_subject_consent('88b20000-0000-5000-8000-000000000073');""",
                     """SELECT tagekyc.raw_export_append_subject_consent_authority('88b20000-0000-5000-8000-000000000002','88b20000-0000-5000-8000-000000000010','SubjectConsentRecorder',1,'Revoked',1,NULL,NULL);""",
                     """INSERT INTO tagekyc.raw_export_subject_consent_events ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId","Revision","EventType","TargetRevision","WithdrawnByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(), decode('0000000000000000000000000000000000000000000000000000000000000000','hex'), '88b20000-0000-5000-8000-000000000073', 'runtime-withdraw', '88b20000-0000-5000-8000-000000000073', 1, 'SubjectRawBiometricExport', '88b20000-0000-5000-8000-000000000010', 99, 'Withdrawn', 1, '88b20000-0000-5000-8000-000000000003', transaction_timestamp());""",
                     """UPDATE tagekyc.raw_export_subject_consent_events SET "Revision" = "Revision";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_events;""",
                     """TRUNCATE tagekyc.raw_export_subject_consent_events;""",
                     """INSERT INTO tagekyc.raw_export_subject_consent_authorities ("AuthorityEventId","AuthorityPrincipalId","ClientApplicationId","AuthorityType","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(),'88b20000-0000-5000-8000-000000000002','88b20000-0000-5000-8000-000000000010','SubjectConsentRecorder',99,'Granted','decision:runtime-direct','88b20000-0000-5000-8000-000000000001',transaction_timestamp());""",
                     """UPDATE tagekyc.raw_export_subject_consent_authorities SET "Revision" = "Revision";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_authorities;""",
                     """TRUNCATE tagekyc.raw_export_subject_consent_authorities;""",
                     """INSERT INTO tagekyc.raw_export_subject_consent_classes ("SubjectConsentRecordId","RawClass","CreatedAt") VALUES (gen_random_uuid(),'LiveSelfieImage',transaction_timestamp());""",
                     """UPDATE tagekyc.raw_export_subject_consent_classes SET "RawClass" = "RawClass";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_classes;""",
                     """TRUNCATE tagekyc.raw_export_subject_consent_classes;""",
                 })
        {
            var denied = await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(deniedConnection, null, sql));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
        }
    }

    [Fact]
    public async Task Direct_deployer_insert_cannot_race_resolver_or_append_paths()
    {
        await using var setup = postgres.CreateDbContext();
        await GrantAuthoritiesAsync(setup);

        var resolverPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000074");
        await SeedPolicyAsync(setup, resolverPolicy);
        var resolverSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, "direct-race-resolver");
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(resolverSession, resolverPolicy));
        await using (var resolverDb = postgres.CreateDbContext())
        {
            await using var resolverTx = await resolverDb.Database.BeginTransactionAsync();
            await new EfRawExportSubjectConsentRepository(resolverDb)
                .ResolveSubjectExportConsentForAuthorizationAsync(resolverSession, resolverPolicy, 1);
            var directTask = DirectWithdrawInsertInSeparateConnectionAsync(resolverSession, resolverPolicy, "direct-race-resolver", 2, 1);
            await Task.Delay(200);
            Assert.False(directTask.IsCompleted);
            await resolverTx.CommitAsync();
            var direct = await directTask;
            Assert.True(direct.Succeeded, direct.Message);
        }

        var appendPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000075");
        await SeedPolicyAsync(setup, appendPolicy);
        var appendSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, "direct-race-append");
        await using (var appendConnection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await appendConnection.OpenAsync();
            await using var appendTx = await appendConnection.BeginTransactionAsync();
            await ExecuteScalarAsync(appendConnection, appendTx, $"SET LOCAL tagekyc.actor_principal_id = '{RecorderPrincipal:D}';");
            await ExecuteScalarAsync(appendConnection, appendTx, $"""
                SELECT tagekyc.raw_export_append_subject_consent_granted(
                    '{appendSession}', '{appendPolicy}', 1, ARRAY['LiveSelfieImage'],
                    'consent-text:v1','sha256:consent-text','external-consent:append','decision:append-race', NULL);
                """);
            var directTask = DirectGrantInsertInSeparateConnectionAsync(appendSession, appendPolicy, "direct-race-append", 2);
            await Task.Delay(200);
            Assert.False(directTask.IsCompleted);
            await appendTx.CommitAsync();
            var direct = await directTask;
            Assert.False(direct.Succeeded);
            Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED", direct.Message, StringComparison.Ordinal);
        }

        var withdrawPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000076");
        await SeedPolicyAsync(setup, withdrawPolicy);
        var withdrawSession = await SeedSessionAsync(setup, VerificationSessionState.Completed, "direct-race-withdraw");
        await new EfRawExportSubjectConsentRepository(setup).RecordSubjectConsentGrantedAsync(GrantCommand(withdrawSession, withdrawPolicy));
        await using (var withdrawConnection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await withdrawConnection.OpenAsync();
            await using var withdrawTx = await withdrawConnection.BeginTransactionAsync();
            await ExecuteScalarAsync(withdrawConnection, withdrawTx, $"SET LOCAL tagekyc.actor_principal_id = '{WithdrawerPrincipal:D}';");
            await ExecuteScalarAsync(withdrawConnection, withdrawTx, $"""
                SELECT tagekyc.raw_export_append_subject_consent_withdrawn(
                    '{withdrawSession}', '{withdrawPolicy}', 1, 1, 1, 'decision:withdraw-race', NULL);
                """);
            var directTask = DirectWithdrawInsertInSeparateConnectionAsync(withdrawSession, withdrawPolicy, "direct-race-withdraw", 2, 1);
            await Task.Delay(200);
            Assert.False(directTask.IsCompleted);
            await withdrawTx.CommitAsync();
            var direct = await directTask;
            Assert.False(direct.Succeeded);
            Assert.Contains("RAW_EXPORT_CONSENT_REVISION_CONFLICT", direct.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Repository_lifecycle_reactivation_paths_do_not_fallback()
    {
        await using var db = postgres.CreateDbContext();
        await GrantAuthoritiesAsync(db);
        var repository = new EfRawExportSubjectConsentRepository(db);
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000077");
        await SeedPolicyAsync(db, policyId);
        var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, "reactivation");

        Assert.Equal(2, await repository.RevokeConsentAuthorityAsync(new(
            AdminPrincipal,
            RecorderPrincipal,
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            1,
            DecisionRef: null,
            TargetRevision: 1)));
        var denied = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId)));
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED", denied.MessageText, StringComparison.Ordinal);

        Assert.Equal(3, await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            RecorderPrincipal,
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            2,
            "decision:recorder-reactivate")));
        var granted = await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
        Assert.Equal(1, granted.ConsentRef?.Revision);

        var withdrawn = await repository.RecordSubjectConsentWithdrawnAsync(new(
            WithdrawerPrincipal,
            sessionId,
            policyId,
            1,
            ExpectedRevision: 1,
            TargetRevision: 1,
            DecisionRef: null));
        Assert.Equal(RawExportSubjectConsentState.NonEffective, withdrawn.State);
        Assert.Equal(2, withdrawn.ConsentRef?.Revision);

        var reactivated = await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId, decisionRef: "decision:consent-reactivate"));
        Assert.Equal(RawExportSubjectConsentState.Effective, reactivated.State);
        Assert.Equal(3, reactivated.ConsentRef?.Revision);
        Assert.Equal("decision:consent-reactivate", reactivated.DecisionRef);
    }

    [Fact]
    public async Task DecisionRef_is_optional_but_blank_is_rejected_for_consent_and_authority_events()
    {
        await using var db = postgres.CreateDbContext();
        var repository = new EfRawExportSubjectConsentRepository(db);

        var nullableAuthorityRevision = await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            Guid.Parse("88b20000-0000-5000-8000-000000000088"),
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            0,
            DecisionRef: null));
        Assert.Equal(1, nullableAuthorityRevision);
        Assert.Null(await QueryAuthorityDecisionRefAsync(
            db,
            Guid.Parse("88b20000-0000-5000-8000-000000000088"),
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            1));

        var authorityWithDecisionPrincipal = Guid.Parse("88b20000-0000-5000-8000-000000000087");
        await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            authorityWithDecisionPrincipal,
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
            0,
            "decision:authority-verbatim"));
        Assert.Equal(
            "decision:authority-verbatim",
            await QueryAuthorityDecisionRefAsync(
                db,
                authorityWithDecisionPrincipal,
                RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
                1));
        await repository.RevokeConsentAuthorityAsync(new(
            AdminPrincipal,
            authorityWithDecisionPrincipal,
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
            1,
            "decision:authority-revoke-verbatim",
            TargetRevision: 1));
        Assert.Equal(
            "decision:authority-revoke-verbatim",
            await QueryAuthorityDecisionRefAsync(
                db,
                authorityWithDecisionPrincipal,
                RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
                2));

        var blankAuthority = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.GrantConsentAuthorityAsync(new(
                AdminPrincipal,
                Guid.Parse("88b20000-0000-5000-8000-000000000089"),
                ClientApplicationId,
                RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
                0,
                "   ")));
        Assert.Contains("RAW_EXPORT_DECISION_REF_INVALID", blankAuthority.MessageText, StringComparison.Ordinal);
        var blankAuthorityRevoke = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.RevokeConsentAuthorityAsync(new(
                AdminPrincipal,
                authorityWithDecisionPrincipal,
                ClientApplicationId,
                RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
                2,
                "   ",
                TargetRevision: 2)));
        Assert.Contains("RAW_EXPORT_DECISION_REF_INVALID", blankAuthorityRevoke.MessageText, StringComparison.Ordinal);

        await GrantAuthoritiesAsync(db);
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000090");
        await SeedPolicyAsync(db, policyId);
        var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, "decision-ref");
        var granted = await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId, decisionRef: null));
        Assert.Null(granted.DecisionRef);

        var withdrawn = await repository.RecordSubjectConsentWithdrawnAsync(new(
            WithdrawerPrincipal,
            sessionId,
            policyId,
            1,
            ExpectedRevision: 1,
            TargetRevision: 1,
            DecisionRef: null));
        Assert.Null(withdrawn.DecisionRef);

        var verbatimPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000093");
        await SeedPolicyAsync(db, verbatimPolicy);
        var verbatimSession = await SeedSessionAsync(db, VerificationSessionState.Completed, "decision-ref-verbatim");
        var verbatimGranted = await repository.RecordSubjectConsentGrantedAsync(
            GrantCommand(verbatimSession, verbatimPolicy, decisionRef: "decision:consent-grant-verbatim"));
        Assert.Equal("decision:consent-grant-verbatim", verbatimGranted.DecisionRef);
        var verbatimWithdrawn = await repository.RecordSubjectConsentWithdrawnAsync(new(
            WithdrawerPrincipal,
            verbatimSession,
            verbatimPolicy,
            1,
            ExpectedRevision: 1,
            TargetRevision: 1,
            DecisionRef: "decision:consent-withdraw-verbatim"));
        Assert.Equal("decision:consent-withdraw-verbatim", verbatimWithdrawn.DecisionRef);

        var blankGrantPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000091");
        await SeedPolicyAsync(db, blankGrantPolicy);
        var blankGrantSession = await SeedSessionAsync(db, VerificationSessionState.Completed, "decision-ref-blank-grant");
        var blankGrant = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.RecordSubjectConsentGrantedAsync(GrantCommand(blankGrantSession, blankGrantPolicy, decisionRef: "  ")));
        Assert.Contains("RAW_EXPORT_DECISION_REF_INVALID", blankGrant.MessageText, StringComparison.Ordinal);

        var blankWithdrawPolicy = Guid.Parse("88b20000-0000-5000-8000-000000000092");
        await SeedPolicyAsync(db, blankWithdrawPolicy);
        var blankWithdrawSession = await SeedSessionAsync(db, VerificationSessionState.Completed, "decision-ref-blank-withdraw");
        await repository.RecordSubjectConsentGrantedAsync(GrantCommand(blankWithdrawSession, blankWithdrawPolicy));
        var blankWithdraw = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.RecordSubjectConsentWithdrawnAsync(new(
                WithdrawerPrincipal,
                blankWithdrawSession,
                blankWithdrawPolicy,
                1,
                ExpectedRevision: 1,
                TargetRevision: 1,
                DecisionRef: "   ")));
        Assert.Contains("RAW_EXPORT_DECISION_REF_INVALID", blankWithdraw.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Direct_sql_negatives_and_security_definer_manifest_are_enforced()
    {
        await using var db = postgres.CreateDbContext();
        var expected = ExpectedFunctionSecurityManifest();
        var functions = await QueryB2FunctionSecurityAsync(db);
        Assert.Equal(
            expected.Select(item => item.Signature).Order(StringComparer.Ordinal),
            functions.Keys.Order(StringComparer.Ordinal));
        foreach (var item in expected)
        {
            var actual = functions[item.Signature];
            Assert.Equal("tagekyc_raw_export_deployer", actual.Owner);
            Assert.False(actual.OwnerCanLogin);
            Assert.Equal(item.SecurityDefiner, actual.SecurityDefiner);
            Assert.Equal("search_path=pg_catalog", actual.Config);
            Assert.False(actual.PublicExecute);
            Assert.Equal(item.RuntimeExecute, actual.RuntimeExecute);
        }

        await db.Database.ExecuteSqlRawAsync("GRANT tagekyc_runtime TO tagekyc;");
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await ExecuteScalarAsync(connection, null, "SET ROLE tagekyc_runtime;");
        foreach (var sql in new[]
                 {
                     """INSERT INTO tagekyc.raw_export_subject_consent_authorities ("AuthorityEventId","AuthorityPrincipalId","ClientApplicationId","AuthorityType","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(),'88b20000-0000-5000-8000-000000000002','88b20000-0000-5000-8000-000000000010','SubjectConsentRecorder',1,'Granted','decision:direct','88b20000-0000-5000-8000-000000000001',transaction_timestamp());""",
                     """UPDATE tagekyc.raw_export_subject_consent_events SET "Revision" = "Revision";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_classes;""",
                     """TRUNCATE tagekyc.raw_export_subject_consent_authorities;""",
                     """SELECT tagekyc.raw_export_consent_lock_key(decode('0000000000000000','hex'));""",
                     """SELECT tagekyc.raw_export_append_subject_consent_authority('88b20000-0000-5000-8000-000000000002','88b20000-0000-5000-8000-000000000010','SubjectConsentRecorder',0,'Granted',NULL,'decision:runtime',NULL);""",
                 })
        {
            var denied = await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(connection, null, sql));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
        }

        await db.Database.ExecuteSqlRawAsync("GRANT tagekyc_raw_export_deployer TO tagekyc;");
        var authorityRevisionConflict = await DirectInsertAsDeployerThrowsAsync(
            AdminPrincipal,
            "authority",
            """INSERT INTO tagekyc.raw_export_subject_consent_authorities ("AuthorityEventId","AuthorityPrincipalId","ClientApplicationId","AuthorityType","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(),'88b20000-0000-5000-8000-000000000002','88b20000-0000-5000-8000-000000000010','SubjectConsentRecorder',2,'Granted','decision:direct-conflict','88b20000-0000-5000-8000-000000000001',transaction_timestamp());""");
        Assert.Contains(
            "RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_REVISION_CONFLICT",
            authorityRevisionConflict.MessageText,
            StringComparison.Ordinal);

        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000063");
        await SeedPolicyAsync(db, policyId);
        await GrantAuthoritiesAsync(db);
        var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, "direct-consent-conflict");
        var consentRevisionConflict = await DirectInsertAsDeployerThrowsAsync(
            RecorderPrincipal,
            "consent",
            $"""INSERT INTO tagekyc.raw_export_subject_consent_events ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId","Revision","EventType","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef","ValidFromUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(), tagekyc.raw_export_consent_scope_hash('{sessionId}','direct-consent-conflict','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}'), '{sessionId}', 'direct-consent-conflict', '{policyId}', 1, 'SubjectRawBiometricExport', '{ClientApplicationId}', 2, 'Granted', 'consent-text:v1', 'sha256:consent-text', 'external-consent:direct', 'decision:direct-conflict', transaction_timestamp(), transaction_timestamp(), '{RecorderPrincipal}', transaction_timestamp());""");
        Assert.Contains("RAW_EXPORT_CONSENT_REVISION_CONFLICT", consentRevisionConflict.MessageText, StringComparison.Ordinal);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE OR REPLACE FUNCTION tagekyc.raw_export_subject_consent_unexpected_probe() RETURNS integer LANGUAGE sql SET search_path = pg_catalog AS $$ SELECT 1 $$;");
        try
        {
            var expanded = await QueryB2FunctionSecurityAsync(db);
            Assert.Contains("tagekyc.raw_export_subject_consent_unexpected_probe()", expanded.Keys);
            Assert.NotEqual(
                expected.Select(item => item.Signature).Order(StringComparer.Ordinal),
                expanded.Keys.Order(StringComparer.Ordinal));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP FUNCTION IF EXISTS tagekyc.raw_export_subject_consent_unexpected_probe();");
        }
    }

    [Fact]
    public async Task Deployer_role_reaches_trigger_defence_in_depth_for_direct_dml_invariants()
    {
        await using var db = postgres.CreateDbContext();
        await GrantAuthoritiesAsync(db);
        var policyId = Guid.Parse("88b20000-0000-5000-8000-000000000067");
        await SeedPolicyAsync(db, policyId);
        var sessionId = await SeedSessionAsync(db, VerificationSessionState.Completed, "trigger-defence");
        var repository = new EfRawExportSubjectConsentRepository(db);
        await repository.RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId));
        await db.Database.ExecuteSqlRawAsync("""
            GRANT UPDATE, DELETE ON
                tagekyc.raw_export_subject_consent_authorities,
                tagekyc.raw_export_subject_consent_events,
                tagekyc.raw_export_subject_consent_classes
            TO tagekyc_raw_export_deployer;
            """);
        var recordId = (Guid)(await ExecuteScalarAsync(db, """
            SELECT "SubjectConsentRecordId"
            FROM tagekyc.raw_export_subject_consent_events
            WHERE "VerificationSessionId" = @sessionId AND "Revision" = 1;
            """, new NpgsqlParameter("sessionId", sessionId)) ?? throw new InvalidOperationException());

        foreach (var sql in new[]
                 {
                     """UPDATE tagekyc.raw_export_subject_consent_authorities SET "Revision" = "Revision";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_authorities;""",
                     """UPDATE tagekyc.raw_export_subject_consent_events SET "Revision" = "Revision";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_events;""",
                     """UPDATE tagekyc.raw_export_subject_consent_classes SET "RawClass" = "RawClass";""",
                     """DELETE FROM tagekyc.raw_export_subject_consent_classes;""",
                 })
        {
            var denied = await DirectInsertAsDeployerThrowsAsync(RecorderPrincipal, "consent", sql);
            Assert.Contains("append-only table", denied.MessageText, StringComparison.Ordinal);
        }

        var directBypass = await DirectInsertAsDeployerThrowsAsync(
            RecorderPrincipal,
            "wrong-context",
            $"""INSERT INTO tagekyc.raw_export_subject_consent_events ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId","Revision","EventType","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef","ValidFromUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(), tagekyc.raw_export_consent_scope_hash('{sessionId}','trigger-defence','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}'), '{sessionId}', 'trigger-defence', '{policyId}', 1, 'SubjectRawBiometricExport', '{ClientApplicationId}', 2, 'Granted', 'consent-text:v1', 'sha256:consent-text', 'external-consent:direct-bypass', 'decision:direct-bypass', transaction_timestamp(), transaction_timestamp(), '{RecorderPrincipal}', transaction_timestamp());""");
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_DIRECT_INSERT_UNSUPPORTED", directBypass.MessageText, StringComparison.Ordinal);

        var childLaterTransaction = await DirectInsertAsDeployerThrowsAsync(
            RecorderPrincipal,
            "consent",
            $"""INSERT INTO tagekyc.raw_export_subject_consent_classes ("SubjectConsentRecordId","RawClass","CreatedAt") VALUES ('{recordId}', 'ChipDg1', transaction_timestamp());""");
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_CHILD_APPEND_UNSUPPORTED", childLaterTransaction.MessageText, StringComparison.Ordinal);

        var selfGrant = await DirectInsertAsDeployerThrowsAsync(
            RecorderPrincipal,
            "authority",
            $"""INSERT INTO tagekyc.raw_export_subject_consent_authorities ("AuthorityEventId","AuthorityPrincipalId","ClientApplicationId","AuthorityType","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(),'{RecorderPrincipal}','{ClientApplicationId}','SubjectConsentRecorder',2,'Granted','decision:self-grant','{RecorderPrincipal}',transaction_timestamp());""");
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_SELF_ESCALATION_DENIED", selfGrant.MessageText, StringComparison.Ordinal);

        var scopeMismatch = await DirectInsertAsDeployerThrowsAsync(
            RecorderPrincipal,
            "consent",
            $"""INSERT INTO tagekyc.raw_export_subject_consent_events ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId","Revision","EventType","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef","ValidFromUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(), tagekyc.raw_export_consent_scope_hash('{sessionId}','wrong-subject','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}'), '{sessionId}', 'wrong-subject', '{policyId}', 1, 'SubjectRawBiometricExport', '{ClientApplicationId}', 2, 'Granted', 'consent-text:v1', 'sha256:consent-text', 'external-consent:scope-mismatch', 'decision:scope-mismatch', transaction_timestamp(), transaction_timestamp(), '{RecorderPrincipal}', transaction_timestamp());""");
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_SCOPE_MISMATCH", scopeMismatch.MessageText, StringComparison.Ordinal);

        var forgedHash = await DirectInsertAsDeployerThrowsAsync(
            RecorderPrincipal,
            "consent",
            $"""INSERT INTO tagekyc.raw_export_subject_consent_events ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId","Revision","EventType","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef","ValidFromUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc") VALUES (gen_random_uuid(), decode('0000000000000000000000000000000000000000000000000000000000000000','hex'), '{sessionId}', 'trigger-defence', '{policyId}', 1, 'SubjectRawBiometricExport', '{ClientApplicationId}', 2, 'Granted', 'consent-text:v1', 'sha256:consent-text', 'external-consent:hash-mismatch', 'decision:hash-mismatch', transaction_timestamp(), transaction_timestamp(), '{RecorderPrincipal}', transaction_timestamp());""");
        Assert.Contains("RAW_EXPORT_SUBJECT_CONSENT_SCOPE_HASH_MISMATCH", forgedHash.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Subject_consent_readiness_validator_passes_as_runtime_and_rejects_function_acl_drift()
    {
        await ValidateB2ReadinessAsRuntimeAsync();

        var resolver = "tagekyc.raw_export_resolve_subject_consent_for_authorization(uuid,uuid,integer)";
        await AssertB2FunctionDriftAsync(
            $"ALTER FUNCTION {resolver} SET search_path = pg_catalog, public;",
            $"ALTER FUNCTION {resolver} SET search_path = pg_catalog;");
        await AssertB2FunctionDriftAsync(
            $"GRANT EXECUTE ON FUNCTION {resolver} TO PUBLIC;",
            $"REVOKE EXECUTE ON FUNCTION {resolver} FROM PUBLIC;");
        await AssertB2FunctionDriftAsync(
            $"ALTER FUNCTION {resolver} SECURITY INVOKER;",
            $"ALTER FUNCTION {resolver} SECURITY DEFINER;");
        await AssertB2FunctionDriftAsync(
            "CREATE OR REPLACE FUNCTION tagekyc.raw_export_subject_consent_unexpected_readiness_probe() RETURNS integer LANGUAGE sql SET search_path = pg_catalog AS $$ SELECT 1 $$;",
            "DROP FUNCTION IF EXISTS tagekyc.raw_export_subject_consent_unexpected_readiness_probe();");
    }

    [Fact]
    public async Task Golden_vectors_pin_rfc4122_guid_hash_and_signed_big_endian_lock_key()
    {
        var sessionId = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var policyId = Guid.Parse("10213243-5465-7687-98a9-bacbdcedfe0f");
        var recipientId = Guid.Parse("ffeeddcc-bbaa-9988-7766-554433221100");
        Assert.Equal(
            "00112233445566778899AABBCCDDEEFF",
            Convert.ToHexString(RawExportConsentScopeCodec.CanonicalGuidBytes(sessionId)));

        var csharpHash = RawExportConsentScopeCodec.ComputeScopeHash(
            sessionId,
            "subject-ref",
            policyId,
            1,
                RawExportSubjectConsentConstants.PurposeCode,
                recipientId);
        var csharpKey = RawExportConsentScopeCodec.ToAdvisoryLockKey(csharpHash);
        Assert.Equal(
            "CB73F2B3ABFBD4D8F5FE7B1A76C6C895502AEE30A084BBBA7348064BE75378CF",
            Convert.ToHexString(csharpHash));
        Assert.Equal(-3786416008189979432, csharpKey);

        await using var db = postgres.CreateDbContext();
        var sqlHash = (byte[])(await ExecuteScalarAsync(db, """
            SELECT tagekyc.raw_export_consent_scope_hash(
                '00112233-4455-6677-8899-aabbccddeeff',
                'subject-ref',
                '10213243-5465-7687-98a9-bacbdcedfe0f',
                1,
                'SubjectRawBiometricExport',
                'ffeeddcc-bbaa-9988-7766-554433221100');
            """) ?? throw new InvalidOperationException());
        var sqlKey = Convert.ToInt64(await ExecuteScalarAsync(db, "SELECT tagekyc.raw_export_consent_lock_key(@p0);", new NpgsqlParameter("p0", sqlHash)));

        Assert.Equal(Convert.ToHexString(csharpHash), Convert.ToHexString(sqlHash));
        Assert.Equal(csharpKey, sqlKey);

        var nfcHash = RawExportConsentScopeCodec.ComputeScopeHash(
            Guid.Parse("11111111-2222-3333-8444-555555555555"),
            "Cafe\u0301",
            Guid.Parse("66666666-7777-4888-9999-aaaaaaaaaaaa"),
            7,
            RawExportSubjectConsentConstants.PurposeCode,
            Guid.Parse("bbbbbbbb-cccc-4ddd-8eee-ffffffffffff"));
        var nfcKey = RawExportConsentScopeCodec.ToAdvisoryLockKey(nfcHash);
        Assert.Equal(
            "8E69EDDD3678F4CC0A600CE4E5B8C504E971CA69A9B55FB07AA2FEB8AC17A738",
            Convert.ToHexString(nfcHash));
        Assert.Equal(-8184749313411713844, nfcKey);

        var sqlNfcHash = (byte[])(await ExecuteScalarAsync(db, """
            SELECT tagekyc.raw_export_consent_scope_hash(
                '11111111-2222-3333-8444-555555555555',
                U&'Cafe\0301',
                '66666666-7777-4888-9999-aaaaaaaaaaaa',
                7,
                'SubjectRawBiometricExport',
                'bbbbbbbb-cccc-4ddd-8eee-ffffffffffff');
            """) ?? throw new InvalidOperationException());
        var sqlNfcKey = Convert.ToInt64(await ExecuteScalarAsync(db, "SELECT tagekyc.raw_export_consent_lock_key(@p0);", new NpgsqlParameter("p0", sqlNfcHash)));
        Assert.Equal(Convert.ToHexString(nfcHash), Convert.ToHexString(sqlNfcHash));
        Assert.Equal(nfcKey, sqlNfcKey);
    }

    [Fact]
    public async Task Inertness_no_tip88b3_or_raw_byte_surface_exists()
    {
        await using var db = postgres.CreateDbContext();
        var tables = await QueryStringsAsync(db, """
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'tagekyc'
              AND (tablename LIKE 'raw_export_authorization%' OR tablename LIKE 'raw_export_permit%' OR tablename LIKE 'raw_export_package%')
            ORDER BY tablename;
            """);
        Assert.Empty(tables);

        var b2Columns = await QueryStringsAsync(db, """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'tagekyc'
              AND table_name LIKE 'raw_export_subject_consent%'
              AND (column_name ILIKE '%payload%' OR column_name ILIKE '%bytes%' OR column_name ILIKE '%hashref%')
            ORDER BY column_name;
            """);
        Assert.DoesNotContain(b2Columns, column => column.Contains("payload", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(b2Columns, column => column.Contains("bytes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Migration_rollback_restores_b1_acl_snapshot_and_reapply_succeeds()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync("20260712133151_Tip88B1RawExportControlPlane");
        await db.Database.ExecuteSqlRawAsync("GRANT SELECT ON tagekyc.raw_export_policy_versions TO tagekyc_runtime;");
        await db.Database.ExecuteSqlRawAsync("GRANT UPDATE ON tagekyc.verification_sessions TO tagekyc_runtime;");
        var b1AclBefore = await SnapshotB1AclAsync(db);
        Assert.Contains("table:runtime:raw_export_policy_versions:SELECT:true", b1AclBefore);
        Assert.Contains("table:runtime:verification_sessions:UPDATE:true", b1AclBefore);

        await migrator.MigrateAsync("20260720022629_Tip88B2SubjectExportConsent");
        await migrator.MigrateAsync("20260712133151_Tip88B1RawExportControlPlane");
        var b1AclAfter = await SnapshotB1AclAsync(db);
        Assert.Equal(b1AclBefore, b1AclAfter);
        Assert.Contains("table:runtime:raw_export_policy_versions:SELECT:true", b1AclAfter);
        Assert.Contains("table:runtime:verification_sessions:UPDATE:true", b1AclAfter);

        await BootstrapB1RootsAsync(db, AdminPrincipal);
        await ProvisionB1RuntimeReadinessSelectsAsync(db);
        await ValidateB1ReadinessAsRuntimeAsync();

        await migrator.MigrateAsync("20260720022629_Tip88B2SubjectExportConsent");
        await ValidateB2ReadinessAsRuntimeAsync();
    }

    private RawExportSubjectConsentGrantCommand GrantCommand(
        Guid sessionId,
        Guid policyId,
        DateTimeOffset? validUntil = null,
        string? decisionRef = "decision:consent") =>
        new(
            RecorderPrincipal,
            sessionId,
            policyId,
            1,
            new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage },
            "consent-text:v1",
            "sha256:consent-text",
            "external-consent:artifact",
            decisionRef,
            validUntil);

    private async Task GrantAuthoritiesAsync(TagEkycDbContext db)
    {
        var repository = new EfRawExportSubjectConsentRepository(db);
        await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            RecorderPrincipal,
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
            0,
            "decision:recorder"));
        await repository.GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            WithdrawerPrincipal,
            ClientApplicationId,
            RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
            0,
            "decision:withdrawer"));
    }

    private static async Task SeedPolicyAsync(TagEkycDbContext db, Guid policyId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_versions
                ("PolicyId","PolicyVersion","Mode","Purpose","RetentionPurposeCode","ConsentRequirement","ControllerRole","ControllerEntityRef",
                 "ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","PermitTtlSeconds","CreatedAt")
            VALUES
                ('{policyId}',1,'ExternalExportOnlyNoRetain','purpose','NO_RETAIN','Required',
                 'Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,300,transaction_timestamp());
            """);
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_allowed_classes
                ("PolicyId","PolicyVersion","RawClass","CreatedAt")
            VALUES
                ('{policyId}',1,'LiveSelfieImage',transaction_timestamp());
            """);
        await transaction.CommitAsync();
    }

    private static async Task<Guid> SeedSessionAsync(
        TagEkycDbContext db,
        VerificationSessionState state,
        string subjectRef,
        Guid? clientApplicationId = null)
    {
        var session = VerificationSession.Create(
            clientApplicationId ?? ClientApplicationId,
            subjectRef,
            VerificationProfile.StandardEkycProfile,
            "raw-export",
            [RequiredCheckType.DocumentNfc],
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow);
        await new EfVerificationSessionRepository(db).AddAsync(session);
        await new EfVerificationSessionRepository(db).SetStateAsync(session.Id, state);
        return session.Id;
    }

    private async Task<(bool Succeeded, string? Message)> WithdrawInSeparateConnectionAsync(
        Guid sessionId,
        Guid policyId,
        int expectedRevision,
        int targetRevision)
    {
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{WithdrawerPrincipal:D}';");
            await ExecuteScalarAsync(connection, transaction, $"""
                SELECT tagekyc.raw_export_append_subject_consent_withdrawn(
                    '{sessionId}', '{policyId}', 1, {expectedRevision}, {targetRevision}, 'decision:withdraw-race', NULL);
                """);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private async Task<(bool Succeeded, string? Message)> WithdrawAsRuntimeInSeparateConnectionAsync(
        Guid sessionId,
        Guid policyId,
        int expectedRevision,
        int targetRevision)
    {
        try
        {
            await using var db = postgres.CreateDbContext();
            await UseRuntimeRoleAsync(db);
            await new EfRawExportSubjectConsentRepository(db)
                .RecordSubjectConsentWithdrawnAsync(new(
                    WithdrawerPrincipal,
                    sessionId,
                    policyId,
                    1,
                    expectedRevision,
                    targetRevision,
                    "decision:runtime-withdraw-lock"));
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private async Task<(bool Succeeded, string? Message)> RecordGrantAsRuntimeInSeparateConnectionAsync(
        Guid sessionId,
        Guid policyId)
    {
        try
        {
            await using var db = postgres.CreateDbContext();
            await UseRuntimeRoleAsync(db);
            await new EfRawExportSubjectConsentRepository(db)
                .RecordSubjectConsentGrantedAsync(GrantCommand(sessionId, policyId, decisionRef: "decision:runtime-grant-lock"));
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private async Task<RawExportSubjectConsentSnapshot> ResolveConsentAsRuntimeInSeparateConnectionAsync(
        Guid sessionId,
        Guid policyId)
    {
        await using var db = postgres.CreateDbContext();
        await UseRuntimeRoleAsync(db);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var snapshot = await new EfRawExportSubjectConsentRepository(db)
            .ResolveSubjectExportConsentForAuthorizationAsync(sessionId, policyId, 1);
        await transaction.CommitAsync();
        return snapshot;
    }

    private async Task<(bool Succeeded, string? Message)> RevokeAuthorityInSeparateConnectionAsync(
        Guid authorityPrincipalId,
        RawExportSubjectConsentAuthorityType authorityType,
        int expectedRevision,
        int targetRevision)
    {
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{AdminPrincipal:D}';");
            await ExecuteScalarAsync(connection, transaction, $"""
                SELECT tagekyc.raw_export_append_subject_consent_authority(
                    '{authorityPrincipalId}', '{ClientApplicationId}', '{authorityType}', {expectedRevision},
                    'Revoked', {targetRevision}, 'decision:authority-revoke-race', NULL);
                """);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private static long ConsentScopeLockKey(Guid sessionId, string subjectRef, Guid policyId)
    {
        var hash = RawExportConsentScopeCodec.ComputeScopeHash(
            sessionId,
            subjectRef,
            policyId,
            1,
            RawExportSubjectConsentConstants.PurposeCode,
            ClientApplicationId);
        return RawExportConsentScopeCodec.ToAdvisoryLockKey(hash);
    }

    private static async Task TakeConsentScopeLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long key,
        bool shared)
    {
        await ExecuteScalarAsync(
            connection,
            transaction,
            shared
                ? "SELECT pg_catalog.pg_advisory_xact_lock_shared(@key);"
                : "SELECT pg_catalog.pg_advisory_xact_lock(@key);",
            new NpgsqlParameter("key", key));
    }

    private static async Task AssertAdvisoryLockHeldAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long key,
        string mode)
    {
        var (classId, objectId) = AdvisoryLockParts(key);
        var count = Convert.ToInt32(await ExecuteScalarAsync(
            connection,
            transaction,
            """
            SELECT count(*)
            FROM pg_catalog.pg_locks
            WHERE locktype = 'advisory'
              AND pid = pg_catalog.pg_backend_pid()
              AND classid::bigint = @classId
              AND objid::bigint = @objectId
              AND mode = @mode;
            """,
            new NpgsqlParameter("classId", classId),
            new NpgsqlParameter("objectId", objectId),
            new NpgsqlParameter("mode", mode)) ?? 0);
        Assert.Equal(1, count);
    }

    private async Task AssertAdvisoryLockWaiterAsync(long key)
    {
        var (classId, objectId) = AdvisoryLockParts(key);
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();

        for (var attempt = 0; attempt < 50; attempt++)
        {
            var count = Convert.ToInt32(await ExecuteScalarAsync(
                connection,
                null,
                """
                SELECT count(*)
                FROM pg_catalog.pg_locks
                WHERE locktype = 'advisory'
                  AND classid::bigint = @classId
                  AND objid::bigint = @objectId
                  AND granted = false;
                """,
                new NpgsqlParameter("classId", classId),
                new NpgsqlParameter("objectId", objectId)) ?? 0);
            if (count > 0)
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException($"Expected an advisory lock waiter for key {key}.");
    }

    private static (long ClassId, long ObjectId) AdvisoryLockParts(long key)
    {
        var unsigned = unchecked((ulong)key);
        return ((long)(uint)(unsigned >> 32), (long)(uint)unsigned);
    }

    private async Task<PostgresException> DirectInsertAsDeployerThrowsAsync(
        Guid actorPrincipalId,
        string appendContext,
        string sql)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await ExecuteScalarAsync(connection, null, "SET ROLE tagekyc_raw_export_deployer;");
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{actorPrincipalId:D}';");
        await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.raw_export_subject_consent_append_context = '{appendContext}';");
        return await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(connection, transaction, sql));
    }

    private static async Task UseRuntimeRoleAsync(TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("GRANT tagekyc_runtime TO tagekyc;");
        await db.Database.ExecuteSqlRawAsync("SET ROLE tagekyc_runtime;");
    }

    private async Task ValidateB2ReadinessAsRuntimeAsync()
    {
        await using var db = postgres.CreateDbContext();
        await UseRuntimeRoleAsync(db);
        await new RawExportSubjectConsentReadinessValidator(db).ValidateAsync(CancellationToken.None);
    }

    private static async Task AssertB2RuntimeTablePrivilegesAsync(NpgsqlConnection connection)
    {
        foreach (var table in new[]
                 {
                     "raw_export_subject_consent_authorities",
                     "raw_export_subject_consent_events",
                     "raw_export_subject_consent_classes",
                 })
        {
            foreach (var privilege in new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE" })
            {
                var allowed = (bool)(await ExecuteScalarAsync(
                    connection,
                    null,
                    $"SELECT has_table_privilege(current_user, 'tagekyc.{table}', '{privilege}');") ?? false);
                if (privilege == "SELECT")
                {
                    Assert.True(allowed, $"{table} should grant runtime SELECT.");
                }
                else
                {
                    Assert.False(allowed, $"{table} should not grant runtime {privilege}.");
                }
            }
        }
    }

    private async Task AssertB2FunctionDriftAsync(string driftSql, string restoreSql)
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(driftSql);
        try
        {
            var exception = await Assert.ThrowsAsync<RawExportSubjectConsentReadinessException>(
                ValidateB2ReadinessAsRuntimeAsync);
            Assert.Equal(RawExportSubjectConsentReadinessValidator.FunctionAclInvalid, exception.Code);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(restoreSql);
        }
    }

    private async Task ValidateB1ReadinessAsRuntimeAsync()
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("GRANT tagekyc_runtime TO tagekyc;");
        await db.Database.ExecuteSqlRawAsync("SET ROLE tagekyc_runtime;");
        await new RawExportControlPlaneReadinessValidator(db).ValidateAsync(CancellationToken.None);
    }

    private static async Task BootstrapB1RootsAsync(TagEkycDbContext db, Guid principalId)
    {
        foreach (var authority in new[] { "GrantAdmin", "RecorderAuthorityAdmin", "ActivationAuthority" })
        {
            await db.Database.ExecuteSqlRawAsync($"""
                SELECT tagekyc.raw_export_bootstrap_global_authority('{principalId}', '{authority}', 'decision:rollback-bootstrap:{authority}');
                """);
        }
    }

    private static async Task ProvisionB1RuntimeReadinessSelectsAsync(TagEkycDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("GRANT tagekyc_runtime TO tagekyc;");
        await db.Database.ExecuteSqlRawAsync("""
            GRANT SELECT ON
                tagekyc.raw_export_grants,
                tagekyc.raw_export_control_authorities,
                tagekyc.raw_export_fulfillments,
                tagekyc.raw_export_policy_lifecycle
            TO tagekyc_runtime;
            """);
    }

    private static async Task<IReadOnlyList<string>> SnapshotB1AclAsync(TagEkycDbContext db)
    {
        return await QueryStringsAsync(db, """
            SELECT 'table:' || item
            FROM (
                VALUES
                    ('runtime:verification_sessions:SELECT:' || has_table_privilege('tagekyc_runtime','tagekyc.verification_sessions','SELECT')::text),
                    ('runtime:verification_sessions:UPDATE:' || has_table_privilege('tagekyc_runtime','tagekyc.verification_sessions','UPDATE')::text),
                    ('deployer:verification_sessions:SELECT:' || has_table_privilege('tagekyc_raw_export_deployer','tagekyc.verification_sessions','SELECT')::text),
                    ('deployer:verification_sessions:UPDATE:' || has_table_privilege('tagekyc_raw_export_deployer','tagekyc.verification_sessions','UPDATE')::text),
                    ('runtime:raw_export_policy_versions:SELECT:' || has_table_privilege('tagekyc_runtime','tagekyc.raw_export_policy_versions','SELECT')::text),
                    ('deployer:raw_export_policy_versions:SELECT:' || has_table_privilege('tagekyc_raw_export_deployer','tagekyc.raw_export_policy_versions','SELECT')::text),
                    ('runtime:raw_export_grants:INSERT:' || has_table_privilege('tagekyc_runtime','tagekyc.raw_export_grants','INSERT')::text),
                    ('runtime:raw_export_control_authorities:INSERT:' || has_table_privilege('tagekyc_runtime','tagekyc.raw_export_control_authorities','INSERT')::text),
                    ('runtime:raw_export_fulfillments:INSERT:' || has_table_privilege('tagekyc_runtime','tagekyc.raw_export_fulfillments','INSERT')::text),
                    ('runtime:raw_export_policy_lifecycle:INSERT:' || has_table_privilege('tagekyc_runtime','tagekyc.raw_export_policy_lifecycle','INSERT')::text)
            ) AS v(item)
            UNION ALL
            SELECT 'function:' || p.oid::regprocedure::text || ':public=' ||
                   has_function_privilege('public', p.oid, 'EXECUTE')::text ||
                   ':runtime=' || has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')::text ||
                   ':bootstrapper=' || has_function_privilege('tagekyc_raw_export_bootstrapper', p.oid, 'EXECUTE')::text ||
                   ':config=' || COALESCE(array_to_string(p.proconfig, ','), '')
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname = 'tagekyc'
              AND (
                p.proname = 'enforce_raw_export_control_plane_insert'
                OR (
                  p.proname LIKE 'raw_export_%'
                  AND p.proname NOT LIKE '%subject_consent%'
                  AND p.proname NOT IN (
                    'raw_export_consent_scope_hash',
                    'raw_export_consent_lock_key',
                    'raw_export_lock_verification_session_for_subject_consent',
                    'raw_export_resolve_subject_consent_for_authorization')))
            ORDER BY 1;
            """);
    }

    private async Task<(bool Succeeded, string? Message)> DirectGrantInsertInSeparateConnectionAsync(
        Guid sessionId,
        Guid policyId,
        string subjectRef,
        int revision)
    {
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await ExecuteScalarAsync(connection, null, "GRANT tagekyc_raw_export_deployer TO tagekyc; SET ROLE tagekyc_raw_export_deployer;");
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{RecorderPrincipal:D}';");
            await ExecuteScalarAsync(connection, transaction, "SET LOCAL tagekyc.raw_export_subject_consent_append_context = 'consent';");
            await ExecuteScalarAsync(connection, transaction, $"""
                INSERT INTO tagekyc.raw_export_subject_consent_events
                    ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
                     "Revision","EventType","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef","ValidFromUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc")
                VALUES
                    (gen_random_uuid(),
                     tagekyc.raw_export_consent_scope_hash('{sessionId}','{subjectRef}','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}'),
                     '{sessionId}','{subjectRef}','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}',
                     {revision},'Granted','consent-text:v1','sha256:consent-text','external-consent:direct','decision:direct-race',
                     transaction_timestamp(),transaction_timestamp(),'{RecorderPrincipal}',transaction_timestamp());
                """);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private async Task<(bool Succeeded, string? Message)> DirectWithdrawInsertInSeparateConnectionAsync(
        Guid sessionId,
        Guid policyId,
        string subjectRef,
        int revision,
        int targetRevision)
    {
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await ExecuteScalarAsync(connection, null, "GRANT tagekyc_raw_export_deployer TO tagekyc; SET ROLE tagekyc_raw_export_deployer;");
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{WithdrawerPrincipal:D}';");
            await ExecuteScalarAsync(connection, transaction, "SET LOCAL tagekyc.raw_export_subject_consent_append_context = 'consent';");
            await ExecuteScalarAsync(connection, transaction, $"""
                INSERT INTO tagekyc.raw_export_subject_consent_events
                    ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
                     "Revision","EventType","TargetRevision","ExternalConsentArtifactRef","DecisionRef","WithdrawnByPrincipalId","RecordedAtUtc")
                VALUES
                    (gen_random_uuid(),
                     tagekyc.raw_export_consent_scope_hash('{sessionId}','{subjectRef}','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}'),
                     '{sessionId}','{subjectRef}','{policyId}',1,'SubjectRawBiometricExport','{ClientApplicationId}',
                     {revision},'Withdrawn',{targetRevision},NULL,'decision:direct-race','{WithdrawerPrincipal}',transaction_timestamp());
                """);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private static IReadOnlyList<FunctionExpectation> ExpectedFunctionSecurityManifest() =>
    [
        new("tagekyc.enforce_raw_export_subject_consent_child_same_transaction()", false, false),
        new("tagekyc.enforce_raw_export_subject_consent_granted_has_classes()", false, false),
        new("tagekyc.enforce_raw_export_subject_consent_insert()", false, false),
        new("tagekyc.raw_export_append_subject_consent_authority(authority_principal_id uuid, client_application_id uuid, authority_type text, expected_revision integer, event_type text, target_revision integer, decision_ref text, valid_until_utc timestamp with time zone)", true, false),
        new("tagekyc.raw_export_append_subject_consent_granted(verification_session_id uuid, policy_id uuid, policy_version integer, raw_classes text[], consent_text_version text, consent_text_content_hash text, external_consent_artifact_ref text, decision_ref text, valid_until_utc timestamp with time zone)", true, true),
        new("tagekyc.raw_export_append_subject_consent_withdrawn(verification_session_id uuid, policy_id uuid, policy_version integer, expected_revision integer, target_revision integer, decision_ref text, external_consent_artifact_ref text)", true, true),
        new("tagekyc.raw_export_consent_lock_key(scope_hash bytea)", false, false),
        new("tagekyc.raw_export_consent_scope_hash(verification_session_id uuid, subject_ref text, policy_id uuid, policy_version integer, purpose_code text, recipient_client_application_id uuid)", false, false),
        new("tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id uuid)", true, false),
        new("tagekyc.raw_export_resolve_subject_consent_for_authorization(verification_session_id uuid, policy_id uuid, policy_version integer)", true, true),
        new("tagekyc.raw_export_subject_consent_has_current_authority(actor_id uuid, client_application_id uuid, required_authority text)", true, false),
    ];

    private static async Task<IReadOnlyDictionary<string, FunctionSecurity>> QueryB2FunctionSecurityAsync(TagEkycDbContext db)
    {
        var rows = await QueryRowsAsync(db, """
            SELECT n.nspname || '.' || p.proname || '(' || pg_catalog.pg_get_function_identity_arguments(p.oid) || ')',
                   p.prosecdef,
                   owner.rolname,
                   owner.rolcanlogin,
                   COALESCE(array_to_string(p.proconfig, ','), ''),
                   has_function_privilege('public', p.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc'
              AND (
                p.proname LIKE '%subject_consent%'
                OR p.proname IN ('raw_export_consent_scope_hash','raw_export_consent_lock_key','raw_export_lock_verification_session_for_subject_consent')
              )
            ORDER BY p.oid::regprocedure::text;
            """);
        return rows.ToDictionary(
            row => (string)row[0]!,
            row => new FunctionSecurity((bool)row[1]!, (string)row[2]!, (bool)row[3]!, (string)row[4]!, (bool)row[5]!, (bool)row[6]!),
            StringComparer.Ordinal);
    }

    private static async Task<IReadOnlyList<string>> QueryStringsAsync(TagEkycDbContext db, string sql)
    {
        var rows = await QueryRowsAsync(db, sql);
        return rows.Select(row => (string)row[0]!).ToArray();
    }

    private static async Task<string?> QueryAuthorityDecisionRefAsync(
        TagEkycDbContext db,
        Guid authorityPrincipalId,
        RawExportSubjectConsentAuthorityType authorityType,
        int revision)
    {
        var value = await ExecuteScalarAsync(
            db,
            """
            SELECT "DecisionRef"
            FROM tagekyc.raw_export_subject_consent_authorities
            WHERE "AuthorityPrincipalId" = @authorityPrincipalId
              AND "AuthorityType" = @authorityType
              AND "Revision" = @revision;
            """,
            new NpgsqlParameter("authorityPrincipalId", authorityPrincipalId),
            new NpgsqlParameter("authorityType", authorityType.ToString()),
            new NpgsqlParameter("revision", revision));
        return value is DBNull or null ? null : (string)value;
    }

    private static async Task<IReadOnlyList<object?[]>> QueryRowsAsync(TagEkycDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var rows = new List<object?[]>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        return rows;
    }

    private static async Task<object?> ExecuteScalarAsync(TagEkycDbContext db, string sql, params NpgsqlParameter[] parameters)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        return await command.ExecuteScalarAsync();
    }

    private static async Task<object?> ExecuteScalarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteScalarAsync();
    }

    private sealed record FunctionExpectation(
        string Signature,
        bool SecurityDefiner,
        bool RuntimeExecute);

    private sealed record FunctionSecurity(
        bool SecurityDefiner,
        string Owner,
        bool OwnerCanLogin,
        string Config,
        bool PublicExecute,
        bool RuntimeExecute);
}

#pragma warning restore EF1002
