using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TagEkycDbContext))]
[Migration("20261002120000_ProductionApplicationPersistencePrincipal")]
public sealed class ProductionApplicationPersistencePrincipal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $migration$
            DECLARE role_row pg_catalog.pg_roles%ROWTYPE;
            BEGIN
              SELECT * INTO role_row FROM pg_catalog.pg_roles
              WHERE rolname='tagekyc_application_persistence';
              IF NOT FOUND THEN
                CREATE ROLE tagekyc_application_persistence
                  NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                  NOREPLICATION NOBYPASSRLS INHERIT;
              ELSIF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolcreatedb OR
                    role_row.rolcreaterole OR role_row.rolreplication OR
                    role_row.rolbypassrls OR NOT role_row.rolinherit THEN
                RAISE EXCEPTION 'APPLICATION_PERSISTENCE_ROLE_INVALID';
              END IF;
            END
            $migration$;

            REVOKE ALL PRIVILEGES ON SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON SCHEMA public FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public FROM tagekyc_application_persistence;

            DO $migration$
            DECLARE relation_row record;
            DECLARE column_list text;
            BEGIN
              FOR relation_row IN
                SELECT n.nspname, c.oid, c.relname
                FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname IN ('tagekyc','public')
                  AND c.relkind IN ('r','p','v','m','f')
              LOOP
                SELECT pg_catalog.string_agg(pg_catalog.format('%I',a.attname),',' ORDER BY a.attnum)
                INTO column_list
                FROM pg_catalog.pg_attribute a
                WHERE a.attrelid=relation_row.oid AND a.attnum>0 AND NOT a.attisdropped;
                IF column_list IS NOT NULL THEN
                  EXECUTE pg_catalog.format(
                    'REVOKE SELECT (%s), INSERT (%s), UPDATE (%s), REFERENCES (%s) ON TABLE %I.%I FROM tagekyc_application_persistence',
                    column_list, column_list, column_list, column_list,
                    relation_row.nspname, relation_row.relname);
                END IF;
              END LOOP;
            END
            $migration$;

            GRANT USAGE ON SCHEMA tagekyc, public TO tagekyc_application_persistence;
            GRANT SELECT, INSERT ON TABLE
              tagekyc.verification_sessions,
              tagekyc.capture_artifacts,
              tagekyc.evidence_results,
              tagekyc.verification_decisions,
              tagekyc.evidence_packages,
              tagekyc.evidence_manifests,
              tagekyc.audit_events,
              tagekyc.append_idempotency_records
              TO tagekyc_application_persistence;
            GRANT UPDATE (
              "State", "Result", "AssuranceLevel", "FinalDecisionId",
              "EvidencePackageId", "EvidencePackageHash", "ManifestHash",
              "RequestId", "CorrelationId", "CompletedAt"
            ) ON TABLE tagekyc.verification_sessions TO tagekyc_application_persistence;
            GRANT SELECT ON TABLE tagekyc.api_keys TO tagekyc_application_persistence;
            GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO tagekyc_application_persistence;

            CREATE OR REPLACE FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(
              p_api_key_id uuid,
              p_recipient_client_application_id uuid,
              p_principal_id uuid,
              p_activation_profile text,
              p_expected_scope_digest bytea)
            RETURNS boolean
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path=pg_catalog
            AS $function$
              SELECT EXISTS (
                SELECT 1
                FROM tagekyc.raw_export_managed_recipient_credentials AS credential
                JOIN tagekyc.raw_export_managed_recipient_identities AS identity
                  ON identity."RecipientClientApplicationId"=credential."RecipientClientApplicationId"
                 AND identity."PrincipalId"=credential."PrincipalId"
                JOIN tagekyc.raw_export_managed_recipient_policies AS policy
                  ON policy."RecipientClientApplicationId"=credential."RecipientClientApplicationId"
                WHERE credential."ApiKeyId"=p_api_key_id
                  AND credential."RecipientClientApplicationId"=p_recipient_client_application_id
                  AND credential."PrincipalId"=p_principal_id
                  AND credential."State"='Active'
                  AND identity."State"='Active'
                  AND policy."State"='Active'
                  AND policy."ActivationProfile"=p_activation_profile
                  AND policy."ActivationScopesDigest"=p_expected_scope_digest
              )
            $function$;
            ALTER FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea)
              OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea) FROM PUBLIC;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea) FROM tagekyc_runtime;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea)
              TO tagekyc_application_persistence;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea)
              FROM tagekyc_application_persistence;
            DROP FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea);
            REVOKE ALL PRIVILEGES ON SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON SCHEMA public FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA tagekyc FROM tagekyc_application_persistence;
            REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public FROM tagekyc_application_persistence;
            DO $migration$
            DECLARE relation_row record;
            DECLARE column_list text;
            BEGIN
              FOR relation_row IN
                SELECT n.nspname, c.oid, c.relname
                FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname IN ('tagekyc','public')
                  AND c.relkind IN ('r','p','v','m','f')
              LOOP
                SELECT pg_catalog.string_agg(pg_catalog.format('%I',a.attname),',' ORDER BY a.attnum)
                INTO column_list
                FROM pg_catalog.pg_attribute a
                WHERE a.attrelid=relation_row.oid AND a.attnum>0 AND NOT a.attisdropped;
                IF column_list IS NOT NULL THEN
                  EXECUTE pg_catalog.format(
                    'REVOKE SELECT (%s), INSERT (%s), UPDATE (%s), REFERENCES (%s) ON TABLE %I.%I FROM tagekyc_application_persistence',
                    column_list, column_list, column_list, column_list,
                    relation_row.nspname, relation_row.relname);
                END IF;
              END LOOP;
            END
            $migration$;
            """);
    }
}
