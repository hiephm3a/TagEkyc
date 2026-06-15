using System.Reflection;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip17DurableMetadataRepositoryBoundaryTests
{
    [Theory]
    [InlineData("credref:business-consumer-primary")]
    [InlineData("credential-id:bc-001")]
    [InlineData("provider-ref:opaque-credential-001")]
    public void Credential_ref_accepts_safe_reference_metadata(string value)
    {
        var credentialRef = new CredentialRef(value);

        Assert.Equal(value, credentialRef.Value);
    }

    [Theory]
    [InlineData("raw:localdev-api-key")]
    [InlineData("secret:client-secret")]
    [InlineData("hashed:api-key-hash")]
    [InlineData("hash:credential-hash")]
    [InlineData("sha256:credential-digest")]
    [InlineData("token:bearer-value")]
    [InlineData("bearer:jwt")]
    [InlineData("password:value")]
    [InlineData("private-key:value")]
    [InlineData("client-secret:value")]
    [InlineData("api-key:value")]
    public void Credential_ref_rejects_raw_or_hashed_secret_shaped_values(string value)
    {
        Assert.Throws<ArgumentException>(() => new CredentialRef(value));
    }

    [Fact]
    public void Durable_metadata_value_object_to_string_values_are_redacted()
    {
        var principalId = new PrincipalId("principal-business-001");
        var credentialRef = new CredentialRef("credential-id:bc-primary");
        var scopeGrantSetId = new ScopeGrantSetId("scope-grant-business-s1");

        Assert.Equal("[principal-id]", principalId.ToString());
        Assert.Equal("[credential-ref]", credentialRef.ToString());
        Assert.Equal("[scope-grant-set-id]", scopeGrantSetId.ToString());
        Assert.DoesNotContain(principalId.Value, principalId.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(credentialRef.Value, credentialRef.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(scopeGrantSetId.Value, scopeGrantSetId.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Durable_metadata_contracts_use_safe_credential_reference_not_secret_storage()
    {
        var contractTypes = Tip17ContractTypes();
        var forbiddenNameFragments = new[]
        {
            "Secret",
            "RawSecret",
            "HashedSecret",
            "ApiKeyValue",
            "ApiKeyHash",
            "Token",
            "Bearer",
            "Password",
            "PrivateKey",
        };

        var publicProperties = contractTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .ToArray();

        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(DurableActorCredentialMetadata.CredentialRef) &&
                property.PropertyType == typeof(CredentialRef));
        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(DurableActorCredentialMetadata.ActorCategory) &&
                property.PropertyType == typeof(AuthenticatedCallerCategory));
        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(DurableAuditIdentityMetadata.ActorCategory) &&
                property.PropertyType == typeof(AuthenticatedCallerCategory));
        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(DurableCompletionAuthorityMetadata.RequestedByActorCategory) &&
                property.PropertyType == typeof(AuthenticatedCallerCategory));
        Assert.Contains(
            publicProperties,
            property => property.Name == nameof(DurableCompletionAuthorityMetadata.FinalizedByActorCategory) &&
                property.PropertyType == typeof(AuthenticatedCallerCategory));
        Assert.DoesNotContain(
            publicProperties,
            property => forbiddenNameFragments.Any(fragment =>
                property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Durable_metadata_contracts_do_not_promote_localdev_api_keys_to_credentials()
    {
        var publicProperties = Tip17ContractTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .ToArray();

        Assert.DoesNotContain(
            publicProperties,
            property => property.PropertyType.Namespace?.Contains("LocalDev", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(
            publicProperties,
            property => property.PropertyType.Name.Contains("ApiKeyMetadata", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            publicProperties,
            property => property.PropertyType.Name.Contains("LocalDevApiKey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Durable_metadata_write_set_models_safe_metadata_references_only()
    {
        var actorCredential = new DurableActorCredentialMetadata(
            new PrincipalId("principal-business-001"),
            AuthenticatedCallerCategory.BusinessConsumer,
            new CredentialRef("credential-id:bc-primary"),
            CredentialType.ManagedApiKey,
            CredentialStatus.Active,
            new ScopeGrantSetId("scope-grant-business-s1"));
        var session = new DurableSessionMetadata(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TenantId: null,
            VerificationSessionState.Completed,
            new PolicySnapshotId("policy-catalog-ref:s1"),
            actorCredential,
            RetentionClass.RegulatedEvidence,
            LegalHoldStatus.None,
            DeletionEligibility.NotEvaluated,
            PurgeBlockReason.None,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow);
        var audit = new DurableAuditIdentityMetadata(
            session.VerificationSessionId,
            session.ClientApplicationId,
            actorCredential.PrincipalId,
            actorCredential.ActorCategory,
            actorCredential.CredentialRef,
            actorCredential.CredentialType,
            actorCredential.ScopeGrantSetId,
            "verification.complete",
            "Allowed",
            ReasonCode: null,
            "req-tip17",
            "corr-tip17",
            DateTimeOffset.UtcNow);

        var writeSet = new DurableMetadataWriteSet(
            session,
            audit,
            EvidencePackage: null,
            CompletionAuthority: null);

        Assert.Equal(actorCredential.PrincipalId, writeSet.Session.ActorCredential.PrincipalId);
        Assert.Equal(actorCredential.CredentialRef, writeSet.AuditIdentity.CredentialRef);
        Assert.Equal(DeletionEligibility.NotEvaluated, writeSet.Session.DeletionEligibility);
        Assert.Null(writeSet.EvidencePackage);
        Assert.Null(writeSet.CompletionAuthority);
    }

    private static Type[] Tip17ContractTypes() =>
    [
        typeof(DurableActorCredentialMetadata),
        typeof(DurableSessionMetadata),
        typeof(DurableAuditIdentityMetadata),
        typeof(DurableEvidencePackageMetadata),
        typeof(DurableCompletionAuthorityMetadata),
        typeof(DurableMetadataWriteSet),
    ];
}
