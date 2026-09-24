using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TagEkycDbContext))]
[Migration("20260923090000_RawExportAssemblyDurableWorkSource")]
public sealed class RawExportAssemblyDurableWorkSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION tagekyc.raw_export_next_assembly_candidate()
            RETURNS TABLE(
              "JobId" uuid,
              "PrincipalId" uuid,
              "ClientApplicationId" uuid,
              "CreatedByApiKeyId" uuid,
              "Revision" bigint,
              "FencingToken" bigint)
            LANGUAGE sql
            SECURITY DEFINER
            SET search_path=pg_catalog
            AS $$
              SELECT i."JobId",i."PrincipalId",i."ClientApplicationId",i."CreatedByApiKeyId",
                     h."Revision",h."FencingToken"
              FROM tagekyc.raw_export_job_identities i
              JOIN tagekyc.raw_export_job_operational_heads h ON h."JobId"=i."JobId"
              WHERE i."ExportMode"='EncryptedExportPacket'
                AND i."JobExpiresAt">clock_timestamp()
                AND (
                  h."CurrentState"='Claimed'
                  OR (h."CurrentState"='Assembling'
                      AND (h."LeaseOwnerId" IS NULL OR h."LeaseExpiresAt"<=clock_timestamp())))
              ORDER BY i."CreatedAt",i."JobId"
              LIMIT 1
            $$;
            ALTER FUNCTION tagekyc.raw_export_next_assembly_candidate() OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_next_assembly_candidate()
              FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_assembly_resolver,tagekyc_raw_export_assembly_sealer;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_next_assembly_candidate()
              TO tagekyc_raw_export_assembly_resolver;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP FUNCTION IF EXISTS tagekyc.raw_export_next_assembly_candidate();
            """);
    }
}
