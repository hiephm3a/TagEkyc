using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2R2DurableCustodyEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_attempt_values",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "R2TerminatedAtUtc",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_attempt_values",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                sql: "\"SchemaVersion\" = 1\nAND \"EncryptionAttemptRevision\" >= 1\nAND \"Fence\" >= 1\nAND \"KekVersion\" >= 1\nAND \"EncryptionFramingVersion\" >= 1\nAND \"ChunkSize\" >= 1\nAND octet_length(\"NonceDerivationSeedCommitment\") = 32\nAND octet_length(\"FramingParametersDigest\") = 32\nAND octet_length(\"EncryptionAttemptFingerprint\") = 32\nAND (\n  (\"R2TerminationDisposition\" IS NULL AND \"R2TerminatedAtUtc\" IS NULL)\n  OR\n  (\"R2TerminationDisposition\" IN ('Terminated','TerminatedBeforeStart')\n   AND \"R2TerminatedAtUtc\" IS NOT NULL)\n)");

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_provisional_object_reconcile_context(p_object_custody_id uuid)
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
                  RETURN QUERY SELECT p."ObjectCustodyId",p."AttemptId",p."AttemptKeyReservationId",p."SourceArtifactId",p."ProvisionalObjectIdentity",p."EncryptionAttemptRevision",p."AttemptFence",p."EncryptionAttemptFingerprint",p."ObjectKey"::text,p."ObjectBindingDigest",p."State"::text,p."StateRevision",p."PutOperationId",p."PutArmedAtUtc",p."CiphertextLength",p."CiphertextDigest",p."ProviderReceiptDigest",p."VerificationEvidenceDigest" FROM tagekyc.raw_export_provisional_objects p WHERE p."ObjectCustodyId"=p_object_custody_id;
                END $fn$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(p_object_custody_id uuid)
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
                  RETURN QUERY SELECT p."ObjectCustodyId",p."AttemptId",p."ProvisionalObjectIdentity",p."ObjectKey"::text,p."ObjectBindingDigest",p."State"::text,p."StateRevision",p."PutOperationId",p."CleanupReasonCode"::text,p."CleanupEvidenceDigest",p."CleanupRequestedAtUtc",p."ProviderReceiptDigest" FROM tagekyc.raw_export_provisional_objects p WHERE p."ObjectCustodyId"=p_object_custody_id;
                END $fn$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path=pg_catalog
                AS $fn$
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
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_read_source_encryption_context(
                  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint)
                RETURNS TABLE(
                  "AttemptId" uuid,"SourceArtifactId" uuid,"EncryptionAttemptRevision" bigint,"Fence" bigint,
                  "AttemptKeyReservationId" uuid,"ProvisionalObjectIdentity" uuid,"EncryptionAttemptFingerprint" bytea,
                  "KeyProviderId" text,"KekId" text,"KekVersion" integer,"KekFingerprint" text,
                  "EncryptionSuiteId" text,"EncryptionFramingVersion" integer,"ChunkSize" integer,"NonceStrategyId" text,
                  "NonceDerivationSeedReferenceOrWrappedSeed" text,"NonceDerivationSeedCommitment" bytea,
                  "FramingParametersDigest" bytea,"WrappedDekMetadataDigest" bytea,
                  "VerificationSessionId" uuid,"CaptureArtifactId" uuid,"CaptureRevision" integer,"RawClass" text,
                  "StableDataScopeId" text,"ControllerIdentity" text,"ClaimedPlaintextLength" bigint,"MediaType" text,
                  "ContentCommitmentSchemaVersion" integer,"ContentCommitmentKeyId" text,
                  "ContentCommitmentKeyVersion" integer,"ContentCommitment" bytea,
                  "OwnershipLeaseExpiresAtUtc" timestamptz,"EffectivePlaintextRetentionExpiresAtUtc" timestamptz,
                  "ReservationExpiresAtUtc" timestamptz)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE actor_id uuid; now_utc timestamptz:=pg_catalog.statement_timestamp();
                BEGIN
                  actor_id:=tagekyc.raw_export_current_actor();
                  RETURN QUERY
                  SELECT a."AttemptId",a."SourceArtifactId",a."EncryptionAttemptRevision",a."Fence",
                    a."AttemptKeyReservationId",a."ProvisionalObjectIdentity",a."EncryptionAttemptFingerprint",
                    a."KeyProviderId"::text,a."KekId"::text,a."KekVersion",a."KekFingerprint"::text,
                    a."EncryptionSuiteId"::text,a."EncryptionFramingVersion",a."ChunkSize",a."NonceStrategyId"::text,
                    a."NonceDerivationSeedReferenceOrWrappedSeed"::text,a."NonceDerivationSeedCommitment",
                    a."FramingParametersDigest",k."WrappedDekMetadataDigest",
                    c."VerificationSessionId",c."CaptureArtifactId",c."CaptureRevision",c."RawClass"::text,
                    r."StableDataScopeId"::text,r."ControllerIdentity"::text,r."ClaimedPlaintextLength",r."MediaType"::text,
                    r."ContentCommitmentSchemaVersion",r."ContentCommitmentKeyId"::text,
                    r."ContentCommitmentKeyVersion",r."ContentCommitment",
                    a."OwnershipLeaseExpiresAtUtc",r."EffectivePlaintextRetentionExpiresAtUtc",r."ReservationExpiresAtUtc"
                  FROM tagekyc.raw_export_source_encryption_attempts a
                  JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
                    AND h."CurrentEncryptionAttemptId"=a."AttemptId" AND h."Fence"=a."Fence"
                  JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
                  JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
                  JOIN tagekyc.raw_export_attempt_key_reservations k
                    ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
                  WHERE a."AttemptId"=p_attempt_id AND a."EncryptionAttemptRevision"=p_expected_revision
                    AND a."Fence"=p_expected_fence AND a."R2TerminationDisposition" IS NULL
                    AND a."R2TerminatedAtUtc" IS NULL AND k."PreparationDisposition"='Active'
                    AND pg_catalog.octet_length(k."WrappedDekMetadataDigest")=32
                    AND a."OwnershipLeaseExpiresAtUtc">now_utc
                    AND r."EffectivePlaintextRetentionExpiresAtUtc">now_utc AND r."ReservationExpiresAtUtc">now_utc;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_read_source_verification_context(
                  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint)
                RETURNS TABLE(
                  "AttemptId" uuid,"SourceArtifactId" uuid,"EncryptionAttemptRevision" bigint,"Fence" bigint,
                  "AttemptKeyReservationId" uuid,"ProvisionalObjectIdentity" uuid,"EncryptionAttemptFingerprint" bytea,
                  "KeyProviderId" text,"KekId" text,"KekVersion" integer,"KekFingerprint" text,
                  "EncryptionSuiteId" text,"EncryptionFramingVersion" integer,"ChunkSize" integer,"NonceStrategyId" text,
                  "FramingParametersDigest" bytea,"WrappedDekMetadataDigest" bytea,
                  "VerificationSessionId" uuid,"CaptureArtifactId" uuid,"CaptureRevision" integer,"RawClass" text,
                  "StableDataScopeId" text,"ControllerIdentity" text,"ClaimedPlaintextLength" bigint,"MediaType" text,
                  "ContentCommitmentSchemaVersion" integer,"ContentCommitmentKeyId" text,
                  "ContentCommitmentKeyVersion" integer,"ContentCommitment" bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE actor_id uuid;
                BEGIN
                  actor_id:=tagekyc.raw_export_current_actor();
                  RETURN QUERY
                  SELECT a."AttemptId",a."SourceArtifactId",a."EncryptionAttemptRevision",a."Fence",
                    a."AttemptKeyReservationId",a."ProvisionalObjectIdentity",a."EncryptionAttemptFingerprint",
                    a."KeyProviderId"::text,a."KekId"::text,a."KekVersion",a."KekFingerprint"::text,
                    a."EncryptionSuiteId"::text,a."EncryptionFramingVersion",a."ChunkSize",a."NonceStrategyId"::text,
                    a."FramingParametersDigest",k."WrappedDekMetadataDigest",
                    c."VerificationSessionId",c."CaptureArtifactId",c."CaptureRevision",c."RawClass"::text,
                    r."StableDataScopeId"::text,r."ControllerIdentity"::text,r."ClaimedPlaintextLength",r."MediaType"::text,
                    r."ContentCommitmentSchemaVersion",r."ContentCommitmentKeyId"::text,
                    r."ContentCommitmentKeyVersion",r."ContentCommitment"
                  FROM tagekyc.raw_export_source_encryption_attempts a
                  JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
                    AND h."CurrentEncryptionAttemptId"=a."AttemptId" AND h."Fence"=a."Fence"
                  JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
                  JOIN tagekyc.raw_export_source_ingress_claims c ON c."IngressClaimId"=r."IngressClaimId"
                  JOIN tagekyc.raw_export_attempt_key_reservations k
                    ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId" AND k."AttemptId"=a."AttemptId"
                  WHERE a."AttemptId"=p_attempt_id AND a."EncryptionAttemptRevision"=p_expected_revision
                    AND a."Fence"=p_expected_fence AND a."R2TerminationDisposition" IS NULL
                    AND a."R2TerminatedAtUtc" IS NULL AND k."PreparationDisposition"='Active'
                    AND pg_catalog.octet_length(k."WrappedDekMetadataDigest")=32;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_terminate_source_encryption_attempt(
                  p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,p_disposition text)
                RETURNS text
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE a record; h record; k record; object_count integer; object_state text;
                  key_found boolean; actor_id uuid;
                  previous_context text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
                BEGIN
                  IF p_attempt_id IS NULL OR p_expected_revision<1 OR p_expected_fence<1
                     OR p_disposition NOT IN ('Terminated','TerminatedBeforeStart') THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R2_TERMINATION_ARGUMENT_INVALID';
                  END IF;
                  actor_id:=tagekyc.raw_export_current_actor();
                  SELECT * INTO a FROM tagekyc.raw_export_source_encryption_attempts
                    WHERE "AttemptId"=p_attempt_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN 'NotFound'; END IF;
                  IF a."EncryptionAttemptRevision"<>p_expected_revision OR a."Fence"<>p_expected_fence THEN
                    RETURN 'StateConflict';
                  END IF;
                  IF a."R2TerminationDisposition"=p_disposition AND a."R2TerminatedAtUtc" IS NOT NULL THEN
                    RETURN 'ExistingMatch';
                  END IF;
                  IF a."R2TerminationDisposition" IS NOT NULL OR a."R2TerminatedAtUtc" IS NOT NULL THEN
                    RETURN 'StateConflict';
                  END IF;
                  SELECT * INTO h FROM tagekyc.raw_export_source_head
                    WHERE "SourceArtifactId"=a."SourceArtifactId" AND "CurrentEncryptionAttemptId"=a."AttemptId"
                      AND "Fence"=a."Fence" FOR UPDATE;
                  IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
                  SELECT * INTO k FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=a."AttemptKeyReservationId" AND "AttemptId"=a."AttemptId" FOR UPDATE;
                  key_found:=FOUND;
                  SELECT po."State"::text
                    INTO object_state FROM tagekyc.raw_export_provisional_objects po
                    WHERE po."AttemptId"=a."AttemptId" FOR UPDATE;
                  object_count:=CASE WHEN FOUND THEN 1 ELSE 0 END;
                  IF p_disposition='TerminatedBeforeStart' THEN
                    IF NOT ((object_count=1 AND object_state='NoObjectEstablished')
                       OR (object_count=0 AND key_found AND k."PreparationDisposition" IN ('Revoked','ReservationAbandoned'))) THEN
                      RETURN 'StateConflict';
                    END IF;
                  ELSIF object_count<>1 OR object_state NOT IN ('Deleted','Quarantined') THEN
                    RETURN 'StateConflict';
                  END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r2-termination-v1',true);
                  UPDATE tagekyc.raw_export_source_encryption_attempts
                    SET "R2TerminationDisposition"=p_disposition,"R2TerminatedAtUtc"=pg_catalog.statement_timestamp()
                    WHERE "AttemptId"=p_attempt_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
                  RETURN 'Terminated';
                EXCEPTION WHEN OTHERS THEN
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(previous_context,''),true);
                  RAISE;
                END $fn$;

                ALTER FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)
                  OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint)
                  OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)
                  OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                  tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint),
                  tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint),
                  tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)
                FROM PUBLIC, tagekyc_runtime, tagekyc_raw_export_custody_encryptor,
                  tagekyc_raw_export_reconciler, tagekyc_raw_export_claim_broker;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)
                  TO tagekyc_raw_export_custody_encryptor;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint),
                  tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)
                  TO tagekyc_raw_export_reconciler;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                REVOKE ALL ON FUNCTION
                  tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint),
                  tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint),
                  tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)
                FROM PUBLIC, tagekyc_runtime, tagekyc_raw_export_custody_encryptor,
                  tagekyc_raw_export_reconciler, tagekyc_raw_export_claim_broker;
                DROP FUNCTION tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint);
                DROP FUNCTION tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint);
                DROP FUNCTION tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text);

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_provisional_object_reconcile_context(p_object_custody_id uuid)
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

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_read_provisional_object_lifecycle_context(p_object_custody_id uuid)
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

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_core_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                BEGIN
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR pg_catalog.current_setting(
                            'tagekyc.raw_export_source_core_write_context',
                            true) <> 'complete-r1' THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_SOURCE_CORE_WRITE_FORBIDDEN';
                    END IF;
                    IF TG_OP <> 'INSERT' THEN
                        RAISE EXCEPTION
                            'RAW_EXPORT_SOURCE_CORE_APPEND_ONLY';
                    END IF;
                    RETURN NEW;
                END;
                $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_attempt_values",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");

            migrationBuilder.DropColumn(
                name: "R2TerminatedAtUtc",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_attempt_values",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                sql: "\"SchemaVersion\" = 1\nAND \"EncryptionAttemptRevision\" >= 1\nAND \"Fence\" >= 1\nAND \"KekVersion\" >= 1\nAND \"EncryptionFramingVersion\" >= 1\nAND \"ChunkSize\" >= 1\nAND octet_length(\"NonceDerivationSeedCommitment\") = 32\nAND octet_length(\"FramingParametersDigest\") = 32\nAND octet_length(\"EncryptionAttemptFingerprint\") = 32\nAND \"R2TerminationDisposition\" IS NULL");
        }
    }
}
