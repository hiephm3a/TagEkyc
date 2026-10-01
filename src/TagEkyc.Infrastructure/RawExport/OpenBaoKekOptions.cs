using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

internal record OpenBaoConnectionOptions(
    Uri Address,
    string? Namespace,
    string RoleIdSecretRef,
    string SecretIdSecretRef,
    string? CaCertificatePath,
    TimeSpan RequestTimeout);

internal sealed record OpenBaoKekOptions(
    Uri Address,
    string? Namespace,
    string RoleIdSecretRef,
    string SecretIdSecretRef,
    string? CaCertificatePath,
    string TransitMount,
    string KeyName,
    int KeyVersion,
    string KeyFingerprint,
    TimeSpan RequestTimeout) : OpenBaoConnectionOptions(
        Address,
        Namespace,
        RoleIdSecretRef,
        SecretIdSecretRef,
        CaCertificatePath,
        RequestTimeout)
{
    internal const string SectionName = "TagEkyc:RawExport:AttemptKey:OpenBao";
    internal const string KeyProviderId = "openbao-transit";
    internal const string WrappingSchemeId = "OPENBAO_TRANSIT_AES_GCM";
    internal const int WrappingSchemeVersion = 1;

    internal static OpenBaoKekOptions Resolve(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var addressText = section["Address"]?.Trim();
        if (!Uri.TryCreate(addressText, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("OPENBAO_HTTPS_ADDRESS_INVALID");
        static string Required(IConfigurationSection section, string key, string code) =>
            string.IsNullOrWhiteSpace(section[key]) ? throw new InvalidOperationException(code) : section[key]!.Trim();
        if (!int.TryParse(section["KeyVersion"], out var keyVersion) || keyVersion < 1)
            throw new InvalidOperationException("OPENBAO_TRANSIT_KEY_VERSION_INVALID");
        if (!int.TryParse(section["RequestTimeoutSeconds"], out var timeoutSeconds))
            timeoutSeconds = 15;
        if (timeoutSeconds is < 1 or > 60)
            throw new InvalidOperationException("OPENBAO_REQUEST_TIMEOUT_INVALID");
        var fingerprint = Required(section, "KeyFingerprint", "OPENBAO_TRANSIT_KEY_FINGERPRINT_MISSING");
        if (fingerprint.Length != 64 || fingerprint.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new InvalidOperationException("OPENBAO_TRANSIT_KEY_FINGERPRINT_INVALID");
        return new(
            address,
            string.IsNullOrWhiteSpace(section["Namespace"]) ? null : section["Namespace"]!.Trim(),
            Required(section, "RoleIdSecretRef", "OPENBAO_APPROLE_ROLE_ID_SECRET_REF_MISSING"),
            Required(section, "SecretIdSecretRef", "OPENBAO_APPROLE_SECRET_ID_SECRET_REF_MISSING"),
            string.IsNullOrWhiteSpace(section["CaCertificatePath"]) ? null : section["CaCertificatePath"]!.Trim(),
            Required(section, "TransitMount", "OPENBAO_TRANSIT_MOUNT_MISSING").Trim('/'),
            Required(section, "KeyName", "OPENBAO_TRANSIT_KEY_NAME_MISSING"),
            keyVersion,
            fingerprint,
            TimeSpan.FromSeconds(timeoutSeconds));
    }

    internal KekReference Reference => new(KeyProviderId, KeyName, KeyVersion, KeyFingerprint);
}

internal sealed class OpenBaoKekWrappedMaterialProfileSource : IKekWrappedMaterialProfileSource
{
    internal static readonly KekWrappedMaterialProfile Profile = new(
        KekWrappedMaterialRepresentations.OpaqueProviderCiphertext,
        KekWrappedMaterialRepresentations.Version1,
        OpenBaoKekOptions.WrappingSchemeId,
        OpenBaoKekOptions.WrappingSchemeVersion);

    public KekWrappedMaterialProfile Current => Profile;
}

internal sealed class LegacyKekWrappedMaterialProfileSource : IKekWrappedMaterialProfileSource
{
    internal static readonly KekWrappedMaterialProfile Profile = new(
        KekWrappedMaterialRepresentations.LegacyAesGcmSplit,
        KekWrappedMaterialRepresentations.Version1,
        FixtureDurableKekCatalog.WrappingSuiteId,
        FixtureDurableKekCatalog.WrappingSuiteVersion);

    public KekWrappedMaterialProfile Current => Profile;
}
