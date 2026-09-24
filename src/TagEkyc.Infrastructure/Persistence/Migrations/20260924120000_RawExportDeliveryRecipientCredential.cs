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
            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              ADD CONSTRAINT ck_raw_export_managed_recipient_policy_shape
              CHECK ("ActivationProfile" IN ('C3C4RecipientV1','RawExportDeliveryRecipientV2')
                AND octet_length("ActivationScopesDigest") = 32
                AND "State" IN ('Active','Disabled') AND "Revision" > 0);

            DO $delivery$
            DECLARE definition text; successor text;
            BEGIN
              SELECT pg_get_functiondef(p.oid) INTO STRICT definition
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname='raw_export_enroll_managed_recipient';
              successor:=replace(definition,'p_principal uuid)','p_principal uuid, p_activation_profile text)');
              successor:=replace(successor,
                'active_scope bytea:=pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'');',
                'active_scope bytea:=CASE p_activation_profile WHEN ''C3C4RecipientV1'' THEN pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'') WHEN ''RawExportDeliveryRecipientV2'' THEN pg_catalog.decode(''dff4f677943b718b7f115462812f39951e0df81344ed0d70b65fdf1ad3f92133'',''hex'') END;');
              successor:=replace(successor,
                'VALUES(p_recipient,''C3C4RecipientV1'',active_scope,''Active'',1,v_now,v_now);',
                'VALUES(p_recipient,p_activation_profile,active_scope,''Active'',1,v_now,v_now);');
              successor:=replace(successor,E'\nBEGIN\n',E'\nBEGIN\n  IF p_activation_profile NOT IN (''C3C4RecipientV1'',''RawExportDeliveryRecipientV2'') THEN\n    RAISE EXCEPTION USING ERRCODE=''P0001'',MESSAGE=''RAW_EXPORT_RECIPIENT_MANAGEMENT_ARGUMENT_INVALID'';\n  END IF;\n');
              IF successor=definition THEN RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_ENROLL_OVERLOAD_NOT_CREATED'; END IF;
              EXECUTE successor;

              SELECT pg_get_functiondef(p.oid) INTO STRICT definition
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname='raw_export_issue_recipient_credential';
              successor:=replace(definition,'p_expires timestamp with time zone)','p_expires timestamp with time zone, p_profile_version integer)');
              successor:=replace(successor,
                'active_scope bytea:=pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'');',
                'active_scope bytea:=(SELECT p."ActivationScopesDigest" FROM tagekyc.raw_export_managed_recipient_policies p WHERE p."RecipientClientApplicationId"=p_recipient AND p."State"=''Active'');');
              successor:=replace(successor,
                'p."ActivationProfile"=''C3C4RecipientV1'' AND p."ActivationScopesDigest"=active_scope',
                'p."ActivationProfile" IN (''C3C4RecipientV1'',''RawExportDeliveryRecipientV2'') AND p."ActivationScopesDigest"=active_scope');
              successor:=replace(successor,
                '''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb',
                '(SELECT CASE p."ActivationProfile" WHEN ''C3C4RecipientV1'' THEN ''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb WHEN ''RawExportDeliveryRecipientV2'' THEN ''["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb END FROM tagekyc.raw_export_managed_recipient_policies p WHERE p."RecipientClientApplicationId"=p_recipient AND p."State"=''Active'')');
              IF successor=definition THEN RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_ISSUE_OVERLOAD_NOT_CREATED'; END IF;
              EXECUTE successor;

              SELECT pg_get_functiondef(p.oid) INTO STRICT definition
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname='raw_export_replace_recipient_credential';
              successor:=replace(definition,'p_reason text)','p_reason text, p_profile_version integer)');
              successor:=replace(successor,
                'active_scope bytea:=pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'');',
                'active_scope bytea:=CASE (SELECT a."ScopesJson" FROM tagekyc.api_keys a WHERE a."ApiKeyId"=p_current_api_key) WHEN ''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb THEN pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'') WHEN ''["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb THEN pg_catalog.decode(''dff4f677943b718b7f115462812f39951e0df81344ed0d70b65fdf1ad3f92133'',''hex'') END;');
              successor:=replace(successor,
                '''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb',
                '(SELECT a."ScopesJson" FROM tagekyc.api_keys a WHERE a."ApiKeyId"=p_current_api_key)');
              IF successor=definition THEN RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_REPLACE_OVERLOAD_NOT_CREATED'; END IF;
              EXECUTE successor;

              SELECT pg_get_functiondef(p.oid) INTO STRICT definition
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname='raw_export_revoke_recipient_credential';
              successor:=replace(definition,'p_reason text)','p_reason text, p_profile_version integer)');
              successor:=replace(successor,
                'active_scope bytea:=pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'');',
                'active_scope bytea:=CASE (SELECT a."ScopesJson" FROM tagekyc.api_keys a WHERE a."ApiKeyId"=p_api_key) WHEN ''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb THEN pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'') WHEN ''["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb THEN pg_catalog.decode(''dff4f677943b718b7f115462812f39951e0df81344ed0d70b65fdf1ad3f92133'',''hex'') END;');
              IF successor=definition THEN RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_REVOKE_OVERLOAD_NOT_CREATED'; END IF;
              EXECUTE successor;

              SELECT pg_get_functiondef(p.oid) INTO STRICT definition
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname='raw_export_read_recipient_activation_readiness';
              successor:=replace(definition,'p_recipient uuid)','p_recipient uuid, p_profile_version integer)');
              successor:=replace(successor,
                'active_scope bytea:=pg_catalog.decode(''406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91'',''hex'');',
                'active_scope bytea:=(SELECT p."ActivationScopesDigest" FROM tagekyc.raw_export_managed_recipient_policies p WHERE p."RecipientClientApplicationId"=p_recipient AND p."State"=''Active'');');
              successor:=replace(successor,
                'p."ActivationProfile"=''C3C4RecipientV1'' AND p."ActivationScopesDigest"=active_scope',
                'p."ActivationProfile" IN (''C3C4RecipientV1'',''RawExportDeliveryRecipientV2'') AND p."ActivationScopesDigest"=active_scope');
              successor:=replace(successor,
                'a."ScopesJson"=''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb',
                'a."ScopesJson"=(SELECT CASE p."ActivationProfile" WHEN ''C3C4RecipientV1'' THEN ''["business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb WHEN ''RawExportDeliveryRecipientV2'' THEN ''["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]''::jsonb END FROM tagekyc.raw_export_managed_recipient_policies p WHERE p."RecipientClientApplicationId"=p_recipient AND p."State"=''Active'')');
              IF successor=definition THEN RAISE EXCEPTION 'RAW_EXPORT_DELIVERY_READINESS_OVERLOAD_NOT_CREATED'; END IF;
              EXECUTE successor;
            END
            $delivery$;

            ALTER FUNCTION tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz,integer) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text,integer) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text,integer) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(uuid,integer) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz,integer) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text,integer) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text,integer) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(uuid,integer) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text) TO tagekyc_raw_export_recipient_manager;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz,integer) TO tagekyc_raw_export_recipient_manager;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text,integer) TO tagekyc_raw_export_recipient_manager;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text,integer) TO tagekyc_raw_export_recipient_manager;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(uuid,integer) TO tagekyc_raw_export_recipient_manager;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP FUNCTION IF EXISTS tagekyc.raw_export_read_recipient_activation_readiness(uuid,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text);

            UPDATE tagekyc.raw_export_managed_recipient_policies
            SET "ActivationProfile"='C3C4RecipientV1',
                "ActivationScopesDigest"=pg_catalog.decode('406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex'),
                "Revision"="Revision"+1,"UpdatedAtUtc"=clock_timestamp()
            WHERE "ActivationProfile"='RawExportDeliveryRecipientV2';
            UPDATE tagekyc.api_keys
            SET "ScopesJson"='["business.raw-export.package.download","business.raw-export.package.references.read"]'
            WHERE "CallerCategory"='BusinessConsumer'
              AND "ScopesJson"='["business.raw-export.authorize","business.raw-export.job.manage","business.raw-export.package.download","business.raw-export.package.references.read"]';
            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              DROP CONSTRAINT ck_raw_export_managed_recipient_policy_shape;
            ALTER TABLE tagekyc.raw_export_managed_recipient_policies
              ADD CONSTRAINT ck_raw_export_managed_recipient_policy_shape
              CHECK ("ActivationProfile" = 'C3C4RecipientV1'
                AND octet_length("ActivationScopesDigest") = 32
                AND "State" IN ('Active','Disabled') AND "Revision" > 0);
            """);
    }
}
