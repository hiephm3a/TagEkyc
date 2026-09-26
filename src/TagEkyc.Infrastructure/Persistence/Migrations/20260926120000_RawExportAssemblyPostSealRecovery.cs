using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TagEkycDbContext))]
[Migration("20260926120000_RawExportAssemblyPostSealRecovery")]
public sealed class RawExportAssemblyPostSealRecovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE tagekyc.raw_export_assembly_post_seal_recovery_claims (
              "C2PreparationId" uuid NOT NULL,
              "JobId" uuid NOT NULL,
              "ClaimOwnerId" uuid NULL,
              "ClaimGeneration" bigint NOT NULL,
              "ClaimExpiresAtUtc" timestamptz NULL,
              "FailureCount" integer NOT NULL,
              "RetryNotBeforeUtc" timestamptz NULL,
              "LastOutcome" character varying(64) NULL,
              "CreatedAtUtc" timestamptz NOT NULL,
              "UpdatedAtUtc" timestamptz NOT NULL,
              "CompletedAtUtc" timestamptz NULL,
              "SchemaVersion" integer NOT NULL,
              CONSTRAINT "raw_export_assembly_post_seal_recovery_claims_pkey"
                PRIMARY KEY ("C2PreparationId"),
              CONSTRAINT "raw_export_assembly_post_seal_recovery_claims_job_key"
                UNIQUE ("JobId"),
              CONSTRAINT "fk_raw_export_post_seal_recovery_preparation"
                FOREIGN KEY ("C2PreparationId")
                REFERENCES tagekyc.raw_export_assembly_preparation_dispositions("C2PreparationId")
                ON DELETE RESTRICT,
              CONSTRAINT "fk_raw_export_post_seal_recovery_job"
                FOREIGN KEY ("JobId")
                REFERENCES tagekyc.raw_export_job_identities("JobId")
                ON DELETE RESTRICT,
              CONSTRAINT "ck_raw_export_post_seal_recovery_shape" CHECK (
                "ClaimGeneration" >= 1 AND "FailureCount" >= 0 AND "SchemaVersion" = 1
                AND (("ClaimOwnerId" IS NULL AND "ClaimExpiresAtUtc" IS NULL)
                  OR ("ClaimOwnerId" IS NOT NULL AND "ClaimExpiresAtUtc" IS NOT NULL))
                AND ("LastOutcome" IS NULL OR pg_catalog.length("LastOutcome") BETWEEN 1 AND 64)
                AND ("CompletedAtUtc" IS NULL OR ("ClaimOwnerId" IS NULL AND "ClaimExpiresAtUtc" IS NULL)))
            );
            CREATE INDEX "ix_raw_export_post_seal_recovery_eligibility"
              ON tagekyc.raw_export_assembly_post_seal_recovery_claims
              ("CompletedAtUtc","RetryNotBeforeUtc","ClaimExpiresAtUtc","UpdatedAtUtc");
            CREATE INDEX "ix_raw_export_preparation_post_seal_discovery"
              ON tagekyc.raw_export_assembly_preparation_dispositions
              ("Disposition","SealCommittedAtUtc","JobId");

            ALTER TABLE tagekyc.raw_export_assembly_post_seal_recovery_claims
              OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON TABLE tagekyc.raw_export_assembly_post_seal_recovery_claims
              FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_assembly_resolver,tagekyc_raw_export_assembly_sealer;

            CREATE FUNCTION tagekyc.enforce_raw_export_post_seal_recovery_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            SET search_path=pg_catalog
            AS $function$
            BEGIN
              IF current_user <> 'tagekyc_raw_export_deployer'
                 OR COALESCE(
                    pg_catalog.current_setting('tagekyc.raw_export_post_seal_recovery_context',true),'')
                    NOT IN ('claim','defer','complete')
              THEN
                RAISE EXCEPTION 'RAW_EXPORT_POST_SEAL_RECOVERY_MUTATION_UNSUPPORTED';
              END IF;
              RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
            END
            $function$;
            ALTER FUNCTION tagekyc.enforce_raw_export_post_seal_recovery_mutation()
              OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.enforce_raw_export_post_seal_recovery_mutation()
              FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_assembly_resolver,tagekyc_raw_export_assembly_sealer;
            CREATE TRIGGER tr_raw_export_post_seal_recovery_mutation
              BEFORE INSERT OR UPDATE OR DELETE
              ON tagekyc.raw_export_assembly_post_seal_recovery_claims
              FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_post_seal_recovery_mutation();

            CREATE FUNCTION tagekyc.raw_export_claim_next_post_seal_recovery(
              p_claim_owner_id uuid,p_lease_seconds integer)
            RETURNS TABLE(
              "Outcome" text,"C2PreparationId" uuid,"AssemblyId" uuid,"JobId" uuid,
              "AttemptId" uuid,"FencingToken" bigint,"ActorPrincipalId" uuid,
              "JobRevision" bigint,"PreparationRevision" bigint,"ClaimGeneration" bigint,
              "AssemblyFingerprint" bytea,"PreparationFingerprint" bytea,
              "AssemblyDigest" bytea,"ManifestDigest" bytea,"AssemblyAuthenticationValue" bytea,
              "ExportMode" text,"JobExpiresAtUtc" timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE
              candidate record; claim_ tagekyc.raw_export_assembly_post_seal_recovery_claims%ROWTYPE;
              now_ timestamptz; previous_context text;
            BEGIN
              IF p_claim_owner_id IS NULL OR p_claim_owner_id='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_lease_seconds NOT BETWEEN 1 AND 3600
              THEN RAISE EXCEPTION 'RAW_EXPORT_POST_SEAL_RECOVERY_CLAIM_INVALID'; END IF;
              now_:=pg_catalog.clock_timestamp();
              SELECT p."C2PreparationId",p."AssemblyId",p."JobId",p."AttemptId",p."FencingToken",
                     p."AssemblyFingerprint",p."PreparationFingerprint",p."RowRevision",
                     i."PrincipalId",i."ExportMode",i."JobExpiresAt",h."Revision",
                     a."AssemblyDigest",a."ManifestDigest",a."AssemblyAuthenticationValue"
              INTO candidate
              FROM tagekyc.raw_export_assembly_preparation_dispositions p
              JOIN tagekyc.raw_export_assembly_identities a
                ON a."C2PreparationId"=p."C2PreparationId" AND a."AssemblyId"=p."AssemblyId"
               AND a."JobId"=p."JobId" AND a."AttemptId"=p."AttemptId"
               AND a."FencingToken"=p."FencingToken" AND a."AssemblyFingerprint"=p."AssemblyFingerprint"
              JOIN tagekyc.raw_export_job_identities i ON i."JobId"=p."JobId"
              JOIN tagekyc.raw_export_job_operational_heads h ON h."JobId"=p."JobId"
              LEFT JOIN tagekyc.raw_export_assembly_post_seal_recovery_claims c
                ON c."C2PreparationId"=p."C2PreparationId"
              WHERE p."Disposition"='SealCommitted'
                AND i."ExportMode" IN ('EncryptedExportPacket','EncryptedRawVaultRetained')
                AND h."CurrentState"='AssemblySealed' AND h."CurrentAttemptId"=p."AttemptId"
                AND h."FencingToken"=p."FencingToken"
                AND EXISTS (
                  SELECT 1 FROM tagekyc.raw_export_job_transitions t
                  WHERE t."JobId"=p."JobId" AND t."ResultingRevision"=h."Revision"
                    AND t."EventType"='AssemblySealed' AND t."FromState"='Assembling'
                    AND t."ToState"='AssemblySealed' AND t."AttemptId"=p."AttemptId"
                    AND t."FencingToken"=p."FencingToken")
                AND a."ItemCount"=(SELECT pg_catalog.count(*) FROM tagekyc.raw_export_assembly_items x
                                   WHERE x."AssemblyId"=a."AssemblyId")
                AND (c."C2PreparationId" IS NULL OR (
                  c."CompletedAtUtc" IS NULL
                  AND (c."RetryNotBeforeUtc" IS NULL OR c."RetryNotBeforeUtc"<=now_)
                  AND (c."ClaimOwnerId" IS NULL OR c."ClaimExpiresAtUtc"<=now_)))
              ORDER BY COALESCE(c."RetryNotBeforeUtc",p."SealCommittedAtUtc"),p."SealCommittedAtUtc",p."JobId"
              FOR UPDATE OF p SKIP LOCKED
              LIMIT 1;
              IF NOT FOUND THEN RETURN; END IF;

              previous_context:=pg_catalog.current_setting('tagekyc.raw_export_post_seal_recovery_context',true);
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context','claim',true);
              INSERT INTO tagekyc.raw_export_assembly_post_seal_recovery_claims(
                "C2PreparationId","JobId","ClaimOwnerId","ClaimGeneration","ClaimExpiresAtUtc",
                "FailureCount","RetryNotBeforeUtc","LastOutcome","CreatedAtUtc","UpdatedAtUtc","CompletedAtUtc","SchemaVersion")
              VALUES(candidate."C2PreparationId",candidate."JobId",p_claim_owner_id,1,
                     now_+pg_catalog.make_interval(secs=>p_lease_seconds),0,NULL,NULL,now_,now_,NULL,1)
              ON CONFLICT ON CONSTRAINT "raw_export_assembly_post_seal_recovery_claims_pkey" DO UPDATE SET
                "ClaimOwnerId"=p_claim_owner_id,
                "ClaimGeneration"=tagekyc.raw_export_assembly_post_seal_recovery_claims."ClaimGeneration"+1,
                "ClaimExpiresAtUtc"=now_+pg_catalog.make_interval(secs=>p_lease_seconds),
                "RetryNotBeforeUtc"=NULL,"UpdatedAtUtc"=now_
              RETURNING * INTO claim_;
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RETURN QUERY SELECT 'Claimed'::text,candidate."C2PreparationId",candidate."AssemblyId",
                candidate."JobId",candidate."AttemptId",candidate."FencingToken",candidate."PrincipalId",
                candidate."Revision",candidate."RowRevision",claim_."ClaimGeneration",
                candidate."AssemblyFingerprint",candidate."PreparationFingerprint",candidate."AssemblyDigest",
                candidate."ManifestDigest",candidate."AssemblyAuthenticationValue",candidate."ExportMode"::text,
                candidate."JobExpiresAt";
            EXCEPTION WHEN OTHERS THEN
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RAISE;
            END
            $function$;

            CREATE FUNCTION tagekyc.raw_export_claim_exact_post_seal_recovery(
              p_job_id uuid,p_attempt_id uuid,p_expected_job_revision bigint,p_expected_fence bigint,
              p_claim_owner_id uuid,p_lease_seconds integer,p_assembly_digest bytea,p_manifest_digest bytea,
              p_authentication_value bytea,p_assembly_fingerprint bytea)
            RETURNS TABLE(
              "Outcome" text,"C2PreparationId" uuid,"AssemblyId" uuid,"JobId" uuid,
              "AttemptId" uuid,"FencingToken" bigint,"ActorPrincipalId" uuid,
              "JobRevision" bigint,"PreparationRevision" bigint,"ClaimGeneration" bigint,
              "AssemblyFingerprint" bytea,"PreparationFingerprint" bytea,
              "AssemblyDigest" bytea,"ManifestDigest" bytea,"AssemblyAuthenticationValue" bytea,
              "ExportMode" text,"JobExpiresAtUtc" timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE
              candidate record; claim_ tagekyc.raw_export_assembly_post_seal_recovery_claims%ROWTYPE;
              now_ timestamptz; previous_context text; outcome_ text; claim_found boolean;
            BEGIN
              IF p_claim_owner_id IS NULL OR p_claim_owner_id='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_lease_seconds NOT BETWEEN 1 AND 3600
              THEN RAISE EXCEPTION 'RAW_EXPORT_POST_SEAL_RECOVERY_CLAIM_INVALID'; END IF;
              now_:=pg_catalog.clock_timestamp();
              SELECT p."C2PreparationId",p."AssemblyId",p."JobId",p."AttemptId",p."FencingToken",
                     p."AssemblyFingerprint",p."PreparationFingerprint",p."Disposition",p."RowRevision",
                     i."PrincipalId",i."ExportMode",i."JobExpiresAt",h."Revision",
                     a."AssemblyDigest",a."ManifestDigest",a."AssemblyAuthenticationValue"
              INTO candidate
              FROM tagekyc.raw_export_assembly_preparation_dispositions p
              JOIN tagekyc.raw_export_assembly_identities a
                ON a."C2PreparationId"=p."C2PreparationId" AND a."AssemblyId"=p."AssemblyId"
               AND a."JobId"=p."JobId" AND a."AttemptId"=p."AttemptId"
               AND a."FencingToken"=p."FencingToken" AND a."AssemblyFingerprint"=p."AssemblyFingerprint"
              JOIN tagekyc.raw_export_job_identities i ON i."JobId"=p."JobId"
              JOIN tagekyc.raw_export_job_operational_heads h ON h."JobId"=p."JobId"
              WHERE p."JobId"=p_job_id
              FOR UPDATE OF p;
              IF NOT FOUND OR candidate."Disposition" NOT IN ('SealCommitted','Finalized')
              THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,
                NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bytea,NULL::bytea,NULL::bytea,NULL::bytea,
                NULL::bytea,NULL::text,NULL::timestamptz; RETURN; END IF;
              IF candidate."AttemptId"<>p_attempt_id OR candidate."FencingToken"<>p_expected_fence
                 OR candidate."Revision"<>p_expected_job_revision+1
                 OR candidate."ExportMode" NOT IN (
                      'ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained')
                 OR NOT EXISTS (SELECT 1 FROM tagekyc.raw_export_job_transitions t
                    WHERE t."JobId"=p_job_id AND t."ResultingRevision"=p_expected_job_revision+1
                      AND t."EventType"='AssemblySealed' AND t."FromState"='Assembling'
                      AND t."ToState"='AssemblySealed' AND t."AttemptId"=p_attempt_id
                      AND t."FencingToken"=p_expected_fence)
                 OR (p_assembly_digest IS NOT NULL AND candidate."AssemblyDigest"<>p_assembly_digest)
                 OR (p_manifest_digest IS NOT NULL AND candidate."ManifestDigest"<>p_manifest_digest)
                 OR (p_authentication_value IS NOT NULL AND candidate."AssemblyAuthenticationValue"<>p_authentication_value)
                 OR (p_assembly_fingerprint IS NOT NULL AND candidate."AssemblyFingerprint"<>p_assembly_fingerprint)
              THEN outcome_:='ExactMismatch';
              ELSIF candidate."Disposition"='Finalized' THEN outcome_:='Completed';
              ELSE
                SELECT c.* INTO claim_
                FROM tagekyc.raw_export_assembly_post_seal_recovery_claims AS c
                WHERE c."C2PreparationId"=candidate."C2PreparationId" FOR UPDATE;
                claim_found:=FOUND;
                IF claim_found AND claim_."CompletedAtUtc" IS NOT NULL THEN outcome_:='Completed';
                ELSIF claim_found AND claim_."ClaimOwnerId" IS DISTINCT FROM p_claim_owner_id
                      AND claim_."ClaimOwnerId" IS NOT NULL AND claim_."ClaimExpiresAtUtc">now_
                THEN outcome_:='ClaimHeld';
                ELSIF claim_found AND claim_."ClaimOwnerId" IS NULL
                      AND claim_."RetryNotBeforeUtc" IS NOT NULL AND claim_."RetryNotBeforeUtc">now_
                THEN outcome_:='Deferred';
                ELSE
                  previous_context:=pg_catalog.current_setting('tagekyc.raw_export_post_seal_recovery_context',true);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context','claim',true);
                  IF NOT claim_found THEN
                    INSERT INTO tagekyc.raw_export_assembly_post_seal_recovery_claims(
                      "C2PreparationId","JobId","ClaimOwnerId","ClaimGeneration","ClaimExpiresAtUtc",
                      "FailureCount","RetryNotBeforeUtc","LastOutcome","CreatedAtUtc","UpdatedAtUtc","CompletedAtUtc","SchemaVersion")
                    VALUES(candidate."C2PreparationId",candidate."JobId",p_claim_owner_id,1,
                      now_+pg_catalog.make_interval(secs=>p_lease_seconds),0,NULL,NULL,now_,now_,NULL,1)
                    RETURNING * INTO claim_;
                    outcome_:='Claimed';
                  ELSIF claim_."ClaimOwnerId"=p_claim_owner_id AND claim_."ClaimExpiresAtUtc">now_ THEN
                    UPDATE tagekyc.raw_export_assembly_post_seal_recovery_claims AS c SET
                      "ClaimExpiresAtUtc"=now_+pg_catalog.make_interval(secs=>p_lease_seconds),"UpdatedAtUtc"=now_
                    WHERE c."C2PreparationId"=candidate."C2PreparationId" RETURNING c.* INTO claim_;
                    outcome_:='Owned';
                  ELSE
                    UPDATE tagekyc.raw_export_assembly_post_seal_recovery_claims AS c SET
                      "ClaimOwnerId"=p_claim_owner_id,"ClaimGeneration"=c."ClaimGeneration"+1,
                      "ClaimExpiresAtUtc"=now_+pg_catalog.make_interval(secs=>p_lease_seconds),
                      "RetryNotBeforeUtc"=NULL,"UpdatedAtUtc"=now_
                    WHERE c."C2PreparationId"=candidate."C2PreparationId" RETURNING c.* INTO claim_;
                    outcome_:='Claimed';
                  END IF;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
                END IF;
              END IF;
              RETURN QUERY SELECT outcome_,candidate."C2PreparationId",candidate."AssemblyId",candidate."JobId",
                candidate."AttemptId",candidate."FencingToken",candidate."PrincipalId",candidate."Revision",
                candidate."RowRevision",claim_."ClaimGeneration",candidate."AssemblyFingerprint",
                candidate."PreparationFingerprint",candidate."AssemblyDigest",candidate."ManifestDigest",
                candidate."AssemblyAuthenticationValue",candidate."ExportMode"::text,candidate."JobExpiresAt";
            EXCEPTION WHEN OTHERS THEN
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RAISE;
            END
            $function$;

            CREATE FUNCTION tagekyc.raw_export_defer_post_seal_recovery(
              p_c2_preparation_id uuid,p_claim_owner_id uuid,p_claim_generation bigint,
              p_outcome text,p_retry_base_seconds integer)
            RETURNS TABLE("Outcome" text,"ClaimGeneration" bigint,"RetryNotBeforeUtc" timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE
              prep tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE;
              claim_ tagekyc.raw_export_assembly_post_seal_recovery_claims%ROWTYPE;
              now_ timestamptz; retry_ timestamptz; previous_context text;
            BEGIN
              IF p_retry_base_seconds NOT BETWEEN 1 AND 60 OR p_outcome IS NULL
                 OR pg_catalog.length(p_outcome) NOT BETWEEN 1 AND 64
              THEN RAISE EXCEPTION 'RAW_EXPORT_POST_SEAL_RECOVERY_DEFER_INVALID'; END IF;
              now_:=pg_catalog.clock_timestamp();
              SELECT p.* INTO prep FROM tagekyc.raw_export_assembly_preparation_dispositions AS p
                WHERE p."C2PreparationId"=p_c2_preparation_id FOR UPDATE;
              SELECT c.* INTO claim_ FROM tagekyc.raw_export_assembly_post_seal_recovery_claims AS c
                WHERE c."C2PreparationId"=p_c2_preparation_id FOR UPDATE;
              IF prep."Disposition"='Finalized' THEN
                RETURN QUERY SELECT 'Completed'::text,claim_."ClaimGeneration",NULL::timestamptz; RETURN;
              END IF;
              IF prep."Disposition" IS DISTINCT FROM 'SealCommitted' THEN
                RETURN QUERY SELECT 'NotPostSeal'::text,NULL::bigint,NULL::timestamptz; RETURN;
              END IF;
              IF claim_."C2PreparationId" IS NULL OR claim_."ClaimOwnerId" IS DISTINCT FROM p_claim_owner_id
                 OR claim_."ClaimGeneration"<>p_claim_generation OR claim_."ClaimExpiresAtUtc"<=now_
              THEN RETURN QUERY SELECT 'ClaimLost'::text,claim_."ClaimGeneration",claim_."RetryNotBeforeUtc"; RETURN; END IF;
              retry_:=now_+pg_catalog.make_interval(secs=>LEAST(
                300,p_retry_base_seconds*(1::integer << LEAST(claim_."FailureCount",5))));
              previous_context:=pg_catalog.current_setting('tagekyc.raw_export_post_seal_recovery_context',true);
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context','defer',true);
              UPDATE tagekyc.raw_export_assembly_post_seal_recovery_claims AS c SET
                "ClaimOwnerId"=NULL,"ClaimExpiresAtUtc"=NULL,"FailureCount"=c."FailureCount"+1,
                "RetryNotBeforeUtc"=retry_,"LastOutcome"=p_outcome,"UpdatedAtUtc"=now_
              WHERE c."C2PreparationId"=p_c2_preparation_id;
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RETURN QUERY SELECT 'Deferred'::text,p_claim_generation,retry_;
            EXCEPTION WHEN OTHERS THEN
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RAISE;
            END
            $function$;

            CREATE FUNCTION tagekyc.raw_export_record_claimed_assembly_finalized(
              p_c2_preparation_id uuid,p_expected_row_revision bigint,p_assembly_fingerprint bytea,
              p_claim_owner_id uuid,p_claim_generation bigint)
            RETURNS TABLE("Outcome" text,"RowRevision" bigint)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE
              prep tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE;
              claim_ tagekyc.raw_export_assembly_post_seal_recovery_claims%ROWTYPE;
              now_ timestamptz; previous_context text;
            BEGIN
              now_:=pg_catalog.clock_timestamp();
              SELECT p.* INTO prep FROM tagekyc.raw_export_assembly_preparation_dispositions AS p
                WHERE p."C2PreparationId"=p_c2_preparation_id FOR UPDATE;
              SELECT c.* INTO claim_ FROM tagekyc.raw_export_assembly_post_seal_recovery_claims AS c
                WHERE c."C2PreparationId"=p_c2_preparation_id FOR UPDATE;
              IF prep."Disposition"='Finalized' AND prep."AssemblyFingerprint"=p_assembly_fingerprint
              THEN RETURN QUERY SELECT 'ExistingMatch'::text,prep."RowRevision"; RETURN; END IF;
              IF prep."Disposition" IS DISTINCT FROM 'SealCommitted' OR prep."RowRevision"<>p_expected_row_revision
                 OR prep."AssemblyFingerprint"<>p_assembly_fingerprint
              THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::bigint; RETURN; END IF;
              IF claim_."C2PreparationId" IS NULL OR claim_."ClaimOwnerId" IS DISTINCT FROM p_claim_owner_id
                 OR claim_."ClaimGeneration"<>p_claim_generation OR claim_."ClaimExpiresAtUtc"<=now_
              THEN RETURN QUERY SELECT 'ClaimLost'::text,NULL::bigint; RETURN; END IF;
              UPDATE tagekyc.raw_export_assembly_preparation_dispositions AS p SET
                "Disposition"='Finalized',"FinalizedAtUtc"=now_,"RowRevision"=p."RowRevision"+1
              WHERE p."C2PreparationId"=p_c2_preparation_id;
              previous_context:=pg_catalog.current_setting('tagekyc.raw_export_post_seal_recovery_context',true);
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context','complete',true);
              UPDATE tagekyc.raw_export_assembly_post_seal_recovery_claims AS c SET
                "ClaimOwnerId"=NULL,"ClaimExpiresAtUtc"=NULL,"RetryNotBeforeUtc"=NULL,
                "LastOutcome"='Finalized',"UpdatedAtUtc"=now_,"CompletedAtUtc"=now_
              WHERE c."C2PreparationId"=p_c2_preparation_id;
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RETURN QUERY SELECT 'Finalized'::text,p_expected_row_revision+1;
            EXCEPTION WHEN OTHERS THEN
              PERFORM pg_catalog.set_config('tagekyc.raw_export_post_seal_recovery_context',COALESCE(previous_context,''),true);
              RAISE;
            END
            $function$;

            ALTER FUNCTION tagekyc.raw_export_claim_next_post_seal_recovery(uuid,integer)
              OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_claim_exact_post_seal_recovery(uuid,uuid,bigint,bigint,uuid,integer,bytea,bytea,bytea,bytea)
              OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_defer_post_seal_recovery(uuid,uuid,bigint,text,integer)
              OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_record_claimed_assembly_finalized(uuid,bigint,bytea,uuid,bigint)
              OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION
              tagekyc.raw_export_claim_next_post_seal_recovery(uuid,integer),
              tagekyc.raw_export_claim_exact_post_seal_recovery(uuid,uuid,bigint,bigint,uuid,integer,bytea,bytea,bytea,bytea),
              tagekyc.raw_export_defer_post_seal_recovery(uuid,uuid,bigint,text,integer),
              tagekyc.raw_export_record_claimed_assembly_finalized(uuid,bigint,bytea,uuid,bigint)
            FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_assembly_resolver,tagekyc_raw_export_assembly_sealer;
            GRANT EXECUTE ON FUNCTION
              tagekyc.raw_export_claim_next_post_seal_recovery(uuid,integer),
              tagekyc.raw_export_claim_exact_post_seal_recovery(uuid,uuid,bigint,bigint,uuid,integer,bytea,bytea,bytea,bytea),
              tagekyc.raw_export_defer_post_seal_recovery(uuid,uuid,bigint,text,integer),
              tagekyc.raw_export_record_claimed_assembly_finalized(uuid,bigint,bytea,uuid,bigint)
            TO tagekyc_raw_export_assembly_sealer;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS tagekyc.raw_export_record_claimed_assembly_finalized(uuid,bigint,bytea,uuid,bigint);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_defer_post_seal_recovery(uuid,uuid,bigint,text,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_claim_exact_post_seal_recovery(uuid,uuid,bigint,bigint,uuid,integer,bytea,bytea,bytea,bytea);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_claim_next_post_seal_recovery(uuid,integer);
            DROP TRIGGER IF EXISTS tr_raw_export_post_seal_recovery_mutation
              ON tagekyc.raw_export_assembly_post_seal_recovery_claims;
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_post_seal_recovery_mutation();
            DROP INDEX IF EXISTS tagekyc."ix_raw_export_preparation_post_seal_discovery";
            DROP TABLE IF EXISTS tagekyc.raw_export_assembly_post_seal_recovery_claims;
            """);
    }
}
