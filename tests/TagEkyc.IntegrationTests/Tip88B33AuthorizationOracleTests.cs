using System.Text.Json.Nodes;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88B33AuthorizationOracleTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string Migration = "20260723052003_Tip88B33RawExportAuthorizationPersistFunction";
    private const string ShapeInvalid = "RAW_EXPORT_AUTHORIZATION_EVIDENCE_SHAPE_INVALID";
    private const string AuthorizedIncomplete =
        "RAW_EXPORT_AUTHORIZATION_AUTHORIZED_EVIDENCE_INCOMPLETE";

    private readonly Tip88B33Harness harness = new(postgres);

    public Task InitializeAsync() => harness.MigrateAsync(Migration);
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public Task K6_session_not_found_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "SESSION_NOT_FOUND",
            over => Decision(over)["ResolvedVerificationSessionId"] =
                Decision(over)["RequestedVerificationSessionId"]!.DeepClone(),
            under => Decision(under)["PrimaryCause"] = null);

    [Fact]
    public Task K6_session_not_owned_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "SESSION_NOT_OWNED",
            over => Decision(over)["SessionSubjectRef"] = "must-not-leak",
            under => Decision(under)["ResolvedVerificationSessionId"] = null);

    [Fact]
    public Task K6_session_not_completed_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "SESSION_NOT_COMPLETED",
            over => Decision(over)["BoundRuleSetVersion"] = 1,
            under => Decision(under)["SessionSubjectRef"] = null);

    [Fact]
    public Task K6_export_eligibility_inactive_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "EXPORT_ELIGIBILITY_INACTIVE",
            over => Decision(over)["PolicyPermitTtlSeconds"] = 300,
            under => Decision(under)["EligibilityPrimaryCause"] = null);

    [Fact]
    public Task K6_policy_permit_ttl_invalid_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "POLICY_PERMIT_TTL_INVALID",
            over => Decision(over)["PurposeCode"] = "SubjectRawBiometricExport",
            under => RemoveClass(under, "PolicyAllowed"));

    [Fact]
    public Task K6_requested_raw_classes_not_allowed_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "REQUESTED_RAW_CLASSES_NOT_ALLOWED",
            over => Decision(over)["PurposeCode"] = "SubjectRawBiometricExport",
            under => RemoveClass(under, "Effective"));

    [Fact]
    public Task K6_subject_consent_not_effective_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "SUBJECT_CONSENT_NOT_EFFECTIVE",
            over => ((JsonArray)over["EligibilityCauses"]!).Add(
                new JsonObject { ["Ordinal"] = 0, ["Cause"] = "GrantMissing" }),
            under => Decision(under)["PurposeCode"] = null);

    [Fact]
    public Task K6_subject_consent_coverage_insufficient_exact_accepts_over_and_under_reject()
        => AssertDeniedShapeAsync(
            "SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT",
            over => Decision(over)["SubjectConsentCause"] = "Missing",
            under => RemoveClass(under, "Consented"));

    [Fact]
    public async Task K6_authorized_exact_accepts_over_rejects_and_under_is_incomplete()
    {
        var ids = await harness.SeedSessionAsync();
        await harness.AssertPersistAcceptedAsync(Tip88B33Payloads.Create(ids, null));

        var over = Tip88B33Payloads.Create(ids, null);
        Decision(over)["EligibilityPrimaryCause"] = "GrantMissing";
        ((JsonArray)over["EligibilityCauses"]!).Add(
            new JsonObject { ["Ordinal"] = 0, ["Cause"] = "GrantMissing" });
        await harness.AssertPersistErrorAsync(over, ShapeInvalid);

        var under = Tip88B33Payloads.Create(ids, null);
        Decision(under)["PurposeCode"] = null;
        await harness.AssertPersistErrorAsync(under, AuthorizedIncomplete);
    }

    private async Task AssertDeniedShapeAsync(
        string cause,
        Action<JsonObject> overpopulate,
        Action<JsonObject> underpopulate)
    {
        var ids = await harness.SeedSessionAsync();
        await harness.AssertPersistAcceptedAsync(Tip88B33Payloads.Create(ids, cause));

        var over = Tip88B33Payloads.Create(ids, cause);
        overpopulate(over);
        await harness.AssertPersistErrorAsync(over, ShapeInvalid);

        var under = Tip88B33Payloads.Create(ids, cause);
        underpopulate(under);
        await harness.AssertPersistErrorAsync(under, ShapeInvalid);
    }

    private static JsonObject Decision(JsonObject payload) => Tip88B33Payloads.Decision(payload);

    private static void RemoveClass(JsonObject payload, string kind)
    {
        var classes = (JsonArray)payload["Classes"]!;
        var index = classes
            .Select((node, index) => (node, index))
            .Single(pair => pair.node!["ClassKind"]!.GetValue<string>() == kind)
            .index;
        classes.RemoveAt(index);
    }
}
