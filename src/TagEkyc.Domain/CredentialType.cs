namespace TagEkyc.Domain;

public enum CredentialType
{
    Unknown = 0,
    ManagedApiKey = 1,
    OAuthClient = 2,
    Certificate = 3,
    MTlsCertificate = 4,
    ServicePrincipal = 5,
    ExternalProviderCredential = 6,
}
