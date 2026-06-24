namespace TagEkyc.Infrastructure.Signing;

public sealed class EvidenceSigningOptions
{
    public const string SectionName = "TagEkyc:EvidenceSigning";

    public string? Backend { get; init; }

    public bool RequireHardwareSigner { get; init; }

    public bool IsLocalDevBackend =>
        string.Equals(Backend, EvidenceSigningBackends.LocalDev, StringComparison.Ordinal);

    public bool IsPkcs11Backend =>
        string.Equals(Backend, EvidenceSigningBackends.Pkcs11, StringComparison.Ordinal);
}

public static class EvidenceSigningBackends
{
    public const string LocalDev = "LocalDev";
    public const string Pkcs11 = "Pkcs11";
}
