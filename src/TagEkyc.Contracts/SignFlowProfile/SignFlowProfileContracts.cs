using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.BusinessConsumer;

namespace TagEkyc.Contracts.SignFlowProfile;

public static class SignFlowS1RequiredChecks
{
    public static readonly IReadOnlyList<RequiredCheckTypeDto> Values =
    [
        RequiredCheckTypeDto.CaptureQuality,
        RequiredCheckTypeDto.DocumentNfc,
        RequiredCheckTypeDto.FaceMatch,
        RequiredCheckTypeDto.Liveness,
    ];
}

public sealed record SigningAuthorizationBindingDto(
    string ExternalSessionId,
    string ClientReference,
    string Challenge,
    string EvidencePackageId,
    string EvidencePackageHash);

public sealed record SignFlowVerificationResultDto(
    VerificationSessionSummaryDto Session,
    SigningAuthorizationBindingDto Binding);
