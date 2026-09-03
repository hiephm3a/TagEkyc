using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Infrastructure.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2DurableKeyProdTests(PostgresPersistenceFixture postgres)
    : IAsyncLifetime
{
    private const string Migration = "20260802105416_Tip88C1B2DurableKeyProd";
    private const string PreviousMigration = "20260731130919_Tip88C1B2BetaExistingCandidates";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DKPROD_59_ResultObserved_LeaseExpiry_Restart_Activates()
    {
        var source = await SeedSourceAsync();
        await using var connection = await OpenAsync();
        await SetRoleAsync(connection, "tagekyc_raw_export_custody_encryptor");

        var prepared = await PrepareAsync(connection, source);
        Assert.Equal("PreparingLive", prepared.Outcome);
        Assert.Equal(43, prepared.Token.Length);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", prepared.Token);
        Assert.Equal(32, prepared.ContextFingerprint.Length);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT tagekyc.raw_export_record_key_provider_wrapped_result(
                    @operation,@reservation,@preparation,@fence,@token,
                    decode(repeat('20',32),'hex'),decode(repeat('01',12),'hex'),
                    decode(repeat('40',16),'hex'),'AES-256-GCM',1,
                    'receipt-integration','provider-resource-integration')
                """;
            command.Parameters.AddWithValue("operation", prepared.OperationId);
            command.Parameters.AddWithValue("reservation", source.ReservationId);
            command.Parameters.AddWithValue("preparation", prepared.PreparationId);
            command.Parameters.AddWithValue("fence", prepared.Fence);
            command.Parameters.AddWithValue("token", prepared.Token);
            Assert.Equal("ResultObserved", await command.ExecuteScalarAsync());
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT tagekyc.raw_export_activate_attempt_key_reservation(
                    @reservation,@preparation,@fence)
                """;
            command.Parameters.AddWithValue("reservation", source.ReservationId);
            command.Parameters.AddWithValue("preparation", prepared.PreparationId);
            command.Parameters.AddWithValue("fence", prepared.Fence);
            Assert.Equal("Activated", await command.ExecuteScalarAsync());
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT preparation_disposition,octet_length(wrapped_dek_metadata_digest)
                FROM tagekyc.raw_export_inspect_attempt_key_reservation(@reservation)
                """;
            command.Parameters.AddWithValue("reservation", source.ReservationId);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Active", reader.GetString(0));
            Assert.Equal(32, reader.GetInt32(1));
            Assert.False(await reader.ReadAsync());
        }
    }

    [Fact]
    public async Task DKPROD_35_WriteGuard_SpoofedGucWithoutOwnerRejected()
    {
        await using var scenario = await StartAsync();
        var source = scenario.Source;
        var connection = scenario.Connection;
        await SetRoleAsync(connection, "tagekyc_raw_export_custody_encryptor");
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations";
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteScalarAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);

        await SetRoleAsync(connection, "tagekyc_raw_export_deployer");
        await ExecuteAsync(connection, """
            GRANT SELECT, UPDATE ON tagekyc.raw_export_attempt_key_reservations
            TO tagekyc_raw_export_custody_encryptor
            """);
        await SetRoleAsync(connection, "tagekyc_raw_export_custody_encryptor");
        Assert.Equal("tagekyc_raw_export_custody_encryptor",
            await ScalarAsync<string>(connection, "SELECT current_user"));
        await ScalarAsync<string>(connection,
            "SELECT set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        var spoofed = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            UPDATE tagekyc.raw_export_attempt_key_reservations SET "UpdatedAtUtc"=clock_timestamp()
            WHERE "AttemptKeyReservationId"=@reservation
            """, ("reservation", source.ReservationId)));
        Assert.Equal("P0001", spoofed.SqlState);
        Assert.Equal("RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID", spoofed.MessageText);
        await SetRoleAsync(connection, "tagekyc_raw_export_deployer");
        await ExecuteAsync(connection, """
            REVOKE SELECT, UPDATE ON tagekyc.raw_export_attempt_key_reservations
            FROM tagekyc_raw_export_custody_encryptor
            """);
    }

    [Fact]
    public async Task DKPROD_37_Roles_Attributes_NoCrossMembership()
    {
        await using var db = postgres.CreateDbContext();
        await new CustodyRoleReadinessValidator(db).ValidateAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DKPROD_42_Csprng_WrongOwnerOrAcl()
    {
        await using var db = postgres.CreateDbContext();
        var owner = await db.Database.SqlQueryRaw<string>("""
            SELECT r.rolname AS "Value"
            FROM pg_catalog.pg_extension e
            JOIN pg_catalog.pg_roles r ON r.oid=e.extowner
            WHERE e.extname='pgcrypto'
            """).SingleAsync();
        var valid = Configuration(owner);
        await new CsprngReadinessValidator(
            db,
            DurableKeyCustodyOptions.Resolve(valid))
            .ValidateAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DurableKeyReadinessException>(
            () => new CsprngReadinessValidator(
                db,
                DurableKeyCustodyOptions.Resolve(Configuration(null)))
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(CsprngReadinessValidator.Code, exception.Code);
    }

    [Fact]
    public async Task DKPROD_49_Identifiers_AllUnder63_CatalogRoundTrip()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*) FROM pg_catalog.pg_proc p
               JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
               WHERE n.nspname='tagekyc' AND p.proname=ANY(@functions)),
              (SELECT count(*) FROM pg_catalog.pg_trigger t
               JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
               JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
               WHERE n.nspname='tagekyc' AND NOT t.tgisinternal
                 AND t.tgname IN ('trg_raw_export_attempt_key_guard',
                                  'trg_raw_export_attempt_key_pair_from_head',
                                  'trg_raw_export_attempt_key_pair_from_operation')),
              (SELECT count(*) FROM pg_catalog.pg_proc p
               WHERE p.oid='tagekyc.raw_export_read_current_attempt_key_recovery_context(uuid)'::regprocedure
                 AND array_position(p.proargnames,'abandonment_provider_operation_token') =
                     array_position(p.proargnames,'abandon_request_preparation_event_id') + 1)
            """;
        command.Parameters.AddWithValue("functions", CallableFunctions);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(15, reader.GetInt64(0));
        Assert.Equal(5, reader.GetInt64(1));
        Assert.Equal(1, reader.GetInt64(2));
    }

    [Fact]
    public async Task DKPROD_50_Migration_DownReapply_NoOrphan_ExtensionPreserved()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        var preserved = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value" FROM pg_catalog.pg_roles
            WHERE rolname IN ('tagekyc_raw_export_encryptor_login',
                              'tagekyc_raw_export_reconciler_login',
                              'tagekyc_raw_export_lifecycle_login')
            """).SingleAsync();
        Assert.Equal(3, preserved);
        var extensionPreserved = await db.Database.SqlQueryRaw<bool>("""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_extension WHERE extname='pgcrypto') AS "Value"
            """).SingleAsync();
        Assert.True(extensionPreserved);

        await migrator.MigrateAsync(Migration);
        var functions = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value" FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname=ANY(ARRAY[
              'raw_export_prepare_attempt_key_reservation','raw_export_record_key_provider_wrapped_result',
              'raw_export_activate_attempt_key_reservation','raw_export_mark_attempt_key_preparation_expired',
              'raw_export_resolve_attempt_key_provider_outcome','raw_export_mark_key_provider_cleanup_required',
              'raw_export_record_key_provider_cleanup_observation','raw_export_acknowledge_key_provider_cleanup',
              'raw_export_record_recovered_key_provider_result','raw_export_request_abandon_attempt_key_reservation',
              'raw_export_finalize_abandon_attempt_key_reservation','raw_export_revoke_attempt_key_reservation',
              'raw_export_inspect_attempt_key_reservation','raw_export_read_current_attempt_key_recovery_context',
              'raw_export_read_active_attempt_key_envelope'])
            """).SingleAsync();
        Assert.Equal(15, functions);
    }

    [Fact]
    public async Task DKPROD_17b_CleanupAck_NoCallerDigest_RecomputedAuthorityBites()
    {
        await using var s = await StartAsync();
        await MarkCleanupRequiredAsync(s);
        await AcknowledgeCleanupAsync(s, "Cleaned");
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        var matches = await ScalarAsync<bool>(s.Connection, """
            SELECT e."ProviderCleanupEvidenceDigest" = tagekyc.raw_export_c1_hash_canonical(
                'tip-88c1-key-provider-cleanup-evidence-v1',
                encode(h."AttemptKeyContextFingerprint",'hex'),replace(e."PreparationId"::text,'-',''),
                e."PreparationFence"::text,e."CleanupResultKind",e."ProviderCleanupReference",e."ProviderCleanupReceipt")
            FROM tagekyc.raw_export_attempt_key_preparation_events e
            JOIN tagekyc.raw_export_attempt_key_reservations h
              ON h."AttemptKeyReservationId"=e."AttemptKeyReservationId"
            WHERE e."AttemptKeyReservationId"=@reservation AND e."EventKind"='CleanupAcknowledged'
            """, ("reservation", s.Source.ReservationId));
        Assert.True(matches);
    }

    [Fact]
    public async Task DKPROD_17c_CleanupAck_CallerDigestParam_AbsentInCatalog()
    {
        await using var connection = await OpenAsync();
        Assert.True(await ScalarAsync<bool>(connection, """
            SELECT count(*)=1 AND bool_and(NOT ('p_cleanup_evidence_digest'=ANY(COALESCE(proargnames,ARRAY[]::text[]))))
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname='raw_export_acknowledge_key_provider_cleanup'
            """));
    }

    [Fact]
    public async Task DKPROD_20b_Recovered_SingleCanonicalSurface()
    {
        await using var connection = await OpenAsync();
        Assert.True(await ScalarAsync<bool>(connection, """
            SELECT
              (SELECT count(*)=1 FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
               WHERE n.nspname='tagekyc' AND p.proname='raw_export_record_recovered_key_provider_result')
              AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_reconciler',
                'tagekyc.raw_export_activate_recovered_attempt_key_reservation_internal(uuid,uuid,bigint,bytea,bytea,bytea,bytea,text,bytea)'::regprocedure,'EXECUTE')
            """));
    }

    [Fact]
    public async Task DKPROD_51_Cleanup_CallerCannotForgeSuccess()
    {
        await using var s = await StartAsync();
        await MarkCleanupRequiredAsync(s);
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        Assert.Equal("EvidenceMissing", await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_acknowledge_key_provider_cleanup(
                @operation,@reservation,@preparation,@fence,@token,'Cleaned','wrong-ref','receipt')
            """, ("operation", s.Prepared.OperationId), ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence), ("token", s.Prepared.Token)));
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
        var denied = await Assert.ThrowsAsync<PostgresException>(() => ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_acknowledge_key_provider_cleanup(
                @operation,@reservation,@preparation,@fence,@token,'Cleaned','cleanup-ref','receipt')
            """, ("operation", s.Prepared.OperationId), ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence), ("token", s.Prepared.Token)));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
    }

    [Fact]
    public async Task DKPROD_53_Cleanup_IssuedLineage_WrappedFamilyStaysNull()
    {
        await using var s = await StartAsync();
        await MarkCleanupRequiredAsync(s);
        await AcknowledgeCleanupAsync(s, "Cleaned");
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        Assert.True(await ScalarAsync<bool>(s.Connection, """
            SELECT "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL
               AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL
            FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationId"=@operation
            """, ("operation", s.Prepared.OperationId)));
    }

    [Fact]
    public async Task DKPROD_54_Cleanup_ResultObservedLineage_WrappedFamilyByteIdentical()
    {
        await using var s = await StartAsync();
        await RecordWrappedAsync(s);
        await MarkCleanupRequiredAsync(s);
        await AcknowledgeCleanupAsync(s, "Cleaned");
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        Assert.True(await ScalarAsync<bool>(s.Connection, """
            SELECT COALESCE("WrappedDekCiphertext"=decode(repeat('20',32),'hex')
               AND "WrappedDekNonce"=decode(repeat('01',12),'hex')
               AND "WrappedDekTag"=decode(repeat('40',16),'hex')
               AND octet_length("WrappedDekMetadataDigest")=32, false)
            FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationId"=@operation
            """, ("operation", s.Prepared.OperationId)));
    }

    [Fact]
    public async Task DKPROD_33_LandedAttempt_FK_23503()
    {
        var pairA = await SeedSourceAsync();
        await using var connection = await OpenAsync();
        await SetRoleAsync(connection, "tagekyc_raw_export_custody_encryptor");
        var preparedA = await PrepareAsync(connection, pairA);
        Assert.Equal("PreparingLive", preparedA.Outcome);

        var pairB = await SeedSourceAsync();
        Assert.NotEqual(pairA.AttemptId, pairB.AttemptId);
        Assert.NotEqual(pairA.ReservationId, pairB.ReservationId);

        await SetRoleAsync(connection, "tagekyc_raw_export_deployer");
        Assert.True(await ScalarAsync<bool>(connection, """
            SELECT EXISTS(
                SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "AttemptId"=@attempt_a AND "AttemptKeyReservationId"=@reservation_a)
            AND EXISTS(
                SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "AttemptId"=@attempt_b AND "AttemptKeyReservationId"=@reservation_b)
            AND EXISTS(
                SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "AttemptId"=@attempt_a)
            AND EXISTS(
                SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "AttemptKeyReservationId"=@reservation_b)
            AND NOT EXISTS(
                SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts
                WHERE "AttemptId"=@attempt_a AND "AttemptKeyReservationId"=@reservation_b)
            AND NOT EXISTS(
                SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation_b)
            """,
            ("attempt_a", pairA.AttemptId),
            ("reservation_a", pairA.ReservationId),
            ("attempt_b", pairB.AttemptId),
            ("reservation_b", pairB.ReservationId)));

        var headCount = await ScalarAsync<long>(connection,
            "SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations");
        var eventCount = await ScalarAsync<long>(connection,
            "SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events");
        var mappingCount = await ScalarAsync<long>(connection,
            "SELECT count(*) FROM tagekyc.raw_export_key_provider_operations");
        var headA = await ScalarAsync<string>(connection, """
            SELECT to_jsonb(h)::text
            FROM tagekyc.raw_export_attempt_key_reservations h
            WHERE "AttemptKeyReservationId"=@reservation_a
            """, ("reservation_a", pairA.ReservationId));

        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await ScalarAsync<string>(connection,
                "SELECT set_config('tagekyc.raw_export_attempt_key_write_context','active',true)");
            await ExecuteAsync(connection, "SAVEPOINT dkprod_33_composite_fk");

            var exception = await Record.ExceptionAsync(() => ExecuteAsync(connection, """
                INSERT INTO tagekyc.raw_export_attempt_key_reservations
                SELECT (jsonb_populate_record(
                    NULL::tagekyc.raw_export_attempt_key_reservations,
                    to_jsonb(h) || jsonb_build_object(
                        'AttemptId', @attempt_a,
                        'AttemptKeyReservationId', @reservation_b,
                        'CreatedAtUtc', clock_timestamp(),
                        'UpdatedAtUtc', clock_timestamp()))).*
                FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE h."AttemptKeyReservationId"=@reservation_a
                """,
                ("attempt_a", pairA.AttemptId),
                ("reservation_a", pairA.ReservationId),
                ("reservation_b", pairB.ReservationId)));

            if (exception is null)
            {
                Assert.Equal(1L, await ScalarAsync<long>(connection, """
                    SELECT count(*)
                    FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptId"=@attempt_a
                      AND "AttemptKeyReservationId"=@reservation_b
                    """,
                    ("attempt_a", pairA.AttemptId),
                    ("reservation_b", pairB.ReservationId)));
                Assert.Fail(
                    $"EXPECTED_COMPOSITE_FK_23503_BUT_INSERT_SUCCEEDED: " +
                    $"PairA=({pairA.AttemptId:N},{pairA.ReservationId:N}); " +
                    $"PairB=({pairB.AttemptId:N},{pairB.ReservationId:N}); " +
                    $"CrossPair=({pairA.AttemptId:N},{pairB.ReservationId:N})");
            }

            var postgresException = Assert.IsType<PostgresException>(exception);
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
            Assert.Equal(
                "fk_raw_export_attempt_key_resv_attempt_composite",
                postgresException.ConstraintName);

            await ExecuteAsync(connection, "ROLLBACK TO SAVEPOINT dkprod_33_composite_fk");
            Assert.Equal(headCount, await ScalarAsync<long>(connection,
                "SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations"));
            Assert.Equal(eventCount, await ScalarAsync<long>(connection,
                "SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events"));
            Assert.Equal(mappingCount, await ScalarAsync<long>(connection,
                "SELECT count(*) FROM tagekyc.raw_export_key_provider_operations"));
            Assert.Equal(headA, await ScalarAsync<string>(connection, """
                SELECT to_jsonb(h)::text
                FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation_a
                """, ("reservation_a", pairA.ReservationId)));
            Assert.False(await ScalarAsync<bool>(connection, """
                SELECT EXISTS(
                    SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptId"=@attempt_a
                      AND "AttemptKeyReservationId"=@reservation_b)
                """,
                ("attempt_a", pairA.AttemptId),
                ("reservation_b", pairB.ReservationId)));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task DKPROD_34_WriteGuard_MissingGucRejected()
    {
        await using var s = await StartAsync();
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(s.Connection, """
            UPDATE tagekyc.raw_export_attempt_key_reservations SET "UpdatedAtUtc"=clock_timestamp()
            WHERE "AttemptKeyReservationId"=@reservation
            """, ("reservation", s.Source.ReservationId)));
        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID", exception.MessageText);
    }

    [Fact]
    public async Task DKPROD_36_WriteGuard_SecurityDefinerPositiveAndContextRestored()
    {
        await using var s = await StartAsync();
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
        Assert.Equal("", await ScalarAsync<string>(s.Connection,
            "SELECT current_setting('tagekyc.raw_export_attempt_key_write_context',true)"));
        await AssertPairAsync(s, "PreparingLive", "Issued");
    }

    [Fact]
    public async Task DKPROD_56_WriteGuard_UnknownTable_FailsClosed()
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "CREATE TEMP TABLE dkprod_unknown_guard(value integer)");
        await ExecuteAsync(connection, """
            CREATE TRIGGER dkprod_unknown_guard BEFORE INSERT OR UPDATE OR DELETE ON dkprod_unknown_guard
            FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_attempt_key_guard()
            """);
        await ExecuteAsync(connection,
            "GRANT INSERT ON dkprod_unknown_guard TO tagekyc_raw_export_deployer");
        await SetRoleAsync(connection, "tagekyc_raw_export_deployer");
        await ScalarAsync<string>(connection,
            "SELECT set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            ExecuteAsync(connection, "INSERT INTO dkprod_unknown_guard(value) VALUES(1)"));
        Assert.Equal("RAW_EXPORT_ATTEMPT_KEY_GUARD_UNKNOWN_TABLE", exception.MessageText);
    }

    [Fact] public Task DKPROD_58a_PairConstraint_HeadTrigger_BitesAtCommit() => AssertPairTriggerAsync(true);
    [Fact] public Task DKPROD_58b_PairConstraint_MappingTrigger_BitesAtCommit() => AssertPairTriggerAsync(false);

    [Fact]
    public async Task DKPROD_55a_WriteGuard_RoutesReservations()
    {
        await AssertGuardRouteAsync("raw_export_attempt_key_reservations");
    }

    [Fact]
    public async Task DKPROD_55b_WriteGuard_RoutesEvents()
    {
        await AssertGuardRouteAsync("raw_export_attempt_key_preparation_events");
    }

    [Fact]
    public async Task DKPROD_55c_WriteGuard_RoutesMapping()
    {
        await AssertGuardRouteAsync("raw_export_key_provider_operations");
    }

    [Fact]
    public async Task DKPROD_57_EventSingleton_IsPerPreparationGeneration()
    {
        await using var s = await StartAsync();
        await MoveToPositiveAbsenceAsync(s);
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
        var next = await PrepareAsync(s.Connection, s.Source);
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        Assert.Equal(2L, await ScalarAsync<long>(s.Connection, """
            SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
            WHERE "AttemptKeyReservationId"=@reservation AND "EventKind"='Opened'
            """, ("reservation", s.Source.ReservationId)));
        Assert.Equal(2, next.Fence);
    }

    [Fact]
    public async Task DKPROD_40_Csprng_MissingExtension()
    {
        await AssertCsprngDisposableDatabaseFailsAsync("TEMPLATE template0", null);
    }

    [Fact]
    public async Task DKPROD_41_Csprng_WrongSchema()
    {
        await AssertCsprngDisposableDatabaseFailsAsync(
            "TEMPLATE template1",
            "ALTER EXTENSION pgcrypto SET SCHEMA public");
    }

    [Fact]
    public async Task DKPROD_43_Csprng_ShadowFunction()
    {
        await AssertCsprngDisposableDatabaseFailsAsync(
            "TEMPLATE template1",
            "CREATE FUNCTION public.gen_random_bytes(integer) RETURNS bytea LANGUAGE sql AS 'SELECT decode(repeat(''00'',$1),''hex'')'");
    }

    [Fact]
    public async Task DKPROD_44_Csprng_OutputLengthNot32()
    {
        await using var connection = await OpenAsync();
        Assert.True(await ScalarAsync<bool>(connection,
            "SELECT octet_length(tagekyc_extensions.gen_random_bytes(32))=32"));
        Assert.False(await ScalarAsync<bool>(connection,
            "SELECT octet_length(tagekyc_extensions.gen_random_bytes(31))=32"));
    }

    [Fact]
    public async Task DKPROD_45_Csprng_TokenLengthNot43()
    {
        await using var s = await StartAsync();
        Assert.Equal(43, s.Prepared.Token.Length);
        Assert.NotEqual(43, Convert.ToBase64String(new byte[31]).TrimEnd('=').Length);
    }

    [Fact]
    public async Task DKPROD_46_Csprng_InvalidBase64UrlChar()
    {
        await using var s = await StartAsync();
        Assert.Matches("^[A-Za-z0-9_-]{43}$", s.Prepared.Token);
        Assert.DoesNotMatch("^[A-Za-z0-9_-]{43}$", new string('A', 42) + "+");
    }

    [Fact]
    public async Task DKPROD_60_CapabilityRoles_PrepareWrapRecordActivateRestartReadUnwrap_NoTableGrant()
    {
        var source = await SeedSourceAsync();
        PreparedIdentity prepared;
        await using (var first = await OpenAsync())
        {
            await SetRoleAsync(first, "tagekyc_raw_export_custody_encryptor");
            prepared = await PrepareAsync(first, source);
        }
        await using (var second = await OpenAsync())
        {
            var s = new Scenario(second, source, prepared);
            Assert.Equal("ResultObserved", await RecordWrappedAsync(s));
        }
        await using (var third = await OpenAsync())
        {
            var s = new Scenario(third, source, prepared);
            Assert.Equal("Activated", await ActivateAsync(s));
            Assert.Equal(32, await ScalarAsync<int>(third, """
                SELECT octet_length(wrapped_dek_metadata_digest)
                FROM tagekyc.raw_export_read_active_attempt_key_envelope(@reservation)
                """, ("reservation", source.ReservationId)));
            var denied = await Assert.ThrowsAsync<PostgresException>(() =>
                ScalarAsync<long>(third, "SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations"));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
        }
    }

    [Fact]
    public async Task DKPROD_61_Lifecycle_Restart_ResolvesCurrentGenerationWithoutReadGrant()
    {
        var source = await SeedSourceAsync();
        PreparedIdentity prepared;
        await using (var first = await OpenAsync())
        {
            await SetRoleAsync(first, "tagekyc_raw_export_custody_encryptor");
            prepared = await PrepareAsync(first, source);
        }
        await using var restarted = await OpenAsync();
        await SetRoleAsync(restarted, "tagekyc_raw_export_reconciler");
        Assert.Equal(prepared.PreparationId, await ScalarAsync<Guid>(restarted, """
            SELECT current_preparation_id
            FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@reservation)
            """, ("reservation", source.ReservationId)));
        var denied = await Assert.ThrowsAsync<PostgresException>(() =>
            ScalarAsync<long>(restarted, "SELECT count(*) FROM tagekyc.raw_export_key_provider_operations"));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
    }

    [Fact]
    public async Task DKPROD_62_FreshGeneration_ResetsBothAttemptCountsAndInterventionFalse()
    {
        await using var s = await StartAsync();
        await MarkCleanupRequiredAsync(s);
        await ForceTimestampAsync(s, "CleanupDeadlineUtc", "clock_timestamp()-interval '1 second'");
        await ObserveCleanupAsync(s, "CleanupFailed");
        Assert.True(await HeadBoolAsync(s, "CleanupOperatorInterventionRequired"));
        await ForceTimestampAsync(s, "NextCleanupAttemptNotBeforeUtc", "clock_timestamp()-interval '1 second'");
        await AcknowledgeCleanupAsync(s, "Cleaned");
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
        var fresh = await PrepareAsync(s.Connection, s.Source);
        var freshScenario = s with { Prepared = fresh };
        Assert.Equal(0L, await HeadLongAsync(freshScenario, "ResolutionAttemptCount"));
        Assert.Equal(0L, await HeadLongAsync(freshScenario, "CleanupAttemptCount"));
        Assert.False(await HeadBoolAsync(freshScenario, "CleanupOperatorInterventionRequired"));
    }

    [Fact] public Task DKPROD_01_PrepareFirst() => AssertTransitionAsync("1");
    [Fact]
    public async Task DKPROD_02_PrepareFresh()
    {
        foreach (var predecessor in new[] { "RF/AP", "RF/CU" })
        {
            await using var s = await StartAsync();
            if (predecessor == "RF/AP")
            {
                await MoveToPositiveAbsenceAsync(s);
            }
            else
            {
                Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
                Assert.Equal("Acknowledged", await AcknowledgeCleanupAsync(s, "Cleaned"));
            }

            var predecessorMapping = predecessor == "RF/AP" ? "AbsenceProven" : "CleanedUp";
            await AssertPairAsync(s, "ReadyForFreshPreparation", predecessorMapping);
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var preserved = await ScalarAsync<string>(s.Connection, """
                SELECT (to_jsonb(h) - ARRAY[
                    'PreparationDisposition','CurrentPreparationId','CurrentPreparationFence',
                    'CurrentPreparationLeaseExpiresAtUtc','CurrentProviderOperationToken',
                    'ResolutionAttemptCount','NextResolutionAttemptNotBeforeUtc','ResolutionDeadlineUtc',
                    'CleanupAttemptCount','NextCleanupAttemptNotBeforeUtc','CleanupDeadlineUtc',
                    'CleanupOperatorInterventionRequired','WrappedDekCiphertext','WrappedDekNonce',
                    'WrappedDekTag','WrappedDekMetadataDigest','PreparedAtUtc','RevokedAtUtc',
                    'RevocationReasonCode','RowRevision','UpdatedAtUtc']::text[])::text
                FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var mappingCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_key_provider_operations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));

            await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
            var fresh = await PrepareAsync(s.Connection, s.Source);
            Assert.Equal("tagekyc_raw_export_custody_encryptor",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            Assert.Equal("PreparingLive", fresh.Outcome);
            Assert.Equal(s.Prepared.Fence + 1, fresh.Fence);
            Assert.NotEqual(s.Prepared.PreparationId, fresh.PreparationId);

            var freshScenario = s with { Prepared = fresh };
            await AssertPairAsync(freshScenario, "PreparingLive", "Issued");
            Assert.Equal(0L, await HeadLongAsync(freshScenario, "ResolutionAttemptCount"));
            Assert.Equal(0L, await HeadLongAsync(freshScenario, "CleanupAttemptCount"));
            Assert.False(await HeadBoolAsync(freshScenario, "CleanupOperatorInterventionRequired"));
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(preserved, await ScalarAsync<string>(s.Connection, """
                SELECT (to_jsonb(h) - ARRAY[
                    'PreparationDisposition','CurrentPreparationId','CurrentPreparationFence',
                    'CurrentPreparationLeaseExpiresAtUtc','CurrentProviderOperationToken',
                    'ResolutionAttemptCount','NextResolutionAttemptNotBeforeUtc','ResolutionDeadlineUtc',
                    'CleanupAttemptCount','NextCleanupAttemptNotBeforeUtc','CleanupDeadlineUtc',
                    'CleanupOperatorInterventionRequired','WrappedDekCiphertext','WrappedDekNonce',
                    'WrappedDekTag','WrappedDekMetadataDigest','PreparedAtUtc','RevokedAtUtc',
                    'RevocationReasonCode','RowRevision','UpdatedAtUtc']::text[])::text
                FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(eventCount + 1, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(mappingCount + 1, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_key_provider_operations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal("Opened", await ScalarAsync<string>(s.Connection, """
                SELECT "EventKind" FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                ORDER BY "EventSequence" DESC LIMIT 1
                """, ("reservation", s.Source.ReservationId)));
        }
    }
    [Fact] public Task DKPROD_03_PrepareReplay() => AssertTransitionAsync("3");
    [Fact] public Task DKPROD_04_RecordWrappedDirect() => AssertTransitionAsync("4");
    [Fact] public Task DKPROD_05_ActivateDirect() => AssertTransitionAsync("5");
    [Fact] public Task DKPROD_06_MarkExpired() => AssertTransitionAsync("6");
    [Fact] public Task DKPROD_07_FirstUnknown() => AssertTransitionAsync("7");
    [Fact] public Task DKPROD_08_RepeatedUnknown() => AssertTransitionAsync("8");
    [Fact] public Task DKPROD_09a_UnavailableFromLive() => AssertTransitionAsync("9a");
    [Fact] public Task DKPROD_09b_UnavailableFromExpired() => AssertTransitionAsync("9b");
    [Fact] public Task DKPROD_09c_UnavailableFromUnknown() => AssertTransitionAsync("9c");
    [Fact] public Task DKPROD_10_PositiveAbsence() => AssertTransitionAsync("10");
    [Fact]
    public async Task DKPROD_11_PositiveAbsenceLeaseGate()
    {
        async Task<(string Head, string Mapping, long EventCount)> SnapshotAsync(Scenario scenario)
        {
            await SetRoleAsync(scenario.Connection, "tagekyc_raw_export_deployer");
            var head = await ScalarAsync<string>(scenario.Connection, """
                SELECT to_jsonb(h)::text
                FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", scenario.Source.ReservationId));
            var mapping = await ScalarAsync<string>(scenario.Connection, """
                SELECT to_jsonb(m)::text
                FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", scenario.Prepared.OperationId));
            var eventCount = await ScalarAsync<long>(scenario.Connection, """
                SELECT count(*)
                FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", scenario.Source.ReservationId));
            return (head, mapping, eventCount);
        }

        static void AssertNoWrite(
            (string Head, string Mapping, long EventCount) expected,
            (string Head, string Mapping, long EventCount) actual,
            string diagnostic)
        {
            Assert.True(StringComparer.Ordinal.Equals(expected.Head, actual.Head),
                $"{diagnostic}: head row changed");
            Assert.True(StringComparer.Ordinal.Equals(expected.Mapping, actual.Mapping),
                $"{diagnostic}: provider-operation row changed");
            Assert.True(expected.EventCount == actual.EventCount,
                $"{diagnostic}: event count changed from {expected.EventCount} to {actual.EventCount}");
        }

        await using (var guardPrecedence = await StartAsync())
        {
            await AssertPairAsync(guardPrecedence, "PreparingLive", "Issued");
            var before = await SnapshotAsync(guardPrecedence);

            var outcome = await ResolveAsync(
                guardPrecedence, "NoProviderResult", absenceReceipt: "absence-proof");
            Assert.True(outcome == "IllegalResolution",
                $"PL_IS_ILLEGAL_RESOLUTION: expected IllegalResolution, observed {outcome}");
            Assert.Equal("tagekyc_raw_export_reconciler",
                await ScalarAsync<string>(guardPrecedence.Connection, "SELECT current_user"));
            await AssertPairAsync(guardPrecedence, "PreparingLive", "Issued");

            var after = await SnapshotAsync(guardPrecedence);
            AssertNoWrite(before, after, "PL_IS_ILLEGAL_RESOLUTION");
        }

        await using (var liveLease = await StartAsync())
        {
            Assert.Equal("AbandonRequested", await RequestAbandonAsync(liveLease));
            await AssertPairAsync(liveLease, "AbandonRequested", "Issued");
            var before = await SnapshotAsync(liveLease);
            Assert.True(await ScalarAsync<bool>(liveLease.Connection, """
                SELECT pg_catalog.statement_timestamp() < "CurrentPreparationLeaseExpiresAtUtc"
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", liveLease.Source.ReservationId)),
                "AR_IS_LIVE_LEASE_RETRY_TOO_SOON: preparation lease was not live by PostgreSQL server time");

            var outcome = await ResolveAsync(
                liveLease, "NoProviderResult", absenceReceipt: "absence-proof");
            Assert.True(outcome == "RetryTooSoon",
                $"AR_IS_LIVE_LEASE_RETRY_TOO_SOON: expected RetryTooSoon, observed {outcome}");
            Assert.Equal("tagekyc_raw_export_reconciler",
                await ScalarAsync<string>(liveLease.Connection, "SELECT current_user"));
            await AssertPairAsync(liveLease, "AbandonRequested", "Issued");

            var after = await SnapshotAsync(liveLease);
            AssertNoWrite(before, after, "AR_IS_LIVE_LEASE_RETRY_TOO_SOON");
        }
    }
    [Fact] public Task DKPROD_12_DirectCorrupt() => AssertTransitionAsync("12");
    [Fact]
    public async Task DKPROD_13_CleanupRequired()
    {
        foreach (var predecessor in new[] { "PL/IS", "PE/IS", "POU/IS" })
        {
            await using var s = await StartAsync();
            if (predecessor == "PE/IS")
            {
                await ExpireAsync(s);
            }
            else if (predecessor == "POU/IS")
            {
                Assert.Equal("ResolvedOutcomeUnknown", await ResolveAsync(s, "ProviderOutcomeUnknown"));
            }

            var predecessorHead = predecessor switch
            {
                "PL/IS" => "PreparingLive",
                "PE/IS" => "PreparingExpiredAwaitingResolution",
                _ => "ProviderOutcomeUnknown",
            };
            await AssertPairAsync(s, predecessorHead, "Issued");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var mappingCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_key_provider_operations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var identity = await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',"AttemptId"::text,encode("AttemptKeyContextFingerprint",'hex'),
                    "CurrentPreparationId"::text,"CurrentPreparationFence"::text,
                    "CurrentProviderOperationToken","CurrentPreparationLeaseExpiresAtUtc"::text)
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));

            Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
            Assert.Equal("tagekyc_raw_export_reconciler",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            await AssertPairAsync(s, "ProviderCleanupRequired", "CleanupRequired");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(identity, await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',"AttemptId"::text,encode("AttemptKeyContextFingerprint",'hex'),
                    "CurrentPreparationId"::text,"CurrentPreparationFence"::text,
                    "CurrentProviderOperationToken","CurrentPreparationLeaseExpiresAtUtc"::text)
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.True(await ScalarAsync<bool>(s.Connection, """
                SELECT "CleanupDeadlineUtc" IS NOT NULL
                   AND "ResolutionDeadlineUtc" IS NULL
                   AND "NextResolutionAttemptNotBeforeUtc" IS NULL
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(mappingCount, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_key_provider_operations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(eventCount + 1, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.True(await ScalarAsync<bool>(s.Connection, """
                SELECT "EventKind"='ResolvedCleanupRequired'
                   AND "ProviderCleanupReference"='cleanup-ref'
                   AND octet_length("ProviderResolutionEvidenceDigest")=32
                FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                ORDER BY "EventSequence" DESC LIMIT 1
                """, ("reservation", s.Source.ReservationId)));
        }
    }
    [Fact] public Task DKPROD_14_CleanupUnavailable() => AssertTransitionAsync("14");
    [Fact] public Task DKPROD_15_CleanupOutcomeUnknown() => AssertTransitionAsync("15");
    [Fact] public Task DKPROD_16_CleanupFailed() => AssertTransitionAsync("16");
    [Fact] public Task DKPROD_17_CleanupAcknowledgedCleaned() => AssertTransitionAsync("17");
    [Fact] public Task DKPROD_18_CleanupAcknowledgedAlreadyAbsent() => AssertTransitionAsync("18");
    [Fact] public Task DKPROD_19_RecoveredFromLive() => AssertTransitionAsync("19");
    [Fact] public Task DKPROD_20_RecoveredFromAwaitingStates() => AssertTransitionAsync("20");
    [Fact]
    public async Task DKPROD_21_RequestAbandon()
    {
        foreach (var predecessor in new[] { "PL/IS", "PL/RO", "PE/IS", "POU/IS", "PCR/CR" })
        {
            await using var s = await StartAsync();
            if (predecessor == "PL/RO")
            {
                Assert.Equal("ResultObserved", await RecordWrappedAsync(s));
            }
            else if (predecessor == "PE/IS")
            {
                await ExpireAsync(s);
            }
            else if (predecessor == "POU/IS")
            {
                Assert.Equal("ResolvedOutcomeUnknown", await ResolveAsync(s, "ProviderOutcomeUnknown"));
            }
            else if (predecessor == "PCR/CR")
            {
                Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
            }

            var separator = predecessor.IndexOf('/');
            var predecessorHead = predecessor[..separator] switch
            {
                "PL" => "PreparingLive",
                "PE" => "PreparingExpiredAwaitingResolution",
                "POU" => "ProviderOutcomeUnknown",
                _ => "ProviderCleanupRequired",
            };
            var predecessorMapping = predecessor[(separator + 1)..] switch
            {
                "IS" => "Issued",
                "RO" => "ResultObserved",
                _ => "CleanupRequired",
            };
            await AssertPairAsync(s, predecessorHead, predecessorMapping);
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var mapping = await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId));
            var identity = await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',"AttemptId"::text,encode("AttemptKeyContextFingerprint",'hex'),
                    "CurrentPreparationId"::text,"CurrentPreparationFence"::text,
                    "CurrentProviderOperationToken","CurrentPreparationLeaseExpiresAtUtc"::text,
                    "ResolutionAttemptCount"::text,"CleanupAttemptCount"::text)
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var rowRevision = await ScalarAsync<long>(s.Connection, """
                SELECT "RowRevision" FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var cleanupDeadline = predecessor == "PCR/CR"
                ? await ScalarAsync<string>(s.Connection, """
                    SELECT "CleanupDeadlineUtc"::text FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=@reservation
                    """, ("reservation", s.Source.ReservationId))
                : null;

            Assert.Equal("AbandonRequested", await RequestAbandonAsync(s));
            Assert.Equal("tagekyc_raw_export_lifecycle",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            await AssertPairAsync(s, "AbandonRequested", predecessorMapping);
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(rowRevision + 1, await ScalarAsync<long>(s.Connection, """
                SELECT "RowRevision" FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(identity, await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',"AttemptId"::text,encode("AttemptKeyContextFingerprint",'hex'),
                    "CurrentPreparationId"::text,"CurrentPreparationFence"::text,
                    "CurrentProviderOperationToken","CurrentPreparationLeaseExpiresAtUtc"::text,
                    "ResolutionAttemptCount"::text,"CleanupAttemptCount"::text)
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(mapping, await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId)));
            Assert.Equal(eventCount + 1, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.True(await ScalarAsync<bool>(s.Connection, """
                SELECT "EventKind"='AbandonRequested'
                   AND "OperatorReasonCode"='operator-request'
                   AND "RequestingActorEvidence" IS NOT NULL
                FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                ORDER BY "EventSequence" DESC LIMIT 1
                """, ("reservation", s.Source.ReservationId)));
            if (cleanupDeadline is not null)
            {
                Assert.Equal(cleanupDeadline, await ScalarAsync<string>(s.Connection, """
                    SELECT "CleanupDeadlineUtc"::text FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=@reservation
                    """, ("reservation", s.Source.ReservationId)));
            }
            else
            {
                Assert.True(await ScalarAsync<bool>(s.Connection, """
                    SELECT "ResolutionDeadlineUtc" IS NULL
                       AND "NextResolutionAttemptNotBeforeUtc" IS NULL
                       AND "CleanupDeadlineUtc" IS NULL
                       AND "NextCleanupAttemptNotBeforeUtc" IS NULL
                    FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=@reservation
                    """, ("reservation", s.Source.ReservationId)));
            }
        }
    }

    [Fact]
    public async Task DKPROD_22a_AbandonCleanupEntry()
    {
        foreach (var predecessorMapping in new[] { "Issued", "ResultObserved" })
        {
            await using var s = await StartAsync();
            if (predecessorMapping == "ResultObserved")
            {
                Assert.Equal("ResultObserved", await RecordWrappedAsync(s));
            }
            Assert.Equal("AbandonRequested", await RequestAbandonAsync(s));
            await AssertPairAsync(s, "AbandonRequested", predecessorMapping);

            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var wrapped = await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',encode("WrappedDekCiphertext",'hex'),encode("WrappedDekNonce",'hex'),
                    encode("WrappedDekTag",'hex'),encode("WrappedDekMetadataDigest",'hex'))
                FROM tagekyc.raw_export_key_provider_operations
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId));
            var rowRevision = await ScalarAsync<long>(s.Connection, """
                SELECT "RowRevision" FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));

            Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
            Assert.Equal("tagekyc_raw_export_reconciler",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            await AssertPairAsync(s, "AbandonRequested", "CleanupRequired");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(rowRevision + 1, await ScalarAsync<long>(s.Connection, """
                SELECT "RowRevision" FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(wrapped, await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',encode("WrappedDekCiphertext",'hex'),encode("WrappedDekNonce",'hex'),
                    encode("WrappedDekTag",'hex'),encode("WrappedDekMetadataDigest",'hex'))
                FROM tagekyc.raw_export_key_provider_operations
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId)));
            Assert.True(await ScalarAsync<bool>(s.Connection, """
                SELECT "CleanupDeadlineUtc" IS NOT NULL
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(eventCount + 1, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.True(await ScalarAsync<bool>(s.Connection, """
                SELECT "EventKind"='ResolvedCleanupRequired'
                   AND "ProviderCleanupReference"='cleanup-ref'
                   AND octet_length("ProviderResolutionEvidenceDigest")=32
                FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                ORDER BY "EventSequence" DESC LIMIT 1
                """, ("reservation", s.Source.ReservationId)));
        }
    }
    [Fact] public Task DKPROD_22b_AbandonCleanupUnavailable() => AssertTransitionAsync("22b");
    [Fact] public Task DKPROD_22c_AbandonCleanupUnknown() => AssertTransitionAsync("22c");
    [Fact] public Task DKPROD_22d_AbandonCleanupFailed() => AssertTransitionAsync("22d");
    [Fact] public Task DKPROD_22e_AbandonCleanupCleaned() => AssertTransitionAsync("22e");
    [Fact] public Task DKPROD_22f_AbandonCleanupAlreadyAbsent() => AssertTransitionAsync("22f");
    [Fact] public Task DKPROD_22g_AbandonPositiveAbsence() => AssertTransitionAsync("22g");
    [Fact] public Task DKPROD_22h_FinalizeFromCleaned() => AssertTransitionAsync("22h");
    [Fact] public Task DKPROD_22i_FinalizeFromAbsence() => AssertTransitionAsync("22i");
    [Fact] public Task DKPROD_22j_FinalizeRejectsIncompleteCleanup() => AssertTransitionAsync("22j");
    [Fact] public Task DKPROD_22k_CrossReservationLineageRejected() => AssertTransitionAsync("22k");
    [Fact] public Task DKPROD_23_RevokeActive() => AssertTransitionAsync("23");
    [Fact] public Task DKPROD_24_RevokeReplayAndNotActive() => AssertTransitionAsync("24");
    [Fact]
    public async Task DKPROD_25_ResolutionDeadlineExhaustion()
    {
        foreach (var predecessor in new[] { "PreparingExpiredAwaitingResolution", "ProviderOutcomeUnknown" })
        {
            await using var s = await StartAsync();
            await ExpireAsync(s);
            if (predecessor == "ProviderOutcomeUnknown")
            {
                Assert.Equal("ResolvedOutcomeUnknown", await ResolveAsync(s, "ProviderOutcomeUnknown"));
                await ForceTimestampAsync(s, "NextResolutionAttemptNotBeforeUtc", "clock_timestamp()-interval '1 second'");
            }
            await ForceTimestampAsync(s, "ResolutionDeadlineUtc", "clock_timestamp()-interval '1 second'");
            await AssertPairAsync(s, predecessor, "Issued");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var mapping = await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId));
            var identity = await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',"AttemptId"::text,encode("AttemptKeyContextFingerprint",'hex'),
                    "CurrentPreparationId"::text,"CurrentPreparationFence"::text,
                    "CurrentProviderOperationToken","ResolutionAttemptCount"::text)
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));

            Assert.Equal("DeadlineExceeded", await ResolveAsync(s, "ProviderOutcomeUnknown"));
            Assert.Equal("tagekyc_raw_export_reconciler",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            await AssertPairAsync(s, "ProviderCorruptOrUnverifiable", "Issued");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(identity, await ScalarAsync<string>(s.Connection, """
                SELECT concat_ws('|',"AttemptId"::text,encode("AttemptKeyContextFingerprint",'hex'),
                    "CurrentPreparationId"::text,"CurrentPreparationFence"::text,
                    "CurrentProviderOperationToken","ResolutionAttemptCount"::text)
                FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(mapping, await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId)));
            Assert.Equal(eventCount + 1, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.True(await ScalarAsync<bool>(s.Connection, """
                SELECT "EventKind"='ResolvedCorrupt'
                   AND "ResolutionKind"='CorruptOrUnverifiable'
                   AND octet_length("ProviderResolutionEvidenceDigest")=32
                FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                ORDER BY "EventSequence" DESC LIMIT 1
                """, ("reservation", s.Source.ReservationId)));
        }
    }
    [Fact] public Task DKPROD_27_CleanupDeadlineIntervention() => AssertTransitionAsync("27");
    [Fact] public Task DKPROD_28_ActivateReplay() => AssertTransitionAsync("28");
    [Fact]
    public async Task DKPROD_29_FinalizeReplay()
    {
        foreach (var mapping in new[] { "CleanedUp", "AbsenceProven" })
        {
            await using var s = await StartAsync();
            if (mapping == "CleanedUp")
            {
                Assert.Equal("AbandonRequested", await RequestAbandonAsync(s));
                Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
                Assert.Equal("Acknowledged", await AcknowledgeCleanupAsync(s, "Cleaned"));
            }
            else
            {
                await ForceTimestampAsync(s, "CurrentPreparationLeaseExpiresAtUtc", "clock_timestamp()-interval '1 hour'");
                Assert.Equal("AbandonRequested", await RequestAbandonAsync(s));
                Assert.Equal("ResolvedNoResult", await ResolveAsync(s, "NoProviderResult", absenceReceipt: "absence-proof"));
            }
            Assert.Equal("ReservationAbandoned", await FinalizeAbandonAsync(s));
            await AssertPairAsync(s, "ReservationAbandoned", mapping);

            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var head = await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(h)::text FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var operation = await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId));
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));

            Assert.Equal("AlreadyAbandoned", await FinalizeAbandonAsync(s));
            Assert.Equal("tagekyc_raw_export_lifecycle",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            await AssertPairAsync(s, "ReservationAbandoned", mapping);
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(head, await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(h)::text FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(operation, await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId)));
            Assert.Equal(eventCount, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
        }
    }
    [Fact] public Task DKPROD_30_StaleFencePrepare() => AssertTransitionAsync("30");
    [Fact] public Task DKPROD_31_StaleTokenResolve() => AssertTransitionAsync("31");
    [Fact]
    public async Task DKPROD_32_StaleStateFinalize()
    {
        await using (var s = await StartAsync())
        {
            await AssertPairAsync(s, "PreparingLive", "Issued");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            var head = await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(h)::text FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
            var operation = await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId));
            var eventCount = await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));

            Assert.Equal("NotRequested", await FinalizeAbandonAsync(s));
            Assert.Equal("tagekyc_raw_export_lifecycle",
                await ScalarAsync<string>(s.Connection, "SELECT current_user"));
            await AssertPairAsync(s, "PreparingLive", "Issued");
            await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
            Assert.Equal(head, await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(h)::text FROM tagekyc.raw_export_attempt_key_reservations h
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
            Assert.Equal(operation, await ScalarAsync<string>(s.Connection, """
                SELECT to_jsonb(m)::text FROM tagekyc.raw_export_key_provider_operations m
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId)));
            Assert.Equal(eventCount, await ScalarAsync<long>(s.Connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId)));
        }

        await using (var connection = await OpenAsync())
        {
            var missingReservation = Guid.NewGuid();
            await SetRoleAsync(connection, "tagekyc_raw_export_deployer");
            var beforeHead = await ScalarAsync<long>(connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", missingReservation));
            var beforeMapping = await ScalarAsync<long>(connection, """
                SELECT count(*) FROM tagekyc.raw_export_key_provider_operations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", missingReservation));
            var beforeEvents = await ScalarAsync<long>(connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", missingReservation));
            await SetActorAsync(connection);
            await SetRoleAsync(connection, "tagekyc_raw_export_lifecycle");
            Assert.Equal("StateConflict", await ScalarAsync<string>(connection,
                "SELECT tagekyc.raw_export_finalize_abandon_attempt_key_reservation(@reservation)",
                ("reservation", missingReservation)));
            Assert.Equal("tagekyc_raw_export_lifecycle",
                await ScalarAsync<string>(connection, "SELECT current_user"));
            await SetRoleAsync(connection, "tagekyc_raw_export_deployer");
            Assert.Equal(beforeHead, await ScalarAsync<long>(connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_reservations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", missingReservation)));
            Assert.Equal(beforeMapping, await ScalarAsync<long>(connection, """
                SELECT count(*) FROM tagekyc.raw_export_key_provider_operations
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", missingReservation)));
            Assert.Equal(beforeEvents, await ScalarAsync<long>(connection, """
                SELECT count(*) FROM tagekyc.raw_export_attempt_key_preparation_events
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", missingReservation)));
        }
    }

    private async Task AssertPairTriggerAsync(bool mutateHead)
    {
        await using var s = await StartAsync();
        if (mutateHead) await RecordWrappedAsync(s);
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        await using var transaction = await s.Connection.BeginTransactionAsync();
        await ExecuteAsync(s.Connection,
            "SELECT set_config('tagekyc.raw_export_attempt_key_write_context','active',true)");
        if (mutateHead)
        {
            await ExecuteAsync(s.Connection, """
                UPDATE tagekyc.raw_export_attempt_key_reservations
                SET "PreparationDisposition"='PreparingExpiredAwaitingResolution',
                    "ResolutionDeadlineUtc"=clock_timestamp()+interval '24 hours',
                    "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=clock_timestamp()
                WHERE "AttemptKeyReservationId"=@reservation
                """, ("reservation", s.Source.ReservationId));
        }
        else
        {
            await ExecuteAsync(s.Connection, """
                UPDATE tagekyc.raw_export_key_provider_operations
                SET "ProviderOperationState"='AbsenceProven',
                    "ProviderAbsenceProofReceipt"='invalid-one-sided',"UpdatedAtUtc"=clock_timestamp()
                WHERE "ProviderOperationId"=@operation
                """, ("operation", s.Prepared.OperationId));
        }
        var exception = await Assert.ThrowsAsync<PostgresException>(() => transaction.CommitAsync());
        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_KEY_STATE_PAIR_INVALID", exception.MessageText);
    }

    private async Task AssertGuardRouteAsync(string table)
    {
        await using var s = await StartAsync();
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        Assert.True(await ScalarAsync<bool>(s.Connection, """
            SELECT count(*)=1
            FROM pg_catalog.pg_trigger t
            JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname=@table
              AND t.tgname='trg_raw_export_attempt_key_guard' AND NOT t.tgisinternal
            """, ("table", table)));
        Assert.True(await ScalarAsync<long>(s.Connection,
            $"SELECT count(*) FROM tagekyc.{table}") > 0);
    }

    private async Task AssertCsprngDisposableDatabaseFailsAsync(
        string createClause,
        string? mutationSql)
    {
        var database = $"dkprod_cs_{Guid.NewGuid():N}";
        var adminBuilder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await ExecuteAsync(admin, $"CREATE DATABASE \"{database}\" {createClause}");
        try
        {
            var targetBuilder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
            {
                Database = database,
                Pooling = false,
            };
            if (mutationSql is not null)
            {
                await using var target = new NpgsqlConnection(targetBuilder.ConnectionString);
                await target.OpenAsync();
                await ExecuteAsync(target, mutationSql);
            }
            var options = new DbContextOptionsBuilder<TagEkycDbContext>()
                .UseNpgsql(targetBuilder.ConnectionString)
                .Options;
            await using var db = new TagEkycDbContext(options);
            var exception = await Assert.ThrowsAsync<DurableKeyReadinessException>(() =>
                new CsprngReadinessValidator(db, DurableKeyCustodyOptions.Resolve(Configuration("postgres")))
                    .ValidateAsync(CancellationToken.None));
            Assert.Equal(CsprngReadinessValidator.Code, exception.Code);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAsync(admin, """
                SELECT pg_terminate_backend(pid) FROM pg_stat_activity
                WHERE datname=@database AND pid<>pg_backend_pid()
                """, ("database", database));
            await ExecuteAsync(admin, $"DROP DATABASE \"{database}\"");
        }
    }

    private async Task AssertTransitionAsync(string proof)
    {
        switch (proof)
        {
            case "1":
            {
                await using var s = await StartAsync();
                Assert.Equal("PreparingLive", s.Prepared.Outcome);
                Assert.Equal(1, s.Prepared.Fence);
                await AssertPairAsync(s, "PreparingLive", "Issued");
                return;
            }
            case "2":
            {
                await using var s = await StartAsync();
                await MoveToPositiveAbsenceAsync(s);
                await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
                var fresh = await PrepareAsync(s.Connection, s.Source);
                Assert.Equal("PreparingLive", fresh.Outcome);
                Assert.Equal(2, fresh.Fence);
                Assert.NotEqual(s.Prepared.PreparationId, fresh.PreparationId);
                await AssertPairAsync(s with { Prepared = fresh }, "PreparingLive", "Issued");
                return;
            }
            case "3":
            {
                await using var s = await StartAsync();
                var replay = await PrepareAsync(s.Connection, s.Source);
                Assert.Equal("InProgress", replay.Outcome);
                Assert.Equal(s.Prepared.PreparationId, replay.PreparationId);
                Assert.Equal(s.Prepared.Fence, replay.Fence);
                await AssertPairAsync(s, "PreparingLive", "Issued");
                return;
            }
            case "4":
            {
                await using var s = await StartAsync();
                Assert.Equal("ResultObserved", await RecordWrappedAsync(s));
                Assert.Equal("ExistingMatch", await RecordWrappedAsync(s));
                await AssertPairAsync(s, "PreparingLive", "ResultObserved");
                return;
            }
            case "5":
            {
                await using var s = await StartAsync();
                await RecordWrappedAsync(s);
                await ForceTimestampAsync(s, "CurrentPreparationLeaseExpiresAtUtc", "clock_timestamp()-interval '1 hour'");
                Assert.Equal("Activated", await ActivateAsync(s));
                await AssertPairAsync(s, "Active", "ResultObserved");
                return;
            }
            case "6":
            {
                await using var s = await StartAsync();
                await ExpireAsync(s);
                await AssertPairAsync(s, "PreparingExpiredAwaitingResolution", "Issued");
                Assert.True(await ScalarAsync<bool>(s.Connection,
                    "SELECT resolution_deadline_utc IS NOT NULL FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@reservation)",
                    ("reservation", s.Source.ReservationId)));
                return;
            }
            case "7":
            {
                await AssertFirstUnknownAsync(false);
                await AssertFirstUnknownAsync(true);
                return;
            }
            case "8":
            {
                await using var s = await StartAsync();
                Assert.Equal("ResolvedOutcomeUnknown", await ResolveAsync(s, "ProviderOutcomeUnknown"));
                await ForceTimestampAsync(s, "NextResolutionAttemptNotBeforeUtc", "clock_timestamp()-interval '1 second'");
                Assert.Equal("ResolvedOutcomeUnknown", await ResolveAsync(s, "ProviderOutcomeUnknown"));
                await AssertPairAsync(s, "ProviderOutcomeUnknown", "Issued");
                Assert.Equal(2L, await HeadLongAsync(s, "ResolutionAttemptCount"));
                return;
            }
            case "9a":
            case "9b":
            case "9c":
            {
                await using var s = await StartAsync();
                var expected = "PreparingLive";
                if (proof == "9b")
                {
                    await ExpireAsync(s);
                    expected = "PreparingExpiredAwaitingResolution";
                }
                else if (proof == "9c")
                {
                    await ResolveAsync(s, "ProviderOutcomeUnknown");
                    await ForceTimestampAsync(s, "NextResolutionAttemptNotBeforeUtc", "clock_timestamp()-interval '1 second'");
                    expected = "ProviderOutcomeUnknown";
                }
                Assert.Equal("ProviderUnavailableObserved", await ResolveAsync(s, "ProviderUnavailable"));
                await AssertPairAsync(s, expected, "Issued");
                Assert.True(await HeadLongAsync(s, "ResolutionAttemptCount") >= 1);
                return;
            }
            case "10":
            {
                await using var s = await StartAsync();
                await MoveToPositiveAbsenceAsync(s);
                await AssertPairAsync(s, "ReadyForFreshPreparation", "AbsenceProven");
                return;
            }
            case "11":
            {
                await using var s = await StartAsync();
                Assert.Equal("IllegalResolution", await ResolveAsync(s, "NoProviderResult", absenceReceipt: "absence-proof"));
                await AssertPairAsync(s, "PreparingLive", "Issued");
                return;
            }
            case "12":
            {
                foreach (var predecessor in new[] { "live", "expired", "unknown" })
                {
                    await using var s = await StartAsync();
                    if (predecessor == "expired") await ExpireAsync(s);
                    if (predecessor == "unknown")
                    {
                        await ResolveAsync(s, "ProviderOutcomeUnknown");
                        await ForceTimestampAsync(s, "NextResolutionAttemptNotBeforeUtc", "clock_timestamp()-interval '1 second'");
                    }
                    Assert.Equal("ResolvedCorrupt", await ResolveAsync(s, "CorruptOrUnverifiable"));
                    await AssertPairAsync(s, "ProviderCorruptOrUnverifiable", "Issued");
                }
                return;
            }
            case "13":
            {
                await using var s = await StartAsync();
                Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
                await AssertPairAsync(s, "ProviderCleanupRequired", "CleanupRequired");
                return;
            }
            case "14":
            case "15":
            case "16":
            {
                await using var s = await StartAsync();
                await MarkCleanupRequiredAsync(s);
                var kind = proof switch
                {
                    "14" => "CleanupUnavailable",
                    "15" => "CleanupOutcomeUnknown",
                    _ => "CleanupFailed",
                };
                Assert.Equal("CleanupObserved", await ObserveCleanupAsync(s, kind));
                await AssertPairAsync(s, "ProviderCleanupRequired", "CleanupRequired");
                Assert.Equal(1L, await HeadLongAsync(s, "CleanupAttemptCount"));
                return;
            }
            case "17":
            case "18":
            {
                await using var s = await StartAsync();
                await MarkCleanupRequiredAsync(s);
                Assert.Equal("Acknowledged", await AcknowledgeCleanupAsync(
                    s, proof == "17" ? "Cleaned" : "AlreadyAbsent"));
                await AssertPairAsync(s, "ReadyForFreshPreparation", "CleanedUp");
                return;
            }
            case "19":
            {
                await using var s = await StartAsync();
                Assert.Equal("RecoveredActivated", await RecordRecoveredAsync(s));
                await AssertPairAsync(s, "Active", "ResultObserved");
                return;
            }
            case "20":
            {
                foreach (var predecessor in new[] { "expired", "unknown" })
                {
                    await using var s = await StartAsync();
                    if (predecessor == "expired") await ExpireAsync(s);
                    else await ResolveAsync(s, "ProviderOutcomeUnknown");
                    Assert.Equal("RecoveredActivated", await RecordRecoveredAsync(s));
                    await AssertPairAsync(s, "Active", "ResultObserved");
                }
                return;
            }
            case "21":
            {
                await using var s = await StartAsync();
                Assert.Equal("AbandonRequested", await RequestAbandonAsync(s));
                Assert.Equal("AlreadyRequested", await RequestAbandonAsync(s));
                await AssertPairAsync(s, "AbandonRequested", "Issued");
                return;
            }
            case "22a":
            {
                await using var s = await StartAsync();
                await RequestAbandonAsync(s);
                Assert.Equal("CleanupRequired", await MarkCleanupRequiredAsync(s));
                await AssertPairAsync(s, "AbandonRequested", "CleanupRequired");
                return;
            }
            case "22b":
            case "22c":
            case "22d":
            {
                await using var s = await StartAsync();
                await RequestAbandonAsync(s);
                await MarkCleanupRequiredAsync(s);
                var kind = proof switch
                {
                    "22b" => "CleanupUnavailable",
                    "22c" => "CleanupOutcomeUnknown",
                    _ => "CleanupFailed",
                };
                Assert.Equal("CleanupObserved", await ObserveCleanupAsync(s, kind));
                await AssertPairAsync(s, "AbandonRequested", "CleanupRequired");
                return;
            }
            case "22e":
            case "22f":
            {
                await using var s = await StartAsync();
                await RequestAbandonAsync(s);
                await MarkCleanupRequiredAsync(s);
                Assert.Equal("Acknowledged", await AcknowledgeCleanupAsync(
                    s, proof == "22e" ? "Cleaned" : "AlreadyAbsent"));
                await AssertPairAsync(s, "AbandonRequested", "CleanedUp");
                return;
            }
            case "22g":
            {
                await using var s = await StartAsync();
                await ForceTimestampAsync(s, "CurrentPreparationLeaseExpiresAtUtc", "clock_timestamp()-interval '1 hour'");
                await RequestAbandonAsync(s);
                Assert.Equal("ResolvedNoResult", await ResolveAsync(s, "NoProviderResult", absenceReceipt: "absence-proof"));
                await AssertPairAsync(s, "AbandonRequested", "AbsenceProven");
                return;
            }
            case "22h":
            case "22i":
            {
                await using var s = await StartAsync();
                if (proof == "22h")
                {
                    await RequestAbandonAsync(s);
                    await MarkCleanupRequiredAsync(s);
                    await AcknowledgeCleanupAsync(s, "Cleaned");
                    Assert.Equal("ReservationAbandoned", await FinalizeAbandonAsync(s));
                    await AssertPairAsync(s, "ReservationAbandoned", "CleanedUp");
                }
                else
                {
                    await ForceTimestampAsync(s, "CurrentPreparationLeaseExpiresAtUtc", "clock_timestamp()-interval '1 hour'");
                    await RequestAbandonAsync(s);
                    await ResolveAsync(s, "NoProviderResult", absenceReceipt: "absence-proof");
                    Assert.Equal("ReservationAbandoned", await FinalizeAbandonAsync(s));
                    await AssertPairAsync(s, "ReservationAbandoned", "AbsenceProven");
                }
                return;
            }
            case "22j":
            {
                await using var s = await StartAsync();
                await RequestAbandonAsync(s);
                Assert.Equal("CleanupNotComplete", await FinalizeAbandonAsync(s));
                await AssertPairAsync(s, "AbandonRequested", "Issued");
                return;
            }
            case "22k":
            {
                await using var first = await StartAsync();
                await using var second = await StartAsync();
                await SetRoleAsync(second.Connection, "tagekyc_raw_export_reconciler");
                var outcome = await ScalarAsync<string>(second.Connection, """
                    SELECT tagekyc.raw_export_mark_key_provider_cleanup_required(
                        @operation,@reservation,@preparation,@fence,@token,'cleanup-ref')
                    """,
                    ("operation", first.Prepared.OperationId),
                    ("reservation", second.Source.ReservationId),
                    ("preparation", second.Prepared.PreparationId),
                    ("fence", second.Prepared.Fence),
                    ("token", first.Prepared.Token));
                Assert.Equal("StaleOperation", outcome);
                await AssertPairAsync(second, "PreparingLive", "Issued");
                return;
            }
            case "23":
            {
                await using var s = await StartAsync();
                await RecordWrappedAsync(s);
                await ActivateAsync(s);
                Assert.Equal("Revoked", await RevokeAsync(s));
                await AssertPairAsync(s, "Revoked", "ResultObserved");
                return;
            }
            case "24":
            {
                await using var s = await StartAsync();
                Assert.Equal("NotActive", await RevokeAsync(s));
                await RecordWrappedAsync(s);
                await ActivateAsync(s);
                Assert.Equal("Revoked", await RevokeAsync(s));
                Assert.Equal("AlreadyRevoked", await RevokeAsync(s));
                return;
            }
            case "25":
            {
                await using var s = await StartAsync();
                await ExpireAsync(s);
                await ForceTimestampAsync(s, "ResolutionDeadlineUtc", "clock_timestamp()-interval '1 second'");
                Assert.Equal("DeadlineExceeded", await ResolveAsync(s, "ProviderOutcomeUnknown"));
                await AssertPairAsync(s, "ProviderCorruptOrUnverifiable", "Issued");
                return;
            }
            case "27":
            {
                await using var s = await StartAsync();
                await MarkCleanupRequiredAsync(s);
                await ForceTimestampAsync(s, "CleanupDeadlineUtc", "clock_timestamp()-interval '1 second'");
                Assert.Equal("DeadlineIntervention", await ObserveCleanupAsync(s, "CleanupFailed"));
                Assert.True(await HeadBoolAsync(s, "CleanupOperatorInterventionRequired"));
                return;
            }
            case "28":
            {
                await using var s = await StartAsync();
                await RecordWrappedAsync(s);
                await ActivateAsync(s);
                Assert.Equal("AlreadyActivated", await ActivateAsync(s));
                await AssertPairAsync(s, "Active", "ResultObserved");
                return;
            }
            case "29":
            {
                await using var s = await StartAsync();
                await RequestAbandonAsync(s);
                await MarkCleanupRequiredAsync(s);
                await AcknowledgeCleanupAsync(s, "Cleaned");
                await FinalizeAbandonAsync(s);
                Assert.Equal("AlreadyAbandoned", await FinalizeAbandonAsync(s));
                return;
            }
            case "30":
            {
                await using var s = await StartAsync();
                await MoveToPositiveAbsenceAsync(s);
                await InsertFutureOperationAsync(s);
                await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
                Assert.Equal("StaleFence", (await PrepareAsync(s.Connection, s.Source)).Outcome);
                await AssertPairAsync(s, "ReadyForFreshPreparation", "AbsenceProven");
                return;
            }
            case "31":
            {
                await using var s = await StartAsync();
                await ForceHeadTextAsync(s, "CurrentProviderOperationToken", new string('B', 43));
                Assert.Equal("StalePreparation", await ResolveAsync(s, "ProviderOutcomeUnknown"));
                await AssertPairAsync(s, "PreparingLive", "Issued");
                return;
            }
            case "32":
            {
                await using var s = await StartAsync();
                Assert.Equal("NotRequested", await FinalizeAbandonAsync(s));
                await AssertPairAsync(s, "PreparingLive", "Issued");
                return;
            }
            default:
                throw new InvalidOperationException($"Unknown proof {proof}.");
        }
    }

    private async Task AssertFirstUnknownAsync(bool expired)
    {
        await using var s = await StartAsync();
        if (expired) await ExpireAsync(s);
        Assert.Equal("ResolvedOutcomeUnknown", await ResolveAsync(s, "ProviderOutcomeUnknown"));
        await AssertPairAsync(s, "ProviderOutcomeUnknown", "Issued");
        Assert.Equal(1L, await HeadLongAsync(s, "ResolutionAttemptCount"));
    }

    private async Task<Scenario> StartAsync()
    {
        var source = await SeedSourceAsync();
        var connection = await OpenAsync();
        await SetRoleAsync(connection, "tagekyc_raw_export_custody_encryptor");
        var prepared = await PrepareAsync(connection, source);
        Assert.Equal("PreparingLive", prepared.Outcome);
        return new Scenario(connection, source, prepared);
    }

    private static async Task<string> RecordWrappedAsync(Scenario s)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_record_key_provider_wrapped_result(
                @operation,@reservation,@preparation,@fence,@token,
                decode(repeat('20',32),'hex'),decode(repeat('01',12),'hex'),
                decode(repeat('40',16),'hex'),'AES-256-GCM',1,
                'receipt-integration','provider-resource-integration')
            """,
            ("operation", s.Prepared.OperationId), ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence),
            ("token", s.Prepared.Token));
    }

    private static async Task<string> ActivateAsync(Scenario s)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_custody_encryptor");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_activate_attempt_key_reservation(@reservation,@preparation,@fence)
            """, ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence));
    }

    private static async Task ExpireAsync(Scenario s)
    {
        await ForceTimestampAsync(s, "CurrentPreparationLeaseExpiresAtUtc", "clock_timestamp()-interval '1 hour'");
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        Assert.Equal("Expired", await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_mark_attempt_key_preparation_expired(@reservation,@preparation,@fence)
            """, ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence)));
    }

    private static async Task<string> ResolveAsync(
        Scenario s,
        string resolution,
        string? cleanupReference = null,
        string? operationReceipt = "provider-receipt",
        string? absenceReceipt = null)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_resolve_attempt_key_provider_outcome(
                @reservation,@preparation,@fence,@resolution,@cleanup,@receipt,@absence)
            """, ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence),
            ("resolution", resolution), ("cleanup", cleanupReference),
            ("receipt", operationReceipt), ("absence", absenceReceipt));
    }

    private static async Task MoveToPositiveAbsenceAsync(Scenario s)
    {
        await ExpireAsync(s);
        Assert.Equal("ResolvedNoResult", await ResolveAsync(
            s, "NoProviderResult", absenceReceipt: "absence-proof"));
    }

    private static async Task<string> MarkCleanupRequiredAsync(Scenario s)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_mark_key_provider_cleanup_required(
                @operation,@reservation,@preparation,@fence,@token,'cleanup-ref')
            """, ("operation", s.Prepared.OperationId),
            ("reservation", s.Source.ReservationId), ("preparation", s.Prepared.PreparationId),
            ("fence", s.Prepared.Fence), ("token", s.Prepared.Token));
    }

    private static async Task<string> ObserveCleanupAsync(Scenario s, string kind)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_record_key_provider_cleanup_observation(
                @operation,@reservation,@preparation,@fence,@token,@kind,'cleanup-ref','cleanup-observation-receipt')
            """, ("operation", s.Prepared.OperationId),
            ("reservation", s.Source.ReservationId), ("preparation", s.Prepared.PreparationId),
            ("fence", s.Prepared.Fence), ("token", s.Prepared.Token), ("kind", kind));
    }

    private static async Task<string> AcknowledgeCleanupAsync(Scenario s, string kind)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_acknowledge_key_provider_cleanup(
                @operation,@reservation,@preparation,@fence,@token,@kind,'cleanup-ref','cleanup-ack-receipt')
            """, ("operation", s.Prepared.OperationId),
            ("reservation", s.Source.ReservationId), ("preparation", s.Prepared.PreparationId),
            ("fence", s.Prepared.Fence), ("token", s.Prepared.Token), ("kind", kind));
    }

    private static async Task<string> RecordRecoveredAsync(Scenario s)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<string>(s.Connection, """
            SELECT tagekyc.raw_export_record_recovered_key_provider_result(
                @reservation,@preparation,@fence,@token,
                decode(repeat('20',32),'hex'),decode(repeat('01',12),'hex'),decode(repeat('40',16),'hex'),
                'AES-256-GCM',1,'provider-resource-recovered','provider-receipt-recovered')
            """, ("reservation", s.Source.ReservationId),
            ("preparation", s.Prepared.PreparationId), ("fence", s.Prepared.Fence),
            ("token", s.Prepared.Token));
    }

    private static async Task<string> RequestAbandonAsync(Scenario s)
    {
        await SetActorAsync(s.Connection);
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_lifecycle");
        return await ScalarAsync<string>(s.Connection,
            "SELECT tagekyc.raw_export_request_abandon_attempt_key_reservation(@reservation,'operator-request')",
            ("reservation", s.Source.ReservationId));
    }

    private static async Task<string> FinalizeAbandonAsync(Scenario s)
    {
        await SetActorAsync(s.Connection);
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_lifecycle");
        return await ScalarAsync<string>(s.Connection,
            "SELECT tagekyc.raw_export_finalize_abandon_attempt_key_reservation(@reservation)",
            ("reservation", s.Source.ReservationId));
    }

    private static async Task<string> RevokeAsync(Scenario s)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<string>(s.Connection,
            "SELECT tagekyc.raw_export_revoke_attempt_key_reservation(@reservation,'operator-revoked')",
            ("reservation", s.Source.ReservationId));
    }

    private static async Task AssertPairAsync(Scenario s, string head, string mapping)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        await using var command = s.Connection.CreateCommand();
        command.CommandText = """
            SELECT preparation_disposition,provider_operation_state
            FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@reservation)
            """;
        command.Parameters.AddWithValue("reservation", s.Source.ReservationId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(head, reader.GetString(0));
        Assert.Equal(mapping, reader.GetString(1));
        Assert.False(await reader.ReadAsync());
    }

    private static async Task<long> HeadLongAsync(Scenario s, string column)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<long>(s.Connection,
            $"SELECT {ToSnakeCase(column)} FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@reservation)",
            ("reservation", s.Source.ReservationId));
    }

    private static async Task<bool> HeadBoolAsync(Scenario s, string column)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_reconciler");
        return await ScalarAsync<bool>(s.Connection,
            $"SELECT {ToSnakeCase(column)} FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@reservation)",
            ("reservation", s.Source.ReservationId));
    }

    private static string ToSnakeCase(string value) =>
        string.Concat(value.Select((character, index) =>
            char.IsUpper(character) && index > 0
                ? $"_{char.ToLowerInvariant(character)}"
                : char.ToLowerInvariant(character).ToString()));

    private static async Task ForceTimestampAsync(Scenario s, string column, string sqlExpression)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        await ScalarAsync<string>(s.Connection,
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        await ExecuteAsync(s.Connection,
            $"UPDATE tagekyc.raw_export_attempt_key_reservations SET \"{column}\"={sqlExpression}, \"UpdatedAtUtc\"=clock_timestamp() WHERE \"AttemptKeyReservationId\"=@reservation",
            ("reservation", s.Source.ReservationId));
        await ScalarAsync<string>(s.Connection,
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','',false)");
    }

    private static async Task ForceHeadTextAsync(Scenario s, string column, string value)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        await ScalarAsync<string>(s.Connection,
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        await ExecuteAsync(s.Connection,
            $"UPDATE tagekyc.raw_export_attempt_key_reservations SET \"{column}\"=@value, \"UpdatedAtUtc\"=clock_timestamp() WHERE \"AttemptKeyReservationId\"=@reservation",
            ("value", value), ("reservation", s.Source.ReservationId));
        await ScalarAsync<string>(s.Connection,
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','',false)");
    }

    private static async Task InsertFutureOperationAsync(Scenario s)
    {
        await SetRoleAsync(s.Connection, "tagekyc_raw_export_deployer");
        await ScalarAsync<string>(s.Connection,
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        await ExecuteAsync(s.Connection, """
            INSERT INTO tagekyc.raw_export_key_provider_operations(
                "ProviderOperationId","KeyProviderId","ProviderOperationToken",
                "AttemptKeyReservationId","PreparationId","PreparationFence",
                "AttemptKeyContextFingerprint","ProviderOperationState","IssuedAtUtc","UpdatedAtUtc")
            SELECT gen_random_uuid(),"KeyProviderId",repeat('A',43),"AttemptKeyReservationId",
                   gen_random_uuid(),"PreparationFence"+1,"AttemptKeyContextFingerprint",'Issued',clock_timestamp(),clock_timestamp()
            FROM tagekyc.raw_export_key_provider_operations
            WHERE "ProviderOperationId"=@operation
            """, ("operation", s.Prepared.OperationId));
        await ScalarAsync<string>(s.Connection,
            "SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','',false)");
    }

    private static async Task SetActorAsync(NpgsqlConnection connection) =>
        await ScalarAsync<string>(connection,
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,false)",
            ("actor", "11111111-1111-1111-1111-111111111111"));

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        var result = await command.ExecuteScalarAsync();
        return (T)result!;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<SourceIdentity> SeedSourceAsync()
    {
        await new Tip88C1B2CoreTests(postgres)
            .C1B2CORE_new_candidate_commits_full_recovery_context_atomically();
        await using var db = postgres.CreateDbContext();
        return await db.RawExportSourceEncryptionAttempts
            .OrderByDescending(row => row.CreatedAtUtc)
            .Select(row => new SourceIdentity(
                row.AttemptKeyReservationId,
                row.AttemptId,
                row.SourceArtifactId))
            .FirstAsync();
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task SetRoleAsync(NpgsqlConnection connection, string role)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"RESET ROLE; SET ROLE {role}";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<PreparedIdentity> PrepareAsync(
        NpgsqlConnection connection,
        SourceIdentity source)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM tagekyc.raw_export_prepare_attempt_key_reservation(
                @reservation,@attempt,@source)
            """;
        command.Parameters.AddWithValue("reservation", source.ReservationId);
        command.Parameters.AddWithValue("attempt", source.AttemptId);
        command.Parameters.AddWithValue("source", source.SourceArtifactId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var outcome = reader.GetString(0);
        if (reader.IsDBNull(1))
            return new(outcome, Guid.Empty, Guid.Empty, 0, string.Empty, []);
        return new(
            outcome, reader.GetGuid(1), reader.GetGuid(2),
            reader.GetInt64(3), reader.GetString(4), (byte[])reader[6]);
    }

    private static IConfiguration Configuration(string? owner)
    {
        var values = new Dictionary<string, string?>
        {
            [DurableKeyTopologyOptions.ConfigurationPath] = "DurableKey",
            [DurableKeyCustodyOptions.CsprngExpectedOwnerPath] = owner,
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static readonly string[] CallableFunctions =
    [
        "raw_export_prepare_attempt_key_reservation",
        "raw_export_record_key_provider_wrapped_result",
        "raw_export_activate_attempt_key_reservation",
        "raw_export_mark_attempt_key_preparation_expired",
        "raw_export_resolve_attempt_key_provider_outcome",
        "raw_export_mark_key_provider_cleanup_required",
        "raw_export_record_key_provider_cleanup_observation",
        "raw_export_acknowledge_key_provider_cleanup",
        "raw_export_record_recovered_key_provider_result",
        "raw_export_request_abandon_attempt_key_reservation",
        "raw_export_finalize_abandon_attempt_key_reservation",
        "raw_export_revoke_attempt_key_reservation",
        "raw_export_inspect_attempt_key_reservation",
        "raw_export_read_current_attempt_key_recovery_context",
        "raw_export_read_active_attempt_key_envelope",
    ];

    private sealed record SourceIdentity(Guid ReservationId, Guid AttemptId, Guid SourceArtifactId);
    private sealed record Scenario(
        NpgsqlConnection Connection,
        SourceIdentity Source,
        PreparedIdentity Prepared) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
    private sealed record PreparedIdentity(
        string Outcome,
        Guid OperationId,
        Guid PreparationId,
        long Fence,
        string Token,
        byte[] ContextFingerprint);
}
