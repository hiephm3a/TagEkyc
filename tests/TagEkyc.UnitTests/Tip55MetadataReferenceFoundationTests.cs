using System.Reflection;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip55MetadataReferenceFoundationTests
{
    [Theory]
    [InlineData("metadata-ref:document-front")]
    [InlineData("reference:session-artifact-001")]
    [InlineData("artifact-ref:opaque-001")]
    public void Metadata_reference_id_accepts_metadata_only_identity(string value)
    {
        var referenceId = new MetadataReferenceId(value);

        Assert.Equal(value, referenceId.Value);
        Assert.Equal("[metadata-reference-id]", referenceId.ToString());
        Assert.DoesNotContain(referenceId.Value, referenceId.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("raw:artifact")]
    [InlineData("payload:provider-response")]
    [InlineData("provider-payload:opaque")]
    [InlineData("bytes:artifact")]
    [InlineData("artifact-bytes:document")]
    [InlineData("artifact-raw:document")]
    [InlineData("raw-artifact:document")]
    [InlineData("content:artifact")]
    [InlineData("secret:value")]
    [InlineData("token:value")]
    public void Metadata_reference_id_rejects_raw_payload_or_artifact_byte_shaped_values(string value)
    {
        Assert.Throws<ArgumentException>(() => new MetadataReferenceId(value));
    }

    [Fact]
    public void Metadata_reference_state_marks_only_registered_metadata_as_not_non_success()
    {
        foreach (var state in Enum.GetValues<MetadataReferenceState>())
        {
            if (state == MetadataReferenceState.RegisteredMetadata)
            {
                Assert.False(state.IsNonSuccess());
            }
            else
            {
                Assert.True(state.IsNonSuccess());
            }
        }
    }

    [Fact]
    public void Metadata_reference_states_do_not_allow_evidence_availability_or_package_completeness_claims()
    {
        foreach (var state in Enum.GetValues<MetadataReferenceState>())
        {
            Assert.False(state.AllowsEvidenceAvailabilityClaim());
            Assert.False(state.AllowsPackageCompletenessClaim());
        }
    }

    [Fact]
    public void Metadata_reference_application_contracts_are_metadata_only()
    {
        var contractTypes = new[]
        {
            typeof(MetadataReferenceRegistration),
            typeof(MetadataReferenceRecord),
            typeof(MetadataReferenceQueryResult),
        };
        var publicProperties = contractTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .ToArray();
        var forbiddenNameFragments = new[]
        {
            "Raw",
            "Payload",
            "Bytes",
            "Byte",
            "Content",
            "Download",
            "Access",
        };

        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(MetadataReferenceRegistration.ReferenceId) &&
                property.PropertyType == typeof(MetadataReferenceId));
        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(MetadataReferenceRegistration.State) &&
                property.PropertyType == typeof(MetadataReferenceState));
        Assert.DoesNotContain(
            publicProperties,
            property => forbiddenNameFragments.Any(fragment =>
                property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(
            publicProperties,
            property => property.PropertyType == typeof(byte[]) ||
                property.PropertyType == typeof(Stream));
    }
}
