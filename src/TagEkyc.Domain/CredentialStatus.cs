namespace TagEkyc.Domain;

public enum CredentialStatus
{
    Unknown = 0,
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Revoked = 4,
    Expired = 5,
    RotatedReplaced = 6,
}
