using TagEkyc.Domain;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

public sealed class Tip88B32AuthorizationCodecTests
{
    private static readonly Guid PrincipalId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid ClientApplicationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid RequestedVerificationSessionId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
    private static readonly Guid PolicyId = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");

    [Fact]
    public void H8a_default_policy_set_matches_independent_golden_vector()
    {
        AssertHash("A092845B29C38B7929B178D8015F08703EDE04A64A7ECF9234377071F1F1C15C");
    }

    [Fact]
    public void H8b_explicit_subset_matches_independent_golden_vector()
    {
        AssertHash(
            "3E57044724280343A46D18947650873B841C040879CD7530DE172B6267B7AFAC",
            selectionMode: RawExportRawClassSelectionMode.ExplicitSubset,
            requestedRawClasses: [RawExportRawClass.ChipDg1, RawExportRawClass.LiveSelfieImage, RawExportRawClass.HandSignatureImage]);
    }

    [Fact]
    public void H8c_explicit_full_ceiling_is_distinct_from_omitted_default()
    {
        var explicitFull = Hash(
            selectionMode: RawExportRawClassSelectionMode.ExplicitSubset,
            requestedRawClasses: Enum.GetValues<RawExportRawClass>());

        Assert.Equal("231A94774A3F44F51AE369CC1CCE1D8A66DCAD2644B9EFBFB5B0D972FFA950F2", Convert.ToHexString(explicitFull));
        Assert.NotEqual(Convert.ToHexString(Hash()), Convert.ToHexString(explicitFull));
    }

    [Fact]
    public void H8d_reordered_explicit_input_canonicalizes_to_the_same_hash()
    {
        var reordered = Hash(
            selectionMode: RawExportRawClassSelectionMode.ExplicitSubset,
            requestedRawClasses: [RawExportRawClass.HandSignatureImage, RawExportRawClass.ChipDg1, RawExportRawClass.LiveSelfieImage]);

        Assert.Equal("3E57044724280343A46D18947650873B841C040879CD7530DE172B6267B7AFAC", Convert.ToHexString(reordered));
    }

    [Fact]
    public void H8e_each_identity_field_mutation_matches_its_golden_vector_and_changes_the_hash()
    {
        var baseline = Convert.ToHexString(Hash());
        var mutations = new[]
        {
            ("DBE5EC432D32B8DF3113D00F97982E0E0D49FE6C9EAF981261CB55C777A5A8E2", Hash(principalId: Guid.Parse("11111111-2222-3333-4444-555555555556"))),
            ("15ED2D22954F3C93C6DEED6C8593310650010AC35CC57AEF7833A7D6FF387F37", Hash(clientApplicationId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeef"))),
            ("6EE01F473A67DEA3AFD4D81ACEDB68531D4FE1D2E0CBC870CC3E21D7CA8A0A29", Hash(requestedVerificationSessionId: Guid.Parse("01234567-89ab-cdef-0123-456789abcdee"))),
            ("D9609BE3B6B52FEDDDE0B0F42152D96DC723618A020432BF6576DED6B305FBD7", Hash(policyId: Guid.Parse("fedcba98-7654-3210-fedc-ba9876543211"))),
        };

        Assert.All(mutations, mutation =>
        {
            Assert.Equal(mutation.Item1, Convert.ToHexString(mutation.Item2));
            Assert.NotEqual(baseline, Convert.ToHexString(mutation.Item2));
        });
    }

    [Fact]
    public void H8f_policy_version_boundaries_and_mutation_match_independent_vectors()
    {
        AssertHash("A092845B29C38B7929B178D8015F08703EDE04A64A7ECF9234377071F1F1C15C", policyVersion: 1);
        AssertHash("975A69F36B0E2111056526B2150A496458823066013A25708E0B8E727DD56096", policyVersion: 2);
        AssertHash("87AC2C88CC5E3BEAB3C00CE59FE58C70F17E1D73C3A774E1EA262325E1DE4E5F", policyVersion: int.MaxValue - 1);
    }

    [Fact]
    public void H8g_selection_mode_mutation_changes_the_hash()
    {
        var explicitOne = Hash(
            selectionMode: RawExportRawClassSelectionMode.ExplicitSubset,
            requestedRawClasses: [RawExportRawClass.ChipDg1]);

        Assert.Equal("763FAE20B892691469DE7EA27661FB443E8FEEF12EF1624FDB4356F23FFEAEDC", Convert.ToHexString(explicitOne));
        Assert.NotEqual(Convert.ToHexString(Hash()), Convert.ToHexString(explicitOne));
    }

    [Fact]
    public void H8h_purpose_code_mutation_matches_independent_vector_and_changes_the_hash()
    {
        var changed = Hash(purposeCode: "SubjectRawBiometricExportV2");

        Assert.Equal("B5ADAD014F8F7AC979A22B9612F97EA11C1C9B248C77431C8B9226B62A00D91E", Convert.ToHexString(changed));
        Assert.NotEqual(Convert.ToHexString(Hash()), Convert.ToHexString(changed));
    }

    [Fact]
    public void H8i_distinct_explicit_subsets_match_independent_vectors_and_differ()
    {
        var first = Hash(
            selectionMode: RawExportRawClassSelectionMode.ExplicitSubset,
            requestedRawClasses: [RawExportRawClass.ChipDg1, RawExportRawClass.LiveSelfieImage, RawExportRawClass.HandSignatureImage]);
        var second = Hash(
            selectionMode: RawExportRawClassSelectionMode.ExplicitSubset,
            requestedRawClasses: [RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LivenessMedia]);

        Assert.Equal("3E57044724280343A46D18947650873B841C040879CD7530DE172B6267B7AFAC", Convert.ToHexString(first));
        Assert.Equal("3BD20D84A2EB90D1A0CF34D36A9CCA9B2E7606621F715C852DB8102CCE9C31A2", Convert.ToHexString(second));
        Assert.NotEqual(Convert.ToHexString(first), Convert.ToHexString(second));
    }

    [Fact]
    public void H8j_case_distinct_idempotency_keys_are_distinct_tuples_with_the_same_fingerprint()
    {
        var lowerKeyTuple = (PrincipalId, ClientApplicationId, RequestedVerificationSessionId, Key: "request-abc");
        var upperKeyTuple = (PrincipalId, ClientApplicationId, RequestedVerificationSessionId, Key: "request-ABC");

        Assert.NotEqual(lowerKeyTuple, upperKeyTuple);
        Assert.Equal("A092845B29C38B7929B178D8015F08703EDE04A64A7ECF9234377071F1F1C15C", Convert.ToHexString(Hash()));
        Assert.Equal(Convert.ToHexString(Hash()), Convert.ToHexString(Hash()));
    }

    private static void AssertHash(
        string expected,
        int policyVersion = 1,
        string purposeCode = "SubjectRawBiometricExport",
        RawExportRawClassSelectionMode selectionMode = RawExportRawClassSelectionMode.DefaultPolicySet,
        IEnumerable<RawExportRawClass>? requestedRawClasses = null)
    {
        Assert.Equal(expected, Convert.ToHexString(Hash(policyVersion: policyVersion, purposeCode: purposeCode, selectionMode: selectionMode, requestedRawClasses: requestedRawClasses)));
    }

    private static byte[] Hash(
        Guid? principalId = null,
        Guid? clientApplicationId = null,
        Guid? requestedVerificationSessionId = null,
        Guid? policyId = null,
        int policyVersion = 1,
        string purposeCode = "SubjectRawBiometricExport",
        RawExportRawClassSelectionMode selectionMode = RawExportRawClassSelectionMode.DefaultPolicySet,
        IEnumerable<RawExportRawClass>? requestedRawClasses = null)
    {
        return RawExportAuthorizationFingerprintCodec.ComputeHash(
            principalId ?? PrincipalId,
            clientApplicationId ?? ClientApplicationId,
            requestedVerificationSessionId ?? RequestedVerificationSessionId,
            policyId ?? PolicyId,
            policyVersion,
            purposeCode,
            selectionMode,
            requestedRawClasses);
    }
}
