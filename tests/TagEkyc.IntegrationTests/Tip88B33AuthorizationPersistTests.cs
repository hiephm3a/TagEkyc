using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Api;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B33AuthorizationPersistTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string Migration = "20260723052003_Tip88B33RawExportAuthorizationPersistFunction";
    private const string PreviousMigration = "20260722163505_Tip88B32RawExportAuthorizationIdempotencyFunctions";
    private const string PersistFunction = "raw_export_persist_authorization_decision";
    private static readonly string[] B3Functions =
    [
        "raw_export_lock_verification_session_for_authorization",
        "raw_export_claim_or_read_authorization_idempotency",
        PersistFunction,
    ];
    private static readonly string[] B3InternalFunctions =
    [
        "enforce_raw_export_authorization_insert",
        "enforce_raw_export_decision_child_same_transaction",
        "enforce_raw_export_permit_child_same_transaction",
        "enforce_raw_export_permit_has_classes",
    ];

    private readonly Tip88B33Harness harness = new(postgres);

    public Task InitializeAsync() => harness.MigrateAsync(Migration);
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task K1_intended_b33_identifiers_are_within_63_bytes_and_round_trip_exactly()
    {
        string[] parameters = ["payload"];
        string[] triggers = [];
        string[] constraints = [];
        var intended = new[] { PersistFunction }.Concat(parameters).Concat(triggers).Concat(constraints).ToArray();
        Assert.All(intended, name => Assert.True(Encoding.UTF8.GetByteCount(name) <= 63, name));
        Assert.Equal(22, 63 - Encoding.UTF8.GetByteCount(PersistFunction));

        await using var connection = await harness.OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT p.proname, p.proargnames
            FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname=@name;
            """, connection);
        command.Parameters.AddWithValue("name", PersistFunction);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(PersistFunction, reader.GetString(0));
        Assert.Equal(["payload"], reader.GetFieldValue<string[]>(1));
        Assert.False(await reader.ReadAsync());
    }

    [Fact]
    public async Task K2_exact_three_function_manifest_and_readiness_503_on_acl_drift()
    {
        await using var connection = await harness.OpenAdminAsync();
        var rows = new List<(string Name, string Args, string Result, bool Definer, string Owner, bool Config, bool Public, bool Runtime)>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT p.proname, pg_catalog.oidvectortypes(p.proargtypes),
                   pg_catalog.pg_get_function_result(p.oid), p.prosecdef, owner.rolname,
                   p.proconfig=ARRAY['search_path=pg_catalog']::text[],
                   pg_catalog.has_function_privilege('public',p.oid,'EXECUTE'),
                   pg_catalog.has_function_privilege('tagekyc_runtime',p.oid,'EXECUTE')
            FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            JOIN pg_roles owner ON owner.oid=p.proowner
            WHERE n.nspname='tagekyc' AND p.proname=ANY(@functions)
            ORDER BY p.proname;
            """, connection))
        {
            command.Parameters.AddWithValue("functions", B3Functions.Concat(B3InternalFunctions).ToArray());
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3),
                    reader.GetString(4), reader.GetBoolean(5), reader.GetBoolean(6), reader.GetBoolean(7)));
            }
        }

        Assert.Equal(7, rows.Count);
        Assert.All(rows.Where(row => B3Functions.Contains(row.Name, StringComparer.Ordinal)), row =>
        {
            Assert.True(row.Definer);
            Assert.Equal("tagekyc_raw_export_deployer", row.Owner);
            Assert.True(row.Config);
            Assert.False(row.Public);
            Assert.True(row.Runtime);
        });
        Assert.All(rows.Where(row => B3InternalFunctions.Contains(row.Name, StringComparer.Ordinal)), row =>
        {
            Assert.False(row.Definer);
            Assert.Equal("tagekyc_raw_export_deployer", row.Owner);
            Assert.True(row.Config);
            Assert.False(row.Public);
            Assert.False(row.Runtime);
            Assert.Equal("", row.Args);
            Assert.Equal("trigger", row.Result);
        });
        Assert.Equal(B3Functions.Order(StringComparer.Ordinal),
            rows.Where(row => row.Runtime).Select(row => row.Name).Order(StringComparer.Ordinal));
        Assert.Equal("uuid", rows.Single(row => row.Name == PersistFunction).Result);
        Assert.Equal("jsonb", rows.Single(row => row.Name == PersistFunction).Args);

        await using (var db = postgres.CreateDbContext())
        {
            await new RawExportAuthorizationReadinessValidator(db).ValidateAsync(CancellationToken.None);
        }
        await Tip88B33Harness.ExecuteAsync(connection, null,
            "REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_persist_authorization_decision(jsonb) FROM tagekyc_runtime;");
        try
        {
            await using (var db = postgres.CreateDbContext())
            {
                var exception = await Assert.ThrowsAsync<RawExportAuthorizationReadinessException>(
                    () => new RawExportAuthorizationReadinessValidator(db).ValidateAsync(CancellationToken.None));
                Assert.Equal(RawExportAuthorizationReadinessValidator.FunctionAclInvalid, exception.Code);
            }

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
            builder.WebHost.UseTestServer();
            builder.Services.AddDbContext<TagEkycDbContext>(options => options.UseNpgsql(postgres.ConnectionString));
            builder.Services.AddScoped<RawExportAuthorizationReadinessValidator>();
            builder.Services.AddScoped<IReadinessCheck, RawExportAuthorizationReadinessCheck>();
            await using var app = builder.Build();
            app.MapReadinessEndpoint();
            await app.StartAsync();
            var response = await app.GetTestClient().GetAsync("/readiness");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains(RawExportAuthorizationReadinessValidator.FunctionAclInvalid,
                await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        finally
        {
            await Tip88B33Harness.ExecuteAsync(connection, null,
                "GRANT EXECUTE ON FUNCTION tagekyc.raw_export_persist_authorization_decision(jsonb) TO tagekyc_runtime;");
        }
    }

    [Fact]
    public async Task K3_unknown_or_missing_key_and_wrong_kind_raise_only_payload_invalid()
    {
        var ids = await harness.SeedSessionAsync();
        var unknown = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        unknown["Surprise"] = 1;
        await harness.AssertPersistErrorAsync(unknown, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);

        var missing = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        missing.Remove("Classes");
        await harness.AssertPersistErrorAsync(missing, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);

        var wrongKind = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        wrongKind["EligibilityCauses"] = new JsonObject();
        await harness.AssertPersistErrorAsync(wrongKind, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);
    }

    [Fact]
    public async Task K3_array_over_64_and_fractional_integer_raise_only_payload_invalid()
    {
        var ids = await harness.SeedSessionAsync();
        var oversized = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        var classes = (JsonArray)oversized["Classes"]!;
        for (var index = 0; index < 65; index++)
        {
            classes.Add(new JsonObject { ["ClassKind"] = "Requested", ["RawClass"] = "ChipDg1", ["Ordinal"] = index });
        }
        await harness.AssertPersistErrorAsync(oversized, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);

        var fractional = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        Tip88B33Payloads.Decision(fractional)["PolicyVersion"] = 1.5;
        await harness.AssertPersistErrorAsync(fractional, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);
    }

    [Fact]
    public async Task K3_malformed_hex_uuid_and_timestamp_are_p0001_while_raw_casts_are_22xxx()
    {
        await using (var admin = await harness.OpenAdminAsync())
        {
            var rawUuid = await Assert.ThrowsAsync<PostgresException>(
                () => Tip88B33Harness.ScalarAsync(admin, null, "SELECT 'not-a-uuid'::uuid;"));
            Assert.StartsWith("22", rawUuid.SqlState, StringComparison.Ordinal);
            var rawTimestamp = await Assert.ThrowsAsync<PostgresException>(
                () => Tip88B33Harness.ScalarAsync(admin, null, "SELECT '2026-02-30T00:00:00Z'::timestamptz;"));
            Assert.StartsWith("22", rawTimestamp.SqlState, StringComparison.Ordinal);
            var rawHex = await Assert.ThrowsAsync<PostgresException>(
                () => Tip88B33Harness.ScalarAsync(admin, null, "SELECT decode('zz','hex');"));
            Assert.StartsWith("22", rawHex.SqlState, StringComparison.Ordinal);
        }

        var ids = await harness.SeedSessionAsync();
        var badHex = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        Tip88B33Payloads.Decision(badHex)["FingerprintHash"] = new string('a', 62) + "zz";
        await harness.AssertPersistErrorAsync(badHex, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);

        var badUuid = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        badUuid["ExportDecisionId"] = "not-a-uuid";
        await harness.AssertPersistErrorAsync(badUuid, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);

        var badTimestamp = Tip88B33Payloads.Create(ids, "EXPORT_ELIGIBILITY_INACTIVE");
        Tip88B33Payloads.Decision(badTimestamp)["EligibilityEvaluatedAtUtc"] = "2026-02-30T00:00:00Z";
        await harness.AssertPersistErrorAsync(badTimestamp, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);
    }

    [Fact]
    public async Task K4_z_and_plus_zero_timestamps_accept_but_seven_digits_and_non_utc_reject()
    {
        var ids = await harness.SeedSessionAsync();
        var zulu = Tip88B33Payloads.Create(ids, "EXPORT_ELIGIBILITY_INACTIVE");
        Tip88B33Payloads.Decision(zulu)["EligibilityEvaluatedAtUtc"] = "2026-07-23T00:00:00.123456Z";
        await harness.AssertPersistAcceptedAsync(zulu);

        var plusZero = Tip88B33Payloads.Create(ids, "EXPORT_ELIGIBILITY_INACTIVE");
        Tip88B33Payloads.Decision(plusZero)["EligibilityEvaluatedAtUtc"] = "2026-07-23T00:00:00.123456+00:00";
        await harness.AssertPersistAcceptedAsync(plusZero);

        var sevenDigits = Tip88B33Payloads.Create(ids, "EXPORT_ELIGIBILITY_INACTIVE");
        Tip88B33Payloads.Decision(sevenDigits)["EligibilityEvaluatedAtUtc"] = "2026-07-23T00:00:00.1234567Z";
        await harness.AssertPersistErrorAsync(sevenDigits, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);

        var nonUtc = Tip88B33Payloads.Create(ids, "EXPORT_ELIGIBILITY_INACTIVE");
        Tip88B33Payloads.Decision(nonUtc)["EligibilityEvaluatedAtUtc"] = "2026-07-23T07:00:00+07:00";
        await harness.AssertPersistErrorAsync(nonUtc, "RAW_EXPORT_AUTHORIZATION_PAYLOAD_INVALID", claim: false);
    }

    [Fact]
    public async Task K5_wrong_tuple_is_missing_and_fingerprint_id_or_cross_copy_divergence_is_mismatch()
    {
        var ids = await harness.SeedSessionAsync();
        var missing = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        await harness.AssertPersistErrorAsync(missing, "RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_MISSING", claim: false,
            setActor: true);

        var fingerprint = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        var claimedFingerprint = (JsonObject)fingerprint.DeepClone();
        Tip88B33Payloads.Decision(fingerprint)["FingerprintHash"] = new string('b', 64);
        await harness.AssertPersistErrorAsync(fingerprint, "RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_MISMATCH",
            claimPayload: claimedFingerprint);

        var decisionId = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        var claimedId = (JsonObject)decisionId.DeepClone();
        decisionId["ExportDecisionId"] = Guid.NewGuid().ToString();
        await harness.AssertPersistErrorAsync(decisionId, "RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_MISMATCH",
            claimPayload: claimedId);

        var crossCopy = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        var claimedIdentity = (JsonObject)crossCopy.DeepClone();
        Tip88B33Payloads.Decision(crossCopy)["ClientApplicationId"] = Guid.NewGuid().ToString();
        await harness.AssertPersistErrorAsync(crossCopy, "RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_MISMATCH",
            claimPayload: claimedIdentity);
    }

    [Fact]
    public async Task K5_both_principal_copies_bind_to_actor_and_missing_guc_fails_closed()
    {
        var ids = await harness.SeedSessionAsync();
        var actorMismatch = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        await harness.AssertPersistErrorAsync(actorMismatch, "RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH",
            actorOverride: Guid.NewGuid());

        var missingActor = Tip88B33Payloads.Create(ids, "SESSION_NOT_FOUND");
        await harness.AssertPersistErrorAsync(missingActor, "RAW_EXPORT_ACTOR_CONTEXT_MISSING",
            clearActorAfterClaim: true);
    }

    [Fact]
    public async Task K7_cross_evidence_rejects_primary_cause_and_permit_class_set_or_order_divergence()
    {
        var ids = await harness.SeedSessionAsync();
        var primary = Tip88B33Payloads.Create(ids, "EXPORT_ELIGIBILITY_INACTIVE");
        Tip88B33Payloads.Decision(primary)["EligibilityPrimaryCause"] = "GrantRevoked";
        await harness.AssertPersistErrorAsync(primary, "RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID");

        var setMismatch = Tip88B33Payloads.Create(ids, null);
        ((JsonArray)setMismatch["PermitClasses"]!)[0]!["RawClass"] = "ChipDg2Portrait";
        await harness.AssertPersistErrorAsync(setMismatch, "RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID");

        var orderMismatch = Tip88B33Payloads.Create(ids, null);
        Tip88B33Payloads.AddClass(orderMismatch, "Authorized", "ChipDg2Portrait");
        ((JsonArray)orderMismatch["PermitClasses"]!).Add(
            new JsonObject { ["RawClass"] = "ChipDg2Portrait", ["Ordinal"] = 1 });
        var permitClasses = (JsonArray)orderMismatch["PermitClasses"]!;
        permitClasses[0]!["Ordinal"] = 1;
        permitClasses[1]!["Ordinal"] = 0;
        await harness.AssertPersistErrorAsync(orderMismatch, "RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID");
    }

    [Fact]
    public async Task K8a_authorized_absent_or_empty_permit_classes_rejects_before_return()
    {
        var ids = await harness.SeedSessionAsync();
        var absent = Tip88B33Payloads.Create(ids, null);
        absent.Remove("PermitClasses");
        await harness.AssertPersistErrorAsync(absent,
            "RAW_EXPORT_AUTHORIZATION_AUTHORIZED_EVIDENCE_INCOMPLETE");

        var empty = Tip88B33Payloads.Create(ids, null);
        empty["PermitClasses"] = new JsonArray();
        await harness.AssertPersistErrorAsync(empty,
            "RAW_EXPORT_AUTHORIZATION_AUTHORIZED_EVIDENCE_INCOMPLETE");
    }

    [Fact]
    public async Task K8b_valid_non_empty_payload_commits_or_mutation_mode_proves_deferred_backstop()
    {
        var ids = await harness.SeedSessionAsync();
        var payload = Tip88B33Payloads.Create(ids, null);
        if (Environment.GetEnvironmentVariable("TAGEKYC_B33_PERMIT_INSERT_MUTATED") == "1")
        {
            await harness.AssertPermitClassBackstopMutationAsync(payload, ids.DecisionId);
            return;
        }

        var typedPayload = JsonSerializer.Deserialize<RawExportAuthorizationPersistencePayload>(
            payload.ToJsonString())!;
        await harness.PersistViaRepositoryAndCommitAsync(typedPayload);
        Assert.Equal((1, 0, 1, 4, 1, 1), await harness.GraphCountsAsync(ids.DecisionId));
    }

    [Fact]
    public async Task K9_valid_authorized_and_denied_payloads_persist_and_reload_full_graph()
    {
        var authorizedIds = await harness.SeedSessionAsync();
        var authorizedJson = Tip88B33Payloads.Create(authorizedIds, null);
        var authorizedPayload = JsonSerializer.Deserialize<RawExportAuthorizationPersistencePayload>(
            authorizedJson.ToJsonString())!;
        await harness.PersistViaRepositoryAndCommitAsync(authorizedPayload);
        var authorizedCounts = await harness.GraphCountsAsync(authorizedIds.DecisionId);
        Assert.Equal((1, 0, 1, 4, 1, 1), authorizedCounts);

        var deniedIds = await harness.SeedSessionAsync();
        var deniedJson = Tip88B33Payloads.Create(deniedIds, "EXPORT_ELIGIBILITY_INACTIVE");
        var deniedPayload = JsonSerializer.Deserialize<RawExportAuthorizationPersistencePayload>(
            deniedJson.ToJsonString())!;
        await harness.PersistViaRepositoryAndCommitAsync(deniedPayload);
        var deniedCounts = await harness.GraphCountsAsync(deniedIds.DecisionId);
        Assert.Equal((1, 1, 0, 0, 0, 0), deniedCounts);
    }

    [Fact]
    public async Task K10_migration_apply_rollback_reapply_preserves_b31_b32_acl_and_drops_only_b33()
    {
        var before = await harness.B3CatalogSnapshotAsync();
        Assert.Contains(before, value => value.StartsWith($"function:{PersistFunction}:", StringComparison.Ordinal));
        await harness.MigrateAsync(PreviousMigration);
        var down = await harness.B3CatalogSnapshotAsync();
        Assert.DoesNotContain(down, value => value.StartsWith($"function:{PersistFunction}:", StringComparison.Ordinal));
        Assert.Equal(before.Where(value => !value.StartsWith($"function:{PersistFunction}:", StringComparison.Ordinal)), down);

        await harness.MigrateAsync(Migration);
        Assert.Equal(before, await harness.B3CatalogSnapshotAsync());
        await harness.MigrateAsync(PreviousMigration);
        await harness.MigrateAsync(Migration);
        Assert.Equal(before, await harness.B3CatalogSnapshotAsync());
    }
}

internal sealed record Tip88B33Ids(
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid SessionId,
    Guid DecisionId,
    Guid PermitId,
    Guid PolicyId,
    string IdempotencyKey);

internal static class Tip88B33Payloads
{
    private const string Timestamp = "2026-07-23T00:00:00.123456+00:00";
    private const string Fingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ConsentHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    public static JsonObject Create(Tip88B33Ids ids, string? cause)
    {
        var authorized = cause is null;
        var decision = new JsonObject
        {
            ["PrincipalId"] = ids.PrincipalId.ToString(),
            ["ClientApplicationId"] = ids.ClientApplicationId.ToString(),
            ["ApiKeyId"] = Guid.NewGuid().ToString(),
            ["RequestedVerificationSessionId"] = ids.SessionId.ToString(),
            ["PolicyId"] = ids.PolicyId.ToString(),
            ["PolicyVersion"] = 1,
            ["FingerprintHash"] = Fingerprint,
            ["RawClassSelectionMode"] = "DefaultPolicySet",
            ["Outcome"] = authorized ? "Authorized" : "Denied",
            ["PrimaryCause"] = cause,
            ["ResolvedVerificationSessionId"] = null,
            ["SessionOwnerClientApplicationId"] = null,
            ["SessionSubjectRef"] = null,
            ["SessionState"] = null,
            ["BoundRuleSetVersion"] = null,
            ["CurrentRuleSetVersion"] = null,
            ["EligibilityPrimaryCause"] = null,
            ["EligibilityEvaluatedAtUtc"] = null,
            ["GrantPrincipalId"] = null,
            ["GrantPolicyId"] = null,
            ["GrantPolicyVersion"] = null,
            ["GrantRevision"] = null,
            ["LifecyclePolicyId"] = null,
            ["LifecyclePolicyVersion"] = null,
            ["LifecycleRevision"] = null,
            ["PurposeCode"] = null,
            ["RecipientClientApplicationId"] = null,
            ["SubjectConsentCause"] = null,
            ["ConsentScopeHash"] = null,
            ["SubjectConsentRecordId"] = null,
            ["ConsentRevision"] = null,
            ["ConsentValidFromUtc"] = null,
            ["ConsentValidUntilUtc"] = null,
            ["ConsentEvaluatedAtUtc"] = null,
            ["PolicyPermitTtlSeconds"] = null,
            ["DecisionExpiresAtUtc"] = null,
        };
        var payload = new JsonObject
        {
            ["PayloadSchemaVersion"] = 1,
            ["ExportDecisionId"] = ids.DecisionId.ToString(),
            ["IdempotencyIdentity"] = new JsonObject
            {
                ["PrincipalId"] = ids.PrincipalId.ToString(),
                ["ClientApplicationId"] = ids.ClientApplicationId.ToString(),
                ["RequestedVerificationSessionId"] = ids.SessionId.ToString(),
                ["IdempotencyKey"] = ids.IdempotencyKey,
            },
            ["Decision"] = decision,
            ["EligibilityCauses"] = new JsonArray(),
            ["FulfillmentRefs"] = new JsonArray(),
            ["Classes"] = new JsonArray(),
        };

        if (cause is not "SESSION_NOT_FOUND")
        {
            decision["ResolvedVerificationSessionId"] = ids.SessionId.ToString();
            decision["SessionOwnerClientApplicationId"] = ids.ClientApplicationId.ToString();
            decision["SessionState"] = "Completed";
        }
        if (cause is not ("SESSION_NOT_FOUND" or "SESSION_NOT_OWNED"))
        {
            decision["SessionSubjectRef"] = "b33-subject";
        }
        if (cause is "EXPORT_ELIGIBILITY_INACTIVE"
            or "POLICY_PERMIT_TTL_INVALID"
            or "REQUESTED_RAW_CLASSES_NOT_ALLOWED"
            or "SUBJECT_CONSENT_NOT_EFFECTIVE"
            or "SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT"
            || authorized)
        {
            decision["BoundRuleSetVersion"] = 1;
            decision["CurrentRuleSetVersion"] = 1;
            decision["EligibilityEvaluatedAtUtc"] = Timestamp;
        }
        if (cause == "EXPORT_ELIGIBILITY_INACTIVE")
        {
            decision["EligibilityPrimaryCause"] = "GrantMissing";
            ((JsonArray)payload["EligibilityCauses"]!).Add(
                new JsonObject { ["Ordinal"] = 0, ["Cause"] = "GrantMissing" });
        }
        if (cause is "POLICY_PERMIT_TTL_INVALID"
            or "REQUESTED_RAW_CLASSES_NOT_ALLOWED"
            or "SUBJECT_CONSENT_NOT_EFFECTIVE"
            or "SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT"
            || authorized)
        {
            decision["GrantPrincipalId"] = ids.PrincipalId.ToString();
            decision["GrantPolicyId"] = ids.PolicyId.ToString();
            decision["GrantPolicyVersion"] = 1;
            decision["GrantRevision"] = 1;
            decision["LifecyclePolicyId"] = ids.PolicyId.ToString();
            decision["LifecyclePolicyVersion"] = 1;
            decision["LifecycleRevision"] = 1;
            ((JsonArray)payload["FulfillmentRefs"]!).Add(new JsonObject
            {
                ["Ordinal"] = 0,
                ["RequirementType"] = "LegalApproval",
                ["FulfillmentEventId"] = Guid.NewGuid().ToString(),
                ["Revision"] = 1,
                ["ArtifactRef"] = "artifact",
                ["ArtifactVersion"] = "v1",
                ["ValidUntilUtc"] = null,
            });
            AddClass(payload, "PolicyAllowed", "ChipDg1");
        }
        if (cause is "REQUESTED_RAW_CLASSES_NOT_ALLOWED"
            or "SUBJECT_CONSENT_NOT_EFFECTIVE"
            or "SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT"
            || authorized)
        {
            decision["PolicyPermitTtlSeconds"] = 300;
            AddClass(payload, "Effective", "ChipDg1");
        }
        if (cause is "SUBJECT_CONSENT_NOT_EFFECTIVE"
            or "SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT"
            || authorized)
        {
            decision["PurposeCode"] = "SubjectRawBiometricExport";
            decision["RecipientClientApplicationId"] = ids.ClientApplicationId.ToString();
            decision["ConsentEvaluatedAtUtc"] = Timestamp;
        }
        if (cause == "SUBJECT_CONSENT_NOT_EFFECTIVE")
        {
            decision["SubjectConsentCause"] = "Missing";
        }
        if (cause == "SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT" || authorized)
        {
            decision["ConsentScopeHash"] = ConsentHash;
            decision["SubjectConsentRecordId"] = Guid.NewGuid().ToString();
            decision["ConsentRevision"] = 1;
            decision["ConsentValidFromUtc"] = Timestamp;
            AddClass(payload, "Consented", "ChipDg1");
        }
        if (authorized)
        {
            decision["DecisionExpiresAtUtc"] = "2026-07-23T00:05:00.123456+00:00";
            AddClass(payload, "Authorized", "ChipDg1");
            payload["Permit"] = new JsonObject
            {
                ["PermitId"] = ids.PermitId.ToString(),
                ["ResolvedVerificationSessionId"] = ids.SessionId.ToString(),
                ["SubjectRef"] = "b33-subject",
                ["PolicyId"] = ids.PolicyId.ToString(),
                ["PolicyVersion"] = 1,
                ["PurposeCode"] = "SubjectRawBiometricExport",
                ["RecipientClientApplicationId"] = ids.ClientApplicationId.ToString(),
                ["DecisionExpiresAtUtc"] = "2026-07-23T00:05:00.123456+00:00",
                ["SchemaVersion"] = 1,
            };
            payload["PermitClasses"] = new JsonArray(
                new JsonObject { ["RawClass"] = "ChipDg1", ["Ordinal"] = 0 });
        }

        return payload;
    }

    public static JsonObject Decision(JsonObject payload) => (JsonObject)payload["Decision"]!;

    public static void AddClass(JsonObject payload, string kind, string rawClass)
    {
        var classes = (JsonArray)payload["Classes"]!;
        var ordinal = classes.Count(node => node!["ClassKind"]!.GetValue<string>() == kind);
        classes.Add(new JsonObject { ["ClassKind"] = kind, ["RawClass"] = rawClass, ["Ordinal"] = ordinal });
    }
}

internal sealed class Tip88B33Harness(PostgresPersistenceFixture postgres)
{
    public async Task MigrateAsync(string migration)
    {
        await using var db = postgres.CreateDbContext();
        await db.GetService<IMigrator>().MigrateAsync(migration);
    }

    public async Task<Tip88B33Ids> SeedSessionAsync()
    {
        await using var db = postgres.CreateDbContext();
        var clientId = Guid.NewGuid();
        var session = VerificationSession.Create(clientId, "b33-subject",
            VerificationProfile.StandardEkycProfile, "raw-export", [RequiredCheckType.DocumentNfc],
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);
        await new EfVerificationSessionRepository(db).AddAsync(session);
        await new EfVerificationSessionRepository(db).SetStateAsync(session.Id, VerificationSessionState.Completed);
        return new Tip88B33Ids(Guid.NewGuid(), clientId, session.Id, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), $"b33-{Guid.NewGuid():N}");
    }

    public async Task AssertPersistAcceptedAsync(JsonObject payload)
    {
        await using var login = await CreateLoginAsync();
        await using var connection = await login.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PrepareAsync(connection, transaction, payload, claimPayload: payload, setActor: true, null, false);
        Assert.Equal(Guid.Parse(payload["ExportDecisionId"]!.GetValue<string>()),
            await PersistAsync(connection, transaction, payload));
        await transaction.RollbackAsync();
    }

    public async Task AssertPersistErrorAsync(
        JsonObject payload,
        string expectedMessage,
        bool claim = true,
        bool setActor = false,
        JsonObject? claimPayload = null,
        Guid? actorOverride = null,
        bool clearActorAfterClaim = false)
    {
        await using var login = await CreateLoginAsync();
        await using var connection = await login.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PrepareAsync(connection, transaction, payload, claim ? claimPayload ?? payload : null,
            setActor || claim, actorOverride, clearActorAfterClaim);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => PersistAsync(connection, transaction, payload));
        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal(expectedMessage, exception.MessageText);
        await transaction.RollbackAsync();
    }

    public async Task PersistViaRepositoryAndCommitAsync(RawExportAuthorizationPersistencePayload payload)
    {
        await using var login = await CreateLoginAsync();
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(login.ConnectionString).Options;
        await using var db = new TagEkycDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_runtime;");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{payload.Decision.PrincipalId.ToString()},true);");
        await ClaimAsync((NpgsqlConnection)db.Database.GetDbConnection(),
            (NpgsqlTransaction)transaction.GetDbTransaction(), JsonSerializer.SerializeToNode(payload)!.AsObject());
        Assert.Equal(payload.ExportDecisionId,
            await new EfRawExportAuthorizationRepository(db).PersistAuthorizationDecisionAsync(payload));
        await transaction.CommitAsync();
    }

    public async Task AssertPermitClassBackstopMutationAsync(JsonObject payload, Guid decisionId)
    {
        await using var login = await CreateLoginAsync();
        await using var connection = await login.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PrepareAsync(connection, transaction, payload, payload, true, null, false);
        Assert.Equal(decisionId, await PersistAsync(connection, transaction, payload));

        var exception = await Assert.ThrowsAsync<PostgresException>(() => transaction.CommitAsync());
        Assert.Equal("P0001", exception.SqlState);
        Assert.Equal("RAW_EXPORT_AUTHORIZATION_PERMIT_CLASSES_REQUIRED", exception.MessageText);

        await using var admin = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*) FROM tagekyc.raw_export_authorization_decisions
            WHERE "ExportDecisionId"=@id;
            """, admin);
        command.Parameters.AddWithValue("id", decisionId);
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }

    public async Task<(int Decision, int Eligibility, int Fulfillment, int Classes, int Permit, int PermitClasses)>
        GraphCountsAsync(Guid decisionId)
    {
        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
              (SELECT count(*) FROM tagekyc.raw_export_authorization_decisions WHERE "ExportDecisionId"=@id),
              (SELECT count(*) FROM tagekyc.raw_export_decision_eligibility_causes WHERE "ExportDecisionId"=@id),
              (SELECT count(*) FROM tagekyc.raw_export_decision_fulfillment_refs WHERE "ExportDecisionId"=@id),
              (SELECT count(*) FROM tagekyc.raw_export_decision_classes WHERE "ExportDecisionId"=@id),
              (SELECT count(*) FROM tagekyc.raw_export_authorization_permits WHERE "AuthorizationDecisionId"=@id),
              (SELECT count(*) FROM tagekyc.raw_export_permit_classes pc
                 JOIN tagekyc.raw_export_authorization_permits p ON p."PermitId"=pc."PermitId"
                 WHERE p."AuthorizationDecisionId"=@id);
            """, connection);
        command.Parameters.AddWithValue("id", decisionId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (checked((int)reader.GetInt64(0)), checked((int)reader.GetInt64(1)),
            checked((int)reader.GetInt64(2)), checked((int)reader.GetInt64(3)),
            checked((int)reader.GetInt64(4)), checked((int)reader.GetInt64(5)));
    }

    public async Task<string[]> B3CatalogSnapshotAsync()
    {
        await using var connection = await OpenAdminAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT value FROM (
              SELECT 'table:'||c.relname||':owner='||owner.rolname||
                     ':select='||has_table_privilege('tagekyc_runtime',c.oid,'SELECT')::text||
                     ':insert='||has_table_privilege('tagekyc_runtime',c.oid,'INSERT')::text AS value
              FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
              JOIN pg_roles owner ON owner.oid=c.relowner
              WHERE n.nspname='tagekyc' AND c.relname=ANY(@tables)
              UNION ALL
              SELECT 'function:'||p.proname||':'||pg_catalog.oidvectortypes(p.proargtypes)||
                     ':owner='||owner.rolname||
                     ':public='||has_function_privilege('public',p.oid,'EXECUTE')::text||
                     ':runtime='||has_function_privilege('tagekyc_runtime',p.oid,'EXECUTE')::text
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              JOIN pg_roles owner ON owner.oid=p.proowner
              WHERE n.nspname='tagekyc' AND p.proname=ANY(@functions)
            ) q ORDER BY value;
            """, connection);
        command.Parameters.AddWithValue("tables", new[]
        {
            "raw_export_authorization_idempotency", "raw_export_authorization_decisions",
            "raw_export_decision_eligibility_causes", "raw_export_decision_fulfillment_refs",
            "raw_export_decision_classes", "raw_export_authorization_permits", "raw_export_permit_classes",
        });
        command.Parameters.AddWithValue("functions", new[]
        {
            "raw_export_lock_verification_session_for_authorization",
            "raw_export_claim_or_read_authorization_idempotency",
            "raw_export_persist_authorization_decision",
            "enforce_raw_export_authorization_insert",
            "enforce_raw_export_decision_child_same_transaction",
            "enforce_raw_export_permit_child_same_transaction",
            "enforce_raw_export_permit_has_classes",
        });
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values.ToArray();
    }

    public async Task<NpgsqlConnection> OpenAdminAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private async Task<Tip88B33Login> CreateLoginAsync()
    {
        var role = $"tagekyc_b33_{Guid.NewGuid():N}";
        var password = $"B33_{Guid.NewGuid():N}";
        await using var connection = await OpenAdminAsync();
        await ExecuteAsync(connection, null,
            $"CREATE ROLE \"{role}\" WITH LOGIN PASSWORD '{password}' NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;");
        await ExecuteAsync(connection, null, $"GRANT tagekyc_runtime TO \"{role}\";");
        var builder = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Username = role,
            Password = password,
            Pooling = false,
        };
        return new Tip88B33Login(postgres.ConnectionString, builder.ConnectionString, role);
    }

    private static async Task PrepareAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        JsonObject payload,
        JsonObject? claimPayload,
        bool setActor,
        Guid? actorOverride,
        bool clearActorAfterClaim)
    {
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE tagekyc_runtime;");
        var principal = Guid.Parse(
            ((JsonObject)payload["IdempotencyIdentity"]!)["PrincipalId"]!.GetValue<string>());
        if (setActor)
        {
            await ExecuteAsync(connection, transaction,
                $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id','{actorOverride ?? principal}',true);");
        }
        if (claimPayload is not null)
        {
            // Claim must use its own principal actor even when persist later tests an actor mismatch.
            var claimPrincipal = Guid.Parse(
                ((JsonObject)claimPayload["IdempotencyIdentity"]!)["PrincipalId"]!.GetValue<string>());
            await ExecuteAsync(connection, transaction,
                $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id','{claimPrincipal}',true);");
            await ClaimAsync(connection, transaction, claimPayload);
            if (actorOverride is not null)
            {
                await ExecuteAsync(connection, transaction,
                    $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id','{actorOverride}',true);");
            }
        }
        if (clearActorAfterClaim)
        {
            await ExecuteAsync(connection, transaction,
                "SELECT pg_catalog.set_config('tagekyc.actor_principal_id','',true);");
        }
    }

    private static async Task ClaimAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        JsonObject payload)
    {
        var identity = (JsonObject)payload["IdempotencyIdentity"]!;
        var decision = Tip88B33Payloads.Decision(payload);
        await using var command = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.raw_export_claim_or_read_authorization_idempotency(
              @principal,@client,@session,@key,@fingerprint,@decision);
            """, connection, transaction);
        command.Parameters.AddWithValue("principal", Guid.Parse(identity["PrincipalId"]!.GetValue<string>()));
        command.Parameters.AddWithValue("client", Guid.Parse(identity["ClientApplicationId"]!.GetValue<string>()));
        command.Parameters.AddWithValue("session", Guid.Parse(identity["RequestedVerificationSessionId"]!.GetValue<string>()));
        command.Parameters.AddWithValue("key", identity["IdempotencyKey"]!.GetValue<string>());
        command.Parameters.AddWithValue("fingerprint",
            Convert.FromHexString(decision["FingerprintHash"]!.GetValue<string>()));
        command.Parameters.AddWithValue("decision", Guid.Parse(payload["ExportDecisionId"]!.GetValue<string>()));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("NewClaim", reader.GetString(0));
    }

    internal static async Task<Guid> PersistAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        JsonObject payload)
    {
        await using var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_persist_authorization_decision(@payload);", connection, transaction);
        command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value = payload.ToJsonString();
        return (Guid)(await command.ExecuteScalarAsync())!;
    }

    internal static async Task ExecuteAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task<object?> ScalarAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteScalarAsync();
    }

    private sealed class Tip88B33Login(
        string adminConnectionString,
        string connectionString,
        string role) : IAsyncDisposable
    {
        public string ConnectionString => connectionString;

        public async Task<NpgsqlConnection> OpenAsync()
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await ExecuteAsync(connection, null,
                $"REVOKE tagekyc_runtime FROM \"{role}\"; DROP ROLE \"{role}\";");
        }
    }
}
