using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2DurableObjectCustody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_source_attempt_object_binding",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                columns: new[] { "AttemptId", "AttemptKeyReservationId", "SourceArtifactId", "ProvisionalObjectIdentity" });

            migrationBuilder.CreateTable(
                name: "raw_export_provisional_objects",
                schema: "tagekyc",
                columns: table => new
                {
                    ObjectCustodyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProvisionalObjectIdentity = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptionAttemptRevision = table.Column<long>(type: "bigint", nullable: false),
                    AttemptFence = table.Column<long>(type: "bigint", nullable: false),
                    EncryptionAttemptFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    ObjectBindingDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StateRevision = table.Column<long>(type: "bigint", nullable: false),
                    PutOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PutArmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PutOutcomeKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OutcomeObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CiphertextLength = table.Column<long>(type: "bigint", nullable: true),
                    CiphertextDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    ProviderReceiptDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    VerificationEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CleanupReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CleanupEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    CleanupRequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletionEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    DeletionEvidenceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QuarantineReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    QuarantineEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    QuarantinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_provisional_objects", x => x.ObjectCustodyId);
                    table.CheckConstraint("ck_raw_export_provisional_objects_sparse", "CASE \"State\"\n  WHEN 'Initiated' THEN \"PutOperationId\" IS NULL AND \"PutArmedAtUtc\" IS NULL AND \"PutOutcomeKind\" IS NULL AND \"OutcomeObservedAtUtc\" IS NULL AND \"CiphertextLength\" IS NULL AND \"CiphertextDigest\" IS NULL AND \"ProviderReceiptDigest\" IS NULL AND \"VerificationEvidenceDigest\" IS NULL AND \"VerifiedAtUtc\" IS NULL AND \"CleanupReasonCode\" IS NULL AND \"CleanupEvidenceDigest\" IS NULL AND \"CleanupRequestedAtUtc\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"DeletionEvidenceKind\" IS NULL AND \"DeletedAtUtc\" IS NULL AND \"QuarantineReasonCode\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL AND \"QuarantinedAtUtc\" IS NULL\n  WHEN 'PutInFlight' THEN \"PutOperationId\" IS NOT NULL AND \"PutArmedAtUtc\" IS NOT NULL AND \"PutOutcomeKind\" IS NULL AND \"OutcomeObservedAtUtc\" IS NULL AND \"CiphertextLength\" IS NULL AND \"CiphertextDigest\" IS NULL AND \"ProviderReceiptDigest\" IS NULL AND \"VerificationEvidenceDigest\" IS NULL AND \"VerifiedAtUtc\" IS NULL AND \"CleanupReasonCode\" IS NULL AND \"CleanupEvidenceDigest\" IS NULL AND \"CleanupRequestedAtUtc\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"DeletionEvidenceKind\" IS NULL AND \"DeletedAtUtc\" IS NULL AND \"QuarantineReasonCode\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL AND \"QuarantinedAtUtc\" IS NULL\n  WHEN 'PutOutcomeUnknown' THEN \"PutOperationId\" IS NOT NULL AND \"PutArmedAtUtc\" IS NOT NULL AND \"PutOutcomeKind\" IN ('OutcomeUnknown','ConditionalConflictObserved') AND \"OutcomeObservedAtUtc\" IS NOT NULL AND \"CiphertextLength\" IS NULL AND \"CiphertextDigest\" IS NULL AND (\"ProviderReceiptDigest\" IS NULL OR \"PutOutcomeKind\"='ConditionalConflictObserved') AND \"VerificationEvidenceDigest\" IS NULL AND \"VerifiedAtUtc\" IS NULL AND \"CleanupReasonCode\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'ObjectPresentPendingVerification' THEN \"PutOperationId\" IS NOT NULL AND \"PutArmedAtUtc\" IS NOT NULL AND \"PutOutcomeKind\" IN ('Created','RecoveredPresent') AND \"OutcomeObservedAtUtc\" IS NOT NULL AND \"CiphertextLength\" IS NOT NULL AND \"CiphertextDigest\" IS NOT NULL AND \"ProviderReceiptDigest\" IS NOT NULL AND \"VerificationEvidenceDigest\" IS NULL AND \"VerifiedAtUtc\" IS NULL AND \"CleanupReasonCode\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'VerifiedCompleted' THEN \"PutOperationId\" IS NOT NULL AND \"PutOutcomeKind\" IN ('Created','RecoveredPresent') AND \"CiphertextLength\" IS NOT NULL AND \"CiphertextDigest\" IS NOT NULL AND \"ProviderReceiptDigest\" IS NOT NULL AND \"VerificationEvidenceDigest\" IS NOT NULL AND \"VerifiedAtUtc\" IS NOT NULL AND \"CleanupReasonCode\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'NoObjectEstablished' THEN ((\"PutOperationId\" IS NULL AND \"PutArmedAtUtc\" IS NULL AND \"PutOutcomeKind\"='NotArmed') OR (\"PutOperationId\" IS NOT NULL AND \"PutArmedAtUtc\" IS NOT NULL AND \"PutOutcomeKind\"='PositiveAbsence')) AND \"OutcomeObservedAtUtc\" IS NOT NULL AND \"CiphertextLength\" IS NULL AND \"CiphertextDigest\" IS NULL AND \"ProviderReceiptDigest\" IS NULL AND \"VerificationEvidenceDigest\" IS NULL AND \"CleanupReasonCode\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'ObjectConflict' THEN \"PutOperationId\" IS NOT NULL AND \"PutOutcomeKind\"='RecoveredMismatch' AND \"OutcomeObservedAtUtc\" IS NOT NULL AND ((\"CiphertextLength\" IS NULL AND \"CiphertextDigest\" IS NULL) OR (\"CiphertextLength\" IS NOT NULL AND \"CiphertextDigest\" IS NOT NULL)) AND \"ProviderReceiptDigest\" IS NOT NULL AND \"VerificationEvidenceDigest\" IS NULL AND \"CleanupReasonCode\" IS NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'CleanupPending' THEN \"PutOperationId\" IS NOT NULL AND \"CiphertextLength\" IS NOT NULL AND \"CiphertextDigest\" IS NOT NULL AND \"ProviderReceiptDigest\" IS NOT NULL AND \"CleanupReasonCode\" IS NOT NULL AND \"CleanupEvidenceDigest\" IS NOT NULL AND \"CleanupRequestedAtUtc\" IS NOT NULL AND \"DeletionEvidenceDigest\" IS NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'Deleted' THEN \"PutOperationId\" IS NOT NULL AND \"CleanupReasonCode\" IS NOT NULL AND \"CleanupEvidenceDigest\" IS NOT NULL AND \"CleanupRequestedAtUtc\" IS NOT NULL AND \"DeletionEvidenceDigest\" IS NOT NULL AND \"DeletionEvidenceKind\" IS NOT NULL AND \"DeletedAtUtc\" IS NOT NULL AND \"QuarantineEvidenceDigest\" IS NULL\n  WHEN 'Quarantined' THEN \"PutOperationId\" IS NOT NULL AND \"ProviderReceiptDigest\" IS NOT NULL AND \"QuarantineReasonCode\" IS NOT NULL AND \"QuarantineEvidenceDigest\" IS NOT NULL AND \"QuarantinedAtUtc\" IS NOT NULL AND \"DeletionEvidenceDigest\" IS NULL\n  ELSE false\nEND");
                    table.CheckConstraint("ck_raw_export_provisional_objects_state", "\"State\" IN ('Initiated','PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','NoObjectEstablished','ObjectConflict','CleanupPending','Deleted','Quarantined')");
                    table.CheckConstraint("ck_raw_export_provisional_objects_values", "\"SchemaVersion\" = 1\nAND \"EncryptionAttemptRevision\" >= 1\nAND \"AttemptFence\" >= 1\nAND \"StateRevision\" >= 1\nAND octet_length(\"EncryptionAttemptFingerprint\") = 32\nAND octet_length(\"ObjectBindingDigest\") = 32\nAND \"ObjectKey\" ~ '^raw-export/c1/v1/[0-9a-f]{32}$'\nAND (\"CiphertextLength\" IS NULL OR \"CiphertextLength\" BETWEEN 1 AND 134217728)\nAND (\"CiphertextDigest\" IS NULL OR octet_length(\"CiphertextDigest\") = 32)\nAND (\"ProviderReceiptDigest\" IS NULL OR octet_length(\"ProviderReceiptDigest\") = 32)\nAND (\"VerificationEvidenceDigest\" IS NULL OR octet_length(\"VerificationEvidenceDigest\") = 32)\nAND (\"CleanupEvidenceDigest\" IS NULL OR octet_length(\"CleanupEvidenceDigest\") = 32)\nAND (\"DeletionEvidenceDigest\" IS NULL OR octet_length(\"DeletionEvidenceDigest\") = 32)\nAND (\"QuarantineEvidenceDigest\" IS NULL OR octet_length(\"QuarantineEvidenceDigest\") = 32)\nAND (\"CleanupReasonCode\" IS NULL OR (\"CleanupReasonCode\" ~ '^[A-Za-z]+$' AND octet_length(\"CleanupReasonCode\") <= 64))\nAND (\"QuarantineReasonCode\" IS NULL OR (\"QuarantineReasonCode\" ~ '^[A-Za-z]+$' AND octet_length(\"QuarantineReasonCode\") <= 64))\nAND (\"DeletionEvidenceKind\" IS NULL OR \"DeletionEvidenceKind\" IN ('DeleteAcknowledged','PositiveAbsenceConfirmed'))");
                    table.ForeignKey(
                        name: "fk_raw_export_provisional_objects_attempt_binding",
                        columns: x => new { x.AttemptId, x.AttemptKeyReservationId, x.SourceArtifactId, x.ProvisionalObjectIdentity },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_encryption_attempts",
                        principalColumns: new[] { "AttemptId", "AttemptKeyReservationId", "SourceArtifactId", "ProvisionalObjectIdentity" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_provisional_object_events",
                schema: "tagekyc",
                columns: table => new
                {
                    ObjectCustodyEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectCustodyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSequence = table.Column<long>(type: "bigint", nullable: false),
                    FromState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StateRevision = table.Column<long>(type: "bigint", nullable: false),
                    EvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    EventAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_provisional_object_events", x => x.ObjectCustodyEventId);
                    table.CheckConstraint("ck_raw_export_provisional_object_events_values", "\"SchemaVersion\" = 1\nAND \"EventSequence\" >= 1\nAND \"StateRevision\" >= 1\nAND \"EventSequence\" = \"StateRevision\"\nAND \"ToState\" IN ('Initiated','PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','NoObjectEstablished','ObjectConflict','CleanupPending','Deleted','Quarantined')\nAND (\"FromState\" IS NULL OR \"FromState\" IN ('Initiated','PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','ObjectConflict','CleanupPending'))\nAND \"ActorKind\" IN ('Writer','Reconciler','Lifecycle')\nAND octet_length(\"EvidenceDigest\") = 32");
                    table.ForeignKey(
                        name: "fk_raw_export_provisional_object_events_head",
                        column: x => x.ObjectCustodyId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_provisional_objects",
                        principalColumn: "ObjectCustodyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_provisional_object_events_revision",
                schema: "tagekyc",
                table: "raw_export_provisional_object_events",
                columns: new[] { "ObjectCustodyId", "StateRevision" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_provisional_object_events_sequence",
                schema: "tagekyc",
                table: "raw_export_provisional_object_events",
                columns: new[] { "ObjectCustodyId", "EventSequence" });

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_provisional_objects_attempt_binding",
                schema: "tagekyc",
                table: "raw_export_provisional_objects",
                columns: new[] { "AttemptId", "AttemptKeyReservationId", "SourceArtifactId", "ProvisionalObjectIdentity" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_provisional_objects_attempt",
                schema: "tagekyc",
                table: "raw_export_provisional_objects",
                column: "AttemptId");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_provisional_objects_identity",
                schema: "tagekyc",
                table: "raw_export_provisional_objects",
                column: "ProvisionalObjectIdentity");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_provisional_objects_key",
                schema: "tagekyc",
                table: "raw_export_provisional_objects",
                column: "ObjectKey");

            migrationBuilder.Sql("""
                ALTER TABLE tagekyc.raw_export_provisional_objects OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_provisional_object_events OWNER TO tagekyc_raw_export_deployer;

                CREATE FUNCTION tagekyc.compute_raw_export_provisional_object_binding(
                  p_attempt_id uuid,
                  p_attempt_key_reservation_id uuid,
                  p_source_artifact_id uuid,
                  p_provisional_object_identity uuid,
                  p_encryption_attempt_revision bigint,
                  p_attempt_fence bigint,
                  p_encryption_attempt_fingerprint bytea,
                  p_object_key text)
                RETURNS bytea
                LANGUAGE sql
                IMMUTABLE STRICT
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $binding$
                  SELECT tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(pg_catalog.normalize('tip-88c1-provisional-object-binding-v1'), 'UTF8'))) || pg_catalog.convert_to(pg_catalog.normalize('tip-88c1-provisional-object-binding-v1'), 'UTF8') ||
                    pg_catalog.int4send(32) || pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_attempt_id::text), '-', ''), 'UTF8') ||
                    pg_catalog.int4send(32) || pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_attempt_key_reservation_id::text), '-', ''), 'UTF8') ||
                    pg_catalog.int4send(32) || pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_source_artifact_id::text), '-', ''), 'UTF8') ||
                    pg_catalog.int4send(32) || pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_provisional_object_identity::text), '-', ''), 'UTF8') ||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_encryption_attempt_revision::text, 'UTF8'))) || pg_catalog.convert_to(p_encryption_attempt_revision::text, 'UTF8') ||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_attempt_fence::text, 'UTF8'))) || pg_catalog.convert_to(p_attempt_fence::text, 'UTF8') ||
                    pg_catalog.int4send(64) || pg_catalog.convert_to(pg_catalog.encode(p_encryption_attempt_fingerprint, 'hex'), 'UTF8') ||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(pg_catalog.normalize(p_object_key), 'UTF8'))) || pg_catalog.convert_to(pg_catalog.normalize(p_object_key), 'UTF8'),
                    'sha256');
                $binding$;

                CREATE FUNCTION tagekyc.enforce_raw_export_provisional_object_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $guard$
                BEGIN
                  IF TG_OP = 'TRUNCATE' OR TG_OP = 'DELETE' OR
                     pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context', true)
                       IS DISTINCT FROM 'tip88c1-object-head-write-v1' THEN
                    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_DIRECT_MUTATION_FORBIDDEN';
                  END IF;
                  RETURN NEW;
                END
                $guard$;

                CREATE FUNCTION tagekyc.enforce_raw_export_provisional_event_append()
                RETURNS trigger
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $guard$
                BEGIN
                  IF TG_OP = 'TRUNCATE' OR TG_OP <> 'INSERT' OR
                     pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context', true)
                       IS DISTINCT FROM 'tip88c1-object-event-append-v1' THEN
                    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVENT_APPEND_FORBIDDEN';
                  END IF;
                  RETURN NEW;
                END
                $guard$;

                CREATE TRIGGER trg_raw_export_provisional_object_write
                BEFORE INSERT OR UPDATE OR DELETE OR TRUNCATE ON tagekyc.raw_export_provisional_objects
                FOR EACH STATEMENT EXECUTE FUNCTION tagekyc.enforce_raw_export_provisional_object_write();

                CREATE TRIGGER trg_raw_export_provisional_event_append
                BEFORE INSERT OR UPDATE OR DELETE OR TRUNCATE ON tagekyc.raw_export_provisional_object_events
                FOR EACH STATEMENT EXECUTE FUNCTION tagekyc.enforce_raw_export_provisional_event_append();
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION tagekyc.raw_export_begin_provisional_object_custody(
                  p_attempt_id uuid, p_expected_encryption_attempt_revision bigint,
                  p_expected_fence bigint, p_maximum_provisional_objects_per_source integer)
                RETURNS TABLE(
                  "OutcomeCode" text, "ObjectCustodyId" uuid, "ObjectState" text, "StateRevision" bigint,
                  "ObjectKey" text, "ProvisionalObjectIdentity" uuid, "ObjectBindingDigest" bytea,
                  "RawClass" text, "ClaimedPlaintextLength" bigint,
                  "OwnershipLeaseExpiresAtUtc" timestamptz,
                  "EffectivePlaintextRetentionExpiresAtUtc" timestamptz,
                  "ReservationExpiresAtUtc" timestamptz, "ProjectionAtUtc" timestamptz)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE
                  a record; h record; r record; c record; k record; existing record;
                  actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
                  object_id uuid:=pg_catalog.gen_random_uuid(); object_key text; binding bytea;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context', true);
                BEGIN
                  IF p_attempt_id IS NULL OR p_expected_encryption_attempt_revision < 1 OR p_expected_fence < 1
                     OR p_maximum_provisional_objects_per_source NOT BETWEEN 1 AND 64 THEN
                    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID';
                  END IF;
                  actor_id:=tagekyc.raw_export_current_actor();
                  SELECT x.*, sh."CurrentEncryptionAttemptId", sh."ReservationRevision", sh."Fence" AS "HeadFence"
                    INTO a FROM tagekyc.raw_export_source_encryption_attempts x
                    JOIN tagekyc.raw_export_source_head sh ON sh."SourceArtifactId"=x."SourceArtifactId"
                    WHERE x."AttemptId"=p_attempt_id FOR UPDATE OF x,sh;
                  IF NOT FOUND OR a."CurrentEncryptionAttemptId"<>p_attempt_id OR a."EncryptionAttemptRevision"<>p_expected_encryption_attempt_revision
                     OR a."Fence"<>p_expected_fence OR a."HeadFence"<>p_expected_fence
                     OR a."R2TerminationDisposition" IS NOT NULL OR a."OwnershipLeaseExpiresAtUtc"<=now_utc THEN
                    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE';
                  END IF;
                  SELECT * INTO k FROM tagekyc.raw_export_attempt_key_reservations kr
                    WHERE kr."AttemptKeyReservationId"=a."AttemptKeyReservationId"
                      AND kr."AttemptId"=a."AttemptId" AND kr."PreparationDisposition"='Active' FOR UPDATE;
                  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
                  SELECT sr.*, ic."RawClass" INTO r FROM tagekyc.raw_export_source_reservations sr
                    JOIN tagekyc.raw_export_source_ingress_claims ic ON ic."IngressClaimId"=sr."IngressClaimId"
                    WHERE sr."SourceArtifactId"=a."SourceArtifactId";
                  IF r."RawClass" NOT IN ('ChipDg2Portrait','LiveSelfieImage') THEN
                    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_CLASS_NOT_SUPPORTED';
                  END IF;
                  SELECT * INTO existing FROM tagekyc.raw_export_provisional_objects po WHERE po."AttemptId"=p_attempt_id;
                  IF FOUND THEN
                    RETURN QUERY SELECT 'ExistingMatch',existing."ObjectCustodyId",existing."State"::text,existing."StateRevision",
                      existing."ObjectKey"::text,existing."ProvisionalObjectIdentity",existing."ObjectBindingDigest",r."RawClass"::text,r."ClaimedPlaintextLength",
                      a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",now_utc;
                    RETURN;
                  END IF;
                  IF (SELECT pg_catalog.count(*) FROM tagekyc.raw_export_provisional_objects po
                      WHERE po."SourceArtifactId"=a."SourceArtifactId" AND po."State" NOT IN ('NoObjectEstablished','Deleted','Quarantined'))
                     >= p_maximum_provisional_objects_per_source THEN
                    RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED';
                  END IF;
                  object_key:='raw-export/c1/v1/'||pg_catalog.replace(pg_catalog.lower(a."ProvisionalObjectIdentity"::text),'-','');
                  binding:=tagekyc.compute_raw_export_provisional_object_binding(a."AttemptId",a."AttemptKeyReservationId",a."SourceArtifactId",
                    a."ProvisionalObjectIdentity",a."EncryptionAttemptRevision",a."Fence",a."EncryptionAttemptFingerprint",object_key);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_objects(
                    "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity",
                    "EncryptionAttemptRevision","AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest",
                    "State","StateRevision","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
                  VALUES(object_id,a."AttemptId",a."AttemptKeyReservationId",a."SourceArtifactId",a."ProvisionalObjectIdentity",
                    a."EncryptionAttemptRevision",a."Fence",a."EncryptionAttemptFingerprint",object_key,binding,
                    'Initiated',1,now_utc,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events(
                    "ObjectCustodyEventId","ObjectCustodyId","EventSequence","FromState","ToState","ActorKind","StateRevision","EvidenceDigest","EventAtUtc","SchemaVersion")
                  VALUES(pg_catalog.gen_random_uuid(),object_id,1,NULL,'Initiated','Writer',1,binding,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'Created',object_id,'Initiated',1::bigint,object_key,a."ProvisionalObjectIdentity",binding,r."RawClass"::text,r."ClaimedPlaintextLength",
                    a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc",now_utc;
                EXCEPTION WHEN OTHERS THEN
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RAISE;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_arm_provisional_object_put(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_put_operation_id uuid)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; a record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
                  evidence bytea; previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_object_custody_id IS NULL OR p_expected_state_revision<1 OR p_put_operation_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor();
                  SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='PutInFlight' AND h."PutOperationId"=p_put_operation_id THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"<>'Initiated' OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=h."AttemptId";
                  IF a."Fence"<>h."AttemptFence" OR a."R2TerminationDisposition" IS NOT NULL OR a."OwnershipLeaseExpiresAtUtc"<=now_utc THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
                  evidence:=tagekyc_extensions.digest(pg_catalog.uuid_send(p_put_operation_id),'sha256');
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='PutInFlight',"StateRevision"=po."StateRevision"+1,"PutOperationId"=p_put_operation_id,"PutArmedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,'Initiated','PutInFlight','Writer',h."StateRevision"+1,evidence,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'Armed',p_object_custody_id,'PutInFlight',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_provisional_object_not_armed(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_local_termination_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); expected bytea;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_object_custody_id IS NULL OR p_expected_state_revision<1 OR p_local_termination_evidence_digest IS NULL OR pg_catalog.octet_length(p_local_termination_evidence_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='NoObjectEstablished' AND h."PutOutcomeKind"='NotArmed' THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"<>'Initiated' OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  expected:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-not-armed-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-not-armed-evidence-v1','UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                    pg_catalog.int4send(8)||pg_catalog.convert_to('NotArmed','UTF8'),'sha256');
                  IF expected<>p_local_termination_evidence_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='NoObjectEstablished',"StateRevision"=po."StateRevision"+1,"PutOutcomeKind"='NotArmed',"OutcomeObservedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,'Initiated','NoObjectEstablished','Writer',h."StateRevision"+1,expected,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'NotArmed',p_object_custody_id,'NoObjectEstablished',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_provisional_object_put_result(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_put_operation_id uuid,p_result_kind text,
                  p_provider_status_code integer,p_ciphertext_length bigint,p_ciphertext_digest bytea,p_provider_receipt_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; a record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); target text; result text; evidence bytea;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_object_custody_id IS NULL OR p_expected_state_revision<1 OR p_put_operation_id IS NULL OR p_result_kind NOT IN ('Created','ConditionalConflictObserved','OutcomeUnknown') THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State" IN ('ObjectPresentPendingVerification','PutOutcomeUnknown') AND h."PutOutcomeKind"=p_result_kind THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"<>'PutInFlight' OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=h."AttemptId";
                  IF a."Fence"<>h."AttemptFence" OR a."R2TerminationDisposition" IS NOT NULL OR a."OwnershipLeaseExpiresAtUtc"<=now_utc THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
                  IF h."PutOperationId"<>p_put_operation_id THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_BINDING_INVALID'; END IF;
                  IF p_result_kind='Created' THEN
                    IF p_provider_status_code IS DISTINCT FROM 200 OR p_ciphertext_length NOT BETWEEN 1 AND 134217728 OR p_ciphertext_digest IS NULL OR pg_catalog.octet_length(p_ciphertext_digest)<>32 OR p_provider_receipt_digest IS NULL OR pg_catalog.octet_length(p_provider_receipt_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-put-created-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-put-created-evidence-v1','UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_put_operation_id::text),'-',''),'UTF8')||
                      pg_catalog.int4send(7)||pg_catalog.convert_to('Created','UTF8')||
                      pg_catalog.int4send(3)||pg_catalog.convert_to('200','UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_ciphertext_length::text,'UTF8')))||pg_catalog.convert_to(p_ciphertext_length::text,'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_ciphertext_digest,'hex'),'UTF8'),'sha256');
                    IF evidence<>p_provider_receipt_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    target:='ObjectPresentPendingVerification'; result:='Recorded';
                  ELSIF p_result_kind='ConditionalConflictObserved' THEN
                    IF p_provider_status_code IS NULL OR p_provider_status_code NOT IN (409,412) OR p_ciphertext_length IS NOT NULL OR p_ciphertext_digest IS NOT NULL OR p_provider_receipt_digest IS NULL OR pg_catalog.octet_length(p_provider_receipt_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-put-conflict-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-put-conflict-evidence-v1','UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_put_operation_id::text),'-',''),'UTF8')||
                      pg_catalog.int4send(27)||pg_catalog.convert_to('ConditionalConflictObserved','UTF8')||
                      pg_catalog.int4send(3)||pg_catalog.convert_to(p_provider_status_code::text,'UTF8'),'sha256');
                    IF evidence<>p_provider_receipt_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    target:='PutOutcomeUnknown'; result:='OutcomeUnknown';
                  ELSE
                    IF p_provider_status_code IS NOT NULL OR p_ciphertext_length IS NOT NULL OR p_ciphertext_digest IS NOT NULL OR p_provider_receipt_digest IS NOT NULL THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    target:='PutOutcomeUnknown'; result:='OutcomeUnknown';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-put-unknown-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-put-unknown-evidence-v1','UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(p_put_operation_id::text),'-',''),'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                      pg_catalog.int4send(14)||pg_catalog.convert_to('OutcomeUnknown','UTF8'),'sha256');
                  END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"=target,"StateRevision"=po."StateRevision"+1,"PutOutcomeKind"=p_result_kind,"OutcomeObservedAtUtc"=now_utc,"CiphertextLength"=p_ciphertext_length,"CiphertextDigest"=p_ciphertext_digest,"ProviderReceiptDigest"=p_provider_receipt_digest,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,'PutInFlight',target,'Writer',h."StateRevision"+1,evidence,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT result,p_object_custody_id,target,h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_resolve_provisional_object_put_outcome(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_resolution_kind text,
                  p_ciphertext_length bigint,p_ciphertext_digest bytea,p_first_observed_at_utc timestamptz,
                  p_second_observed_at_utc timestamptz,p_observation_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; a record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); target text; result text;
                  expected bytea; first_ticks text; second_ticks text; length_text text; digest_text text;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_object_custody_id IS NULL OR p_expected_state_revision<1 OR p_resolution_kind NOT IN ('RecoveredPresent','PositiveAbsence','RecoveredMismatch','StillUnknown') OR p_observation_evidence_digest IS NULL OR pg_catalog.octet_length(p_observation_evidence_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF (h."State"='ObjectPresentPendingVerification' AND p_resolution_kind='RecoveredPresent') OR (h."State"='NoObjectEstablished' AND p_resolution_kind='PositiveAbsence') OR (h."State"='ObjectConflict' AND p_resolution_kind='RecoveredMismatch') THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State" NOT IN ('PutInFlight','PutOutcomeUnknown') OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=h."AttemptId";
                  IF h."State"='PutInFlight' AND a."OwnershipLeaseExpiresAtUtc">=now_utc THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE'; END IF;
                  IF p_resolution_kind='StillUnknown' THEN RETURN QUERY SELECT 'OutcomeUnknown',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF p_first_observed_at_utc IS NULL OR p_first_observed_at_utc>now_utc THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  IF p_resolution_kind='PositiveAbsence' THEN
                    IF p_second_observed_at_utc IS NULL OR p_second_observed_at_utc<=p_first_observed_at_utc OR p_ciphertext_length IS NOT NULL OR p_ciphertext_digest IS NOT NULL THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    target:='NoObjectEstablished'; result:='PositiveAbsence';
                  ELSIF p_resolution_kind='RecoveredPresent' THEN
                    IF p_second_observed_at_utc IS NOT NULL OR p_ciphertext_length NOT BETWEEN 1 AND 134217728 OR p_ciphertext_digest IS NULL OR pg_catalog.octet_length(p_ciphertext_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    target:='ObjectPresentPendingVerification'; result:='RecoveredPresent';
                  ELSE
                    IF p_second_observed_at_utc IS NOT NULL OR ((p_ciphertext_length IS NULL)<>(p_ciphertext_digest IS NULL)) OR (p_ciphertext_length IS NOT NULL AND (p_ciphertext_length NOT BETWEEN 1 AND 134217728 OR pg_catalog.octet_length(p_ciphertext_digest)<>32)) THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                    target:='ObjectConflict'; result:='ConditionalConflict';
                  END IF;
                  first_ticks:=(621355968000000000::numeric+extract(epoch FROM p_first_observed_at_utc)*10000000)::bigint::text;
                  second_ticks:=CASE WHEN p_second_observed_at_utc IS NULL THEN 'none' ELSE (621355968000000000::numeric+extract(epoch FROM p_second_observed_at_utc)*10000000)::bigint::text END;
                  length_text:=COALESCE(p_ciphertext_length::text,'none');
                  digest_text:=CASE WHEN p_ciphertext_digest IS NULL THEN 'none' ELSE pg_catalog.encode(p_ciphertext_digest,'hex') END;
                  expected:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-reconcile-observation-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-reconcile-observation-v1','UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(pg_catalog.lower(h."PutOperationId"::text),'-',''),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_resolution_kind,'UTF8')))||pg_catalog.convert_to(p_resolution_kind,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(first_ticks,'UTF8')))||pg_catalog.convert_to(first_ticks,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(second_ticks,'UTF8')))||pg_catalog.convert_to(second_ticks,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(length_text,'UTF8')))||pg_catalog.convert_to(length_text,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(digest_text,'UTF8')))||pg_catalog.convert_to(digest_text,'UTF8'),'sha256');
                  IF expected<>p_observation_evidence_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"=target,"StateRevision"=po."StateRevision"+1,"PutOutcomeKind"=p_resolution_kind,"OutcomeObservedAtUtc"=now_utc,"CiphertextLength"=p_ciphertext_length,"CiphertextDigest"=p_ciphertext_digest,"ProviderReceiptDigest"=CASE WHEN p_resolution_kind='PositiveAbsence' THEN NULL ELSE p_observation_evidence_digest END,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,h."State",target,'Reconciler',h."StateRevision"+1,p_observation_evidence_digest,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT result,p_object_custody_id,target,h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_mark_provisional_object_verified(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_verification_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_object_custody_id IS NULL OR p_expected_state_revision<1 OR p_verification_evidence_digest IS NULL OR pg_catalog.octet_length(p_verification_evidence_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='VerifiedCompleted' AND h."VerificationEvidenceDigest"=p_verification_evidence_digest THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"<>'ObjectPresentPendingVerification' OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='VerifiedCompleted',"StateRevision"=po."StateRevision"+1,"VerificationEvidenceDigest"=p_verification_evidence_digest,"VerifiedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,'ObjectPresentPendingVerification','VerifiedCompleted','Reconciler',h."StateRevision"+1,p_verification_evidence_digest,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'Verified',p_object_custody_id,'VerifiedCompleted',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_mark_provisional_object_cleanup_required(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_cleanup_reason_code text,p_cleanup_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); expected bytea;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_object_custody_id IS NULL OR p_expected_state_revision<1 OR p_cleanup_evidence_digest IS NULL OR pg_catalog.octet_length(p_cleanup_evidence_digest)<>32 OR p_cleanup_reason_code NOT IN ('VerificationFailed','SourceExpired','SourceConsumed','SourceCancelled') THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='CleanupPending' AND h."CleanupReasonCode"=p_cleanup_reason_code AND h."CleanupEvidenceDigest"=p_cleanup_evidence_digest THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."StateRevision"<>p_expected_state_revision OR NOT ((h."State"='ObjectPresentPendingVerification' AND p_cleanup_reason_code='VerificationFailed') OR (h."State"='VerifiedCompleted' AND p_cleanup_reason_code IN ('SourceExpired','SourceConsumed','SourceCancelled'))) THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  expected:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-cleanup-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-cleanup-evidence-v1','UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(h."State",'UTF8')))||pg_catalog.convert_to(h."State",'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_cleanup_reason_code,'UTF8')))||pg_catalog.convert_to(p_cleanup_reason_code,'UTF8'),'sha256');
                  IF expected<>p_cleanup_evidence_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='CleanupPending',"StateRevision"=po."StateRevision"+1,"CleanupReasonCode"=p_cleanup_reason_code,"CleanupEvidenceDigest"=p_cleanup_evidence_digest,"CleanupRequestedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,h."State",'CleanupPending',CASE WHEN p_cleanup_reason_code='VerificationFailed' THEN 'Reconciler' ELSE 'Lifecycle' END,h."StateRevision"+1,p_cleanup_evidence_digest,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'CleanupRequired',p_object_custody_id,'CleanupPending',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_provisional_object_delete_acknowledged(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_provider_status_code integer,p_deletion_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); expected bytea;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_provider_status_code IS DISTINCT FROM 204 OR p_deletion_evidence_digest IS NULL OR pg_catalog.octet_length(p_deletion_evidence_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='Deleted' AND h."DeletionEvidenceKind"='DeleteAcknowledged' AND h."DeletionEvidenceDigest"=p_deletion_evidence_digest THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"<>'CleanupPending' OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  expected:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-delete-ack-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-delete-ack-evidence-v1','UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                    pg_catalog.int4send(18)||pg_catalog.convert_to('DeleteAcknowledged','UTF8')||
                    pg_catalog.int4send(3)||pg_catalog.convert_to('204','UTF8'),'sha256');
                  IF expected<>p_deletion_evidence_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='Deleted',"StateRevision"=po."StateRevision"+1,"DeletionEvidenceDigest"=p_deletion_evidence_digest,"DeletionEvidenceKind"='DeleteAcknowledged',"DeletedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,'CleanupPending','Deleted','Lifecycle',h."StateRevision"+1,p_deletion_evidence_digest,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'Deleted',p_object_custody_id,'Deleted',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_provisional_object_absence_confirmed(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_first_absence_observed_at_utc timestamptz,p_second_absence_observed_at_utc timestamptz,p_deletion_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); expected bytea; first_ticks text; second_ticks text;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_first_absence_observed_at_utc IS NULL OR p_second_absence_observed_at_utc IS NULL OR p_second_absence_observed_at_utc<=p_first_absence_observed_at_utc OR p_second_absence_observed_at_utc>now_utc OR p_deletion_evidence_digest IS NULL OR pg_catalog.octet_length(p_deletion_evidence_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='Deleted' AND h."DeletionEvidenceKind"='PositiveAbsenceConfirmed' AND h."DeletionEvidenceDigest"=p_deletion_evidence_digest THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"<>'CleanupPending' OR h."StateRevision"<>p_expected_state_revision THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  first_ticks:=(621355968000000000::numeric+extract(epoch FROM p_first_absence_observed_at_utc)*10000000)::bigint::text;
                  second_ticks:=(621355968000000000::numeric+extract(epoch FROM p_second_absence_observed_at_utc)*10000000)::bigint::text;
                  expected:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-object-delete-absence-evidence-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-object-delete-absence-evidence-v1','UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(first_ticks,'UTF8')))||pg_catalog.convert_to(first_ticks,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(second_ticks,'UTF8')))||pg_catalog.convert_to(second_ticks,'UTF8')||
                    pg_catalog.int4send(24)||pg_catalog.convert_to('PositiveAbsenceConfirmed','UTF8'),'sha256');
                  IF expected<>p_deletion_evidence_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='Deleted',"StateRevision"=po."StateRevision"+1,"DeletionEvidenceDigest"=p_deletion_evidence_digest,"DeletionEvidenceKind"='PositiveAbsenceConfirmed',"DeletedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,'CleanupPending','Deleted','Reconciler',h."StateRevision"+1,p_deletion_evidence_digest,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'Deleted',p_object_custody_id,'Deleted',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_provisional_object_quarantined(
                  p_object_custody_id uuid,p_expected_state_revision bigint,p_quarantine_reason_code text,p_quarantine_evidence_digest bytea)
                RETURNS TABLE("OutcomeCode" text,"ObjectCustodyId" uuid,"ObjectState" text,"StateRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE h record; actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp(); expected bytea; supporting bytea; domain text;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_provisional_object_write_context',true);
                BEGIN
                  IF p_quarantine_reason_code NOT IN ('DeleteOutcomeIndeterminateTerminal','ObjectBindingMismatch','CiphertextMetadataMismatch','ObjectFormatInvalid') OR p_quarantine_evidence_digest IS NULL OR pg_catalog.octet_length(p_quarantine_evidence_digest)<>32 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor(); SELECT * INTO h FROM tagekyc.raw_export_provisional_objects po WHERE po."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_object_custody_id,NULL::text,0::bigint; RETURN; END IF;
                  IF h."State"='Quarantined' AND h."QuarantineReasonCode"=p_quarantine_reason_code AND h."QuarantineEvidenceDigest"=p_quarantine_evidence_digest THEN RETURN QUERY SELECT 'ExistingMatch',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."StateRevision"<>p_expected_state_revision OR NOT ((h."State"='CleanupPending' AND p_quarantine_reason_code='DeleteOutcomeIndeterminateTerminal') OR (h."State"='ObjectConflict' AND p_quarantine_reason_code IN ('ObjectBindingMismatch','CiphertextMetadataMismatch','ObjectFormatInvalid'))) THEN RETURN QUERY SELECT 'StateConflict',h."ObjectCustodyId",h."State"::text,h."StateRevision"; RETURN; END IF;
                  IF h."State"='CleanupPending' THEN
                    domain:='tip-88c1-object-cleanup-quarantine-evidence-v1'; supporting:=h."CleanupEvidenceDigest";
                    expected:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(domain,'UTF8')))||pg_catalog.convert_to(domain,'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(14)||pg_catalog.convert_to('CleanupPending','UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                      pg_catalog.int4send(34)||pg_catalog.convert_to('DeleteOutcomeIndeterminateTerminal','UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(supporting,'hex'),'UTF8'),'sha256');
                  ELSE
                    domain:='tip-88c1-object-conflict-quarantine-evidence-v1'; supporting:=h."ProviderReceiptDigest";
                    expected:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(domain,'UTF8')))||pg_catalog.convert_to(domain,'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(h."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(14)||pg_catalog.convert_to('ObjectConflict','UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')))||pg_catalog.convert_to(p_expected_state_revision::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_quarantine_reason_code,'UTF8')))||pg_catalog.convert_to(p_quarantine_reason_code,'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(supporting,'hex'),'UTF8'),'sha256');
                  END IF;
                  IF supporting IS NULL OR expected<>p_quarantine_evidence_digest THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_EVIDENCE_INVALID'; END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
                  UPDATE tagekyc.raw_export_provisional_objects po SET "State"='Quarantined',"StateRevision"=po."StateRevision"+1,"QuarantineReasonCode"=p_quarantine_reason_code,"QuarantineEvidenceDigest"=p_quarantine_evidence_digest,"QuarantinedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE po."ObjectCustodyId"=p_object_custody_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-event-append-v1',true);
                  INSERT INTO tagekyc.raw_export_provisional_object_events VALUES(pg_catalog.gen_random_uuid(),p_object_custody_id,h."StateRevision"+1,h."State",'Quarantined','Lifecycle',h."StateRevision"+1,p_quarantine_evidence_digest,now_utc,1);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true);
                  RETURN QUERY SELECT 'Quarantined',p_object_custody_id,'Quarantined',h."StateRevision"+1;
                EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context',COALESCE(previous_context,''),true); RAISE; END $fn$;

                CREATE FUNCTION tagekyc.raw_export_read_provisional_object_reconcile_context(p_object_custody_id uuid)
                RETURNS TABLE(
                  "ObjectCustodyId" uuid,"AttemptId" uuid,"AttemptKeyReservationId" uuid,"SourceArtifactId" uuid,
                  "ProvisionalObjectIdentity" uuid,"EncryptionAttemptRevision" bigint,"AttemptFence" bigint,
                  "EncryptionAttemptFingerprint" bytea,"ObjectKey" text,"ObjectBindingDigest" bytea,"State" text,
                  "StateRevision" bigint,"PutOperationId" uuid,"PutArmedAtUtc" timestamptz,"CiphertextLength" bigint,
                  "CiphertextDigest" bytea,"ProviderReceiptDigest" bytea,"VerificationEvidenceDigest" bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE actor_id uuid;
                BEGIN
                  IF p_object_custody_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor();
                  RETURN QUERY SELECT p."ObjectCustodyId",p."AttemptId",p."AttemptKeyReservationId",p."SourceArtifactId",p."ProvisionalObjectIdentity",p."EncryptionAttemptRevision",p."AttemptFence",p."EncryptionAttemptFingerprint",p."ObjectKey",p."ObjectBindingDigest",p."State",p."StateRevision",p."PutOperationId",p."PutArmedAtUtc",p."CiphertextLength",p."CiphertextDigest",p."ProviderReceiptDigest",p."VerificationEvidenceDigest" FROM tagekyc.raw_export_provisional_objects p WHERE p."ObjectCustodyId"=p_object_custody_id;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(p_object_custody_id uuid)
                RETURNS TABLE(
                  "ObjectCustodyId" uuid,"AttemptId" uuid,"ProvisionalObjectIdentity" uuid,"ObjectKey" text,
                  "ObjectBindingDigest" bytea,"State" text,"StateRevision" bigint,"PutOperationId" uuid,
                  "CleanupReasonCode" text,"CleanupEvidenceDigest" bytea,"CleanupRequestedAtUtc" timestamptz,
                  "ProviderReceiptDigest" bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE actor_id uuid;
                BEGIN
                  IF p_object_custody_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID'; END IF;
                  actor_id:=tagekyc.raw_export_current_actor();
                  RETURN QUERY SELECT p."ObjectCustodyId",p."AttemptId",p."ProvisionalObjectIdentity",p."ObjectKey",p."ObjectBindingDigest",p."State",p."StateRevision",p."PutOperationId",p."CleanupReasonCode",p."CleanupEvidenceDigest",p."CleanupRequestedAtUtc",p."ProviderReceiptDigest" FROM tagekyc.raw_export_provisional_objects p WHERE p."ObjectCustodyId"=p_object_custody_id;
                END $fn$;

                ALTER FUNCTION tagekyc.compute_raw_export_provisional_object_binding(uuid,uuid,uuid,uuid,bigint,bigint,bytea,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_provisional_object_write() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_provisional_event_append() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_provisional_object_reconcile_context(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid) OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON tagekyc.raw_export_provisional_objects,tagekyc.raw_export_provisional_object_events FROM PUBLIC;
                REVOKE ALL ON tagekyc.raw_export_provisional_objects,tagekyc.raw_export_provisional_object_events FROM tagekyc_runtime,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;
                REVOKE ALL ON FUNCTION
                  tagekyc.compute_raw_export_provisional_object_binding(uuid,uuid,uuid,uuid,bigint,bigint,bytea,text),
                  tagekyc.enforce_raw_export_provisional_object_write(),
                  tagekyc.enforce_raw_export_provisional_event_append(),
                  tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer),
                  tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid),
                  tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea),
                  tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea),
                  tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea),
                  tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea),
                  tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea),
                  tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea),
                  tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea),
                  tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea),
                  tagekyc.raw_export_read_provisional_object_reconcile_context(uuid),
                  tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)
                FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;

                GRANT EXECUTE ON FUNCTION
                  tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer),
                  tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid),
                  tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea),
                  tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)
                TO tagekyc_raw_export_custody_encryptor;
                GRANT EXECUTE ON FUNCTION
                  tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea),
                  tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea),
                  tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea),
                  tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea),
                  tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)
                TO tagekyc_raw_export_reconciler;
                GRANT EXECUTE ON FUNCTION
                  tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea),
                  tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea),
                  tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea),
                  tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)
                TO tagekyc_raw_export_lifecycle;

                DO $acl$
                DECLARE catalog_ok boolean;
                BEGIN
                  WITH function_manifest(signature, security_definer, exact_config) AS (VALUES
                    ('tagekyc.compute_raw_export_provisional_object_binding(uuid,uuid,uuid,uuid,bigint,bigint,bytea,text)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.enforce_raw_export_provisional_object_write()',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.enforce_raw_export_provisional_event_append()',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)',true,ARRAY['search_path=pg_catalog']::text[]),
                    ('tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)',true,ARRAY['search_path=pg_catalog']::text[])
                  ), expected_acl(signature, grantee) AS (VALUES
                    ('tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)','tagekyc_raw_export_custody_encryptor'),
                    ('tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid)','tagekyc_raw_export_custody_encryptor'),
                    ('tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea)','tagekyc_raw_export_custody_encryptor'),
                    ('tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)','tagekyc_raw_export_custody_encryptor'),
                    ('tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea)','tagekyc_raw_export_reconciler'),
                    ('tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea)','tagekyc_raw_export_reconciler'),
                    ('tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)','tagekyc_raw_export_reconciler'),
                    ('tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)','tagekyc_raw_export_lifecycle'),
                    ('tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea)','tagekyc_raw_export_lifecycle'),
                    ('tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea)','tagekyc_raw_export_reconciler'),
                    ('tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea)','tagekyc_raw_export_lifecycle'),
                    ('tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)','tagekyc_raw_export_reconciler'),
                    ('tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)','tagekyc_raw_export_lifecycle')
                  ), actual_acl AS (
                    SELECT f.signature, grantee.rolname AS grantee, grantor.rolname AS grantor,
                           a.privilege_type, a.is_grantable
                    FROM function_manifest f
                    JOIN pg_catalog.pg_proc p ON p.oid=pg_catalog.to_regprocedure(f.signature)
                    CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                    JOIN pg_catalog.pg_roles grantee ON grantee.oid=a.grantee
                    JOIN pg_catalog.pg_roles grantor ON grantor.oid=a.grantor
                    WHERE a.grantee<>p.proowner
                  ), expected_members(member_name, role_name) AS (VALUES
                    ('tagekyc_raw_export_encryptor_login','tagekyc_raw_export_custody_encryptor'),
                    ('tagekyc_raw_export_reconciler_login','tagekyc_raw_export_reconciler'),
                    ('tagekyc_raw_export_lifecycle_login','tagekyc_raw_export_lifecycle')
                  ), actual_members AS (
                    SELECT member.rolname AS member_name, role.rolname AS role_name,
                           m.admin_option,m.inherit_option,m.set_option
                    FROM pg_catalog.pg_auth_members m
                    JOIN pg_catalog.pg_roles member ON member.oid=m.member
                    JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
                    WHERE member.rolname IN (
                            'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                            'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login')
                       OR role.rolname IN (
                            'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                            'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login')
                  )
                  SELECT
                    (SELECT count(*)=2 AND pg_catalog.bool_and(owner.rolname='tagekyc_raw_export_deployer' AND c.relkind='r')
                     FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                     JOIN pg_catalog.pg_roles owner ON owner.oid=c.relowner
                     WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events'))
                    AND NOT EXISTS (
                      SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                      CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) a
                      WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events')
                        AND a.grantee<>c.relowner)
                    AND NOT EXISTS (
                      SELECT 1 FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid
                      JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                      WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events')
                        AND a.attnum>0 AND NOT a.attisdropped AND a.attacl IS NOT NULL)
                    AND (SELECT count(*)=15 AND pg_catalog.bool_and(p.oid IS NOT NULL
                               AND owner.rolname='tagekyc_raw_export_deployer' AND p.prosecdef=f.security_definer
                               AND p.proconfig IS NOT DISTINCT FROM f.exact_config)
                         FROM function_manifest f
                         LEFT JOIN pg_catalog.pg_proc p ON p.oid=pg_catalog.to_regprocedure(f.signature)
                         LEFT JOIN pg_catalog.pg_roles owner ON owner.oid=p.proowner)
                    AND (SELECT count(*)=15 FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                         WHERE n.nspname='tagekyc' AND p.proname IN (
                           'compute_raw_export_provisional_object_binding','enforce_raw_export_provisional_object_write',
                           'enforce_raw_export_provisional_event_append','raw_export_begin_provisional_object_custody',
                           'raw_export_arm_provisional_object_put','raw_export_record_provisional_object_not_armed',
                           'raw_export_record_provisional_object_put_result','raw_export_resolve_provisional_object_put_outcome',
                           'raw_export_mark_provisional_object_verified','raw_export_mark_provisional_object_cleanup_required',
                           'raw_export_record_provisional_object_delete_acknowledged','raw_export_record_provisional_object_absence_confirmed',
                           'raw_export_record_provisional_object_quarantined','raw_export_read_provisional_object_reconcile_context',
                           'raw_export_read_provisional_object_lifecycle_context'))
                    AND NOT EXISTS (
                      (SELECT signature,grantee,'tagekyc_raw_export_deployer'::text,'EXECUTE'::text,false FROM expected_acl
                       EXCEPT SELECT signature,grantee,grantor,privilege_type,is_grantable FROM actual_acl)
                      UNION ALL
                      (SELECT signature,grantee,grantor,privilege_type,is_grantable FROM actual_acl
                       EXCEPT SELECT signature,grantee,'tagekyc_raw_export_deployer'::text,'EXECUTE'::text,false FROM expected_acl))
                    AND (SELECT count(*)=6 AND pg_catalog.bool_and(NOT r.rolsuper AND NOT r.rolcreatedb
                               AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls AND r.rolinherit
                               AND r.rolcanlogin=(r.rolname LIKE '%_login'))
                         FROM pg_catalog.pg_roles r WHERE r.rolname IN (
                           'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                           'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login'))
                    AND NOT EXISTS (
                      (SELECT member_name,role_name,false,true,false FROM expected_members
                       EXCEPT SELECT member_name,role_name,admin_option,inherit_option,set_option FROM actual_members)
                      UNION ALL
                      (SELECT member_name,role_name,admin_option,inherit_option,set_option FROM actual_members
                       EXCEPT SELECT member_name,role_name,false,true,false FROM expected_members))
                  INTO catalog_ok;
                  IF NOT COALESCE(catalog_ok,false) THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID';
                  END IF;
                END $acl$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE ALL ON FUNCTION
                  tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer),
                  tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid),
                  tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea),
                  tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)
                FROM tagekyc_raw_export_custody_encryptor;
                REVOKE ALL ON FUNCTION
                  tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea),
                  tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea),
                  tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea),
                  tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea),
                  tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)
                FROM tagekyc_raw_export_reconciler;
                REVOKE ALL ON FUNCTION
                  tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea),
                  tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea),
                  tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea),
                  tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)
                FROM tagekyc_raw_export_lifecycle;

                DROP FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid);
                DROP FUNCTION tagekyc.raw_export_read_provisional_object_reconcile_context(uuid);
                DROP FUNCTION tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea);
                DROP FUNCTION tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea);
                DROP FUNCTION tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea);
                DROP FUNCTION tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea);
                DROP FUNCTION tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea);
                DROP FUNCTION tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea);
                DROP FUNCTION tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea);
                DROP FUNCTION tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea);
                DROP FUNCTION tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid);
                DROP FUNCTION tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer);
                DROP TRIGGER trg_raw_export_provisional_event_append ON tagekyc.raw_export_provisional_object_events;
                DROP TRIGGER trg_raw_export_provisional_object_write ON tagekyc.raw_export_provisional_objects;
                DROP FUNCTION tagekyc.enforce_raw_export_provisional_event_append();
                DROP FUNCTION tagekyc.enforce_raw_export_provisional_object_write();
                DROP FUNCTION tagekyc.compute_raw_export_provisional_object_binding(uuid,uuid,uuid,uuid,bigint,bigint,bytea,text);
                """);

            migrationBuilder.DropTable(
                name: "raw_export_provisional_object_events",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_provisional_objects",
                schema: "tagekyc");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_raw_export_source_attempt_object_binding",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");

        }
    }
}
