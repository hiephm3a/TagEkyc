using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportSubjectConsentReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportSubjectConsentReadinessValidator(TagEkycDbContext dbContext)
{
    public const string TableMutationPrivilege = "PROD_RAW_EXPORT_SUBJECT_CONSENT_TABLE_MUTATION_PRIVILEGE";

    public const string FunctionAclInvalid = "PROD_RAW_EXPORT_SUBJECT_CONSENT_FUNCTION_ACL_INVALID";

    private static readonly string[] EventTables =
    [
        "raw_export_subject_consent_events",
        "raw_export_subject_consent_classes",
        "raw_export_subject_consent_authorities",
    ];

    private static readonly string[] MutationPrivileges =
    [
        "INSERT",
        "UPDATE",
        "DELETE",
        "TRUNCATE",
    ];

    private static readonly FunctionExpectation[] ExpectedFunctions =
    [
        new("tagekyc.enforce_raw_export_subject_consent_child_same_transaction()", false, false),
        new("tagekyc.enforce_raw_export_subject_consent_granted_has_classes()", false, false),
        new("tagekyc.enforce_raw_export_subject_consent_insert()", false, false),
        new("tagekyc.raw_export_append_subject_consent_authority(authority_principal_id uuid, client_application_id uuid, authority_type text, expected_revision integer, event_type text, target_revision integer, decision_ref text, valid_until_utc timestamp with time zone)", true, false),
        new("tagekyc.raw_export_append_subject_consent_granted(verification_session_id uuid, policy_id uuid, policy_version integer, raw_classes text[], consent_text_version text, consent_text_content_hash text, external_consent_artifact_ref text, decision_ref text, valid_until_utc timestamp with time zone)", true, true),
        new("tagekyc.raw_export_append_subject_consent_withdrawn(verification_session_id uuid, policy_id uuid, policy_version integer, expected_revision integer, target_revision integer, decision_ref text, external_consent_artifact_ref text)", true, true),
        new("tagekyc.raw_export_consent_lock_key(scope_hash bytea)", false, false),
        new("tagekyc.raw_export_consent_scope_hash(verification_session_id uuid, subject_ref text, policy_id uuid, policy_version integer, purpose_code text, recipient_client_application_id uuid)", false, false),
        new("tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id uuid)", true, false),
        new("tagekyc.raw_export_resolve_subject_consent_for_authorization(verification_session_id uuid, policy_id uuid, policy_version integer)", true, true),
        new("tagekyc.raw_export_subject_consent_has_current_authority(actor_id uuid, client_application_id uuid, required_authority text)", true, false),
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        foreach (var table in EventTables)
        {
            foreach (var privilege in MutationPrivileges)
            {
                if (await HasTablePrivilegeAsync(table, privilege, cancellationToken))
                {
                    throw new RawExportSubjectConsentReadinessException(TableMutationPrivilege);
                }
            }
        }

        await ValidateFunctionAclAsync(cancellationToken);
    }

    private async Task<bool> HasTablePrivilegeAsync(
        string table,
        string privilege,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT has_table_privilege(current_user, @table, @privilege)";
        command.Parameters.Add(Parameter("table", $"tagekyc.{table}"));
        command.Parameters.Add(Parameter("privilege", privilege));
        return await command.ExecuteScalarAsync(cancellationToken) is bool hasPrivilege && hasPrivilege;
    }

    private async Task ValidateFunctionAclAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT n.nspname || '.' || p.proname || '(' || pg_catalog.pg_get_function_identity_arguments(p.oid) || ')',
                   p.prosecdef,
                   owner.rolname,
                   owner.rolcanlogin,
                   COALESCE(array_to_string(p.proconfig, ','), ''),
                   has_function_privilege('public', p.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc'
              AND (
                p.proname LIKE '%subject_consent%'
                OR p.proname IN ('raw_export_consent_scope_hash','raw_export_consent_lock_key','raw_export_lock_verification_session_for_subject_consent'))
            ORDER BY p.oid::regprocedure::text;
            """;

        var rows = new Dictionary<string, FunctionInfo>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows[reader.GetString(0)] = new FunctionInfo(
                reader.GetBoolean(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6));
        }

        if (!rows.Keys.Order(StringComparer.Ordinal).SequenceEqual(
                ExpectedFunctions.Select(function => function.Signature).Order(StringComparer.Ordinal),
                StringComparer.Ordinal))
        {
            throw new RawExportSubjectConsentReadinessException(FunctionAclInvalid);
        }

        foreach (var expected in ExpectedFunctions)
        {
            if (!rows.TryGetValue(expected.Signature, out var actual) ||
                actual.SecurityDefiner != expected.SecurityDefiner ||
                actual.Owner != "tagekyc_raw_export_deployer" ||
                actual.OwnerCanLogin ||
                actual.Config != "search_path=pg_catalog" ||
                actual.PublicExecute ||
                actual.RuntimeExecute != expected.RuntimeExecute)
            {
                throw new RawExportSubjectConsentReadinessException(FunctionAclInvalid);
            }
        }
    }

    private static System.Data.Common.DbParameter Parameter(string name, object value)
    {
        var parameter = new Npgsql.NpgsqlParameter(name, value);
        return parameter;
    }

    private sealed record FunctionExpectation(
        string Signature,
        bool SecurityDefiner,
        bool RuntimeExecute);

    private sealed record FunctionInfo(
        bool SecurityDefiner,
        string Owner,
        bool OwnerCanLogin,
        string Config,
        bool PublicExecute,
        bool RuntimeExecute);
}
