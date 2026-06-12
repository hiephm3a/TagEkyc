namespace TagEkyc.Domain;

public enum PurgeBlockReason
{
    None = 0,
    LegalHold = 1,
    RetentionPolicy = 2,
    ActiveSession = 3,
}
