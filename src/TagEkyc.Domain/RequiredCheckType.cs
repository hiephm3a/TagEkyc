namespace TagEkyc.Domain;

public enum RequiredCheckType
{
    CaptureQuality = 0,
    DocumentOcr = 1,
    DocumentNfc = 2,
    FaceMatch = 3,
    Liveness = 4,
    Fingerprint = 5,
    RiskEvaluation = 6,
}
