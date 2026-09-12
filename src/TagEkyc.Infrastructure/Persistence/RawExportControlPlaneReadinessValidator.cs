using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportControlPlaneReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class RawExportControlPlaneReadinessValidator(TagEkycDbContext dbContext)
{
    public const string RootAuthorityMissing = "PROD_RAW_EXPORT_ROOT_AUTHORITY_MISSING";

    public const string DevDefaultPrincipalForbidden = "PROD_RAW_EXPORT_ROOT_AUTHORITY_DEV_DEFAULT";

    public const string EventTableMutationPrivilege = "PROD_RAW_EXPORT_CONTROL_PLANE_TABLE_MUTATION_PRIVILEGE";

    public const string FunctionAclInvalid = "PROD_RAW_EXPORT_CONTROL_PLANE_FUNCTION_ACL_INVALID";

    public const string DeploymentRoleInvalid = "PROD_RAW_EXPORT_CONTROL_PLANE_DEPLOYMENT_ROLE_INVALID";

    public const string ForbiddenTablePrivilege =
        "PROD_RAW_EXPORT_CONTROL_PLANE_FORBIDDEN_TABLE_PRIVILEGE";

    private static readonly string[] EventTables =
    [
        "raw_export_grants",
        "raw_export_control_authorities",
        "raw_export_fulfillments",
        "raw_export_policy_lifecycle",
        "raw_export_capture_acceptance_events",
        "raw_export_session_capture_selections",
    ];

    private static readonly string[] MutationPrivileges =
    [
        "INSERT",
        "UPDATE",
        "DELETE",
    ];

    private static readonly string[] ForbiddenTables =
    [
        "raw_export_policy_versions",
        "raw_export_policy_allowed_classes",
        "raw_export_policy_requirements",
        "raw_export_policy_closures",
        "raw_export_requirement_rule_sets",
        "raw_export_grants",
        "raw_export_fulfillments",
        "raw_export_policy_lifecycle",
        "verification_sessions",
        "raw_export_subject_consent_authorities",
        "raw_export_subject_consent_events",
        "raw_export_subject_consent_classes",
        "raw_export_requirement_rules",
        "raw_export_control_authorities",
        "raw_export_capture_acceptance_events",
        "raw_export_session_capture_selections",
        "raw_export_source_ingress_claims",
        "raw_export_source_ingress_claim_aliases",
    ];

    private static readonly string[] ForbiddenPrivileges =
    [
        "SELECT",
        "INSERT",
        "UPDATE",
        "DELETE",
        "TRUNCATE",
        "REFERENCES",
        "TRIGGER",
    ];

    private static readonly string[] RequiredCapabilityRoles =
    [
        "tagekyc_runtime",
        "tagekyc_raw_export_deployer",
        "tagekyc_raw_export_bootstrapper",
    ];

    private static readonly FunctionBackingReadExpectation[] FunctionBackingReads =
    [
        new(
            "tagekyc.raw_export_read_authorization_eligibility_inputs(principal_id uuid, policy_id uuid, policy_version integer)",
            [
                "raw_export_policy_versions",
                "raw_export_requirement_rule_sets",
                "raw_export_policy_closures",
                "raw_export_grants",
                "raw_export_policy_lifecycle",
                "raw_export_policy_requirements",
                "raw_export_fulfillments",
            ]),
        new(
            "tagekyc.raw_export_read_authorization_policy_inputs(principal_id uuid, policy_id uuid, policy_version integer)",
            [
                "raw_export_policy_versions",
                "raw_export_policy_closures",
                "raw_export_policy_allowed_classes",
            ]),
        new(
            "tagekyc.raw_export_control_plane_root_health()",
            ["raw_export_control_authorities"]),
        new(
            "tagekyc.raw_export_append_capture_acceptance(verification_session_id uuid, client_application_id uuid, raw_class text, capture_artifact_id uuid, capture_revision integer, session_challenge_hash text, accepted_evidence_ref text, acceptance_policy_id text, acceptance_policy_version integer)",
            [
                "capture_artifacts",
                "raw_export_capture_acceptance_events",
            ]),
        new(
            "tagekyc.raw_export_select_session_capture_acceptance(verification_session_id uuid, raw_class text, capture_acceptance_id uuid)",
            ["raw_export_capture_acceptance_events"]),
        new(
            "tagekyc.begin_raw_export_source_ingress_claim(p_authenticated_principal_id uuid, p_client_application_id uuid, p_producer_id text, p_capture_agent_instance_id text, p_ingress_idempotency_key text, p_verification_session_id uuid, p_capture_acceptance_id uuid, p_capture_artifact_id uuid, p_capture_revision integer, p_raw_class text, p_session_challenge_hash text, p_authority_snapshot_id text, p_claimed_plaintext_length bigint, p_media_type text, p_captured_at_utc timestamp with time zone, p_plaintext_retention_started_at_utc timestamp with time zone, p_plaintext_retention_expires_at_utc timestamp with time zone, p_plaintext_retention_budget_seconds integer, p_commitment_key_selector_id text, p_commitment_key_selector_version integer, p_claim_evaluation_owner_id uuid, p_claim_evaluation_token_ttl_seconds integer, p_idempotency_lock_timeout_milliseconds integer)",
            [
                "verification_sessions",
                "capture_artifacts",
                "raw_export_capture_acceptance_events",
                "raw_export_source_ingress_claims",
                "raw_export_source_ingress_claim_aliases",
            ]),
        new(
            "tagekyc.validate_raw_export_claim_evaluation_token(p_client_application_id uuid, p_producer_id text, p_capture_agent_instance_id text, p_ingress_idempotency_key text, p_claim_evaluation_id uuid, p_claim_evaluation_revision bigint, p_claim_evaluation_fence bigint, p_token_variant text, p_token_expires_at_utc timestamp with time zone, p_claim_evaluation_token text)",
            [
                "raw_export_source_ingress_claims",
                "raw_export_source_ingress_claim_aliases",
            ]),
    ];

    private static readonly DeployerTableExpectation[] DeployerTableExpectations =
    [
        new("raw_export_control_authorities", true, true),
        new("raw_export_fulfillments", true, true),
        new("raw_export_grants", true, true),
        new("raw_export_policy_allowed_classes", true, false),
        new("raw_export_policy_closures", true, false),
        new("raw_export_policy_lifecycle", true, true),
        new("raw_export_policy_requirements", true, false),
        new("raw_export_policy_versions", true, false),
        new("raw_export_requirement_rule_sets", true, false),
        new("capture_artifacts", true, false),
        new("raw_export_capture_acceptance_events", true, true),
        new("raw_export_session_capture_selections", true, true),
        new("raw_export_source_ingress_claims", true, true, true),
        new("raw_export_source_ingress_claim_aliases", true, true, true),
    ];

    private static readonly FunctionExpectation[] ExpectedFunctions =
    [
        new("tagekyc.enforce_raw_export_control_plane_insert()", false, false, false),
        new("tagekyc.raw_export_activation_gates_hold(policy_id uuid, policy_version integer)", true, false, false),
        new("tagekyc.raw_export_append_control_authority(principal_id uuid, authority_type text, scope_type text, scope_id uuid, requirement_type text, expected_revision integer, event_type text, decision_ref text)", true, true, false),
        new("tagekyc.raw_export_append_fulfillment(policy_id uuid, policy_version integer, requirement_type text, expected_revision integer, event_type text, supersedes_revision integer, target_revision integer, artifact_ref text, artifact_version text, valid_from_utc timestamp with time zone, valid_until_utc timestamp with time zone, decision_ref text)", true, true, false),
        new("tagekyc.raw_export_append_grant(principal_id uuid, policy_id uuid, policy_version integer, expected_revision integer, event_type text, client_application_id uuid, decision_ref text)", true, true, false),
        new("tagekyc.raw_export_append_lifecycle(policy_id uuid, policy_version integer, expected_revision integer, event_type text, decision_ref text)", true, true, false),
        new("tagekyc.raw_export_bootstrap_global_authority(principal_id uuid, authority_type text, decision_ref text)", true, false, true),
        new("tagekyc.raw_export_current_actor()", true, false, false),
        new("tagekyc.raw_export_has_current_authority(actor_id uuid, required_authority text, policy_id uuid, requirement_type text)", true, false, false),
        new("tagekyc.raw_export_policy_exists(policy_id uuid)", true, false, false),
        new("tagekyc.enforce_raw_export_capture_acceptance_insert()", false, false, false),
        new(
            "tagekyc.raw_export_append_capture_acceptance(verification_session_id uuid, client_application_id uuid, raw_class text, capture_artifact_id uuid, capture_revision integer, session_challenge_hash text, accepted_evidence_ref text, acceptance_policy_id text, acceptance_policy_version integer)",
            true,
            false,
            false,
            "uuid",
            "plpgsql"),
        new(
            "tagekyc.raw_export_select_session_capture_acceptance(verification_session_id uuid, raw_class text, capture_acceptance_id uuid)",
            true,
            false,
            false,
            "uuid",
            "plpgsql"),
        new(
            "tagekyc.enforce_raw_export_source_ingress_write()",
            false,
            false,
            false,
            "trigger",
            "plpgsql"),
        new(
            "tagekyc.begin_raw_export_source_ingress_claim(p_authenticated_principal_id uuid, p_client_application_id uuid, p_producer_id text, p_capture_agent_instance_id text, p_ingress_idempotency_key text, p_verification_session_id uuid, p_capture_acceptance_id uuid, p_capture_artifact_id uuid, p_capture_revision integer, p_raw_class text, p_session_challenge_hash text, p_authority_snapshot_id text, p_claimed_plaintext_length bigint, p_media_type text, p_captured_at_utc timestamp with time zone, p_plaintext_retention_started_at_utc timestamp with time zone, p_plaintext_retention_expires_at_utc timestamp with time zone, p_plaintext_retention_budget_seconds integer, p_commitment_key_selector_id text, p_commitment_key_selector_version integer, p_claim_evaluation_owner_id uuid, p_claim_evaluation_token_ttl_seconds integer, p_idempotency_lock_timeout_milliseconds integer)",
            true,
            true,
            false,
            "TABLE(outcome_code text, claim_evaluation_token text, token_variant text, token_expires_at_utc timestamp with time zone, claim_evaluation_id uuid, claim_evaluation_revision bigint, claim_evaluation_fence bigint, retry_not_before_utc timestamp with time zone)",
            "plpgsql"),
        new(
            "tagekyc.validate_raw_export_claim_evaluation_token(p_client_application_id uuid, p_producer_id text, p_capture_agent_instance_id text, p_ingress_idempotency_key text, p_claim_evaluation_id uuid, p_claim_evaluation_revision bigint, p_claim_evaluation_fence bigint, p_token_variant text, p_token_expires_at_utc timestamp with time zone, p_claim_evaluation_token text)",
            true,
            true,
            false,
            "boolean",
            "plpgsql"),
        new(
            "tagekyc.raw_export_control_plane_root_health()",
            true,
            true,
            false,
            "TABLE(\"IsHealthy\" boolean, \"StatusCode\" text)",
            "plpgsql",
            "364393e456b50ec7127dd32261ee45858b2a29753450e27f914afe9f4b49bf46"),
        new(
            "tagekyc.raw_export_read_authorization_eligibility_inputs(principal_id uuid, policy_id uuid, policy_version integer)",
            true,
            true,
            false,
            "TABLE(\"PolicyId\" uuid, \"PolicyVersion\" integer, \"EvaluatedAtUtc\" timestamp with time zone, \"PolicyExists\" boolean, \"BoundRuleSetVersion\" integer, \"CurrentRuleSetVersion\" integer, \"ClosureType\" text, \"GrantPrincipalId\" uuid, \"GrantPolicyId\" uuid, \"GrantPolicyVersion\" integer, \"GrantRevision\" integer, \"GrantEventType\" text, \"LifecyclePolicyId\" uuid, \"LifecyclePolicyVersion\" integer, \"LifecycleRevision\" integer, \"LifecycleEventType\" text, \"RequirementOrdinal\" integer, \"RequirementType\" text, \"FulfillmentEventId\" uuid, \"FulfillmentRevision\" integer, \"FulfillmentEventType\" text, \"ArtifactRef\" text, \"ArtifactVersion\" text, \"ValidFromUtc\" timestamp with time zone, \"ValidUntilUtc\" timestamp with time zone)",
            "plpgsql",
            "b3852ce11556bad68ec4bb6f3ff0b8074fb9eb37e05dda45c2eafcb79c883fcf"),
        new(
            "tagekyc.raw_export_read_authorization_policy_inputs(principal_id uuid, policy_id uuid, policy_version integer)",
            true,
            true,
            false,
            "TABLE(\"PolicyId\" uuid, \"PolicyVersion\" integer, \"EvaluatedAtUtc\" timestamp with time zone, \"PolicyExists\" boolean, \"PermitTtlSeconds\" integer, \"ClosureType\" text, \"ClassOrdinal\" integer, \"RawClass\" text)",
            "plpgsql",
            "9476121061bff3df52c7ae522b27f4f7b2878f70c99f5029883d180f47a39712"),
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        var deployment = await ReadDeploymentFoundationAsync(cancellationToken);
        await ValidateFunctionAclAsync(cancellationToken);
        await ValidateFunctionOwnerBackingReadsAsync(cancellationToken);
        await ValidateDeployerTableCapabilitiesAsync(cancellationToken);
        await ValidateIngressClaimTableAclAsync(cancellationToken);
        await ValidateBoundaryExecutionAvailableAsync(cancellationToken);
        await ValidateRootHealthAsync(cancellationToken);

        if (await IsCurrentUserEventTableOwnerOrSuperuserAsync(
                deployment.CurrentRole,
                cancellationToken) &&
            await HasAnyCurrentUserEventMutationPrivilegeAsync(cancellationToken))
        {
            Throw(EventTableMutationPrivilege);
        }

        await ValidateApplicationLoginAsync(deployment, cancellationToken);

        if (await HasAnyForbiddenRuntimeTablePrivilegeAsync(cancellationToken))
        {
            Throw(ForbiddenTablePrivilege);
        }

        if (await HasAnyCurrentUserEventMutationPrivilegeAsync(cancellationToken))
        {
            Throw(EventTableMutationPrivilege);
        }
    }

    private static void Throw(string code) => throw new RawExportControlPlaneReadinessException(code);

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

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "table";
        tableParameter.Value = $"tagekyc.{table}";
        command.Parameters.Add(tableParameter);

        var privilegeParameter = command.CreateParameter();
        privilegeParameter.ParameterName = "privilege";
        privilegeParameter.Value = privilege;
        command.Parameters.Add(privilegeParameter);

        return await command.ExecuteScalarAsync(cancellationToken) is bool hasPrivilege && hasPrivilege;
    }

    private async Task<bool> HasAnyCurrentUserEventMutationPrivilegeAsync(
        CancellationToken cancellationToken)
    {
        foreach (var table in EventTables)
        {
            foreach (var privilege in MutationPrivileges)
            {
                if (await HasTablePrivilegeAsync(table, privilege, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task<bool> IsCurrentUserEventTableOwnerOrSuperuserAsync(
        RoleInfo currentRole,
        CancellationToken cancellationToken)
    {
        if (currentRole.Superuser)
        {
            return true;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        var tableNames = string.Join(
            ",",
            EventTables.Select(table => $"'{table.Replace("'", "''")}'"));
        command.CommandText = $$"""
            SELECT EXISTS (
                SELECT 1
                FROM pg_class AS relation
                JOIN pg_namespace AS namespace ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname IN ({{tableNames}})
                  AND relation.relowner = current_user::regrole::oid);
            """;

        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private async Task<bool> HasAnyForbiddenRuntimeTablePrivilegeAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        var values = string.Join(
            $",{Environment.NewLine}",
            ForbiddenTables.SelectMany(table =>
                ForbiddenPrivileges.Select(privilege =>
                    $"('{table.Replace("'", "''")}','{privilege.Replace("'", "''")}')")));
        command.CommandText = $$"""
            SELECT COALESCE(bool_or(
                has_table_privilege(
                    'tagekyc_runtime',
                    'tagekyc.' || matrix.table_name,
                    matrix.privilege_name)
                 OR has_table_privilege(
                    current_user,
                    'tagekyc.' || matrix.table_name,
                    matrix.privilege_name)), false)
            FROM (VALUES
                {{values}}
            ) AS matrix(table_name, privilege_name);
            """;

        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private async Task ValidateRootHealthAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "IsHealthy", "StatusCode"
            FROM tagekyc.raw_export_control_plane_root_health();
            """;

        bool healthy;
        string code;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken) ||
                reader.IsDBNull(0) ||
                reader.IsDBNull(1))
            {
                Throw(FunctionAclInvalid);
            }

            healthy = reader.GetBoolean(0);
            code = reader.GetString(1);
            if (await reader.ReadAsync(cancellationToken))
            {
                Throw(FunctionAclInvalid);
            }
        }

        if (healthy && code == "OK")
        {
            return;
        }

        if (!healthy && (code == RootAuthorityMissing || code == DevDefaultPrincipalForbidden))
        {
            Throw(code);
        }

        Throw(FunctionAclInvalid);
    }

    private async Task<DeploymentFoundation> ReadDeploymentFoundationAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT current_setting('server_version_num')::integer,
                   session_user::text,
                   current_user::text;
            """;

        int serverVersion;
        string sessionUser;
        string currentUser;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                Throw(DeploymentRoleInvalid);
            }

            serverVersion = reader.GetInt32(0);
            sessionUser = reader.GetString(1);
            currentUser = reader.GetString(2);
        }

        if (!string.Equals(sessionUser, currentUser, StringComparison.Ordinal))
        {
            Throw(DeploymentRoleInvalid);
        }

        ValidateServerVersion(serverVersion);

        await using var roleCommand = connection.CreateCommand();
        roleCommand.CommandText = """
            SELECT oid, rolname, rolcanlogin, rolinherit, rolsuper, rolcreatedb,
                   rolcreaterole, rolreplication, rolbypassrls
            FROM pg_roles
            WHERE rolname IN (
                'tagekyc_runtime',
                'tagekyc_raw_export_deployer',
                'tagekyc_raw_export_bootstrapper',
                current_user::text);
            """;

        var roles = new Dictionary<string, RoleInfo>(StringComparer.Ordinal);
        {
            await using var reader = await roleCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var role = new RoleInfo(
                    Convert.ToUInt32(reader.GetValue(0)),
                    reader.GetString(1),
                    reader.GetBoolean(2),
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetBoolean(5),
                    reader.GetBoolean(6),
                    reader.GetBoolean(7),
                    reader.GetBoolean(8));
                roles[role.Name] = role;
            }
        }

        foreach (var roleName in RequiredCapabilityRoles)
        {
            if (!roles.TryGetValue(roleName, out var role) ||
                role.CanLogin ||
                !role.Inherit ||
                role.Superuser ||
                role.CreateDatabase ||
                role.CreateRole ||
                role.Replication ||
                role.BypassRls)
            {
                Throw(DeploymentRoleInvalid);
            }
        }

        if (!roles.TryGetValue(currentUser, out var currentRole))
        {
            Throw(DeploymentRoleInvalid);
        }

        return new DeploymentFoundation(
            serverVersion,
            sessionUser,
            currentUser,
            currentRole!,
            roles["tagekyc_runtime"]);
    }

    private static void ValidateServerVersion(int serverVersion)
    {
        if (serverVersion < 160000)
        {
            Throw(DeploymentRoleInvalid);
        }
    }

    private async Task ValidateApplicationLoginAsync(
        DeploymentFoundation deployment,
        CancellationToken cancellationToken)
    {
        var role = deployment.CurrentRole;
        if (!role.CanLogin ||
            !role.Inherit ||
            role.Superuser ||
            role.CreateDatabase ||
            role.CreateRole ||
            role.Replication ||
            role.BypassRls)
        {
            Throw(DeploymentRoleInvalid);
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH RECURSIVE reachable(roleid, path) AS (
                SELECT membership.roleid,
                       ARRAY[membership.member, membership.roleid]::oid[]
                FROM pg_auth_members AS membership
                WHERE membership.member = CAST(@caller AS oid)

                UNION ALL

                SELECT membership.roleid,
                       reachable.path || membership.roleid
                FROM reachable
                JOIN pg_auth_members AS membership
                  ON membership.member = reachable.roleid
                WHERE NOT membership.roleid = ANY(reachable.path)
            )
            SELECT
                COALESCE(
                    (SELECT array_agg(DISTINCT roleid ORDER BY roleid)
                     FROM reachable),
                    ARRAY[]::oid[]),
                (SELECT count(*)
                 FROM pg_auth_members
                 WHERE member = CAST(@caller AS oid)
                   AND roleid = CAST(@runtime AS oid)),
                COALESCE(
                    (SELECT bool_and(
                        NOT admin_option AND inherit_option AND NOT set_option)
                     FROM pg_auth_members
                     WHERE member = CAST(@caller AS oid)
                       AND roleid = CAST(@runtime AS oid)),
                    false),
                (SELECT count(*)
                 FROM pg_auth_members
                 WHERE member = CAST(@runtime AS oid));
            """;

        var callerParameter = command.CreateParameter();
        callerParameter.ParameterName = "caller";
        callerParameter.Value = (long)deployment.CurrentRole.Oid;
        command.Parameters.Add(callerParameter);

        var runtimeParameter = command.CreateParameter();
        runtimeParameter.ParameterName = "runtime";
        runtimeParameter.Value = (long)deployment.RuntimeRole.Oid;
        command.Parameters.Add(runtimeParameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            Throw(DeploymentRoleInvalid);
        }

        var reachable = reader.GetFieldValue<uint[]>(0);
        var directRuntimeRows = reader.GetInt64(1);
        var directRuntimeOptionsSafe = reader.GetBoolean(2);
        var runtimeOutgoingRows = reader.GetInt64(3);

        if (reachable.Length != 1 ||
            reachable[0] != deployment.RuntimeRole.Oid ||
            directRuntimeRows != 1 ||
            !directRuntimeOptionsSafe ||
            runtimeOutgoingRows != 0)
        {
            Throw(DeploymentRoleInvalid);
        }
    }

    private async Task ValidateFunctionOwnerBackingReadsAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        var values = string.Join(
            $",{Environment.NewLine}",
            FunctionBackingReads.SelectMany(expectation =>
                expectation.Tables.Select(table =>
                    $"('{expectation.Function.Replace("'", "''")}','{table.Replace("'", "''")}')")));
        command.CommandText = $$"""
            SELECT COALESCE(bool_and(
                has_table_privilege(
                    deployer.oid,
                    relation.oid,
                    'SELECT')), false)
            FROM (VALUES
                {{values}}
            ) AS dependency(function_signature, table_name)
            JOIN pg_namespace AS namespace
              ON namespace.nspname = 'tagekyc'
            JOIN pg_class AS relation
              ON relation.relnamespace = namespace.oid
             AND relation.relname = dependency.table_name
            JOIN pg_roles AS deployer
              ON deployer.rolname = 'tagekyc_raw_export_deployer';
            """;

        if (await command.ExecuteScalarAsync(cancellationToken) is not true)
        {
            Throw(FunctionAclInvalid);
        }
    }

    private async Task ValidateBoundaryExecutionAvailableAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*) = 3
               AND bool_and(has_function_privilege(current_user, function.oid, 'EXECUTE'))
            FROM pg_proc AS function
            JOIN pg_namespace AS namespace ON namespace.oid = function.pronamespace
            WHERE namespace.nspname = 'tagekyc'
              AND function.proname IN (
                  'raw_export_read_authorization_eligibility_inputs',
                  'raw_export_read_authorization_policy_inputs',
                  'raw_export_control_plane_root_health');
            """;

        if (await command.ExecuteScalarAsync(cancellationToken) is not true)
        {
            Throw(DeploymentRoleInvalid);
        }
    }

    private async Task ValidateDeployerTableCapabilitiesAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        var values = string.Join(
            $",{Environment.NewLine}",
            DeployerTableExpectations.Select(expectation =>
                $"('{expectation.Table.Replace("'", "''")}')"));
        command.CommandText = $$"""
            WITH expected(table_name) AS (
                VALUES
                    {{values}}
            )
            SELECT expected.table_name,
                   owner.rolname,
                   has_table_privilege(deployer.oid, relation.oid, 'SELECT'),
                   has_table_privilege(deployer.oid, relation.oid, 'INSERT'),
                   has_table_privilege(deployer.oid, relation.oid, 'UPDATE'),
                   has_table_privilege(deployer.oid, relation.oid, 'DELETE'),
                   has_table_privilege(deployer.oid, relation.oid, 'TRUNCATE'),
                   has_table_privilege(deployer.oid, relation.oid, 'REFERENCES'),
                   has_table_privilege(deployer.oid, relation.oid, 'TRIGGER')
            FROM expected
            JOIN pg_class AS relation ON relation.relname = expected.table_name
            JOIN pg_namespace AS namespace
              ON namespace.oid = relation.relnamespace
             AND namespace.nspname = 'tagekyc'
            JOIN pg_roles AS owner ON owner.oid = relation.relowner
            JOIN pg_roles AS deployer
              ON deployer.rolname = 'tagekyc_raw_export_deployer'
            ORDER BY expected.table_name;
            """;

        var actual = new Dictionary<string, DeployerTableCapability>(StringComparer.Ordinal);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                actual[reader.GetString(0)] = new DeployerTableCapability(
                    reader.GetString(1),
                    reader.GetBoolean(2),
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetBoolean(5),
                    reader.GetBoolean(6),
                    reader.GetBoolean(7),
                    reader.GetBoolean(8));
            }
        }

        foreach (var expected in DeployerTableExpectations)
        {
            if (!actual.TryGetValue(expected.Table, out var capability) ||
                capability.Owner != "tagekyc" ||
                capability.Select != expected.Select ||
                capability.Insert != expected.Insert ||
                capability.Update != expected.Update ||
                capability.Delete ||
                capability.Truncate ||
                capability.References ||
                capability.Trigger)
            {
                Throw(FunctionAclInvalid);
            }
        }
    }

    private async Task ValidateIngressClaimTableAclAsync(
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT relation.relname,
                   owner.rolname,
                   COALESCE((
                       SELECT pg_catalog.array_agg(
                           COALESCE(grantor.rolname, acl.grantor::text) || ':' ||
                           CASE
                               WHEN acl.grantee = 0 THEN 'PUBLIC'
                               ELSE COALESCE(grantee.rolname, acl.grantee::text)
                           END || ':' ||
                           acl.privilege_type || ':' ||
                           acl.is_grantable::text
                           ORDER BY
                               COALESCE(grantor.rolname, acl.grantor::text),
                               CASE
                                   WHEN acl.grantee = 0 THEN 'PUBLIC'
                                   ELSE COALESCE(grantee.rolname, acl.grantee::text)
                               END,
                               acl.privilege_type,
                               acl.is_grantable)
                       FROM pg_catalog.aclexplode(
                           COALESCE(
                               relation.relacl,
                               pg_catalog.acldefault('r', relation.relowner))) AS acl
                       LEFT JOIN pg_catalog.pg_roles AS grantor
                         ON grantor.oid = acl.grantor
                       LEFT JOIN pg_catalog.pg_roles AS grantee
                         ON grantee.oid = acl.grantee
                       WHERE acl.grantee <> relation.relowner
                   ), ARRAY[]::text[]) AS explicit_non_owner_acl_rows,
                   NOT EXISTS (
                       SELECT 1
                       FROM pg_catalog.pg_attribute AS attribute
                       WHERE attribute.attrelid = relation.oid
                         AND attribute.attnum > 0
                         AND NOT attribute.attisdropped
                         AND attribute.attacl IS NOT NULL)
            FROM pg_catalog.pg_class AS relation
            JOIN pg_catalog.pg_namespace AS namespace
              ON namespace.oid = relation.relnamespace
            JOIN pg_catalog.pg_roles AS owner ON owner.oid = relation.relowner
            WHERE namespace.nspname = 'tagekyc'
              AND relation.relname IN (
                  'raw_export_source_ingress_claims',
                  'raw_export_source_ingress_claim_aliases')
            ORDER BY relation.relname;
            """;

        var expectedAclRows = new[]
        {
            "tagekyc:tagekyc_raw_export_deployer:INSERT:false",
            "tagekyc:tagekyc_raw_export_deployer:SELECT:false",
            "tagekyc:tagekyc_raw_export_deployer:UPDATE:false",
        };
        var rowCount = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rowCount++;
            var owner = reader.GetString(1);
            var aclRows = reader.GetFieldValue<string[]>(2);
            var columnAclEmpty = reader.GetBoolean(3);
            if (owner != "tagekyc" ||
                !aclRows.SequenceEqual(expectedAclRows, StringComparer.Ordinal) ||
                !columnAclEmpty)
            {
                Throw(FunctionAclInvalid);
            }
        }

        if (rowCount != 2)
        {
            Throw(FunctionAclInvalid);
        }
    }

    private async Task ValidateFunctionAclAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        var expectedValues = string.Join(
            $",{Environment.NewLine}",
            ExpectedFunctions.Select(function => $"('{function.Name.Replace("'", "''")}')"));
        command.CommandText = $$"""
            WITH expected(signature) AS (
                VALUES
                    {{expectedValues}}
            ),
            actual AS (
                SELECT n.nspname || '.' || p.proname || '(' || pg_catalog.pg_get_function_identity_arguments(p.oid) || ')' AS signature,
                       p.oid,
                       p.prosecdef,
                       owner.rolname,
                       owner.rolcanlogin,
                       COALESCE(array_to_string(p.proconfig, ','), '') AS config,
                       pg_catalog.pg_get_function_result(p.oid) AS result,
                       language.lanname AS language,
                       p.prosrc,
                       has_function_privilege('public', p.oid, 'EXECUTE') AS public_execute,
                       has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE') AS runtime_execute,
                       has_function_privilege('tagekyc_raw_export_bootstrapper', p.oid, 'EXECUTE') AS bootstrapper_execute,
                       COALESCE((
                           SELECT pg_catalog.array_agg(
                               COALESCE(grantor.rolname, acl.grantor::text) || ':' ||
                               CASE
                                   WHEN acl.grantee = 0 THEN 'PUBLIC'
                                   ELSE COALESCE(grantee.rolname, acl.grantee::text)
                               END || ':' ||
                               acl.privilege_type || ':' ||
                               acl.is_grantable::text
                               ORDER BY
                                   COALESCE(grantor.rolname, acl.grantor::text),
                                   CASE
                                       WHEN acl.grantee = 0 THEN 'PUBLIC'
                                       ELSE COALESCE(grantee.rolname, acl.grantee::text)
                                   END,
                                   acl.privilege_type,
                                   acl.is_grantable)
                           FROM pg_catalog.aclexplode(
                               COALESCE(
                                   p.proacl,
                                   pg_catalog.acldefault('f', p.proowner))) AS acl
                           LEFT JOIN pg_catalog.pg_roles AS grantor ON grantor.oid = acl.grantor
                           LEFT JOIN pg_catalog.pg_roles AS grantee ON grantee.oid = acl.grantee
                           WHERE acl.grantee <> p.proowner
                       ), ARRAY[]::text[]) AS explicit_non_owner_acl_rows
                FROM pg_proc p
                JOIN pg_namespace n ON n.oid = p.pronamespace
                JOIN pg_roles owner ON owner.oid = p.proowner
                JOIN pg_language language ON language.oid = p.prolang
                WHERE n.nspname = 'tagekyc'
            )
            SELECT expected.signature,
                   actual.oid IS NOT NULL,
                   actual.prosecdef,
                   actual.rolname,
                   actual.rolcanlogin,
                   actual.config,
                   actual.result,
                   actual.language,
                   actual.prosrc,
                   actual.public_execute,
                   actual.runtime_execute,
                   actual.bootstrapper_execute,
                   actual.explicit_non_owner_acl_rows
            FROM expected
            LEFT JOIN actual ON actual.signature = expected.signature
            ORDER BY expected.signature;
            """;

        var rows = new Dictionary<string, FunctionInfo>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows[reader.GetString(0)] = new FunctionInfo(
                reader.GetBoolean(1),
                reader.IsDBNull(2) ? false : reader.GetBoolean(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                !reader.IsDBNull(4) && reader.GetBoolean(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                reader.IsDBNull(8) ? string.Empty : HashBody(reader.GetString(8)),
                !reader.IsDBNull(9) && reader.GetBoolean(9),
                !reader.IsDBNull(10) && reader.GetBoolean(10),
                !reader.IsDBNull(11) && reader.GetBoolean(11),
                reader.IsDBNull(12) ? [] : reader.GetFieldValue<string[]>(12));
        }

        if (!rows.Keys.Order(StringComparer.Ordinal).SequenceEqual(
                ExpectedFunctions.Select(function => function.Name).Order(StringComparer.Ordinal),
                StringComparer.Ordinal))
        {
            Throw(FunctionAclInvalid);
        }

        foreach (var expected in ExpectedFunctions)
        {
            var expectedExplicitNonOwnerAclRows = new List<string>();
            if (expected.BootstrapperExecute)
            {
                expectedExplicitNonOwnerAclRows.Add(
                    "tagekyc_raw_export_deployer:tagekyc_raw_export_bootstrapper:EXECUTE:false");
            }
            if (expected.RuntimeExecute)
            {
                expectedExplicitNonOwnerAclRows.Add(
                    "tagekyc_raw_export_deployer:tagekyc_runtime:EXECUTE:false");
            }
            expectedExplicitNonOwnerAclRows.Sort(StringComparer.Ordinal);

            if (!rows.TryGetValue(expected.Name, out var actual) ||
                !actual.Exists ||
                actual.SecurityDefiner != expected.SecurityDefiner ||
                actual.Owner != "tagekyc_raw_export_deployer" ||
                actual.OwnerCanLogin ||
                actual.Config != "search_path=pg_catalog" ||
                (expected.Result is not null && actual.Result != expected.Result) ||
                (expected.Language is not null && actual.Language != expected.Language) ||
                (expected.BodySha256 is not null && actual.BodySha256 != expected.BodySha256) ||
                actual.PublicExecute ||
                actual.RuntimeExecute != expected.RuntimeExecute ||
                actual.BootstrapperExecute != expected.BootstrapperExecute ||
                !actual.ExplicitNonOwnerAclRows.SequenceEqual(
                    expectedExplicitNonOwnerAclRows,
                    StringComparer.Ordinal))
            {
                Throw(FunctionAclInvalid);
            }
        }
    }

    private sealed record FunctionExpectation(
        string Name,
        bool SecurityDefiner,
        bool RuntimeExecute,
        bool BootstrapperExecute,
        string? Result = null,
        string? Language = null,
        string? BodySha256 = null);

    private sealed record FunctionInfo(
        bool Exists,
        bool SecurityDefiner,
        string Owner,
        bool OwnerCanLogin,
        string Config,
        string Result,
        string Language,
        string BodySha256,
        bool PublicExecute,
        bool RuntimeExecute,
        bool BootstrapperExecute,
        IReadOnlyList<string> ExplicitNonOwnerAclRows);

    private sealed record FunctionBackingReadExpectation(
        string Function,
        IReadOnlyList<string> Tables);

    private sealed record DeployerTableExpectation(
        string Table,
        bool Select,
        bool Insert,
        bool Update = false);

    private sealed record DeployerTableCapability(
        string Owner,
        bool Select,
        bool Insert,
        bool Update,
        bool Delete,
        bool Truncate,
        bool References,
        bool Trigger);

    private sealed record RoleInfo(
        uint Oid,
        string Name,
        bool CanLogin,
        bool Inherit,
        bool Superuser,
        bool CreateDatabase,
        bool CreateRole,
        bool Replication,
        bool BypassRls);

    private sealed record DeploymentFoundation(
        int ServerVersion,
        string SessionUser,
        string CurrentUser,
        RoleInfo CurrentRole,
        RoleInfo RuntimeRole);

    private static string HashBody(string body) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
}
