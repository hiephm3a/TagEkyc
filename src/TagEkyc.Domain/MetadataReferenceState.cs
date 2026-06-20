namespace TagEkyc.Domain;

public enum MetadataReferenceState
{
    NotRegistered = 0,
    RegisteredMetadata = 1,
    PresentButUnresolved = 2,
    Missing = 3,
    Expired = 4,
    Deleted = 5,
    Inaccessible = 6,
    Unauthorized = 7,
    Quarantined = 8,
    OrphanSuspected = 9,
    InconsistentMetadata = 10,
}

public static class MetadataReferenceStateExtensions
{
    public static bool IsNonSuccess(this MetadataReferenceState state) =>
        state is not MetadataReferenceState.RegisteredMetadata;

    public static bool AllowsEvidenceAvailabilityClaim(this MetadataReferenceState state) => false;

    public static bool AllowsPackageCompletenessClaim(this MetadataReferenceState state) => false;
}
