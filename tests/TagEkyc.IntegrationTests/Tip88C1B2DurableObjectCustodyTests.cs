using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2DurableObjectCustodyTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string Migration = "20260804120000_Tip88C1B2DurableObjectCustody";
    private const string PreviousMigration = "20260803151824_Tip88C1B2DurableKeyFixtureProof";

    private static readonly string[] Tables =
    [
        "raw_export_provisional_objects",
        "raw_export_provisional_object_events",
    ];

    private static readonly string[] Constraints =
    [
        "pk_raw_export_provisional_objects", "uq_raw_export_provisional_objects_attempt",
        "uq_raw_export_provisional_objects_identity", "uq_raw_export_provisional_objects_key",
        "uq_raw_export_source_attempt_object_binding", "fk_raw_export_provisional_objects_attempt_binding",
        "ck_raw_export_provisional_objects_state", "ck_raw_export_provisional_objects_values",
        "ck_raw_export_provisional_objects_sparse", "pk_raw_export_provisional_object_events",
        "uq_raw_export_provisional_object_events_sequence", "uq_raw_export_provisional_object_events_revision",
        "fk_raw_export_provisional_object_events_head", "ck_raw_export_provisional_object_events_values",
    ];

    private static readonly string[] Functions =
    [
        "compute_raw_export_provisional_object_binding", "enforce_raw_export_provisional_object_write",
        "enforce_raw_export_provisional_event_append", "raw_export_begin_provisional_object_custody",
        "raw_export_arm_provisional_object_put", "raw_export_record_provisional_object_not_armed",
        "raw_export_record_provisional_object_put_result", "raw_export_resolve_provisional_object_put_outcome",
        "raw_export_mark_provisional_object_verified", "raw_export_mark_provisional_object_cleanup_required",
        "raw_export_record_provisional_object_delete_acknowledged",
        "raw_export_record_provisional_object_absence_confirmed",
        "raw_export_record_provisional_object_quarantined",
        "raw_export_read_provisional_object_reconcile_context",
        "raw_export_read_provisional_object_lifecycle_context",
    ];

    private static readonly string[] Triggers =
    [
        "trg_raw_export_provisional_object_write", "trg_raw_export_provisional_event_append",
    ];

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task O01_intended_identifiers_round_trip_exactly_and_fit_63_bytes()
    {
        var intendedNames = Tables.Concat(Constraints).Concat(Functions).Concat(Triggers).ToArray();
        await using var connection = await OpenAsync();
        var observedTables = await ReadStringsAsync(connection,
            "SELECT relname FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='tagekyc' AND relname=ANY(@names) ORDER BY 1", Tables);
        var observedConstraints = await ReadStringsAsync(connection,
            "SELECT conname FROM pg_catalog.pg_constraint c JOIN pg_catalog.pg_namespace n ON n.oid=c.connamespace WHERE n.nspname='tagekyc' AND conname=ANY(@names) ORDER BY 1", Constraints);
        var observedFunctions = await ReadStringsAsync(connection,
            "SELECT proname FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='tagekyc' AND proname=ANY(@names) ORDER BY 1", Functions);
        var observedTriggers = await ReadStringsAsync(connection,
            "SELECT tgname FROM pg_catalog.pg_trigger t JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='tagekyc' AND NOT t.tgisinternal AND tgname=ANY(@names) ORDER BY 1", Triggers);
        Assert.Multiple(
            () => Assert.All(intendedNames, name => Assert.InRange(Encoding.UTF8.GetByteCount(name), 1, 63)),
            () => Assert.Equal(Tables.Order(), observedTables.Order()),
            () => Assert.Equal(Constraints.Order(), observedConstraints.Order()),
            () => Assert.Equal(Functions.Order(), observedFunctions.Order()),
            () => Assert.Equal(Triggers.Order(), observedTriggers.Order()));
    }

    [Fact]
    public async Task O02_head_event_sparse_and_typed_value_manifests_are_exact()
    {
        var valid = await CreateObjectAsync();
        await AssertCheckViolationAsync(
            valid.ObjectCustodyId,
            "UPDATE tagekyc.raw_export_provisional_objects SET \"State\"='UnknownState' WHERE \"ObjectCustodyId\"=@id",
            "ck_raw_export_provisional_objects_state",
            "tip88c1-object-head-write-v1",
            "ALTER TABLE tagekyc.raw_export_provisional_objects DROP CONSTRAINT ck_raw_export_provisional_objects_sparse");
        await AssertCheckViolationAsync(
            valid.ObjectCustodyId,
            "UPDATE tagekyc.raw_export_provisional_objects SET \"StateRevision\"=0 WHERE \"ObjectCustodyId\"=@id",
            "ck_raw_export_provisional_objects_values",
            "tip88c1-object-head-write-v1");
        await AssertCheckViolationAsync(
            valid.ObjectCustodyId,
            "UPDATE tagekyc.raw_export_provisional_objects SET \"PutOperationId\"=pg_catalog.gen_random_uuid() WHERE \"ObjectCustodyId\"=@id",
            "ck_raw_export_provisional_objects_sparse",
            "tip88c1-object-head-write-v1");
        await AssertCheckViolationAsync(
            valid.ObjectCustodyId,
            """
            INSERT INTO tagekyc.raw_export_provisional_object_events(
                "ObjectCustodyEventId","ObjectCustodyId","EventSequence","FromState","ToState",
                "ActorKind","StateRevision","EvidenceDigest","EventAtUtc","SchemaVersion")
            SELECT pg_catalog.gen_random_uuid(),"ObjectCustodyId",2,'Initiated','Initiated','Intruder',2,
                   "EvidenceDigest",pg_catalog.statement_timestamp(),1
            FROM tagekyc.raw_export_provisional_object_events
            WHERE "ObjectCustodyId"=@id AND "EventSequence"=1
            """,
            "ck_raw_export_provisional_object_events_values",
            "tip88c1-object-event-append-v1");

        await using (var verify = await OpenAsync())
        {
            Assert.Equal("Initiated", await ScalarStringAsync(verify,
                "SELECT \"State\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                ("id", valid.ObjectCustodyId)));
            Assert.Equal(1L, await ScalarLongAsync(verify,
                "SELECT count(*) FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id",
                ("id", valid.ObjectCustodyId)));
        }

        var notArmed = await CreateObjectAsync();
        var notArmedEvidence = HashCanonical(
            "tip-88c1-object-not-armed-evidence-v1",
            Convert.ToHexString(notArmed.ObjectBindingDigest).ToLowerInvariant(),
            notArmed.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "NotArmed");
        await using (var writer = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
            Assert.Equal("NotArmed", (await RecordNotArmedAsync(
                writer, notArmed.ObjectCustodyId, notArmed.StateRevision, notArmedEvidence)).OutcomeCode);

        var positiveAbsence = await CreateUnknownObjectAsync();
        var first = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-2));
        var second = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-1));
        var absenceEvidence = ReconcileEvidence(
            positiveAbsence.Object.Object.ObjectBindingDigest,
            positiveAbsence.Object.PutOperationId,
            "PositiveAbsence",
            first,
            second,
            null,
            null);
        await using (var reconciler = await OpenAsAsync("tagekyc_raw_export_reconciler"))
            Assert.Equal("PositiveAbsence", (await ResolvePutOutcomeAsync(
                reconciler,
                positiveAbsence.Object.Object.ObjectCustodyId,
                positiveAbsence.UnknownRevision,
                "PositiveAbsence",
                null,
                null,
                first,
                second,
                absenceEvidence)).OutcomeCode);

        await using var distinctVerify = await OpenAsync();
        Assert.Equal("NotArmed", await ScalarStringAsync(distinctVerify,
            "SELECT \"PutOutcomeKind\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", notArmed.ObjectCustodyId)));
        Assert.Equal("PositiveAbsence", await ScalarStringAsync(distinctVerify,
            "SELECT \"PutOutcomeKind\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", positiveAbsence.Object.Object.ObjectCustodyId)));
    }

    [Fact]
    public async Task O03_one_attempt_can_create_only_one_object_custody_row()
    {
        var active = await CreateActiveAttemptAsync();
        CreatedObject canonical;
        await using (var writer = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
            canonical = await BeginObjectAsync(writer, active, 64);

        var duplicateIdentity = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var context = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true)",
            connection,
            transaction))
            await context.ExecuteNonQueryAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO tagekyc.raw_export_provisional_objects(
                "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
                "EncryptionAttemptRevision","AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest",
                "State","StateRevision","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
            SELECT pg_catalog.gen_random_uuid(),"AttemptId","AttemptKeyReservationId","SourceArtifactId",@identity,
                   "EncryptionAttemptRevision","Fence","EncryptionAttemptFingerprint",@key,
                   tagekyc.compute_raw_export_provisional_object_binding(
                       "AttemptId","AttemptKeyReservationId","SourceArtifactId",@identity,
                       "EncryptionAttemptRevision","Fence","EncryptionAttemptFingerprint",@key),
                   'Initiated',1,pg_catalog.statement_timestamp(),pg_catalog.statement_timestamp(),1
            FROM tagekyc.raw_export_source_encryption_attempts
            WHERE "AttemptId"=@attempt
            """, connection, transaction);
        command.Parameters.AddWithValue("identity", duplicateIdentity);
        command.Parameters.AddWithValue("key", $"raw-export/c1/v1/{duplicateIdentity:N}");
        command.Parameters.AddWithValue("attempt", active.AttemptId);
        PostgresException? exception = null;
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException observed)
        {
            exception = observed;
        }

        if (exception is null)
        {
            await using var count = new NpgsqlCommand(
                "SELECT count(*) FROM tagekyc.raw_export_provisional_objects WHERE \"AttemptId\"=@attempt",
                connection,
                transaction);
            count.Parameters.AddWithValue("attempt", active.AttemptId);
            var observedRowCount = Assert.IsType<long>(await count.ExecuteScalarAsync());
            Assert.Fail(
                $"EXPECTED_ONE_OBJECT_PER_ATTEMPT_BUT_SECOND_INSERT_SUCCEEDED; observed row count: {observedRowCount}");
        }

        Assert.True(
            exception.SqlState == "23505"
            && exception.ConstraintName == "uq_raw_export_provisional_objects_attempt",
            $"Expected 23505 / uq_raw_export_provisional_objects_attempt; observed {exception.SqlState} / {exception.ConstraintName}");
        await transaction.RollbackAsync();

        await using var verify = await OpenAsync();
        Assert.Equal(1L, await ScalarLongAsync(verify,
            "SELECT count(*) FROM tagekyc.raw_export_provisional_objects WHERE \"AttemptId\"=@attempt",
            ("attempt", active.AttemptId)));
        await using var readCanonical = new NpgsqlCommand(
            "SELECT \"ObjectCustodyId\" FROM tagekyc.raw_export_provisional_objects WHERE \"AttemptId\"=@attempt",
            verify);
        readCanonical.Parameters.AddWithValue("attempt", active.AttemptId);
        Assert.Equal(canonical.ObjectCustodyId, Assert.IsType<Guid>(await readCanonical.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task O04_object_binding_composite_fk_rejects_cross_attempt_values()
    {
        var active = await CreateActiveAttemptAsync();
        var sibling = await SeedNonterminalSiblingObjectAsync(active);
        var foreignIdentity = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await SetActorAndRoleAsync(connection, "tagekyc_raw_export_deployer");
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',false);
            INSERT INTO tagekyc.raw_export_provisional_objects(
                "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
                "EncryptionAttemptRevision","AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest",
                "State","StateRevision","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
            SELECT pg_catalog.gen_random_uuid(),@activeAttempt,@siblingReservation,"SourceArtifactId",@foreignIdentity,
                   "EncryptionAttemptRevision","Fence","EncryptionAttemptFingerprint",
                   'raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower(@foreignIdentity::text),'-',''),
                   tagekyc.compute_raw_export_provisional_object_binding(
                       @activeAttempt,@siblingReservation,"SourceArtifactId",@foreignIdentity,
                       "EncryptionAttemptRevision","Fence","EncryptionAttemptFingerprint",
                       'raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower(@foreignIdentity::text),'-','')),
                   'Initiated',1,pg_catalog.statement_timestamp(),pg_catalog.statement_timestamp(),1
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=@activeAttempt;
            """, connection);
        command.Parameters.AddWithValue("activeAttempt", active.AttemptId);
        command.Parameters.AddWithValue("siblingReservation", sibling.AttemptKeyReservationId);
        command.Parameters.AddWithValue("foreignIdentity", foreignIdentity);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23503", exception.SqlState);
        Assert.Equal("fk_raw_export_provisional_objects_attempt_binding", exception.ConstraintName);
        await using var verify = await OpenAsync();
        Assert.Equal(1L, await ScalarLongAsync(verify,
            "SELECT count(*) FROM tagekyc.raw_export_provisional_objects WHERE \"SourceArtifactId\"=@source",
            ("source", active.SourceArtifactId)));
    }

    [Fact]
    public async Task O05_begin_derives_key_binding_and_absolute_digest_vectors()
    {
        var attempt = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var reservation = Guid.Parse("11112222-3333-4444-5555-666677778888");
        var source = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var identity = Guid.Parse("12345678-9abc-def0-1234-56789abcdef0");
        var fingerprint = Enumerable.Range(0, 32).Select(x => (byte)x).ToArray();
        var key = "raw-export/c1/v1/" + identity.ToString("N");
        var expected = HashCanonical("tip-88c1-provisional-object-binding-v1", attempt.ToString("N"), reservation.ToString("N"), source.ToString("N"), identity.ToString("N"), "7", "9", Convert.ToHexString(fingerprint).ToLowerInvariant(), key);
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("SELECT tagekyc.compute_raw_export_provisional_object_binding(@a,@r,@s,@i,7,9,@f,@k)", connection);
        command.Parameters.AddWithValue("a", attempt); command.Parameters.AddWithValue("r", reservation);
        command.Parameters.AddWithValue("s", source); command.Parameters.AddWithValue("i", identity);
        command.Parameters.AddWithValue("f", fingerprint); command.Parameters.AddWithValue("k", key);
        Assert.Equal(expected, Assert.IsType<byte[]>(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task O06_writer_arm_is_revision_fenced_and_idempotent()
    {
        var positive = await CreateObjectAsync();
        var positiveOperationId = Guid.NewGuid();
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var armed = await ArmAsync(connection, positive.ObjectCustodyId, positive.StateRevision, positiveOperationId);
            Assert.Equal("Armed", armed.OutcomeCode);
            Assert.Equal("PutInFlight", armed.ObjectState);
            Assert.Equal(positive.StateRevision + 1, armed.StateRevision);
            var replay = await ArmAsync(connection, positive.ObjectCustodyId, armed.StateRevision, positiveOperationId);
            Assert.Equal("ExistingMatch", replay.OutcomeCode);
            Assert.Equal("PutInFlight", replay.ObjectState);
            Assert.Equal(armed.StateRevision, replay.StateRevision);
        }
        await using (var positiveVerify = await OpenAsync())
            Assert.Equal(2L, await ScalarLongAsync(positiveVerify,
                "SELECT count(*) FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id",
                ("id", positive.ObjectCustodyId)));

        var stale = await CreateObjectAsync();
        await using var beforeConnection = await OpenAsync();
        var eventCountBefore = await ScalarLongAsync(beforeConnection,
            "SELECT count(*) FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id",
            ("id", stale.ObjectCustodyId));
        await using var staleConnection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        var conflict = await ArmAsync(
            staleConnection,
            stale.ObjectCustodyId,
            stale.StateRevision + 1,
            Guid.NewGuid());

        Assert.Equal("StateConflict", conflict.OutcomeCode);
        Assert.Equal("Initiated", conflict.ObjectState);
        Assert.Equal(stale.StateRevision, conflict.StateRevision);
        await using var verifyConnection = await OpenAsync();
        Assert.Equal("Initiated", await ScalarStringAsync(verifyConnection,
            "SELECT \"State\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", stale.ObjectCustodyId)));
        Assert.Equal(stale.StateRevision, await ScalarLongAsync(verifyConnection,
            "SELECT \"StateRevision\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", stale.ObjectCustodyId)));
        Assert.Equal(eventCountBefore, await ScalarLongAsync(verifyConnection,
            "SELECT count(*) FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id",
            ("id", stale.ObjectCustodyId)));
    }
    [Fact]
    public async Task O07_created_put_records_exact_bounded_ciphertext_and_canonical_evidence()
    {
        const long maximumCiphertextLength = 134_217_728;

        var oversized = await CreateArmedObjectAsync();
        var oversizedDigest = SHA256.HashData("oversized"u8);
        var oversizedEvidence = PutCreatedEvidence(
            oversized.Object.ObjectBindingDigest,
            oversized.PutOperationId,
            maximumCiphertextLength + 1,
            oversizedDigest);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => RecordPutResultAsync(
                connection,
                oversized.Object.ObjectCustodyId,
                oversized.ArmedRevision,
                oversized.PutOperationId,
                "Created",
                200,
                maximumCiphertextLength + 1,
                oversizedDigest,
                oversizedEvidence));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", exception.MessageText);
        }
        await AssertObjectStateAsync(oversized.Object.ObjectCustodyId, "PutInFlight", oversized.ArmedRevision, 2);

        var wrongEvidence = await CreateArmedObjectAsync();
        var wrongEvidenceDigest = SHA256.HashData("valid-ciphertext"u8);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => RecordPutResultAsync(
                connection,
                wrongEvidence.Object.ObjectCustodyId,
                wrongEvidence.ArmedRevision,
                wrongEvidence.PutOperationId,
                "Created",
                200,
                maximumCiphertextLength,
                wrongEvidenceDigest,
                new byte[32]));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", exception.MessageText);
        }
        await AssertObjectStateAsync(wrongEvidence.Object.ObjectCustodyId, "PutInFlight", wrongEvidence.ArmedRevision, 2);

        var created = await CreateArmedObjectAsync();
        var ciphertextDigest = SHA256.HashData("exact-bounded-ciphertext"u8);
        var createdEvidence = PutCreatedEvidence(
            created.Object.ObjectBindingDigest,
            created.PutOperationId,
            maximumCiphertextLength,
            ciphertextDigest);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var result = await RecordPutResultAsync(
                connection,
                created.Object.ObjectCustodyId,
                created.ArmedRevision,
                created.PutOperationId,
                "Created",
                200,
                maximumCiphertextLength,
                ciphertextDigest,
                createdEvidence);
            Assert.Equal("Recorded", result.OutcomeCode);
            Assert.Equal("ObjectPresentPendingVerification", result.ObjectState);
            Assert.Equal(created.ArmedRevision + 1, result.StateRevision);
        }
        await using (var connection = await OpenAsync())
        {
            Assert.Equal(maximumCiphertextLength, await ScalarLongAsync(connection,
                "SELECT \"CiphertextLength\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                ("id", created.Object.ObjectCustodyId)));
            Assert.Equal(createdEvidence, await ScalarBytesAsync(connection,
                "SELECT \"EvidenceDigest\" FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id ORDER BY \"EventSequence\" DESC LIMIT 1",
                ("id", created.Object.ObjectCustodyId)));
        }

        var unknown = await CreateArmedObjectAsync();
        var unknownEvidence = HashCanonical(
            "tip-88c1-object-put-unknown-evidence-v1",
            Convert.ToHexString(unknown.Object.ObjectBindingDigest).ToLowerInvariant(),
            unknown.PutOperationId.ToString("N"),
            unknown.ArmedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "OutcomeUnknown");
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var result = await RecordPutResultAsync(
                connection,
                unknown.Object.ObjectCustodyId,
                unknown.ArmedRevision,
                unknown.PutOperationId,
                "OutcomeUnknown",
                null,
                null,
                null,
                null);
            Assert.Equal("OutcomeUnknown", result.OutcomeCode);
            Assert.Equal("PutOutcomeUnknown", result.ObjectState);
        }
        await using (var connection = await OpenAsync())
            Assert.Equal(unknownEvidence, await ScalarBytesAsync(connection,
                "SELECT \"EvidenceDigest\" FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id ORDER BY \"EventSequence\" DESC LIMIT 1",
                ("id", unknown.Object.ObjectCustodyId)));
    }
    [Fact]
    public async Task O08_lost_success_response_recovers_the_existing_exact_object()
    {
        var unknown = await CreateUnknownObjectAsync();
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        Guid provisionalObjectIdentity;
        string objectKey;
        await using (var lookup = await OpenAsync())
        await using (var command = new NpgsqlCommand(
                         "SELECT \"ProvisionalObjectIdentity\",\"ObjectKey\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                         lookup))
        {
            command.Parameters.AddWithValue("id", unknown.Object.Object.ObjectCustodyId);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            provisionalObjectIdentity = reader.GetGuid(0);
            objectKey = reader.GetString(1);
        }

        var locator = new ExactObjectLocator(
            provisionalObjectIdentity,
            objectKey,
            unknown.Object.Object.ObjectBindingDigest);
        var ciphertext = Enumerable.Range(0, 4096).Select(value => (byte)value).ToArray();
        using (var writer = new S3CompatibleProvisionalObjectWriter(
                   minio.Options(ProvisionalObjectCapability.Writer)))
        await using (var body = new MemoryStream(ciphertext, writable: false))
            Assert.Equal(ConditionalPutOutcome.Created, (await writer.PutIfAbsentAsync(
                new ExactWriteRequest(locator, unknown.Object.PutOperationId, ciphertext.Length),
                body,
                CancellationToken.None)).Outcome);

        using var reconciler = new S3CompatibleProvisionalObjectReconciler(
            minio.Options(ProvisionalObjectCapability.Reconciler));
        var inspection = await reconciler.InspectExactAsync(locator, CancellationToken.None);
        Assert.Equal(ExactObjectInspectionOutcome.Present, inspection.Outcome);
        Assert.Equal(ciphertext.Length, inspection.CiphertextLength);
        Assert.Equal(locator.ObjectBindingDigest, inspection.ObjectBindingDigest);
        Assert.Equal(ProvisionalObjectDigests.PutOperation(unknown.Object.PutOperationId), inspection.PutOperationDigest);

        var observedAt = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-1));
        var ciphertextLength = inspection.CiphertextLength!.Value;
        var ciphertextDigest = SHA256.HashData(ciphertext);
        var evidence = ReconcileEvidence(
            unknown.Object.Object.ObjectBindingDigest,
            unknown.Object.PutOperationId,
            "RecoveredPresent",
            observedAt,
            null,
            ciphertextLength,
            ciphertextDigest);

        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => ResolvePutOutcomeAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                unknown.UnknownRevision,
                "RecoveredPresent",
                ciphertextLength,
                ciphertextDigest,
                observedAt,
                null,
                new byte[32]));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", exception.MessageText);
        }
        await AssertObjectStateAsync(unknown.Object.Object.ObjectCustodyId, "PutOutcomeUnknown", unknown.UnknownRevision, 3);

        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var result = await ResolvePutOutcomeAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                unknown.UnknownRevision,
                "RecoveredPresent",
                ciphertextLength,
                ciphertextDigest,
                observedAt,
                null,
                evidence);
            Assert.Equal("RecoveredPresent", result.OutcomeCode);
            Assert.Equal("ObjectPresentPendingVerification", result.ObjectState);
            Assert.Equal(unknown.UnknownRevision + 1, result.StateRevision);
        }
        await using var verify = await OpenAsync();
        Assert.Equal(ciphertextLength, await ScalarLongAsync(verify,
            "SELECT \"CiphertextLength\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", unknown.Object.Object.ObjectCustodyId)));
        Assert.Equal(evidence, await ScalarBytesAsync(verify,
            "SELECT \"ProviderReceiptDigest\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", unknown.Object.Object.ObjectCustodyId)));
        Assert.Equal(evidence, await ScalarBytesAsync(verify,
            "SELECT \"EvidenceDigest\" FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id ORDER BY \"EventSequence\" DESC LIMIT 1",
            ("id", unknown.Object.Object.ObjectCustodyId)));
    }

    [Fact]
    public async Task O09_absence_requires_not_armed_or_quiesced_two_exact_observations()
    {
        var active = await CreateArmedObjectAsync();
        var first = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-2));
        var second = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-1));
        var activeEvidence = ReconcileEvidence(
            active.Object.ObjectBindingDigest,
            active.PutOperationId,
            "PositiveAbsence",
            first,
            second,
            null,
            null);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => ResolvePutOutcomeAsync(
                connection,
                active.Object.ObjectCustodyId,
                active.ArmedRevision,
                "PositiveAbsence",
                null,
                null,
                first,
                second,
                activeEvidence));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE", exception.MessageText);
        }
        await AssertObjectStateAsync(active.Object.ObjectCustodyId, "PutInFlight", active.ArmedRevision, 2);

        var unknown = await CreateUnknownObjectAsync();
        var oneObservationEvidence = ReconcileEvidence(
            unknown.Object.Object.ObjectBindingDigest,
            unknown.Object.PutOperationId,
            "PositiveAbsence",
            first,
            null,
            null,
            null);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => ResolvePutOutcomeAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                unknown.UnknownRevision,
                "PositiveAbsence",
                null,
                null,
                first,
                null,
                oneObservationEvidence));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", exception.MessageText);
        }
        await AssertObjectStateAsync(unknown.Object.Object.ObjectCustodyId, "PutOutcomeUnknown", unknown.UnknownRevision, 3);

        var absenceEvidence = ReconcileEvidence(
            unknown.Object.Object.ObjectBindingDigest,
            unknown.Object.PutOperationId,
            "PositiveAbsence",
            first,
            second,
            null,
            null);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var result = await ResolvePutOutcomeAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                unknown.UnknownRevision,
                "PositiveAbsence",
                null,
                null,
                first,
                second,
                absenceEvidence);
            Assert.Equal("PositiveAbsence", result.OutcomeCode);
            Assert.Equal("NoObjectEstablished", result.ObjectState);
        }
        await using (var verify = await OpenAsync())
        {
            Assert.Equal("PositiveAbsence", await ScalarStringAsync(verify,
                "SELECT \"PutOutcomeKind\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                ("id", unknown.Object.Object.ObjectCustodyId)));
            Assert.True(await ScalarIsNullAsync(verify,
                "SELECT \"ProviderReceiptDigest\" IS NULL FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                ("id", unknown.Object.Object.ObjectCustodyId)));
        }

        var notArmed = await CreateObjectAsync();
        var notArmedEvidence = HashCanonical(
            "tip-88c1-object-not-armed-evidence-v1",
            Convert.ToHexString(notArmed.ObjectBindingDigest).ToLowerInvariant(),
            notArmed.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "NotArmed");
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var result = await RecordNotArmedAsync(
                connection,
                notArmed.ObjectCustodyId,
                notArmed.StateRevision,
                notArmedEvidence);
            Assert.Equal("NotArmed", result.OutcomeCode);
            Assert.Equal("NoObjectEstablished", result.ObjectState);
        }
        await using (var verify = await OpenAsync())
            Assert.Equal("NotArmed", await ScalarStringAsync(verify,
                "SELECT \"PutOutcomeKind\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                ("id", notArmed.ObjectCustodyId)));
    }
    [Fact]
    public async Task O10_conditional_conflict_is_reconciled_without_overwrite_or_auto_delete()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var identity = Guid.NewGuid();
        var binding = SHA256.HashData("o10-binding"u8);
        var operation = Guid.NewGuid();
        var locator = new ExactObjectLocator(identity, $"raw-export/c1/v1/{identity:N}", binding);
        var original = "first-sentinel-ciphertext"u8.ToArray();
        var replacement = "second-must-not-overwrite"u8.ToArray();

        using var writer = new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer));
        await using var originalStream = new MemoryStream(original, writable: false);
        var created = await writer.PutIfAbsentAsync(
            new ExactWriteRequest(locator, operation, original.Length),
            originalStream,
            CancellationToken.None);
        Assert.Equal(ConditionalPutOutcome.Created, created.Outcome);
        Assert.Equal(200, created.StatusCode);
        Assert.Equal(original, await ReadObjectBytesAsync(minio, locator.ObjectKey));
        Assert.Equal(PutCreatedEvidence(binding, operation, original.Length, SHA256.HashData(original)), created.ProviderReceiptDigest);

        var competingOperation = Guid.NewGuid();
        await using var replacementStream = new MemoryStream(replacement, writable: false);
        var conflict = await writer.PutIfAbsentAsync(
            new ExactWriteRequest(locator, competingOperation, replacement.Length),
            replacementStream,
            CancellationToken.None);
        Assert.Equal(ConditionalPutOutcome.ConditionalConflict, conflict.Outcome);
        Assert.Contains(conflict.StatusCode, new int?[] { 409, 412 });
        Assert.Equal(PutConflictEvidence(binding, competingOperation, conflict.StatusCode!.Value), conflict.ProviderReceiptDigest);
        Assert.Equal(original, await ReadObjectBytesAsync(minio, locator.ObjectKey));

        using var reconciler = new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler));
        var exact = await reconciler.InspectExactAsync(locator, CancellationToken.None);
        Assert.Equal(ExactObjectInspectionOutcome.Present, exact.Outcome);
        Assert.Equal(original.Length, exact.CiphertextLength);
        Assert.Equal(binding, exact.ObjectBindingDigest);
        Assert.Equal(ProvisionalObjectDigests.PutOperation(operation), exact.PutOperationDigest);

        var mismatched = await reconciler.InspectExactAsync(
            locator with { ObjectBindingDigest = SHA256.HashData("foreign-binding"u8) },
            CancellationToken.None);
        Assert.Equal(ExactObjectInspectionOutcome.Present, mismatched.Outcome);
        Assert.NotEqual(SHA256.HashData("foreign-binding"u8), mismatched.ObjectBindingDigest);
        Assert.Equal(original, await ReadObjectBytesAsync(minio, locator.ObjectKey));
    }
    [Fact]
    public async Task O11_mismatched_existing_object_becomes_quarantined()
    {
        var unknown = await CreateUnknownObjectAsync();
        var observedAt = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-1));
        const long observedLength = 2048;
        var observedDigest = SHA256.HashData("mismatched-existing-object"u8);
        var observationEvidence = ReconcileEvidence(
            unknown.Object.Object.ObjectBindingDigest,
            unknown.Object.PutOperationId,
            "RecoveredMismatch",
            observedAt,
            null,
            observedLength,
            observedDigest);
        OperationResult conflict;
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
            conflict = await ResolvePutOutcomeAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                unknown.UnknownRevision,
                "RecoveredMismatch",
                observedLength,
                observedDigest,
                observedAt,
                null,
                observationEvidence);
        Assert.Equal("ConditionalConflict", conflict.OutcomeCode);
        Assert.Equal("ObjectConflict", conflict.ObjectState);

        var quarantineEvidence = HashCanonical(
            "tip-88c1-object-conflict-quarantine-evidence-v1",
            Convert.ToHexString(unknown.Object.Object.ObjectBindingDigest).ToLowerInvariant(),
            "ObjectConflict",
            conflict.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "ObjectBindingMismatch",
            Convert.ToHexString(observationEvidence).ToLowerInvariant());
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_lifecycle"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => RecordQuarantinedAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                conflict.StateRevision,
                "ObjectBindingMismatch",
                new byte[32]));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", exception.MessageText);
        }
        await AssertObjectStateAsync(unknown.Object.Object.ObjectCustodyId, "ObjectConflict", conflict.StateRevision, 4);

        await using (var connection = await OpenAsAsync("tagekyc_raw_export_lifecycle"))
        {
            var quarantined = await RecordQuarantinedAsync(
                connection,
                unknown.Object.Object.ObjectCustodyId,
                conflict.StateRevision,
                "ObjectBindingMismatch",
                quarantineEvidence);
            Assert.Equal("Quarantined", quarantined.OutcomeCode);
            Assert.Equal("Quarantined", quarantined.ObjectState);
        }
        await using var verify = await OpenAsync();
        Assert.Equal(quarantineEvidence, await ScalarBytesAsync(verify,
            "SELECT \"QuarantineEvidenceDigest\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", unknown.Object.Object.ObjectCustodyId)));
    }

    [Fact]
    public async Task O12_verified_completion_requires_authenticated_verification_evidence()
    {
        var present = await CreatePresentObjectAsync();
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            PostgresException? nullException = null;
            try
            {
                await MarkVerifiedAsync(
                    connection,
                    present.Object.Object.ObjectCustodyId,
                    present.PresentRevision,
                    null);
            }
            catch (PostgresException observed)
            {
                nullException = observed;
            }

            if (nullException is null)
            {
                await using var invalidVerify = await OpenAsync();
                var invalidState = await ScalarStringAsync(invalidVerify,
                    "SELECT \"State\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                    ("id", present.Object.Object.ObjectCustodyId));
                var evidenceIsNull = await ScalarIsNullAsync(invalidVerify,
                    "SELECT \"VerificationEvidenceDigest\" IS NULL FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                    ("id", present.Object.Object.ObjectCustodyId));
                Assert.Fail(
                    $"EXPECTED_AUTHENTICATED_VERIFICATION_EVIDENCE_BUT_NULL_WAS_ACCEPTED; state={invalidState}; evidence_is_null={evidenceIsNull}");
            }

            Assert.Equal("P0001", nullException.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID", nullException.MessageText);

            var shortException = await Assert.ThrowsAsync<PostgresException>(() => MarkVerifiedAsync(
                connection,
                present.Object.Object.ObjectCustodyId,
                present.PresentRevision,
                new byte[31]));
            Assert.Equal("P0001", shortException.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID", shortException.MessageText);
        }
        await AssertObjectStateAsync(present.Object.Object.ObjectCustodyId, "ObjectPresentPendingVerification", present.PresentRevision, 3);

        var verificationEvidence = SHA256.HashData("fixture-only-authenticated-verification"u8);
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var verified = await MarkVerifiedAsync(
                connection,
                present.Object.Object.ObjectCustodyId,
                present.PresentRevision,
                verificationEvidence);
            Assert.Equal("Verified", verified.OutcomeCode);
            Assert.Equal("VerifiedCompleted", verified.ObjectState);
            Assert.Equal(present.PresentRevision + 1, verified.StateRevision);
        }
        await using var verify = await OpenAsync();
        Assert.Equal(verificationEvidence, await ScalarBytesAsync(verify,
            "SELECT \"VerificationEvidenceDigest\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", present.Object.Object.ObjectCustodyId)));
    }
    [Fact]
    public async Task O13_cleanup_delete_ack_absence_and_quarantine_evidence_are_exact()
    {
        var acknowledged = await CreateCleanupPendingObjectAsync();
        var acknowledgedHead = acknowledged.Object.Object.Object.Object;
        var deleteEvidence = HashCanonical(
            "tip-88c1-object-delete-ack-evidence-v1",
            Convert.ToHexString(acknowledgedHead.ObjectBindingDigest).ToLowerInvariant(),
            acknowledged.CleanupRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "DeleteAcknowledged",
            "204");
        await using (var wrongActor = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => RecordDeleteAcknowledgedAsync(
                wrongActor,
                acknowledgedHead.ObjectCustodyId,
                acknowledged.CleanupRevision,
                204,
                deleteEvidence));
            Assert.Equal("42501", exception.SqlState);
        }
        await using (var lifecycle = await OpenAsAsync("tagekyc_raw_export_lifecycle"))
        {
            var invalid = await Assert.ThrowsAsync<PostgresException>(() => RecordDeleteAcknowledgedAsync(
                lifecycle,
                acknowledgedHead.ObjectCustodyId,
                acknowledged.CleanupRevision,
                204,
                new byte[32]));
            Assert.Equal("P0001", invalid.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", invalid.MessageText);
            var deleted = await RecordDeleteAcknowledgedAsync(
                lifecycle,
                acknowledgedHead.ObjectCustodyId,
                acknowledged.CleanupRevision,
                204,
                deleteEvidence);
            Assert.Equal("Deleted", deleted.OutcomeCode);
        }
        await AssertDeletionAsync(acknowledgedHead.ObjectCustodyId, "DeleteAcknowledged", deleteEvidence);

        var absent = await CreateCleanupPendingObjectAsync();
        var absentHead = absent.Object.Object.Object.Object;
        var first = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-2));
        var second = RoundToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-1));
        var absenceEvidence = HashCanonical(
            "tip-88c1-object-delete-absence-evidence-v1",
            Convert.ToHexString(absentHead.ObjectBindingDigest).ToLowerInvariant(),
            absent.CleanupRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            first.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            second.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "PositiveAbsenceConfirmed");
        await using (var wrongActor = await OpenAsAsync("tagekyc_raw_export_lifecycle"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => RecordAbsenceConfirmedAsync(
                wrongActor,
                absentHead.ObjectCustodyId,
                absent.CleanupRevision,
                first,
                second,
                absenceEvidence));
            Assert.Equal("42501", exception.SqlState);
        }
        await using (var reconciler = await OpenAsAsync("tagekyc_raw_export_reconciler"))
        {
            var invalid = await Assert.ThrowsAsync<PostgresException>(() => RecordAbsenceConfirmedAsync(
                reconciler,
                absentHead.ObjectCustodyId,
                absent.CleanupRevision,
                first,
                second,
                new byte[32]));
            Assert.Equal("P0001", invalid.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", invalid.MessageText);
            var deleted = await RecordAbsenceConfirmedAsync(
                reconciler,
                absentHead.ObjectCustodyId,
                absent.CleanupRevision,
                first,
                second,
                absenceEvidence);
            Assert.Equal("Deleted", deleted.OutcomeCode);
        }
        await AssertDeletionAsync(absentHead.ObjectCustodyId, "PositiveAbsenceConfirmed", absenceEvidence);

        var cleanupQuarantine = await CreateCleanupPendingObjectAsync();
        var cleanupQuarantineHead = cleanupQuarantine.Object.Object.Object.Object;
        var cleanupQuarantineEvidence = HashCanonical(
            "tip-88c1-object-cleanup-quarantine-evidence-v1",
            Convert.ToHexString(cleanupQuarantineHead.ObjectBindingDigest).ToLowerInvariant(),
            "CleanupPending",
            cleanupQuarantine.CleanupRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "DeleteOutcomeIndeterminateTerminal",
            Convert.ToHexString(cleanupQuarantine.CleanupEvidenceDigest).ToLowerInvariant());
        var sourceSwappedCleanupEvidence = HashCanonical(
            "tip-88c1-object-cleanup-quarantine-evidence-v1",
            Convert.ToHexString(cleanupQuarantineHead.ObjectBindingDigest).ToLowerInvariant(),
            "CleanupPending",
            cleanupQuarantine.CleanupRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "DeleteOutcomeIndeterminateTerminal",
            Convert.ToHexString(cleanupQuarantine.Object.Object.ProviderReceiptDigest).ToLowerInvariant());
        await using (var lifecycle = await OpenAsAsync("tagekyc_raw_export_lifecycle"))
        {
            var invalid = await Assert.ThrowsAsync<PostgresException>(() => RecordQuarantinedAsync(
                lifecycle,
                cleanupQuarantineHead.ObjectCustodyId,
                cleanupQuarantine.CleanupRevision,
                "DeleteOutcomeIndeterminateTerminal",
                sourceSwappedCleanupEvidence));
            Assert.Equal("P0001", invalid.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", invalid.MessageText);
            var quarantined = await RecordQuarantinedAsync(
                lifecycle,
                cleanupQuarantineHead.ObjectCustodyId,
                cleanupQuarantine.CleanupRevision,
                "DeleteOutcomeIndeterminateTerminal",
                cleanupQuarantineEvidence);
            Assert.Equal("Quarantined", quarantined.OutcomeCode);
        }
    }

    [Fact]
    public async Task O14_head_guard_and_event_append_only_rules_bite_as_owner()
    {
        var created = await CreateObjectAsync();
        await using var connection = await OpenAsync();
        foreach (var sql in new[]
                 {
                     "UPDATE tagekyc.raw_export_provisional_objects SET \"UpdatedAtUtc\"=pg_catalog.statement_timestamp() WHERE \"ObjectCustodyId\"=@id",
                     "DELETE FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
                     "TRUNCATE tagekyc.raw_export_provisional_objects CASCADE",
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", created.ObjectCustodyId);
            var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_DIRECT_MUTATION_FORBIDDEN", exception.MessageText);
        }

        foreach (var sql in new[]
                 {
                     "UPDATE tagekyc.raw_export_provisional_object_events SET \"EventAtUtc\"=pg_catalog.statement_timestamp() WHERE \"ObjectCustodyId\"=@id",
                     "DELETE FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id",
                     "TRUNCATE tagekyc.raw_export_provisional_object_events",
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", created.ObjectCustodyId);
            var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVENT_APPEND_FORBIDDEN", exception.MessageText);
        }

        await AssertObjectStateAsync(created.ObjectCustodyId, "Initiated", created.StateRevision, 1);
    }

    [Fact]
    public async Task O15_role_acl_and_membership_manifests_are_exact()
    {
        await using var connection = await OpenAsync();
        Assert.Equal(0L, await ScalarLongAsync(connection, "SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) a WHERE n.nspname='tagekyc' AND c.relname=ANY(ARRAY['raw_export_provisional_objects','raw_export_provisional_object_events']) AND a.grantee<>c.relowner"));
        Assert.Equal(0L, await ScalarLongAsync(connection, "SELECT count(*) FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='tagekyc' AND c.relname=ANY(ARRAY['raw_export_provisional_objects','raw_export_provisional_object_events']) AND a.attnum>0 AND NOT a.attisdropped AND a.attacl IS NOT NULL"));
        Assert.Equal(3L, await ScalarLongAsync(connection, "SELECT count(*) FROM pg_catalog.pg_auth_members m JOIN pg_catalog.pg_roles member ON member.oid=m.member JOIN pg_catalog.pg_roles role ON role.oid=m.roleid WHERE (member.rolname=ANY(ARRAY['tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle','tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login']) OR role.rolname=ANY(ARRAY['tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle','tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login'])) AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option"));
        Assert.Equal(6L, await ScalarLongAsync(connection, "SELECT count(*) FROM pg_catalog.pg_roles r WHERE r.rolname=ANY(ARRAY['tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle','tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login']) AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls AND r.rolinherit AND r.rolcanlogin=(r.rolname LIKE '%_login')"));
        var expectedFunctionAcl = new[]
        {
            "tagekyc_raw_export_deployer>tagekyc_raw_export_custody_encryptor:raw_export_arm_provisional_object_put:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_custody_encryptor:raw_export_begin_provisional_object_custody:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_custody_encryptor:raw_export_record_provisional_object_not_armed:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_custody_encryptor:raw_export_record_provisional_object_put_result:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_lifecycle:raw_export_mark_provisional_object_cleanup_required:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_lifecycle:raw_export_read_provisional_object_lifecycle_context:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_lifecycle:raw_export_record_provisional_object_delete_acknowledged:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_lifecycle:raw_export_record_provisional_object_quarantined:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_reconciler:raw_export_mark_provisional_object_cleanup_required:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_reconciler:raw_export_mark_provisional_object_verified:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_reconciler:raw_export_read_provisional_object_reconcile_context:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_reconciler:raw_export_record_provisional_object_absence_confirmed:EXECUTE",
            "tagekyc_raw_export_deployer>tagekyc_raw_export_reconciler:raw_export_resolve_provisional_object_put_outcome:EXECUTE",
        };
        var observedFunctionAcl = await ReadStringsAsync(connection, """
            SELECT grantor.rolname||'>'||grantee.rolname||':'||p.proname||':'||acl.privilege_type
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) acl
            JOIN pg_catalog.pg_roles grantor ON grantor.oid=acl.grantor
            JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
            WHERE n.nspname='tagekyc'
              AND p.proname=ANY(@names)
              AND acl.grantee<>p.proowner
            ORDER BY 1
            """, Functions);
        Assert.Equal(expectedFunctionAcl.Order(), observedFunctionAcl.Order());

        await AssertObjectDatabaseReadinessCodeAsync("PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE");
        await AssertDatabaseManifestMutationRejectedAsync(
            "ALTER TABLE tagekyc.raw_export_provisional_objects OWNER TO tagekyc_runtime",
            "ALTER TABLE tagekyc.raw_export_provisional_objects OWNER TO tagekyc_raw_export_deployer");
        await AssertDatabaseManifestMutationRejectedAsync(
            "ALTER FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid) OWNER TO tagekyc_runtime",
            "ALTER FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid) OWNER TO tagekyc_raw_export_deployer");
        await AssertDatabaseManifestMutationRejectedAsync(
            "REVOKE tagekyc_raw_export_reconciler FROM tagekyc_raw_export_reconciler_login",
            "GRANT tagekyc_raw_export_reconciler TO tagekyc_raw_export_reconciler_login WITH ADMIN FALSE, INHERIT TRUE, SET FALSE");
        var capabilityOutgoingRole = $"tagekyc_dobj_cap_out_{Guid.NewGuid():N}";
        await AssertDatabaseManifestMutationRejectedAsync(
            $"CREATE ROLE {capabilityOutgoingRole} NOLOGIN; GRANT {capabilityOutgoingRole} TO tagekyc_raw_export_custody_encryptor WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
            $"REVOKE {capabilityOutgoingRole} FROM tagekyc_raw_export_custody_encryptor; DROP ROLE {capabilityOutgoingRole}");
        var loginOutgoingRole = $"tagekyc_dobj_login_out_{Guid.NewGuid():N}";
        await AssertDatabaseManifestMutationRejectedAsync(
            $"CREATE ROLE {loginOutgoingRole} NOLOGIN; GRANT {loginOutgoingRole} TO tagekyc_raw_export_encryptor_login WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
            $"REVOKE {loginOutgoingRole} FROM tagekyc_raw_export_encryptor_login; DROP ROLE {loginOutgoingRole}");
        await AssertDatabaseManifestMutationRejectedAsync(
            "ALTER ROLE tagekyc_raw_export_custody_encryptor NOINHERIT",
            "ALTER ROLE tagekyc_raw_export_custody_encryptor INHERIT");
        await AssertDatabaseManifestMutationRejectedAsync(
            "GRANT SELECT (\"State\") ON tagekyc.raw_export_provisional_objects TO tagekyc_runtime",
            "REVOKE SELECT (\"State\") ON tagekyc.raw_export_provisional_objects FROM tagekyc_runtime");
        await AssertDatabaseManifestMutationRejectedAsync(
            "GRANT EXECUTE ON FUNCTION tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid) TO tagekyc_runtime",
            "REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid) FROM tagekyc_runtime");
        await AssertDatabaseManifestMutationRejectedAsync(
            "GRANT EXECUTE ON FUNCTION tagekyc.enforce_raw_export_provisional_object_write() TO tagekyc_raw_export_reconciler",
            "REVOKE EXECUTE ON FUNCTION tagekyc.enforce_raw_export_provisional_object_write() FROM tagekyc_raw_export_reconciler");
        await AssertDatabaseManifestMutationRejectedAsync(
            "ALTER FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid) RENAME TO raw_export_read_provisional_object_lifecycle_context_decoy",
            "ALTER FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context_decoy(uuid) RENAME TO raw_export_read_provisional_object_lifecycle_context");
    }

    [Fact]
    public async Task O16_minio_restart_preserves_conditional_object_and_recovery()
    {
        Assert.Equal(
            "minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e",
            DurableObjectMinioFixture.Image);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var identity = Guid.NewGuid();
        var binding = SHA256.HashData("o16-binding"u8);
        var operation = Guid.NewGuid();
        var locator = new ExactObjectLocator(identity, $"raw-export/c1/v1/{identity:N}", binding);
        var ciphertext = "durable-across-provider-dbcontext-process-replacement"u8.ToArray();

        using (var writer = new S3CompatibleProvisionalObjectWriter(minio.Options(ProvisionalObjectCapability.Writer)))
        await using (var body = new MemoryStream(ciphertext, writable: false))
            Assert.Equal(ConditionalPutOutcome.Created, (await writer.PutIfAbsentAsync(
                new ExactWriteRequest(locator, operation, ciphertext.Length), body, CancellationToken.None)).Outcome);

        await using (var firstDbContext = postgres.CreateDbContext())
            Assert.NotNull(firstDbContext.Model);

        await minio.RestartAsync();

        await using (var restartedDbContext = postgres.CreateDbContext())
            Assert.NotNull(restartedDbContext.Model);
        using var reconciler = new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler));
        var inspection = await reconciler.InspectExactAsync(locator, CancellationToken.None);
        Assert.Equal(ExactObjectInspectionOutcome.Present, inspection.Outcome);
        Assert.Equal(binding, inspection.ObjectBindingDigest);
        Assert.Equal(ProvisionalObjectDigests.PutOperation(operation), inspection.PutOperationDigest);
        await using var read = await reconciler.OpenExactReadAsync(locator, CancellationToken.None);
        await using var copy = new MemoryStream();
        await read.Ciphertext.CopyToAsync(copy);
        Assert.Equal(ciphertext, copy.ToArray());
        Assert.Equal(ciphertext.Length, read.CiphertextLength);

        var missingIdentity = Guid.NewGuid();
        var missing = locator with { ProvisionalObjectIdentity = missingIdentity, ObjectKey = $"raw-export/c1/v1/{missingIdentity:N}" };
        var absent = await Assert.ThrowsAsync<ProvisionalObjectReadException>(() =>
            reconciler.OpenExactReadAsync(missing, CancellationToken.None));
        Assert.Equal(ExactObjectReadFailure.PositivelyAbsent, absent.Failure);

        await using var bounded = new BoundedProviderReadStream(
            new MemoryStream(new byte[4], writable: false),
            new MemoryStream(),
            3,
            3);
        var buffer = new byte[4];
        var boundedFailure = await Assert.ThrowsAsync<ProvisionalObjectReadException>(async () =>
            await bounded.ReadAsync(buffer));
        Assert.Equal(ExactObjectReadFailure.Indeterminate, boundedFailure.Failure);
    }
    [Fact]
    public async Task O17_bucket_versioning_object_lock_lifecycle_and_public_access_fail_closed()
    {
        var canonicalAcl = await CaptureObjectTableAclAsync();
        await AssertPostureReadinessCodeAsync(
            new(false, true, true, true, true),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");

        await AssertInjectedPostureCodeAsync(client => client.ObjectLock = () =>
            Task.FromResult(new GetObjectLockConfigurationResponse()),
            "PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED");
        await AssertInjectedPostureCodeAsync(client => client.Versioning = () =>
            Task.FromResult(new GetBucketVersioningResponse
            {
                VersioningConfig = new S3BucketVersioningConfig
                {
                    Status = VersionStatus.FindValue("Unsupported"),
                },
            }),
            "PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED");
        var initializeCollections = Amazon.AWSConfigs.InitializeCollections;
        try
        {
            Amazon.AWSConfigs.InitializeCollections = false;
            await AssertInjectedPostureCodeAsync(client => client.Lifecycle = () =>
                Task.FromResult(new GetLifecycleConfigurationResponse { Configuration = null }),
                "PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED");
            await AssertInjectedPostureCodeAsync(client => client.Acl = () =>
                Task.FromResult(new GetACLResponse { AccessControlList = null }),
                "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");
        }
        finally
        {
            Amazon.AWSConfigs.InitializeCollections = initializeCollections;
        }
        await AssertInjectedPostureCodeAsync(client => client.Policy = () =>
            Task.FromResult(new GetBucketPolicyResponse { Policy = "   " }),
            "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");
        await AssertInjectedPostureCodeAsync(client => client.Policy = () =>
            Task.FromResult(new GetBucketPolicyResponse { Policy = "{not-json" }),
            "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");
        await AssertInjectedPostureCodeAsync(_ => { },
            "PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE");
        await AssertInjectedPostureCodeAsync(client => client.ObjectLock = () =>
            Task.FromException<GetObjectLockConfigurationResponse>(S3Failure(HttpStatusCode.NotFound)),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");
        await AssertInjectedPostureCodeAsync(client => client.Lifecycle = () =>
            Task.FromException<GetLifecycleConfigurationResponse>(S3Failure(HttpStatusCode.NotFound)),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");
        await AssertInjectedPostureCodeAsync(client => client.Policy = () =>
            Task.FromException<GetBucketPolicyResponse>(S3Failure(HttpStatusCode.NotFound)),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");
        foreach (var status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError })
        {
            await AssertInjectedPostureCodeAsync(client => client.Versioning = () =>
                Task.FromException<GetBucketVersioningResponse>(S3Failure(status)),
                "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");
            await AssertInjectedPostureCodeAsync(client => client.ObjectLock = () =>
                Task.FromException<GetObjectLockConfigurationResponse>(S3Failure(status)),
                "PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED");
            await AssertInjectedPostureCodeAsync(client => client.Lifecycle = () =>
                Task.FromException<GetLifecycleConfigurationResponse>(S3Failure(status)),
                "PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED");
            await AssertInjectedPostureCodeAsync(client => client.Acl = () =>
                Task.FromException<GetACLResponse>(S3Failure(status)),
                "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");
            await AssertInjectedPostureCodeAsync(client => client.Policy = () =>
                Task.FromException<GetBucketPolicyResponse>(S3Failure(status)),
                "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");
        }

        await AssertNonOwnerAclRejectedAsync("PUBLIC", createDisposableRole: false, canonicalAcl);
        await AssertNonOwnerAclRejectedAsync("tagekyc_runtime", createDisposableRole: false, canonicalAcl);
        await AssertNonOwnerAclRejectedAsync(
            "tagekyc_raw_export_custody_encryptor",
            createDisposableRole: false,
            canonicalAcl);
        await AssertNonOwnerAclRejectedAsync(
            $"tagekyc_dobj_acl_probe_{Guid.NewGuid():N}",
            createDisposableRole: true,
            canonicalAcl);

        await AssertPostureReadinessCodeAsync(
            new(false, true, true, true, true),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");

        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await AssertCapabilityPolicyMatrixAsync(minio);
        var enabledOptions = minio.Options(ProvisionalObjectCapability.PostureProbe);
        var enabledConfiguration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["TagEkyc:RawExport:ObjectCustody:Topology"] = enabledOptions.Topology.ToString(),
                ["TagEkyc:RawExport:ObjectCustody:Capability"] = enabledOptions.Capability.ToString(),
                ["TagEkyc:RawExport:ObjectCustody:ServiceUrl"] = enabledOptions.ServiceUrl?.ToString(),
                ["TagEkyc:RawExport:ObjectCustody:BucketName"] = enabledOptions.BucketName,
                ["TagEkyc:RawExport:ObjectCustody:AccessKeyId"] = enabledOptions.AccessKeyId,
                ["TagEkyc:RawExport:ObjectCustody:SecretAccessKey"] = enabledOptions.SecretAccessKey,
                ["TagEkyc:RawExport:ObjectCustody:AllowLoopbackHttp"] = enabledOptions.AllowLoopbackHttp.ToString(),
                ["TagEkyc:RawExport:ObjectCustody:MaximumSinglePartCiphertextBytes"] = enabledOptions.MaximumSinglePartCiphertextBytes.ToString(),
                ["TagEkyc:RawExport:ObjectCustody:OperationTimeoutSeconds"] = enabledOptions.OperationTimeout.TotalSeconds.ToString(),
            }).Build();
        var enabledServices = new ServiceCollection()
            .AddScoped(_ => postgres.CreateDbContext())
            .AddTagEkycProvisionalObjectCustody(enabledConfiguration);
        using (var enabledProvider = enabledServices.BuildServiceProvider(
                   new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }))
        using (var enabledScope = enabledProvider.CreateScope())
        {
            var repository = enabledScope.ServiceProvider.GetRequiredService<ProvisionalObjectCustodyRepository>();
            var readiness = enabledScope.ServiceProvider.GetRequiredService<ProvisionalObjectCustodyReadinessValidator>();
            Assert.Same(repository,
                enabledScope.ServiceProvider.GetRequiredService<ProvisionalObjectCustodyRepository>());
            Assert.Same(readiness,
                enabledScope.ServiceProvider.GetRequiredService<ProvisionalObjectCustodyReadinessValidator>());
        }
        await AssertProviderPostureReadinessCodeAsync(
            minio.Options(ProvisionalObjectCapability.PostureProbe, $"missing-{Guid.NewGuid():N}"),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");

        using var admin = minio.CreateAdminClient();
        var versionedBucket = await minio.CreateBucketAsync();
        await admin.PutBucketVersioningAsync(new PutBucketVersioningRequest
        {
            BucketName = versionedBucket,
            VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled },
        });
        await AssertProviderPostureReadinessCodeAsync(
            minio.Options(ProvisionalObjectCapability.PostureProbe, versionedBucket),
            "PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED");

        var lockedBucket = await minio.CreateBucketAsync(objectLockEnabled: true);
        await AssertProviderPostureReadinessCodeAsync(
            minio.Options(ProvisionalObjectCapability.PostureProbe, lockedBucket),
            "PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED");

        var lifecycleBucket = await minio.CreateBucketAsync();
        await admin.PutLifecycleConfigurationAsync(new PutLifecycleConfigurationRequest
        {
            BucketName = lifecycleBucket,
            Configuration = new LifecycleConfiguration
            {
                Rules =
                [
                    new LifecycleRule
                    {
                        Id = "fixture-rule",
                        Status = LifecycleRuleStatus.Enabled,
                        Filter = new LifecycleFilter
                        {
                            LifecycleFilterPredicate = new LifecyclePrefixPredicate { Prefix = string.Empty },
                        },
                        Expiration = new LifecycleRuleExpiration { Days = 1 },
                    },
                ],
            },
        });
        await AssertProviderPostureReadinessCodeAsync(
            minio.Options(ProvisionalObjectCapability.PostureProbe, lifecycleBucket),
            "PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED");

        var publicBucket = await minio.CreateBucketAsync();
        await admin.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = publicBucket,
            Policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{publicBucket}}/*"]}]}
                """,
        });
        await AssertProviderPostureReadinessCodeAsync(
            minio.Options(ProvisionalObjectCapability.PostureProbe, publicBucket),
            "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");

        await AssertProviderPostureReadinessCodeAsync(
            minio.Options(ProvisionalObjectCapability.PostureProbe),
            "PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE");
    }

    [Fact]
    public async Task O18_apply_down_reapply_restores_catalog_and_acl()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        var before = await CaptureObjectCatalogAndAclAsync(db);
        Assert.Empty(before);

        await migrator.MigrateAsync(Migration);
        var applied = await CaptureObjectCatalogAndAclAsync(db);
        Assert.NotEmpty(applied);

        await migrator.MigrateAsync(PreviousMigration);
        Assert.Equal(before, await CaptureObjectCatalogAndAclAsync(db));

        await migrator.MigrateAsync(Migration);
        Assert.Equal(applied, await CaptureObjectCatalogAndAclAsync(db));
    }

    [Fact]
    public async Task O19_no_plaintext_or_ciphertext_bytes_persist_in_postgres()
    {
        await using var connection = await OpenAsync();
        Assert.Equal(0L, await ScalarLongAsync(connection, "SELECT count(*) FROM information_schema.columns WHERE table_schema='tagekyc' AND table_name=ANY(ARRAY['raw_export_provisional_objects','raw_export_provisional_object_events']) AND lower(column_name) ~ '(payload|plaintext|content|blob|bytes)'"));
    }

    [Fact]
    public async Task O20_readiness_precedence_and_exact_codes_are_total()
    {
        Assert.Equal(
        [
            "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID",
            "PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID",
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID",
            "PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID",
            "PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID",
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE",
            "PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED",
            "PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED",
            "PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED",
            "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED",
            "PROD_RAW_EXPORT_OBJECT_CAPABILITY_INVALID",
            "PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE",
            "PROD_RAW_EXPORT_OBJECT_OPERATION_UNAVAILABLE",
            "PROD_RAW_EXPORT_OBJECT_R2_DEPENDENCY_UNAVAILABLE",
        ], ProvisionalObjectCustodyReadinessValidator.Codes);

        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("Topology", null)),
            "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("Topology", "UnknownTopology")),
            "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("Topology", "not-an-enum"), ("MaximumSinglePartCiphertextBytes", null)),
            "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("MaximumSinglePartCiphertextBytes", null)),
            "PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("OperationTimeoutSeconds", "not-an-integer")),
            "PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("ServiceUrl", "ftp://object-store.invalid")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("BucketName", "INVALID_BUCKET")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("BucketName", "InvalidBucket")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("BucketName", "example..com")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("BucketName", "192.168.5.4")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("BucketName", " tagekyc-raw-export ")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("ServiceUrl", "https://object-store.example/base-path")),
            "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("Capability", null)),
            "PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID");
        await AssertConfiguredReadinessCodeAsync(
            ConfigurationValues(("AccessKeyId", "   ")),
            "PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID");

        var disabledServices = new ServiceCollection()
            .AddTagEkycProvisionalObjectCustody(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{ProvisionalObjectCustodyOptions.SectionPath}:Topology"] = "Disabled",
                }).Build());
        Assert.Single(disabledServices, descriptor =>
            descriptor.ServiceType == typeof(ProvisionalObjectCustodyOptions));
        Assert.DoesNotContain(disabledServices, descriptor =>
            descriptor.ServiceType == typeof(ProvisionalObjectCustodyReadinessValidator));
        Assert.DoesNotContain(disabledServices, descriptor =>
            descriptor.ServiceType == typeof(ProvisionalObjectCustodyRepository));
        Assert.DoesNotContain(disabledServices, descriptor => descriptor.ServiceType == typeof(IProvisionalObjectWriter)
            || descriptor.ServiceType == typeof(IProvisionalObjectReconciler)
            || descriptor.ServiceType == typeof(IProvisionalObjectLifecycle)
            || descriptor.ServiceType == typeof(IProvisionalObjectPostureProbe));

        using var noCapabilities = new ServiceCollection().BuildServiceProvider();
        var fixedOptions = new ProvisionalObjectCustodyOptions(
            ProvisionalObjectTopology.S3CompatibleDurable,
            ProvisionalObjectCapability.PostureProbe,
            new Uri("https://object-store.invalid"),
            "tagekyc-raw-export",
            "fixture-access",
            "fixture-secret",
            false,
            ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes,
            ProvisionalObjectCustodyOptions.FixedOperationTimeout,
            true);
        await AssertReadinessCodeAsync(fixedOptions with
        {
            Topology = (ProvisionalObjectTopology)999,
            IsSyntacticallyValid = false,
            MaximumSinglePartCiphertextBytes = 1,
        }, noCapabilities, "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID");
        await AssertReadinessCodeAsync(fixedOptions with
        {
            MaximumSinglePartCiphertextBytes = 1,
            ServiceUrl = new Uri("http://object-store.invalid"),
            AccessKeyId = null,
        }, noCapabilities, "PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID");
        await AssertReadinessCodeAsync(fixedOptions with
        {
            ServiceUrl = new Uri("http://object-store.invalid"),
            AccessKeyId = null,
        }, noCapabilities, "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID");
        await AssertReadinessCodeAsync(fixedOptions with { AccessKeyId = null }, noCapabilities,
            "PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID");

        await using (var connection = await OpenAsync())
        {
            try
            {
                await using (var grant = new NpgsqlCommand(
                    "GRANT SELECT ON tagekyc.raw_export_provisional_objects TO PUBLIC", connection))
                    await grant.ExecuteNonQueryAsync();
                await AssertReadinessCodeAsync(fixedOptions, noCapabilities,
                    "PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID");
            }
            finally
            {
                await using var revoke = new NpgsqlCommand(
                    "REVOKE ALL ON tagekyc.raw_export_provisional_objects FROM PUBLIC", connection);
                await revoke.ExecuteNonQueryAsync();
            }
        }

        await AssertReadinessCodeAsync(fixedOptions, noCapabilities,
            "PROD_RAW_EXPORT_OBJECT_CAPABILITY_INVALID");
        await AssertPostureReadinessCodeAsync(new(false, false, false, false, false),
            "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE");
        await AssertPostureReadinessCodeAsync(new(true, false, false, false, false),
            "PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED");
        await AssertPostureReadinessCodeAsync(new(true, false, true, false, false),
            "PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED");
        await AssertPostureReadinessCodeAsync(new(true, true, true, false, false),
            "PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED");
        await AssertPostureReadinessCodeAsync(new(true, true, true, true, false),
            "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED");
        await AssertPostureReadinessCodeAsync(new(true, true, true, true, true),
            "PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE");
    }
    [Fact]
    public async Task O21_per_source_object_capacity_is_config_bound_and_atomic()
    {
        var lockProbe = await CreateActiveAttemptAsync();
        await using (var blocker = await OpenAsync())
        await using (var transaction = await blocker.BeginTransactionAsync())
        await using (var contender = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            await using (var holdSourceHead = new NpgsqlCommand(
                "SELECT 1 FROM tagekyc.raw_export_source_head WHERE \"SourceArtifactId\"=@source FOR UPDATE",
                blocker,
                transaction))
            {
                holdSourceHead.Parameters.AddWithValue("source", lockProbe.SourceArtifactId);
                await holdSourceHead.ExecuteScalarAsync();
            }

            var beginWhileLocked = BeginObjectAsync(contender, lockProbe, 64);
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            var blockedOnSourceHead = !beginWhileLocked.IsCompleted;
            await transaction.RollbackAsync();
            var lockProbeResult = await beginWhileLocked;
            Assert.True(blockedOnSourceHead,
                "EXPECTED_SOURCE_HEAD_LOCK_TO_SERIALIZE_CAPACITY_CHECK_BUT_BEGIN_COMPLETED_WHILE_LOCK_WAS_HELD");
            Assert.Equal("Created", lockProbeResult.OutcomeCode);
        }

        var capped = await CreateActiveAttemptAsync();
        await SeedNonterminalSiblingObjectAsync(capped);
        await using (var writer = await OpenAsAsync("tagekyc_raw_export_custody_encryptor"))
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => BeginObjectAsync(writer, capped, 1));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED", exception.MessageText);
        }
        await using (var verify = await OpenAsync())
            Assert.Equal(1L, await ScalarLongAsync(verify,
                "SELECT count(*) FROM tagekyc.raw_export_provisional_objects WHERE \"SourceArtifactId\"=@source",
                ("source", capped.SourceArtifactId)));

        var concurrent = await CreateActiveAttemptAsync();
        await using var firstConnection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        await using var secondConnection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        var first = BeginObjectAsync(firstConnection, concurrent, 64);
        var second = BeginObjectAsync(secondConnection, concurrent, 64);
        var outcomes = await Task.WhenAll(first, second);
        Assert.Equal(new[] { "Created", "ExistingMatch" }, outcomes.Select(x => x.OutcomeCode).Order().ToArray());
        Assert.Single(outcomes.Select(x => x.ObjectCustodyId).Distinct());
        await using var concurrentVerify = await OpenAsync();
        Assert.Equal(1L, await ScalarLongAsync(concurrentVerify,
            "SELECT count(*) FROM tagekyc.raw_export_provisional_objects WHERE \"SourceArtifactId\"=@source",
            ("source", concurrent.SourceArtifactId)));
    }
    [Fact]
    public void O22_writer_deadline_is_frozen_to_the_earliest_landed_expiry()
    {
        var projection = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
        var far = projection.AddHours(1);

        Assert.Equal(projection.AddSeconds(300), ProvisionalObjectCustodyRepository.ComputeWriterDeadline(
            BeginForDeadline(projection, far, far, far)));
        Assert.Equal(projection.AddSeconds(100), ProvisionalObjectCustodyRepository.ComputeWriterDeadline(
            BeginForDeadline(projection, projection.AddSeconds(100), far, far)));
        Assert.Equal(projection.AddSeconds(120), ProvisionalObjectCustodyRepository.ComputeWriterDeadline(
            BeginForDeadline(projection, far, projection.AddSeconds(120), far)));
        Assert.Equal(projection.AddSeconds(140), ProvisionalObjectCustodyRepository.ComputeWriterDeadline(
            BeginForDeadline(projection, far, far, projection.AddSeconds(140))));
    }

    private static ProvisionalObjectBeginResult BeginForDeadline(
        DateTimeOffset projectionAtUtc,
        DateTimeOffset ownershipLeaseExpiresAtUtc,
        DateTimeOffset effectivePlaintextRetentionExpiresAtUtc,
        DateTimeOffset reservationExpiresAtUtc) =>
        new(
            new ProvisionalObjectMutationResult("Created", Guid.Empty, "Initiated", 1),
            "raw-export/c1/v1/00000000000000000000000000000000",
            Guid.Empty,
            new byte[32],
            "ChipDg2Portrait",
            1,
            ownershipLeaseExpiresAtUtc,
            effectivePlaintextRetentionExpiresAtUtc,
            reservationExpiresAtUtc,
            projectionAtUtc);

    private async Task<CreatedObject> CreateObjectAsync()
    {
        var active = await CreateActiveAttemptAsync();
        await using var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        var result = await BeginObjectAsync(connection, active, 64);
        Assert.Equal("Created", result.OutcomeCode);
        return result;
    }

    private async Task<ActiveAttempt> CreateActiveAttemptAsync()
    {
        await new Tip88C1B2DurableKeyProdTests(postgres)
            .DKPROD_60_CapabilityRoles_PrepareWrapRecordActivateRestartReadUnwrap_NoTableGrant();
        await using var connection = await OpenAsync();
        Guid attemptId;
        Guid sourceArtifactId;
        long revision;
        long fence;
        await using (var lookup = new NpgsqlCommand("""
            SELECT "AttemptId","SourceArtifactId","EncryptionAttemptRevision","Fence"
            FROM tagekyc.raw_export_source_encryption_attempts
            ORDER BY "CreatedAtUtc" DESC LIMIT 1
            """, connection))
        await using (var lookupReader = await lookup.ExecuteReaderAsync())
        {
            Assert.True(await lookupReader.ReadAsync());
            attemptId = lookupReader.GetGuid(0);
            sourceArtifactId = lookupReader.GetGuid(1);
            revision = lookupReader.GetInt64(2);
            fence = lookupReader.GetInt64(3);
        }
        return new ActiveAttempt(attemptId, sourceArtifactId, revision, fence);
    }

    private static async Task<CreatedObject> BeginObjectAsync(
        NpgsqlConnection connection,
        ActiveAttempt active,
        int maximumPerSource)
    {
        await using var command = new NpgsqlCommand("""
            SELECT *
            FROM tagekyc.raw_export_begin_provisional_object_custody(
                @attempt,@revision,@fence,@maximum)
            """, connection);
        command.Parameters.AddWithValue("attempt", active.AttemptId);
        command.Parameters.AddWithValue("revision", active.Revision);
        command.Parameters.AddWithValue("fence", active.Fence);
        command.Parameters.AddWithValue("maximum", maximumPerSource);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new CreatedObject(reader.GetString(0), reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.GetFieldValue<byte[]>(6));
    }

    private async Task<SiblingObject> SeedNonterminalSiblingObjectAsync(ActiveAttempt active)
    {
        var siblingAttemptId = Guid.NewGuid();
        var siblingReservationId = Guid.NewGuid();
        var siblingIdentity = Guid.NewGuid();
        var siblingObjectId = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await SetActorAndRoleAsync(connection, "tagekyc_raw_export_deployer");
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','complete-r1',false);
            INSERT INTO tagekyc.raw_export_source_encryption_attempts(
                "AttemptId","SourceArtifactId","EncryptionAttemptRevision","Fence","ProvisionalObjectIdentity",
                "AttemptKeyReservationId","KeyProviderId","KekId","KekVersion","KekFingerprint",
                "EncryptionSuiteId","EncryptionFramingVersion","NonceStrategyId",
                "NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize",
                "FramingParametersDigest","EncryptionAttemptFingerprint","OwnershipLeaseExpiresAtUtc",
                "R2TerminationDisposition","CreatedAtUtc","SchemaVersion")
            SELECT @siblingAttempt,"SourceArtifactId","EncryptionAttemptRevision"+100,"Fence"+100,@siblingIdentity,
                   @siblingReservation,"KeyProviderId","KekId","KekVersion","KekFingerprint",
                   "EncryptionSuiteId","EncryptionFramingVersion","NonceStrategyId",
                   "NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize",
                   "FramingParametersDigest","EncryptionAttemptFingerprint",pg_catalog.statement_timestamp()+interval '1 hour',
                   NULL,pg_catalog.statement_timestamp(),1
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=@activeAttempt;
            SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',false);
            INSERT INTO tagekyc.raw_export_provisional_objects(
                "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
                "EncryptionAttemptRevision","AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest",
                "State","StateRevision","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
            SELECT @siblingObject,"AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
                   "EncryptionAttemptRevision","Fence","EncryptionAttemptFingerprint",
                   'raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower("ProvisionalObjectIdentity"::text),'-',''),
                   tagekyc.compute_raw_export_provisional_object_binding(
                       "AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
                       "EncryptionAttemptRevision","Fence","EncryptionAttemptFingerprint",
                       'raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower("ProvisionalObjectIdentity"::text),'-','')),
                   'Initiated',1,pg_catalog.statement_timestamp(),pg_catalog.statement_timestamp(),1
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=@siblingAttempt;
            SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','',false);
            SELECT pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','',false);
            """, connection);
        command.Parameters.AddWithValue("activeAttempt", active.AttemptId);
        command.Parameters.AddWithValue("siblingAttempt", siblingAttemptId);
        command.Parameters.AddWithValue("siblingReservation", siblingReservationId);
        command.Parameters.AddWithValue("siblingIdentity", siblingIdentity);
        command.Parameters.AddWithValue("siblingObject", siblingObjectId);
        await command.ExecuteNonQueryAsync();
        return new SiblingObject(siblingAttemptId, siblingReservationId, siblingIdentity, siblingObjectId);
    }

    private async Task<ArmedObject> CreateArmedObjectAsync()
    {
        var created = await CreateObjectAsync();
        var operationId = Guid.NewGuid();
        await using var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        var result = await ArmAsync(connection, created.ObjectCustodyId, created.StateRevision, operationId);
        Assert.Equal("Armed", result.OutcomeCode);
        return new ArmedObject(created, operationId, result.StateRevision);
    }

    private async Task<UnknownObject> CreateUnknownObjectAsync()
    {
        var armed = await CreateArmedObjectAsync();
        await using var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        var result = await RecordPutResultAsync(
            connection,
            armed.Object.ObjectCustodyId,
            armed.ArmedRevision,
            armed.PutOperationId,
            "OutcomeUnknown",
            null,
            null,
            null,
            null);
        Assert.Equal("OutcomeUnknown", result.OutcomeCode);
        return new UnknownObject(armed, result.StateRevision);
    }

    private async Task<PresentObject> CreatePresentObjectAsync()
    {
        var armed = await CreateArmedObjectAsync();
        const long length = 1024;
        var digest = SHA256.HashData("present-ciphertext"u8);
        var evidence = PutCreatedEvidence(armed.Object.ObjectBindingDigest, armed.PutOperationId, length, digest);
        await using var connection = await OpenAsAsync("tagekyc_raw_export_custody_encryptor");
        var result = await RecordPutResultAsync(
            connection,
            armed.Object.ObjectCustodyId,
            armed.ArmedRevision,
            armed.PutOperationId,
            "Created",
            200,
            length,
            digest,
            evidence);
        Assert.Equal("Recorded", result.OutcomeCode);
        return new PresentObject(armed, result.StateRevision, evidence);
    }

    private async Task<VerifiedObject> CreateVerifiedObjectAsync()
    {
        var present = await CreatePresentObjectAsync();
        var evidence = SHA256.HashData("fixture-only-authenticated-verification"u8);
        await using var connection = await OpenAsAsync("tagekyc_raw_export_reconciler");
        var result = await MarkVerifiedAsync(
            connection,
            present.Object.Object.ObjectCustodyId,
            present.PresentRevision,
            evidence);
        Assert.Equal("Verified", result.OutcomeCode);
        return new VerifiedObject(present, result.StateRevision, evidence);
    }

    private async Task<CleanupPendingObject> CreateCleanupPendingObjectAsync()
    {
        var verified = await CreateVerifiedObjectAsync();
        var evidence = HashCanonical(
            "tip-88c1-object-cleanup-evidence-v1",
            Convert.ToHexString(verified.Object.Object.Object.ObjectBindingDigest).ToLowerInvariant(),
            "VerifiedCompleted",
            verified.VerifiedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "SourceExpired");
        await using (var connection = await OpenAsAsync("tagekyc_raw_export_lifecycle"))
        {
            var invalid = await Assert.ThrowsAsync<PostgresException>(() => MarkCleanupRequiredAsync(
                connection,
                verified.Object.Object.Object.ObjectCustodyId,
                verified.VerifiedRevision,
                "SourceExpired",
                new byte[32]));
            Assert.Equal("P0001", invalid.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID", invalid.MessageText);
            var result = await MarkCleanupRequiredAsync(
                connection,
                verified.Object.Object.Object.ObjectCustodyId,
                verified.VerifiedRevision,
                "SourceExpired",
                evidence);
            Assert.Equal("CleanupRequired", result.OutcomeCode);
            return new CleanupPendingObject(verified, result.StateRevision, evidence);
        }
    }

    private async Task<NpgsqlConnection> OpenAsAsync(string role)
    {
        var connection = await OpenAsync();
        await SetActorAndRoleAsync(connection, role);
        return connection;
    }

    private static async Task SetActorAndRoleAsync(NpgsqlConnection connection, string role)
    {
        await using var command = new NpgsqlCommand($"""
            SELECT pg_catalog.set_config('tagekyc.actor_principal_id','11111111-1111-1111-1111-111111111111',false);
            RESET ROLE;
            SET ROLE {role};
            """, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<OperationResult> ArmAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        Guid putOperationId)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_arm_provisional_object_put(@id,@revision,@operation)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("operation", putOperationId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> RecordPutResultAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        Guid putOperationId,
        string resultKind,
        int? providerStatusCode,
        long? ciphertextLength,
        byte[]? ciphertextDigest,
        byte[]? providerReceiptDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_record_provisional_object_put_result(
                @id,@revision,@operation,@kind,@status,@length,@digest,@receipt)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("operation", putOperationId);
        command.Parameters.AddWithValue("kind", resultKind);
        command.Parameters.Add("status", NpgsqlDbType.Integer).Value = providerStatusCode.HasValue ? providerStatusCode.Value : DBNull.Value;
        command.Parameters.Add("length", NpgsqlDbType.Bigint).Value = ciphertextLength.HasValue ? ciphertextLength.Value : DBNull.Value;
        command.Parameters.Add("digest", NpgsqlDbType.Bytea).Value = ciphertextDigest is not null ? ciphertextDigest : DBNull.Value;
        command.Parameters.Add("receipt", NpgsqlDbType.Bytea).Value = providerReceiptDigest is not null ? providerReceiptDigest : DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> ResolvePutOutcomeAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        string resolutionKind,
        long? ciphertextLength,
        byte[]? ciphertextDigest,
        DateTimeOffset? firstObservedAtUtc,
        DateTimeOffset? secondObservedAtUtc,
        byte[] observationEvidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_resolve_provisional_object_put_outcome(
                @id,@revision,@kind,@length,@digest,@first,@second,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("kind", resolutionKind);
        command.Parameters.Add("length", NpgsqlDbType.Bigint).Value = ciphertextLength.HasValue ? ciphertextLength.Value : DBNull.Value;
        command.Parameters.Add("digest", NpgsqlDbType.Bytea).Value = ciphertextDigest is not null ? ciphertextDigest : DBNull.Value;
        command.Parameters.Add("first", NpgsqlDbType.TimestampTz).Value = firstObservedAtUtc.HasValue ? firstObservedAtUtc.Value : DBNull.Value;
        command.Parameters.Add("second", NpgsqlDbType.TimestampTz).Value = secondObservedAtUtc.HasValue ? secondObservedAtUtc.Value : DBNull.Value;
        command.Parameters.AddWithValue("evidence", observationEvidenceDigest);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> RecordNotArmedAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        byte[] evidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_record_provisional_object_not_armed(
                @id,@revision,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("evidence", evidenceDigest);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> MarkVerifiedAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        byte[]? evidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_mark_provisional_object_verified(
                @id,@revision,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.Add("evidence", NpgsqlDbType.Bytea).Value = evidenceDigest is not null ? evidenceDigest : DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> RecordQuarantinedAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        string reasonCode,
        byte[] evidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_record_provisional_object_quarantined(
                @id,@revision,@reason,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("reason", reasonCode);
        command.Parameters.AddWithValue("evidence", evidenceDigest);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> MarkCleanupRequiredAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        string reasonCode,
        byte[] evidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_mark_provisional_object_cleanup_required(
                @id,@revision,@reason,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("reason", reasonCode);
        command.Parameters.AddWithValue("evidence", evidenceDigest);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> RecordDeleteAcknowledgedAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        int statusCode,
        byte[] evidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_record_provisional_object_delete_acknowledged(
                @id,@revision,@status,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("status", statusCode);
        command.Parameters.AddWithValue("evidence", evidenceDigest);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private static async Task<OperationResult> RecordAbsenceConfirmedAsync(
        NpgsqlConnection connection,
        Guid objectCustodyId,
        long expectedRevision,
        DateTimeOffset firstObservedAtUtc,
        DateTimeOffset secondObservedAtUtc,
        byte[] evidenceDigest)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_record_provisional_object_absence_confirmed(
                @id,@revision,@first,@second,@evidence)
            """, connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("first", firstObservedAtUtc);
        command.Parameters.AddWithValue("second", secondObservedAtUtc);
        command.Parameters.AddWithValue("evidence", evidenceDigest);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new OperationResult(reader.GetString(0), reader.GetString(2), reader.GetInt64(3));
    }

    private async Task AssertDeletionAsync(Guid objectCustodyId, string evidenceKind, byte[] evidenceDigest)
    {
        await using var connection = await OpenAsync();
        Assert.Equal("Deleted", await ScalarStringAsync(connection,
            "SELECT \"State\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", objectCustodyId)));
        Assert.Equal(evidenceKind, await ScalarStringAsync(connection,
            "SELECT \"DeletionEvidenceKind\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", objectCustodyId)));
        Assert.Equal(evidenceDigest, await ScalarBytesAsync(connection,
            "SELECT \"DeletionEvidenceDigest\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", objectCustodyId)));
    }

    private async Task AssertCheckViolationAsync(
        Guid objectCustodyId,
        string sql,
        string expectedConstraint,
        string writeContext,
        string? preSql = null)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var context = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',@context,true)",
            connection,
            transaction))
        {
            context.Parameters.AddWithValue("context", writeContext);
            await context.ExecuteNonQueryAsync();
        }
        if (preSql is not null)
        {
            await using var pre = new NpgsqlCommand(preSql, connection, transaction);
            await pre.ExecuteNonQueryAsync();
        }
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", objectCustodyId);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", exception.SqlState);
        Assert.Equal(expectedConstraint, exception.ConstraintName);
        await transaction.RollbackAsync();
    }

    private async Task AssertObjectStateAsync(Guid objectCustodyId, string state, long revision, long eventCount)
    {
        await using var connection = await OpenAsync();
        Assert.Equal(state, await ScalarStringAsync(connection,
            "SELECT \"State\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", objectCustodyId)));
        Assert.Equal(revision, await ScalarLongAsync(connection,
            "SELECT \"StateRevision\" FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id",
            ("id", objectCustodyId)));
        Assert.Equal(eventCount, await ScalarLongAsync(connection,
            "SELECT count(*) FROM tagekyc.raw_export_provisional_object_events WHERE \"ObjectCustodyId\"=@id",
            ("id", objectCustodyId)));
    }

    private static byte[] PutCreatedEvidence(byte[] bindingDigest, Guid operationId, long length, byte[] ciphertextDigest) =>
        HashCanonical(
            "tip-88c1-object-put-created-evidence-v1",
            Convert.ToHexString(bindingDigest).ToLowerInvariant(),
            operationId.ToString("N"),
            "Created",
            "200",
            length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToHexString(ciphertextDigest).ToLowerInvariant());

    private static byte[] PutConflictEvidence(byte[] bindingDigest, Guid operationId, int statusCode) =>
        HashCanonical(
            "tip-88c1-object-put-conflict-evidence-v1",
            Convert.ToHexString(bindingDigest).ToLowerInvariant(),
            operationId.ToString("N"),
            "ConditionalConflictObserved",
            statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static async Task<byte[]> ReadObjectBytesAsync(DurableObjectMinioFixture minio, string key)
    {
        using var reconciler = minio.CreateCapabilityClient(ProvisionalObjectCapability.Reconciler);
        using var response = await reconciler.GetObjectAsync(minio.BucketName, key);
        await using var copy = new MemoryStream();
        await response.ResponseStream.CopyToAsync(copy);
        return copy.ToArray();
    }

    private static byte[] ReconcileEvidence(
        byte[] bindingDigest,
        Guid operationId,
        string resolutionKind,
        DateTimeOffset firstObservedAtUtc,
        DateTimeOffset? secondObservedAtUtc,
        long? ciphertextLength,
        byte[]? ciphertextDigest) =>
        HashCanonical(
            "tip-88c1-object-reconcile-observation-v1",
            Convert.ToHexString(bindingDigest).ToLowerInvariant(),
            operationId.ToString("N"),
            resolutionKind,
            firstObservedAtUtc.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            secondObservedAtUtc?.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none",
            ciphertextLength?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none",
            ciphertextDigest is null ? "none" : Convert.ToHexString(ciphertextDigest).ToLowerInvariant());

    private static DateTimeOffset RoundToMicroseconds(DateTimeOffset value) =>
        new(value.UtcTicks - (value.UtcTicks % 10), TimeSpan.Zero);

    private async Task AssertNonOwnerAclRejectedAsync(
        string grantee,
        bool createDisposableRole,
        IReadOnlyList<string> canonicalAcl)
    {
        var quotedGrantee = grantee == "PUBLIC"
            ? grantee
            : new NpgsqlCommandBuilder().QuoteIdentifier(grantee);
        await using var connection = await OpenAsync();
        try
        {
            if (createDisposableRole)
            {
                await using var create = new NpgsqlCommand(
                    $"CREATE ROLE {quotedGrantee} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT",
                    connection);
                await create.ExecuteNonQueryAsync();
            }

            await using (var grant = new NpgsqlCommand(
                $"GRANT SELECT ON tagekyc.raw_export_provisional_objects TO {quotedGrantee}",
                connection))
                await grant.ExecuteNonQueryAsync();

            await AssertPostureReadinessCodeAsync(
                new(false, true, true, true, true),
                "PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID");
        }
        finally
        {
            await using (var revoke = new NpgsqlCommand(
                $"REVOKE ALL ON tagekyc.raw_export_provisional_objects FROM {quotedGrantee}",
                connection))
                await revoke.ExecuteNonQueryAsync();
            if (createDisposableRole)
            {
                await using var drop = new NpgsqlCommand($"DROP ROLE {quotedGrantee}", connection);
                await drop.ExecuteNonQueryAsync();
            }
        }

        Assert.Equal(canonicalAcl, await CaptureObjectTableAclAsync());
    }

    private async Task<IReadOnlyList<string>> CaptureObjectTableAclAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT c.relname || '|' || c.relowner::text || '|' || acl.grantee::text || '|' ||
                   acl.privilege_type || '|' || acl.is_grantable::text
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(c.relacl) acl
            WHERE n.nspname='tagekyc'
              AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events')
            ORDER BY 1
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync()) rows.Add(reader.GetString(0));
        return rows;
    }

    private async Task AssertPostureReadinessCodeAsync(ObjectBucketPosture posture, string expectedCode)
    {
        await using var db = postgres.CreateDbContext();
        using var services = new ServiceCollection()
            .AddSingleton<IProvisionalObjectPostureProbe>(new FixedPostureProbe(posture))
            .BuildServiceProvider();
        var options = new ProvisionalObjectCustodyOptions(
            ProvisionalObjectTopology.S3CompatibleDurable,
            ProvisionalObjectCapability.PostureProbe,
            new Uri("https://object-store.invalid"),
            "tagekyc-raw-export",
            "fixture-access",
            "fixture-secret",
            false,
            ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes,
            ProvisionalObjectCustodyOptions.FixedOperationTimeout,
            true);
        var exception = await Assert.ThrowsAsync<ProvisionalObjectCustodyReadinessException>(() =>
            new ProvisionalObjectCustodyReadinessValidator(db, options, services)
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private async Task AssertConfiguredReadinessCodeAsync(
        IReadOnlyDictionary<string, string?> values,
        string expectedCode)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection()
            .AddScoped(_ => postgres.CreateDbContext())
            .AddTagEkycProvisionalObjectCustody(configuration);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(ProvisionalObjectCustodyRepository));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IProvisionalObjectWriter)
            || descriptor.ServiceType == typeof(IProvisionalObjectReconciler)
            || descriptor.ServiceType == typeof(IProvisionalObjectLifecycle)
            || descriptor.ServiceType == typeof(IProvisionalObjectPostureProbe));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<ProvisionalObjectCustodyReadinessValidator>();
        var exception = await Assert.ThrowsAsync<ProvisionalObjectCustodyReadinessException>(() =>
            validator.ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private async Task AssertDatabaseManifestMutationRejectedAsync(string mutationSql, string restoreSql)
    {
        await using var connection = await OpenAsync();
        try
        {
            await using (var mutation = new NpgsqlCommand(mutationSql, connection))
                await mutation.ExecuteNonQueryAsync();
            await AssertObjectDatabaseReadinessCodeAsync(
                "PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID");
        }
        finally
        {
            await using var restore = new NpgsqlCommand(restoreSql, connection);
            await restore.ExecuteNonQueryAsync();
        }
        await AssertObjectDatabaseReadinessCodeAsync("PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE");
    }

    private async Task AssertObjectDatabaseReadinessCodeAsync(string expectedCode)
    {
        await using var db = postgres.CreateDbContext();
        using var services = new ServiceCollection()
            .AddSingleton<IProvisionalObjectPostureProbe>(
                new FixedPostureProbe(new(true, true, true, true, true)))
            .BuildServiceProvider();
        var options = new ProvisionalObjectCustodyOptions(
            ProvisionalObjectTopology.S3CompatibleDurable,
            ProvisionalObjectCapability.PostureProbe,
            new Uri("https://object-store.invalid"),
            "tagekyc-raw-export",
            "fixture-access",
            "fixture-secret",
            false,
            ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes,
            ProvisionalObjectCustodyOptions.FixedOperationTimeout,
            true);
        var exception = await Assert.ThrowsAsync<ProvisionalObjectCustodyReadinessException>(() =>
            new ProvisionalObjectCustodyReadinessValidator(db, options, services)
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private static Dictionary<string, string?> ConfigurationValues(
        params (string Key, string? Value)[] replacements)
    {
        var prefix = ProvisionalObjectCustodyOptions.SectionPath;
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"{prefix}:Topology"] = "S3CompatibleDurable",
            [$"{prefix}:Capability"] = "PostureProbe",
            [$"{prefix}:ServiceUrl"] = "https://object-store.invalid",
            [$"{prefix}:BucketName"] = "tagekyc-raw-export",
            [$"{prefix}:AccessKeyId"] = "fixture-access",
            [$"{prefix}:SecretAccessKey"] = "fixture-secret",
            [$"{prefix}:AllowLoopbackHttp"] = "false",
            [$"{prefix}:MaximumSinglePartCiphertextBytes"] =
                ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            [$"{prefix}:OperationTimeoutSeconds"] =
                ((int)ProvisionalObjectCustodyOptions.FixedOperationTimeout.TotalSeconds).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (var replacement in replacements)
        {
            var key = $"{prefix}:{replacement.Key}";
            if (replacement.Value is null)
                values.Remove(key);
            else
                values[key] = replacement.Value;
        }
        return values;
    }

    private async Task AssertProviderPostureReadinessCodeAsync(
        ProvisionalObjectCustodyOptions options,
        string expectedCode)
    {
        await using var db = postgres.CreateDbContext();
        using var probe = new S3CompatibleProvisionalObjectPostureProbe(options);
        var services = new ServiceCollection()
            .AddSingleton<IProvisionalObjectPostureProbe>(probe)
            .BuildServiceProvider();
        var exception = await Assert.ThrowsAsync<ProvisionalObjectCustodyReadinessException>(() =>
            new ProvisionalObjectCustodyReadinessValidator(db, options, services)
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private async Task AssertInjectedPostureCodeAsync(
        Action<StubPostureS3Client> arrange,
        string expectedCode)
    {
        var options = new ProvisionalObjectCustodyOptions(
            ProvisionalObjectTopology.S3CompatibleDurable,
            ProvisionalObjectCapability.PostureProbe,
            new Uri("https://object-store.invalid"),
            "tagekyc-raw-export",
            "fixture-access",
            "fixture-secret",
            false,
            ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes,
            ProvisionalObjectCustodyOptions.FixedOperationTimeout,
            true);
        using var client = new StubPostureS3Client();
        arrange(client);
        using var probe = new S3CompatibleProvisionalObjectPostureProbe(options, client);
        var posture = await probe.InspectBucketPostureAsync(CancellationToken.None);
        await AssertPostureReadinessCodeAsync(posture, expectedCode);
    }

    private static async Task AssertCapabilityPolicyMatrixAsync(DurableObjectMinioFixture minio)
    {
        Assert.Equal(5, minio.DistinctCredentialCount());
        var prefix = "raw-export/c1/v1/";
        var readKey = $"{prefix}{Guid.NewGuid():N}";
        var deleteKey = $"{prefix}{Guid.NewGuid():N}";

        using var writer = minio.CreateCapabilityClient(ProvisionalObjectCapability.Writer);
        foreach (var key in new[] { readKey, deleteKey })
        {
            await writer.PutObjectAsync(new PutObjectRequest
            {
                BucketName = minio.BucketName,
                Key = key,
                InputStream = new MemoryStream([1, 2, 3], writable: false),
                IfNoneMatch = "*",
            });
        }

        using var reconciler = minio.CreateCapabilityClient(ProvisionalObjectCapability.Reconciler);
        _ = await reconciler.GetObjectMetadataAsync(minio.BucketName, readKey);
        using (var read = await reconciler.GetObjectAsync(minio.BucketName, readKey))
        {
            Assert.Equal(HttpStatusCode.OK, read.HttpStatusCode);
        }

        using var lifecycle = minio.CreateCapabilityClient(ProvisionalObjectCapability.Lifecycle);
        var deleted = await lifecycle.DeleteObjectAsync(minio.BucketName, deleteKey);
        Assert.Equal(HttpStatusCode.NoContent, deleted.HttpStatusCode);

        using var posture = minio.CreateCapabilityClient(ProvisionalObjectCapability.PostureProbe);
        _ = await posture.GetBucketVersioningAsync(minio.BucketName);
        await Assert.ThrowsAsync<AmazonS3Exception>(() =>
            posture.GetObjectLockConfigurationAsync(new GetObjectLockConfigurationRequest
            {
                BucketName = minio.BucketName,
            }));
        var lifecyclePosture = await posture.GetLifecycleConfigurationAsync(
            new GetLifecycleConfigurationRequest { BucketName = minio.BucketName });
        Assert.Empty(lifecyclePosture.Configuration.Rules);
        _ = await posture.GetACLAsync(new GetACLRequest { BucketName = minio.BucketName });
        var privatePolicy = await posture.GetBucketPolicyAsync(
            new GetBucketPolicyRequest { BucketName = minio.BucketName });
        Assert.False(string.IsNullOrWhiteSpace(privatePolicy.Policy));
        using var cleanup = minio.CreateAdminClient();
        var listed = await cleanup.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = minio.BucketName,
        });
        Assert.Contains(listed.S3Objects, item => item.Key == readKey);

        await AssertProviderDeniedAsync(() => writer.GetObjectMetadataAsync(minio.BucketName, readKey));
        await AssertProviderDeniedAsync(() => writer.DeleteObjectAsync(minio.BucketName, readKey));
        await AssertProviderDeniedAsync(() => writer.ListObjectsV2Async(new ListObjectsV2Request { BucketName = minio.BucketName }));
        await AssertProviderDeniedAsync(() => writer.GetBucketVersioningAsync(minio.BucketName));
        await AssertProviderDeniedAsync(() => writer.GetACLAsync(new GetACLRequest { BucketName = minio.BucketName }));

        await AssertProviderDeniedAsync(() => reconciler.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = $"{prefix}{Guid.NewGuid():N}",
            InputStream = new MemoryStream([4], writable: false),
            IfNoneMatch = "*",
        }));
        await AssertProviderDeniedAsync(() => reconciler.DeleteObjectAsync(minio.BucketName, readKey));
        await AssertProviderDeniedAsync(() => reconciler.ListObjectsV2Async(new ListObjectsV2Request { BucketName = minio.BucketName }));
        await AssertProviderDeniedAsync(() => reconciler.GetBucketVersioningAsync(minio.BucketName));

        await AssertProviderDeniedAsync(() => lifecycle.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = $"{prefix}{Guid.NewGuid():N}",
            InputStream = new MemoryStream([5], writable: false),
            IfNoneMatch = "*",
        }));
        await AssertProviderDeniedAsync(() => lifecycle.GetObjectMetadataAsync(minio.BucketName, readKey));
        await AssertProviderDeniedAsync(() => lifecycle.ListObjectsV2Async(new ListObjectsV2Request { BucketName = minio.BucketName }));
        await AssertProviderDeniedAsync(() => lifecycle.GetBucketVersioningAsync(minio.BucketName));

        await AssertProviderDeniedAsync(() => posture.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = $"{prefix}{Guid.NewGuid():N}",
            InputStream = new MemoryStream([6], writable: false),
            IfNoneMatch = "*",
        }));
        await AssertProviderDeniedAsync(() => posture.GetObjectMetadataAsync(minio.BucketName, readKey));
        await AssertProviderDeniedAsync(() => posture.DeleteObjectAsync(minio.BucketName, readKey));
        await AssertProviderDeniedAsync(() => posture.ListObjectsV2Async(new ListObjectsV2Request { BucketName = minio.BucketName }));

        var outsidePrefixKey = $"outside-prefix/{Guid.NewGuid():N}";
        await AssertProviderDeniedAsync(() => writer.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = outsidePrefixKey,
            InputStream = new MemoryStream([7], writable: false),
            IfNoneMatch = "*",
        }));
        await AssertProviderDeniedAsync(() => reconciler.GetObjectMetadataAsync(minio.BucketName, outsidePrefixKey));
        await AssertProviderDeniedAsync(() => lifecycle.DeleteObjectAsync(minio.BucketName, outsidePrefixKey));

        var alternateBucket = await minio.CreateBucketAsync();
        using var alternatePosture = minio.CreateCapabilityClient(
            ProvisionalObjectCapability.PostureProbe, alternateBucket);
        _ = await alternatePosture.GetBucketVersioningAsync(alternateBucket);
        await AssertProviderDeniedAsync(() => writer.PutObjectAsync(new PutObjectRequest
        {
            BucketName = alternateBucket,
            Key = $"{prefix}{Guid.NewGuid():N}",
            InputStream = new MemoryStream([8], writable: false),
            IfNoneMatch = "*",
        }));
        await AssertProviderDeniedAsync(() => reconciler.GetObjectMetadataAsync(alternateBucket, readKey));
        await AssertProviderDeniedAsync(() => lifecycle.DeleteObjectAsync(alternateBucket, deleteKey));
        await AssertProviderDeniedAsync(() => posture.GetBucketVersioningAsync(alternateBucket));

        AssertExactPolicy(
            minio.CapabilityPolicyDocument(ProvisionalObjectCapability.Writer),
            ["s3:PutObject"],
            [$"arn:aws:s3:::{minio.BucketName}/{prefix}*"]);
        AssertExactPolicy(
            minio.CapabilityPolicyDocument(ProvisionalObjectCapability.Reconciler),
            ["s3:GetObject"],
            [$"arn:aws:s3:::{minio.BucketName}/{prefix}*"]);
        AssertExactPolicy(
            minio.CapabilityPolicyDocument(ProvisionalObjectCapability.Lifecycle),
            ["s3:DeleteObject"],
            [$"arn:aws:s3:::{minio.BucketName}/{prefix}*"]);
        AssertExactPolicy(
            minio.CapabilityPolicyDocument(ProvisionalObjectCapability.PostureProbe),
            ["s3:GetBucketObjectLockConfiguration", "s3:GetBucketPolicy",
             "s3:GetBucketVersioning", "s3:GetLifecycleConfiguration"],
            [$"arn:aws:s3:::{minio.BucketName}"]);
    }

    private static void AssertExactPolicy(
        string policyDocument,
        string[] expectedActions,
        string[] expectedResources)
    {
        using var json = JsonDocument.Parse(policyDocument);
        var statements = json.RootElement.GetProperty("Statement").EnumerateArray().ToArray();
        var actions = statements
            .SelectMany(statement => statement.GetProperty("Action").EnumerateArray())
            .Select(value => value.GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var resources = statements
            .SelectMany(statement => statement.GetProperty("Resource").EnumerateArray())
            .Select(value => value.GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedActions.Order(StringComparer.Ordinal), actions);
        Assert.Equal(expectedResources.Order(StringComparer.Ordinal), resources);
        Assert.DoesNotContain("tagekyc-*", policyDocument, StringComparison.Ordinal);
    }

    private static async Task AssertProviderDeniedAsync(Func<Task> operation)
    {
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(operation);
        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    private static AmazonS3Exception S3Failure(
        HttpStatusCode statusCode,
        string? errorCode = null) =>
        new("POSTURE_FIXTURE_FAILURE") { StatusCode = statusCode, ErrorCode = errorCode };

    private async Task AssertReadinessCodeAsync(
        ProvisionalObjectCustodyOptions options,
        IServiceProvider services,
        string expectedCode)
    {
        await using var db = postgres.CreateDbContext();
        var exception = await Assert.ThrowsAsync<ProvisionalObjectCustodyReadinessException>(() =>
            new ProvisionalObjectCustodyReadinessValidator(db, options, services)
                .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
    }

    private static async Task<IReadOnlyList<string>> CaptureObjectCatalogAndAclAsync(TagEkycDbContext db)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT manifest
            FROM (
              SELECT 'relation|' || c.relname || '|' || c.relkind::text || '|' ||
                     c.relowner::regrole::text || '|' || COALESCE(c.relacl::text, '') AS manifest
              FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc' AND c.relname=ANY(@tables)
              UNION ALL
              SELECT 'column|' || c.relname || '|' || a.attnum::text || '|' || a.attname || '|' ||
                     pg_catalog.format_type(a.atttypid,a.atttypmod) || '|' || a.attnotnull::text || '|' ||
                     COALESCE(pg_catalog.pg_get_expr(d.adbin,d.adrelid),'') || '|' || COALESCE(a.attacl::text,'')
              FROM pg_catalog.pg_attribute a
              JOIN pg_catalog.pg_class c ON c.oid=a.attrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
              WHERE n.nspname='tagekyc' AND c.relname=ANY(@tables) AND a.attnum>0 AND NOT a.attisdropped
              UNION ALL
              SELECT 'constraint|' || con.conname || '|' || con.contype::text || '|' ||
                     con.condeferrable::text || '|' || con.condeferred::text || '|' ||
                     pg_catalog.pg_get_constraintdef(con.oid,true)
              FROM pg_catalog.pg_constraint con
              JOIN pg_catalog.pg_namespace n ON n.oid=con.connamespace
              WHERE n.nspname='tagekyc' AND con.conname=ANY(@constraints)
              UNION ALL
              SELECT 'trigger|' || t.tgname || '|' || pg_catalog.pg_get_triggerdef(t.oid,true)
              FROM pg_catalog.pg_trigger t
              JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc' AND NOT t.tgisinternal AND t.tgname=ANY(@triggers)
              UNION ALL
              SELECT 'function|' || p.proname || '|' || pg_catalog.pg_get_function_identity_arguments(p.oid) || '|' ||
                     pg_catalog.pg_get_function_result(p.oid) || '|' || p.proowner::regrole::text || '|' ||
                     p.prosecdef::text || '|' || COALESCE(p.proconfig::text,'') || '|' || COALESCE(p.proacl::text,'') || '|' ||
                     pg_catalog.pg_get_functiondef(p.oid)
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname=ANY(@functions)
            ) catalog
            ORDER BY manifest
            """, connection);
        command.Parameters.AddWithValue("tables", Tables);
        command.Parameters.AddWithValue("constraints", Constraints);
        command.Parameters.AddWithValue("triggers", Triggers);
        command.Parameters.AddWithValue("functions", Functions);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<string>();
        while (await reader.ReadAsync()) result.Add(reader.GetString(0));
        return result;
    }

    private sealed class FixedPostureProbe(ObjectBucketPosture posture) : IProvisionalObjectPostureProbe
    {
        public Task<ObjectBucketPosture> InspectBucketPostureAsync(CancellationToken cancellationToken) =>
            Task.FromResult(posture);
    }

    private sealed class StubPostureS3Client : AmazonS3Client
    {
        internal StubPostureS3Client()
            : base(new Amazon.Runtime.AnonymousAWSCredentials(), new AmazonS3Config
            {
                ServiceURL = "http://127.0.0.1:1",
                ForcePathStyle = true,
                MaxErrorRetry = 0,
            })
        {
        }

        internal Func<Task<GetBucketVersioningResponse>> Versioning { get; set; } = () =>
            Task.FromResult(new GetBucketVersioningResponse
            {
                VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Off },
            });
        internal Func<Task<GetObjectLockConfigurationResponse>> ObjectLock { get; set; } = () =>
            Task.FromException<GetObjectLockConfigurationResponse>(S3Failure(
                HttpStatusCode.NotFound, "ObjectLockConfigurationNotFoundError"));
        internal Func<Task<GetLifecycleConfigurationResponse>> Lifecycle { get; set; } = () =>
            Task.FromException<GetLifecycleConfigurationResponse>(S3Failure(
                HttpStatusCode.NotFound, "NoSuchLifecycleConfiguration"));
        internal Func<Task<GetACLResponse>> Acl { get; set; } = () => Task.FromResult(new GetACLResponse
        {
            AccessControlList = new S3AccessControlList { Grants = [] },
        });
        internal Func<Task<GetBucketPolicyResponse>> Policy { get; set; } = () =>
            Task.FromException<GetBucketPolicyResponse>(S3Failure(
                HttpStatusCode.NotFound, "NoSuchBucketPolicy"));

        public override Task<GetBucketVersioningResponse> GetBucketVersioningAsync(
            GetBucketVersioningRequest request, CancellationToken cancellationToken) => Versioning();
        public override Task<GetObjectLockConfigurationResponse> GetObjectLockConfigurationAsync(
            GetObjectLockConfigurationRequest request, CancellationToken cancellationToken) => ObjectLock();
        public override Task<GetLifecycleConfigurationResponse> GetLifecycleConfigurationAsync(
            GetLifecycleConfigurationRequest request, CancellationToken cancellationToken) => Lifecycle();
        public override Task<GetACLResponse> GetACLAsync(
            GetACLRequest request, CancellationToken cancellationToken) => Acl();
        public override Task<GetBucketPolicyResponse> GetBucketPolicyAsync(
            GetBucketPolicyRequest request, CancellationToken cancellationToken) => Policy();
    }

    private async Task<NpgsqlConnection> OpenAsync() { var connection=new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); return connection; }
    private async Task<string> FunctionDefinitionAsync(string name) { await using var c=await OpenAsync(); await using var q=new NpgsqlCommand("SELECT pg_catalog.pg_get_functiondef(p.oid) FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='tagekyc' AND p.proname=@name",c); q.Parameters.AddWithValue("name",name); return Assert.IsType<string>(await q.ExecuteScalarAsync()); }
    private async Task<string> ConstraintDefinitionsAsync() { await using var c=await OpenAsync(); await using var q=new NpgsqlCommand("SELECT string_agg(pg_catalog.pg_get_constraintdef(oid),' ') FROM pg_catalog.pg_constraint WHERE conname=ANY(@names)",c); q.Parameters.AddWithValue("names",Constraints); return Assert.IsType<string>(await q.ExecuteScalarAsync()); }
    private async Task<bool> ConstraintExistsAsync(string name,string type) { await using var c=await OpenAsync(); await using var q=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_constraint WHERE conname=@n AND contype::text=@t)",c); q.Parameters.AddWithValue("n",name);q.Parameters.AddWithValue("t",type);return Assert.IsType<bool>(await q.ExecuteScalarAsync()); }
    private static async Task<IReadOnlyList<string>> ReadStringsAsync(NpgsqlConnection c,string sql,string[] values){await using var q=new NpgsqlCommand(sql,c);q.Parameters.AddWithValue("names",values);await using var r=await q.ExecuteReaderAsync();var list=new List<string>();while(await r.ReadAsync())list.Add(r.GetString(0));return list;}
    private static async Task<long> ScalarLongAsync(NpgsqlConnection c,string sql,params (string Name,object Value)[] parameters){await using var q=new NpgsqlCommand(sql,c);foreach(var (name,value) in parameters)q.Parameters.AddWithValue(name,value);return Convert.ToInt64(await q.ExecuteScalarAsync());}
    private static async Task<string> ScalarStringAsync(NpgsqlConnection c,string sql,params (string Name,object Value)[] parameters){await using var q=new NpgsqlCommand(sql,c);foreach(var (name,value) in parameters)q.Parameters.AddWithValue(name,value);return Assert.IsType<string>(await q.ExecuteScalarAsync());}
    private static async Task<byte[]> ScalarBytesAsync(NpgsqlConnection c,string sql,params (string Name,object Value)[] parameters){await using var q=new NpgsqlCommand(sql,c);foreach(var (name,value) in parameters)q.Parameters.AddWithValue(name,value);return Assert.IsType<byte[]>(await q.ExecuteScalarAsync());}
    private static async Task<bool> ScalarIsNullAsync(NpgsqlConnection c,string sql,params (string Name,object Value)[] parameters){await using var q=new NpgsqlCommand(sql,c);foreach(var (name,value) in parameters)q.Parameters.AddWithValue(name,value);return Assert.IsType<bool>(await q.ExecuteScalarAsync());}
    private static async Task<bool> TableExistsAsync(TagEkycDbContext db,string table){await using var command=db.Database.GetDbConnection().CreateCommand();if(command.Connection!.State!=System.Data.ConnectionState.Open)await command.Connection.OpenAsync();command.CommandText="SELECT to_regclass(@name) IS NOT NULL";var parameter=command.CreateParameter();parameter.ParameterName="name";parameter.Value=$"tagekyc.{table}";command.Parameters.Add(parameter);return Assert.IsType<bool>(await command.ExecuteScalarAsync());}
    private static byte[] HashCanonical(params string[] fields){using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var length=new byte[4];foreach(var field in fields){var bytes=Encoding.UTF8.GetBytes(field.Normalize(NormalizationForm.FormC));BinaryPrimitives.WriteInt32BigEndian(length,bytes.Length);hash.AppendData(length);hash.AppendData(bytes);}return hash.GetHashAndReset();}
    private static string ProjectPath(string relative)=>Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../..",relative));

    private sealed record ActiveAttempt(Guid AttemptId, Guid SourceArtifactId, long Revision, long Fence);
    private sealed record SiblingObject(Guid AttemptId, Guid AttemptKeyReservationId, Guid ProvisionalObjectIdentity, Guid ObjectCustodyId);
    private sealed record CreatedObject(string OutcomeCode, Guid ObjectCustodyId, string State, long StateRevision, byte[] ObjectBindingDigest);
    private sealed record ArmedObject(CreatedObject Object, Guid PutOperationId, long ArmedRevision);
    private sealed record UnknownObject(ArmedObject Object, long UnknownRevision);
    private sealed record PresentObject(ArmedObject Object, long PresentRevision, byte[] ProviderReceiptDigest);
    private sealed record VerifiedObject(PresentObject Object, long VerifiedRevision, byte[] VerificationEvidenceDigest);
    private sealed record CleanupPendingObject(VerifiedObject Object, long CleanupRevision, byte[] CleanupEvidenceDigest);
    private sealed record OperationResult(string OutcomeCode, string ObjectState, long StateRevision);
}
