using System.Text.Json;
using System.Text.Json.Serialization;
using TagEkyc.Contracts.Common;

namespace TagEkyc.Contracts.BusinessConsumer;

public sealed record RequiredCheckRequestDto(
    RequiredCheckTypeDto CheckType,
    bool Required,
    decimal? MinimumConfidence);

[JsonConverter(typeof(CreateVerificationSessionRequestDtoJsonConverter))]
public sealed record CreateVerificationSessionRequestDto(
    string? ExternalSessionId,
    string SubjectRef,
    string Purpose,
    VerificationProfileDto Profile,
    IReadOnlyList<RequiredCheckRequestDto> RequiredChecks,
    DateTimeOffset ExpiresAt,
    string? ClientReference = null,
    string? Challenge = null,
    string? RequestId = null,
    string? CorrelationId = null)
{
    [JsonIgnore]
    public bool HasConflictingChallengeFields { get; init; }
}

public sealed record CreateVerificationSessionResponseDto(
    string VerificationSessionId,
    VerificationProfileDto Profile,
    VerificationSessionStateDto State,
    VerificationResultDto Result,
    string? Challenge,
    string? ClientReference,
    string RequestId,
    string CorrelationId,
    DateTimeOffset ExpiresAt);

public sealed record VerificationSessionSummaryDto(
    string VerificationSessionId,
    VerificationProfileDto Profile,
    string? ExternalSessionId,
    string? Challenge,
    string? ClientReference,
    string Purpose,
    VerificationSessionStateDto State,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    string? EvidencePackageId,
    string? EvidencePackageHash,
    string? ManifestHash,
    string RequestId,
    string CorrelationId,
    DateTimeOffset? CompletedAt);

public enum EvidenceLedgerSubmissionStatusDto
{
    Missing = 0,
    Submitted = 1,
}

public sealed record EvidenceLedgerRequiredCheckDto(
    RequiredCheckTypeDto CheckType,
    EvidenceLedgerSubmissionStatusDto SubmissionStatus,
    VerificationResultDto? Result,
    string? CurrentEvidenceResultId,
    string? PayloadHash,
    DateTimeOffset? CreatedAt);

public sealed record EvidenceLedgerCaptureArtifactDto(
    string CaptureArtifactId,
    string ArtifactType,
    string? ArtifactHash,
    DateTimeOffset CreatedAt);

public sealed record EvidenceLedgerEvidenceResultDto(
    string EvidenceResultId,
    string ResultType,
    VerificationResultDto Result,
    string? PayloadHash,
    DateTimeOffset CreatedAt);

public sealed record EvidenceLedgerDto(
    string VerificationSessionId,
    VerificationSessionStateDto State,
    bool EvidenceCompleteEligible,
    bool AllRequiredChecksPassed,
    IReadOnlyList<EvidenceLedgerRequiredCheckDto> RequiredChecks,
    IReadOnlyList<EvidenceLedgerCaptureArtifactDto> AcceptedCaptureArtifacts,
    IReadOnlyList<EvidenceLedgerEvidenceResultDto> AcceptedEvidenceResults);

public sealed record EvidenceRefSummaryDto(
    string ResultType,
    string EvidenceResultId,
    string Type,
    string Id,
    string? ArtifactHash);

public sealed record CompleteVerificationSessionRequestDto(
    bool ForceReview = false,
    string? RequestId = null,
    string? CorrelationId = null);

public sealed record CompleteVerificationSessionResponseDto(
    string VerificationSessionId,
    VerificationSessionStateDto State,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    string FinalDecisionId,
    string EvidencePackageId,
    string EvidencePackageHash,
    string ManifestHash,
    string? Challenge,
    string? ClientReference,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CompletedAt,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus);

public sealed record EvidencePackageSummaryDto(
    string EvidencePackageId,
    string VerificationSessionId,
    string PackageVersion,
    string PackageHash,
    string ManifestHash,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    IReadOnlyList<EvidenceRefSummaryDto> EvidenceRefs,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CompletedAt);

public sealed record EvidenceProofEngineRefDto(
    string EvidenceResultType,
    string EvidenceResultId,
    string EngineName,
    string EngineVersion,
    string CheckType);

public sealed record EvidencePackageVerificationViewDto(
    string ProofVersion,
    string Purpose,
    string SessionId,
    string IdentityRef,
    string PackageId,
    string PackageVersion,
    string CanonicalizationScheme,
    string HashAlgorithm,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    IReadOnlyList<RequiredCheckTypeDto> RequiredChecks,
    IReadOnlyList<RequiredCheckTypeDto> CompletedChecks,
    IReadOnlyList<EvidenceProofEngineRefDto> EvidenceEngines,
    DateTimeOffset SignedAt,
    string Challenge,
    string? ClientReference,
    string SignedManifestHash,
    string ResultHash,
    string ResultHashAlgorithm,
    string ResultHashCanonicalizationScheme,
    string SignatureValue,
    string SignatureFormat,
    string SignatureScheme,
    string SignatureAlgorithm,
    string KeyId,
    string PublicKeyJwk,
    string PublicKeyFingerprint);

public sealed record VerificationCompletedEventDto(
    string EventType,
    string DeliveryId,
    DateTimeOffset SentAt,
    string VerificationSessionId,
    string ClientApplicationId,
    VerificationProfileDto Profile,
    string? ExternalSessionId,
    VerificationResultDto Result,
    AssuranceLevelDto AssuranceLevel,
    string EvidencePackageId,
    string EvidencePackageHash,
    string ManifestHash,
    string RequestId,
    string CorrelationId,
    DateTimeOffset CompletedAt,
    SignaturePlaceholderStatusDto WebhookSignatureStatus,
    SignaturePlaceholderStatusDto EvidencePackageSignatureStatus);

public sealed class CreateVerificationSessionRequestDtoJsonConverter : JsonConverter<CreateVerificationSessionRequestDto>
{
    public override CreateVerificationSessionRequestDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var hasChallenge = TryGetProperty(root, "challenge", out var challengeElement);
        var hasLegacyChallenge = TryGetProperty(root, "bindingNonceHash", out var legacyChallengeElement);
        var challenge = hasChallenge ? ReadNullableString(challengeElement) : null;
        var legacyChallenge = hasLegacyChallenge ? ReadNullableString(legacyChallengeElement) : null;
        var challengeConflict = hasChallenge &&
            hasLegacyChallenge &&
            !string.Equals(challenge, legacyChallenge, StringComparison.Ordinal);

        var hasClientReference = TryGetProperty(root, "clientReference", out var clientReferenceElement);
        var hasLegacyClientReference = TryGetProperty(root, "externalTransactionId", out var legacyClientReferenceElement);
        var clientReference = hasClientReference ? ReadNullableString(clientReferenceElement) : null;
        var legacyClientReference = hasLegacyClientReference ? ReadNullableString(legacyClientReferenceElement) : null;
        var clientReferenceConflict = hasClientReference &&
            hasLegacyClientReference &&
            !string.Equals(clientReference, legacyClientReference, StringComparison.Ordinal);

        return new CreateVerificationSessionRequestDto(
            TryGetProperty(root, "externalSessionId", out var externalSessionIdElement) ? ReadNullableString(externalSessionIdElement) : null,
            ReadRequiredString(GetRequired(root, "subjectRef")),
            ReadRequiredString(GetRequired(root, "purpose")),
            VerificationProfileDtoJsonConverter.ParseWireValue(ReadRequiredString(GetRequired(root, "profile"))),
            JsonSerializer.Deserialize<IReadOnlyList<RequiredCheckRequestDto>>(GetRequired(root, "requiredChecks").GetRawText(), options)
                ?? [],
            JsonSerializer.Deserialize<DateTimeOffset>(GetRequired(root, "expiresAt").GetRawText(), options),
            hasClientReference ? clientReference : legacyClientReference,
            hasChallenge ? challenge : legacyChallenge,
            TryGetProperty(root, "requestId", out var requestIdElement) ? ReadNullableString(requestIdElement) : null,
            TryGetProperty(root, "correlationId", out var correlationIdElement) ? ReadNullableString(correlationIdElement) : null)
        {
            HasConflictingChallengeFields = challengeConflict || clientReferenceConflict,
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        CreateVerificationSessionRequestDto value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(
            writer,
            new
            {
                value.ExternalSessionId,
                value.SubjectRef,
                value.Purpose,
                value.Profile,
                value.RequiredChecks,
                value.ExpiresAt,
                value.ClientReference,
                value.Challenge,
                value.RequestId,
                value.CorrelationId,
            },
            options);
    }

    private static JsonElement GetRequired(JsonElement root, string propertyName) =>
        TryGetProperty(root, propertyName, out var value)
            ? value
            : throw new JsonException($"Missing required property '{propertyName}'.");

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string ReadRequiredString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : throw new JsonException("Expected a string value.");

    private static string? ReadNullableString(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new JsonException("Expected a string value."),
        };
}
