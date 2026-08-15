using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2R3VerifiedCiphertextStagingTests(
    PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string PreviousMigration = "20260807120000_Tip88C1B2R2DurableCustodyEncryption";
    private const string R3Migration = "20260810120000_Tip88C1B2R3VerifiedCiphertextStaging";
    private const string ExpectedSnapshotSha256 =
        "0A7713E431F90B5E23652FA47CE40030940FE3ADA1E5C77E084C9407038252CD";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task R301_apply_down_reapply_restores_R2_catalog_model_and_acl()
    {
        await RecreateDatabaseAtMigrationAsync(PreviousMigration);
        var expectedR2Catalog = await ReadR2CatalogFingerprintAsync();

        await using (var applyDb = postgres.CreateDbContext())
        {
            await applyDb.Database.GetService<IMigrator>().MigrateAsync(R3Migration);
        }
        await using (var minio = await DurableObjectMinioFixture.StartAsync())
        {
            var source = await CreateVerifiedAsync(minio, "r301-occupied"u8.ToArray());
            Assert.Equal(RawExportR3StageDisposition.Staged, (await StageAsync(source)).Disposition);
        }

        await using (var occupied = postgres.CreateDbContext())
        {
            var migrator = occupied.Database.GetService<IMigrator>();
            var error = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(PreviousMigration));
            Assert.Equal("RAW_EXPORT_R3_DOWN_OCCUPIED", error.MessageText);
            Assert.Equal(1, await occupied.RawExportSourceEncryptionAttempts.CountAsync(row => row.StagedAtUtc != null));
        }

        await RecreateDatabaseAtMigrationAsync(PreviousMigration);
        Assert.Equal(expectedR2Catalog, await ReadR2CatalogFingerprintAsync());
        await using var db = postgres.CreateDbContext();
        var cleanMigrator = db.Database.GetService<IMigrator>();
        await cleanMigrator.MigrateAsync(R3Migration);
        await cleanMigrator.MigrateAsync(PreviousMigration);
        Assert.Equal(expectedR2Catalog, await ReadR2CatalogFingerprintAsync());
        Assert.False(await FunctionExistsAsync());
        Assert.False(await ColumnExistsAsync("StagedCiphertextFingerprint"));
        await cleanMigrator.MigrateAsync(R3Migration);
        Assert.True(await FunctionExistsAsync());
        Assert.True(await ColumnExistsAsync("StagedCiphertextFingerprint"));
    }

    [Fact]
    public async Task R302_exact_verified_object_stages_once_with_complete_persisted_shape()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r302-exact-stage"u8.ToArray());
        var before = DateTimeOffset.UtcNow;
        var result = await StageAsync(source);
        var after = DateTimeOffset.UtcNow;
        Assert.Equal(RawExportR3StageDisposition.Staged, result.Disposition);
        Assert.Equal(source.SourceArtifactId, result.SourceArtifactId);
        Assert.Equal(source.AttemptId, result.AttemptId);
        Assert.Equal(source.ObjectCustodyId, result.ObjectCustodyId);
        Assert.Equal(2, result.StagedCiphertextFingerprintSchemaVersion);
        Assert.Equal(32, result.StagedCiphertextFingerprint!.Length);
        Assert.Equal(source.ReservationRevision + 1, result.ReservationRevision);
        Assert.Equal(source.Fence, result.Fence);
        Assert.InRange(result.StagedAtUtc!.Value, before, after);

        await using var db = postgres.CreateDbContext();
        var attempt = await db.RawExportSourceEncryptionAttempts.AsNoTracking()
            .SingleAsync(row => row.AttemptId == source.AttemptId);
        var head = await db.RawExportSourceHeads.AsNoTracking()
            .SingleAsync(row => row.SourceArtifactId == source.SourceArtifactId);
        Assert.Equal("Staged", head.CustodyState);
        Assert.Equal(result.StagedAtUtc, attempt.StagedAtUtc);
        Assert.Equal(source.ObjectCustodyId, attempt.StagedObjectCustodyId);
        Assert.Equal(source.ObjectStateRevision, attempt.StagedObjectStateRevision);
        Assert.Equal(source.CiphertextLength, attempt.StagedCiphertextLength);
        Assert.Equal(source.CiphertextDigest, attempt.StagedCiphertextDigest);
        Assert.Equal(source.ProviderReceiptDigest, attempt.StagedProviderReceiptDigest);
        Assert.Equal(source.VerificationEvidenceDigest, attempt.StagedVerificationEvidenceDigest);
    }

    [Fact]
    public async Task R303_response_loss_replay_returns_existing_match_before_fresh_authority()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r303-replay"u8.ToArray());
        var first = await StageAsync(source);
        await WithdrawAuthorityAsync(source);
        var replay = await StageAsync(source);
        Assert.Equal(RawExportR3StageDisposition.ExistingMatch, replay.Disposition);
        Assert.Equal(first.StagedCiphertextFingerprint, replay.StagedCiphertextFingerprint);
        Assert.Equal(first.StagedAtUtc, replay.StagedAtUtc);
        Assert.Equal(first.ReservationRevision, replay.ReservationRevision);
    }

    [Fact]
    public async Task R304_stale_reservation_attempt_fence_or_object_revision_cannot_stage()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r304-stale"u8.ToArray());
        var stale = new[]
        {
            Command(source) with { ExpectedReservationRevision = source.ReservationRevision + 1 },
            Command(source) with { ExpectedEncryptionAttemptRevision = source.EncryptionAttemptRevision + 1 },
            Command(source) with { ExpectedFence = source.Fence + 1 },
            Command(source) with { ExpectedObjectStateRevision = source.ObjectStateRevision + 1 },
        };
        foreach (var command in stale)
            Assert.Equal(RawExportR3StageDisposition.StateConflict, (await StageAsync(command)).Disposition);
        await AssertUnstagedAsync(source);
        Assert.Equal(RawExportR3StageDisposition.Staged, (await StageAsync(source)).Disposition);
        foreach (var command in stale)
            Assert.Equal(RawExportR3StageDisposition.StateConflict, (await StageAsync(command)).Disposition);
    }

    [Fact]
    public async Task R305_only_VerifiedCompleted_object_state_can_stage()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var r2 = new Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(postgres);
        foreach (var state in new[]
        {
            "Initiated", "PutInFlight", "PutOutcomeUnknown", "ObjectPresentPendingVerification",
            "NoObjectEstablished", "ObjectConflict", "CleanupPending", "Deleted", "Quarantined",
        })
        {
            var fixture = await r2.CreateR3ObjectStateAsync(state, minio);
            await using var readDb = postgres.CreateDbContext();
            var head = await readDb.RawExportSourceHeads.AsNoTracking()
                .SingleAsync(row => row.SourceArtifactId == fixture.SourceArtifactId);
            var command = new RawExportR3StageCommand(
                fixture.ActorPrincipalId,
                fixture.AttemptId,
                fixture.ObjectCustodyId,
                head.ReservationRevision,
                fixture.EncryptionAttemptRevision,
                fixture.Fence,
                fixture.ObjectStateRevision);
            Assert.Equal(state, fixture.ObjectState);
            Assert.Equal(RawExportR3StageDisposition.StateConflict, (await StageAsync(command)).Disposition);
        }

        var verified = await CreateVerifiedAsync(minio, "r305-verified"u8.ToArray());
        Assert.Equal(RawExportR3StageDisposition.Staged, (await StageAsync(verified)).Disposition);
    }

    [Fact]
    public async Task R306_fresh_authority_snapshot_consent_and_deadlines_gate_new_stage()
    {
        await AssertDirectSqlArgumentGuardAsync();

        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var withdrawn = await CreateVerifiedAsync(minio, "r306-authority"u8.ToArray());
        await WithdrawAuthorityAsync(withdrawn);
        Assert.Equal(RawExportR3StageDisposition.SourceRetentionNotAuthorized, (await StageAsync(withdrawn)).Disposition);
        await AssertUnstagedAsync(withdrawn);

        var authorityWait = await CreateVerifiedAsync(
            minio, "r306-authority-wait"u8.ToArray(), sourceLifetime: TimeSpan.FromSeconds(15));
        await AssertLockWaitCrossesExpiryAsync(authorityWait, authorityLock: true);

        var consentWait = await CreateVerifiedAsync(
            minio, "r306-consent-wait"u8.ToArray(), sourceLifetime: TimeSpan.FromSeconds(15));
        await AssertLockWaitCrossesExpiryAsync(consentWait, authorityLock: false);

        var comparatorSource = await CreateVerifiedAsync(minio, "r306-comparators"u8.ToArray());
        await AssertAuthorityProjectionComparatorsAsync(comparatorSource);
        await AssertConsentProjectionComparatorsAsync(comparatorSource);
        await AssertDeadlineComparatorAsync(comparatorSource, absoluteSourceDeadline: false);
        await AssertDeadlineComparatorAsync(comparatorSource, absoluteSourceDeadline: true);

        var sql = await FunctionDefinitionAsync();
        Assert.Contains("stage_time:=pg_catalog.clock_timestamp()", sql, StringComparison.Ordinal);
        Assert.Contains("stage_time>=r.\"ReservationExpiresAtUtc\"", sql, StringComparison.Ordinal);
        Assert.Contains("stage_time>=r.\"AbsoluteSourceExpiresAtUtc\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task R307_stage_precedence_is_exact_under_simultaneous_failures()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r307-precedence"u8.ToArray());
        await WithdrawAuthorityAsync(source);
        Assert.Equal(
            RawExportR3StageDisposition.StateConflict,
            (await StageAsync(Command(source) with { ExpectedFence = source.Fence + 1 })).Disposition);
        Assert.Equal(RawExportR3StageDisposition.SourceRetentionNotAuthorized, (await StageAsync(source)).Disposition);
        await AssertUnstagedAsync(source);

        var keyRevokeFirst = await CreateVerifiedAsync(minio, "r307-key-revoke-first"u8.ToArray());
        await RevokeKeyAsync(keyRevokeFirst);
        Assert.Equal(RawExportR3StageDisposition.StateConflict, (await StageAsync(keyRevokeFirst)).Disposition);

        var keyStageFirst = await CreateVerifiedAsync(minio, "r307-key-stage-first"u8.ToArray());
        var keyBlocker = await BeginBlockingLockAsync(keyStageFirst, authorityLock: true);
        await using (keyBlocker.Connection)
        await using (keyBlocker.Transaction)
        {
            var stage = StageAsync(keyStageFirst);
            await WaitForBlockedStageAsync();
            var revoke = RevokeKeyAsync(keyStageFirst);
            await Task.Delay(250);
            Assert.False(stage.IsCompleted);
            Assert.False(revoke.IsCompleted);
            await keyBlocker.Transaction.CommitAsync();
            Assert.Equal(RawExportR3StageDisposition.Staged, (await stage).Disposition);
            await revoke;
        }

        var authorityStageFirst = await CreateVerifiedAsync(minio, "r307-authority-stage-first"u8.ToArray());
        var consentBlocker = await BeginBlockingLockAsync(authorityStageFirst, authorityLock: false);
        await using (consentBlocker.Connection)
        await using (consentBlocker.Transaction)
        {
            var stage = StageAsync(authorityStageFirst);
            await WaitForBlockedStageAsync();
            var withdrawal = WithdrawAuthorityAsync(authorityStageFirst);
            await Task.Delay(250);
            Assert.False(stage.IsCompleted);
            Assert.False(withdrawal.IsCompleted);
            await consentBlocker.Transaction.CommitAsync();
            Assert.Equal(RawExportR3StageDisposition.Staged, (await stage).Disposition);
            await withdrawal;
        }
    }

    [Fact]
    public void R308_staged_ciphertext_v2_codec_matches_absolute_vector_and_every_field_bites()
    {
        var input = new RawExportR3FingerprintInput(
            Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Convert.FromHexString("202122232425262728292A2B2C2D2E2F303132333435363738393A3B3C3D3E3F"),
            3,
            Convert.FromHexString("E0E1E2E3E4E5E6E7E8E9EAEBECEDEEEFF0F1F2F3F4F5F6F7F8F9FAFBFCFDFEFF"),
            462,
            Convert.FromHexString("E2BDFF09933146098393A45537023E0E7460FE396A9D6FED078D8768C4FC9C06"),
            Sequential(0x40),
            Sequential(0x60));
        var expected = Convert.FromHexString("6B7B1CE324D3CE6687C33A6D1AA6A58F90E17107EBC6133247E486CAB7144FC5");
        Assert.Equal(expected, RawExportR3StagedCiphertextFingerprintCodec.Compute(input));
        Assert.Equal(expected, SHA256.HashData(IndependentPreimage(input)));

        foreach (var mutation in Mutations(input))
            Assert.NotEqual(expected, RawExportR3StagedCiphertextFingerprintCodec.Compute(mutation));
    }

    [Fact]
    public async Task R309_concurrent_exact_stage_has_one_winner_and_one_existing_match()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r309-concurrent"u8.ToArray());
        var results = await Task.WhenAll(StageAsync(source), StageAsync(source));
        Assert.Equal(1, results.Count(result => result.Disposition == RawExportR3StageDisposition.Staged));
        Assert.Equal(1, results.Count(result => result.Disposition == RawExportR3StageDisposition.ExistingMatch));
        await using var db = postgres.CreateDbContext();
        Assert.Equal(source.ReservationRevision + 1,
            (await db.RawExportSourceHeads.SingleAsync(row => row.SourceArtifactId == source.SourceArtifactId)).ReservationRevision);
    }

    [Fact]
    public async Task R310_staging_shapes_and_attempt_head_guards_are_exact_and_one_way()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r310-guard"u8.ToArray());
        Assert.Equal(RawExportR3StageDisposition.Staged, (await StageAsync(source)).Disposition);

        await using (var constraintConnection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await constraintConnection.OpenAsync();
            await using (var disable = new NpgsqlCommand(
                "ALTER TABLE tagekyc.raw_export_source_encryption_attempts DISABLE TRIGGER tr_raw_export_source_attempts_guard",
                constraintConnection))
                await disable.ExecuteNonQueryAsync();
            try
            {
                foreach (var property in StagingProperties)
                {
                    await using var nullField = new NpgsqlCommand(
                        $"UPDATE tagekyc.raw_export_source_encryption_attempts SET \"{property}\"=NULL WHERE \"AttemptId\"=@id",
                        constraintConnection);
                    nullField.Parameters.AddWithValue("id", source.AttemptId);
                    var error = await Assert.ThrowsAsync<PostgresException>(() => nullField.ExecuteNonQueryAsync());
                    Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
                    Assert.Equal("ck_raw_export_source_attempt_values", error.ConstraintName);
                }
            }
            finally
            {
                await using var enable = new NpgsqlCommand(
                    "ALTER TABLE tagekyc.raw_export_source_encryption_attempts ENABLE TRIGGER tr_raw_export_source_attempts_guard",
                    constraintConnection);
                await enable.ExecuteNonQueryAsync();
            }
        }

        var unstaged = await CreateVerifiedAsync(minio, "r310-trigger"u8.ToArray());
        foreach (var assignment in new[]
        {
            "\"StagedCiphertextFingerprintSchemaVersion\"=2",
            "\"StagedCiphertextFingerprint\"=decode(repeat('00',32),'hex')",
            "\"StagedObjectCustodyId\"=@object",
            "\"StagedObjectStateRevision\"=1",
            "\"StagedFromReservationRevision\"=1",
            "\"VerifiedPlaintextLength\"=1",
            "\"StagedCiphertextLength\"=1",
            "\"StagedCiphertextDigest\"=decode(repeat('00',32),'hex')",
            "\"StagedProviderReceiptDigest\"=decode(repeat('00',32),'hex')",
            "\"StagedVerificationEvidenceDigest\"=decode(repeat('00',32),'hex')",
            "\"StagedAtUtc\"=pg_catalog.clock_timestamp()",
            "\"Fence\"=\"Fence\"+1",
        })
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"UPDATE tagekyc.raw_export_source_encryption_attempts SET {assignment} WHERE \"AttemptId\"=@id",
                connection);
            command.Parameters.AddWithValue("id", unstaged.AttemptId);
            command.Parameters.AddWithValue("object", unstaged.ObjectCustodyId);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("RAW_EXPORT_SOURCE_CORE_APPEND_ONLY", error.MessageText);
        }
        await AssertUnstagedAsync(unstaged);

        await AssertExactR3ContextRejectsSiblingMutationsAsync(unstaged);
        var definition = await ReadConstraintDefinitionAsync("ck_raw_export_source_attempt_values");
        foreach (var property in StagingProperties)
            Assert.Contains($"\"{property}\"", definition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task R311_R3_function_owner_acl_role_and_table_privileges_are_exact()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.pg_get_userbyid(p.proowner),
                   COALESCE(pg_catalog.array_to_string(p.proacl,','),'')
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname='raw_export_stage_verified_source_ciphertext'
              AND pg_catalog.pg_get_function_identity_arguments(p.oid)='p_attempt_id uuid, p_object_custody_id uuid, p_expected_reservation_revision bigint, p_expected_encryption_attempt_revision bigint, p_expected_fence bigint, p_expected_object_state_revision bigint'
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("tagekyc_raw_export_deployer", reader.GetString(0));
        var acl = reader.GetString(1);
        Assert.Contains("tagekyc_raw_export_reconciler=X", acl, StringComparison.Ordinal);
        Assert.DoesNotContain("PUBLIC", acl, StringComparison.OrdinalIgnoreCase);
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();

        await using var privileges = new NpgsqlCommand("""
            SELECT
              pg_catalog.has_function_privilege(
                'tagekyc_raw_export_reconciler',
                'tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)',
                'EXECUTE'),
              pg_catalog.has_function_privilege(
                'tagekyc_runtime',
                'tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)',
                'EXECUTE'),
              (SELECT COUNT(*) FROM (VALUES
                ('tagekyc_raw_export_reconciler'),('tagekyc_runtime')) role(role_name)
               CROSS JOIN (VALUES
                ('raw_export_source_encryption_attempts'),('raw_export_source_head'),
                ('raw_export_source_reservations'),('raw_export_provisional_objects')) tab(table_name)
               CROSS JOIN (VALUES ('SELECT'),('INSERT'),('UPDATE'),('DELETE')) op(privilege)
               WHERE pg_catalog.has_table_privilege(
                 role_name,pg_catalog.format('tagekyc.%I',table_name),privilege))
            """, connection);
        await using var privilegeReader = await privileges.ExecuteReaderAsync();
        Assert.True(await privilegeReader.ReadAsync());
        Assert.True(privilegeReader.GetBoolean(0));
        Assert.False(privilegeReader.GetBoolean(1));
        Assert.Equal(0L, privilegeReader.GetInt64(2));
    }

    [Fact]
    public void R312_R3_schema_contract_result_and_logs_contain_no_raw_plaintext_digest_or_key_material()
    {
        var forbidden = new[] { "RawBytes", "PlaintextDigest", "ObjectKey", "Credential", "WrappedKey", "Dek" };
        foreach (var type in new[] { typeof(RawExportR3StageCommand), typeof(RawExportR3StageResult) })
            foreach (var property in type.GetProperties())
                Assert.DoesNotContain(forbidden, token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("<redacted>", new RawExportR3StageResult(RawExportR3StageDisposition.NotFound,
            null, null, null, null, null, null, null, null).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task R314_restart_recovery_verified_object_advances_to_Staged_without_process_result_handoff()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var source = await CreateVerifiedAsync(minio, "r314-restart"u8.ToArray(), true);
        var result = await StageAsync(source);
        Assert.Equal(RawExportR3StageDisposition.Staged, result.Disposition);
        Assert.Equal(source.ObjectCustodyId, result.ObjectCustodyId);
    }

    [Fact]
    public async Task R316_model_snapshot_catalog_and_E3_tripwire_are_synchronized()
    {
        Assert.Equal(ExpectedSnapshotSha256, Convert.ToHexString(SHA256.HashData(
            File.ReadAllBytes(ProjectPath(
                "src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs")))));
        await using var db = postgres.CreateDbContext();
        var entity = db.Model.FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportSourceEncryptionAttemptRow");
        Assert.NotNull(entity);
        foreach (var property in StagingProperties)
            Assert.NotNull(entity!.FindProperty(property));
        Assert.True(await FunctionExistsAsync());
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*) FROM pg_catalog.pg_constraint
            WHERE conname IN ('ck_raw_export_source_attempt_values','ck_raw_export_source_head_values','fk_raw_export_source_attempt_staged_object')
            """, connection);
        Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
    }

    private async Task<Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture> CreateVerifiedAsync(
        DurableObjectMinioFixture minio,
        byte[] plaintext,
        bool restartBeforeVerification = false,
        TimeSpan? sourceLifetime = null,
        TimeSpan? consentLifetime = null) =>
        await new Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(postgres)
            .CreateR3VerifiedSourceAsync(
                plaintext, minio, restartBeforeVerification, sourceLifetime, consentLifetime);

    private RawExportR3StageCommand Command(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source) =>
        new(source.ActorPrincipalId, source.AttemptId, source.ObjectCustodyId,
            source.ReservationRevision, source.EncryptionAttemptRevision, source.Fence, source.ObjectStateRevision);

    private async Task<RawExportR3StageResult> StageAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source) =>
        await StageAsync(Command(source));

    private async Task<RawExportR3StageResult> StageAsync(RawExportR3StageCommand command)
    {
        await using var db = postgres.CreateDbContext();
        return await new RawExportR3StagingService(db).StageAsync(command);
    }

    private async Task AssertUnstagedAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var db = postgres.CreateDbContext();
        var attempt = await db.RawExportSourceEncryptionAttempts.AsNoTracking()
            .SingleAsync(row => row.AttemptId == source.AttemptId);
        var head = await db.RawExportSourceHeads.AsNoTracking()
            .SingleAsync(row => row.SourceArtifactId == source.SourceArtifactId);
        Assert.Null(attempt.StagedAtUtc);
        Assert.Equal("Reserved", head.CustodyState);
        Assert.Equal(source.ReservationRevision, head.ReservationRevision);
    }

    private async Task WithdrawAuthorityAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var actor = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)", connection, transaction))
        {
            actor.Parameters.AddWithValue("actor", source.ActorPrincipalId.ToString("D"));
            await actor.ExecuteNonQueryAsync();
        }
        await using (var command = new NpgsqlCommand("""
            SELECT tagekyc.raw_export_withdraw_authority_snapshot(
              @client,@session,@acceptance,@class,@revision,@actor)
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("client", source.ClientApplicationId);
            command.Parameters.AddWithValue("session", source.VerificationSessionId);
            command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            command.Parameters.AddWithValue("class", source.RawClass);
            command.Parameters.AddWithValue("revision", source.AuthorityRevision);
            command.Parameters.AddWithValue("actor", source.ActorPrincipalId);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private async Task AssertLockWaitCrossesExpiryAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source,
        bool authorityLock)
    {
        await using var blocker = new NpgsqlConnection(postgres.ConnectionString);
        await blocker.OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        var sql = authorityLock
            ? """
              SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext(
                'tip88c1:b2-authority:'||@client::text||':'||@session::text||':'||@acceptance::text||':'||@class))
              """
            : """
              SELECT pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(
                tagekyc.raw_export_consent_scope_hash(
                  @session,@subject,@policy,@policyVersion,'SubjectRawBiometricExport',@client)))
              """;
        await using (var command = new NpgsqlCommand(sql, blocker, blockerTransaction))
        {
            command.Parameters.AddWithValue("client", source.ClientApplicationId);
            command.Parameters.AddWithValue("session", source.VerificationSessionId);
            command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            command.Parameters.AddWithValue("class", source.RawClass);
            command.Parameters.AddWithValue("subject", source.SubjectRef);
            command.Parameters.AddWithValue("policy", source.ConsentPolicyId);
            command.Parameters.AddWithValue("policyVersion", source.ConsentPolicyVersion);
            await command.ExecuteNonQueryAsync();
        }

        var staged = StageAsync(source);
        await Task.Delay(250);
        Assert.False(staged.IsCompleted);
        var wait = source.AbsoluteSourceExpiresAtUtc - DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(250);
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait);
        await blockerTransaction.CommitAsync();

        Assert.Equal(RawExportR3StageDisposition.SourceRetentionNotAuthorized, (await staged).Disposition);
        await AssertUnstagedAsync(source);
    }

    private async Task AssertAuthorityProjectionComparatorsAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        var cases = new (string Field, string Expression)[]
        {
            ("AuthoritySnapshotSchemaVersion", "2::integer"),
            ("AuthoritySnapshotId", "'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'::uuid"),
            ("ControllerIdentity", "'controller:r306-mismatch'::text"),
            ("StableDataScopeId", "'scope:r306-mismatch'::text"),
            ("ConsentPolicyId", "'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'::uuid"),
            ("ConsentPolicyVersion", "(r.\"ConsentPolicyVersion\"+1)::integer"),
            ("AbsoluteSourceExpiresAtUtc", "r.\"AbsoluteSourceExpiresAtUtc\"+interval '1 second'"),
            ("ApprovedPurpose", "'R306Mismatch'::text"),
        };
        foreach (var (field, expression) in cases)
            await AssertResolverProjectionMismatchAsync(
                source,
                "raw_export_resolve_current_authority_for_source",
                "uuid,uuid,uuid,text,timestamptz",
                BuildAuthorityResolverWrapper(field, expression));
    }

    private async Task AssertConsentProjectionComparatorsAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        var cases = new (string Field, string Expression)[]
        {
            ("State", "'NonEffective'::text"),
            ("VerificationSessionId", "'cccccccc-cccc-4ccc-8ccc-cccccccccccc'::uuid"),
            ("PolicyId", "'dddddddd-dddd-4ddd-8ddd-dddddddddddd'::uuid"),
            ("PolicyVersion", "(r.\"PolicyVersion\"+1)::integer"),
            ("PurposeCode", "'R306Mismatch'::text"),
            ("RecipientClientApplicationId", "'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee'::uuid"),
            ("RawClass", "'ChipDg2Portrait'::text"),
            ("ValidFromUtc", "pg_catalog.clock_timestamp()+interval '1 hour'"),
            ("ValidUntilUtc", "pg_catalog.clock_timestamp()-interval '1 second'"),
        };
        foreach (var (field, expression) in cases)
            await AssertResolverProjectionMismatchAsync(
                source,
                "raw_export_resolve_subject_consent_for_authorization",
                "uuid,uuid,integer",
                BuildConsentResolverWrapper(field, expression));
    }

    private async Task AssertResolverProjectionMismatchAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source,
        string functionName,
        string identityArguments,
        string wrapperSql)
    {
        var backupName = functionName + "_r306_original";
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using (var replace = new NpgsqlCommand($"""
            ALTER FUNCTION tagekyc.{functionName}({identityArguments}) RENAME TO {backupName};
            {wrapperSql}
            ALTER FUNCTION tagekyc.{functionName}({identityArguments}) OWNER TO tagekyc_raw_export_deployer;
            """, connection))
            await replace.ExecuteNonQueryAsync();
        try
        {
            Assert.Equal(
                RawExportR3StageDisposition.SourceRetentionNotAuthorized,
                (await StageAsync(source)).Disposition);
            await AssertUnstagedAsync(source);
        }
        finally
        {
            await using var restore = new NpgsqlCommand($"""
                DROP FUNCTION tagekyc.{functionName}({identityArguments});
                ALTER FUNCTION tagekyc.{backupName}({identityArguments}) RENAME TO {functionName};
                """, connection);
            await restore.ExecuteNonQueryAsync();
        }
    }

    private static string BuildAuthorityResolverWrapper(string field, string expression)
    {
        string Value(string name, string canonical) => field == name ? expression : canonical;
        return $"""
            CREATE FUNCTION tagekyc.raw_export_resolve_current_authority_for_source(
              p_client_application_id uuid,p_verification_session_id uuid,p_capture_acceptance_id uuid,
              p_raw_class text,p_evaluated_at_utc timestamptz)
            RETURNS TABLE(
              "Revision" bigint,"ValidFromUtc" timestamptz,"ValidUntilUtc" timestamptz,
              "AuthoritySnapshotSchemaVersion" integer,"AuthoritySnapshotId" uuid,"AuthorityArtifactId" uuid,
              "AuthorityArtifactVersion" integer,"ControllerIdentity" text,"ApprovedPurpose" text,
              "StableDataScopeId" text,"RetentionPolicyId" text,"RetentionPolicyVersion" integer,
              "ConsentPolicyId" uuid,"ConsentPolicyVersion" integer,"RetentionClass" text,
              "RetentionStartEvent" text,"AbsoluteSourceExpiresAtUtc" timestamptz,"ReuseDisposition" text,
              "ExtensionDisposition" text,"RevocationPolicyId" text,"PurgePolicyId" text,
              "LegalHoldPolicyId" text,"EvaluatedAtUtc" timestamptz)
            LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $r306$
              SELECT r."Revision",r."ValidFromUtc",r."ValidUntilUtc",
                {Value("AuthoritySnapshotSchemaVersion", "r.\"AuthoritySnapshotSchemaVersion\"")},
                {Value("AuthoritySnapshotId", "r.\"AuthoritySnapshotId\"")},r."AuthorityArtifactId",r."AuthorityArtifactVersion",
                {Value("ControllerIdentity", "r.\"ControllerIdentity\"")},
                {Value("ApprovedPurpose", "r.\"ApprovedPurpose\"")},
                {Value("StableDataScopeId", "r.\"StableDataScopeId\"")},r."RetentionPolicyId",r."RetentionPolicyVersion",
                {Value("ConsentPolicyId", "r.\"ConsentPolicyId\"")},
                {Value("ConsentPolicyVersion", "r.\"ConsentPolicyVersion\"")},r."RetentionClass",r."RetentionStartEvent",
                {Value("AbsoluteSourceExpiresAtUtc", "r.\"AbsoluteSourceExpiresAtUtc\"")},r."ReuseDisposition",r."ExtensionDisposition",
                r."RevocationPolicyId",r."PurgePolicyId",r."LegalHoldPolicyId",r."EvaluatedAtUtc"
              FROM tagekyc.raw_export_resolve_current_authority_for_source_r306_original(
                p_client_application_id,p_verification_session_id,p_capture_acceptance_id,p_raw_class,p_evaluated_at_utc) r
            $r306$;
            """;
    }

    private static string BuildConsentResolverWrapper(string field, string expression)
    {
        string Value(string name, string canonical) => field == name ? expression : canonical;
        return $"""
            CREATE FUNCTION tagekyc.raw_export_resolve_subject_consent_for_authorization(
              verification_session_id uuid,policy_id uuid,policy_version integer)
            RETURNS TABLE(
              "State" text,"Cause" text,"SubjectConsentRecordId" uuid,"ConsentScopeHash" bytea,
              "Revision" integer,"VerificationSessionId" uuid,"SubjectRef" text,"PolicyId" uuid,
              "PolicyVersion" integer,"PurposeCode" text,"RecipientClientApplicationId" uuid,"RawClass" text,
              "ValidFromUtc" timestamptz,"ValidUntilUtc" timestamptz,"EvaluatedAtUtc" timestamptz,
              "ConsentTextVersion" text,"ConsentTextContentHash" text,"ExternalConsentArtifactRef" text,"DecisionRef" text)
            LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $r306$
              SELECT {Value("State", "r.\"State\"")},r."Cause",r."SubjectConsentRecordId",r."ConsentScopeHash",r."Revision",
                {Value("VerificationSessionId", "r.\"VerificationSessionId\"")},r."SubjectRef",
                {Value("PolicyId", "r.\"PolicyId\"")},{Value("PolicyVersion", "r.\"PolicyVersion\"")},
                {Value("PurposeCode", "r.\"PurposeCode\"")},
                {Value("RecipientClientApplicationId", "r.\"RecipientClientApplicationId\"")},
                {Value("RawClass", "r.\"RawClass\"")},{Value("ValidFromUtc", "r.\"ValidFromUtc\"")},
                {Value("ValidUntilUtc", "r.\"ValidUntilUtc\"")},r."EvaluatedAtUtc",r."ConsentTextVersion",
                r."ConsentTextContentHash",r."ExternalConsentArtifactRef",r."DecisionRef"
              FROM tagekyc.raw_export_resolve_subject_consent_for_authorization_r306_original(
                verification_session_id,policy_id,policy_version) r
            $r306$;
            """;
    }

    private async Task AssertDeadlineComparatorAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source,
        bool absoluteSourceDeadline)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var setup = new NpgsqlCommand(
            absoluteSourceDeadline
                ? """
                  SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true);
                  ALTER TABLE tagekyc.raw_export_source_reservations DISABLE TRIGGER tr_raw_export_source_reservations_guard;
                  ALTER TABLE tagekyc.raw_export_authority_snapshots DISABLE TRIGGER tr_raw_export_authority_snapshots_append_only;
                  ALTER TABLE tagekyc.raw_export_source_reservations DROP CONSTRAINT ck_raw_export_source_reservation_values;
                  ALTER TABLE tagekyc.raw_export_authority_snapshots DROP CONSTRAINT ck_raw_export_authority_snapshot_values;
                  UPDATE tagekyc.raw_export_source_reservations SET "AbsoluteSourceExpiresAtUtc"=pg_catalog.clock_timestamp()-interval '1 second'
                    WHERE "SourceArtifactId"=@source;
                  UPDATE tagekyc.raw_export_authority_snapshots SET "AbsoluteSourceExpiresAtUtc"=pg_catalog.clock_timestamp()-interval '1 second'
                    WHERE "ClientApplicationId"=@client AND "VerificationSessionId"=@session
                      AND "CaptureAcceptanceId"=@acceptance AND "RawClass"=@class AND "EventType"='Granted';
                  """
                : """
                  SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true);
                  ALTER TABLE tagekyc.raw_export_source_reservations DISABLE TRIGGER tr_raw_export_source_reservations_guard;
                  UPDATE tagekyc.raw_export_source_reservations SET "ReservationExpiresAtUtc"=pg_catalog.clock_timestamp()-interval '1 second'
                    WHERE "SourceArtifactId"=@source;
                  """,
            connection,
            transaction))
        {
            setup.Parameters.AddWithValue("actor", source.ActorPrincipalId.ToString("D"));
            setup.Parameters.AddWithValue("source", source.SourceArtifactId);
            setup.Parameters.AddWithValue("client", source.ClientApplicationId);
            setup.Parameters.AddWithValue("session", source.VerificationSessionId);
            setup.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            setup.Parameters.AddWithValue("class", source.RawClass);
            await setup.ExecuteNonQueryAsync();
        }

        await using (var stage = new NpgsqlCommand("""
            SELECT "Outcome" FROM tagekyc.raw_export_stage_verified_source_ciphertext(
              @attempt,@object,@reservationRevision,@attemptRevision,@fence,@objectRevision)
            """, connection, transaction))
        {
            stage.Parameters.AddWithValue("attempt", source.AttemptId);
            stage.Parameters.AddWithValue("object", source.ObjectCustodyId);
            stage.Parameters.AddWithValue("reservationRevision", source.ReservationRevision);
            stage.Parameters.AddWithValue("attemptRevision", source.EncryptionAttemptRevision);
            stage.Parameters.AddWithValue("fence", source.Fence);
            stage.Parameters.AddWithValue("objectRevision", source.ObjectStateRevision);
            Assert.Equal("SourceRetentionNotAuthorized", (string)(await stage.ExecuteScalarAsync())!);
        }
        Assert.Equal(0L, await ScalarLongAsync(connection, transaction,
            "SELECT COUNT(*) FROM tagekyc.raw_export_source_encryption_attempts WHERE \"AttemptId\"=@id AND \"StagedAtUtc\" IS NOT NULL",
            source.AttemptId));
        await transaction.RollbackAsync();
        await AssertUnstagedAsync(source);
    }

    private static async Task<long> ScalarLongAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Guid id)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task RevokeKeyAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var actor = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)", connection, transaction))
        {
            actor.Parameters.AddWithValue("actor", source.ActorPrincipalId.ToString("D"));
            await actor.ExecuteNonQueryAsync();
        }
        await using (var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_revoke_attempt_key_reservation(@id,'r3-concurrency-test')",
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("id", source.AttemptKeyReservationId);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private async Task<(NpgsqlConnection Connection, NpgsqlTransaction Transaction)> BeginBlockingLockAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source,
        bool authorityLock)
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        var sql = authorityLock
            ? """
              SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext(
                'tip88c1:b2-authority:'||@client::text||':'||@session::text||':'||@acceptance::text||':'||@class))
              """
            : """
              SELECT pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(
                tagekyc.raw_export_consent_scope_hash(
                  @session,@subject,@policy,@policyVersion,'SubjectRawBiometricExport',@client)))
              """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("client", source.ClientApplicationId);
        command.Parameters.AddWithValue("session", source.VerificationSessionId);
        command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
        command.Parameters.AddWithValue("class", source.RawClass);
        command.Parameters.AddWithValue("subject", source.SubjectRef);
        command.Parameters.AddWithValue("policy", source.ConsentPolicyId);
        command.Parameters.AddWithValue("policyVersion", source.ConsentPolicyVersion);
        await command.ExecuteNonQueryAsync();
        return (connection, transaction);
    }

    private async Task WaitForBlockedStageAsync()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT EXISTS(
                  SELECT 1 FROM pg_catalog.pg_stat_activity
                  WHERE datname=current_database()
                    AND query LIKE '%raw_export_stage_verified_source_ciphertext%'
                    AND wait_event_type='Lock' AND wait_event='advisory')
                """, connection);
            if ((bool)(await command.ExecuteScalarAsync())!)
                return;
            await Task.Delay(50);
        }
        throw new TimeoutException("R3 stage did not reach the expected advisory-lock wait.");
    }

    private async Task AssertDirectSqlArgumentGuardAsync()
    {
        var validAttempt = Guid.NewGuid();
        var validObject = Guid.NewGuid();
        var cases = new[]
        {
            $"NULL::uuid,'{validObject:D}'::uuid,1,1,1,1",
            $"'00000000-0000-0000-0000-000000000000'::uuid,'{validObject:D}'::uuid,1,1,1,1",
            $"'{validAttempt:D}'::uuid,NULL::uuid,1,1,1,1",
            $"'{validAttempt:D}'::uuid,'00000000-0000-0000-0000-000000000000'::uuid,1,1,1,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,NULL::bigint,1,1,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,0,1,1,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,1,NULL::bigint,1,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,1,0,1,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,1,1,NULL::bigint,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,1,1,0,1",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,1,1,1,NULL::bigint",
            $"'{validAttempt:D}'::uuid,'{validObject:D}'::uuid,1,1,1,0",
        };

        foreach (var arguments in cases)
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"SELECT * FROM tagekyc.raw_export_stage_verified_source_ciphertext({arguments})",
                connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Equal("RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID", error.MessageText);
        }
    }

    private async Task AssertExactR3ContextRejectsSiblingMutationsAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        var completeStaging = CompleteR3StagingAssignments(source);
        var mutations = new[]
        {
            ($"UPDATE tagekyc.raw_export_source_encryption_attempts SET \"StagedCiphertextFingerprintSchemaVersion\"=2 WHERE \"AttemptId\"='{source.AttemptId:D}'::uuid", "RAW_EXPORT_SOURCE_CORE_APPEND_ONLY"),
            ($"UPDATE tagekyc.raw_export_source_encryption_attempts SET {completeStaging},\"Fence\"=\"Fence\"+1 WHERE \"AttemptId\"='{source.AttemptId:D}'::uuid", "RAW_EXPORT_SOURCE_CORE_APPEND_ONLY"),
            ($"UPDATE tagekyc.raw_export_source_encryption_attempts SET {completeStaging},\"CreatedAtUtc\"=\"CreatedAtUtc\"+INTERVAL '1 microsecond' WHERE \"AttemptId\"='{source.AttemptId:D}'::uuid", "RAW_EXPORT_SOURCE_CORE_APPEND_ONLY"),
            ($"UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Staged' WHERE \"SourceArtifactId\"='{source.SourceArtifactId:D}'::uuid", "RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN"),
            ($"UPDATE tagekyc.raw_export_source_head SET \"ReservationRevision\"=\"ReservationRevision\"+1 WHERE \"SourceArtifactId\"='{source.SourceArtifactId:D}'::uuid", "RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN"),
            ($"UPDATE tagekyc.raw_export_source_head SET \"SourceArtifactId\"=pg_catalog.gen_random_uuid(),\"CustodyState\"='Staged',\"ReservationRevision\"=\"ReservationRevision\"+1 WHERE \"SourceArtifactId\"='{source.SourceArtifactId:D}'::uuid", "RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN"),
            ($"UPDATE tagekyc.raw_export_source_head SET \"Fence\"=\"Fence\"+1,\"CustodyState\"='Staged',\"ReservationRevision\"=\"ReservationRevision\"+1 WHERE \"SourceArtifactId\"='{source.SourceArtifactId:D}'::uuid", "RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN"),
            ($"UPDATE tagekyc.raw_export_source_head SET \"CurrentEncryptionAttemptId\"=pg_catalog.gen_random_uuid(),\"CustodyState\"='Staged',\"ReservationRevision\"=\"ReservationRevision\"+1 WHERE \"SourceArtifactId\"='{source.SourceArtifactId:D}'::uuid", "RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN"),
        };

        foreach (var (mutation, expectedMessage) in mutations)
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = new NpgsqlCommand(
                $"SET LOCAL ROLE tagekyc_raw_export_deployer; SELECT pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r3-stage-v1',true); {mutation};",
                connection,
                transaction);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Equal(expectedMessage, error.MessageText);
            await transaction.RollbackAsync();
        }
    }

    private static string CompleteR3StagingAssignments(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        return $"""
            "StagedCiphertextFingerprintSchemaVersion"=2,
            "StagedCiphertextFingerprint"=decode(repeat('11',32),'hex'),
            "StagedObjectCustodyId"='{source.ObjectCustodyId:D}'::uuid,
            "StagedObjectStateRevision"={source.ObjectStateRevision},
            "StagedFromReservationRevision"={source.ReservationRevision},
            "VerifiedPlaintextLength"=1,
            "StagedCiphertextLength"={source.CiphertextLength},
            "StagedCiphertextDigest"=decode('{Convert.ToHexString(source.CiphertextDigest).ToLowerInvariant()}','hex'),
            "StagedProviderReceiptDigest"=decode('{Convert.ToHexString(source.ProviderReceiptDigest).ToLowerInvariant()}','hex'),
            "StagedVerificationEvidenceDigest"=decode('{Convert.ToHexString(source.VerificationEvidenceDigest).ToLowerInvariant()}','hex'),
            "StagedAtUtc"=pg_catalog.clock_timestamp()
            """.ReplaceLineEndings(" ");
    }

    private async Task RecreateDatabaseAtMigrationAsync(string targetMigration)
    {
        await using var db = postgres.CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.GetService<IMigrator>().MigrateAsync(targetMigration);
    }

    private async Task<string> ReadR2CatalogFingerprintAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.jsonb_build_object(
              'functions',(
                SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                  'identity',p.oid::regprocedure::text,
                  'owner',pg_catalog.pg_get_userbyid(p.proowner),
                  'acl',COALESCE(p.proacl::text,''),
                  'config',COALESCE(p.proconfig::text,''),
                  'definition',pg_catalog.pg_get_functiondef(p.oid)) ORDER BY p.proname)
                FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname IN (
                  'enforce_raw_export_source_core_write','enforce_raw_export_source_head_write')),
              'checks',(
                SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                  'name',c.conname,'definition',pg_catalog.pg_get_constraintdef(c.oid,true)) ORDER BY c.conname)
                FROM pg_catalog.pg_constraint c JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
                WHERE n.nspname='tagekyc' AND c.conname IN (
                  'ck_raw_export_source_attempt_values','ck_raw_export_source_head_values')),
              'stageFunction',to_regprocedure('tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)') IS NOT NULL,
              'stagingColumns',(SELECT COUNT(*) FROM pg_catalog.pg_attribute a
                JOIN pg_catalog.pg_class t ON t.oid=a.attrelid JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
                WHERE n.nspname='tagekyc' AND t.relname='raw_export_source_encryption_attempts'
                  AND a.attname IN ('StagedCiphertextFingerprintSchemaVersion','StagedCiphertextFingerprint',
                    'StagedObjectCustodyId','StagedObjectStateRevision','StagedFromReservationRevision',
                    'VerifiedPlaintextLength','StagedCiphertextLength','StagedCiphertextDigest',
                    'StagedProviderReceiptDigest','StagedVerificationEvidenceDigest','StagedAtUtc')
                  AND a.attnum>0 AND NOT a.attisdropped),
              'stagingFk',(SELECT COUNT(*) FROM pg_catalog.pg_constraint WHERE conname='fk_raw_export_source_attempt_staged_object'),
              'stagingIndex',(SELECT COUNT(*) FROM pg_catalog.pg_class t JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
                WHERE n.nspname='tagekyc' AND t.relname='IX_raw_export_source_encryption_attempts_StagedObjectCustodyId'))::text
            """, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> FunctionExistsAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT to_regprocedure('tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)') IS NOT NULL",
            connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> ColumnExistsAsync(string columnName)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS(
              SELECT 1 FROM pg_catalog.pg_attribute a
              JOIN pg_catalog.pg_class t ON t.oid=a.attrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
              WHERE n.nspname='tagekyc' AND t.relname='raw_export_source_encryption_attempts'
                AND a.attname=@name AND a.attnum>0 AND NOT a.attisdropped)
            """, connection);
        command.Parameters.AddWithValue("name", columnName);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> FunctionDefinitionAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.pg_get_functiondef('tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint)'::regprocedure)",
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadConstraintDefinitionAsync(string name)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.pg_get_constraintdef(c.oid,true)
            FROM pg_catalog.pg_constraint c
            JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='tagekyc' AND c.conname=@name
            """, connection);
        command.Parameters.AddWithValue("name", name);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static byte[] Sequential(byte start) => Enumerable.Range(start, 32).Select(value => (byte)value).ToArray();

    private static IEnumerable<RawExportR3FingerprintInput> Mutations(RawExportR3FingerprintInput input)
    {
        yield return input with { EncryptionAttemptFingerprint = input.EncryptionAttemptFingerprint.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() };
        yield return input with { ObjectCustodyId = Guid.Parse("22222222-2222-2222-2222-222222222223") };
        yield return input with { ObjectBindingDigest = input.ObjectBindingDigest.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() };
        yield return input with { VerifiedPlaintextLength = 4 };
        yield return input with { ContentCommitment = input.ContentCommitment.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() };
        yield return input with { StagedCiphertextLength = 463 };
        yield return input with { StagedCiphertextDigest = input.StagedCiphertextDigest.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() };
        yield return input with { StagedProviderReceiptDigest = input.StagedProviderReceiptDigest.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() };
        yield return input with { StagedVerificationEvidenceDigest = input.StagedVerificationEvidenceDigest.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() };
    }

    private static byte[] IndependentPreimage(RawExportR3FingerprintInput input)
    {
        var fields = new[]
        {
            "tip-88c1-staged-ciphertext-v2",
            Convert.ToHexString(input.EncryptionAttemptFingerprint).ToLowerInvariant(),
            input.ObjectCustodyId.ToString("N").ToLowerInvariant(),
            Convert.ToHexString(input.ObjectBindingDigest).ToLowerInvariant(),
            input.VerifiedPlaintextLength.ToString(CultureInfo.InvariantCulture),
            Convert.ToHexString(input.ContentCommitment).ToLowerInvariant(),
            input.StagedCiphertextLength.ToString(CultureInfo.InvariantCulture),
            Convert.ToHexString(input.StagedCiphertextDigest).ToLowerInvariant(),
            Convert.ToHexString(input.StagedProviderReceiptDigest).ToLowerInvariant(),
            Convert.ToHexString(input.StagedVerificationEvidenceDigest).ToLowerInvariant(),
        };
        using var stream = new MemoryStream();
        var length = new byte[4];
        foreach (var field in fields)
        {
            var bytes = Encoding.UTF8.GetBytes(field.Normalize(NormalizationForm.FormC));
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }
        Assert.Equal(489, stream.Length);
        return stream.ToArray();
    }

    private static readonly string[] StagingProperties =
    [
        "StagedCiphertextFingerprintSchemaVersion", "StagedCiphertextFingerprint",
        "StagedObjectCustodyId", "StagedObjectStateRevision", "StagedFromReservationRevision",
        "VerifiedPlaintextLength", "StagedCiphertextLength", "StagedCiphertextDigest",
        "StagedProviderReceiptDigest", "StagedVerificationEvidenceDigest", "StagedAtUtc",
    ];

    private static string ProjectPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(relative);
    }
}
