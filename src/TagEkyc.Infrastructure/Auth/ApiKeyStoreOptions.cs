namespace TagEkyc.Infrastructure.Auth;

public sealed class ApiKeyStoreOptions
{
    public const string SectionName = "TagEkyc:ApiKeyStore";

    public string Backend { get; set; } = ApiKeyStoreBackends.LocalDev;

    public string? PepperSecretRef { get; set; }

    public string? Pepper { get; set; }
}

public static class ApiKeyStoreBackends
{
    public const string LocalDev = "LocalDev";

    public const string Postgres = "Postgres";
}

public sealed record ApiKeyStorePepper(byte[] Value);

public sealed record ApiKeyStorePepperSecretRef(string? Value);

public static class ManagedApiKeyConstants
{
    public const string CredentialType = "ManagedApiKey";

    public const int KeyPrefixLength = 16;

    public const int MinimumSecretLength = 43;

    public const int HashLength = 32;
}
