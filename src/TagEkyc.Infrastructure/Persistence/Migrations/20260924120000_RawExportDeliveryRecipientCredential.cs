using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TagEkycDbContext))]
[Migration("20260924120000_RawExportDeliveryRecipientCredential")]
public sealed class RawExportDeliveryRecipientCredential : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              DROP CONSTRAINT ck_raw_export_managed_recipient_policy_shape;

            UPDATE tagekyc.raw_export_managed_recipient_policies
            SET "ActivationProfile"='RawExportDeliveryRecipientV2',
                "ActivationScopesDigest"=pg_catalog.decode(
                  'dff4f677943b718b7f115462812f39951e0df81344ed0d70b65fdf1ad3f92133','hex'),
                "Revision"="Revision"+1,
                "UpdatedAtUtc"=clock_timestamp()
            WHERE "ActivationProfile"='C3C4RecipientV1';

            UPDATE tagekyc.api_keys
            SET "ScopesJson"='["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]'
            WHERE "CallerCategory"='BusinessConsumer'
              AND "ScopesJson"='["business.raw-export.package.download","business.raw-export.package.references.read"]';

            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              ADD CONSTRAINT ck_raw_export_managed_recipient_policy_shape
              CHECK ("ActivationProfile" = 'RawExportDeliveryRecipientV2'
                AND octet_length("ActivationScopesDigest") = 32
                AND "State" IN ('Active','Disabled') AND "Revision" > 0);

            DO $delivery$
            DECLARE
              function_name text;
              definition text;
              successor text;
            BEGIN
              FOREACH function_name IN ARRAY ARRAY[
                'raw_export_replace_recipient_credential',
                'raw_export_revoke_recipient_credential',
                'raw_export_enroll_managed_recipient',
                'raw_export_issue_recipient_credential',
                'raw_export_read_recipient_activation_readiness'
              ]
              LOOP
                SELECT pg_get_functiondef(p.oid) INTO STRICT definition
                FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname=function_name;

                successor := replace(definition,
                  '406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91',
                  'dff4f677943b718b7f115462812f39951e0df81344ed0d70b65fdf1ad3f92133');
                successor := replace(successor,
                  'C3C4RecipientV1', 'RawExportDeliveryRecipientV2');
                successor := replace(successor,
                  '["business.raw-export.package.download","business.raw-export.package.references.read"]',
                  '["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]');

                IF successor = definition THEN
                  RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_CREDENTIAL_FUNCTION_NOT_UPDATED:%', function_name;
                END IF;
                EXECUTE successor;
              END LOOP;
            END
            $delivery$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              DROP CONSTRAINT ck_raw_export_managed_recipient_policy_shape;

            UPDATE tagekyc.raw_export_managed_recipient_policies
            SET "ActivationProfile"='C3C4RecipientV1',
                "ActivationScopesDigest"=pg_catalog.decode(
                  '406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex'),
                "Revision"="Revision"+1,
                "UpdatedAtUtc"=clock_timestamp()
            WHERE "ActivationProfile"='RawExportDeliveryRecipientV2';

            UPDATE tagekyc.api_keys
            SET "ScopesJson"='["business.raw-export.package.download","business.raw-export.package.references.read"]'
            WHERE "CallerCategory"='BusinessConsumer'
              AND "ScopesJson"='["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]';

            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              ADD CONSTRAINT ck_raw_export_managed_recipient_policy_shape
              CHECK ("ActivationProfile" = 'C3C4RecipientV1'
                AND octet_length("ActivationScopesDigest") = 32
                AND "State" IN ('Active','Disabled') AND "Revision" > 0);

            DO $delivery$
            DECLARE
              function_name text;
              definition text;
              predecessor text;
            BEGIN
              FOREACH function_name IN ARRAY ARRAY[
                'raw_export_replace_recipient_credential',
                'raw_export_revoke_recipient_credential',
                'raw_export_enroll_managed_recipient',
                'raw_export_issue_recipient_credential',
                'raw_export_read_recipient_activation_readiness'
              ]
              LOOP
                SELECT pg_get_functiondef(p.oid) INTO STRICT definition
                FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname=function_name;

                predecessor := replace(definition,
                  'dff4f677943b718b7f115462812f39951e0df81344ed0d70b65fdf1ad3f92133',
                  '406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91');
                predecessor := replace(predecessor,
                  'RawExportDeliveryRecipientV2', 'C3C4RecipientV1');
                predecessor := replace(predecessor,
                  '["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]',
                  '["business.raw-export.package.download","business.raw-export.package.references.read"]');

                IF predecessor = definition THEN
                  RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_CREDENTIAL_FUNCTION_NOT_RESTORED:%', function_name;
                END IF;
                EXECUTE predecessor;
              END LOOP;
            END
            $delivery$;
            """);
    }
}
