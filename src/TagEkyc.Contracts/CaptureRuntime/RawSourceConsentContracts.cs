using System.Text.Json.Serialization;

namespace TagEkyc.Contracts.CaptureRuntime;

public sealed record E01RecordRequest(
    [property: JsonPropertyName("externalConsentArtifactRef")] string ExternalConsentArtifactRef,
    [property: JsonPropertyName("sourceVersion")] string SourceVersion,
    [property: JsonPropertyName("expectedReferenceRevision")] long ExpectedReferenceRevision,
    [property: JsonPropertyName("consentTextVersion")] string ConsentTextVersion,
    [property: JsonPropertyName("consentTextContentHash")] string ConsentTextContentHash,
    [property: JsonPropertyName("validFromUtc")] DateTimeOffset ValidFromUtc,
    [property: JsonPropertyName("validUntilUtc")] DateTimeOffset ValidUntilUtc);

public sealed record E01BoundResponse(
    [property: JsonPropertyName("consentReferenceId")] Guid ConsentReferenceId,
    [property: JsonPropertyName("consentReferenceRevision")] long ConsentReferenceRevision,
    [property: JsonPropertyName("consentBindingId")] Guid ConsentBindingId);

public sealed record E01WithdrawRequest(
    [property: JsonPropertyName("expectedReferenceRevision")] long ExpectedReferenceRevision,
    [property: JsonPropertyName("sourceVersion")] string SourceVersion,
    [property: JsonPropertyName("decisionRef")] string DecisionRef);

public sealed record E01WithdrawnResponse(
    [property: JsonPropertyName("consentReferenceId")] Guid ConsentReferenceId,
    [property: JsonPropertyName("consentReferenceRevision")] long ConsentReferenceRevision,
    [property: JsonPropertyName("state")] string State);
