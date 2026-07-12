using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

#pragma warning disable EF1002

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B1RawExportControlPlaneTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private static readonly Guid AdminPrincipal = Guid.Parse("88b10000-0000-5000-8000-000000000001");
    private static readonly Guid RecorderPrincipal = Guid.Parse("88b10000-0000-5000-8000-000000000002");
    private static readonly Guid ConsumerPrincipal = Guid.Parse("88b10000-0000-5000-8000-000000000003");
    private static readonly Guid OtherConsumerPrincipal = Guid.Parse("88b10000-0000-5000-8000-000000000004");

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PrincipalId_is_surfaced_from_postgres_api_key_store()
    {
        await using var db = postgres.CreateDbContext();
        var pepper = new ApiKeyStorePepper(Tip84BTestSupport.Pepper);
        var generator = new FixedApiKeyGenerator(Tip84BTestSupport.PresentedKey);
        var provisioning = new ApiKeyProvisioningService(
            db,
            pepper,
            new TagEkyc.Application.LocalDev.LocalDevRuntimePolicySource(),
            generator);
        var result = await provisioning.ProvisionAsync(new ApiKeyProvisioningCommand(
            TagEkyc.Application.LocalDev.LocalDevRuntimePolicySource.BusinessClientId,
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.session.read" },
            PrincipalId: ConsumerPrincipal));

        var resolved = await new PostgresHashedApiKeyStore(db, pepper).FindByPresentedKeyAsync(result.PresentedKey);

        Assert.NotNull(resolved);
        Assert.Equal(ConsumerPrincipal, resolved!.PrincipalId);
        var context = new AuthenticatedClientContext(
            resolved.ApiKeyId,
            resolved.ClientApplicationId,
            resolved.KeyPrefix,
            resolved.CallerCategory,
            resolved.Scopes,
            PrincipalId: resolved.PrincipalId);
        Assert.Equal(ConsumerPrincipal, context.PrincipalId);
    }

    [Fact]
    public async Task Security_definer_functions_are_hardened_and_direct_event_insert_is_rejected()
    {
        await using var db = postgres.CreateDbContext();
        var expected = ExpectedFunctionSecurityManifest();
        var functions = await QueryFunctionSecurityAsync(db);

        Assert.Equal(
            expected.Select(item => item.Name).Order(StringComparer.Ordinal),
            functions.Keys.Order(StringComparer.Ordinal));
        foreach (var item in expected)
        {
            var functionInfo = functions[item.Name];
            Assert.Equal("tagekyc_raw_export_deployer", functionInfo.Owner);
            Assert.False(functionInfo.OwnerCanLogin);
            Assert.Equal(item.SecurityDefiner, functionInfo.SecurityDefiner);
            Assert.Contains("search_path=pg_catalog", functionInfo.Config);
            Assert.False(functionInfo.PublicExecute);
            Assert.Equal(item.RuntimeExecute, functionInfo.RuntimeExecute);
            Assert.Equal(item.BootstrapperExecute, functionInfo.BootstrapperExecute);
        }

        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_export_grants
                    ("PrincipalId","PolicyId","PolicyVersion","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                VALUES
                    ('88b10000-0000-5000-8000-000000000003','88b10000-0000-5000-8000-00000000aaaa',1,1,'Granted','decision:direct','88b10000-0000-5000-8000-000000000001',transaction_timestamp());
                """));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_export_control_authorities
                    ("AuthorityEventId","PrincipalId","AuthorityType","ScopeType","ScopeId","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                VALUES
                    (gen_random_uuid(),'88b10000-0000-5000-8000-000000000002','GrantAdmin','Policy','88b10000-0000-5000-8000-00000000aaaa',1,'Granted','decision:direct','88b10000-0000-5000-8000-000000000001',transaction_timestamp());
                """));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_export_fulfillments
                    ("FulfillmentEventId","PolicyId","PolicyVersion","RequirementType","Revision","EventType","ArtifactRef","ArtifactVersion","ValidFromUtc","RecordedByPrincipalId","FulfillmentDecisionRef","RecordedAtUtc")
                VALUES
                    (gen_random_uuid(),'88b10000-0000-5000-8000-00000000aaaa',1,'LegalApproval',1,'Accepted','artifact','v1',transaction_timestamp(),'88b10000-0000-5000-8000-000000000001','decision:direct',transaction_timestamp());
                """));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_export_policy_lifecycle
                    ("PolicyId","PolicyVersion","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                VALUES
                    ('88b10000-0000-5000-8000-00000000aaaa',1,99,'Activated','decision:direct','88b10000-0000-5000-8000-000000000001',transaction_timestamp());
                """));
    }

    [Fact]
    public async Task Append_functions_reject_missing_blank_malformed_actor_and_set_local_does_not_leak()
    {
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000010");
        await using (var setup = postgres.CreateDbContext())
        {
            await SeedApprovedPolicyAsync(setup, policyId);
            await BootstrapRootsAsync(setup, AdminPrincipal);
        }

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(connection, null, """
            SELECT tagekyc.raw_export_append_grant(
                '88b10000-0000-5000-8000-000000000003','88b10000-0000-5000-8000-000000000010',1,0,'Granted',NULL,'decision:no-actor');
            """));

        await using (var tx = await connection.BeginTransactionAsync())
        {
            await ExecuteScalarAsync(connection, tx, "SELECT set_config('tagekyc.actor_principal_id','',true);");
            await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(connection, tx, """
                SELECT tagekyc.raw_export_append_grant(
                    '88b10000-0000-5000-8000-000000000003','88b10000-0000-5000-8000-000000000010',1,0,'Granted',NULL,'decision:blank');
                """));
        }

        await using (var tx = await connection.BeginTransactionAsync())
        {
            await ExecuteScalarAsync(connection, tx, "SELECT set_config('tagekyc.actor_principal_id','not-a-guid',true);");
            await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(connection, tx, """
                SELECT tagekyc.raw_export_append_grant(
                    '88b10000-0000-5000-8000-000000000003','88b10000-0000-5000-8000-000000000010',1,0,'Granted',NULL,'decision:malformed');
                """));
        }

        await using (var tx = await connection.BeginTransactionAsync())
        {
            await ExecuteScalarAsync(connection, tx, $"SET LOCAL tagekyc.actor_principal_id = '{AdminPrincipal:D}';");
            await tx.CommitAsync();
        }

        await Assert.ThrowsAsync<PostgresException>(() => ExecuteScalarAsync(connection, null, """
            SELECT tagekyc.raw_export_append_grant(
                '88b10000-0000-5000-8000-000000000003','88b10000-0000-5000-8000-000000000010',1,0,'Granted',NULL,'decision:leak');
            """));
    }

    [Fact]
    public async Task Activation_and_resolver_ignore_subject_scoped_consent_artifact_but_bind_policy_refs()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000020");
        await SeedApprovedPolicyAsync(db, policyId, includeConsentArtifact: true);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        await repository.GrantExportPolicyAsync(Grant(policyId, ConsumerPrincipal, 0));
        await repository.GrantControlAuthorityAsync(RecorderAuthority(policyId, RecorderPrincipal, RawExportRequirementType.LegalApproval, 0));
        await repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 0, null));
        await repository.ActivatePolicyAsync(Lifecycle(policyId, 0, "decision:activate"));

        await using var transaction = await db.Database.BeginTransactionAsync();
        var snapshot = await repository.ResolveExportEligibilityForAuthorizationAsync(ConsumerPrincipal, policyId, 1);
        await transaction.CommitAsync();

        Assert.Equal(RawExportEligibilityState.Active, snapshot.State);
        Assert.Null(snapshot.PrimaryCause);
        Assert.Empty(snapshot.Causes);
        Assert.Equal(1, snapshot.GrantRef?.Revision);
        Assert.Equal(1, snapshot.LifecycleRef?.Revision);
        Assert.Single(snapshot.FulfillmentRefs);
        Assert.Equal(RawExportRequirementType.LegalApproval, snapshot.FulfillmentRefs[0].RequirementType);
    }

    [Fact]
    public async Task Scope_matching_negatives_and_consent_artifact_fulfillment_are_denied()
    {
        await using var db = postgres.CreateDbContext();
        var policyA = Guid.Parse("88b10000-0000-5000-8000-000000000030");
        var policyB = Guid.Parse("88b10000-0000-5000-8000-000000000031");
        await SeedApprovedPolicyAsync(db, policyA);
        await SeedApprovedPolicyAsync(db, policyB);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        await repository.GrantControlAuthorityAsync(RecorderAuthority(policyA, RecorderPrincipal, RawExportRequirementType.LegalApproval, 0));

        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.AcceptFulfillmentAsync(Accept(policyB, RawExportRequirementType.LegalApproval, 0, null)));
        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.AcceptFulfillmentAsync(Accept(policyA, RawExportRequirementType.ConsentArtifact, 0, null)));
        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.GrantControlAuthorityAsync(new RawExportAuthorityCommand(
                AdminPrincipal,
                OtherConsumerPrincipal,
                RawExportAuthorityType.GrantAdmin,
                RawExportAuthorityScopeType.Global,
                null,
                null,
                0,
                "decision:runtime-global")));
        await Assert.ThrowsAsync<DbUpdateException>(() =>
        {
            db.RawExportControlAuthorities.Add(new()
            {
                AuthorityEventId = Guid.NewGuid(),
                PrincipalId = OtherConsumerPrincipal,
                AuthorityType = "GrantAdmin",
                ScopeType = "Policy",
                ScopeId = null,
                Revision = 1,
                EventType = "Granted",
                DecisionRef = "decision:bad-shape",
                RecordedByPrincipalId = AdminPrincipal,
                RecordedAtUtc = DateTimeOffset.UtcNow,
            });
            return db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Export_grant_does_not_authorize_policy_fulfillment_recording()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000032");
        await SeedApprovedPolicyAsync(db, policyId);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        await repository.GrantExportPolicyAsync(Grant(policyId, ConsumerPrincipal, 0));

        var denied = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.AcceptFulfillmentAsync(new RawExportFulfillmentAcceptCommand(
                ConsumerPrincipal,
                policyId,
                1,
                RawExportRequirementType.LegalApproval,
                0,
                null,
                "artifact:consumer",
                "v1",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1),
                "decision:consumer-fulfillment")));
        Assert.Contains("RAW_EXPORT_AUTHORITY_DENIED", denied.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fulfillment_renewal_withdrawal_and_half_open_validity_are_enforced()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000040");
        await SeedApprovedPolicyAsync(db, policyId);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        await repository.GrantControlAuthorityAsync(RecorderAuthority(policyId, RecorderPrincipal, RawExportRequirementType.LegalApproval, 0));
        var expired = await repository.AcceptFulfillmentAsync(Accept(
            policyId,
            RawExportRequirementType.LegalApproval,
            0,
            null,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1)));
        var renewed = await repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 1, expired));
        await repository.WithdrawFulfillmentAsync(new RawExportFulfillmentWithdrawCommand(
            RecorderPrincipal,
            policyId,
            1,
            RawExportRequirementType.LegalApproval,
            2,
            renewed,
            "decision:withdraw"));
        var renewedAfterWithdraw = await repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 3, renewed));

        Assert.Equal(4, renewedAfterWithdraw);
        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 4, 1)));
        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.WithdrawFulfillmentAsync(new RawExportFulfillmentWithdrawCommand(
                RecorderPrincipal,
                policyId,
                1,
                RawExportRequirementType.LegalApproval,
                4,
                1,
                "decision:stale-withdraw")));
        var invalidInstant = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.AcceptFulfillmentAsync(Accept(
                policyId,
                RawExportRequirementType.LegalApproval,
                4,
                renewedAfterWithdraw,
                invalidInstant,
                invalidInstant)));
    }

    [Fact]
    public async Task Lifecycle_activation_bypass_and_revoked_terminal_are_rejected()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000050");
        await SeedApprovedPolicyAsync(db, policyId);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.ActivatePolicyAsync(Lifecycle(policyId, 0, "decision:missing-fulfillment")));

        await repository.GrantControlAuthorityAsync(RecorderAuthority(policyId, RecorderPrincipal, RawExportRequirementType.LegalApproval, 0));
        await repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 0, null));
        await repository.ActivatePolicyAsync(Lifecycle(policyId, 0, "decision:activate"));
        await repository.RevokePolicyAsync(Lifecycle(policyId, 1, "decision:revoke"));

        await Assert.ThrowsAsync<PostgresException>(() =>
            repository.ActivatePolicyAsync(Lifecycle(policyId, 2, "decision:after-revoke")));
        await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO tagekyc.raw_export_policy_lifecycle
                    ("PolicyId","PolicyVersion","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                VALUES
                    ('{policyId}',1,3,'Activated','decision:bypass','{AdminPrincipal}',transaction_timestamp());
                """));
    }

    [Fact]
    public async Task Activation_rejects_policy_bound_to_stale_rule_set_after_v2_publish()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000051");
        await SeedApprovedPolicyAsync(db, policyId);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        await repository.GrantControlAuthorityAsync(RecorderAuthority(policyId, RecorderPrincipal, RawExportRequirementType.LegalApproval, 0));
        await repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 0, null));
        await PublishRuleSetV2Async(db);

        var denied = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.ActivatePolicyAsync(Lifecycle(policyId, 0, "decision:stale-rule-activate")));
        Assert.Contains("RAW_EXPORT_ACTIVATION_GATE_DENIED", denied.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolver_returns_deterministic_causes_and_authoritative_db_time_boundaries()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000060");
        await SeedApprovedPolicyAsync(db, policyId);
        await BootstrapRootsAsync(db, AdminPrincipal);
        var repository = new EfRawExportControlPlaneRepository(db);

        var noAmbient = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.ResolveExportEligibilityForAuthorizationAsync(ConsumerPrincipal, policyId, 1));
        Assert.Equal("RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION", noAmbient.Message);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var snapshot = await repository.ResolveExportEligibilityForAuthorizationAsync(ConsumerPrincipal, policyId, 1);
        await transaction.CommitAsync();

        Assert.Equal(RawExportEligibilityState.Inactive, snapshot.State);
        Assert.Equal(RawExportEligibilityCause.NotActivated, snapshot.PrimaryCause);
        Assert.Contains(RawExportEligibilityCause.NoGrant, snapshot.Causes);
        Assert.True(DateTimeOffset.UtcNow.AddMinutes(-1) < snapshot.EvaluatedAtUtc);
    }

    [Fact]
    public async Task Readiness_fails_when_required_root_is_missing_or_dev_default()
    {
        await using var missing = postgres.CreateDbContext();
        var missingException = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(() =>
            new RawExportControlPlaneReadinessValidator(missing).ValidateAsync(CancellationToken.None));
        Assert.Equal(RawExportControlPlaneReadinessValidator.RootAuthorityMissing, missingException.Code);

        await BootstrapRootsAsync(missing, RawExportControlPlaneConstants.DeploymentPrincipalId);
        var devException = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(() =>
            new RawExportControlPlaneReadinessValidator(missing).ValidateAsync(CancellationToken.None));
        Assert.Equal(RawExportControlPlaneReadinessValidator.DevDefaultPrincipalForbidden, devException.Code);
    }

    [Fact]
    public async Task Readiness_rejects_owner_or_migration_principal_as_runtime_for_control_plane_tables()
    {
        await using var db = postgres.CreateDbContext();
        await BootstrapRootsAsync(db, AdminPrincipal);

        var exception = await Assert.ThrowsAsync<RawExportControlPlaneReadinessException>(() =>
            new RawExportControlPlaneReadinessValidator(db).ValidateAsync(CancellationToken.None));

        Assert.Equal(RawExportControlPlaneReadinessValidator.EventTableMutationPrivilege, exception.Code);
    }

    [Fact]
    public async Task Two_connection_same_revision_grant_race_has_exactly_one_winner()
    {
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000070");
        await using (var setup = postgres.CreateDbContext())
        {
            await SeedApprovedPolicyAsync(setup, policyId);
            await BootstrapRootsAsync(setup, AdminPrincipal);
        }

        var results = await Task.WhenAll(
            ExecuteGrantFunctionInTransactionAsync(policyId, ConsumerPrincipal, 0, "decision:race-a"),
            ExecuteGrantFunctionInTransactionAsync(policyId, ConsumerPrincipal, 0, "decision:race-b"));

        Assert.Single(results, result => result.Succeeded);
        var loser = Assert.Single(results, result => !result.Succeeded);
        Assert.Contains("RAW_EXPORT_REVISION_CONFLICT", loser.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Two_connection_resolver_shared_lock_blocks_revoke_until_authorization_transaction_finishes()
    {
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000080");
        await using (var setup = postgres.CreateDbContext())
        {
            await SeedApprovedPolicyAsync(setup, policyId);
            await BootstrapRootsAsync(setup, AdminPrincipal);
            var repository = new EfRawExportControlPlaneRepository(setup);
            await repository.GrantExportPolicyAsync(Grant(policyId, ConsumerPrincipal, 0));
        }

        await using var resolverDb = postgres.CreateDbContext();
        await using var resolverTx = await resolverDb.Database.BeginTransactionAsync();
        var snapshot = await new EfRawExportControlPlaneRepository(resolverDb)
            .ResolveExportEligibilityForAuthorizationAsync(ConsumerPrincipal, policyId, 1);
        Assert.Contains(RawExportEligibilityCause.NotActivated, snapshot.Causes);

        var revokeTask = ExecuteGrantFunctionInTransactionAsync(policyId, ConsumerPrincipal, 1, "decision:revoke", "Revoked");
        await Task.Delay(200);
        Assert.False(revokeTask.IsCompleted);

        await resolverTx.CommitAsync();
        var revoke = await revokeTask;
        Assert.True(revoke.Succeeded);
    }

    [Fact]
    public async Task Two_connection_resolver_shared_lock_blocks_withdraw_until_authorization_transaction_finishes()
    {
        var policyId = Guid.Parse("88b10000-0000-5000-8000-000000000081");
        await using (var setup = postgres.CreateDbContext())
        {
            await SeedApprovedPolicyAsync(setup, policyId);
            await BootstrapRootsAsync(setup, AdminPrincipal);
            var repository = new EfRawExportControlPlaneRepository(setup);
            await repository.GrantControlAuthorityAsync(RecorderAuthority(policyId, RecorderPrincipal, RawExportRequirementType.LegalApproval, 0));
            await repository.AcceptFulfillmentAsync(Accept(policyId, RawExportRequirementType.LegalApproval, 0, null));
        }

        await using var resolverDb = postgres.CreateDbContext();
        await using var resolverTx = await resolverDb.Database.BeginTransactionAsync();
        await new EfRawExportControlPlaneRepository(resolverDb)
            .ResolveExportEligibilityForAuthorizationAsync(ConsumerPrincipal, policyId, 1);

        var withdrawTask = ExecuteFulfillmentWithdrawFunctionInTransactionAsync(policyId, RawExportRequirementType.LegalApproval, 1, 1);
        await Task.Delay(200);
        Assert.False(withdrawTask.IsCompleted);

        await resolverTx.CommitAsync();
        var withdraw = await withdrawTask;
        Assert.True(withdraw.Succeeded);
    }

    private RawExportGrantCommand Grant(Guid policyId, Guid principalId, int expectedRevision) =>
        new(AdminPrincipal, principalId, policyId, 1, expectedRevision, null, $"decision:grant:{expectedRevision}");

    private RawExportAuthorityCommand RecorderAuthority(Guid policyId, Guid principalId, RawExportRequirementType requirementType, int expectedRevision) =>
        new(AdminPrincipal, principalId, RawExportAuthorityType.FulfillmentRecorder, RawExportAuthorityScopeType.Policy, policyId, requirementType, expectedRevision, $"decision:recorder:{requirementType}:{expectedRevision}");

    private RawExportFulfillmentAcceptCommand Accept(
        Guid policyId,
        RawExportRequirementType requirementType,
        int expectedRevision,
        int? supersedesRevision,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null) =>
        new(
            RecorderPrincipal,
            policyId,
            1,
            requirementType,
            expectedRevision,
            supersedesRevision,
            $"artifact:{requirementType}:{expectedRevision + 1}",
            "v1",
            validFrom ?? DateTimeOffset.UtcNow.AddMinutes(-1),
            validUntil ?? DateTimeOffset.UtcNow.AddHours(1),
            $"decision:fulfillment:{requirementType}:{expectedRevision + 1}");

    private RawExportLifecycleCommand Lifecycle(Guid policyId, int expectedRevision, string decisionRef) =>
        new(AdminPrincipal, policyId, 1, expectedRevision, decisionRef);

    private static async Task BootstrapRootsAsync(TagEkycDbContext db, Guid principalId)
    {
        foreach (var authority in new[] { "GrantAdmin", "RecorderAuthorityAdmin", "ActivationAuthority" })
        {
            await db.Database.ExecuteSqlRawAsync($"""
                SELECT tagekyc.raw_export_bootstrap_global_authority('{principalId}', '{authority}', 'decision:bootstrap:{authority}');
                """);
        }
    }

    private static async Task PublishRuleSetV2Async(TagEkycDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rule_sets DISABLE TRIGGER tr_raw_export_rule_sets_runtime_mutation_reject;");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rules DISABLE TRIGGER tr_raw_export_rules_runtime_mutation_reject;");
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_export_requirement_rule_sets
                    ("RuleSetId", "RuleSetVersion", "MigrationRef", "HomeJurisdictionCode", "CreatedAt")
                VALUES
                    ('RAW_EXPORT_REQUIREMENTS', 2, 'tip88b1-test-v2', 'VN', transaction_timestamp());
                """);

            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.raw_export_requirement_rules
                    ("Id", "RuleSetId", "RuleSetVersion", "RuleSelector", "SelectorOperand", "RequirementType", "CreatedAt")
                VALUES
                    ('00000000-0000-5000-8000-000000088b21', 'RAW_EXPORT_REQUIREMENTS', 2, 'Always', NULL, 'LegalApproval', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088b22', 'RAW_EXPORT_REQUIREMENTS', 2, 'ModeEquals', 'EncryptedExportPacket', 'RetentionSchedule', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088b23', 'RAW_EXPORT_REQUIREMENTS', 2, 'ModeEquals', 'EncryptedRawVaultRetained', 'RetentionSchedule', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088b24', 'RAW_EXPORT_REQUIREMENTS', 2, 'ModeEquals', 'EncryptedRawVaultRetained', 'Dpia', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088b25', 'RAW_EXPORT_REQUIREMENTS', 2, 'ConsentRequired', NULL, 'ConsentArtifact', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088b26', 'RAW_EXPORT_REQUIREMENTS', 2, 'AnyJurisdictionForeign', NULL, 'CrossBorderAssessment', transaction_timestamp());
                """);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rules ENABLE TRIGGER tr_raw_export_rules_runtime_mutation_reject;");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE tagekyc.raw_export_requirement_rule_sets ENABLE TRIGGER tr_raw_export_rule_sets_runtime_mutation_reject;");
        }
    }

    private static async Task SeedApprovedPolicyAsync(TagEkycDbContext db, Guid policyId, bool includeConsentArtifact = false)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_versions
                ("PolicyId","PolicyVersion","Mode","Purpose","RetentionPurposeCode","ConsentRequirement","ControllerRole","ControllerEntityRef",
                 "ControllerJurisdiction","RecipientJurisdiction","ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion","CreatedAt")
            VALUES
                ('{policyId}',1,'ExternalExportOnlyNoRetain','purpose','NO_RETAIN','{(includeConsentArtifact ? "Required" : "NotRequired")}',
                 'Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,transaction_timestamp());
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

        await transaction.CommitAsync();
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.raw_export_policy_closures
                ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef")
            VALUES
                ('{policyId}',1,'CatalogApproved',transaction_timestamp(),'principal:catalog','decision:catalog');
            """);
    }

    private async Task<(bool Succeeded, string? Message)> ExecuteGrantFunctionInTransactionAsync(
        Guid policyId,
        Guid principalId,
        int expectedRevision,
        string decisionRef,
        string eventType = "Granted")
    {
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{AdminPrincipal:D}';");
            await ExecuteScalarAsync(connection, transaction, $"""
                SELECT tagekyc.raw_export_append_grant(
                    '{principalId}', '{policyId}', 1, {expectedRevision}, '{eventType}', NULL, '{decisionRef}');
                """);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private async Task<(bool Succeeded, string? Message)> ExecuteFulfillmentWithdrawFunctionInTransactionAsync(
        Guid policyId,
        RawExportRequirementType requirementType,
        int expectedRevision,
        int targetRevision)
    {
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await ExecuteScalarAsync(connection, transaction, $"SET LOCAL tagekyc.actor_principal_id = '{RecorderPrincipal:D}';");
            await ExecuteScalarAsync(connection, transaction, $"""
                SELECT tagekyc.raw_export_append_fulfillment(
                    '{policyId}', 1, '{requirementType}', {expectedRevision}, 'Withdrawn',
                    NULL, {targetRevision}, NULL, NULL, NULL, NULL, 'decision:withdraw-race');
                """);
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (PostgresException exception)
        {
            return (false, exception.MessageText);
        }
    }

    private static IReadOnlyList<FunctionExpectation> ExpectedFunctionSecurityManifest() =>
    [
        new("enforce_raw_export_control_plane_insert", false, false, false),
        new("raw_export_activation_gates_hold", true, false, false),
        new("raw_export_append_control_authority", true, true, false),
        new("raw_export_append_fulfillment", true, true, false),
        new("raw_export_append_grant", true, true, false),
        new("raw_export_append_lifecycle", true, true, false),
        new("raw_export_bootstrap_global_authority", true, false, true),
        new("raw_export_current_actor", true, false, false),
        new("raw_export_has_current_authority", true, false, false),
        new("raw_export_policy_exists", true, false, false),
    ];

    private static async Task<IReadOnlyDictionary<string, FunctionSecurity>> QueryFunctionSecurityAsync(
        TagEkycDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.proname,
                   p.prosecdef,
                   owner.rolname,
                   owner.rolcanlogin,
                   COALESCE(array_to_string(p.proconfig, ','), ''),
                   has_function_privilege('public', p.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_raw_export_bootstrapper', p.oid, 'EXECUTE')
            FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles owner ON owner.oid = p.proowner
            WHERE n.nspname = 'tagekyc'
              AND (p.proname LIKE 'raw_export_%' OR p.proname = 'enforce_raw_export_control_plane_insert')
            ORDER BY p.proname;
            """;

        var result = new Dictionary<string, FunctionSecurity>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result[reader.GetString(0)] = new FunctionSecurity(
                reader.GetBoolean(1),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6),
                reader.GetBoolean(7));
        }

        return result;
    }

    private static async Task<object?> ExecuteScalarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteScalarAsync();
    }

    private sealed class FixedApiKeyGenerator(string key) : IManagedApiKeyGenerator
    {
        public ManagedApiKeyMaterial Generate()
        {
            var parsed = ManagedApiKeyParser.Parse(key) ?? throw new InvalidOperationException("Invalid fixed key.");
            return new ManagedApiKeyMaterial(key, parsed.Prefix);
        }
    }

    private sealed record FunctionExpectation(
        string Name,
        bool SecurityDefiner,
        bool RuntimeExecute,
        bool BootstrapperExecute);

    private sealed record FunctionSecurity(
        bool SecurityDefiner,
        string Owner,
        bool OwnerCanLogin,
        string Config,
        bool PublicExecute,
        bool RuntimeExecute,
        bool BootstrapperExecute);
}

#pragma warning restore EF1002
