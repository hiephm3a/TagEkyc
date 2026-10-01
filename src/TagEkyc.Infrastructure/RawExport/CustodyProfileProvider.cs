namespace TagEkyc.Infrastructure.RawExport;

internal sealed record SourceEncryptionProfileBundle(
    string StorageProfileId,
    string SourceEncryptionProfileId,
    int SourceEncryptionProfileVersion,
    string EncryptionSuiteId,
    int EncryptionFramingVersion,
    string NonceStrategyId,
    int ChunkSize);

internal sealed record KekReferenceBundle(
    string KeyProviderId,
    string KekId,
    int KekVersion,
    string KekFingerprint);

internal sealed record CustodyTimeBounds(
    TimeSpan SafetyMargin,
    TimeSpan MaxRemainingContinuationWindow,
    TimeSpan EncryptionAttemptDeadline,
    TimeSpan OwnershipLeaseDuration);

internal interface ICustodyProfileProvider
{
    SourceEncryptionProfileBundle ActiveSourceEncryptionProfile { get; }

    KekReferenceBundle ActiveKekReference { get; }

    CustodyTimeBounds TimeBounds { get; }
}

internal sealed class FixtureSourceEncryptionProfileCatalog
{
    internal const string StorageProfileId = "fixture-storage-local-v1";
    internal const string SourceEncryptionProfileId =
        "fixture-source-encryption-v1";
    internal const int SourceEncryptionProfileVersion = 1;
    internal const string EncryptionSuiteId =
        "fixture-aead-aes256gcm-v1";
    internal const int EncryptionFramingVersion = 1;
    internal const string NonceStrategyId =
        "fixture-nonce-random96-v1";
    internal const int ChunkSize = 1_048_576;

    public SourceEncryptionProfileBundle GetActive() =>
        new(
            StorageProfileId,
            SourceEncryptionProfileId,
            SourceEncryptionProfileVersion,
            EncryptionSuiteId,
            EncryptionFramingVersion,
            NonceStrategyId,
            ChunkSize);
}

internal sealed class FixtureKekReferenceCatalog
{
    internal const string KeyProviderId = "fixture-kek-provider-v1";
    internal const string KekId = "fixture-kek-v1";
    internal const int KekVersion = 1;

    // SHA-256("tagekyc-tip88c1-fixture-kek-reference-v1").
    // This is a public, non-secret fixture reference, not key material.
    internal const string KekFingerprint =
        "f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2";

    public KekReferenceBundle GetActive() =>
        new(
            KeyProviderId,
            KekId,
            KekVersion,
            KekFingerprint);
}

internal sealed class FixtureCustodyProfileProvider(
    FixtureSourceEncryptionProfileCatalog sourceEncryptionProfiles,
    FixtureKekReferenceCatalog kekReferences,
    CustodyTimeBoundsState timeBoundsState) : ICustodyProfileProvider
{
    public SourceEncryptionProfileBundle ActiveSourceEncryptionProfile { get; } =
        sourceEncryptionProfiles.GetActive();

    public KekReferenceBundle ActiveKekReference { get; } =
        kekReferences.GetActive();

    public CustodyTimeBounds TimeBounds =>
        timeBoundsState.Value
        ?? throw new RawExportCustodyProfileReadinessException(
            RawExportCustodyProfileReadinessValidator.TimeBoundsInvalid);
}

internal sealed class OpenBaoProductionCustodyProfileProvider : ICustodyProfileProvider
{
    public OpenBaoProductionCustodyProfileProvider(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        OpenBaoKekOptions openBao,
        CustodyTimeBoundsState timeBoundsState)
    {
        var section = configuration.GetSection("TagEkyc:RawExport:CustodyProfile");
        static string Required(Microsoft.Extensions.Configuration.IConfigurationSection section,string key) =>
            string.IsNullOrWhiteSpace(section[key]) ? throw new RawExportCustodyProfileReadinessException(RawExportCustodyProfileReadinessValidator.ProfileInvalid) : section[key]!.Trim();
        if(!int.TryParse(section["SourceEncryptionProfileVersion"],out var profileVersion) || profileVersion<1
           || !int.TryParse(section["EncryptionFramingVersion"],out var framingVersion) || framingVersion<1
           || !int.TryParse(section["ChunkSize"],out var chunkSize) || chunkSize<1)
            throw new RawExportCustodyProfileReadinessException(RawExportCustodyProfileReadinessValidator.ProfileInvalid);
        ActiveSourceEncryptionProfile=new(
            Required(section,"StorageProfileId"),Required(section,"SourceEncryptionProfileId"),profileVersion,
            Required(section,"EncryptionSuiteId"),framingVersion,Required(section,"NonceStrategyId"),chunkSize);
        ActiveKekReference=new(openBao.Reference.KeyProviderId,openBao.Reference.KekId,openBao.Reference.KekVersion,openBao.Reference.KekFingerprint);
        TimeBounds=timeBoundsState.Value ?? throw new RawExportCustodyProfileReadinessException(RawExportCustodyProfileReadinessValidator.TimeBoundsInvalid);
    }

    public SourceEncryptionProfileBundle ActiveSourceEncryptionProfile { get; }
    public KekReferenceBundle ActiveKekReference { get; }
    public CustodyTimeBounds TimeBounds { get; }
}
