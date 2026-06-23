using System.Text.Json;
using System.Text.Json.Serialization;

namespace TagEkyc.Contracts.Common;

[JsonConverter(typeof(VerificationProfileDtoJsonConverter))]
public enum VerificationProfileDto
{
    StandardEkycProfile = 0,
    ChallengeBoundEkycProfile = 1,
}

public sealed class VerificationProfileDtoJsonConverter : JsonConverter<VerificationProfileDto>
{
    public override VerificationProfileDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Verification profile must be a string.");
        }

        return ParseWireValue(reader.GetString());
    }

    public static VerificationProfileDto ParseWireValue(string? value) =>
        value switch
        {
            "STANDARD_EKYC_PROFILE" or "StandardEkycProfile" => VerificationProfileDto.StandardEkycProfile,
            "CHALLENGE_BOUND_EKYC_PROFILE" or "ChallengeBoundEkycProfile" => VerificationProfileDto.ChallengeBoundEkycProfile,
            "TRANSACTION_BOUND_EKYC_PROFILE" or "TransactionBoundEkycProfile" => VerificationProfileDto.ChallengeBoundEkycProfile,
            _ => throw new JsonException($"Unknown verification profile '{value}'."),
        };

    public override void Write(
        Utf8JsonWriter writer,
        VerificationProfileDto value,
        JsonSerializerOptions options)
    {
        var wireValue = value switch
        {
            VerificationProfileDto.StandardEkycProfile => "STANDARD_EKYC_PROFILE",
            VerificationProfileDto.ChallengeBoundEkycProfile => "CHALLENGE_BOUND_EKYC_PROFILE",
            _ => throw new JsonException($"Unknown verification profile '{value}'."),
        };

        writer.WriteStringValue(wireValue);
    }
}

public enum VerificationSessionStateDto
{
    Created = 0,
    InProgress = 1,
    ReadyToComplete = 2,
    Completed = 3,
    Expired = 4,
    Cancelled = 5,
    TechnicalTerminal = 6,
}

public enum VerificationResultDto
{
    NotAvailable = 0,
    Passed = 1,
    RetryRequired = 2,
    FailedCaptureQuality = 3,
    FailedIdentity = 4,
    ReviewRequired = 5,
    TechnicalError = 6,
    NotSupported = 7,
}

public enum RequiredCheckTypeDto
{
    CaptureQuality = 0,
    DocumentOcr = 1,
    DocumentNfc = 2,
    FaceMatch = 3,
    Liveness = 4,
    Fingerprint = 5,
    RiskEvaluation = 6,
}

public enum AssuranceLevelDto
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Unknown = 4,
}

public enum SignaturePlaceholderStatusDto
{
    PlaceholderUnverified = 0,
    Signed = 1,
}
