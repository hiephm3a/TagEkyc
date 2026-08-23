using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1C4PackageReferenceDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_raw_export_recipient_package_reference_list",
                schema: "tagekyc",
                table: "raw_export_recipient_package_preparations",
                columns: new[] { "RecipientClientApplicationId", "FinalizedAtUtc", "PackageId" },
                descending: new[] { false, true, true },
                filter: "\"State\" = 'Finalized' AND \"FinalizedAtUtc\" IS NOT NULL");

            migrationBuilder.Sql("""
                DO $c4_roles$
                DECLARE capability pg_catalog.pg_roles%ROWTYPE; login_role pg_catalog.pg_roles%ROWTYPE;
                BEGIN
                  SELECT * INTO capability FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_package_reference';
                  IF FOUND THEN
                    IF capability.rolcanlogin OR capability.rolsuper OR capability.rolcreatedb OR capability.rolcreaterole
                       OR capability.rolreplication OR capability.rolbypassrls OR NOT capability.rolinherit THEN
                      RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='PROD_RAW_EXPORT_PACKAGE_REFERENCE_ROLE_TOPOLOGY_INVALID';
                    END IF;
                  ELSE
                    CREATE ROLE tagekyc_raw_export_package_reference NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT;
                  END IF;
                  SELECT * INTO login_role FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_package_reference_login';
                  IF NOT FOUND OR NOT login_role.rolcanlogin OR login_role.rolsuper OR login_role.rolcreatedb OR login_role.rolcreaterole
                     OR login_role.rolreplication OR login_role.rolbypassrls OR NOT login_role.rolinherit THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='PROD_RAW_EXPORT_PACKAGE_REFERENCE_ROLE_TOPOLOGY_INVALID';
                  END IF;
                END $c4_roles$;

                GRANT tagekyc_raw_export_package_reference TO tagekyc_raw_export_package_reference_login WITH INHERIT TRUE, SET FALSE;
                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_raw_export_package_reference;

                CREATE FUNCTION tagekyc.raw_export_list_recipient_package_references(
                  p_recipient_client_application_id uuid,
                  p_boundary_finalized_at_utc timestamp with time zone,
                  p_boundary_package_id uuid,
                  p_page_size integer)
                RETURNS TABLE("PackageId" uuid,"FinalizedAtUtc" timestamp with time zone)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                BEGIN
                  IF p_recipient_client_application_id IS NULL
                     OR p_recipient_client_application_id='00000000-0000-0000-0000-000000000000'::uuid
                     OR p_page_size IS NULL OR p_page_size<1 OR p_page_size>50
                     OR ((p_boundary_finalized_at_utc IS NULL)<>(p_boundary_package_id IS NULL))
                     OR p_boundary_package_id='00000000-0000-0000-0000-000000000000'::uuid THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_PACKAGE_REFERENCE_ARGUMENT_INVALID';
                  END IF;
                  RETURN QUERY
                    SELECT p."PackageId",p."FinalizedAtUtc"
                    FROM tagekyc.raw_export_recipient_package_preparations p
                    WHERE p."RecipientClientApplicationId"=p_recipient_client_application_id
                      AND p."State"='Finalized'
                      AND p."FinalizedAtUtc" IS NOT NULL
                      AND p."PackageId"<>'00000000-0000-0000-0000-000000000000'::uuid
                      AND (p_boundary_finalized_at_utc IS NULL
                           OR p."FinalizedAtUtc"<p_boundary_finalized_at_utc
                           OR (p."FinalizedAtUtc"=p_boundary_finalized_at_utc AND p."PackageId"<p_boundary_package_id))
                    ORDER BY p."FinalizedAtUtc" DESC,p."PackageId" DESC
                    LIMIT p_page_size+1;
                END $fn$;
                ALTER FUNCTION tagekyc.raw_export_list_recipient_package_references(uuid,timestamp with time zone,uuid,integer)
                  OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_list_recipient_package_references(uuid,timestamp with time zone,uuid,integer)
                  FROM PUBLIC,tagekyc_raw_export_package_reference_login;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_list_recipient_package_references(uuid,timestamp with time zone,uuid,integer)
                  TO tagekyc_raw_export_package_reference;
                REVOKE ALL ON TABLE tagekyc.raw_export_recipient_package_preparations
                  FROM tagekyc_raw_export_package_reference,tagekyc_raw_export_package_reference_login;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS tagekyc.raw_export_list_recipient_package_references(
                  uuid,timestamp with time zone,uuid,integer);
                REVOKE USAGE ON SCHEMA tagekyc FROM tagekyc_raw_export_package_reference;
                """);
            migrationBuilder.DropIndex(
                name: "ix_raw_export_recipient_package_reference_list",
                schema: "tagekyc",
                table: "raw_export_recipient_package_preparations");
        }
    }
}
