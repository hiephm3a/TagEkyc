using TagEkyc.Domain;

namespace TagEkyc.Application.VerificationSessions;

internal static class EvidenceSelection
{
    public static IReadOnlyDictionary<RequiredCheckType, EvidenceResult> LatestEvidenceByRequiredCheck(
        IEnumerable<EvidenceResult> evidence) =>
        evidence
            .OrderBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .Select(candidate => new
            {
                Check = MapEvidenceToCheck(candidate.ResultType),
                Evidence = candidate,
            })
            .GroupBy(candidate => candidate.Check)
            .ToDictionary(group => group.Key, group => group.Last().Evidence);

    public static bool AllRequiredChecksPassed(
        VerificationSession session,
        IEnumerable<EvidenceResult> allEvidence)
    {
        var latestByCheck = LatestEvidenceByRequiredCheck(allEvidence);

        return session.RequiredChecks.All(check =>
            latestByCheck.TryGetValue(check, out var latest) &&
            latest.Result == VerificationResult.Passed);
    }

    public static RequiredCheckType MapEvidenceToCheck(EvidenceResultType resultType) =>
        resultType switch
        {
            EvidenceResultType.CaptureQuality => RequiredCheckType.CaptureQuality,
            EvidenceResultType.DocumentOcr => RequiredCheckType.DocumentOcr,
            EvidenceResultType.NfcValidation => RequiredCheckType.DocumentNfc,
            EvidenceResultType.FaceMatch => RequiredCheckType.FaceMatch,
            EvidenceResultType.Liveness => RequiredCheckType.Liveness,
            EvidenceResultType.FingerprintMatch => RequiredCheckType.Fingerprint,
            EvidenceResultType.FraudRisk => RequiredCheckType.RiskEvaluation,
            _ => throw new InvalidOperationException($"Unsupported evidence result type '{resultType}'."),
        };
}
