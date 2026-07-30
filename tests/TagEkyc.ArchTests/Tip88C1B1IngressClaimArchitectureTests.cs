using System.Text;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B1IngressClaimArchitectureTests
{
    internal static readonly string[] IntendedIdentifiers =
    [
        "raw_export_source_ingress_claims",
        "raw_export_source_ingress_claim_aliases",
        "pk_raw_export_source_ingress_claims",
        "uq_raw_export_source_ingress_exact_artifact",
        "fk_raw_export_source_ingress_claims_session",
        "fk_raw_export_source_ingress_claims_acceptance",
        "fk_raw_export_source_ingress_claims_capture_artifact",
        "ck_raw_export_source_ingress_state",
        "pk_raw_export_source_ingress_claim_aliases",
        "uq_raw_export_source_ingress_alias_key",
        "fk_raw_export_source_ingress_alias_claim",
        "ck_raw_export_source_ingress_alias_state",
        "IX_raw_export_source_ingress_aliases_claim",
        "IX_raw_export_source_ingress_claims_acceptance",
        "IX_raw_export_source_ingress_claims_artifact",
        "IX_raw_export_source_ingress_claims_session",
        "tr_raw_export_source_ingress_claims_write_guard",
        "tr_raw_export_source_ingress_aliases_write_guard",
        "enforce_raw_export_source_ingress_write",
        "begin_raw_export_source_ingress_claim",
        "validate_raw_export_claim_evaluation_token",
        "IngressClaimId",
        "VerificationSessionId",
        "CaptureAcceptanceId",
        "CaptureArtifactId",
        "ClientApplicationId",
        "AuthenticatedPrincipalId",
        "ProducerId",
        "CaptureAgentInstanceId",
        "CaptureRevision",
        "RawClass",
        "SessionChallengeHash",
        "AuthoritySnapshotId",
        "IngressIdentityFingerprint",
        "ClaimState",
        "CommitmentKeySelectorId",
        "CommitmentKeySelectorVersion",
        "CreatedAtUtc",
        "IngressClaimAliasId",
        "IngressIdempotencyKey",
        "AttemptedIngressIdentityFingerprint",
        "ProducerClaimEnvelopeFingerprint",
        "AliasState",
        "CurrentClaimEvaluationId",
        "CurrentClaimEvaluationOwnerId",
        "CurrentClaimEvaluationDisposition",
        "CurrentTokenIssuedAtUtc",
        "CurrentTokenExpiresAtUtc",
        "CurrentTokenSchemaVersion",
        "CurrentTokenVariant",
        "CurrentTokenAudience",
        "CurrentTokenDigest",
        "CurrentClaimEvaluationRevision",
        "CurrentClaimEvaluationFence",
        "LatestIssuedTokenExpiresAtUtc",
    ];

    [Fact]
    public void C1B1_all_intended_identifiers_are_at_most_63_utf8_bytes()
    {
        var overlong = IntendedIdentifiers
            .Select(identifier => new
            {
                Identifier = identifier,
                Bytes = Encoding.UTF8.GetByteCount(identifier),
            })
            .Where(candidate => candidate.Bytes > 63)
            .ToArray();

        Assert.Empty(overlong);
    }
}
