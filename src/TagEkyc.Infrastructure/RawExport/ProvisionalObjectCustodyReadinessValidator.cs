using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class ProvisionalObjectCustodyReadinessException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class ProvisionalObjectCustodyReadinessValidator(
    TagEkycDbContext db,
    ProvisionalObjectCustodyOptions options,
    IServiceProvider services)
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID",
        "PROD_RAW_EXPORT_OBJECT_LIMITS_INVALID",
        "PROD_RAW_EXPORT_OBJECT_ENDPOINT_INVALID",
        "PROD_RAW_EXPORT_OBJECT_CREDENTIAL_SOURCE_INVALID",
        "PROD_RAW_EXPORT_OBJECT_DATABASE_ACL_INVALID",
        "PROD_RAW_EXPORT_OBJECT_BUCKET_UNAVAILABLE",
        "PROD_RAW_EXPORT_OBJECT_VERSIONING_PROHIBITED",
        "PROD_RAW_EXPORT_OBJECT_LOCK_PROHIBITED",
        "PROD_RAW_EXPORT_OBJECT_LIFECYCLE_PROHIBITED",
        "PROD_RAW_EXPORT_OBJECT_PUBLIC_ACCESS_PROHIBITED",
        "PROD_RAW_EXPORT_OBJECT_CAPABILITY_INVALID",
        "PROD_RAW_EXPORT_OBJECT_GOV_ART_INACTIVE",
        "PROD_RAW_EXPORT_OBJECT_OPERATION_UNAVAILABLE",
        "PROD_RAW_EXPORT_OBJECT_R2_DEPENDENCY_UNAVAILABLE",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (!options.TopologyConfigurationValid || !Enum.IsDefined(options.Topology))
            throw new ProvisionalObjectCustodyReadinessException(Codes[0]);
        if (!options.LimitsConfigurationValid
            || options.MaximumSinglePartCiphertextBytes != ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes
            || options.OperationTimeout != ProvisionalObjectCustodyOptions.FixedOperationTimeout)
            throw new ProvisionalObjectCustodyReadinessException(Codes[1]);
        if (!options.EndpointConfigurationValid)
            throw new ProvisionalObjectCustodyReadinessException(Codes[2]);
        if (!options.CredentialConfigurationValid)
            throw new ProvisionalObjectCustodyReadinessException(Codes[3]);
        if (options.Topology == ProvisionalObjectTopology.Disabled) return;
        if (!options.IsSyntacticallyValid || options.Topology != ProvisionalObjectTopology.S3CompatibleDurable)
            throw new ProvisionalObjectCustodyReadinessException(Codes[0]);
        if (options.ServiceUrl is null || options.ServiceUrl.Scheme == Uri.UriSchemeHttp
            && (!options.AllowLoopbackHttp || !options.ServiceUrl.IsLoopback))
            throw new ProvisionalObjectCustodyReadinessException(Codes[2]);
        if (string.IsNullOrWhiteSpace(options.AccessKeyId) || string.IsNullOrWhiteSpace(options.SecretAccessKey))
            throw new ProvisionalObjectCustodyReadinessException(Codes[3]);

        var catalogOk = await db.Database.SqlQueryRaw<bool>("""
            WITH function_manifest(signature, security_definer, exact_config) AS (VALUES
              ('tagekyc.compute_raw_export_provisional_object_binding(uuid,uuid,uuid,uuid,bigint,bigint,bytea,text)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.enforce_raw_export_provisional_object_write()',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.enforce_raw_export_provisional_event_append()',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)',true,ARRAY['search_path=pg_catalog']::text[]),
              ('tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)',true,ARRAY['search_path=pg_catalog']::text[])
            ), expected_acl(signature, grantee) AS (VALUES
              ('tagekyc.raw_export_begin_provisional_object_custody(uuid,bigint,bigint,integer)','tagekyc_raw_export_custody_encryptor'),
              ('tagekyc.raw_export_arm_provisional_object_put(uuid,bigint,uuid)','tagekyc_raw_export_custody_encryptor'),
              ('tagekyc.raw_export_record_provisional_object_not_armed(uuid,bigint,bytea)','tagekyc_raw_export_custody_encryptor'),
              ('tagekyc.raw_export_record_provisional_object_put_result(uuid,bigint,uuid,text,integer,bigint,bytea,bytea)','tagekyc_raw_export_custody_encryptor'),
              ('tagekyc.raw_export_resolve_provisional_object_put_outcome(uuid,bigint,text,bigint,bytea,timestamptz,timestamptz,bytea)','tagekyc_raw_export_reconciler'),
              ('tagekyc.raw_export_mark_provisional_object_verified(uuid,bigint,bytea)','tagekyc_raw_export_reconciler'),
              ('tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)','tagekyc_raw_export_reconciler'),
              ('tagekyc.raw_export_mark_provisional_object_cleanup_required(uuid,bigint,text,bytea)','tagekyc_raw_export_lifecycle'),
              ('tagekyc.raw_export_record_provisional_object_delete_acknowledged(uuid,bigint,integer,bytea)','tagekyc_raw_export_lifecycle'),
              ('tagekyc.raw_export_record_provisional_object_absence_confirmed(uuid,bigint,timestamptz,timestamptz,bytea)','tagekyc_raw_export_reconciler'),
              ('tagekyc.raw_export_record_provisional_object_quarantined(uuid,bigint,text,bytea)','tagekyc_raw_export_lifecycle'),
              ('tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)','tagekyc_raw_export_reconciler'),
              ('tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)','tagekyc_raw_export_lifecycle')
            ), actual_acl AS (
              SELECT f.signature AS signature,
                     grantee.rolname AS grantee,
                     grantor.rolname AS grantor,
                     acl.privilege_type,
                     acl.is_grantable
              FROM function_manifest f
              JOIN pg_catalog.pg_proc p ON p.oid=pg_catalog.to_regprocedure(f.signature)
              CROSS JOIN LATERAL pg_catalog.aclexplode(
                COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) acl
              JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
              JOIN pg_catalog.pg_roles grantor ON grantor.oid=acl.grantor
              WHERE acl.grantee<>p.proowner
            ), expected_members(member_name, role_name) AS (VALUES
              ('tagekyc_raw_export_encryptor_login','tagekyc_raw_export_custody_encryptor'),
              ('tagekyc_raw_export_reconciler_login','tagekyc_raw_export_reconciler'),
              ('tagekyc_raw_export_lifecycle_login','tagekyc_raw_export_lifecycle')
            ), actual_members AS (
              SELECT member.rolname AS member_name, role.rolname AS role_name,
                     m.admin_option, m.inherit_option, m.set_option
              FROM pg_catalog.pg_auth_members m
              JOIN pg_catalog.pg_roles member ON member.oid=m.member
              JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
              WHERE member.rolname IN (
                      'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                      'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login')
                 OR role.rolname IN (
                      'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                      'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login')
            )
            SELECT
              (SELECT count(*)=2 AND pg_catalog.bool_and(owner.rolname='tagekyc_raw_export_deployer' AND c.relkind='r')
               FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
               JOIN pg_catalog.pg_roles owner ON owner.oid=c.relowner
               WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events'))
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                CROSS JOIN LATERAL pg_catalog.aclexplode(
                  COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) acl
                WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events')
                  AND acl.grantee<>c.relowner)
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_provisional_objects','raw_export_provisional_object_events')
                  AND a.attnum>0 AND NOT a.attisdropped AND a.attacl IS NOT NULL)
              AND (SELECT count(*)=15 AND pg_catalog.bool_and(p.oid IS NOT NULL
                       AND owner.rolname='tagekyc_raw_export_deployer'
                       AND p.prosecdef=f.security_definer
                       AND p.proconfig IS NOT DISTINCT FROM f.exact_config)
                   FROM function_manifest f
                   LEFT JOIN pg_catalog.pg_proc p ON p.oid=pg_catalog.to_regprocedure(f.signature)
                   LEFT JOIN pg_catalog.pg_roles owner ON owner.oid=p.proowner)
              AND (SELECT count(*)=15 FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                   WHERE n.nspname='tagekyc' AND p.proname IN (
                     'compute_raw_export_provisional_object_binding','enforce_raw_export_provisional_object_write',
                     'enforce_raw_export_provisional_event_append','raw_export_begin_provisional_object_custody',
                     'raw_export_arm_provisional_object_put','raw_export_record_provisional_object_not_armed',
                     'raw_export_record_provisional_object_put_result','raw_export_resolve_provisional_object_put_outcome',
                     'raw_export_mark_provisional_object_verified','raw_export_mark_provisional_object_cleanup_required',
                     'raw_export_record_provisional_object_delete_acknowledged','raw_export_record_provisional_object_absence_confirmed',
                     'raw_export_record_provisional_object_quarantined','raw_export_read_provisional_object_reconcile_context',
                     'raw_export_read_provisional_object_lifecycle_context'))
              AND NOT EXISTS (
                (SELECT signature,grantee,'tagekyc_raw_export_deployer'::text,'EXECUTE'::text,false FROM expected_acl
                 EXCEPT SELECT signature,grantee,grantor,privilege_type,is_grantable FROM actual_acl)
                UNION ALL
                (SELECT signature,grantee,grantor,privilege_type,is_grantable FROM actual_acl
                 EXCEPT SELECT signature,grantee,'tagekyc_raw_export_deployer'::text,'EXECUTE'::text,false FROM expected_acl))
              AND (SELECT count(*)=6 AND pg_catalog.bool_and(NOT r.rolsuper AND NOT r.rolcreatedb
                         AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls AND r.rolinherit
                         AND r.rolcanlogin=(r.rolname LIKE '%_login'))
                   FROM pg_catalog.pg_roles r WHERE r.rolname IN (
                     'tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                     'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login'))
              AND NOT EXISTS (
                (SELECT member_name,role_name,false,true,false FROM expected_members
                 EXCEPT SELECT member_name,role_name,admin_option,inherit_option,set_option FROM actual_members)
                UNION ALL
                (SELECT member_name,role_name,admin_option,inherit_option,set_option FROM actual_members
                 EXCEPT SELECT member_name,role_name,false,true,false FROM expected_members))
              AS "Value"
            """).SingleAsync(cancellationToken).ConfigureAwait(false);
        if (!catalogOk) throw new ProvisionalObjectCustodyReadinessException(Codes[4]);

        var expected = options.Capability switch
        {
            ProvisionalObjectCapability.Writer => typeof(IProvisionalObjectWriter),
            ProvisionalObjectCapability.Reconciler => typeof(IProvisionalObjectReconciler),
            ProvisionalObjectCapability.Lifecycle => typeof(IProvisionalObjectLifecycle),
            ProvisionalObjectCapability.PostureProbe => typeof(IProvisionalObjectPostureProbe),
            _ => null,
        };
        if (expected is null || services.GetService(expected) is null)
            throw new ProvisionalObjectCustodyReadinessException(Codes[10]);
        var all = new[] { typeof(IProvisionalObjectWriter), typeof(IProvisionalObjectReconciler), typeof(IProvisionalObjectLifecycle), typeof(IProvisionalObjectPostureProbe) };
        if (all.Count(type => services.GetService(type) is not null) != 1)
            throw new ProvisionalObjectCustodyReadinessException(Codes[10]);

        if (options.Capability == ProvisionalObjectCapability.PostureProbe)
        {
            var posture = await ((IProvisionalObjectPostureProbe)services.GetService(expected)!)
                .InspectBucketPostureAsync(cancellationToken).ConfigureAwait(false);
            if (!posture.BucketAvailable) throw new ProvisionalObjectCustodyReadinessException(Codes[5]);
            if (!posture.ObjectLockAbsent) throw new ProvisionalObjectCustodyReadinessException(Codes[7]);
            if (!posture.VersioningNeverEnabled) throw new ProvisionalObjectCustodyReadinessException(Codes[6]);
            if (!posture.LifecycleRulesAbsent) throw new ProvisionalObjectCustodyReadinessException(Codes[8]);
            if (!posture.PublicAccessAbsent) throw new ProvisionalObjectCustodyReadinessException(Codes[9]);
        }

        // Gate A is fixture-only. Production activation record and corrected R2 remain separate slices.
        throw new ProvisionalObjectCustodyReadinessException(Codes[11]);
    }
}
