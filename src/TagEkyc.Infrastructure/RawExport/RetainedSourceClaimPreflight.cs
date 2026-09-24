using System.Globalization;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

// Values are supplied by the transaction owner from B-R/B-B and the admitted
// metadata. This collaborator cannot open a connection, select authority, or
// read a body. In particular, the commitment selector is the locked claim's
// selector, not the current configuration's selector on an existing claim.
internal sealed record SourceClaimPreflightInput(
    ReadOnlyMemory<byte> AttemptedIngressIdentityFingerprint,
    ReadOnlyMemory<byte> ProducerEnvelopeFingerprint,
    Guid VerificationSessionId,
    Guid CaptureArtifactId,
    int CaptureRevision,
    string RawClass,
    string StableDataScopeId,
    string ControllerIdentity,
    string SubjectRef,
    string CommitmentSelectorId,
    int CommitmentSelectorVersion,
    string TokenVariant,
    ReadOnlyMemory<byte> ClaimedPlaintextDigest,
    long ClaimedPlaintextLength,
    string MediaType,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset PlaintextRetentionStartedAtUtc,
    DateTimeOffset PlaintextRetentionExpiresAtUtc,
    int PlaintextRetentionBudgetSeconds);

internal sealed record SourceClaimPreflightResult(
    byte[] ProducerEnvelopeFingerprint,
    byte[]? ContentCommitment,
    byte[] SubjectToken);

internal sealed record SourceClaimProfileDigests(
    byte[] NonceSeedCommitment,
    byte[] FramingParametersDigest);

internal sealed class RawIngressProviderCapabilityUnavailableException(string reason)
    : InvalidOperationException(reason);

internal sealed class RetainedSourceClaimPreflight(
    IContentCommitmentService contentCommitments,
    ISubjectRefTokenService subjectTokens)
{
    internal static byte[] ComputeEnvelope(SourceClaimPreflightInput input) =>
        C1HashCanonical.ComputeProducerClaimEnvelopeFingerprint(
            input.AttemptedIngressIdentityFingerprint.Span,
            input.ClaimedPlaintextLength,
            input.MediaType,
            input.CapturedAtUtc,
            input.PlaintextRetentionStartedAtUtc,
            input.PlaintextRetentionExpiresAtUtc,
            input.PlaintextRetentionBudgetSeconds);

    // The caller passes the qualified subject selector explicitly. There is no
    // fixture selector, latest-version lookup, DB dependency, or default here.
    internal async Task<SourceClaimPreflightResult?> ComputeAsync(
        SourceClaimPreflightInput input,
        SubjectTokenKeySelector subjectSelector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(subjectSelector);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.TokenVariant is not ("NewClaimEvaluationToken" or "ExistingClaimComparisonToken"))
            return null;
        var envelope = ComputeEnvelope(input);
        if (!envelope.AsSpan().SequenceEqual(input.ProducerEnvelopeFingerprint.Span))
            return null;

        var commitmentPayload = C1HashCanonical.EncodeLengthPrefixedPayload(
            "TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1",
            input.StableDataScopeId,
            input.ControllerIdentity,
            input.VerificationSessionId.ToString("N"),
            input.CaptureArtifactId.ToString("N"),
            input.CaptureRevision.ToString(CultureInfo.InvariantCulture),
            input.RawClass,
            Convert.ToHexString(input.ClaimedPlaintextDigest.Span).ToLowerInvariant(),
            input.ClaimedPlaintextLength.ToString(CultureInfo.InvariantCulture),
            input.MediaType);
        var commitment = await contentCommitments.ComputeAsync(
            new CommitmentKeySelector(input.CommitmentSelectorId, input.CommitmentSelectorVersion),
            commitmentPayload, cancellationToken);
        var historicUnavailable = !commitment.IsSuccess
            && input.TokenVariant == "ExistingClaimComparisonToken";
        if (!commitment.IsSuccess && !historicUnavailable)
            throw new RawIngressProviderCapabilityUnavailableException(
                "RAW_EXPORT_CONTENT_COMMITMENT_PROVIDER_FAILURE");

        byte[] subjectTokenBytes;
        if (historicUnavailable)
        {
            // Preserve the existing SQL historic-unavailable path: NULL
            // commitment, zero token placeholder, and no subject provider call.
            subjectTokenBytes = new byte[32];
        }
        else
        {
            var subjectPayload = C1HashCanonical.EncodeLengthPrefixedPayload(
                "TAG-EKYC:RAW-EXPORT:SUBJECT-TOKEN:C1:V1",
                input.StableDataScopeId,
                input.ControllerIdentity,
                input.SubjectRef.Normalize());
            var subjectToken = await subjectTokens.ComputeAsync(
                subjectSelector, subjectPayload, cancellationToken);
            if (!subjectToken.IsSuccess)
                throw new RawIngressProviderCapabilityUnavailableException(
                    "RAW_EXPORT_SUBJECT_TOKEN_PROVIDER_FAILURE");
            subjectTokenBytes = subjectToken.Token.ToArray();
        }

        return new SourceClaimPreflightResult(
            envelope,
            commitment.IsSuccess ? commitment.Mac.ToArray() : null,
            subjectTokenBytes);
    }

    // Kept separate so the legacy caller retains its provider-failure-before-
    // custody-configuration order. Both calls remain inside the A3 owner's B.
    internal static SourceClaimProfileDigests ComputeProfileDigests(SourceEncryptionProfileBundle profile) =>
        new(
            C1HashCanonical.ComputeNonceSeedCommitment(profile.NonceStrategyId),
            C1HashCanonical.ComputeFramingParametersDigest(
                profile.EncryptionSuiteId, profile.EncryptionFramingVersion,
                profile.ChunkSize, profile.NonceStrategyId));
}
