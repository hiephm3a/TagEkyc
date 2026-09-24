using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TagEkycDbContext))]
[Migration("20260924130000_RawExportLegacyConsentClassFence")]
public sealed class RawExportLegacyConsentClassFence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        RewriteConsentLookup(migrationBuilder, addClassFence: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RewriteConsentLookup(migrationBuilder, addClassFence: false);

    private static void RewriteConsentLookup(
        MigrationBuilder migrationBuilder,
        bool addClassFence)
    {
        const string unfencedTail =
            "c.\"VerificationSessionId\",r.\"ConsentPolicyId\",r.\"ConsentPolicyVersion\");";
        const string fencedTail =
            "c.\"VerificationSessionId\",r.\"ConsentPolicyId\",r.\"ConsentPolicyVersion\") resolved_consent WHERE resolved_consent.\"RawClass\"=c.\"RawClass\";";
        var sourceTail = addClassFence ? unfencedTail : fencedTail;
        var targetTail = addClassFence ? fencedTail : unfencedTail;

        migrationBuilder.Sql($$"""
            DO $class_fence$
            DECLARE
              function_name text;
              definition text;
              successor text;
            BEGIN
              FOREACH function_name IN ARRAY ARRAY[
                'raw_export_stage_verified_source_ciphertext',
                'raw_export_commit_staged_source',
                'raw_export_publish_available_source']
              LOOP
                SELECT pg_catalog.pg_get_functiondef(p.oid) INTO STRICT definition
                FROM pg_catalog.pg_proc p
                JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname=function_name;

                successor:=pg_catalog.replace(definition,{{SqlLiteral(sourceTail)}},{{SqlLiteral(targetTail)}});
                IF successor=definition THEN
                  RAISE EXCEPTION 'RAW_EXPORT_LEGACY_CONSENT_CLASS_FENCE_NOT_APPLIED: %',function_name;
                END IF;
                EXECUTE successor;
              END LOOP;
            END
            $class_fence$;
            """);
    }

    private static string SqlLiteral(string value) =>
        $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
