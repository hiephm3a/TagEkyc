using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip60LocalDevMetadataReferenceRegistryTests
{
    [Fact]
    public async Task LocalDev_registry_can_register_and_query_registered_metadata_references()
    {
        var registry = new LocalDevInMemoryMetadataReferenceRegistry();
        var registration = CreateRegistration(
            "metadata-ref:tip60-registered",
            MetadataReferenceState.RegisteredMetadata);

        await registry.RegisterAsync(registration);
        var result = await registry.QueryAsync(registration.ReferenceId);

        Assert.True(result.HasRegisteredMetadata());
        Assert.False(result.IsNonSuccess());
        Assert.NotNull(result.Reference);
        Assert.Equal(registration.ReferenceId, result.Reference.ReferenceId);
        Assert.Equal(registration.ReferenceKind, result.Reference.ReferenceKind);
        Assert.Equal(registration.State, result.Reference.State);
        Assert.Equal(registration.MetadataHash, result.Reference.MetadataHash);
        Assert.Equal(registration.RegisteredAt, result.Reference.RegisteredAt);
        Assert.NotNull(result.Reference.LastObservedAt);
    }

    [Fact]
    public async Task Unknown_query_remains_non_success_and_not_reliable()
    {
        var registry = new LocalDevInMemoryMetadataReferenceRegistry();

        var result = await registry.QueryAsync(new MetadataReferenceId("metadata-ref:tip60-unknown"));

        Assert.Null(result.Reference);
        Assert.False(result.HasRegisteredMetadata());
        Assert.True(result.IsNonSuccess());
        Assert.True(result.RequiresPacketBeforeReliance());
        Assert.True(result.IsNotReliableForEvidenceReliance());
    }

    [Fact]
    public async Task Registered_metadata_still_denies_artifact_package_provider_and_readiness_proofs()
    {
        var registry = new LocalDevInMemoryMetadataReferenceRegistry();
        var registration = CreateRegistration(
            "metadata-ref:tip60-non-proof",
            MetadataReferenceState.RegisteredMetadata);

        await registry.RegisterAsync(registration);
        var result = await registry.QueryAsync(registration.ReferenceId);

        Assert.True(result.HasRegisteredMetadata());
        Assert.True(result.RequiresPacketBeforeReliance());
        Assert.True(result.IsNotReliableForEvidenceReliance());
        Assert.True(result.DeniesEvidenceAvailabilityProof());
        Assert.True(result.DeniesArtifactAccessProof());
        Assert.True(result.DeniesCompletePackageProof());
        Assert.True(result.DeniesProviderEvidenceAvailabilityProof());
        Assert.True(result.DeniesReadinessProof());
    }

    [Fact]
    public async Task Non_registered_metadata_states_remain_non_success_in_the_registry()
    {
        var registry = new LocalDevInMemoryMetadataReferenceRegistry();

        foreach (var state in Enum.GetValues<MetadataReferenceState>()
            .Where(state => state != MetadataReferenceState.RegisteredMetadata))
        {
            var registration = CreateRegistration($"metadata-ref:tip60-{state}", state);

            await registry.RegisterAsync(registration);
            var result = await registry.QueryAsync(registration.ReferenceId);

            Assert.False(result.HasRegisteredMetadata());
            Assert.True(result.IsNonSuccess());
            Assert.True(result.RequiresPacketBeforeReliance());
            Assert.True(result.IsNotReliableForEvidenceReliance());
            Assert.True(result.DeniesEvidenceAvailabilityProof());
            Assert.True(result.DeniesArtifactAccessProof());
            Assert.True(result.DeniesCompletePackageProof());
            Assert.True(result.DeniesProviderEvidenceAvailabilityProof());
            Assert.True(result.DeniesReadinessProof());
        }
    }

    [Fact]
    public void Tip55_and_tip57_metadata_semantics_are_not_weakened()
    {
        foreach (var state in Enum.GetValues<MetadataReferenceState>())
        {
            Assert.False(state.AllowsEvidenceAvailabilityClaim());
            Assert.False(state.AllowsArtifactAccessProof());
            Assert.False(state.AllowsPackageCompletenessClaim());
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

    private static MetadataReferenceRegistration CreateRegistration(
        string referenceId,
        MetadataReferenceState state) =>
        new(
            new MetadataReferenceId(referenceId),
            "metadata-reference",
            state,
            new HashRef("sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"),
            DateTimeOffset.UnixEpoch);
}
