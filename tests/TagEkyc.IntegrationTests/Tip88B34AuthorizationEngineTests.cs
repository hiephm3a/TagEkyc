using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B34AuthorizationEngineTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private static readonly Guid AdminPrincipal = Guid.Parse("88b34000-0000-5000-8000-000000000001");
    private static readonly Guid RecorderPrincipal = Guid.Parse("88b34000-0000-5000-8000-000000000002");
    private static readonly Guid WithdrawerPrincipal = Guid.Parse("88b34000-0000-5000-8000-000000000006");
    internal static readonly Guid ConsumerPrincipal = Guid.Parse("88b34000-0000-5000-8000-000000000003");
    internal static readonly Guid ClientApplicationId = Guid.Parse("88b34000-0000-5000-8000-000000000004");
    internal static readonly Guid ApiKeyId = Guid.Parse("88b34000-0000-5000-8000-000000000005");
    internal static readonly AuthenticatedRawExportActor Actor =
        new(ConsumerPrincipal, ClientApplicationId, ApiKeyId);

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task L2_session_not_found_persists_exact_denial()
    {
        await using var db = postgres.CreateDbContext();
        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "l2-session-not-found"));

        Assert.Equal(RawExportAuthorizationOutcome.Denied, result.Decision.Outcome);
        Assert.Equal(
            RawExportAuthorizationPrimaryCause.SESSION_NOT_FOUND,
            result.Decision.PrimaryCause);
        Assert.Null(result.Decision.ResolvedVerificationSessionId);
        Assert.Null(result.Decision.SessionSubjectRef);
        Assert.Empty(result.EligibilityCauses);
        Assert.Empty(result.FulfillmentRefs);
        Assert.Empty(result.Classes);
        Assert.Null(result.Permit);
        Assert.Empty(result.PermitClasses);
    }

    [Fact]
    public async Task L2_authorized_produces_permit_and_expiry()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:authorized");
        var fulfillmentExpiry = DateTimeOffset.UtcNow.AddMinutes(4);
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300,
            fulfillmentExpiry);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            validUntilUtc: DateTimeOffset.UtcNow.AddMinutes(3));

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            "l2-authorized"));

        Assert.Equal(RawExportAuthorizationOutcome.Authorized, result.Decision.Outcome);
        Assert.Null(result.Decision.PrimaryCause);
        Assert.Equal(sessionId, result.Decision.ResolvedVerificationSessionId);
        Assert.Equal(ClientApplicationId, result.Decision.SessionOwnerClientApplicationId);
        Assert.Equal("subject:authorized", result.Decision.SessionSubjectRef);
        Assert.NotNull(result.Permit);
        Assert.Equal(result.Decision.ExportDecisionId, result.Permit!.AuthorizationDecisionId);
        Assert.Equal(result.Decision.DecisionExpiresAtUtc, result.Permit.DecisionExpiresAtUtc);
        Assert.Equal(
            [RawExportRawClass.LiveSelfieImage],
            result.PermitClasses.Select(item => item.RawClass));
    }

    public static IEnumerable<object[]> InvalidPreflightCommands()
    {
        var session = Guid.NewGuid();
        var policy = Guid.NewGuid();
        yield return ["empty-principal", Command(session, policy, "l1-a", actor: Actor with { PrincipalId = Guid.Empty })];
        yield return ["empty-client", Command(session, policy, "l1-b", actor: Actor with { ClientApplicationId = Guid.Empty })];
        yield return ["empty-api-key", Command(session, policy, "l1-c", actor: Actor with { ApiKeyId = Guid.Empty })];
        yield return ["empty-session", Command(Guid.Empty, policy, "l1-d")];
        yield return ["empty-policy", Command(session, Guid.Empty, "l1-e")];
        yield return ["policy-version-zero", Command(session, policy, "l1-f") with { PolicyVersion = 0 }];
        yield return ["empty-key", Command(session, policy, string.Empty)];
        yield return ["blank-key", Command(session, policy, " ")];
        yield return ["key-with-whitespace", Command(session, policy, "invalid key")];
        yield return ["key-over-256", Command(session, policy, new string('a', 257))];
        yield return ["explicit-empty", Command(session, policy, "l1-g", [])];
        yield return
        [
            "explicit-unknown",
            Command(session, policy, "l1-h", [(RawExportRawClass)int.MaxValue]),
        ];
        yield return
        [
            "explicit-duplicate",
            Command(
                session,
                policy,
                "l1-i",
                [RawExportRawClass.LiveSelfieImage, RawExportRawClass.LiveSelfieImage]),
        ];
    }

    [Theory]
    [MemberData(nameof(InvalidPreflightCommands))]
    public async Task L1_each_invalid_input_is_typed_and_creates_no_transaction_claim_or_decision(
        string _,
        AuthorizeRawExportCommand command)
    {
        await using var db = postgres.CreateDbContext();
        var claimsBefore = await db.RawExportAuthorizationIdempotency.CountAsync();
        var decisionsBefore = await db.RawExportAuthorizationDecisions.CountAsync();
        var exception = await Assert.ThrowsAsync<RawExportAuthorizationInputException>(
            () => CreateRepository(db).AuthorizeExportAsync(command));

        Assert.Equal("REQUEST_VALIDATION_FAILED", exception.Code);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(claimsBefore, await db.RawExportAuthorizationIdempotency.CountAsync());
        Assert.Equal(decisionsBefore, await db.RawExportAuthorizationDecisions.CountAsync());
        Assert.Equal(
            0,
            await db.RawExportAuthorizationIdempotency.CountAsync(
                item => item.RequestedVerificationSessionId == command.RequestedVerificationSessionId));
        Assert.Equal(
            0,
            await db.RawExportAuthorizationDecisions.CountAsync(
                item => item.RequestedVerificationSessionId == command.RequestedVerificationSessionId));
    }

    [Fact]
    public async Task L2_session_not_owned_does_not_persist_foreign_subject()
    {
        await using var db = postgres.CreateDbContext();
        var sessionId = await SeedCompletedSessionAsync(
            db,
            Guid.NewGuid(),
            "subject:must-not-persist");
        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            Guid.NewGuid(),
            "l2-session-not-owned"));

        Assert.Equal(RawExportAuthorizationPrimaryCause.SESSION_NOT_OWNED, result.Decision.PrimaryCause);
        Assert.Equal(sessionId, result.Decision.ResolvedVerificationSessionId);
        Assert.Null(result.Decision.SessionSubjectRef);
        Assert.NotEqual(ClientApplicationId, result.Decision.SessionOwnerClientApplicationId);
    }

    [Fact]
    public async Task L2_session_not_completed_persists_locked_session_evidence()
    {
        await using var db = postgres.CreateDbContext();
        var sessionId = await SeedCompletedSessionAsync(
            db,
            ClientApplicationId,
            "subject:not-completed",
            VerificationSessionState.InProgress);
        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            Guid.NewGuid(),
            "l2-session-not-completed"));

        Assert.Equal(
            RawExportAuthorizationPrimaryCause.SESSION_NOT_COMPLETED,
            result.Decision.PrimaryCause);
        Assert.Equal("subject:not-completed", result.Decision.SessionSubjectRef);
        Assert.Equal(VerificationSessionState.InProgress, result.Decision.SessionState);
    }

    [Fact]
    public async Task L2_export_eligibility_inactive_passes_b1_primary_and_ordered_causes_verbatim()
    {
        await using var db = postgres.CreateDbContext();
        var sessionId = await SeedCompletedSessionAsync(
            db,
            ClientApplicationId,
            "subject:inactive");
        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            Guid.NewGuid(),
            "l2-eligibility-inactive"));

        Assert.Equal(
            RawExportAuthorizationPrimaryCause.EXPORT_ELIGIBILITY_INACTIVE,
            result.Decision.PrimaryCause);
        Assert.Equal(
            [
                RawExportEligibilityCause.NotCatalogApproved,
                RawExportEligibilityCause.PolicyNotActive,
                RawExportEligibilityCause.GrantMissing,
            ],
            result.EligibilityCauses.Select(item => item.Cause));
        Assert.Equal(
            RawExportEligibilityCause.NotCatalogApproved,
            result.Decision.EligibilityPrimaryCause);
        Assert.Equal([0, 1, 2], result.EligibilityCauses.Select(item => item.Ordinal));
    }

    [Fact]
    public async Task L2_policy_permit_ttl_invalid_is_a_durable_business_denial()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:ttl");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 1_000);

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            "l2-ttl-invalid"));

        Assert.Equal(
            RawExportAuthorizationPrimaryCause.POLICY_PERMIT_TTL_INVALID,
            result.Decision.PrimaryCause);
        Assert.Equal(1_000, result.Decision.PolicyPermitTtlSeconds);
        Assert.Contains(
            result.Classes,
            item => item.ClassKind == RawExportAuthorizationClassKind.PolicyAllowed);
        Assert.Null(result.Decision.DecisionExpiresAtUtc);
    }

    [Fact]
    public async Task L2_requested_raw_classes_not_allowed_persists_policy_and_effective_sets()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:ceiling");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300);

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            "l2-policy-ceiling",
            [RawExportRawClass.LivenessMedia]));

        Assert.Equal(
            RawExportAuthorizationPrimaryCause.REQUESTED_RAW_CLASSES_NOT_ALLOWED,
            result.Decision.PrimaryCause);
        Assert.Equal(
            [RawExportRawClass.LiveSelfieImage],
            Classes(result, RawExportAuthorizationClassKind.PolicyAllowed));
        Assert.Equal(
            [RawExportRawClass.LivenessMedia],
            Classes(result, RawExportAuthorizationClassKind.Requested));
        Assert.Equal(
            [RawExportRawClass.LivenessMedia],
            Classes(result, RawExportAuthorizationClassKind.Effective));
    }

    [Fact]
    public async Task L2_subject_consent_not_effective_persists_the_factual_cause()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:no-consent");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300);

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            "l2-consent-missing"));

        Assert.Equal(
            RawExportAuthorizationPrimaryCause.SUBJECT_CONSENT_NOT_EFFECTIVE,
            result.Decision.PrimaryCause);
        Assert.Equal(RawExportSubjectConsentCause.Missing, result.Decision.SubjectConsentCause);
        Assert.Null(result.Decision.SubjectConsentRef);
        Assert.Empty(Classes(result, RawExportAuthorizationClassKind.Consented));
    }

    [Fact]
    public async Task L2_subject_consent_coverage_insufficient_persists_all_reached_class_sets()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:coverage");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage, RawExportRawClass.LivenessMedia],
            permitTtlSeconds: 300);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage]);

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            "l2-consent-coverage"));

        Assert.Equal(
            RawExportAuthorizationPrimaryCause.SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT,
            result.Decision.PrimaryCause);
        Assert.Null(result.Decision.SubjectConsentCause);
        Assert.Equal(
            [RawExportRawClass.LiveSelfieImage, RawExportRawClass.LivenessMedia],
            Classes(result, RawExportAuthorizationClassKind.PolicyAllowed));
        Assert.Equal(
            [RawExportRawClass.LiveSelfieImage, RawExportRawClass.LivenessMedia],
            Classes(result, RawExportAuthorizationClassKind.Effective));
        Assert.Equal(
            [RawExportRawClass.LiveSelfieImage],
            Classes(result, RawExportAuthorizationClassKind.Consented));
        Assert.Empty(Classes(result, RawExportAuthorizationClassKind.Authorized));
    }

    [Fact]
    public async Task L3_requested_and_resolved_session_identities_follow_foundness()
    {
        await using var db = postgres.CreateDbContext();
        var missingId = Guid.NewGuid();
        var missing = await CreateRepository(db).AuthorizeExportAsync(Command(
            missingId,
            Guid.NewGuid(),
            "l3-missing"));
        var existingId = await SeedCompletedSessionAsync(
            db,
            ClientApplicationId,
            "subject:l3-existing",
            VerificationSessionState.InProgress);
        var existing = await CreateRepository(db).AuthorizeExportAsync(Command(
            existingId,
            Guid.NewGuid(),
            "l3-existing"));

        Assert.Equal(missingId, missing.Decision.RequestedVerificationSessionId);
        Assert.Null(missing.Decision.ResolvedVerificationSessionId);
        Assert.Equal(existingId, existing.Decision.RequestedVerificationSessionId);
        Assert.Equal(existingId, existing.Decision.ResolvedVerificationSessionId);
    }

    [Fact]
    public async Task L4_one_actor_atomically_drives_guc_b1_owner_idempotency_and_evidence()
    {
        await using var db = postgres.CreateDbContext();
        var distinctActor = new AuthenticatedRawExportActor(
            Guid.Parse("88b34000-0000-5000-8000-00000000a001"),
            Guid.Parse("88b34000-0000-5000-8000-00000000a002"),
            Guid.Parse("88b34000-0000-5000-8000-00000000a003"));
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(
            db,
            distinctActor.ClientApplicationId,
            "subject:anti-mixing");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300,
            principalId: distinctActor.PrincipalId);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            clientApplicationId: distinctActor.ClientApplicationId);

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            "l4-anti-mixing",
            actor: distinctActor));
        var identity = await db.RawExportAuthorizationIdempotency.SingleAsync(
            item => item.ExportDecisionId == result.Decision.ExportDecisionId);

        Assert.Equal(distinctActor.PrincipalId, result.Decision.PrincipalId);
        Assert.Equal(distinctActor.ClientApplicationId, result.Decision.ClientApplicationId);
        Assert.Equal(distinctActor.ApiKeyId, result.Decision.ApiKeyId);
        Assert.Equal(distinctActor.ClientApplicationId, result.Decision.SessionOwnerClientApplicationId);
        Assert.Equal(distinctActor.ClientApplicationId, result.Decision.RecipientClientApplicationId);
        Assert.Equal(distinctActor.PrincipalId, identity.PrincipalId);
        Assert.Equal(distinctActor.ClientApplicationId, identity.ClientApplicationId);
    }

    [Fact]
    public async Task L4_session_not_owned_decision_identity_comes_only_from_actor()
    {
        await using var db = postgres.CreateDbContext();
        var distinctActor = new AuthenticatedRawExportActor(
            Guid.Parse("88b34000-0000-5000-8000-00000000a011"),
            Guid.Parse("88b34000-0000-5000-8000-00000000a012"),
            Guid.Parse("88b34000-0000-5000-8000-00000000a013"));
        var foreignOwner = Guid.Parse("88b34000-0000-5000-8000-00000000a014");
        var sessionId = await SeedCompletedSessionAsync(
            db,
            foreignOwner,
            "subject:anti-mixing-denied");

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            Guid.NewGuid(),
            "l4-anti-mixing-denied",
            actor: distinctActor));

        Assert.Equal(RawExportAuthorizationPrimaryCause.SESSION_NOT_OWNED, result.Decision.PrimaryCause);
        Assert.Equal(distinctActor.PrincipalId, result.Decision.PrincipalId);
        Assert.Equal(distinctActor.ClientApplicationId, result.Decision.ClientApplicationId);
        Assert.Equal(distinctActor.ApiKeyId, result.Decision.ApiKeyId);
        Assert.Equal(foreignOwner, result.Decision.SessionOwnerClientApplicationId);
        Assert.NotEqual(distinctActor.ClientApplicationId, result.Decision.SessionOwnerClientApplicationId);
    }

    [Fact]
    public async Task L4_b3_entry_functions_run_through_ephemeral_noinherit_runtime_role()
    {
        var loginName = $"b34_runtime_{Guid.NewGuid():N}";
        const string password = "B34RuntimeOnly_2026";
        await using (var admin = new NpgsqlConnection(postgres.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"""
                CREATE ROLE "{loginName}" LOGIN PASSWORD '{password}'
                    NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
                GRANT tagekyc_runtime TO "{loginName}";
                """, admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
            {
                Username = loginName,
                Password = password,
                Pooling = false,
            }.ConnectionString;
            await using var runtime = new NpgsqlConnection(connectionString);
            await runtime.OpenAsync();
            await using var transaction = await runtime.BeginTransactionAsync();
            await using (var role = new NpgsqlCommand(
                "SET LOCAL ROLE tagekyc_runtime;",
                runtime,
                transaction))
            {
                await role.ExecuteNonQueryAsync();
            }

            await using (var actor = new NpgsqlCommand(
                "SELECT pg_catalog.set_config('tagekyc.actor_principal_id', @actor, true);",
                runtime,
                transaction))
            {
                actor.Parameters.AddWithValue("actor", ConsumerPrincipal.ToString("D"));
                await actor.ExecuteScalarAsync();
            }

            await using (var sessionLock = new NpgsqlCommand(
                """
                SELECT *
                FROM tagekyc.raw_export_lock_verification_session_for_authorization(@sessionId);
                """,
                runtime,
                transaction))
            {
                sessionLock.Parameters.AddWithValue("sessionId", Guid.NewGuid());
                Assert.Null(await sessionLock.ExecuteScalarAsync());
            }

            await using (var claim = new NpgsqlCommand(
                """
                SELECT outcome
                FROM tagekyc.raw_export_claim_or_read_authorization_idempotency(
                    @principalId,@clientApplicationId,@sessionId,@key,@fingerprint,@decisionId);
                """,
                runtime,
                transaction))
            {
                claim.Parameters.AddWithValue("principalId", ConsumerPrincipal);
                claim.Parameters.AddWithValue("clientApplicationId", ClientApplicationId);
                claim.Parameters.AddWithValue("sessionId", Guid.NewGuid());
                claim.Parameters.AddWithValue("key", "l4-runtime-role");
                claim.Parameters.AddWithValue("fingerprint", new byte[32]);
                claim.Parameters.AddWithValue("decisionId", Guid.NewGuid());
                Assert.Equal("NewClaim", await claim.ExecuteScalarAsync());
            }

            await transaction.RollbackAsync();
        }
        finally
        {
            await using var admin = new NpgsqlConnection(postgres.ConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"""
                REVOKE tagekyc_runtime FROM "{loginName}";
                DROP ROLE "{loginName}";
                """, admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public void L10_postgres_registration_resolves_the_authorization_port()
    {
        var services = new ServiceCollection();
        services.AddSingleton(RawExportPermitTtlBoundsState.Valid(60, 900));
        services.AddTagEkycPostgresPersistence(postgres.ConnectionString);
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<EfRawExportAuthorizationRepository>(
            scope.ServiceProvider.GetRequiredService<IRawExportAuthorizationRepository>());
    }

    public static IEnumerable<object[]> ConsentIntegrityMutations()
    {
        yield return
        [
            "session",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { VerificationSessionId = Guid.NewGuid() }),
        ];
        yield return
        [
            "subject",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { SubjectRef = "subject:mismatch" }),
        ];
        yield return
        [
            "policy",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { PolicyId = Guid.NewGuid() }),
        ];
        yield return
        [
            "policy-version",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { PolicyVersion = snapshot.PolicyVersion + 1 }),
        ];
        yield return
        [
            "purpose",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { PurposeCode = "WrongPurpose" }),
        ];
        yield return
        [
            "recipient",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { RecipientClientApplicationId = Guid.NewGuid() }),
        ];
        yield return
        [
            "evaluated-at",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with { EvaluatedAtUtc = snapshot.EvaluatedAtUtc.AddSeconds(1) }),
        ];
        yield return
        [
            "scope-hash",
            (Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot>)(snapshot =>
                snapshot with
                {
                    ConsentRef = snapshot.ConsentRef! with
                    {
                        ConsentScopeHash = snapshot.ConsentRef!.ConsentScopeHash.ToUpperInvariant(),
                    },
                }),
        ];
    }

    [Theory]
    [MemberData(nameof(ConsentIntegrityMutations))]
    public async Task L6_each_b2_snapshot_mismatch_rolls_back_claim_and_decision(
        string _,
        Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot> mutation)
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:l6");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage]);
        var realConsent = new EfRawExportSubjectConsentRepository(db);
        var repository = CreateRepository(
            db,
            subjectConsent: new MutatingSubjectConsentRepository(realConsent, mutation));

        var exception = await Assert.ThrowsAsync<RawExportAuthorizationException>(
            () => repository.AuthorizeExportAsync(Command(
                sessionId,
                policyId,
                $"l6-{Guid.NewGuid():N}")));

        Assert.Equal("RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE", exception.Code);
        Assert.Equal(0, await db.RawExportAuthorizationIdempotency.CountAsync(
            item => item.RequestedVerificationSessionId == sessionId));
        Assert.Equal(0, await db.RawExportAuthorizationDecisions.CountAsync(
            item => item.RequestedVerificationSessionId == sessionId));
    }

    [Fact]
    public async Task L6_bounds_invalid_is_operational_and_rolls_back_the_claim()
    {
        await using var db = postgres.CreateDbContext();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:bounds-invalid");
        var exception = await Assert.ThrowsAsync<RawExportAuthorizationException>(
            () => CreateRepository(
                db,
                bounds: RawExportPermitTtlBoundsState.Invalid)
                .AuthorizeExportAsync(Command(
                    sessionId,
                    Guid.NewGuid(),
                    "l6-bounds-invalid")));

        Assert.Equal(RawExportPermitTtlBoundsState.InvalidCode, exception.Code);
        Assert.Equal(0, await db.RawExportAuthorizationIdempotency.CountAsync(
            item => item.RequestedVerificationSessionId == sessionId));
        Assert.Equal(0, await db.RawExportAuthorizationDecisions.CountAsync(
            item => item.RequestedVerificationSessionId == sessionId));
    }

    [Fact]
    public async Task L6_b1_active_but_exact_policy_missing_is_an_invariant_rollback()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:policy-invariant");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300);
        var repository = CreateRepository(
            db,
            projections: new MissingPolicyProjectionReader(
                new EfRawExportAuthorizationProjectionReader(db)));

        var exception = await Assert.ThrowsAsync<RawExportAuthorizationException>(
            () => repository.AuthorizeExportAsync(Command(
                sessionId,
                policyId,
                "l6-policy-invariant")));

        Assert.Equal("RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE", exception.Code);
        Assert.Equal(0, await db.RawExportAuthorizationIdempotency.CountAsync(
            item => item.RequestedVerificationSessionId == sessionId));
        Assert.Equal(0, await db.RawExportAuthorizationDecisions.CountAsync(
            item => item.RequestedVerificationSessionId == sessionId));
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("consent")]
    [InlineData("fulfillment")]
    [InlineData("absent-optionals")]
    public async Task L7_expiry_is_minimum_of_present_db_time_bounds_without_sentinel(string scenario)
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, $"subject:l7:{scenario}");
        var now = DateTimeOffset.UtcNow;
        var policyTtl = scenario == "policy" || scenario == "absent-optionals" ? 60 : 300;
        DateTimeOffset? consentExpiry = scenario switch
        {
            "consent" => now.AddSeconds(90),
            "policy" => now.AddMinutes(4),
            "fulfillment" => now.AddMinutes(4),
            _ => null,
        };
        DateTimeOffset? fulfillmentExpiry = scenario switch
        {
            "fulfillment" => now.AddSeconds(90),
            "policy" => now.AddMinutes(4),
            "consent" => now.AddMinutes(4),
            _ => null,
        };
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            policyTtl,
            fulfillmentExpiry);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            consentExpiry);

        var result = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            policyId,
            $"l7-{scenario}"));
        var expected = scenario switch
        {
            "consent" => result.Decision.ConsentValidUntilUtc!.Value,
            "fulfillment" => result.FulfillmentRefs.Single().ValidUntilUtc!.Value,
            _ => result.Decision.EligibilityEvaluatedAtUtc!.Value.AddSeconds(policyTtl),
        };

        Assert.Equal(
            result.Decision.EligibilityEvaluatedAtUtc,
            result.Decision.ConsentEvaluatedAtUtc);
        Assert.Equal(expected, result.Decision.DecisionExpiresAtUtc);
        Assert.Equal(expected, result.Permit!.DecisionExpiresAtUtc);
        Assert.NotEqual(DateTimeOffset.MaxValue, result.Decision.DecisionExpiresAtUtc);
    }

    [Fact]
    public async Task L8_authorized_and_denied_replay_return_the_same_immutable_graph()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:l8-authorized");
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage]);
        var repository = CreateRepository(db);
        var authorizedCommand = Command(sessionId, policyId, "l8-authorized");
        var firstAuthorized = await repository.AuthorizeExportAsync(authorizedCommand);
        var replayAuthorized = await repository.AuthorizeExportAsync(authorizedCommand);
        var missingSession = Guid.NewGuid();
        var deniedCommand = Command(missingSession, Guid.NewGuid(), "l8-denied");
        var firstDenied = await repository.AuthorizeExportAsync(deniedCommand);
        var replayDenied = await repository.AuthorizeExportAsync(deniedCommand);

        Assert.Equal(firstAuthorized.Decision.ExportDecisionId, replayAuthorized.Decision.ExportDecisionId);
        Assert.Equal(firstAuthorized.Decision.DecidedAtUtc, replayAuthorized.Decision.DecidedAtUtc);
        Assert.Equal(firstAuthorized.Permit!.PermitId, replayAuthorized.Permit!.PermitId);
        Assert.Equal(firstDenied.Decision.ExportDecisionId, replayDenied.Decision.ExportDecisionId);
        Assert.Equal(firstDenied.Decision.DecidedAtUtc, replayDenied.Decision.DecidedAtUtc);
        Assert.Equal(
            1,
            await db.RawExportAuthorizationDecisions.CountAsync(
                item => item.RequestedVerificationSessionId == sessionId));
        Assert.Equal(
            1,
            await db.RawExportAuthorizationDecisions.CountAsync(
                item => item.RequestedVerificationSessionId == missingSession));
    }

    [Fact]
    public async Task L8_same_key_different_fingerprint_conflicts_without_exposing_or_creating_graph()
    {
        await using var db = postgres.CreateDbContext();
        var sessionId = Guid.NewGuid();
        var first = await CreateRepository(db).AuthorizeExportAsync(Command(
            sessionId,
            Guid.NewGuid(),
            "l8-conflict"));

        var conflict = await Assert.ThrowsAsync<RawExportAuthorizationException>(
            () => CreateRepository(db).AuthorizeExportAsync(Command(
                sessionId,
                Guid.NewGuid(),
                "l8-conflict")));

        Assert.Equal("RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_CONFLICT", conflict.Code);
        Assert.Equal(
            [first.Decision.ExportDecisionId],
            await db.RawExportAuthorizationDecisions
                .Where(item => item.RequestedVerificationSessionId == sessionId)
                .Select(item => item.ExportDecisionId)
                .ToListAsync());
    }

    [Fact]
    public async Task L8_re_evaluation_after_denial_requires_a_new_idempotency_key()
    {
        await using var db = postgres.CreateDbContext();
        var policyId = Guid.NewGuid();
        var sessionId = await SeedCompletedSessionAsync(db, ClientApplicationId, "subject:l8-new-key");
        var repository = CreateRepository(db);
        var deniedCommand = Command(sessionId, policyId, "l8-original-key");
        var denied = await repository.AuthorizeExportAsync(deniedCommand);
        await SeedActivePolicyAsync(
            db,
            policyId,
            [RawExportRawClass.LiveSelfieImage],
            permitTtlSeconds: 300);
        await SeedEffectiveConsentAsync(
            db,
            sessionId,
            policyId,
            [RawExportRawClass.LiveSelfieImage]);

        var replay = await repository.AuthorizeExportAsync(deniedCommand);
        var reevaluated = await repository.AuthorizeExportAsync(
            deniedCommand with { IdempotencyKey = "l8-new-key" });

        Assert.Equal(RawExportAuthorizationOutcome.Denied, denied.Decision.Outcome);
        Assert.Equal(denied.Decision.ExportDecisionId, replay.Decision.ExportDecisionId);
        Assert.Equal(RawExportAuthorizationOutcome.Authorized, reevaluated.Decision.Outcome);
        Assert.NotEqual(denied.Decision.ExportDecisionId, reevaluated.Decision.ExportDecisionId);
    }

    [Theory]
    [InlineData("grant-revoke")]
    [InlineData("lifecycle-suspend")]
    [InlineData("fulfillment-withdraw")]
    public async Task L5_b1_mutation_waits_for_authorization_commit_without_stale_authorized(
        string mutationKind)
    {
        Guid policyId;
        Guid sessionId;
        await using (var setup = postgres.CreateDbContext())
        {
            policyId = Guid.NewGuid();
            sessionId = await SeedCompletedSessionAsync(
                setup,
                ClientApplicationId,
                $"subject:l5:{mutationKind}");
            await SeedActivePolicyAsync(
                setup,
                policyId,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 300);
            await SeedEffectiveConsentAsync(
                setup,
                sessionId,
                policyId,
                [RawExportRawClass.LiveSelfieImage]);
        }

        await using var engineDb = postgres.CreateDbContext();
        var pause = new PausingControlPlaneRepository(
            new EfRawExportControlPlaneRepository(engineDb));
        var authorize = CreateRepository(engineDb, controlPlane: pause)
            .AuthorizeExportAsync(Command(
                sessionId,
                policyId,
                $"l5-{mutationKind}"));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var controlPlane = new EfRawExportControlPlaneRepository(mutationDb);
        var mutation = mutationKind switch
        {
            "grant-revoke" => controlPlane.RevokeExportPolicyGrantAsync(new(
                AdminPrincipal,
                ConsumerPrincipal,
                policyId,
                1,
                ExpectedRevision: 1,
                ClientApplicationId: null,
                "decision:l5-revoke")),
            "lifecycle-suspend" => controlPlane.SuspendPolicyAsync(new(
                AdminPrincipal,
                policyId,
                1,
                ExpectedRevision: 1,
                "decision:l5-suspend")),
            "fulfillment-withdraw" => controlPlane.WithdrawFulfillmentAsync(new(
                RecorderPrincipal,
                policyId,
                1,
                RawExportRequirementType.LegalApproval,
                ExpectedRevision: 1,
                TargetRevision: 1,
                "decision:l5-withdraw")),
            _ => throw new InvalidOperationException(mutationKind),
        };
        await Task.Delay(250);
        Assert.False(mutation.IsCompleted, $"{mutationKind} did not block on the held B1 lock.");

        pause.Release();
        var result = await authorize.WaitAsync(TimeSpan.FromSeconds(10));
        await mutation.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(RawExportAuthorizationOutcome.Authorized, result.Decision.Outcome);
    }

    [Fact]
    public async Task L5_consent_withdraw_waits_for_authorization_commit_without_stale_authorized()
    {
        Guid policyId;
        Guid sessionId;
        await using (var setup = postgres.CreateDbContext())
        {
            policyId = Guid.NewGuid();
            sessionId = await SeedCompletedSessionAsync(
                setup,
                ClientApplicationId,
                "subject:l5-consent");
            await SeedActivePolicyAsync(
                setup,
                policyId,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 300);
            await SeedEffectiveConsentAsync(
                setup,
                sessionId,
                policyId,
                [RawExportRawClass.LiveSelfieImage]);
            await EnsureConsentAuthorityAsync(
                setup,
                WithdrawerPrincipal,
                RawExportSubjectConsentAuthorityType.SubjectConsentWithdrawer,
                ClientApplicationId);
        }

        await using var engineDb = postgres.CreateDbContext();
        var pause = new PausingSubjectConsentRepository(
            new EfRawExportSubjectConsentRepository(engineDb));
        var authorize = CreateRepository(engineDb, subjectConsent: pause)
            .AuthorizeExportAsync(Command(
                sessionId,
                policyId,
                "l5-consent-withdraw"));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var withdraw = new EfRawExportSubjectConsentRepository(mutationDb)
            .RecordSubjectConsentWithdrawnAsync(new(
                WithdrawerPrincipal,
                sessionId,
                policyId,
                1,
                ExpectedRevision: 1,
                TargetRevision: 1,
                "decision:l5-consent-withdraw"));
        await Task.Delay(250);
        Assert.False(withdraw.IsCompleted, "Consent withdrawal did not block on the held B2 lock.");

        pause.Release();
        var result = await authorize.WaitAsync(TimeSpan.FromSeconds(10));
        await withdraw.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(RawExportAuthorizationOutcome.Authorized, result.Decision.Outcome);
    }

    [Fact]
    public async Task L5_session_transition_waits_for_authorization_commit_without_stale_authorized()
    {
        Guid policyId;
        Guid sessionId;
        await using (var setup = postgres.CreateDbContext())
        {
            policyId = Guid.NewGuid();
            sessionId = await SeedCompletedSessionAsync(
                setup,
                ClientApplicationId,
                "subject:l5-session");
            await SeedActivePolicyAsync(
                setup,
                policyId,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 300);
            await SeedEffectiveConsentAsync(
                setup,
                sessionId,
                policyId,
                [RawExportRawClass.LiveSelfieImage]);
        }

        await using var engineDb = postgres.CreateDbContext();
        var pause = new PausingControlPlaneRepository(
            new EfRawExportControlPlaneRepository(engineDb));
        var authorize = CreateRepository(engineDb, controlPlane: pause)
            .AuthorizeExportAsync(Command(
                sessionId,
                policyId,
                "l5-session-transition"));
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var mutationDb = postgres.CreateDbContext();
        var transition = new EfVerificationSessionRepository(mutationDb)
            .SetStateAsync(sessionId, VerificationSessionState.Expired);
        await Task.Delay(250);
        Assert.False(transition.IsCompleted, "Session transition did not block on the B3 row lock.");

        pause.Release();
        var result = await authorize.WaitAsync(TimeSpan.FromSeconds(10));
        await transition.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(RawExportAuthorizationOutcome.Authorized, result.Decision.Outcome);
    }

    [Fact]
    public async Task L5_concurrent_duplicate_blocks_then_replays_exactly_one_decision()
    {
        Guid policyId;
        Guid sessionId;
        await using (var setup = postgres.CreateDbContext())
        {
            policyId = Guid.NewGuid();
            sessionId = await SeedCompletedSessionAsync(
                setup,
                ClientApplicationId,
                "subject:l5-duplicate");
            await SeedActivePolicyAsync(
                setup,
                policyId,
                [RawExportRawClass.LiveSelfieImage],
                permitTtlSeconds: 300);
            await SeedEffectiveConsentAsync(
                setup,
                sessionId,
                policyId,
                [RawExportRawClass.LiveSelfieImage]);
        }

        var command = Command(sessionId, policyId, "l5-duplicate");
        await using var firstDb = postgres.CreateDbContext();
        var pause = new PausingControlPlaneRepository(
            new EfRawExportControlPlaneRepository(firstDb));
        var first = CreateRepository(firstDb, controlPlane: pause).AuthorizeExportAsync(command);
        await pause.Reached.WaitAsync(TimeSpan.FromSeconds(10));

        await using var secondDb = postgres.CreateDbContext();
        var second = CreateRepository(secondDb).AuthorizeExportAsync(command);
        await Task.Delay(250);
        Assert.False(second.IsCompleted, "Duplicate authorization did not block on the idempotency winner.");

        pause.Release();
        var winner = await first.WaitAsync(TimeSpan.FromSeconds(10));
        var replay = await second.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(winner.Decision.ExportDecisionId, replay.Decision.ExportDecisionId);
        await using var verify = postgres.CreateDbContext();
        Assert.Equal(
            1,
            await verify.RawExportAuthorizationDecisions.CountAsync(
                item => item.RequestedVerificationSessionId == sessionId));
    }

    internal static AuthorizeRawExportCommand Command(
        Guid sessionId,
        Guid policyId,
        string idempotencyKey,
        IReadOnlyList<RawExportRawClass>? requestedClasses = null,
        AuthenticatedRawExportActor? actor = null) =>
        new(
            actor ?? Actor,
            sessionId,
            policyId,
            PolicyVersion: 1,
            requestedClasses,
            idempotencyKey);

    internal static EfRawExportAuthorizationRepository CreateRepository(
        TagEkycDbContext db,
        IRawExportControlPlaneRepository? controlPlane = null,
        IRawExportSubjectConsentRepository? subjectConsent = null,
        IRawExportAuthorizationProjectionReader? projections = null,
        RawExportPermitTtlBoundsState? bounds = null)
    {
        projections ??= new EfRawExportAuthorizationProjectionReader(db);
        return new(
            db,
            projections,
            controlPlane ?? new EfRawExportControlPlaneRepository(db, projections),
            subjectConsent ?? new EfRawExportSubjectConsentRepository(db),
            bounds ?? RawExportPermitTtlBoundsState.Valid(60, 900));
    }

    internal static async Task<Guid> SeedCompletedSessionAsync(
        TagEkycDbContext db,
        Guid clientApplicationId,
        string subjectRef,
        VerificationSessionState state = VerificationSessionState.Completed)
    {
        var now = DateTimeOffset.UtcNow;
        var session = VerificationSession.Create(
            clientApplicationId,
            subjectRef,
            VerificationProfile.StandardEkycProfile,
            "raw-export",
            [RequiredCheckType.DocumentNfc],
            now.AddHours(1),
            now);
        var repository = new EfVerificationSessionRepository(db);
        await repository.AddAsync(session);
        await repository.SetStateAsync(session.Id, state);
        return session.Id;
    }

    internal static async Task SeedActivePolicyAsync(
        TagEkycDbContext db,
        Guid policyId,
        IReadOnlyList<RawExportRawClass> allowedClasses,
        int? permitTtlSeconds,
        DateTimeOffset? fulfillmentExpiry = null,
        Guid? principalId = null)
    {
        await SeedPolicyAsync(db, policyId, allowedClasses, permitTtlSeconds, catalogApproved: true);
        await BootstrapB1RootsAsync(db);
        var repository = new EfRawExportControlPlaneRepository(db);
        await repository.GrantExportPolicyAsync(new(
            AdminPrincipal,
            principalId ?? ConsumerPrincipal,
            policyId,
            1,
            ExpectedRevision: 0,
            ClientApplicationId: null,
            "decision:b34-grant"));
        await repository.GrantControlAuthorityAsync(new(
            AdminPrincipal,
            RecorderPrincipal,
            RawExportAuthorityType.FulfillmentRecorder,
            RawExportAuthorityScopeType.Policy,
            policyId,
            RawExportRequirementType.LegalApproval,
            ExpectedRevision: 0,
            "decision:b34-recorder"));
        await repository.AcceptFulfillmentAsync(new(
            RecorderPrincipal,
            policyId,
            1,
            RawExportRequirementType.LegalApproval,
            ExpectedRevision: 0,
            SupersedesRevision: null,
            "artifact:b34-legal",
            "v1",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            fulfillmentExpiry,
            "decision:b34-fulfillment"));
        await repository.ActivatePolicyAsync(new(
            AdminPrincipal,
            policyId,
            1,
            ExpectedRevision: 0,
            "decision:b34-activate"));
    }

    private static async Task SeedPolicyAsync(
        TagEkycDbContext db,
        Guid policyId,
        IReadOnlyList<RawExportRawClass> allowedClasses,
        int? permitTtlSeconds,
        bool catalogApproved)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tagekyc.raw_export_policy_versions
                ("PolicyId","PolicyVersion","Mode","Purpose","RetentionPurposeCode","ConsentRequirement",
                 "ControllerRole","ControllerEntityRef","ControllerJurisdiction","RecipientJurisdiction",
                 "ProcessingInfrastructureJurisdiction","RequirementRuleSetId","RequirementRuleSetVersion",
                 "PermitTtlSeconds","CreatedAt")
            VALUES
                ({policyId},1,'ExternalExportOnlyNoRetain','purpose','NO_RETAIN','Required',
                 'Processor','controller','VN','VN','VN','RAW_EXPORT_REQUIREMENTS',1,
                 {permitTtlSeconds},transaction_timestamp());
            """);
        foreach (var rawClass in allowedClasses)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_allowed_classes
                    ("PolicyId","PolicyVersion","RawClass","CreatedAt")
                VALUES ({policyId},1,{rawClass.ToString()},transaction_timestamp());
                """);
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO tagekyc.raw_export_policy_requirements
                ("PolicyId","PolicyVersion","RequirementType","CreatedAt")
            VALUES
                ({policyId},1,'LegalApproval',transaction_timestamp()),
                ({policyId},1,'ConsentArtifact',transaction_timestamp());
            """);
        await transaction.CommitAsync();
        if (catalogApproved)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.raw_export_policy_closures
                    ("PolicyId","PolicyVersion","ClosureType","ClosedAtUtc","ClosedByPrincipalId","DecisionRef")
                VALUES ({policyId},1,'CatalogApproved',transaction_timestamp(),
                        'principal:b34-catalog','decision:b34-catalog');
                """);
        }
    }

    private static async Task BootstrapB1RootsAsync(TagEkycDbContext db)
    {
        foreach (var authority in new[] { "GrantAdmin", "RecorderAuthorityAdmin", "ActivationAuthority" })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT tagekyc.raw_export_bootstrap_global_authority(
                    {AdminPrincipal},{authority},{"decision:b34-bootstrap:" + authority});
                """);
        }
    }

    internal static async Task SeedEffectiveConsentAsync(
        TagEkycDbContext db,
        Guid sessionId,
        Guid policyId,
        IReadOnlyList<RawExportRawClass> consentedClasses,
        DateTimeOffset? validUntilUtc = null,
        Guid? clientApplicationId = null)
    {
        var repository = new EfRawExportSubjectConsentRepository(db);
        var targetClient = clientApplicationId ?? ClientApplicationId;
        if (!await db.RawExportSubjectConsentAuthorities.AnyAsync(item =>
                item.AuthorityPrincipalId == RecorderPrincipal &&
                item.ClientApplicationId == targetClient &&
                item.AuthorityType ==
                RawExportSubjectConsentAuthorityType.SubjectConsentRecorder.ToString()))
        {
            await repository.GrantConsentAuthorityAsync(new(
                AdminPrincipal,
                RecorderPrincipal,
                targetClient,
                RawExportSubjectConsentAuthorityType.SubjectConsentRecorder,
                ExpectedRevision: 0,
                "decision:b34-consent-authority"));
        }

        await repository.RecordSubjectConsentGrantedAsync(new(
            RecorderPrincipal,
            sessionId,
            policyId,
            1,
            consentedClasses.ToHashSet(),
            "consent-text:v1",
            "sha256:b34-consent",
            "external:b34-consent",
            "decision:b34-consent",
            validUntilUtc));
    }

    private static async Task EnsureConsentAuthorityAsync(
        TagEkycDbContext db,
        Guid authorityPrincipalId,
        RawExportSubjectConsentAuthorityType authorityType,
        Guid clientApplicationId)
    {
        if (await db.RawExportSubjectConsentAuthorities.AnyAsync(item =>
                item.AuthorityPrincipalId == authorityPrincipalId &&
                item.ClientApplicationId == clientApplicationId &&
                item.AuthorityType == authorityType.ToString()))
        {
            return;
        }

        await new EfRawExportSubjectConsentRepository(db).GrantConsentAuthorityAsync(new(
            AdminPrincipal,
            authorityPrincipalId,
            clientApplicationId,
            authorityType,
            ExpectedRevision: 0,
            $"decision:b34-authority:{authorityType}"));
    }

    private static IReadOnlyList<RawExportRawClass> Classes(
        RawExportAuthorizationResult result,
        RawExportAuthorizationClassKind kind) =>
        result.Classes
            .Where(item => item.ClassKind == kind)
            .OrderBy(item => item.Ordinal)
            .Select(item => item.RawClass)
            .ToArray();

    private sealed class MutatingSubjectConsentRepository(
        IRawExportSubjectConsentRepository inner,
        Func<RawExportSubjectConsentSnapshot, RawExportSubjectConsentSnapshot> mutation)
        : IRawExportSubjectConsentRepository
    {
        public Task<int> GrantConsentAuthorityAsync(
            RawExportSubjectConsentAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantConsentAuthorityAsync(command, cancellationToken);

        public Task<int> RevokeConsentAuthorityAsync(
            RawExportSubjectConsentAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeConsentAuthorityAsync(command, cancellationToken);

        public Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentGrantedAsync(
            RawExportSubjectConsentGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RecordSubjectConsentGrantedAsync(command, cancellationToken);

        public Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentWithdrawnAsync(
            RawExportSubjectConsentWithdrawCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RecordSubjectConsentWithdrawnAsync(command, cancellationToken);

        public async Task<RawExportSubjectConsentSnapshot>
            ResolveSubjectExportConsentForAuthorizationAsync(
                Guid verificationSessionId,
                Guid policyId,
                int policyVersion,
                CancellationToken cancellationToken = default) =>
            mutation(await inner.ResolveSubjectExportConsentForAuthorizationAsync(
                verificationSessionId,
                policyId,
                policyVersion,
                cancellationToken));
    }

    private sealed class PausingControlPlaneRepository(
        IRawExportControlPlaneRepository inner)
        : IRawExportControlPlaneRepository
    {
        private readonly TaskCompletionSource reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Reached => reached.Task;

        public void Release() => released.TrySetResult();

        public Task<int> GrantExportPolicyAsync(
            RawExportGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantExportPolicyAsync(command, cancellationToken);

        public Task<int> RevokeExportPolicyGrantAsync(
            RawExportGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeExportPolicyGrantAsync(command, cancellationToken);

        public Task<int> GrantControlAuthorityAsync(
            RawExportAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantControlAuthorityAsync(command, cancellationToken);

        public Task<int> RevokeControlAuthorityAsync(
            RawExportAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeControlAuthorityAsync(command, cancellationToken);

        public Task<int> AcceptFulfillmentAsync(
            RawExportFulfillmentAcceptCommand command,
            CancellationToken cancellationToken = default) =>
            inner.AcceptFulfillmentAsync(command, cancellationToken);

        public Task<int> WithdrawFulfillmentAsync(
            RawExportFulfillmentWithdrawCommand command,
            CancellationToken cancellationToken = default) =>
            inner.WithdrawFulfillmentAsync(command, cancellationToken);

        public Task<int> ActivatePolicyAsync(
            RawExportLifecycleCommand command,
            CancellationToken cancellationToken = default) =>
            inner.ActivatePolicyAsync(command, cancellationToken);

        public Task<int> SuspendPolicyAsync(
            RawExportLifecycleCommand command,
            CancellationToken cancellationToken = default) =>
            inner.SuspendPolicyAsync(command, cancellationToken);

        public Task<int> RevokePolicyAsync(
            RawExportLifecycleCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokePolicyAsync(command, cancellationToken);

        public async Task<RawExportEligibilitySnapshot>
            ResolveExportEligibilityForAuthorizationAsync(
                Guid principalId,
                Guid policyId,
                int policyVersion,
                CancellationToken cancellationToken = default)
        {
            var snapshot = await inner.ResolveExportEligibilityForAuthorizationAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);
            reached.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return snapshot;
        }
    }

    private sealed class PausingSubjectConsentRepository(
        IRawExportSubjectConsentRepository inner)
        : IRawExportSubjectConsentRepository
    {
        private readonly TaskCompletionSource reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Reached => reached.Task;

        public void Release() => released.TrySetResult();

        public Task<int> GrantConsentAuthorityAsync(
            RawExportSubjectConsentAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.GrantConsentAuthorityAsync(command, cancellationToken);

        public Task<int> RevokeConsentAuthorityAsync(
            RawExportSubjectConsentAuthorityCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RevokeConsentAuthorityAsync(command, cancellationToken);

        public Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentGrantedAsync(
            RawExportSubjectConsentGrantCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RecordSubjectConsentGrantedAsync(command, cancellationToken);

        public Task<RawExportSubjectConsentSnapshot> RecordSubjectConsentWithdrawnAsync(
            RawExportSubjectConsentWithdrawCommand command,
            CancellationToken cancellationToken = default) =>
            inner.RecordSubjectConsentWithdrawnAsync(command, cancellationToken);

        public async Task<RawExportSubjectConsentSnapshot>
            ResolveSubjectExportConsentForAuthorizationAsync(
                Guid verificationSessionId,
                Guid policyId,
                int policyVersion,
                CancellationToken cancellationToken = default)
        {
            var snapshot = await inner.ResolveSubjectExportConsentForAuthorizationAsync(
                verificationSessionId,
                policyId,
                policyVersion,
                cancellationToken);
            reached.TrySetResult();
            await released.Task.WaitAsync(cancellationToken);
            return snapshot;
        }
    }

    private sealed class MissingPolicyProjectionReader(IRawExportAuthorizationProjectionReader inner)
        : IRawExportAuthorizationProjectionReader
    {
        public Task<RawExportAuthorizationEligibilityProjection> ReadEligibilityInputsAsync(
            Guid principalId,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken = default) =>
            inner.ReadEligibilityInputsAsync(principalId, policyId, policyVersion, cancellationToken);

        public async Task<RawExportAuthorizationPolicyProjection> ReadPolicyInputsAsync(
            Guid principalId,
            Guid policyId,
            int policyVersion,
            CancellationToken cancellationToken = default)
        {
            var projection = await inner.ReadPolicyInputsAsync(
                principalId,
                policyId,
                policyVersion,
                cancellationToken);
            return projection with
            {
                PolicyExists = false,
                PermitTtlSeconds = null,
                ClosureType = null,
                AllowedClasses = new HashSet<RawExportRawClass>(),
            };
        }
    }

}
