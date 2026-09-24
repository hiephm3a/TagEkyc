using Npgsql;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Infrastructure.Persistence;

// Activated-only catalogue check for the additive A3 surface.  It reads only
// PostgreSQL catalogues; it never treats live ACLs as the source of the
// expected manifest and never reads retained business rows.
public sealed class RawSourceRetentionReadinessValidator(
    string connectionString,
    RawSourceRetentionProfileValidator profiles,
    ConfiguredRawExportCaptureAcceptancePolicyProvider acceptancePolicies)
{
    private static readonly string[] Tables =
    [
        "raw_source_consent_references",
        "raw_source_consent_reference_events",
        "raw_source_consent_bindings",
        "raw_source_retention_permits",
        "raw_source_retention_permit_classes"
    ];

    private static readonly string[] Functions =
    [
        "raw_source_record_consent_reference(uuid,uuid,uuid,text,text,bigint,text,text,timestamp with time zone,timestamp with time zone,uuid,bytea)",
        "raw_source_withdraw_consent_reference(uuid,uuid,uuid,bigint,text,text,uuid,bytea)",
        "raw_source_issue_retention_authority(uuid,uuid,uuid,uuid,uuid,integer,text[],text,text,text,integer,text,text,text,text,integer,uuid,bytea)",
        "raw_source_resolve_retention_authority(uuid,bigint,uuid,uuid,uuid,text,timestamp with time zone)",
        "raw_export_read_retained_source_continuation(uuid)",
        "raw_export_list_retained_source_continuations(uuid,integer)",
        "raw_export_record_retained_r2_terminal_intent(uuid,uuid,bigint,bigint,text,text)",
        "raw_export_finalize_retained_r2_terminal(uuid,uuid,bigint,bigint)",
        "raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)",
        "raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer)",
        "raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid)"
    ];

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        if (!profiles.IsReady || profiles.ClientApplicationIds.Any(
            client => !acceptancePolicies.ClientApplicationIds.Contains(client))) return false;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH expected_tables(name) AS (SELECT unnest(@tables::text[])),
            expected_functions(signature) AS (SELECT unnest(@functions::text[])),
            resolved AS (
              SELECT signature,
                     to_regprocedure('tagekyc.' || signature) AS oid
              FROM expected_functions
            )
            SELECT
              (SELECT count(*) = cardinality(@tables::text[])
                 FROM expected_tables
                WHERE to_regclass('tagekyc.' || name) IS NOT NULL)
              AND
              (SELECT count(*) = cardinality(@functions::text[])
                 FROM resolved r
                 JOIN pg_proc p ON p.oid = r.oid
                 JOIN pg_roles owner_role ON owner_role.oid = p.proowner
                WHERE owner_role.rolname = 'tagekyc_raw_export_deployer'
                  AND p.prosecdef
                  AND p.proconfig = ARRAY['search_path=pg_catalog']::text[]
                  AND NOT has_function_privilege('public', p.oid, 'EXECUTE'))
              AND has_function_privilege('tagekyc_runtime',
                    'tagekyc.raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer)', 'EXECUTE')
              AND has_function_privilege('tagekyc_runtime',
                    'tagekyc.raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid)', 'EXECUTE')
              AND NOT has_table_privilege('tagekyc_runtime',
                    'tagekyc.raw_source_consent_references', 'SELECT,INSERT,UPDATE,DELETE')
              AND NOT has_table_privilege('tagekyc_runtime',
                    'tagekyc.raw_source_consent_reference_events', 'SELECT,INSERT,UPDATE,DELETE')
              AND NOT has_table_privilege('tagekyc_runtime',
                    'tagekyc.raw_source_consent_bindings', 'SELECT,INSERT,UPDATE,DELETE')
              AND NOT has_table_privilege('tagekyc_runtime',
                    'tagekyc.raw_source_retention_permits', 'SELECT,INSERT,UPDATE,DELETE')
              AND NOT has_table_privilege('tagekyc_runtime',
                    'tagekyc.raw_source_retention_permit_classes', 'SELECT,INSERT,UPDATE,DELETE') AS "Value";
            """;
        command.Parameters.AddWithValue("tables", Tables);
        command.Parameters.AddWithValue("functions", Functions);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
    }
}
