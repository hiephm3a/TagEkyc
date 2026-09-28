using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TagEkycDbContext))]
[Migration("20260927120000_SiteQualificationMeasurementPlane")]
public sealed class SiteQualificationMeasurementPlane : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE tagekyc.site_raw_ingress_qualification_synthetic_credentials (
              "SiteId" varchar(128) NOT NULL,"EndpointOrigin" varchar(512) NOT NULL,
              "DeploymentRevision" varchar(128) NOT NULL,"CredentialId" uuid NOT NULL,
              "CredentialGeneration" bigint NOT NULL,"EnrolledAtUtc" timestamptz NOT NULL,
              "ExpiresAtUtc" timestamptz NOT NULL,"EnrolledByApiKeyId" uuid NOT NULL,
              PRIMARY KEY("SiteId","DeploymentRevision","CredentialId","CredentialGeneration"),
              CHECK("ExpiresAtUtc">"EnrolledAtUtc" AND
                "EnrolledByApiKeyId"<>'00000000-0000-0000-0000-000000000000'::uuid));
            ALTER TABLE tagekyc.site_raw_ingress_qualification_synthetic_credentials
              OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON TABLE tagekyc.site_raw_ingress_qualification_synthetic_credentials
              FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker;

            CREATE TABLE tagekyc.site_raw_ingress_qualification_runs (
              "QualificationRunId" uuid PRIMARY KEY,
              "QualificationSuiteId" uuid NOT NULL,
              "SiteId" varchar(128) NOT NULL,"EndpointOrigin" varchar(512) NOT NULL,
              "DeploymentRevision" varchar(128) NOT NULL,"CredentialId" uuid NOT NULL,
              "CredentialGeneration" bigint NOT NULL,"IngressIdempotencyKey" uuid NOT NULL,
              "IngressMetadataSha256" char(64) NOT NULL,"MediaType" varchar(64) NOT NULL,
              "ContentLength" bigint NOT NULL,"PlaintextSha256" char(64) NOT NULL,
              "Mode" varchar(32) NOT NULL,"State" varchar(32) NOT NULL,
              "CreatedAtUtc" timestamptz NOT NULL,"ExpiresAtUtc" timestamptz NOT NULL,
              "RegisteredByApiKeyId" uuid NOT NULL,"ConsumedAtUtc" timestamptz NULL,
              "BrokerHeldAtUtc" timestamptz NULL,"BrokerCommittedAtUtc" timestamptz NULL,
              "ReleaseRequestedAtUtc" timestamptz NULL,"CommitAcknowledgedAtUtc" timestamptz NULL,
              "AgentObservationCompletedAtUtc" timestamptz NULL,
              "ServerObservationCompletedAtUtc" timestamptz NULL,
              "RawPostCount" integer NOT NULL DEFAULT 0,
              "ServerBodyReadsWhileBrokerHeld" integer NOT NULL DEFAULT 0,
              "ClientTransportEntryCount" integer NOT NULL DEFAULT 0,
              "ContentBytesCopied" bigint NOT NULL DEFAULT 0,
              "AgentBodyBytesSentWhileBrokerHeld" bigint NOT NULL DEFAULT 0,
              "ObservedContinue" boolean NOT NULL DEFAULT false,
              "ContinueObservedBeforeBrokerCommit" boolean NOT NULL DEFAULT false,
              "ApplicationPrebufferObserved" boolean NOT NULL DEFAULT false,
              "FinalResponseObserved" boolean NOT NULL DEFAULT false,
              "UpdatedAtUtc" timestamptz NOT NULL,
              CONSTRAINT "ck_site_raw_ingress_qualification_run_shape" CHECK (
                "QualificationRunId"<>'00000000-0000-0000-0000-000000000000'::uuid
                AND "QualificationSuiteId"<>'00000000-0000-0000-0000-000000000000'::uuid
                AND "CredentialId"<>'00000000-0000-0000-0000-000000000000'::uuid
                AND "IngressIdempotencyKey"<>'00000000-0000-0000-0000-000000000000'::uuid
                AND "RegisteredByApiKeyId"<>'00000000-0000-0000-0000-000000000000'::uuid
                AND "CredentialGeneration">=1 AND "ContentLength">0
                AND "IngressMetadataSha256"~'^[0-9a-f]{64}$' AND "PlaintextSha256"~'^[0-9a-f]{64}$'
                AND "MediaType"='image/jpeg' AND "Mode" IN ('FullBodyHeldCommit','LostFinalNoRetry')
                AND "State" IN ('Registered','Consumed','BrokerHeld','BrokerCommitted','Expired')
                AND "ExpiresAtUtc">"CreatedAtUtc" AND "RawPostCount">=0
                AND "ServerBodyReadsWhileBrokerHeld">=0 AND "ClientTransportEntryCount">=0
                AND "ContentBytesCopied">=0 AND "AgentBodyBytesSentWhileBrokerHeld">=0));
            CREATE UNIQUE INDEX "ix_site_raw_ingress_qualification_binding"
              ON tagekyc.site_raw_ingress_qualification_runs
              ("CredentialId","CredentialGeneration","IngressIdempotencyKey");
            CREATE INDEX "ix_site_raw_ingress_qualification_expiry"
              ON tagekyc.site_raw_ingress_qualification_runs("ExpiresAtUtc","State");
            ALTER TABLE tagekyc.site_raw_ingress_qualification_runs OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON TABLE tagekyc.site_raw_ingress_qualification_runs
              FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker;

            CREATE FUNCTION tagekyc.site_qualification_enroll_synthetic_credential(
              p_site_id text,p_endpoint_origin text,p_deployment_revision text,
              p_credential_id uuid,p_credential_generation bigint,p_expires_at timestamptz,
              p_enrolled_by_api_key_id uuid)
            RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE decision_time timestamptz:=pg_catalog.clock_timestamp();
            BEGIN
              IF p_site_id IS NULL OR pg_catalog.length(p_site_id) NOT BETWEEN 1 AND 128
                 OR p_endpoint_origin IS NULL OR pg_catalog.length(p_endpoint_origin) NOT BETWEEN 1 AND 512
                 OR p_deployment_revision IS NULL OR pg_catalog.length(p_deployment_revision) NOT BETWEEN 1 AND 128
                 OR p_credential_id IS NULL OR p_credential_generation IS NULL OR p_credential_generation<1
                 OR p_enrolled_by_api_key_id IS NULL OR
                    p_enrolled_by_api_key_id='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_expires_at IS NULL OR p_expires_at<=decision_time OR
                    p_expires_at>decision_time+INTERVAL '24 hours'
              THEN RETURN false; END IF;
              INSERT INTO tagekyc.site_raw_ingress_qualification_synthetic_credentials(
                "SiteId","EndpointOrigin","DeploymentRevision","CredentialId","CredentialGeneration",
                "EnrolledAtUtc","ExpiresAtUtc","EnrolledByApiKeyId") VALUES(
                p_site_id,p_endpoint_origin,p_deployment_revision,p_credential_id,p_credential_generation,
                decision_time,p_expires_at,p_enrolled_by_api_key_id)
              ON CONFLICT("SiteId","DeploymentRevision","CredentialId","CredentialGeneration") DO UPDATE SET
                "EndpointOrigin"=EXCLUDED."EndpointOrigin","EnrolledAtUtc"=decision_time,
                "ExpiresAtUtc"=EXCLUDED."ExpiresAtUtc","EnrolledByApiKeyId"=EXCLUDED."EnrolledByApiKeyId";
              RETURN true;
            END $function$;

            CREATE FUNCTION tagekyc.site_qualification_register_run(
              p_site_id text,p_endpoint_origin text,p_deployment_revision text,p_suite_id uuid,
              p_credential_id uuid,p_credential_generation bigint,p_ingress_idempotency_key uuid,
              p_metadata_sha256 text,p_media_type text,p_content_length bigint,p_plaintext_sha256 text,
              p_mode text,p_expires_at timestamptz,p_registered_by_api_key_id uuid)
            RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE run_id uuid:=pg_catalog.gen_random_uuid(); decision_time timestamptz:=pg_catalog.clock_timestamp();
            BEGIN
              IF p_site_id IS NULL OR pg_catalog.length(p_site_id) NOT BETWEEN 1 AND 128
                 OR p_endpoint_origin IS NULL OR pg_catalog.length(p_endpoint_origin) NOT BETWEEN 1 AND 512
                 OR p_deployment_revision IS NULL OR pg_catalog.length(p_deployment_revision) NOT BETWEEN 1 AND 128
                 OR p_suite_id IS NULL OR p_suite_id='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_credential_id IS NULL OR p_credential_id='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_credential_generation IS NULL OR p_credential_generation<1
                 OR p_ingress_idempotency_key IS NULL OR p_ingress_idempotency_key='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_metadata_sha256 !~ '^[0-9a-f]{64}$' OR p_plaintext_sha256 !~ '^[0-9a-f]{64}$'
                 OR p_media_type<>'image/jpeg' OR p_content_length IS NULL OR p_content_length<1
                 OR p_registered_by_api_key_id IS NULL OR p_registered_by_api_key_id='00000000-0000-0000-0000-000000000000'::uuid
                 OR p_mode NOT IN ('FullBodyHeldCommit','LostFinalNoRetry')
                 OR p_expires_at IS NULL OR p_expires_at<=decision_time
                 OR p_expires_at>decision_time+INTERVAL '10 minutes'
              THEN RAISE EXCEPTION 'SITE_QUALIFICATION_RUN_INVALID'; END IF;
              IF NOT EXISTS(SELECT 1 FROM tagekyc.site_raw_ingress_qualification_synthetic_credentials AS s
                WHERE s."SiteId"=p_site_id AND s."EndpointOrigin"=p_endpoint_origin
                  AND s."DeploymentRevision"=p_deployment_revision
                  AND s."CredentialId"=p_credential_id AND s."CredentialGeneration"=p_credential_generation
                  AND s."ExpiresAtUtc">decision_time)
              THEN RAISE EXCEPTION 'SITE_QUALIFICATION_SYNTHETIC_PROVENANCE_REQUIRED'; END IF;
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET "State"='Expired',"UpdatedAtUtc"=decision_time
              WHERE "CredentialId"=p_credential_id AND "CredentialGeneration"=p_credential_generation
                AND "IngressIdempotencyKey"=p_ingress_idempotency_key
                AND "ExpiresAtUtc"<=decision_time AND "State"<>'Expired';
              IF EXISTS(SELECT 1 FROM tagekyc.site_raw_ingress_qualification_runs
                WHERE "CredentialId"=p_credential_id AND "CredentialGeneration"=p_credential_generation
                  AND "IngressIdempotencyKey"=p_ingress_idempotency_key
                  AND "ExpiresAtUtc">decision_time AND "State"<>'Expired')
              THEN RAISE EXCEPTION 'SITE_QUALIFICATION_RUN_CONFLICT'; END IF;
              INSERT INTO tagekyc.site_raw_ingress_qualification_runs(
                "QualificationRunId","QualificationSuiteId","SiteId","EndpointOrigin","DeploymentRevision",
                "CredentialId","CredentialGeneration","IngressIdempotencyKey","IngressMetadataSha256",
                "MediaType","ContentLength","PlaintextSha256","Mode","State","CreatedAtUtc",
                "ExpiresAtUtc","RegisteredByApiKeyId","UpdatedAtUtc")
              VALUES(run_id,p_suite_id,p_site_id,p_endpoint_origin,p_deployment_revision,p_credential_id,
                p_credential_generation,p_ingress_idempotency_key,p_metadata_sha256,p_media_type,
                p_content_length,p_plaintext_sha256,p_mode,'Registered',decision_time,p_expires_at,
                p_registered_by_api_key_id,decision_time);
              RETURN run_id;
            END $function$;

            CREATE FUNCTION tagekyc.site_qualification_observe_raw_post(
              p_site_id text,p_endpoint_origin text,p_deployment_revision text,
              p_credential_id uuid,p_credential_generation bigint,p_ingress_idempotency_key uuid,
              p_metadata_sha256 text,p_media_type text,p_content_length bigint,p_plaintext_sha256 text)
            RETURNS TABLE("QualificationRunId" uuid,"Mode" text,"IsActive" boolean)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE selected tagekyc.site_raw_ingress_qualification_runs%ROWTYPE; decision_time timestamptz;
            BEGIN
              SELECT q.* INTO selected FROM tagekyc.site_raw_ingress_qualification_runs AS q
              WHERE q."SiteId"=p_site_id AND q."EndpointOrigin"=p_endpoint_origin
                AND q."DeploymentRevision"=p_deployment_revision AND q."CredentialId"=p_credential_id
                AND q."CredentialGeneration"=p_credential_generation
                AND q."IngressIdempotencyKey"=p_ingress_idempotency_key
                AND q."IngressMetadataSha256"=p_metadata_sha256 AND q."MediaType"=p_media_type
                AND q."ContentLength"=p_content_length AND q."PlaintextSha256"=p_plaintext_sha256
                AND q."State"<>'Expired' ORDER BY q."CreatedAtUtc" DESC LIMIT 1 FOR UPDATE;
              IF NOT FOUND THEN RETURN; END IF;
              decision_time:=pg_catalog.clock_timestamp();
              UPDATE tagekyc.site_raw_ingress_qualification_runs AS q SET
                "RawPostCount"=q."RawPostCount"+1,
                "State"=CASE WHEN q."ExpiresAtUtc"<=decision_time THEN 'Expired' ELSE q."State" END,
                "UpdatedAtUtc"=decision_time WHERE q."QualificationRunId"=selected."QualificationRunId";
              RETURN QUERY SELECT selected."QualificationRunId",selected."Mode"::text,
                selected."State"='Registered' AND selected."ExpiresAtUtc">decision_time;
            END $function$;

            CREATE FUNCTION tagekyc.site_qualification_consume_authenticated(
              p_run_id uuid,p_credential_id uuid,p_credential_generation bigint,
              p_ingress_idempotency_key uuid,p_metadata_sha256 text,p_media_type text,
              p_content_length bigint,p_plaintext_sha256 text)
            RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE selected tagekyc.site_raw_ingress_qualification_runs%ROWTYPE; decision_time timestamptz;
            BEGIN
              SELECT q.* INTO selected FROM tagekyc.site_raw_ingress_qualification_runs AS q
              WHERE q."QualificationRunId"=p_run_id FOR UPDATE;
              IF NOT FOUND THEN RETURN false; END IF;
              decision_time:=pg_catalog.clock_timestamp();
              IF selected."State"<>'Registered' OR selected."ExpiresAtUtc"<=decision_time
                 OR selected."CredentialId" IS DISTINCT FROM p_credential_id
                 OR selected."CredentialGeneration" IS DISTINCT FROM p_credential_generation
                 OR selected."IngressIdempotencyKey" IS DISTINCT FROM p_ingress_idempotency_key
                 OR selected."IngressMetadataSha256" IS DISTINCT FROM p_metadata_sha256
                 OR selected."MediaType" IS DISTINCT FROM p_media_type
                 OR selected."ContentLength" IS DISTINCT FROM p_content_length
                 OR selected."PlaintextSha256" IS DISTINCT FROM p_plaintext_sha256
              THEN
                IF selected."ExpiresAtUtc"<=decision_time AND selected."State"='Registered' THEN
                  UPDATE tagekyc.site_raw_ingress_qualification_runs SET "State"='Expired',
                    "UpdatedAtUtc"=decision_time WHERE "QualificationRunId"=p_run_id;
                END IF;
                RETURN false;
              END IF;
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET "State"='Consumed',
                "ConsumedAtUtc"=decision_time,"UpdatedAtUtc"=decision_time
              WHERE "QualificationRunId"=p_run_id;
              RETURN true;
            END $function$;

            CREATE FUNCTION tagekyc.site_qualification_release_broker(
              p_run_id uuid,p_api_key_id uuid,p_site_id text,p_endpoint_origin text,p_deployment_revision text)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET
                "ReleaseRequestedAtUtc"=pg_catalog.clock_timestamp(),"UpdatedAtUtc"=pg_catalog.clock_timestamp()
              WHERE "QualificationRunId"=p_run_id AND "ExpiresAtUtc">pg_catalog.clock_timestamp()
                AND "RegisteredByApiKeyId"=p_api_key_id AND "SiteId"=p_site_id
                AND "EndpointOrigin"=p_endpoint_origin AND "DeploymentRevision"=p_deployment_revision
                AND "State" IN ('Consumed','BrokerHeld') AND "ReleaseRequestedAtUtc" IS NULL RETURNING true
            $function$;

            CREATE FUNCTION tagekyc.site_qualification_record_agent_observation(
              p_run_id uuid,p_api_key_id uuid,p_site_id text,p_endpoint_origin text,p_deployment_revision text,
              p_transport_entries integer,p_content_bytes_copied bigint,p_body_bytes_while_held bigint,
              p_observed_continue boolean,p_continue_before_commit boolean,p_application_prebuffer boolean,
              p_final_response boolean)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET
                "ClientTransportEntryCount"=p_transport_entries,"ContentBytesCopied"=p_content_bytes_copied,
                "AgentBodyBytesSentWhileBrokerHeld"=p_body_bytes_while_held,
                "ObservedContinue"=p_observed_continue,
                "ContinueObservedBeforeBrokerCommit"=p_continue_before_commit,
                "ApplicationPrebufferObserved"=p_application_prebuffer,"FinalResponseObserved"=p_final_response,
                "AgentObservationCompletedAtUtc"=pg_catalog.clock_timestamp(),
                "UpdatedAtUtc"=pg_catalog.clock_timestamp()
              WHERE "QualificationRunId"=p_run_id AND "ExpiresAtUtc">pg_catalog.clock_timestamp()
                AND "RegisteredByApiKeyId"=p_api_key_id AND "SiteId"=p_site_id
                AND "EndpointOrigin"=p_endpoint_origin AND "DeploymentRevision"=p_deployment_revision
                AND "AgentObservationCompletedAtUtc" IS NULL AND p_transport_entries BETWEEN 0 AND 16
                AND p_content_bytes_copied BETWEEN 0 AND 2147483647
                AND p_body_bytes_while_held BETWEEN 0 AND 2147483647 RETURNING true
            $function$;

            CREATE FUNCTION tagekyc.site_qualification_acknowledge_broker_commit(
              p_run_id uuid,p_api_key_id uuid,p_site_id text,p_endpoint_origin text,p_deployment_revision text)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET
                "CommitAcknowledgedAtUtc"=pg_catalog.clock_timestamp(),"UpdatedAtUtc"=pg_catalog.clock_timestamp()
              WHERE "QualificationRunId"=p_run_id AND "ExpiresAtUtc">pg_catalog.clock_timestamp()
                AND "RegisteredByApiKeyId"=p_api_key_id AND "SiteId"=p_site_id
                AND "EndpointOrigin"=p_endpoint_origin AND "DeploymentRevision"=p_deployment_revision
                AND "State"='BrokerCommitted' AND "CommitAcknowledgedAtUtc" IS NULL RETURNING true
            $function$;

            CREATE FUNCTION tagekyc.site_qualification_record_server_body_read(p_run_id uuid,p_reads_while_held integer)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET
                "ServerBodyReadsWhileBrokerHeld"=p_reads_while_held,
                "ServerObservationCompletedAtUtc"=pg_catalog.clock_timestamp(),
                "UpdatedAtUtc"=pg_catalog.clock_timestamp()
              WHERE "QualificationRunId"=p_run_id AND "ExpiresAtUtc">pg_catalog.clock_timestamp()
                AND "ServerObservationCompletedAtUtc" IS NULL AND p_reads_while_held BETWEEN 0 AND 1000000 RETURNING true
            $function$;

            CREATE FUNCTION tagekyc.site_qualification_read_run(
              p_run_id uuid,p_api_key_id uuid,p_site_id text,p_endpoint_origin text,p_deployment_revision text)
            RETURNS TABLE("QualificationRunId" uuid,"QualificationSuiteId" uuid,"SiteId" text,
              "EndpointOrigin" text,"DeploymentRevision" text,"State" text,"Mode" text,
              "CreatedAtUtc" timestamptz,"ExpiresAtUtc" timestamptz,"ConsumedAtUtc" timestamptz,
              "BrokerHeldAtUtc" timestamptz,"BrokerCommittedAtUtc" timestamptz,"RawPostCount" integer,
              "ServerBodyReadsWhileBrokerHeld" integer,"ClientTransportEntryCount" integer,
              "ContentBytesCopied" bigint,"AgentBodyBytesSentWhileBrokerHeld" bigint,
              "ObservedContinue" boolean,"ContinueObservedBeforeBrokerCommit" boolean,
              "ApplicationPrebufferObserved" boolean,"FinalResponseObserved" boolean,
              "AgentObservationCompleted" boolean,"ServerObservationCompleted" boolean,"EvidenceComplete" boolean)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
            DECLARE decision_time timestamptz:=pg_catalog.clock_timestamp();
            BEGIN
              UPDATE tagekyc.site_raw_ingress_qualification_runs AS q SET
                "State"=CASE WHEN q."ExpiresAtUtc"<=decision_time AND q."State"<>'BrokerCommitted'
                  THEN 'Expired' ELSE q."State" END,"UpdatedAtUtc"=decision_time
              WHERE q."QualificationRunId"=p_run_id AND q."RegisteredByApiKeyId"=p_api_key_id
                AND q."SiteId"=p_site_id AND q."EndpointOrigin"=p_endpoint_origin
                AND q."DeploymentRevision"=p_deployment_revision;
              RETURN QUERY SELECT q."QualificationRunId",q."QualificationSuiteId",q."SiteId"::text,
                q."EndpointOrigin"::text,q."DeploymentRevision"::text,q."State"::text,q."Mode"::text,
                q."CreatedAtUtc",q."ExpiresAtUtc",q."ConsumedAtUtc",q."BrokerHeldAtUtc",q."BrokerCommittedAtUtc",
                q."RawPostCount",q."ServerBodyReadsWhileBrokerHeld",q."ClientTransportEntryCount",
                q."ContentBytesCopied",q."AgentBodyBytesSentWhileBrokerHeld",q."ObservedContinue",
                q."ContinueObservedBeforeBrokerCommit",q."ApplicationPrebufferObserved",q."FinalResponseObserved",
                q."AgentObservationCompletedAtUtc" IS NOT NULL,q."ServerObservationCompletedAtUtc" IS NOT NULL,
                q."State"='BrokerCommitted' AND q."CommitAcknowledgedAtUtc" IS NOT NULL
                  AND q."AgentObservationCompletedAtUtc" IS NOT NULL
                  AND q."ServerObservationCompletedAtUtc" IS NOT NULL
                  AND q."ContentBytesCopied"=q."ContentLength"
              FROM tagekyc.site_raw_ingress_qualification_runs AS q
              WHERE q."QualificationRunId"=p_run_id AND q."RegisteredByApiKeyId"=p_api_key_id
                AND q."SiteId"=p_site_id AND q."EndpointOrigin"=p_endpoint_origin
                AND q."DeploymentRevision"=p_deployment_revision;
            END $function$;

            CREATE FUNCTION tagekyc.site_qualification_broker_mark_held(p_run_id uuid)
            RETURNS uuid LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET "State"='BrokerHeld',
                "BrokerHeldAtUtc"=pg_catalog.clock_timestamp(),"UpdatedAtUtc"=pg_catalog.clock_timestamp()
              WHERE "QualificationRunId"=p_run_id AND "State"='Consumed'
                AND "ExpiresAtUtc">pg_catalog.clock_timestamp()
              RETURNING "QualificationRunId"
            $function$;
            CREATE FUNCTION tagekyc.site_qualification_broker_is_released(p_run_id uuid)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              SELECT COALESCE((SELECT "ReleaseRequestedAtUtc" IS NOT NULL OR "ExpiresAtUtc"<=pg_catalog.clock_timestamp()
                FROM tagekyc.site_raw_ingress_qualification_runs WHERE "QualificationRunId"=p_run_id AND "State"='BrokerHeld'),false)
            $function$;
            CREATE FUNCTION tagekyc.site_qualification_broker_mark_committed(p_run_id uuid)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              UPDATE tagekyc.site_raw_ingress_qualification_runs SET "State"='BrokerCommitted',
                "BrokerCommittedAtUtc"=pg_catalog.clock_timestamp(),"UpdatedAtUtc"=pg_catalog.clock_timestamp()
              WHERE "QualificationRunId"=p_run_id AND "State"='BrokerHeld'
                AND "ExpiresAtUtc">pg_catalog.clock_timestamp() RETURNING true
            $function$;
            CREATE FUNCTION tagekyc.site_qualification_broker_is_commit_acknowledged(p_run_id uuid)
            RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $function$
              SELECT COALESCE((SELECT "CommitAcknowledgedAtUtc" IS NOT NULL OR "ExpiresAtUtc"<=pg_catalog.clock_timestamp()
                FROM tagekyc.site_raw_ingress_qualification_runs WHERE "QualificationRunId"=p_run_id
                  AND "State"='BrokerCommitted'),true)
            $function$;

            ALTER FUNCTION tagekyc.site_qualification_enroll_synthetic_credential(text,text,text,uuid,bigint,timestamptz,uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_register_run(text,text,text,uuid,uuid,bigint,uuid,text,text,bigint,text,text,timestamptz,uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_observe_raw_post(text,text,text,uuid,bigint,uuid,text,text,bigint,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_consume_authenticated(uuid,uuid,bigint,uuid,text,text,bigint,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_release_broker(uuid,uuid,text,text,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_record_agent_observation(uuid,uuid,text,text,text,integer,bigint,bigint,boolean,boolean,boolean,boolean) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_acknowledge_broker_commit(uuid,uuid,text,text,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_record_server_body_read(uuid,integer) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_read_run(uuid,uuid,text,text,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_broker_mark_held(uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_broker_is_released(uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_broker_mark_committed(uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.site_qualification_broker_is_commit_acknowledged(uuid) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.site_qualification_enroll_synthetic_credential(text,text,text,uuid,bigint,timestamptz,uuid),
              tagekyc.site_qualification_register_run(text,text,text,uuid,uuid,bigint,uuid,text,text,bigint,text,text,timestamptz,uuid),
              tagekyc.site_qualification_observe_raw_post(text,text,text,uuid,bigint,uuid,text,text,bigint,text),
              tagekyc.site_qualification_consume_authenticated(uuid,uuid,bigint,uuid,text,text,bigint,text),
              tagekyc.site_qualification_release_broker(uuid,uuid,text,text,text),
              tagekyc.site_qualification_record_agent_observation(uuid,uuid,text,text,text,integer,bigint,bigint,boolean,boolean,boolean,boolean),
              tagekyc.site_qualification_acknowledge_broker_commit(uuid,uuid,text,text,text),
              tagekyc.site_qualification_record_server_body_read(uuid,integer),tagekyc.site_qualification_read_run(uuid,uuid,text,text,text),
              tagekyc.site_qualification_broker_mark_held(uuid),tagekyc.site_qualification_broker_is_released(uuid),
              tagekyc.site_qualification_broker_mark_committed(uuid),tagekyc.site_qualification_broker_is_commit_acknowledged(uuid)
              FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_claim_broker;
            GRANT EXECUTE ON FUNCTION tagekyc.site_qualification_enroll_synthetic_credential(text,text,text,uuid,bigint,timestamptz,uuid),
              tagekyc.site_qualification_register_run(text,text,text,uuid,uuid,bigint,uuid,text,text,bigint,text,text,timestamptz,uuid),
              tagekyc.site_qualification_observe_raw_post(text,text,text,uuid,bigint,uuid,text,text,bigint,text),
              tagekyc.site_qualification_consume_authenticated(uuid,uuid,bigint,uuid,text,text,bigint,text),
              tagekyc.site_qualification_release_broker(uuid,uuid,text,text,text),
              tagekyc.site_qualification_record_agent_observation(uuid,uuid,text,text,text,integer,bigint,bigint,boolean,boolean,boolean,boolean),
              tagekyc.site_qualification_acknowledge_broker_commit(uuid,uuid,text,text,text),
              tagekyc.site_qualification_record_server_body_read(uuid,integer),tagekyc.site_qualification_read_run(uuid,uuid,text,text,text)
              TO tagekyc_runtime;
            GRANT EXECUTE ON FUNCTION tagekyc.site_qualification_broker_mark_held(uuid),
              tagekyc.site_qualification_broker_is_released(uuid),tagekyc.site_qualification_broker_mark_committed(uuid),
              tagekyc.site_qualification_broker_is_commit_acknowledged(uuid) TO tagekyc_raw_export_claim_broker;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_broker_is_commit_acknowledged(uuid);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_broker_mark_committed(uuid);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_broker_is_released(uuid);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_broker_mark_held(uuid);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_read_run(uuid,uuid,text,text,text);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_record_server_body_read(uuid,integer);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_acknowledge_broker_commit(uuid,uuid,text,text,text);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_record_agent_observation(uuid,uuid,text,text,text,integer,bigint,bigint,boolean,boolean,boolean,boolean);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_release_broker(uuid,uuid,text,text,text);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_consume_authenticated(uuid,uuid,bigint,uuid,text,text,bigint,text);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_observe_raw_post(text,text,text,uuid,bigint,uuid,text,text,bigint,text);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_register_run(text,text,text,uuid,uuid,bigint,uuid,text,text,bigint,text,text,timestamptz,uuid);
            DROP FUNCTION IF EXISTS tagekyc.site_qualification_enroll_synthetic_credential(text,text,text,uuid,bigint,timestamptz,uuid);
            DROP TABLE IF EXISTS tagekyc.site_raw_ingress_qualification_runs;
            DROP TABLE IF EXISTS tagekyc.site_raw_ingress_qualification_synthetic_credentials;
            """);
    }
}
