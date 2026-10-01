using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class OpenBaoProductionKekProviderTests(PostgresPersistenceFixture postgres)
{
    private const string Current = "20260930082453_OpenBaoProductionKekProvider";
    private const string Previous = "20260927120000_SiteQualificationMeasurementPlane";

    [Fact]
    public void Representation_and_Aad_contracts_are_discriminated_and_mutation_sensitive()
    {
        var legacy = new LegacyAesGcmWrappedMaterial(
            Enumerable.Repeat((byte)1, 32).ToArray(),
            Enumerable.Repeat((byte)2, 12).ToArray(),
            Enumerable.Repeat((byte)3, 16).ToArray(),
            "AES-256-GCM", 1, "resource", "receipt");
        var opaque = new OpaqueProviderWrappedMaterial(
            "vault:v1:opaque"u8.ToArray(), OpenBaoKekOptions.WrappingSchemeId, 1,
            "resource", "receipt");

        Assert.Equal(KekWrappedMaterialRepresentations.LegacyAesGcmSplit, legacy.RepresentationId);
        Assert.Equal((32, 12, 16), (legacy.Ciphertext.Length, legacy.Nonce.Length, legacy.Tag.Length));
        Assert.Equal(KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, opaque.RepresentationId);
        Assert.Equal(1, opaque.RepresentationVersion);
        Assert.DoesNotContain("vault:v1:opaque", opaque.ToString(), StringComparison.Ordinal);

        var context = Enumerable.Repeat((byte)0x41, 32).ToArray();
        var reference = new KekReference(OpenBaoKekOptions.KeyProviderId, "raw-export", 7,
            new string('a', 64));
        var canonical = OpenBaoTransitKekOperationProvider.ComputeAad(reference, context);
        Assert.Equal(32, canonical.Length);
        Assert.Equal("B54CAD729E08C482D442004449DD033BDC04E53771F18CE90402442B697FD4FB",
            Convert.ToHexString(canonical));
        Assert.NotEqual(canonical, OpenBaoTransitKekOperationProvider.ComputeAad(
            reference with { KekVersion = 8 }, context));
        Assert.NotEqual(canonical, OpenBaoTransitKekOperationProvider.ComputeAad(
            reference with { KekFingerprint = new string('b', 64) }, context));
        context[0] ^= 0xff;
        Assert.NotEqual(canonical, OpenBaoTransitKekOperationProvider.ComputeAad(reference, context));
    }

    [Fact]
    public async Task Representation_database_contract_round_trips_exact_bytes_and_rejects_mixed_unknown_and_oversized_shapes()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_shapes");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        var opaque = await PrepareOpaqueAsync(database);
        Assert.Equal("ShapeInvalid", await RecordAsync(database, opaque,
            KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, 1,
            new byte[32], new byte[12], new byte[16], "vault:v1:mixed"u8.ToArray(),
            OpenBaoKekOptions.WrappingSchemeId, 1));
        Assert.Equal("ShapeInvalid", await RecordAsync(database, opaque,
            "UNKNOWN_REPRESENTATION", 1, null, null, null, "vault:v1:unknown"u8.ToArray(),
            OpenBaoKekOptions.WrappingSchemeId, 1));
        Assert.Equal("ShapeInvalid", await RecordAsync(database, opaque,
            KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, 2,
            null, null, null, "vault:v1:unknown-version"u8.ToArray(),
            OpenBaoKekOptions.WrappingSchemeId, 1));
        Assert.Equal("ShapeInvalid", await RecordAsync(database, opaque,
            KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, 1,
            null, null, null, Enumerable.Repeat((byte)'x', 4097).ToArray(),
            OpenBaoKekOptions.WrappingSchemeId, 1));

        var exactOpaque = "vault:v1:exact-byte-round-trip"u8.ToArray();
        Assert.Equal("ResultObserved", await RecordAsync(database, opaque,
            KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, 1,
            null, null, null, exactOpaque, OpenBaoKekOptions.WrappingSchemeId, 1));
        await using (var db = database.CreateDbContext())
        {
            Assert.Equal(exactOpaque, await db.RawExportKeyProviderOperations
                .Where(row => row.ProviderOperationId == opaque.ProviderOperationId)
                .Select(row => row.OpaqueWrappedDekPayload!)
                .SingleAsync());
        }

        var legacy = await PrepareAsync(LegacyKekWrappedMaterialProfileSource.Profile, database);
        var ciphertext = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var nonce = Enumerable.Range(32, 12).Select(value => (byte)value).ToArray();
        var tag = Enumerable.Range(64, 16).Select(value => (byte)value).ToArray();
        Assert.Equal("ResultObserved", await RecordAsync(database, legacy,
            KekWrappedMaterialRepresentations.LegacyAesGcmSplit, 1,
            ciphertext, nonce, tag, null, "AES-256-GCM", 1));
        await using (var db = database.CreateDbContext())
        {
            var stored = await db.RawExportKeyProviderOperations
                .Where(row => row.ProviderOperationId == legacy.ProviderOperationId)
                .Select(row => new { row.WrappedDekCiphertext, row.WrappedDekNonce, row.WrappedDekTag,
                    row.OpaqueWrappedDekPayload })
                .SingleAsync();
            Assert.Equal(ciphertext, stored.WrappedDekCiphertext);
            Assert.Equal(nonce, stored.WrappedDekNonce);
            Assert.Equal(tag, stored.WrappedDekTag);
            Assert.Null(stored.OpaqueWrappedDekPayload);
        }
    }

    [Fact]
    public async Task Fingerprint_and_opaque_digest_domains_are_exact_and_mutation_sensitive()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_fingerprint");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        var legacy = await PrepareAsync(LegacyKekWrappedMaterialProfileSource.Profile, database);
        var opaque = await PrepareOpaqueAsync(database);
        await using var db = database.CreateDbContext();

        var expectedLegacy = await ContextFingerprintAsync(db, legacy,
            "tip-88c1-attempt-key-context-v1", includeRepresentation: false);
        var expectedOpaque = await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true);
        Assert.Equal(expectedLegacy, legacy.Context);
        Assert.Equal(expectedOpaque, opaque.Context);
        Assert.NotEqual(legacy.Context, opaque.Context);
        Assert.NotEqual(expectedOpaque, await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true,
            representationId: KekWrappedMaterialRepresentations.LegacyAesGcmSplit));
        Assert.NotEqual(expectedOpaque, await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true,
            representationVersion: 2));
        Assert.NotEqual(expectedOpaque, await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true,
            wrappingSchemeId: "MUTANT-SCHEME"));
        Assert.NotEqual(expectedOpaque, await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true,
            keyProviderId: "mutant-provider"));
        Assert.NotEqual(expectedOpaque, await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true,
            kekVersion: 2));
        Assert.NotEqual(expectedOpaque, await ContextFingerprintAsync(db, opaque,
            "tip-88c1-attempt-key-context-v2", includeRepresentation: true,
            kekFingerprint: new string('f', 64)));

        var payload = "vault:v1:digest-source"u8.ToArray();
        Assert.Equal("ResultObserved", await RecordAsync(database, opaque,
            KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, 1,
            null, null, null, payload, OpenBaoKekOptions.WrappingSchemeId, 1));
        var storedDigest = await db.RawExportKeyProviderOperations
            .Where(row => row.ProviderOperationId == opaque.ProviderOperationId)
            .Select(row => row.WrappedDekMetadataDigest!)
            .SingleAsync();
        var expectedDigest = await db.Database.SqlQuery<byte[]>($"""
            SELECT tagekyc.raw_export_c1_hash_canonical(
              'tip-88c1-wrapped-dek-metadata-opaque-v1',
              {Convert.ToHexString(opaque.Context).ToLowerInvariant()},
              {KekWrappedMaterialRepresentations.OpaqueProviderCiphertext},'1',
              {Convert.ToHexString(payload).ToLowerInvariant()}) AS "Value"
            """).SingleAsync();
        Assert.Equal(expectedDigest, storedDigest);

        var changed = payload.ToArray();
        changed[^1] ^= 0x01;
        Assert.Equal("StateConflict", await RecordAsync(database, opaque,
            KekWrappedMaterialRepresentations.OpaqueProviderCiphertext, 1,
            null, null, null, changed, OpenBaoKekOptions.WrappingSchemeId, 1));
    }

    [Fact]
    public void OpenBao_configuration_rejects_non_tls_and_invalid_key_identity()
    {
        var nonTls = Configuration("http://openbao.invalid", new string('a', 64));
        Assert.Equal("OPENBAO_HTTPS_ADDRESS_INVALID",
            Assert.Throws<InvalidOperationException>(() => OpenBaoKekOptions.Resolve(nonTls)).Message);

        var badFingerprint = Configuration("https://openbao.invalid", "ABC");
        Assert.Equal("OPENBAO_TRANSIT_KEY_FINGERPRINT_INVALID",
            Assert.Throws<InvalidOperationException>(() => OpenBaoKekOptions.Resolve(badFingerprint)).Message);
    }

    [Fact]
    public void OpenBao_wrap_failure_taxonomy_separates_transport_shape_and_unknown_failures()
    {
        Assert.IsType<KekWrapResult.Unavailable>(
            OpenBaoTransitKekOperationProvider.ClassifyWrapFailure(
                new OpenBaoTransportException(403, false)));
        Assert.IsType<KekWrapResult.Unavailable>(
            OpenBaoTransitKekOperationProvider.ClassifyWrapFailure(
                new OpenBaoTransportException(0, true)));
        Assert.IsType<KekWrapResult.Unavailable>(
            OpenBaoTransitKekOperationProvider.ClassifyWrapFailure(
                new IOException("network")));
        Assert.IsType<KekWrapResult.CorruptOrUnverifiable>(
            OpenBaoTransitKekOperationProvider.ClassifyWrapFailure(
                new System.Text.Json.JsonException("bad-shape")));
        Assert.IsType<KekWrapResult.CorruptOrUnverifiable>(
            OpenBaoTransitKekOperationProvider.ClassifyWrapFailure(
                new KeyNotFoundException("missing-data")));
        Assert.IsType<KekWrapResult.OutcomeUnknown>(
            OpenBaoTransitKekOperationProvider.ClassifyWrapFailure(
                new InvalidOperationException("unclassified")));
    }

    [Fact]
    public async Task Encryption_key_access_failure_is_stable_and_fail_closed()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_encrypt_unavailable");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        var prepared = await PrepareAsync(LegacyKekWrappedMaterialProfileSource.Profile, database);
        Assert.Equal("ResultObserved", await RecordAsync(database, prepared,
            KekWrappedMaterialRepresentations.LegacyAesGcmSplit, 1,
            new byte[32], new byte[12], new byte[16], null, "AES-256-GCM", 1));
        await using var db = database.CreateDbContext();
        var map = new PostgresKeyProviderOperationMap(db);
        Assert.Equal("Activated", await map.ActivateAsync(
            prepared.ReservationId, prepared.PreparationId, prepared.Fence, CancellationToken.None));
        var operation = new AttemptAeadEncryptionOperationService(
            db, new UnavailableUnwrapProvider(), DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            operation.EncryptBoundedChunkAsync(new(
                prepared.ReservationId, "plaintext"u8.ToArray(), new byte[12], "aad"u8.ToArray()),
                CancellationToken.None));
        Assert.Equal("RAW_EXPORT_KEY_ACCESS_INDETERMINATE", failure.Message);
        Assert.IsType<IOException>(failure.InnerException);
    }

    [Fact]
    public async Task Migration_legacy_only_Down_and_reapply_restore_exact_boundary()
    {
        await using var predecessor = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_predecessor_boundary");
        await using (var predecessorDb = predecessor.CreateDbContext())
        {
            await predecessorDb.Database.EnsureDeletedAsync();
            await predecessorDb.GetService<IMigrator>().MigrateAsync(Previous);
        }
        var signatures = new[]
        {
            "tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)",
            "tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)",
            "tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)",
            "tagekyc.raw_export_read_active_attempt_key_envelope(uuid)",
            "tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint)",
        };
        var expectedBoundary = await ReadFunctionBoundaryAsync(predecessor.ConnectionString, signatures);
        var expectedStageAclCount = await CountStageRightsAsync(predecessor.ConnectionString);

        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_legacy_roundtrip");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        var prepared = await PrepareAsync(LegacyKekWrappedMaterialProfileSource.Profile, database);
        var ciphertext = Enumerable.Range(0, 32).Select(value => (byte)(0x80 + value)).ToArray();
        var nonce = Enumerable.Range(0, 12).Select(value => (byte)(0x40 + value)).ToArray();
        var tag = Enumerable.Range(0, 16).Select(value => (byte)(0x20 + value)).ToArray();
        Assert.Equal("ResultObserved", await RecordAsync(database, prepared,
            KekWrappedMaterialRepresentations.LegacyAesGcmSplit, 1,
            ciphertext, nonce, tag, null, "AES-256-GCM", 1));
        var before = await ReadLegacyMaterialAsync(isolated.ConnectionString, prepared.ProviderOperationId,
            includeRepresentation: true);
        Assert.Equal(KekWrappedMaterialRepresentations.LegacyAesGcmSplit, before.RepresentationId);
        Assert.Equal(1, before.RepresentationVersion);
        Assert.Null(before.OpaquePayload);

        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        await migrator.MigrateAsync(Previous);
        Assert.Equal(Previous, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.False(await ExistsAsync(db, "SELECT to_regclass('tagekyc.raw_export_openbao_kek_operation_journal') IS NOT NULL"));
        Assert.False(await ExistsAsync(db, "SELECT to_regprocedure('tagekyc.raw_export_openbao_issue_kek_operation(text,bytea,text,text,integer,text,text,integer,text,integer)') IS NOT NULL"));
        var restoredBoundary = await ReadFunctionBoundaryAsync(isolated.ConnectionString, signatures);
        Assert.Equal(expectedBoundary, restoredBoundary);
        Assert.Equal(expectedStageAclCount, await CountStageRightsAsync(isolated.ConnectionString));
        await AssertPredecessorStageRightsAsync(isolated.ConnectionString);

        await using (var connection = new NpgsqlConnection(isolated.ConnectionString))
        {
            await connection.OpenAsync();
            await using var prepare = connection.CreateCommand();
            prepare.CommandText = "SELECT outcome FROM tagekyc.raw_export_prepare_attempt_key_reservation(@reservation,@attempt,@source)";
            prepare.Parameters.AddWithValue("reservation", prepared.ReservationId);
            prepare.Parameters.AddWithValue("attempt", await ReadAttemptIdAsync(isolated.ConnectionString, prepared.ReservationId));
            prepare.Parameters.AddWithValue("source", await ReadSourceArtifactIdAsync(isolated.ConnectionString, prepared.ReservationId));
            Assert.Equal("InProgress", await prepare.ExecuteScalarAsync());

            await using var activate = connection.CreateCommand();
            activate.CommandText = "SELECT tagekyc.raw_export_activate_attempt_key_reservation(@reservation,@preparation,@fence)";
            activate.Parameters.AddWithValue("reservation", prepared.ReservationId);
            activate.Parameters.AddWithValue("preparation", prepared.PreparationId);
            activate.Parameters.AddWithValue("fence", prepared.Fence);
            Assert.Equal("Activated", await activate.ExecuteScalarAsync());
        }
        var afterDown = await ReadLegacyMaterialAsync(isolated.ConnectionString, prepared.ProviderOperationId,
            includeRepresentation: false);
        AssertLegacyMaterialEqual(before with { RepresentationId = null, RepresentationVersion = null }, afterDown);

        await migrator.MigrateAsync(Current);
        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.True(await ExistsAsync(db, "SELECT to_regclass('tagekyc.raw_export_openbao_kek_operation_journal') IS NOT NULL"));
        Assert.True(await ExistsAsync(db, "SELECT to_regprocedure('tagekyc.raw_export_read_active_attempt_key_material(uuid)') IS NOT NULL"));
        var afterReapply = await ReadLegacyMaterialAsync(isolated.ConnectionString, prepared.ProviderOperationId,
            includeRepresentation: true);
        AssertLegacyMaterialEqual(before, afterReapply);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Migration_Down_rejects_opaque_reservation_before_schema_mutation()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_down_opaque");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        await new Tip88C1B2CoreTests(database)
            .C1B2CORE_new_candidate_commits_full_recovery_context_atomically();
        await using var db = database.CreateDbContext();
        var source = await db.RawExportSourceEncryptionAttempts
            .OrderByDescending(row => row.CreatedAtUtc)
            .Select(row => new { row.AttemptKeyReservationId, row.AttemptId, row.SourceArtifactId })
            .FirstAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT outcome FROM tagekyc.raw_export_prepare_attempt_key_reservation(
              {source.AttemptKeyReservationId},{source.AttemptId},{source.SourceArtifactId},
              {KekWrappedMaterialRepresentations.OpaqueProviderCiphertext},1,
              {OpenBaoKekOptions.WrappingSchemeId},1)
            """);
        var before = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var failure = await Assert.ThrowsAsync<PostgresException>(() =>
            db.GetService<IMigrator>().MigrateAsync(Previous));
        Assert.Equal("REPRESENTATION_DOWNGRADE_NOT_LOSSLESS", failure.MessageText);
        Assert.Equal(before, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.True(await ExistsAsync(db, "SELECT to_regclass('tagekyc.raw_export_openbao_kek_operation_journal') IS NOT NULL"));
        Assert.True(await ExistsAsync(db, "SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='tagekyc' AND table_name='raw_export_attempt_key_reservations' AND column_name='MaterialRepresentationId')"));
    }

    [Fact]
    public async Task PostgreSql_journal_replays_exact_result_and_rejects_conflict_and_late_writer()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_journal_replay");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        var prepared = await PrepareOpaqueAsync(database);
        await using var db = database.CreateDbContext();
        var journal = new PostgresOpenBaoKekJournal(db);
        var reference = new KekReference(prepared.KeyProviderId, prepared.KekId,
            prepared.KekVersion, prepared.KekFingerprint);
        var issued = await journal.IssueAsync(prepared.Token, prepared.Context,
            reference, OpenBaoKekWrappedMaterialProfileSource.Profile, CancellationToken.None);
        Assert.NotNull(issued);
        Assert.Equal("Issued", issued.JournalState);

        var payload = "vault:v1:durable-ciphertext"u8.ToArray();
        var wrapped = await journal.RecordWrappedAsync(prepared.Token, prepared.Context,
            issued.RowRevision, payload, "openbao-transit:raw-export:1", CancellationToken.None);
        Assert.NotNull(wrapped);
        Assert.Equal("Wrapped", wrapped.JournalState);
        Assert.Equal(payload, wrapped.OpaquePayload);
        Assert.StartsWith("openbao-wrap:", wrapped.ProviderOperationReceipt, StringComparison.Ordinal);

        var replay = await journal.IssueAsync(prepared.Token, prepared.Context,
            reference, OpenBaoKekWrappedMaterialProfileSource.Profile, CancellationToken.None);
        Assert.NotNull(replay);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(payload, replay.OpaquePayload);
        Assert.Null(await journal.IssueAsync(prepared.Token, Enumerable.Repeat((byte)7, 32).ToArray(),
            reference, OpenBaoKekWrappedMaterialProfileSource.Profile, CancellationToken.None));
        Assert.Null(await journal.IssueAsync(prepared.Token, prepared.Context,
            reference with { KeyProviderId = "another-provider" },
            OpenBaoKekWrappedMaterialProfileSource.Profile, CancellationToken.None));
        var wrongRevision = await journal.RecordWrappedAsync(prepared.Token, prepared.Context,
            issued.RowRevision + 100, "vault:v1:wrong-revision"u8.ToArray(),
            "openbao-transit:raw-export:1", CancellationToken.None);
        Assert.NotNull(wrongRevision);
        Assert.Equal("Conflict", wrongRevision.Outcome);
        var conflict = await journal.RecordWrappedAsync(prepared.Token, prepared.Context,
            issued.RowRevision, "vault:v1:conflicting-writer"u8.ToArray(), "openbao-transit:raw-export:1",
            CancellationToken.None);
        Assert.NotNull(conflict);
        Assert.Equal("Conflict", conflict.Outcome);
        Assert.Equal(payload, conflict.OpaquePayload);

        var cleanup = await journal.RequireCleanupAsync(prepared.Token, prepared.Context,
            wrapped.RowRevision, CancellationToken.None);
        Assert.NotNull(cleanup);
        Assert.Equal("CleanupRequired", cleanup.JournalState);
        Assert.StartsWith("openbao-cleanup:", cleanup.ProviderCleanupReference, StringComparison.Ordinal);
        var cleaned = await journal.CompleteCleanupAsync(cleanup.ProviderCleanupReference!,
            prepared.Context, CancellationToken.None);
        Assert.NotNull(cleaned);
        Assert.Equal("CleanedUp", cleaned.JournalState);
        Assert.Null(cleaned.OpaquePayload);
        Assert.StartsWith("openbao-cleaned:", cleaned.ProviderCleanupReceipt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostgreSql_journal_receipts_are_transition_owned_and_application_roles_have_no_table_Dml()
    {
        await using var db = postgres.CreateDbContext();
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT bool_and(NOT pg_catalog.has_table_privilege(role_name,
              'tagekyc.raw_export_openbao_kek_operation_journal','INSERT,UPDATE,DELETE')) AS "Value"
            FROM unnest(ARRAY['tagekyc_raw_export_custody_encryptor',
                              'tagekyc_raw_export_reconciler',
                              'tagekyc_raw_export_lifecycle']) role_name
            """).SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT bool_and(NOT ('p_receipt'=ANY(COALESCE(p.proargnames,ARRAY[]::text[])))) AS "Value"
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname IN
              ('raw_export_openbao_prove_absence','raw_export_openbao_complete_cleanup')
            """).SingleAsync());
    }

    [Fact]
    public async Task PostgreSql_journal_authoritative_absence_rejects_late_Transit_writer()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("openbao_journal_absence");
        var database = new PostgresPersistenceFixture(isolated.ConnectionString);
        var prepared = await PrepareOpaqueAsync(database);
        await using var db = database.CreateDbContext();
        var journal = new PostgresOpenBaoKekJournal(db);
        var reference = new KekReference(prepared.KeyProviderId, prepared.KekId,
            prepared.KekVersion, prepared.KekFingerprint);
        var issued = await journal.IssueAsync(prepared.Token, prepared.Context,
            reference, OpenBaoKekWrappedMaterialProfileSource.Profile, CancellationToken.None);
        Assert.NotNull(issued);

        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SET ROLE tagekyc_raw_export_deployer");
        await db.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',false)");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tagekyc.raw_export_attempt_key_reservations
            SET "CurrentPreparationLeaseExpiresAtUtc"=clock_timestamp()-interval '1 hour',
                "UpdatedAtUtc"=clock_timestamp()
            WHERE "AttemptKeyReservationId"={prepared.ReservationId}
            """);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','',false)");
        await db.Database.ExecuteSqlRawAsync("RESET ROLE");
        Assert.Equal("Expired", await db.Database.SqlQuery<string>($"""
            SELECT tagekyc.raw_export_mark_attempt_key_preparation_expired(
              {prepared.ReservationId},{prepared.PreparationId},{prepared.Fence}) AS "Value"
            """).SingleAsync());

        var absent = await journal.ProveAbsenceAsync(prepared.Token, prepared.Context,
            issued.RowRevision, CancellationToken.None);
        Assert.NotNull(absent);
        Assert.Equal("AbsenceProven", absent.JournalState);
        Assert.StartsWith("openbao-absence:", absent.ProviderAbsenceProofReceipt, StringComparison.Ordinal);
        Assert.Null(await journal.RecordWrappedAsync(prepared.Token, prepared.Context, issued.RowRevision,
            "vault:v1:late-writer"u8.ToArray(), "openbao-transit:raw-export:1", CancellationToken.None));
        var reread = await journal.ReadAsync(prepared.Token, prepared.Context, CancellationToken.None);
        Assert.NotNull(reread);
        Assert.Equal("AbsenceProven", reread.JournalState);
        Assert.Null(reread.OpaquePayload);
    }

    private Task<OpaquePreparation> PrepareOpaqueAsync(PostgresPersistenceFixture? database = null) =>
        PrepareAsync(OpenBaoKekWrappedMaterialProfileSource.Profile, database);

    private async Task<OpaquePreparation> PrepareAsync(
        KekWrappedMaterialProfile profile,
        PostgresPersistenceFixture? database = null)
    {
        database ??= postgres;
        await new Tip88C1B2CoreTests(database)
            .C1B2CORE_new_candidate_commits_full_recovery_context_atomically();
        await using var db = database.CreateDbContext();
        var source = await db.RawExportSourceEncryptionAttempts
            .OrderByDescending(row => row.CreatedAtUtc)
            .Select(row => new { row.AttemptKeyReservationId, row.AttemptId, row.SourceArtifactId })
            .FirstAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM tagekyc.raw_export_prepare_attempt_key_reservation(
              @reservation,@attempt,@source,@representation,@representationVersion,
              @scheme,@schemeVersion)
            """;
        command.Parameters.AddWithValue("reservation", source.AttemptKeyReservationId);
        command.Parameters.AddWithValue("attempt", source.AttemptId);
        command.Parameters.AddWithValue("source", source.SourceArtifactId);
        command.Parameters.AddWithValue("representation", profile.RepresentationId);
        command.Parameters.AddWithValue("representationVersion", profile.RepresentationVersion);
        command.Parameters.AddWithValue("scheme", profile.WrappingSchemeId);
        command.Parameters.AddWithValue("schemeVersion", profile.WrappingSchemeVersion);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("PreparingLive", reader.GetString(0));
        return new(
            source.AttemptKeyReservationId,
            reader.GetGuid(1),
            reader.GetGuid(2), reader.GetInt64(3),
            new ProviderOperationToken(reader.GetString(4)),
            (byte[])reader[6],
            reader.GetString(7), reader.GetString(8), reader.GetInt32(9), reader.GetString(10));
    }

    private async Task<string> RecordAsync(
        PostgresPersistenceFixture database,
        OpaquePreparation prepared,
        string representation,
        int representationVersion,
        byte[]? ciphertext,
        byte[]? nonce,
        byte[]? tag,
        byte[]? opaque,
        string scheme,
        int schemeVersion)
    {
        await using var db = database.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT tagekyc.raw_export_record_key_provider_wrapped_result(
              @operation,@reservation,@preparation,@fence,@token,
              @representation,@representationVersion,@ciphertext,@nonce,@tag,@opaque,
              @scheme,@schemeVersion,'test-operation-receipt','test-resource-reference')
            """;
        command.Parameters.AddWithValue("operation", prepared.ProviderOperationId);
        command.Parameters.AddWithValue("reservation", prepared.ReservationId);
        command.Parameters.AddWithValue("preparation", prepared.PreparationId);
        command.Parameters.AddWithValue("fence", prepared.Fence);
        command.Parameters.AddWithValue("token", prepared.Token.Value);
        command.Parameters.AddWithValue("representation", representation);
        command.Parameters.AddWithValue("representationVersion", representationVersion);
        command.Parameters.Add(new NpgsqlParameter("ciphertext", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = ciphertext ?? (object)DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("nonce", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = nonce ?? (object)DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("tag", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = tag ?? (object)DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("opaque", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = opaque ?? (object)DBNull.Value });
        command.Parameters.AddWithValue("scheme", scheme);
        command.Parameters.AddWithValue("schemeVersion", schemeVersion);
        return (string)(await command.ExecuteScalarAsync() ?? "StateConflict");
    }

    private static async Task<byte[]> ContextFingerprintAsync(
        TagEkycDbContext db,
        OpaquePreparation prepared,
        string domain,
        bool includeRepresentation,
        string? representationId = null,
        int? representationVersion = null,
        string? wrappingSchemeId = null,
        int? wrappingSchemeVersion = null,
        string? keyProviderId = null,
        int? kekVersion = null,
        string? kekFingerprint = null)
    {
        var source = await db.RawExportSourceEncryptionAttempts
            .Where(row => row.AttemptKeyReservationId == prepared.ReservationId)
            .Select(row => new { row.AttemptId, row.EncryptionAttemptFingerprint, row.KeyProviderId,
                row.KekId, row.KekVersion, row.KekFingerprint })
            .SingleAsync();
        var fields = new List<C1HashCanonical.Component>
        {
            new C1HashCanonical.Scalar(prepared.ReservationId.ToString("N")),
            new C1HashCanonical.Scalar(source.AttemptId.ToString("N")),
            new C1HashCanonical.Scalar(Convert.ToHexString(source.EncryptionAttemptFingerprint).ToLowerInvariant()),
            new C1HashCanonical.Scalar(keyProviderId ?? source.KeyProviderId),
            new C1HashCanonical.Scalar(source.KekId),
            new C1HashCanonical.Scalar((kekVersion ?? source.KekVersion).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(kekFingerprint ?? source.KekFingerprint),
        };
        if (includeRepresentation)
        {
            fields.Add(new C1HashCanonical.Scalar(representationId ?? KekWrappedMaterialRepresentations.OpaqueProviderCiphertext));
            fields.Add(new C1HashCanonical.Scalar((representationVersion ?? 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        fields.Add(new C1HashCanonical.Scalar(wrappingSchemeId ?? (includeRepresentation
            ? OpenBaoKekOptions.WrappingSchemeId
            : "AES-256-GCM")));
        fields.Add(new C1HashCanonical.Scalar((wrappingSchemeVersion ?? 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return C1HashCanonical.Compute(domain, fields.ToArray());
    }

    private static IConfiguration Configuration(string address, string fingerprint) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{OpenBaoKekOptions.SectionName}:Address"] = address,
            [$"{OpenBaoKekOptions.SectionName}:RoleIdSecretRef"] = "env:OPENBAO_ROLE_ID",
            [$"{OpenBaoKekOptions.SectionName}:SecretIdSecretRef"] = "env:OPENBAO_SECRET_ID",
            [$"{OpenBaoKekOptions.SectionName}:TransitMount"] = "transit",
            [$"{OpenBaoKekOptions.SectionName}:KeyName"] = "raw-export",
            [$"{OpenBaoKekOptions.SectionName}:KeyVersion"] = "7",
            [$"{OpenBaoKekOptions.SectionName}:KeyFingerprint"] = fingerprint,
        }).Build();

    private static async Task<bool> ExistsAsync(TagEkycDbContext db, string expression) =>
        await db.Database.SqlQueryRaw<bool>($"SELECT ({expression}) AS \"Value\"").SingleAsync();

    private static async Task<Dictionary<string, FunctionBoundary>> ReadFunctionBoundaryAsync(
        string connectionString,
        IEnumerable<string> signatures)
    {
        var result = new Dictionary<string, FunctionBoundary>(StringComparer.Ordinal);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var signature in signatures)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT pg_catalog.encode(tagekyc_extensions.digest(pg_catalog.convert_to(
                         pg_catalog.replace(p.prosrc,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10)),
                         'UTF8'),'sha256'),'hex'),
                       pg_catalog.pg_get_userbyid(p.proowner),p.prosecdef,
                       COALESCE(pg_catalog.array_to_string(p.proconfig,E'\n'),''),
                       COALESCE(pg_catalog.array_to_string(p.proacl,E'\n'),'')
                FROM pg_catalog.pg_proc p WHERE p.oid=pg_catalog.to_regprocedure(@signature)
                """;
            command.Parameters.AddWithValue("signature", signature);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync(), $"Missing predecessor function: {signature}");
            result.Add(signature, new(
                reader.GetString(0), reader.GetString(1), reader.GetBoolean(2),
                reader.GetString(3), reader.GetString(4)));
        }
        return result;
    }

    private static async Task<int> CountStageRightsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pg_catalog.count(*)
            FROM pg_catalog.pg_proc p
            CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) acl
            JOIN pg_catalog.pg_roles r ON r.oid=acl.grantee
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND acl.privilege_type='EXECUTE'
              AND r.rolname IN ('tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle')
            """;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task AssertPredecessorStageRightsAsync(string connectionString)
    {
        var predecessor = CaptureRuntimeCustodyProviderScopes.StageRights
            .Where(right => !right.Signature.StartsWith("raw_export_openbao_", StringComparison.Ordinal))
            .Select(right => (Signature: right.Signature switch
                {
                    "raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid,text,integer,text,integer)" =>
                        "raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)",
                    "raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text)" =>
                        "raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)",
                    "raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text)" =>
                        "raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)",
                    "raw_export_read_active_attempt_key_material(uuid)" =>
                        "raw_export_read_active_attempt_key_envelope(uuid)",
                    _ => right.Signature,
                }, right.Roles))
            .ToArray();
        Assert.Equal(41, predecessor.Length);
        var roles = new[]
        {
            (Name: "tagekyc_raw_export_custody_encryptor", Bit: 1),
            (Name: "tagekyc_raw_export_reconciler", Bit: 2),
            (Name: "tagekyc_raw_export_lifecycle", Bit: 4),
        };
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var right in predecessor)
        foreach (var role in roles)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_catalog.has_function_privilege(@role,@signature,'EXECUTE')";
            command.Parameters.AddWithValue("role", role.Name);
            command.Parameters.AddWithValue("signature", "tagekyc." + right.Signature);
            Assert.Equal((right.Roles & role.Bit) != 0, await command.ExecuteScalarAsync());
        }
    }

    private static async Task<Guid> ReadAttemptIdAsync(string connectionString, Guid reservationId) =>
        await ReadReservationIdentityAsync(connectionString, reservationId, "\"AttemptId\"");

    private static async Task<Guid> ReadSourceArtifactIdAsync(string connectionString, Guid reservationId) =>
        await ReadReservationIdentityAsync(connectionString, reservationId, "a.\"SourceArtifactId\"");

    private static async Task<Guid> ReadReservationIdentityAsync(
        string connectionString, Guid reservationId, string projection)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {projection}
            FROM tagekyc.raw_export_source_encryption_attempts a
            WHERE a."AttemptKeyReservationId"=@reservation
            """;
        command.Parameters.AddWithValue("reservation", reservationId);
        return (Guid)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<LegacyMaterialSnapshot> ReadLegacyMaterialAsync(
        string connectionString,
        Guid providerOperationId,
        bool includeRepresentation)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag","WrappedDekMetadataDigest",
                   "WrappingSuiteId","WrappingSuiteVersion","ProviderResourceReference","ProviderOperationReceipt"
                   { (includeRepresentation ? ",\"MaterialRepresentationId\",\"MaterialRepresentationVersion\",\"OpaqueWrappedDekPayload\"" : string.Empty) }
            FROM tagekyc.raw_export_key_provider_operations
            WHERE "ProviderOperationId"=@operation
            """;
        command.Parameters.AddWithValue("operation", providerOperationId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new(
            (byte[])reader[0], (byte[])reader[1], (byte[])reader[2], (byte[])reader[3],
            reader.GetString(4), reader.GetInt32(5), reader.GetString(6), reader.GetString(7),
            includeRepresentation ? reader.GetString(8) : null,
            includeRepresentation ? reader.GetInt32(9) : null,
            includeRepresentation && !reader.IsDBNull(10) ? (byte[])reader[10] : null);
    }

    private static void AssertLegacyMaterialEqual(
        LegacyMaterialSnapshot expected,
        LegacyMaterialSnapshot actual)
    {
        Assert.Equal(expected.Ciphertext, actual.Ciphertext);
        Assert.Equal(expected.Nonce, actual.Nonce);
        Assert.Equal(expected.Tag, actual.Tag);
        Assert.Equal(expected.MetadataDigest, actual.MetadataDigest);
        Assert.Equal(expected.WrappingSuiteId, actual.WrappingSuiteId);
        Assert.Equal(expected.WrappingSuiteVersion, actual.WrappingSuiteVersion);
        Assert.Equal(expected.ProviderResourceReference, actual.ProviderResourceReference);
        Assert.Equal(expected.ProviderOperationReceipt, actual.ProviderOperationReceipt);
        Assert.Equal(expected.RepresentationId, actual.RepresentationId);
        Assert.Equal(expected.RepresentationVersion, actual.RepresentationVersion);
        Assert.Equal(expected.OpaquePayload, actual.OpaquePayload);
    }

    private sealed record OpaquePreparation(
        Guid ReservationId,
        Guid ProviderOperationId,
        Guid PreparationId,
        long Fence,
        ProviderOperationToken Token,
        byte[] Context,
        string KeyProviderId,
        string KekId,
        int KekVersion,
        string KekFingerprint);

    private sealed record LegacyMaterialSnapshot(
        byte[] Ciphertext,
        byte[] Nonce,
        byte[] Tag,
        byte[] MetadataDigest,
        string WrappingSuiteId,
        int WrappingSuiteVersion,
        string ProviderResourceReference,
        string ProviderOperationReceipt,
        string? RepresentationId,
        int? RepresentationVersion,
        byte[]? OpaquePayload);

    private sealed record FunctionBoundary(
        string BodySha256,
        string Owner,
        bool SecurityDefiner,
        string Configuration,
        string Acl);

    private sealed class UnavailableUnwrapProvider : IKekOperationProvider
    {
        public Task<KekWrapResult> WrapDekAsync(
            KekReference reference, ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, IAttemptDekCandidate candidate,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<KekOperationLookup> LookupByOperationTokenAsync(
            ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AttemptDekLease> UnwrapDekAsync(
            KekReference reference, KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken) =>
            Task.FromException<AttemptDekLease>(new IOException("openbao-unavailable"));
    }
}
