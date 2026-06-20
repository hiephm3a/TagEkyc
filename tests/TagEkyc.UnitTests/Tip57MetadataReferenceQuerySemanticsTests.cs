using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip57MetadataReferenceQuerySemanticsTests
{
    [Fact]
    public void Metadata_reference_states_deny_all_reliance_proof_claims()
    {
        foreach (var state in Enum.GetValues<MetadataReferenceState>())
        {
            Assert.False(state.AllowsArtifactAccessProof());
            Assert.False(state.AllowsProviderEvidenceAvailabilityClaim());
            Assert.False(state.AllowsReadinessClaim());
            Assert.True(state.RequiresPacketBeforeReliance());
            Assert.True(state.IsNotReliableForEvidenceReliance());
            Assert.True(state.DeniesEvidenceAvailabilityProof());
            Assert.True(state.DeniesCompletePackageProof());
            Assert.True(state.DeniesArtifactAccessProof());
            Assert.True(state.DeniesProviderEvidenceAvailabilityProof());
            Assert.True(state.DeniesReadinessProof());
        }
    }

    [Fact]
    public void Registered_metadata_query_result_is_metadata_only_not_reliance_proof()
    {
        var result = new MetadataReferenceQueryResult(CreateRecord(MetadataReferenceState.RegisteredMetadata));

        Assert.True(result.HasRegisteredMetadata());
        Assert.False(result.IsNonSuccess());
        Assert.True(result.RequiresPacketBeforeReliance());
        Assert.True(result.IsNotReliableForEvidenceReliance());
        Assert.True(result.DeniesEvidenceAvailabilityProof());
        Assert.True(result.DeniesCompletePackageProof());
        Assert.True(result.DeniesArtifactAccessProof());
        Assert.True(result.DeniesProviderEvidenceAvailabilityProof());
        Assert.True(result.DeniesReadinessProof());
    }

    [Fact]
    public void Unknown_query_result_is_non_success_and_requires_packet_before_reliance()
    {
        var result = new MetadataReferenceQueryResult(null);

        Assert.False(result.HasRegisteredMetadata());
        Assert.True(result.IsNonSuccess());
        Assert.True(result.RequiresPacketBeforeReliance());
        Assert.True(result.IsNotReliableForEvidenceReliance());
        Assert.True(result.DeniesEvidenceAvailabilityProof());
        Assert.True(result.DeniesCompletePackageProof());
        Assert.True(result.DeniesArtifactAccessProof());
        Assert.True(result.DeniesProviderEvidenceAvailabilityProof());
        Assert.True(result.DeniesReadinessProof());
    }

    [Fact]
    public void Non_registered_query_result_states_are_non_success_and_not_reliable()
    {
        foreach (var state in Enum.GetValues<MetadataReferenceState>()
            .Where(state => state != MetadataReferenceState.RegisteredMetadata))
        {
            var result = new MetadataReferenceQueryResult(CreateRecord(state));

            Assert.False(result.HasRegisteredMetadata());
            Assert.True(result.IsNonSuccess());
            Assert.True(result.RequiresPacketBeforeReliance());
            Assert.True(result.IsNotReliableForEvidenceReliance());
            Assert.True(result.DeniesEvidenceAvailabilityProof());
            Assert.True(result.DeniesCompletePackageProof());
            Assert.True(result.DeniesArtifactAccessProof());
            Assert.True(result.DeniesProviderEvidenceAvailabilityProof());
            Assert.True(result.DeniesReadinessProof());
        }
    }

    private static MetadataReferenceRecord CreateRecord(MetadataReferenceState state) =>
        new(
            new MetadataReferenceId($"metadata-ref:{state}"),
            "metadata-reference",
            state,
            MetadataHash: null,
            RegisteredAt: DateTimeOffset.UnixEpoch,
            LastObservedAt: null);
}
