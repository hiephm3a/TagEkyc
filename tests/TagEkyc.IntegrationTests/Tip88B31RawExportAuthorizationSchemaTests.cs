using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B31RawExportAuthorizationSchemaTests(PostgresPersistenceFixture postgres)
{
    private const string Migration = "20260722084729_Tip88B31RawExportAuthorizationSchema";
    private const string PreviousMigration = "20260721070118_Tip88AE2PolicyPermitTtlField";

    private static readonly string[] Tables =
    [
        "raw_export_authorization_idempotency", "raw_export_authorization_decisions",
        "raw_export_decision_eligibility_causes", "raw_export_decision_fulfillment_refs",
        "raw_export_decision_classes", "raw_export_authorization_permits", "raw_export_permit_classes",
    ];

    private static readonly string[] Constraints =
    [
        "CK_raw_export_authorization_decisions_row_shape", "CK_raw_export_authorization_decisions_FingerprintHash_len",
        "CK_raw_export_authorization_decisions_ConsentScopeHash_len", "CK_raw_export_authorization_idempotency_FingerprintHash_len",
        "CK_raw_export_authorization_decisions_PolicyVersion_range", "CK_raw_export_authorization_permits_PolicyVersion_range",
        "CK_raw_export_authorization_permits_SchemaVersion_eq1", "CK_raw_export_decision_eligibility_causes_Ordinal_nonneg",
        "CK_raw_export_decision_fulfillment_refs_Ordinal_nonneg", "CK_raw_export_decision_fulfillment_refs_Revision_pos",
        "CK_raw_export_decision_classes_Ordinal_nonneg", "CK_raw_export_permit_classes_Ordinal_nonneg",
        "CK_raw_export_authorization_decisions_SelectionMode_enum", "CK_raw_export_authorization_decisions_Outcome_enum",
        "CK_raw_export_authorization_decisions_PrimaryCause_enum", "CK_raw_export_authorization_decisions_SessionState_enum",
        "CK_raw_export_authorization_decisions_EligibilityCause_enum", "CK_raw_export_authorization_decisions_SubjectConsentCause_enum",
        "CK_raw_export_decision_eligibility_causes_Cause_enum", "CK_raw_export_decision_fulfillment_refs_RequirementType_enum",
        "CK_raw_export_decision_classes_ClassKind_enum", "CK_raw_export_decision_classes_RawClass_enum",
        "CK_raw_export_permit_classes_RawClass_enum", "PK_raw_export_authorization_idempotency",
        "PK_raw_export_authorization_decisions", "PK_raw_export_decision_eligibility_causes",
        "PK_raw_export_decision_fulfillment_refs", "PK_raw_export_decision_classes",
        "PK_raw_export_authorization_permits", "PK_raw_export_permit_classes",
        "UQ_raw_export_authorization_idempotency_ExportDecisionId", "UQ_raw_export_authorization_permits_AuthorizationDecisionId",
        "UQ_raw_export_decision_fulfillment_refs_Ordinal", "UQ_raw_export_decision_classes_Ordinal",
        "UQ_raw_export_permit_classes_Ordinal", "FK_raw_export_authorization_idempotency_decision",
        "FK_raw_export_authorization_decisions_session", "FK_raw_export_decision_eligibility_causes_decision",
        "FK_raw_export_decision_fulfillment_refs_decision", "FK_raw_export_decision_classes_decision",
        "FK_raw_export_authorization_permits_decision", "FK_raw_export_authorization_permits_session",
        "FK_raw_export_permit_classes_permit",
    ];

    [Fact]
    public async Task G1_intended_b3_identifiers_are_within_63_bytes_and_round_trip_exactly()
    {
        string[] triggers =
        [
            "tr_b3_authz_idempotency_insert", "tr_b3_authz_decisions_insert",
            "tr_b3_authz_eligibility_causes_insert", "tr_b3_authz_fulfillment_refs_insert",
            "tr_b3_authz_decision_classes_insert", "tr_b3_authz_permits_insert",
            "tr_b3_authz_permit_classes_insert", "tr_b3_authz_eligibility_causes_xmin",
            "tr_b3_authz_fulfillment_refs_xmin", "tr_b3_authz_decision_classes_xmin",
            "tr_b3_authz_permits_xmin", "tr_b3_authz_permit_classes_xmin",
            "tr_b3_authz_permit_has_classes", "tr_b3_authz_idempotency_append_only",
            "tr_b3_authz_decisions_append_only", "tr_b3_authz_eligibility_causes_append_only",
            "tr_b3_authz_fulfillment_refs_append_only", "tr_b3_authz_decision_classes_append_only",
            "tr_b3_authz_permits_append_only", "tr_b3_authz_permit_classes_append_only",
        ];
        string[] functions =
        [
            "enforce_raw_export_authorization_insert",
            "enforce_raw_export_decision_child_same_transaction",
            "enforce_raw_export_permit_child_same_transaction",
            "enforce_raw_export_permit_has_classes",
        ];

        await using var db = postgres.CreateDbContext();
        var intendedColumns = Tables.SelectMany(table =>
        {
            var storeObject = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(table, "tagekyc");
            return db.Model.GetEntityTypes()
                .Where(entityType => entityType.GetTableName() == table)
                .SelectMany(entityType => entityType.GetProperties())
                .Select(property => property.GetColumnName(storeObject))
                .Where(column => column is not null)
                .Select(column => $"{table}.{column}");
        }).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var intendedNames = Tables.Concat(Constraints).Concat(triggers).Concat(functions)
            .Concat(intendedColumns.Select(column => column[(column.IndexOf('.') + 1)..]))
            .ToArray();

        await using var connection = await OpenAsync();
        var actualTables = await QueryStringsAsync(connection, """
            SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname = ANY(@tables);
            """, new NpgsqlParameter("tables", Tables));
        var actualConstraints = await QueryStringsAsync(connection, """
            SELECT c.conname FROM pg_constraint c JOIN pg_class r ON r.oid=c.conrelid
            JOIN pg_namespace n ON n.oid=r.relnamespace
            WHERE n.nspname='tagekyc' AND r.relname = ANY(@tables) AND c.contype <> 't';
            """, new NpgsqlParameter("tables", Tables));
        var actualTriggers = await QueryStringsAsync(connection, """
            SELECT t.tgname FROM pg_trigger t JOIN pg_class r ON r.oid=t.tgrelid
            JOIN pg_namespace n ON n.oid=r.relnamespace
            WHERE n.nspname='tagekyc' AND r.relname = ANY(@tables) AND NOT t.tgisinternal;
            """, new NpgsqlParameter("tables", Tables));
        var actualFunctions = await QueryStringsAsync(connection, """
            SELECT DISTINCT p.proname FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname = ANY(@functions);
            """, new NpgsqlParameter("functions", functions));
        var actualColumns = await QueryStringsAsync(connection, """
            SELECT c.relname || '.' || a.attname FROM pg_attribute a
            JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relname = ANY(@tables)
              AND a.attnum > 0 AND NOT a.attisdropped;
            """, new NpgsqlParameter("tables", Tables));

        Assert.Multiple(
            () => Assert.All(intendedNames, name => Assert.True(System.Text.Encoding.UTF8.GetByteCount(name) <= 63, $"Intended identifier exceeds 63 bytes: {name}")),
            () => Assert.Equal(Tables.Order(StringComparer.Ordinal), actualTables.Order(StringComparer.Ordinal)),
            () => Assert.Equal(Constraints.Order(StringComparer.Ordinal), actualConstraints.Order(StringComparer.Ordinal)),
            () => Assert.Equal(triggers.Order(StringComparer.Ordinal), actualTriggers.Order(StringComparer.Ordinal)),
            () => Assert.Equal(functions.Order(StringComparer.Ordinal), actualFunctions.Order(StringComparer.Ordinal)),
            () => Assert.Equal(intendedColumns, actualColumns.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public async Task G2_pg_constraint_contains_all_43_exact_names_and_deferred_fk_shape()
    {
        await using var connection = await OpenAsync();
        var actual = await QueryStringsAsync(connection, """
            SELECT c.conname FROM pg_constraint c JOIN pg_class r ON r.oid=c.conrelid
            JOIN pg_namespace n ON n.oid=r.relnamespace
            WHERE n.nspname='tagekyc' AND r.relname = ANY(@tables) AND c.contype <> 't' ORDER BY c.conname;
            """, new NpgsqlParameter("tables", Tables));
        Assert.Equal(Constraints.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));

        Assert.True((bool)(await ScalarAsync(connection, """
            SELECT condeferrable AND condeferred FROM pg_constraint
            WHERE conname='FK_raw_export_authorization_idempotency_decision';
            """) ?? false));

        var functionAclViolations = Convert.ToInt32(await ScalarAsync(connection, """
            SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            JOIN pg_roles owner ON owner.oid=p.proowner
            WHERE n.nspname='tagekyc'
              AND p.proname IN ('enforce_raw_export_authorization_insert','enforce_raw_export_decision_child_same_transaction','enforce_raw_export_permit_child_same_transaction','enforce_raw_export_permit_has_classes')
              AND (owner.rolname <> 'tagekyc_raw_export_deployer'
                   OR has_function_privilege('public',p.oid,'EXECUTE')
                   OR has_function_privilege('tagekyc_runtime',p.oid,'EXECUTE'));
            """));
        Assert.Equal(0, functionAclViolations);
        Assert.Equal(4, Convert.ToInt32(await ScalarAsync(connection, """
            SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname LIKE 'enforce_raw_export%authorization%'
               OR n.nspname='tagekyc' AND p.proname IN ('enforce_raw_export_decision_child_same_transaction','enforce_raw_export_permit_child_same_transaction','enforce_raw_export_permit_has_classes');
            """)));
    }

    [Fact]
    public async Task G5_runtime_selects_all_7_tables_and_every_dml_kind_fails_with_42501()
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "GRANT tagekyc_runtime TO tagekyc; SET ROLE tagekyc_runtime;");
        foreach (var table in Tables)
        {
            await ScalarAsync(connection, $"SELECT count(*) FROM tagekyc.{table};");
            var updateColumn = table switch
            {
                "raw_export_authorization_decisions" => "ExportDecisionId",
                "raw_export_authorization_idempotency" => "PrincipalId",
                "raw_export_authorization_permits" => "PermitId",
                _ => "Ordinal",
            };
            foreach (var sql in new[] { $"INSERT INTO tagekyc.{table} DEFAULT VALUES;", $"UPDATE tagekyc.{table} SET \"{updateColumn}\"=\"{updateColumn}\";", $"DELETE FROM tagekyc.{table};", $"TRUNCATE tagekyc.{table};" })
            {
                var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, sql));
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
            }
        }
    }

    [Fact]
    public async Task G3_all_check_constraint_categories_reject_invalid_rows()
    {
        await AssertDecisionCheckAsync(DeniedDecisionSql(Guid.NewGuid()).Replace("repeat('00',32)", "repeat('00',31)"), "CK_raw_export_authorization_decisions_FingerprintHash_len");
        var badConsentHash = DeniedDecisionSql(Guid.NewGuid()).Replace("\"DecidedAtUtc\")", "\"ConsentScopeHash\",\"DecidedAtUtc\")").Replace("transaction_timestamp());", "decode(repeat('11',31),'hex'),transaction_timestamp());");
        await AssertDecisionCheckAsync(badConsentHash, "CK_raw_export_authorization_decisions_ConsentScopeHash_len");
        await AssertDecisionCheckAsync(DeniedDecisionSql(Guid.NewGuid()).Replace(",1,decode", ",0,decode"), "CK_raw_export_authorization_decisions_PolicyVersion_range");
        await AssertDecisionCheckAsync(DeniedDecisionSql(Guid.NewGuid()).Replace("'DefaultPolicySet'", "'InvalidMode'"), "CK_raw_export_authorization_decisions_SelectionMode_enum");
        await AssertDecisionCheckAsync(DeniedDecisionSql(Guid.NewGuid()).Replace("'SESSION_NOT_FOUND'", "'INVALID_CAUSE'"), "CK_raw_export_authorization_decisions_PrimaryCause_enum");
        var authorizedMissingEvidence = DeniedDecisionSql(Guid.NewGuid()).Replace("'Denied','SESSION_NOT_FOUND'", "'Authorized',NULL");
        await AssertDecisionCheckAsync(authorizedMissingEvidence, "CK_raw_export_authorization_decisions_row_shape");
        var deniedMissingCause = DeniedDecisionSql(Guid.NewGuid()).Replace("'Denied','SESSION_NOT_FOUND'", "'Denied',NULL");
        await AssertDecisionCheckAsync(deniedMissingCause, "CK_raw_export_authorization_decisions_row_shape");

        await AssertChildCheckAsync("eligibility_cause", "raw_export_decision_eligibility_causes", "0,'InvalidCause'", "CK_raw_export_decision_eligibility_causes_Cause_enum");
        await AssertChildCheckAsync("fulfillment_ref", "raw_export_decision_fulfillment_refs", "'InvalidRequirement',0,gen_random_uuid(),1,'artifact','v1',NULL", "CK_raw_export_decision_fulfillment_refs_RequirementType_enum");
        await AssertChildCheckAsync("decision_class", "raw_export_decision_classes", "'InvalidKind','ChipDg1',0", "CK_raw_export_decision_classes_ClassKind_enum");
        await AssertChildCheckAsync("decision_class", "raw_export_decision_classes", "'Authorized','InvalidClass',0", "CK_raw_export_decision_classes_RawClass_enum");

        await using (var connection = await OpenAsync())
        {
            var id = Guid.NewGuid();
            await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
            await ExecuteAsync(connection, DeniedDecisionSql(id));
            await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','idempotency',true);");
            var badIdempotencyHash = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_authorization_idempotency VALUES (gen_random_uuid(),gen_random_uuid(),gen_random_uuid(),'bad-hash',decode(repeat('00',31),'hex'),'{id}',transaction_timestamp());"));
            Assert.Equal("CK_raw_export_authorization_idempotency_FingerprintHash_len", badIdempotencyHash.ConstraintName);
            await ExecuteAsync(connection, "ROLLBACK;");
        }

        var sessionId = await SeedSessionAsync();
        await AssertPermitCheckAsync(sessionId, PermitSql(Guid.NewGuid(), Guid.Empty, sessionId).Replace("'00000000-0000-0000-0000-000000000000'", "'{decision}'").Replace("000000000015',1,'Subject", "000000000015',0,'Subject"), "CK_raw_export_authorization_permits_PolicyVersion_range");
        await AssertPermitCheckAsync(sessionId, PermitSql(Guid.NewGuid(), Guid.Empty, sessionId).Replace("'00000000-0000-0000-0000-000000000000'", "'{decision}'").Replace(",1,transaction_timestamp());", ",2,transaction_timestamp());"), "CK_raw_export_authorization_permits_SchemaVersion_eq1");
        await using (var connection = await OpenAsync())
        {
            var decision = Guid.NewGuid(); var permit = Guid.NewGuid();
            await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
            await ExecuteAsync(connection, AuthorizedDecisionSql(decision, sessionId));
            await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','permit',true);");
            await ExecuteAsync(connection, PermitSql(permit, decision, sessionId));
            await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','permit_class',true);");
            var badPermitClass = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_permit_classes VALUES ('{permit}','InvalidClass',0);"));
            Assert.Equal("CK_raw_export_permit_classes_RawClass_enum", badPermitClass.ConstraintName);
            await ExecuteAsync(connection, "ROLLBACK;");
        }
        await using (var db = postgres.CreateDbContext()) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM tagekyc.verification_sessions WHERE \"Id\"={sessionId};");
    }

    [Fact]
    public async Task G7_insert_guard_accepts_correct_tokens_and_rejects_wrong_or_missing_token()
    {
        var sessionId = await SeedSessionAsync();
        var decisionId = Guid.Parse("88b31000-0000-4000-8000-000000000041");
        var permitId = Guid.Parse("88b31000-0000-4000-8000-000000000042");
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; SET ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(connection, "BEGIN;");
        await InsertCompleteGraphAsync(connection, sessionId, decisionId, permitId, "g7-complete");
        await ExecuteAsync(connection, "COMMIT;");
        await CleanupGraphAsync(connection, decisionId, permitId);

        await ExecuteAsync(connection, "BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','wrong',true);");
        var wrong = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, DeniedDecisionSql(Guid.NewGuid())));
        Assert.Equal("P0001", wrong.SqlState);
        Assert.Equal("RAW_EXPORT_AUTHORIZATION_DIRECT_INSERT_UNSUPPORTED", wrong.MessageText);
        await ExecuteAsync(connection, "ROLLBACK;");

        await ExecuteAsync(connection, "BEGIN; SET ROLE tagekyc_raw_export_deployer; RESET tagekyc.raw_export_authorization_insert_context;");
        var missing = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, DeniedDecisionSql(Guid.NewGuid())));
        Assert.Equal("P0001", missing.SqlState);
        Assert.Equal("RAW_EXPORT_AUTHORIZATION_DIRECT_INSERT_UNSUPPORTED", missing.MessageText);
        await ExecuteAsync(connection, "ROLLBACK; RESET ROLE;");
        await ExecuteAsync(connection, $"DELETE FROM tagekyc.verification_sessions WHERE \"Id\"='{sessionId}';");
    }

    [Fact]
    public async Task G8_xmin_child_guard_mutation_is_detected()
    {
        var id = Guid.Parse("88b31000-0000-4000-8000-000000000003");
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; SET ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(connection, "BEGIN; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
        await ExecuteAsync(connection, DeniedDecisionSql(id));
        await ExecuteAsync(connection, "COMMIT;");

        await ExecuteAsync(connection, "BEGIN; SELECT set_config('tagekyc.raw_export_authorization_insert_context','eligibility_cause',true);");
        var guarded = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_decision_eligibility_causes VALUES ('{id}',0,'GrantMissing');"));
        Assert.Equal("P0001", guarded.SqlState);
        await ExecuteAsync(connection, "ROLLBACK;");
        await CleanupDecisionAsync(connection, id);
    }

    [Fact]
    public async Task G6_owner_update_and_delete_are_rejected_on_all_7_tables()
    {
        var sessionId = await SeedSessionAsync();
        var decisionId = Guid.Parse("88b31000-0000-4000-8000-000000000051");
        var permitId = Guid.Parse("88b31000-0000-4000-8000-000000000052");
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; BEGIN; SET ROLE tagekyc_raw_export_deployer;");
        await InsertCompleteGraphAsync(connection, sessionId, decisionId, permitId, "g6-complete");
        await ExecuteAsync(connection, "COMMIT;");

        foreach (var table in Tables)
        {
            var key = table switch
            {
                "raw_export_authorization_decisions" => "ExportDecisionId",
                "raw_export_authorization_idempotency" => "PrincipalId",
                "raw_export_authorization_permits" => "PermitId",
                "raw_export_permit_classes" => "PermitId",
                _ => "ExportDecisionId",
            };
            foreach (var mutation in new[] { $"UPDATE tagekyc.{table} SET \"{key}\"=\"{key}\";", $"DELETE FROM tagekyc.{table};" })
            {
                await ExecuteAsync(connection, "BEGIN;");
                var blocked = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, mutation));
                Assert.Equal("P0001", blocked.SqlState);
                await ExecuteAsync(connection, "ROLLBACK;");
            }
        }

        await CleanupGraphAsync(connection, decisionId, permitId);
        await ExecuteAsync(connection, $"DELETE FROM tagekyc.verification_sessions WHERE \"Id\"='{sessionId}';");
    }

    [Fact]
    public async Task G8_deferred_permit_has_classes_trigger_mutation_is_detected()
    {
        Guid sessionId;
        await using (var db = postgres.CreateDbContext())
        {
            var session = VerificationSession.Create(Guid.Parse("88b31000-0000-4000-8000-000000000012"), "b3-subject", VerificationProfile.StandardEkycProfile, "raw-export", [RequiredCheckType.DocumentNfc], DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);
            var repository = new EfVerificationSessionRepository(db);
            await repository.AddAsync(session);
            await repository.SetStateAsync(session.Id, VerificationSessionState.Completed);
            sessionId = session.Id;
        }

        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc;");
        var rejectedDecision = Guid.Parse("88b31000-0000-4000-8000-000000000021");
        var rejectedPermit = Guid.Parse("88b31000-0000-4000-8000-000000000022");
        await ExecuteAsync(connection, "BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
        await ExecuteAsync(connection, AuthorizedDecisionSql(rejectedDecision, sessionId));
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','permit',true);");
        await ExecuteAsync(connection, PermitSql(rejectedPermit, rejectedDecision, sessionId));
        var rejected = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, "COMMIT;"));
        Assert.Equal("P0001", rejected.SqlState);
        await ExecuteAsync(connection, "ROLLBACK;");
        await ExecuteAsync(connection, $"RESET ROLE; DELETE FROM tagekyc.verification_sessions WHERE \"Id\"='{sessionId}';");
    }

    [Fact]
    public async Task G4_migration_apply_rollback_reapply_preserves_acl_equivalence()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        string[] expectedPreE3RuntimeAcl =
        [
            "raw_export_subject_consent_authorities|tagekyc|tagekyc_runtime|SELECT|false",
            "raw_export_subject_consent_classes|tagekyc|tagekyc_runtime|SELECT|false",
            "raw_export_subject_consent_events|tagekyc|tagekyc_runtime|SELECT|false",
            "verification_sessions|tagekyc|tagekyc_runtime|SELECT|false",
        ];
        async Task<string[]> RuntimeConsentAclAsync() =>
            await db.Database.SqlQueryRaw<string>(
                    """
                    SELECT
                        relation.relname || '|' ||
                        COALESCE(grantor.rolname, acl.grantor::text) || '|' ||
                        COALESCE(grantee.rolname, acl.grantee::text) || '|' ||
                        acl.privilege_type || '|' ||
                        acl.is_grantable::text AS "Value"
                    FROM pg_class AS relation
                    JOIN pg_namespace AS namespace
                      ON namespace.oid = relation.relnamespace
                    CROSS JOIN LATERAL aclexplode(
                        COALESCE(
                            relation.relacl,
                            acldefault('r', relation.relowner))) AS acl
                    LEFT JOIN pg_roles AS grantor ON grantor.oid = acl.grantor
                    LEFT JOIN pg_roles AS grantee ON grantee.oid = acl.grantee
                    WHERE namespace.nspname = 'tagekyc'
                      AND relation.relname IN (
                          'verification_sessions',
                          'raw_export_subject_consent_authorities',
                          'raw_export_subject_consent_events',
                          'raw_export_subject_consent_classes')
                      AND acl.grantee = 'tagekyc_runtime'::regrole::oid
                    ORDER BY relation.relname, grantor.rolname, acl.privilege_type;
                    """)
                .ToArrayAsync();

        var currentAcl = await LandedAclAsync(db);
        var b3Before = await B3AclAsync(db);
        Assert.Empty(await RuntimeConsentAclAsync());
        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            Assert.Equal(expectedPreE3RuntimeAcl, await RuntimeConsentAclAsync());
            var historicalAcl = await LandedAclAsync(db);
            await using (var connection = await OpenAsync())
            {
                Assert.Equal(0, Convert.ToInt32(await ScalarAsync(connection, "SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='tagekyc' AND c.relname = ANY(@tables);", new NpgsqlParameter("tables", Tables))));
            }
            await migrator.MigrateAsync(Migration);
            Assert.Equal(expectedPreE3RuntimeAcl, await RuntimeConsentAclAsync());
            Assert.Equal(historicalAcl, await LandedAclAsync(db));
            Assert.Equal(b3Before, await B3AclAsync(db));
            await migrator.MigrateAsync(PreviousMigration);
            Assert.Equal(expectedPreE3RuntimeAcl, await RuntimeConsentAclAsync());
            await migrator.MigrateAsync(Migration);
            Assert.Equal(expectedPreE3RuntimeAcl, await RuntimeConsentAclAsync());
            Assert.Equal(historicalAcl, await LandedAclAsync(db));
            Assert.Equal(b3Before, await B3AclAsync(db));
        }
        finally
        {
            await migrator.MigrateAsync("20260724015546_Tip88B1E3ResolverReadBoundary");
        }
        Assert.Empty(await RuntimeConsentAclAsync());
        Assert.Equal(currentAcl, await LandedAclAsync(db));
        Assert.Equal(
            "20260724015546_Tip88B1E3ResolverReadBoundary",
            await db.Database.SqlQueryRaw<string>(
                    """
                    SELECT "MigrationId" AS "Value"
                    FROM "__EFMigrationsHistory"
                    ORDER BY "MigrationId" DESC
                    LIMIT 1
                    """)
                .SingleAsync());

        string[] protectedTables =
        [
            "raw_export_policy_versions", "raw_export_policy_allowed_classes",
            "raw_export_policy_requirements", "raw_export_policy_closures",
            "raw_export_requirement_rule_sets", "raw_export_grants",
            "raw_export_fulfillments", "raw_export_policy_lifecycle",
            "verification_sessions", "raw_export_subject_consent_authorities",
            "raw_export_subject_consent_events", "raw_export_subject_consent_classes",
            "raw_export_requirement_rules", "raw_export_control_authorities",
        ];
        string[] forbiddenPrivileges =
            ["SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER"];
        await using var verify = await OpenAsync();
        Assert.Equal(
            0,
            Convert.ToInt32(await ScalarAsync(
                verify,
                """
                SELECT count(*)
                FROM unnest(@tables) AS t(table_name)
                CROSS JOIN unnest(@privileges) AS p(privilege_name)
                WHERE has_table_privilege(
                    'tagekyc_runtime',
                    'tagekyc.' || t.table_name,
                    p.privilege_name);
                """,
                new NpgsqlParameter("tables", protectedTables),
                new NpgsqlParameter("privileges", forbiddenPrivileges))));
        Assert.Equal(
            3,
            Convert.ToInt32(await ScalarAsync(
                verify,
                """
                SELECT count(*)
                FROM pg_proc AS p
                JOIN pg_namespace AS n ON n.oid = p.pronamespace
                JOIN pg_roles AS owner ON owner.oid = p.proowner
                WHERE n.nspname = 'tagekyc'
                  AND p.proname = ANY(@functions)
                  AND p.prosecdef
                  AND owner.rolname = 'tagekyc_raw_export_deployer'
                  AND NOT owner.rolcanlogin
                  AND p.proconfig = ARRAY['search_path=pg_catalog']
                  AND has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE')
                  AND NOT has_function_privilege('public', p.oid, 'EXECUTE')
                  AND (
                      SELECT array_agg(
                          (CASE WHEN acl.grantee = 0 THEN 'PUBLIC' ELSE grantee.rolname END)::text
                          ORDER BY (CASE WHEN acl.grantee = 0 THEN 'PUBLIC' ELSE grantee.rolname END)::text)
                      FROM aclexplode(COALESCE(p.proacl, acldefault('f', p.proowner))) AS acl
                      LEFT JOIN pg_roles AS grantee ON grantee.oid = acl.grantee
                      WHERE acl.privilege_type = 'EXECUTE'
                        AND acl.grantee <> p.proowner
                  ) = ARRAY['tagekyc_runtime'];
                """,
                new NpgsqlParameter(
                    "functions",
                    new[]
                    {
                        "raw_export_read_authorization_eligibility_inputs",
                        "raw_export_read_authorization_policy_inputs",
                        "raw_export_control_plane_root_health",
                    }))));
        Assert.Equal(
            "9359b6f264931b77dc3194136fbc5cf2",
            Convert.ToString(await ScalarAsync(
                verify,
                """
                SELECT md5(p.prosrc)
                FROM pg_proc AS p
                JOIN pg_namespace AS n ON n.oid = p.pronamespace
                WHERE n.nspname = 'tagekyc'
                  AND p.proname = 'raw_export_append_subject_consent_granted';
                """)));
    }

    [Fact]
    public async Task G9_readiness_returns_503_with_table_mutation_code()
    {
        await using var db = postgres.CreateDbContext();
        await new RawExportAuthorizationReadinessValidator(db).ValidateAsync(CancellationToken.None);
        await db.Database.ExecuteSqlRawAsync("GRANT INSERT ON tagekyc.raw_export_authorization_decisions TO tagekyc_runtime;");
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
            builder.WebHost.UseTestServer();
            builder.Services.AddDbContext<TagEkycDbContext>(options => options.UseNpgsql(postgres.ConnectionString));
            builder.Services.AddScoped<RawExportAuthorizationReadinessValidator>();
            builder.Services.AddScoped<IReadinessCheck, RawExportAuthorizationReadinessCheck>();
            await using var app = builder.Build();
            app.MapReadinessEndpoint();
            await app.StartAsync();

            var response = await app.GetTestClient().GetAsync("/readiness");
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains(RawExportAuthorizationReadinessValidator.TableMutationPrivilege, body, StringComparison.Ordinal);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("REVOKE INSERT ON tagekyc.raw_export_authorization_decisions FROM tagekyc_runtime;");
        }
    }

    private async Task<Guid> SeedSessionAsync()
    {
        await using var db = postgres.CreateDbContext();
        var session = VerificationSession.Create(Guid.Parse("88b31000-0000-4000-8000-000000000012"), "b3-subject", VerificationProfile.StandardEkycProfile, "raw-export", [RequiredCheckType.DocumentNfc], DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);
        var repository = new EfVerificationSessionRepository(db);
        await repository.AddAsync(session);
        await repository.SetStateAsync(session.Id, VerificationSessionState.Completed);
        return session.Id;
    }

    private static async Task InsertCompleteGraphAsync(NpgsqlConnection connection, Guid sessionId, Guid decisionId, Guid permitId, string idempotencyKey)
    {
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
        await ExecuteAsync(connection, AuthorizedDecisionSql(decisionId, sessionId));
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','idempotency',true);");
        await ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_authorization_idempotency VALUES ('88b31000-0000-4000-8000-000000000011','88b31000-0000-4000-8000-000000000012','{sessionId}','{idempotencyKey}',decode(repeat('00',32),'hex'),'{decisionId}',transaction_timestamp());");
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','eligibility_cause',true);");
        await ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_decision_eligibility_causes VALUES ('{decisionId}',0,'GrantMissing');");
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','fulfillment_ref',true);");
        await ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_decision_fulfillment_refs VALUES ('{decisionId}','LegalApproval',0,gen_random_uuid(),1,'artifact','v1',NULL);");
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision_class',true);");
        await ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_decision_classes VALUES ('{decisionId}','Authorized','ChipDg1',0);");
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','permit',true);");
        await ExecuteAsync(connection, PermitSql(permitId, decisionId, sessionId));
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','permit_class',true);");
        await ExecuteAsync(connection, $"INSERT INTO tagekyc.raw_export_permit_classes VALUES ('{permitId}','ChipDg1',0);");
    }

    private async Task<NpgsqlConnection> OpenAsync() { var connection = new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); return connection; }
    private async Task AssertDecisionCheckAsync(string sql, string expectedConstraint)
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, sql));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(expectedConstraint, exception.ConstraintName);
        await ExecuteAsync(connection, "ROLLBACK;");
    }
    private async Task AssertChildCheckAsync(string context, string table, string valuesAfterDecisionId, string expectedConstraint)
    {
        await using var connection = await OpenAsync();
        var decision = Guid.NewGuid();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
        await ExecuteAsync(connection, DeniedDecisionSql(decision));
        await ExecuteAsync(connection, $"SELECT set_config('tagekyc.raw_export_authorization_insert_context','{context}',true);");
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"INSERT INTO tagekyc.{table} VALUES ('{decision}',{valuesAfterDecisionId});"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(expectedConstraint, exception.ConstraintName);
        await ExecuteAsync(connection, "ROLLBACK;");
    }
    private async Task AssertPermitCheckAsync(Guid sessionId, string permitTemplate, string expectedConstraint)
    {
        await using var connection = await OpenAsync();
        var decision = Guid.NewGuid();
        await ExecuteAsync(connection, "GRANT tagekyc_raw_export_deployer TO tagekyc; BEGIN; SET ROLE tagekyc_raw_export_deployer; SELECT set_config('tagekyc.raw_export_authorization_insert_context','decision',true);");
        await ExecuteAsync(connection, AuthorizedDecisionSql(decision, sessionId));
        await ExecuteAsync(connection, "SELECT set_config('tagekyc.raw_export_authorization_insert_context','permit',true);");
        var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, permitTemplate.Replace("{decision}", decision.ToString())));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(expectedConstraint, exception.ConstraintName);
        await ExecuteAsync(connection, "ROLLBACK;");
    }
    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null) { await using var command = new NpgsqlCommand(sql, connection, transaction); await command.ExecuteNonQueryAsync(); }
    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters) { await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddRange(parameters); return await command.ExecuteScalarAsync(); }
    private static async Task<string[]> QueryStringsAsync(NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters) { await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddRange(parameters); await using var reader = await command.ExecuteReaderAsync(); var values = new List<string>(); while (await reader.ReadAsync()) values.Add(reader.GetString(0)); return values.ToArray(); }
    private static string DeniedDecisionSql(Guid id) => $"""INSERT INTO tagekyc.raw_export_authorization_decisions ("ExportDecisionId","PrincipalId","ClientApplicationId","ApiKeyId","RequestedVerificationSessionId","PolicyId","PolicyVersion","FingerprintHash","RawClassSelectionMode","Outcome","PrimaryCause","DecidedAtUtc") VALUES ('{id}','88b31000-0000-4000-8000-000000000011','88b31000-0000-4000-8000-000000000012','88b31000-0000-4000-8000-000000000013','88b31000-0000-4000-8000-000000000014','88b31000-0000-4000-8000-000000000015',1,decode(repeat('00',32),'hex'),'DefaultPolicySet','Denied','SESSION_NOT_FOUND',transaction_timestamp());""";
    private static string AuthorizedDecisionSql(Guid id, Guid sessionId) => $"""INSERT INTO tagekyc.raw_export_authorization_decisions ("ExportDecisionId","PrincipalId","ClientApplicationId","ApiKeyId","RequestedVerificationSessionId","PolicyId","PolicyVersion","FingerprintHash","RawClassSelectionMode","Outcome","ResolvedVerificationSessionId","SessionOwnerClientApplicationId","SessionSubjectRef","SessionState","BoundRuleSetVersion","CurrentRuleSetVersion","EligibilityEvaluatedAtUtc","GrantPrincipalId","GrantPolicyId","GrantPolicyVersion","GrantRevision","LifecyclePolicyId","LifecyclePolicyVersion","LifecycleRevision","PurposeCode","RecipientClientApplicationId","ConsentScopeHash","SubjectConsentRecordId","ConsentRevision","ConsentValidFromUtc","ConsentEvaluatedAtUtc","PolicyPermitTtlSeconds","DecisionExpiresAtUtc","DecidedAtUtc") VALUES ('{id}','88b31000-0000-4000-8000-000000000011','88b31000-0000-4000-8000-000000000012','88b31000-0000-4000-8000-000000000013','{sessionId}','88b31000-0000-4000-8000-000000000015',1,decode(repeat('00',32),'hex'),'DefaultPolicySet','Authorized','{sessionId}','88b31000-0000-4000-8000-000000000012','b3-subject','Completed',1,1,transaction_timestamp(),'88b31000-0000-4000-8000-000000000011','88b31000-0000-4000-8000-000000000015',1,1,'88b31000-0000-4000-8000-000000000015',1,1,'SubjectRawBiometricExport','88b31000-0000-4000-8000-000000000012',decode(repeat('11',32),'hex'),'88b31000-0000-4000-8000-000000000031',1,transaction_timestamp(),transaction_timestamp(),300,transaction_timestamp()+interval '5 minutes',transaction_timestamp());""";
    private static string PermitSql(Guid permitId, Guid decisionId, Guid sessionId) => $"""INSERT INTO tagekyc.raw_export_authorization_permits ("PermitId","AuthorizationDecisionId","ResolvedVerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId","DecisionExpiresAtUtc","SchemaVersion","CreatedAt") VALUES ('{permitId}','{decisionId}','{sessionId}','b3-subject','88b31000-0000-4000-8000-000000000015',1,'SubjectRawBiometricExport','88b31000-0000-4000-8000-000000000012',transaction_timestamp()+interval '5 minutes',1,transaction_timestamp());""";
    private static async Task CleanupDecisionAsync(NpgsqlConnection connection, Guid id) { await ExecuteAsync(connection, $"RESET ROLE; ALTER TABLE tagekyc.raw_export_authorization_decisions DISABLE TRIGGER tr_b3_authz_decisions_append_only; DELETE FROM tagekyc.raw_export_authorization_decisions WHERE \"ExportDecisionId\"='{id}'; ALTER TABLE tagekyc.raw_export_authorization_decisions ENABLE TRIGGER tr_b3_authz_decisions_append_only;"); }
    private static async Task CleanupGraphAsync(NpgsqlConnection connection, Guid decisionId, Guid permitId) { await ExecuteAsync(connection, $"RESET ROLE; ALTER TABLE tagekyc.raw_export_authorization_idempotency DISABLE TRIGGER tr_b3_authz_idempotency_append_only; ALTER TABLE tagekyc.raw_export_decision_eligibility_causes DISABLE TRIGGER tr_b3_authz_eligibility_causes_append_only; ALTER TABLE tagekyc.raw_export_decision_fulfillment_refs DISABLE TRIGGER tr_b3_authz_fulfillment_refs_append_only; ALTER TABLE tagekyc.raw_export_decision_classes DISABLE TRIGGER tr_b3_authz_decision_classes_append_only; ALTER TABLE tagekyc.raw_export_permit_classes DISABLE TRIGGER tr_b3_authz_permit_classes_append_only; ALTER TABLE tagekyc.raw_export_authorization_permits DISABLE TRIGGER tr_b3_authz_permits_append_only; ALTER TABLE tagekyc.raw_export_authorization_decisions DISABLE TRIGGER tr_b3_authz_decisions_append_only; DELETE FROM tagekyc.raw_export_authorization_idempotency WHERE \"ExportDecisionId\"='{decisionId}'; DELETE FROM tagekyc.raw_export_decision_eligibility_causes WHERE \"ExportDecisionId\"='{decisionId}'; DELETE FROM tagekyc.raw_export_decision_fulfillment_refs WHERE \"ExportDecisionId\"='{decisionId}'; DELETE FROM tagekyc.raw_export_decision_classes WHERE \"ExportDecisionId\"='{decisionId}'; DELETE FROM tagekyc.raw_export_permit_classes WHERE \"PermitId\"='{permitId}'; DELETE FROM tagekyc.raw_export_authorization_permits WHERE \"PermitId\"='{permitId}'; DELETE FROM tagekyc.raw_export_authorization_decisions WHERE \"ExportDecisionId\"='{decisionId}'; ALTER TABLE tagekyc.raw_export_authorization_idempotency ENABLE TRIGGER tr_b3_authz_idempotency_append_only; ALTER TABLE tagekyc.raw_export_decision_eligibility_causes ENABLE TRIGGER tr_b3_authz_eligibility_causes_append_only; ALTER TABLE tagekyc.raw_export_decision_fulfillment_refs ENABLE TRIGGER tr_b3_authz_fulfillment_refs_append_only; ALTER TABLE tagekyc.raw_export_decision_classes ENABLE TRIGGER tr_b3_authz_decision_classes_append_only; ALTER TABLE tagekyc.raw_export_permit_classes ENABLE TRIGGER tr_b3_authz_permit_classes_append_only; ALTER TABLE tagekyc.raw_export_authorization_permits ENABLE TRIGGER tr_b3_authz_permits_append_only; ALTER TABLE tagekyc.raw_export_authorization_decisions ENABLE TRIGGER tr_b3_authz_decisions_append_only;"); }
    private static async Task<string> LandedAclAsync(TagEkycDbContext db) => (await db.Database.SqlQueryRaw<string>("SELECT has_table_privilege('tagekyc_runtime','tagekyc.raw_export_subject_consent_events','SELECT')::text || ':' || has_table_privilege('tagekyc_runtime','tagekyc.raw_export_subject_consent_events','INSERT')::text AS \"Value\"").SingleAsync());
    private static async Task<string> B3AclAsync(TagEkycDbContext db) => (await db.Database.SqlQueryRaw<string>("""
        SELECT string_agg(v, ',' ORDER BY v) AS "Value" FROM (
          SELECT table_name || ':s=' || has_table_privilege('tagekyc_runtime','tagekyc.' || table_name,'SELECT')::text || ':i=' || has_table_privilege('tagekyc_runtime','tagekyc.' || table_name,'INSERT')::text AS v
          FROM information_schema.tables
          WHERE table_schema='tagekyc' AND table_name = ANY(ARRAY['raw_export_authorization_idempotency','raw_export_authorization_decisions','raw_export_decision_eligibility_causes','raw_export_decision_fulfillment_refs','raw_export_decision_classes','raw_export_authorization_permits','raw_export_permit_classes'])
        ) q
        """).SingleAsync());
}
