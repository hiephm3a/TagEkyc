using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

#pragma warning disable EF1002 // Raw SQL is intentional in DB-bypass invariant tests; values are test-owned constants.

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88ARawExportPolicyCatalogTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Repository_adds_system_derived_policy_and_catalog_approves_inert_closure()
    {
        await using var db = postgres.CreateDbContext();
        var repository = new EfRawExportPolicyRepository(db);
        var policyId = Guid.Parse("88a00000-0000-5000-8000-000000000001");

        var draft = await repository.AddVersionAsync(new AddRawExportPolicyVersionCommand(
            policyId,
            ExpectedLatestVersion: 0,
            RawExportMode.ExternalExportOnlyNoRetain,
            Purpose: "tsp-raw-export",
            RetentionProfileRef: null,
            RetentionPurposeCode: "NO_RETAIN",
            RawExportConsentRequirement.Required,
            RecipientCategory: "TrustedAdapter",
            RecipientAssuranceRequirement: "ContractedTsp",
            ControllerRole: "Processor",
            ControllerEntityRef: "controller:tagekyc",
            ControllerJurisdiction: "VN",
            RecipientJurisdiction: "VN",
            ProcessingInfrastructureJurisdiction: "VN",
            TransferScenarioCode: null,
            TransferLegalBasisCode: null,
            new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage }));

        Assert.Equal(RawExportPolicyStatus.Draft, draft.Status);
        Assert.Equal(1, draft.PolicyVersion);
        Assert.Equal(RawExportPolicyConstants.RequirementRuleSetId, draft.RequirementRuleSetId);
        Assert.Equal(1, draft.RequirementRuleSetVersion);
        Assert.Equal(
            new[] { RawExportRequirementType.ConsentArtifact, RawExportRequirementType.LegalApproval },
            draft.Requirements.OrderBy(requirement => requirement.ToString()).ToArray());

        var approved = await repository.CatalogApproveAsync(new CloseRawExportPolicyVersionCommand(
            policyId,
            draft.PolicyVersion,
            "principal:catalog-author",
            "decision:approval:88a"));

        Assert.Equal(RawExportPolicyStatus.CatalogApproved, approved.Status);
        Assert.NotNull(approved.Closure);
        Assert.Equal(RawExportPolicyClosureType.CatalogApproved, approved.Closure!.ClosureType);

        var latestApproved = await repository.GetLatestCatalogApprovedVersionAsync(policyId);
        Assert.Equal(approved.PolicyVersion, latestApproved?.PolicyVersion);
    }

    [Fact]
    public async Task Requirement_rule_set_v1_matches_golden_vector()
    {
        await using var db = postgres.CreateDbContext();
        var expected = new HashSet<(string RuleSelector, string? SelectorOperand, string RequirementType)>
        {
            ("Always", null, "LegalApproval"),
            ("ConsentRequired", null, "ConsentArtifact"),
            ("AnyJurisdictionForeign", null, "CrossBorderAssessment"),
            ("ModeEquals", "EncryptedExportPacket", "RetentionSchedule"),
            ("ModeEquals", "EncryptedRawVaultRetained", "RetentionSchedule"),
            ("ModeEquals", "EncryptedRawVaultRetained", "Dpia"),
        };

        var actual = await QueryRuleSetV1Async(db);

        Assert.Equal(expected.Count, actual.Count);
        Assert.Empty(expected.Except(actual));
        Assert.Empty(actual.Except(expected));
    }

    [Fact]
    public async Task Requirement_rule_tuple_index_is_database_owned_unique_expression_index()
    {
        await using var db = postgres.CreateDbContext();
        var indexDefinition = await QueryScalarStringAsync(db, """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'tagekyc'
              AND tablename = 'raw_export_requirement_rules'
              AND indexname = 'UX_raw_export_requirement_rules_tuple';
            """);

        Assert.NotNull(indexDefinition);
        Assert.Contains("CREATE UNIQUE INDEX", indexDefinition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COALESCE", indexDefinition, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Runtime_privilege_validator_rejects_rule_table_mutation_privilege()
    {
        await using var db = postgres.CreateDbContext();

        var exception = await Assert.ThrowsAsync<RawExportRuntimePrivilegeException>(() =>
            new RawExportRuntimePrivilegeValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(RawExportRuntimePrivilegeValidator.RuleTableMutationPrivilege, exception.Code);
    }

    [Fact]
    public async Task Db_invariants_reject_raw_sql_bypasses()
    {
        await using var db = postgres.CreateDbContext();

        await AssertAppendOnlyAsync(db, "raw_export_policy_versions", "\"Purpose\" = \"Purpose\"");
        await AssertAppendOnlyAsync(db, "raw_export_policy_allowed_classes", "\"RawClass\" = \"RawClass\"");
        await AssertAppendOnlyAsync(db, "raw_export_policy_requirements", "\"RequirementType\" = \"RequirementType\"");
        await AssertAppendOnlyAsync(db, "raw_export_policy_closures", "\"ClosureType\" = \"ClosureType\"");
        await AssertAppendOnlyAsync(db, "raw_export_requirement_rule_sets", "\"MigrationRef\" = \"MigrationRef\"");
        await AssertAppendOnlyAsync(db, "raw_export_requirement_rules", "\"RequirementType\" = \"RequirementType\"");

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_policy_closures ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef") VALUES ('88a00000-0000-5000-8000-00000000ffff',1,'CatalogApproved',transaction_timestamp(),'principal','decision');"""));

        var incompletePolicyId = Guid.Parse("88a00000-0000-5000-8000-000000000010");
        await InsertDraftAsync(db, incompletePolicyId, includeConsentArtifact: false);
        await Assert.ThrowsAsync<PostgresException>(() => InsertClosureAsync(db, incompletePolicyId, "CatalogApproved"));

        var extraPolicyId = Guid.Parse("88a00000-0000-5000-8000-000000000011");
        await InsertDraftAsync(db, extraPolicyId, extraRequirement: "Dpia");
        await Assert.ThrowsAsync<PostgresException>(() => InsertClosureAsync(db, extraPolicyId, "CatalogApproved"));

        var childPolicyId = Guid.Parse("88a00000-0000-5000-8000-000000000012");
        await InsertDraftAsync(db, childPolicyId);
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"""INSERT INTO tagekyc.raw_export_policy_allowed_classes ("PolicyId","PolicyVersion","RawClass","CreatedAt") VALUES ('{childPolicyId}',1,'ChipDg2Portrait',transaction_timestamp());"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"""INSERT INTO tagekyc.raw_export_policy_requirements ("PolicyId","PolicyVersion","RequirementType","CreatedAt") VALUES ('{childPolicyId}',1,'Dpia',transaction_timestamp());"""));

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_policy_versions ("PolicyId","PolicyVersion","Mode","Purpose","ConsentRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt") VALUES ('88a00000-0000-5000-8000-000000000020',2,'ExternalExportOnlyNoRetain','purpose','Required','Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,transaction_timestamp());"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_policy_versions ("PolicyId","PolicyVersion","Mode","Purpose","ConsentRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt") VALUES ('88a00000-0000-5000-8000-000000000021',1,'ExternalExportOnlyNoRetain','purpose','Required','Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',99,transaction_timestamp());"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_requirement_rule_sets ("RuleSetId","RuleSetVersion","MigrationRef","HomeJurisdictionCode","CreatedAt") VALUES ('LEGACY',1,'test','VN',transaction_timestamp());"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_requirement_rules ("Id","RuleSetId","RuleSetVersion","RuleSelector","SelectorOperand","RequirementType","CreatedAt") VALUES ('88a00000-0000-5000-8000-00000000aaa1','RAW_EXPORT_REQUIREMENTS',1,'ModeEquals','encrypted_magic_mode','Dpia',transaction_timestamp());"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_requirement_rules ("Id","RuleSetId","RuleSetVersion","RuleSelector","SelectorOperand","RequirementType","CreatedAt") VALUES ('88a00000-0000-5000-8000-00000000aaa2','RAW_EXPORT_REQUIREMENTS',1,'Always','','Dpia',transaction_timestamp());"""));
    }

    [Fact]
    public async Task Version_chain_rejects_open_draft_gap_and_allows_abandon_then_reauthor()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88a00000-0000-5000-8000-000000000030");
        await InsertDraftAsync(db, policyId);

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"""INSERT INTO tagekyc.raw_export_policy_versions ("PolicyId","PolicyVersion","Mode","Purpose","ConsentRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt") VALUES ('{policyId}',2,'ExternalExportOnlyNoRetain','purpose','Required','Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,transaction_timestamp());"""));

        await InsertClosureAsync(db, policyId, "Abandoned");

        var repository = new EfRawExportPolicyRepository(db);
        var second = await repository.AddVersionAsync(new AddRawExportPolicyVersionCommand(
            policyId,
            ExpectedLatestVersion: 1,
            RawExportMode.ExternalExportOnlyNoRetain,
            "reauthored",
            null,
            "NO_RETAIN",
            RawExportConsentRequirement.Required,
            null,
            null,
            "Processor",
            "controller",
            "VN",
            "VN",
            "VN",
            null,
            null,
            new HashSet<RawExportRawClass> { RawExportRawClass.LiveSelfieImage }));

        Assert.Equal(2, second.PolicyVersion);
        Assert.Equal(RawExportPolicyStatus.Draft, second.Status);
    }

    [Fact]
    public async Task Concurrent_approve_vs_abandon_allows_exactly_one_closure()
    {
        var policyId = Guid.Parse("88a00000-0000-5000-8000-000000000040");
        await using (var setup = postgres.CreateDbContext())
        {
            await InsertDraftAsync(setup, policyId);
        }

        var results = await Task.WhenAll(
            InsertClosureInCommittedTransactionAsync(postgres.ConnectionString, policyId, "CatalogApproved"),
            InsertClosureInCommittedTransactionAsync(postgres.ConnectionString, policyId, "Abandoned"));

        await using var check = postgres.CreateDbContext();
        var closures = await check.RawExportPolicyClosures.AsNoTracking()
            .Where(row => row.PolicyId == policyId)
            .ToListAsync();
        Assert.Single(results, result => result.Succeeded);
        var loser = Assert.Single(results, result => !result.Succeeded);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, loser.SqlState);
        Assert.Single(closures);
        Assert.Contains(closures[0].ClosureType, new[] { "CatalogApproved", "Abandoned" });
    }

    [Fact]
    public async Task Csharp_and_database_requirement_derivation_have_parity_for_home_jurisdiction_vectors()
    {
        await using var db = postgres.CreateDbContext();
        var repository = new EfRawExportPolicyRepository(db);
        var policyId = Guid.Parse("88a00000-0000-5000-8000-000000000050");

        var draft = await repository.AddVersionAsync(new AddRawExportPolicyVersionCommand(
            policyId,
            0,
            RawExportMode.EncryptedRawVaultRetained,
            "vault-policy",
            "retention:short",
            "RAW_BIO_EXPORT",
            RawExportConsentRequirement.Required,
            null,
            null,
            "Processor",
            "controller",
            "VN",
            "SG",
            "VN",
            "cross-border",
            "contract",
            new HashSet<RawExportRawClass> { RawExportRawClass.ChipDg2Portrait }));

        Assert.Equal(
            new[]
            {
                RawExportRequirementType.ConsentArtifact,
                RawExportRequirementType.CrossBorderAssessment,
                RawExportRequirementType.Dpia,
                RawExportRequirementType.LegalApproval,
                RawExportRequirementType.RetentionSchedule,
            },
            draft.Requirements.OrderBy(requirement => requirement.ToString()).ToArray());

        var approved = await repository.CatalogApproveAsync(new CloseRawExportPolicyVersionCommand(
            policyId,
            1,
            "principal:catalog-author",
            "decision:cross-border"));
        Assert.Equal(RawExportPolicyStatus.CatalogApproved, approved.Status);
    }

    [Fact]
    public async Task Rule_set_publication_and_binding_are_current_family_and_ruleful()
    {
        await using var db = postgres.CreateDbContext();
        await SeedRuleSetVersionAsync(db, 2, includeRules: true);

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_policy_versions ("PolicyId","PolicyVersion","Mode","Purpose","ConsentRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt") VALUES ('88a00000-0000-5000-8000-000000000060',1,'ExternalExportOnlyNoRetain','purpose','Required','Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,transaction_timestamp());"""));

        await SeedRuleSetVersionAsync(db, 3, includeRules: false);
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""INSERT INTO tagekyc.raw_export_policy_versions ("PolicyId","PolicyVersion","Mode","Purpose","ConsentRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt") VALUES ('88a00000-0000-5000-8000-000000000061',1,'ExternalExportOnlyNoRetain','purpose','Required','Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',3,transaction_timestamp());"""));
    }

    [Fact]
    public async Task Closure_timestamp_is_database_stamped_and_empty_actor_or_ref_is_rejected()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88a00000-0000-5000-8000-000000000070");
        await InsertDraftAsync(db, policyId);

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"""INSERT INTO tagekyc.raw_export_policy_closures ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef") VALUES ('{policyId}',1,'Abandoned','2000-01-01T00:00:00Z','','decision');"""));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"""INSERT INTO tagekyc.raw_export_policy_closures ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef") VALUES ('{policyId}',1,'Abandoned','2000-01-01T00:00:00Z','principal','');"""));

        await InsertClosureAsync(db, policyId, "Abandoned");
        var closure = await db.RawExportPolicyClosures.AsNoTracking().SingleAsync(row => row.PolicyId == policyId);
        Assert.True(closure.ClosedAtUtc > new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static async Task AssertAppendOnlyAsync(TagEkycDbContext db, string tableName, string updateSet)
    {
        var policyId = Guid.Parse("88a00000-0000-5000-8000-00000000a000");
        if (!await db.RawExportPolicyVersions.AnyAsync(row => row.PolicyId == policyId))
        {
            await InsertDraftAsync(db, policyId);
            await InsertClosureAsync(db, policyId, "Abandoned");
        }

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"UPDATE tagekyc.{tableName} SET {updateSet};"));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"DELETE FROM tagekyc.{tableName};"));
    }

    private static async Task InsertDraftAsync(
        TagEkycDbContext db,
        Guid policyId,
        bool includeConsentArtifact = true,
        string? extraRequirement = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_versions
                ("PolicyId","PolicyVersion","Mode","Purpose","RetentionPurposeCode","ConsentRequirement","ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt")
            VALUES
                ('{policyId}',1,'ExternalExportOnlyNoRetain','purpose','NO_RETAIN','Required','Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,transaction_timestamp());
            """);
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_allowed_classes
                ("PolicyId","PolicyVersion","RawClass","CreatedAt")
            VALUES
                ('{policyId}',1,'LiveSelfieImage',transaction_timestamp());
            """);
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_requirements
                ("PolicyId","PolicyVersion","RequirementType","CreatedAt")
            VALUES
                ('{policyId}',1,'LegalApproval',transaction_timestamp());
            """);
        if (includeConsentArtifact)
        {
            await db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO tagekyc.raw_export_policy_requirements
                    ("PolicyId","PolicyVersion","RequirementType","CreatedAt")
                VALUES
                    ('{policyId}',1,'ConsentArtifact',transaction_timestamp());
                """);
        }

        if (extraRequirement is not null)
        {
            await db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO tagekyc.raw_export_policy_requirements
                    ("PolicyId","PolicyVersion","RequirementType","CreatedAt")
                VALUES
                    ('{policyId}',1,'{extraRequirement}',transaction_timestamp());
                """);
        }

        await transaction.CommitAsync();
    }

    private static Task InsertClosureAsync(TagEkycDbContext db, Guid policyId, string closureType)
        => db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_closures
                ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef")
            VALUES
                ('{policyId}',1,'{closureType}','2000-01-01T00:00:00Z','principal:tester','decision:{closureType}');
            """);

    private static async Task<(bool Succeeded, string? SqlState)> InsertClosureInCommittedTransactionAsync(
        string connectionString,
        Guid policyId,
        string closureType)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteClosureCommandAsync(connection, transaction, policyId, closureType);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.SqlState);
        }
    }

    private static async Task ExecuteClosureCommandAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid policyId,
        string closureType)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tagekyc.raw_export_policy_closures
                ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef")
            VALUES
                ($1,1,$2,transaction_timestamp(),'principal:concurrent',$3);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue(policyId);
        command.Parameters.AddWithValue(closureType);
        command.Parameters.AddWithValue($"decision:{closureType}");
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SeedRuleSetVersionAsync(TagEkycDbContext db, int version, bool includeRules)
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rule_sets DISABLE TRIGGER tr_raw_export_rule_sets_runtime_mutation_reject;");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rules DISABLE TRIGGER tr_raw_export_rules_runtime_mutation_reject;");
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO tagekyc.raw_export_requirement_rule_sets
                    ("RuleSetId","RuleSetVersion","MigrationRef","HomeJurisdictionCode","CreatedAt")
                VALUES
                    ('RAW_EXPORT_REQUIREMENTS',{version},'test-rule-set-{version}','VN',transaction_timestamp());
                """);

            if (includeRules)
            {
                await db.Database.ExecuteSqlRawAsync($"""
                    INSERT INTO tagekyc.raw_export_requirement_rules
                        ("Id","RuleSetId","RuleSetVersion","RuleSelector","SelectorOperand","RequirementType","CreatedAt")
                    VALUES
                        ('88a00000-0000-5000-8000-00000000b0{version}1','RAW_EXPORT_REQUIREMENTS',{version},'Always',NULL,'LegalApproval',transaction_timestamp()),
                        ('88a00000-0000-5000-8000-00000000b0{version}2','RAW_EXPORT_REQUIREMENTS',{version},'ConsentRequired',NULL,'ConsentArtifact',transaction_timestamp());
                    """);
            }
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rules ENABLE TRIGGER tr_raw_export_rules_runtime_mutation_reject;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rule_sets ENABLE TRIGGER tr_raw_export_rule_sets_runtime_mutation_reject;");
        }
    }

    private static async Task<HashSet<(string RuleSelector, string? SelectorOperand, string RequirementType)>> QueryRuleSetV1Async(
        TagEkycDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "RuleSelector", "SelectorOperand", "RequirementType"
            FROM tagekyc.raw_export_requirement_rules
            WHERE "RuleSetId" = 'RAW_EXPORT_REQUIREMENTS'
              AND "RuleSetVersion" = 1;
            """;

        var result = new HashSet<(string RuleSelector, string? SelectorOperand, string RequirementType)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add((
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2)));
        }

        return result;
    }

    private static async Task<string?> QueryScalarStringAsync(TagEkycDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync() as string;
    }
}

#pragma warning restore EF1002
