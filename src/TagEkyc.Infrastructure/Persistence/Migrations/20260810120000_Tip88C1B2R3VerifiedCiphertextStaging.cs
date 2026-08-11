using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations;

public partial class Tip88C1B2R3VerifiedCiphertextStaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_raw_export_source_attempt_values",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts");
        migrationBuilder.DropCheckConstraint(
            name: "ck_raw_export_source_head_values",
            schema: "tagekyc",
            table: "raw_export_source_head");

        migrationBuilder.AddColumn<int>(name: "StagedCiphertextFingerprintSchemaVersion", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "integer", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "StagedCiphertextFingerprint", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "StagedObjectCustodyId", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<long>(name: "StagedObjectStateRevision", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<long>(name: "StagedFromReservationRevision", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<long>(name: "VerifiedPlaintextLength", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<long>(name: "StagedCiphertextLength", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "StagedCiphertextDigest", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "StagedProviderReceiptDigest", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "StagedVerificationEvidenceDigest", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "StagedAtUtc", schema: "tagekyc", table: "raw_export_source_encryption_attempts", type: "timestamp with time zone", nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_raw_export_source_attempt_values",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts",
            sql: AttemptCheck);
        migrationBuilder.AddCheckConstraint(
            name: "ck_raw_export_source_head_values",
            schema: "tagekyc",
            table: "raw_export_source_head",
            sql: "\"CustodyState\" IN ('Reserved','Staged') AND \"ReservationRevision\" >= 1 AND \"Fence\" >= 1");
        migrationBuilder.CreateIndex(
            name: "IX_raw_export_source_encryption_attempts_StagedObjectCustodyId",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts",
            column: "StagedObjectCustodyId");
        migrationBuilder.AddForeignKey(
            name: "fk_raw_export_source_attempt_staged_object",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts",
            column: "StagedObjectCustodyId",
            principalSchema: "tagekyc",
            principalTable: "raw_export_provisional_objects",
            principalColumn: "ObjectCustodyId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DownPreflightSql);

        migrationBuilder.DropForeignKey(
            name: "fk_raw_export_source_attempt_staged_object",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts");

        migrationBuilder.Sql(DownRestoreSql);

        migrationBuilder.DropIndex(
            name: "IX_raw_export_source_encryption_attempts_StagedObjectCustodyId",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts");
        migrationBuilder.DropCheckConstraint(
            name: "ck_raw_export_source_attempt_values",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts");
        migrationBuilder.DropCheckConstraint(
            name: "ck_raw_export_source_head_values",
            schema: "tagekyc",
            table: "raw_export_source_head");

        foreach (var column in StagingColumns)
            migrationBuilder.DropColumn(name: column, schema: "tagekyc", table: "raw_export_source_encryption_attempts");

        migrationBuilder.AddCheckConstraint(
            name: "ck_raw_export_source_attempt_values",
            schema: "tagekyc",
            table: "raw_export_source_encryption_attempts",
            sql: R2AttemptCheck);
        migrationBuilder.AddCheckConstraint(
            name: "ck_raw_export_source_head_values",
            schema: "tagekyc",
            table: "raw_export_source_head",
            sql: "\"CustodyState\" = 'Reserved' AND \"ReservationRevision\" >= 1 AND \"Fence\" >= 1");
    }

    private static readonly string[] StagingColumns =
    [
        "StagedCiphertextFingerprintSchemaVersion",
        "StagedCiphertextFingerprint",
        "StagedObjectCustodyId",
        "StagedObjectStateRevision",
        "StagedFromReservationRevision",
        "VerifiedPlaintextLength",
        "StagedCiphertextLength",
        "StagedCiphertextDigest",
        "StagedProviderReceiptDigest",
        "StagedVerificationEvidenceDigest",
        "StagedAtUtc",
    ];

    private const string R2AttemptCheck = """
        "SchemaVersion" = 1
        AND "EncryptionAttemptRevision" >= 1
        AND "Fence" >= 1
        AND "KekVersion" >= 1
        AND "EncryptionFramingVersion" >= 1
        AND "ChunkSize" >= 1
        AND octet_length("NonceDerivationSeedCommitment") = 32
        AND octet_length("FramingParametersDigest") = 32
        AND octet_length("EncryptionAttemptFingerprint") = 32
        AND (
          ("R2TerminationDisposition" IS NULL AND "R2TerminatedAtUtc" IS NULL)
          OR
          ("R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
           AND "R2TerminatedAtUtc" IS NOT NULL)
        )
        """;

    private const string AttemptCheck = R2AttemptCheck + """
        AND (
          (
            "StagedCiphertextFingerprintSchemaVersion" IS NULL
            AND "StagedCiphertextFingerprint" IS NULL
            AND "StagedObjectCustodyId" IS NULL
            AND "StagedObjectStateRevision" IS NULL
            AND "StagedFromReservationRevision" IS NULL
            AND "VerifiedPlaintextLength" IS NULL
            AND "StagedCiphertextLength" IS NULL
            AND "StagedCiphertextDigest" IS NULL
            AND "StagedProviderReceiptDigest" IS NULL
            AND "StagedVerificationEvidenceDigest" IS NULL
            AND "StagedAtUtc" IS NULL
          )
          OR
          (
            "R2TerminationDisposition" IS NULL
            AND "R2TerminatedAtUtc" IS NULL
            AND "StagedCiphertextFingerprintSchemaVersion" IS NOT NULL
            AND "StagedCiphertextFingerprint" IS NOT NULL
            AND "StagedObjectCustodyId" IS NOT NULL
            AND "StagedObjectStateRevision" IS NOT NULL
            AND "StagedFromReservationRevision" IS NOT NULL
            AND "VerifiedPlaintextLength" IS NOT NULL
            AND "StagedCiphertextLength" IS NOT NULL
            AND "StagedCiphertextDigest" IS NOT NULL
            AND "StagedProviderReceiptDigest" IS NOT NULL
            AND "StagedVerificationEvidenceDigest" IS NOT NULL
            AND "StagedAtUtc" IS NOT NULL
            AND "StagedCiphertextFingerprintSchemaVersion" = 2
            AND octet_length("StagedCiphertextFingerprint") = 32
            AND "StagedObjectStateRevision" >= 1
            AND "StagedFromReservationRevision" >= 1
            AND "VerifiedPlaintextLength" >= 1
            AND "StagedCiphertextLength" BETWEEN 1 AND 134217728
            AND octet_length("StagedCiphertextDigest") = 32
            AND octet_length("StagedProviderReceiptDigest") = 32
            AND octet_length("StagedVerificationEvidenceDigest") = 32
            AND "StagedAtUtc" IS NOT NULL
          )
        )
        """;

    private const string UpSql = """
        CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        BEGIN
          IF TG_OP='INSERT' THEN
            IF current_user<>'tagekyc_raw_export_deployer'
               OR pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)<>'complete-r1' THEN
              RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
            END IF;
            RETURN NEW;
          END IF;
        IF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
            IF current_user='tagekyc_raw_export_deployer'
               AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r2-termination-v1'
               AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
               AND NEW."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
               AND NEW."R2TerminatedAtUtc" IS NOT NULL
               AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
                   NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
                   NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
                   NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
                   NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
                   NEW."OwnershipLeaseExpiresAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion",
                   NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."StagedObjectCustodyId",
                   NEW."StagedObjectStateRevision",NEW."StagedFromReservationRevision",NEW."VerifiedPlaintextLength",
                   NEW."StagedCiphertextLength",NEW."StagedCiphertextDigest",NEW."StagedProviderReceiptDigest",
                   NEW."StagedVerificationEvidenceDigest",NEW."StagedAtUtc")
                   IS NOT DISTINCT FROM
                   ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
                   OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
                   OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
                   OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
                   OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
                   OLD."OwnershipLeaseExpiresAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion",
                   OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."StagedObjectCustodyId",
                   OLD."StagedObjectStateRevision",OLD."StagedFromReservationRevision",OLD."VerifiedPlaintextLength",
                   OLD."StagedCiphertextLength",OLD."StagedCiphertextDigest",OLD."StagedProviderReceiptDigest",
                   OLD."StagedVerificationEvidenceDigest",OLD."StagedAtUtc") THEN
              RETURN NEW;
            END IF;
            IF current_user='tagekyc_raw_export_deployer'
               AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
               AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
               AND NEW."R2TerminationDisposition" IS NULL AND NEW."R2TerminatedAtUtc" IS NULL
               AND OLD."StagedCiphertextFingerprintSchemaVersion" IS NULL
               AND OLD."StagedCiphertextFingerprint" IS NULL AND OLD."StagedObjectCustodyId" IS NULL
               AND OLD."StagedObjectStateRevision" IS NULL AND OLD."StagedFromReservationRevision" IS NULL
               AND OLD."VerifiedPlaintextLength" IS NULL AND OLD."StagedCiphertextLength" IS NULL
               AND OLD."StagedCiphertextDigest" IS NULL AND OLD."StagedProviderReceiptDigest" IS NULL
               AND OLD."StagedVerificationEvidenceDigest" IS NULL AND OLD."StagedAtUtc" IS NULL
               AND NEW."StagedCiphertextFingerprintSchemaVersion"=2
               AND pg_catalog.octet_length(NEW."StagedCiphertextFingerprint")=32
               AND NEW."StagedObjectCustodyId" IS NOT NULL AND NEW."StagedObjectStateRevision">=1
               AND NEW."StagedFromReservationRevision">=1 AND NEW."VerifiedPlaintextLength">=1
               AND NEW."StagedCiphertextLength" BETWEEN 1 AND 134217728
               AND pg_catalog.octet_length(NEW."StagedCiphertextDigest")=32
               AND pg_catalog.octet_length(NEW."StagedProviderReceiptDigest")=32
               AND pg_catalog.octet_length(NEW."StagedVerificationEvidenceDigest")=32
               AND NEW."StagedAtUtc" IS NOT NULL
               AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
                   NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
                   NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
                   NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
                   NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
                   NEW."OwnershipLeaseExpiresAtUtc",NEW."R2TerminationDisposition",NEW."R2TerminatedAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion")
                   IS NOT DISTINCT FROM
                   ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
                   OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
                   OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
                   OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
                   OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
                   OLD."OwnershipLeaseExpiresAtUtc",OLD."R2TerminationDisposition",OLD."R2TerminatedAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion") THEN
              RETURN NEW;
            END IF;
          END IF;
          IF TG_OP='UPDATE' THEN
            RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
          END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
        END $guard$;

        CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_head_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        BEGIN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='complete-r1'
             AND TG_OP<>'DELETE' THEN
            RETURN NEW;
          END IF;
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
             AND TG_OP='UPDATE'
             AND OLD."CustodyState"='Reserved' AND NEW."CustodyState"='Staged'
             AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
             AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
                 IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN
            RETURN NEW;
          END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
        END $guard$;

        CREATE FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(
          p_attempt_id uuid,p_object_custody_id uuid,p_expected_reservation_revision bigint,
          p_expected_encryption_attempt_revision bigint,p_expected_fence bigint,p_expected_object_state_revision bigint)
        RETURNS TABLE(
          "Outcome" text,"SourceArtifactId" uuid,"AttemptId" uuid,"ObjectCustodyId" uuid,
          "StagedCiphertextFingerprintSchemaVersion" integer,"StagedCiphertextFingerprint" bytea,
          "ReservationRevision" bigint,"Fence" bigint,"StagedAtUtc" timestamptz)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $stage$
        DECLARE
          actor_id uuid; a record; h record; r record; c record; k record; o record; consent record; authority record;
          stage_time timestamptz; fingerprint bytea;
          previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
          any_staged boolean;
        BEGIN
          IF p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_object_custody_id IS NULL OR p_object_custody_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
             OR p_expected_encryption_attempt_revision IS NULL OR p_expected_encryption_attempt_revision<1
             OR p_expected_fence IS NULL OR p_expected_fence<1
             OR p_expected_object_state_revision IS NULL OR p_expected_object_state_revision<1 THEN
            RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID';
          END IF;
          actor_id:=tagekyc.raw_export_current_actor();
          SELECT attempt.* INTO a FROM tagekyc.raw_export_source_encryption_attempts AS attempt
            WHERE attempt."AttemptId"=p_attempt_id FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
          SELECT head.* INTO h FROM tagekyc.raw_export_source_head AS head
            WHERE head."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
          SELECT reservation.* INTO r FROM tagekyc.raw_export_source_reservations AS reservation
            WHERE reservation."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
          SELECT claim.* INTO c FROM tagekyc.raw_export_source_ingress_claims AS claim
            WHERE claim."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
          SELECT key_reservation.* INTO k FROM tagekyc.raw_export_attempt_key_reservations AS key_reservation
            WHERE key_reservation."AttemptId"=a."AttemptId"
              AND key_reservation."AttemptKeyReservationId"=a."AttemptKeyReservationId" FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
          SELECT provisional_object.* INTO o FROM tagekyc.raw_export_provisional_objects AS provisional_object
            WHERE provisional_object."ObjectCustodyId"=p_object_custody_id FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

          IF h."CurrentEncryptionAttemptId"<>a."AttemptId" OR h."Fence"<>a."Fence"
             OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
             OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
             OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
             OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
             OR k."AttemptId"<>a."AttemptId" OR k."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
             OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint" THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
          END IF;

          any_staged:=a."StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR a."StagedCiphertextFingerprint" IS NOT NULL
            OR a."StagedObjectCustodyId" IS NOT NULL OR a."StagedObjectStateRevision" IS NOT NULL
            OR a."StagedFromReservationRevision" IS NOT NULL OR a."VerifiedPlaintextLength" IS NOT NULL
            OR a."StagedCiphertextLength" IS NOT NULL OR a."StagedCiphertextDigest" IS NOT NULL
            OR a."StagedProviderReceiptDigest" IS NOT NULL OR a."StagedVerificationEvidenceDigest" IS NOT NULL
            OR a."StagedAtUtc" IS NOT NULL;
          IF any_staged OR h."CustodyState"='Staged' THEN
            IF o."CiphertextLength" IS NULL OR o."CiphertextDigest" IS NULL OR o."ProviderReceiptDigest" IS NULL
               OR o."VerificationEvidenceDigest" IS NULL OR r."ClaimedPlaintextLength" IS NULL
               OR r."ContentCommitment" IS NULL
               OR o."CiphertextLength" NOT BETWEEN 1 AND 134217728
               OR pg_catalog.octet_length(o."CiphertextDigest")<>32
               OR pg_catalog.octet_length(o."ProviderReceiptDigest")<>32
               OR pg_catalog.octet_length(o."VerificationEvidenceDigest")<>32
               OR r."ClaimedPlaintextLength"<1 OR pg_catalog.octet_length(r."ContentCommitment")<>32 THEN
              RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
            END IF;
            fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-staged-ciphertext-v2',VARIADIC ARRAY[
              pg_catalog.encode(a."EncryptionAttemptFingerprint",'hex'),pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),
              pg_catalog.encode(o."ObjectBindingDigest",'hex'),r."ClaimedPlaintextLength"::text,pg_catalog.encode(r."ContentCommitment",'hex'),
              o."CiphertextLength"::text,pg_catalog.encode(o."CiphertextDigest",'hex'),pg_catalog.encode(o."ProviderReceiptDigest",'hex'),
              pg_catalog.encode(o."VerificationEvidenceDigest",'hex')]);
            IF a."StagedCiphertextFingerprintSchemaVersion"=2
               AND a."StagedCiphertextFingerprint"=fingerprint AND a."StagedObjectCustodyId"=p_object_custody_id
               AND a."StagedObjectStateRevision"=p_expected_object_state_revision
               AND a."StagedFromReservationRevision"=p_expected_reservation_revision
               AND a."EncryptionAttemptRevision"=p_expected_encryption_attempt_revision AND a."Fence"=p_expected_fence
               AND a."VerifiedPlaintextLength"=r."ClaimedPlaintextLength" AND a."StagedCiphertextLength"=o."CiphertextLength"
               AND a."StagedCiphertextDigest"=o."CiphertextDigest"
               AND a."StagedProviderReceiptDigest"=o."ProviderReceiptDigest"
               AND a."StagedVerificationEvidenceDigest"=o."VerificationEvidenceDigest"
               AND h."CustodyState"='Staged' AND h."ReservationRevision"=a."StagedFromReservationRevision"+1 THEN
              RETURN QUERY SELECT 'ExistingMatch'::text,a."SourceArtifactId",a."AttemptId",a."StagedObjectCustodyId",2,a."StagedCiphertextFingerprint",h."ReservationRevision",a."Fence",a."StagedAtUtc"; RETURN;
            END IF;
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
          END IF;

          IF h."CustodyState"<>'Reserved' OR h."ReservationRevision"<>p_expected_reservation_revision
             OR a."EncryptionAttemptRevision"<>p_expected_encryption_attempt_revision OR a."Fence"<>p_expected_fence
             OR o."StateRevision"<>p_expected_object_state_revision
             OR a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL
           OR k."PreparationDisposition"<>'Active' OR o."State"<>'VerifiedCompleted'
             OR o."CiphertextLength" IS NULL OR o."CiphertextDigest" IS NULL
             OR o."ProviderReceiptDigest" IS NULL OR o."VerificationEvidenceDigest" IS NULL
             OR r."ClaimedPlaintextLength" IS NULL OR r."ContentCommitment" IS NULL
             OR o."CiphertextLength" NOT BETWEEN 1 AND 134217728 OR pg_catalog.octet_length(o."CiphertextDigest")<>32
             OR pg_catalog.octet_length(o."ProviderReceiptDigest")<>32 OR pg_catalog.octet_length(o."VerificationEvidenceDigest")<>32
             OR o."VerifiedAtUtc" IS NULL OR r."ClaimedPlaintextLength"<1 OR pg_catalog.octet_length(r."ContentCommitment")<>32 THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
          END IF;

          PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext(
            'tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
          SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(
            c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
          stage_time:=pg_catalog.clock_timestamp();
          SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(
            c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",stage_time);
          IF NOT FOUND
             OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion"
             OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
             OR authority."ControllerIdentity"<>r."ControllerIdentity"
             OR authority."StableDataScopeId"<>r."StableDataScopeId"
             OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
             OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
             OR authority."ApprovedPurpose"<>'SubjectRawBiometricExport'
             OR consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
             OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
             OR consent."PurposeCode"<>'SubjectRawBiometricExport'
             OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
             OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">stage_time
             OR (consent."ValidUntilUtc" IS NOT NULL AND stage_time>=consent."ValidUntilUtc")
             OR stage_time>=r."ReservationExpiresAtUtc" OR stage_time>=r."AbsoluteSourceExpiresAtUtc" THEN
            RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::integer,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN;
          END IF;

          fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-staged-ciphertext-v2',VARIADIC ARRAY[
            pg_catalog.encode(a."EncryptionAttemptFingerprint",'hex'),pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),
            pg_catalog.encode(o."ObjectBindingDigest",'hex'),r."ClaimedPlaintextLength"::text,pg_catalog.encode(r."ContentCommitment",'hex'),
            o."CiphertextLength"::text,pg_catalog.encode(o."CiphertextDigest",'hex'),pg_catalog.encode(o."ProviderReceiptDigest",'hex'),
            pg_catalog.encode(o."VerificationEvidenceDigest",'hex')]);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r3-stage-v1',true);
          UPDATE tagekyc.raw_export_source_encryption_attempts AS attempt SET
            "StagedCiphertextFingerprintSchemaVersion"=2,"StagedCiphertextFingerprint"=fingerprint,
            "StagedObjectCustodyId"=o."ObjectCustodyId","StagedObjectStateRevision"=o."StateRevision",
            "StagedFromReservationRevision"=h."ReservationRevision","VerifiedPlaintextLength"=r."ClaimedPlaintextLength",
            "StagedCiphertextLength"=o."CiphertextLength","StagedCiphertextDigest"=o."CiphertextDigest",
            "StagedProviderReceiptDigest"=o."ProviderReceiptDigest",
            "StagedVerificationEvidenceDigest"=o."VerificationEvidenceDigest","StagedAtUtc"=stage_time
          WHERE attempt."AttemptId"=a."AttemptId";
          UPDATE tagekyc.raw_export_source_head AS head
          SET "CustodyState"='Staged',"ReservationRevision"=head."ReservationRevision"+1
          WHERE head."SourceArtifactId"=h."SourceArtifactId" AND head."CustodyState"='Reserved'
            AND head."ReservationRevision"=p_expected_reservation_revision
            AND head."CurrentEncryptionAttemptId"=a."AttemptId" AND head."Fence"=a."Fence";
          IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_R3_STAGE_CAS_FAILED'; END IF;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
          RETURN QUERY SELECT 'Staged'::text,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",2,fingerprint,h."ReservationRevision"+1,a."Fence",stage_time;
        EXCEPTION WHEN OTHERS THEN
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
          RAISE;
        END $stage$;

        ALTER FUNCTION tagekyc.enforce_raw_export_source_core_write() OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.enforce_raw_export_source_head_write() OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint) TO tagekyc_raw_export_reconciler;
        """;

    private const string DownPreflightSql = """
        DO $down$
        BEGIN
          IF EXISTS (SELECT 1 FROM tagekyc.raw_export_source_head WHERE "CustodyState"='Staged')
             OR EXISTS (SELECT 1 FROM tagekyc.raw_export_source_encryption_attempts WHERE
               "StagedCiphertextFingerprintSchemaVersion" IS NOT NULL OR "StagedCiphertextFingerprint" IS NOT NULL
               OR "StagedObjectCustodyId" IS NOT NULL OR "StagedObjectStateRevision" IS NOT NULL
               OR "StagedFromReservationRevision" IS NOT NULL OR "VerifiedPlaintextLength" IS NOT NULL
               OR "StagedCiphertextLength" IS NOT NULL OR "StagedCiphertextDigest" IS NOT NULL
               OR "StagedProviderReceiptDigest" IS NOT NULL OR "StagedVerificationEvidenceDigest" IS NOT NULL OR "StagedAtUtc" IS NOT NULL) THEN
            RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R3_DOWN_OCCUPIED';
          END IF;
        END $down$;
        """;

    private const string DownRestoreSql = """
        REVOKE ALL ON FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint) FROM tagekyc_raw_export_reconciler;
        DROP FUNCTION tagekyc.raw_export_stage_verified_source_ciphertext(uuid,uuid,bigint,bigint,bigint,bigint);

        CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        BEGIN
          IF TG_OP='INSERT' THEN
            IF current_user<>'tagekyc_raw_export_deployer'
               OR pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)<>'complete-r1' THEN
              RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
            END IF;
            RETURN NEW;
          END IF;
        IF TG_OP='UPDATE' AND TG_TABLE_NAME='raw_export_source_encryption_attempts' THEN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r2-termination-v1'
             AND OLD."R2TerminationDisposition" IS NULL AND OLD."R2TerminatedAtUtc" IS NULL
             AND NEW."R2TerminationDisposition" IN ('Terminated','TerminatedBeforeStart')
             AND NEW."R2TerminatedAtUtc" IS NOT NULL
             AND ROW(NEW."AttemptId",NEW."SourceArtifactId",NEW."EncryptionAttemptRevision",NEW."Fence",
                 NEW."ProvisionalObjectIdentity",NEW."AttemptKeyReservationId",NEW."KeyProviderId",NEW."KekId",
                 NEW."KekVersion",NEW."KekFingerprint",NEW."EncryptionSuiteId",NEW."EncryptionFramingVersion",
                 NEW."NonceStrategyId",NEW."NonceDerivationSeedReferenceOrWrappedSeed",NEW."NonceDerivationSeedCommitment",
                 NEW."ChunkSize",NEW."FramingParametersDigest",NEW."EncryptionAttemptFingerprint",
                 NEW."OwnershipLeaseExpiresAtUtc",NEW."CreatedAtUtc",NEW."SchemaVersion")
                 IS NOT DISTINCT FROM
                 ROW(OLD."AttemptId",OLD."SourceArtifactId",OLD."EncryptionAttemptRevision",OLD."Fence",
                 OLD."ProvisionalObjectIdentity",OLD."AttemptKeyReservationId",OLD."KeyProviderId",OLD."KekId",
                 OLD."KekVersion",OLD."KekFingerprint",OLD."EncryptionSuiteId",OLD."EncryptionFramingVersion",
                 OLD."NonceStrategyId",OLD."NonceDerivationSeedReferenceOrWrappedSeed",OLD."NonceDerivationSeedCommitment",
                 OLD."ChunkSize",OLD."FramingParametersDigest",OLD."EncryptionAttemptFingerprint",
                 OLD."OwnershipLeaseExpiresAtUtc",OLD."CreatedAtUtc",OLD."SchemaVersion") THEN
            RETURN NEW;
          END IF;
        END IF;
          IF TG_OP='UPDATE' THEN
            RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
          END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
        END $guard$;
        CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_head_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        BEGIN
            IF current_user <> 'tagekyc_raw_export_deployer'
               OR pg_catalog.current_setting(
                    'tagekyc.raw_export_source_core_write_context',
                    true) <> 'complete-r1'
               OR TG_OP = 'DELETE' THEN
                RAISE EXCEPTION
                    'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
            END IF;
            RETURN NEW;
        END;
        $guard$;
        ALTER FUNCTION tagekyc.enforce_raw_export_source_core_write() OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.enforce_raw_export_source_head_write() OWNER TO tagekyc_raw_export_deployer;
        """;
}
