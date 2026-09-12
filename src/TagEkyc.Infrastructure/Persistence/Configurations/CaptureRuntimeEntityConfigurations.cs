using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class PlatformOperatorCredentialsRowConfiguration : IEntityTypeConfiguration<PlatformOperatorCredentialsRow>
{
    public void Configure(EntityTypeBuilder<PlatformOperatorCredentialsRow> builder)
    {
        builder.ToTable("platform_operator_credentials", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CredentialId);
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.KeyLookupPrefix).HasColumnName("KeyLookupPrefix").HasColumnType("varchar(12)").IsRequired();
        builder.Property(entity => entity.SecretDigest).HasColumnName("SecretDigest").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.VerifierPepperVersion).HasColumnName("VerifierPepperVersion").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.PrincipalId).HasColumnName("PrincipalId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.Scopes).HasColumnName("Scopes").HasColumnType("text[]").IsRequired();
        builder.Property(entity => entity.State).HasColumnName("State").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.IssuedAtUtc).HasColumnName("IssuedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RevocationReason).HasColumnName("RevocationReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureRuntimeRolePolicyRevisionsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRolePolicyRevisionsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRolePolicyRevisionsRow> builder)
    {
        builder.ToTable("capture_runtime_role_policy_revisions", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.CatalogId, entity.Revision });
        builder.Property(entity => entity.CatalogId).HasColumnName("CatalogId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.Roles).HasColumnName("Roles").HasColumnType("text[]").IsRequired();
        builder.Property(entity => entity.EffectiveAtUtc).HasColumnName("EffectiveAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.PublishedByCredentialId).HasColumnName("PublishedByCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.PublishedAtUtc).HasColumnName("PublishedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeRolePolicyHeadsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRolePolicyHeadsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRolePolicyHeadsRow> builder)
    {
        builder.ToTable("capture_runtime_role_policy_heads", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CatalogId);
        builder.Property(entity => entity.CatalogId).HasColumnName("CatalogId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CurrentRevision).HasColumnName("CurrentRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.HeadRevision).HasColumnName("HeadRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("UpdatedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeTrustProfileRevisionsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeTrustProfileRevisionsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeTrustProfileRevisionsRow> builder)
    {
        builder.ToTable("capture_runtime_trust_profile_revisions", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.CatalogId, entity.Revision });
        builder.Property(entity => entity.CatalogId).HasColumnName("CatalogId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RuntimeType).HasColumnName("RuntimeType").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.RetainedRawEnabled).HasColumnName("RetainedRawEnabled").HasColumnType("boolean").IsRequired();
        builder.Property(entity => entity.AllowTrustedEvidence).HasColumnName("AllowTrustedEvidence").HasColumnType("boolean").IsRequired();
        builder.Property(entity => entity.RequireHandoffAttestation).HasColumnName("RequireHandoffAttestation").HasColumnType("boolean").IsRequired();
        builder.Property(entity => entity.EffectiveAtUtc).HasColumnName("EffectiveAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.PublishedByCredentialId).HasColumnName("PublishedByCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.PublishedAtUtc).HasColumnName("PublishedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeTrustProfileHeadsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeTrustProfileHeadsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeTrustProfileHeadsRow> builder)
    {
        builder.ToTable("capture_runtime_trust_profile_heads", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CatalogId);
        builder.Property(entity => entity.CatalogId).HasColumnName("CatalogId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CurrentRevision).HasColumnName("CurrentRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.HeadRevision).HasColumnName("HeadRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("UpdatedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeConfigurationRevisionsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeConfigurationRevisionsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeConfigurationRevisionsRow> builder)
    {
        builder.ToTable("capture_runtime_configuration_revisions", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.CatalogId, entity.Revision });
        builder.Property(entity => entity.CatalogId).HasColumnName("CatalogId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.EffectiveAtUtc).HasColumnName("EffectiveAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.RawExportEnabled).HasColumnName("RawExportEnabled").HasColumnType("boolean").IsRequired();
        builder.Property(entity => entity.PlaintextBudgetSeconds).HasColumnName("PlaintextBudgetSeconds").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.RawExportSourceClaimSafetyMarginMilliseconds).HasColumnName("RawExportSourceClaimSafetyMarginMilliseconds").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.CaptureAgentConfigurationPollingIntervalSeconds).HasColumnName("CaptureAgentConfigurationPollingIntervalSeconds").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.RawExportSourceMaximumChipDg2PortraitBytes).HasColumnName("RawExportSourceMaximumChipDg2PortraitBytes").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.RawExportSourceMaximumLiveSelfieImageBytes).HasColumnName("RawExportSourceMaximumLiveSelfieImageBytes").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.RawExportCaptureMaximumAggregatePlaintextBytesPerHost).HasColumnName("RawExportCaptureMaximumAggregatePlaintextBytesPerHost").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RawExportCustodyMaximumPlaintextWindowBytesPerStream).HasColumnName("RawExportCustodyMaximumPlaintextWindowBytesPerStream").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment).HasColumnName("RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RawExportIngressMaximumPreAdmissionBufferedBytes).HasColumnName("RawExportIngressMaximumPreAdmissionBufferedBytes").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.PublishedByCredentialId).HasColumnName("PublishedByCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.PublishedAtUtc).HasColumnName("PublishedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeConfigurationHeadsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeConfigurationHeadsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeConfigurationHeadsRow> builder)
    {
        builder.ToTable("capture_runtime_configuration_heads", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CatalogId);
        builder.Property(entity => entity.CatalogId).HasColumnName("CatalogId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CurrentRevision).HasColumnName("CurrentRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.HeadRevision).HasColumnName("HeadRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("UpdatedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeRegistrationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRegistrationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRegistrationsRow> builder)
    {
        builder.ToTable("capture_runtime_registrations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CaptureAgentId);
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RuntimeType).HasColumnName("RuntimeType").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.TrustProfileId).HasColumnName("TrustProfileId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.TrustProfileRevision).HasColumnName("TrustProfileRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.ConfigurationId).HasColumnName("ConfigurationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ConfigurationRevision).HasColumnName("ConfigurationRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.ConfigurationOverrideId).HasColumnName("ConfigurationOverrideId").HasColumnType("uuid");
        builder.Property(entity => entity.NextRolePolicyId).HasColumnName("NextRolePolicyId").HasColumnType("uuid");
        builder.Property(entity => entity.NextRolePolicyRevision).HasColumnName("NextRolePolicyRevision").HasColumnType("bigint");
        builder.Property(entity => entity.LifecycleState).HasColumnName("LifecycleState").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.SuspendedAtUtc).HasColumnName("SuspendedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RetiredAtUtc).HasColumnName("RetiredAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.LifecycleReason).HasColumnName("LifecycleReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureRuntimeInstallationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeInstallationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeInstallationsRow> builder)
    {
        builder.ToTable("capture_runtime_installations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.DeviceInstallationId);
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CurrentCredentialId).HasColumnName("CurrentCredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.CurrentCredentialGeneration).HasColumnName("CurrentCredentialGeneration").HasColumnType("bigint");
        builder.Property(entity => entity.LifecycleState).HasColumnName("LifecycleState").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.EnrolledAtUtc).HasColumnName("EnrolledAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.SuspendedAtUtc).HasColumnName("SuspendedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RetiredAtUtc).HasColumnName("RetiredAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.LifecycleReason).HasColumnName("LifecycleReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureRuntimeCredentialGenerationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeCredentialGenerationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeCredentialGenerationsRow> builder)
    {
        builder.ToTable("capture_runtime_credential_generations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.CredentialId, entity.Generation });
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.Generation).HasColumnName("Generation").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CandidateKeyId).HasColumnName("CandidateKeyId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.PublicVerifierSpki).HasColumnName("PublicVerifierSpki").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.Algorithm).HasColumnName("Algorithm").HasColumnType("varchar(64)").IsRequired();
        builder.Property(entity => entity.PublicKeyThumbprint).HasColumnName("PublicKeyThumbprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.RolePolicyId).HasColumnName("RolePolicyId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RolePolicyRevision).HasColumnName("RolePolicyRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.ValidFromUtc).HasColumnName("ValidFromUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ValidUntilUtc).HasColumnName("ValidUntilUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.State).HasColumnName("State").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RotatedAtUtc).HasColumnName("RotatedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RetiredAtUtc).HasColumnName("RetiredAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.TerminalReason).HasColumnName("TerminalReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureRuntimeRequestNoncesRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRequestNoncesRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRequestNoncesRow> builder)
    {
        builder.ToTable("capture_runtime_request_nonces", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.CredentialId, entity.CredentialGeneration, entity.Nonce });
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CredentialGeneration).HasColumnName("CredentialGeneration").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.Nonce).HasColumnName("Nonce").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.SignedTimestampUtc).HasColumnName("SignedTimestampUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.AdmittedAtUtc).HasColumnName("AdmittedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.PurgeAfterUtc).HasColumnName("PurgeAfterUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeBootstrapIssuancesRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeBootstrapIssuancesRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeBootstrapIssuancesRow> builder)
    {
        builder.ToTable("capture_runtime_bootstrap_issuances", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.BootstrapIssuanceId);
        builder.Property(entity => entity.BootstrapIssuanceId).HasColumnName("BootstrapIssuanceId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.KeyLookupPrefix).HasColumnName("KeyLookupPrefix").HasColumnType("varchar(12)").IsRequired();
        builder.Property(entity => entity.SecretDigest).HasColumnName("SecretDigest").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.VerifierPepperVersion).HasColumnName("VerifierPepperVersion").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.RuntimeType).HasColumnName("RuntimeType").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.TrustProfileId).HasColumnName("TrustProfileId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.TrustProfileRevision).HasColumnName("TrustProfileRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RolePolicyId).HasColumnName("RolePolicyId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RolePolicyRevision).HasColumnName("RolePolicyRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.ConfigurationId).HasColumnName("ConfigurationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ConfigurationRevision).HasColumnName("ConfigurationRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.AttestationRequirementDigest).HasColumnName("AttestationRequirementDigest").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.IssueOperationId).HasColumnName("IssueOperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.IssuedByCredentialId).HasColumnName("IssuedByCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.IssuedAtUtc).HasColumnName("IssuedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.State).HasColumnName("State").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RedeemedAtUtc).HasColumnName("RedeemedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid");
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid");
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.CredentialGeneration).HasColumnName("CredentialGeneration").HasColumnType("bigint");
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.ExpiredAtUtc).HasColumnName("ExpiredAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.TerminalReason).HasColumnName("TerminalReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureCapabilitiesRowConfiguration : IEntityTypeConfiguration<CaptureCapabilitiesRow>
{
    public void Configure(EntityTypeBuilder<CaptureCapabilitiesRow> builder)
    {
        builder.ToTable("capture_capabilities", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CaptureCapabilityId);
        builder.Property(entity => entity.CaptureCapabilityId).HasColumnName("CaptureCapabilityId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.VerificationSessionId).HasColumnName("VerificationSessionId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ClientApplicationId).HasColumnName("ClientApplicationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.KeyLookupPrefix).HasColumnName("KeyLookupPrefix").HasColumnType("varchar(12)").IsRequired();
        builder.Property(entity => entity.SecretDigest).HasColumnName("SecretDigest").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.VerifierPepperVersion).HasColumnName("VerifierPepperVersion").HasColumnType("integer").IsRequired();
        builder.Property(entity => entity.Audience).HasColumnName("Audience").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.Challenge).HasColumnName("Challenge").HasColumnType("varchar(128)").IsRequired();
        builder.Property(entity => entity.IssuedAtUtc).HasColumnName("IssuedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.State).HasColumnName("State").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.PredecessorCapabilityId).HasColumnName("PredecessorCapabilityId").HasColumnType("uuid");
        builder.Property(entity => entity.SuccessorCapabilityId).HasColumnName("SuccessorCapabilityId").HasColumnType("uuid");
        builder.Property(entity => entity.BoundAtUtc).HasColumnName("BoundAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.ExpiredAtUtc).HasColumnName("ExpiredAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.TerminalReason).HasColumnName("TerminalReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureExecutionBindingsRowConfiguration : IEntityTypeConfiguration<CaptureExecutionBindingsRow>
{
    public void Configure(EntityTypeBuilder<CaptureExecutionBindingsRow> builder)
    {
        builder.ToTable("capture_execution_bindings", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.CaptureExecutionBindingId);
        builder.Property(entity => entity.CaptureExecutionBindingId).HasColumnName("CaptureExecutionBindingId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.VerificationSessionId).HasColumnName("VerificationSessionId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CaptureCapabilityId).HasColumnName("CaptureCapabilityId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ClientApplicationId).HasColumnName("ClientApplicationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CredentialGeneration).HasColumnName("CredentialGeneration").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.PublicKeyThumbprint).HasColumnName("PublicKeyThumbprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.RuntimeRevision).HasColumnName("RuntimeRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.InstallationRevision).HasColumnName("InstallationRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.CredentialRevision).HasColumnName("CredentialRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.TrustProfileId).HasColumnName("TrustProfileId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.TrustProfileRevision).HasColumnName("TrustProfileRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RolePolicyId).HasColumnName("RolePolicyId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RolePolicyRevision).HasColumnName("RolePolicyRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.ConfigurationId).HasColumnName("ConfigurationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ConfigurationRevision).HasColumnName("ConfigurationRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.Challenge).HasColumnName("Challenge").HasColumnType("varchar(128)").IsRequired();
        builder.Property(entity => entity.BindOperationId).HasColumnName("BindOperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BoundAtUtc).HasColumnName("BoundAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExecutionExpiresAtUtc).HasColumnName("ExecutionExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeConfigurationOverridesRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeConfigurationOverridesRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeConfigurationOverridesRow> builder)
    {
        builder.ToTable("capture_runtime_configuration_overrides", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.ConfigurationOverrideId);
        builder.Property(entity => entity.ConfigurationOverrideId).HasColumnName("ConfigurationOverrideId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BaseConfigurationId).HasColumnName("BaseConfigurationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BaseConfigurationRevision).HasColumnName("BaseConfigurationRevision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.RawExportEnabled).HasColumnName("RawExportEnabled").HasColumnType("boolean");
        builder.Property(entity => entity.PlaintextBudgetSeconds).HasColumnName("PlaintextBudgetSeconds").HasColumnType("integer");
        builder.Property(entity => entity.RawExportSourceClaimSafetyMarginMilliseconds).HasColumnName("RawExportSourceClaimSafetyMarginMilliseconds").HasColumnType("integer");
        builder.Property(entity => entity.CaptureAgentConfigurationPollingIntervalSeconds).HasColumnName("CaptureAgentConfigurationPollingIntervalSeconds").HasColumnType("integer");
        builder.Property(entity => entity.RawExportSourceMaximumChipDg2PortraitBytes).HasColumnName("RawExportSourceMaximumChipDg2PortraitBytes").HasColumnType("integer");
        builder.Property(entity => entity.RawExportSourceMaximumLiveSelfieImageBytes).HasColumnName("RawExportSourceMaximumLiveSelfieImageBytes").HasColumnType("integer");
        builder.Property(entity => entity.RawExportCaptureMaximumAggregatePlaintextBytesPerHost).HasColumnName("RawExportCaptureMaximumAggregatePlaintextBytesPerHost").HasColumnType("bigint");
        builder.Property(entity => entity.RawExportCustodyMaximumPlaintextWindowBytesPerStream).HasColumnName("RawExportCustodyMaximumPlaintextWindowBytesPerStream").HasColumnType("integer");
        builder.Property(entity => entity.RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment).HasColumnName("RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment").HasColumnType("bigint");
        builder.Property(entity => entity.RawExportIngressMaximumPreAdmissionBufferedBytes).HasColumnName("RawExportIngressMaximumPreAdmissionBufferedBytes").HasColumnType("integer");
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("UpdatedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeRotationAuthorizationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRotationAuthorizationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRotationAuthorizationsRow> builder)
    {
        builder.ToTable("capture_runtime_rotation_authorizations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.RotationAuthorizationId);
        builder.Property(entity => entity.RotationAuthorizationId).HasColumnName("RotationAuthorizationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CurrentGeneration).HasColumnName("CurrentGeneration").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.AuthorizeOperationId).HasColumnName("AuthorizeOperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.AuthorizedByCredentialId).HasColumnName("AuthorizedByCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.AuthorizedAtUtc).HasColumnName("AuthorizedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc).HasColumnName("ExpiresAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.State).HasColumnName("State").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.CandidateKeyId).HasColumnName("CandidateKeyId").HasColumnType("uuid");
        builder.Property(entity => entity.SuccessorPublicVerifierSpki).HasColumnName("SuccessorPublicVerifierSpki").HasColumnType("bytea");
        builder.Property(entity => entity.SuccessorPublicKeyThumbprint).HasColumnName("SuccessorPublicKeyThumbprint").HasColumnType("bytea");
        builder.Property(entity => entity.SuccessorGeneration).HasColumnName("SuccessorGeneration").HasColumnType("bigint");
        builder.Property(entity => entity.CompletedAtUtc).HasColumnName("CompletedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.RevokedAtUtc).HasColumnName("RevokedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.ExpiredAtUtc).HasColumnName("ExpiredAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.TerminalReason).HasColumnName("TerminalReason").HasColumnType("varchar(64)");
    }
}

public sealed class CaptureRuntimeManagementOperationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeManagementOperationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeManagementOperationsRow> builder)
    {
        builder.ToTable("capture_runtime_management_operations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.ActorCredentialId, entity.OperationKind, entity.IdempotencyKey });
        builder.Property(entity => entity.ActorCredentialId).HasColumnName("ActorCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.OperationKind).HasColumnName("OperationKind").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasColumnName("IdempotencyKey").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.TargetKind).HasColumnName("TargetKind").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.TargetId).HasColumnName("TargetId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ResultCode).HasColumnName("ResultCode").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.ResultRevision).HasColumnName("ResultRevision").HasColumnType("bigint");
        builder.Property(entity => entity.ResultId).HasColumnName("ResultId").HasColumnType("uuid");
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.CompletedAtUtc).HasColumnName("CompletedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeManagementEventsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeManagementEventsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeManagementEventsRow> builder)
    {
        builder.ToTable("capture_runtime_management_events", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.EventId);
        builder.Property(entity => entity.EventId).HasColumnName("EventId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ActorCredentialId).HasColumnName("ActorCredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.OperationKind).HasColumnName("OperationKind").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasColumnName("IdempotencyKey").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.EventType).HasColumnName("EventType").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.TargetKind).HasColumnName("TargetKind").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.TargetId).HasColumnName("TargetId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BeforeRevision).HasColumnName("BeforeRevision").HasColumnType("bigint");
        builder.Property(entity => entity.AfterRevision).HasColumnName("AfterRevision").HasColumnType("bigint");
        builder.Property(entity => entity.Reason).HasColumnName("Reason").HasColumnType("varchar(64)");
        builder.Property(entity => entity.RecordedAtUtc).HasColumnName("RecordedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureCapabilityOperationsRowConfiguration : IEntityTypeConfiguration<CaptureCapabilityOperationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureCapabilityOperationsRow> builder)
    {
        builder.ToTable("capture_capability_operations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.ClientApplicationId, entity.VerificationSessionId, entity.OperationKind, entity.IdempotencyKey });
        builder.Property(entity => entity.ClientApplicationId).HasColumnName("ClientApplicationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.VerificationSessionId).HasColumnName("VerificationSessionId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.OperationKind).HasColumnName("OperationKind").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasColumnName("IdempotencyKey").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.RuntimeCaptureAgentId).HasColumnName("RuntimeCaptureAgentId").HasColumnType("uuid");
        builder.Property(entity => entity.RuntimeInstallationId).HasColumnName("RuntimeInstallationId").HasColumnType("uuid");
        builder.Property(entity => entity.RuntimeCredentialId).HasColumnName("RuntimeCredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.RuntimeCredentialGeneration).HasColumnName("RuntimeCredentialGeneration").HasColumnType("bigint");
        builder.Property(entity => entity.ResultCode).HasColumnName("ResultCode").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.ResultCapabilityId).HasColumnName("ResultCapabilityId").HasColumnType("uuid");
        builder.Property(entity => entity.ResultBindingId).HasColumnName("ResultBindingId").HasColumnType("uuid");
        builder.Property(entity => entity.ResultRevision).HasColumnName("ResultRevision").HasColumnType("bigint");
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.CompletedAtUtc).HasColumnName("CompletedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureCapabilityEventsRowConfiguration : IEntityTypeConfiguration<CaptureCapabilityEventsRow>
{
    public void Configure(EntityTypeBuilder<CaptureCapabilityEventsRow> builder)
    {
        builder.ToTable("capture_capability_events", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.EventId);
        builder.Property(entity => entity.EventId).HasColumnName("EventId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ClientApplicationId).HasColumnName("ClientApplicationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.VerificationSessionId).HasColumnName("VerificationSessionId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.OperationKind).HasColumnName("OperationKind").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasColumnName("IdempotencyKey").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CaptureCapabilityId).HasColumnName("CaptureCapabilityId").HasColumnType("uuid");
        builder.Property(entity => entity.EventType).HasColumnName("EventType").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.RuntimeCaptureAgentId).HasColumnName("RuntimeCaptureAgentId").HasColumnType("uuid");
        builder.Property(entity => entity.RuntimeInstallationId).HasColumnName("RuntimeInstallationId").HasColumnType("uuid");
        builder.Property(entity => entity.BeforeRevision).HasColumnName("BeforeRevision").HasColumnType("bigint");
        builder.Property(entity => entity.AfterRevision).HasColumnName("AfterRevision").HasColumnType("bigint");
        builder.Property(entity => entity.RecordedAtUtc).HasColumnName("RecordedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeBootstrapRedemptionOperationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeBootstrapRedemptionOperationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeBootstrapRedemptionOperationsRow> builder)
    {
        builder.ToTable("capture_runtime_bootstrap_redemption_operations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.BootstrapIssuanceId, entity.RedeemOperationId });
        builder.Property(entity => entity.BootstrapIssuanceId).HasColumnName("BootstrapIssuanceId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RedeemOperationId).HasColumnName("RedeemOperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.CandidateKeyId).HasColumnName("CandidateKeyId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ResultCode).HasColumnName("ResultCode").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid");
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid");
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.Generation).HasColumnName("Generation").HasColumnType("bigint");
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.CompletedAtUtc).HasColumnName("CompletedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeRotationCompletionOperationsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRotationCompletionOperationsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRotationCompletionOperationsRow> builder)
    {
        builder.ToTable("capture_runtime_rotation_completion_operations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => new { entity.RotationAuthorizationId, entity.BusinessIdempotencyKey });
        builder.Property(entity => entity.RotationAuthorizationId).HasColumnName("RotationAuthorizationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BusinessIdempotencyKey).HasColumnName("BusinessIdempotencyKey").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.PredecessorGeneration).HasColumnName("PredecessorGeneration").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.SuccessorGeneration).HasColumnName("SuccessorGeneration").HasColumnType("bigint");
        builder.Property(entity => entity.CandidateKeyId).HasColumnName("CandidateKeyId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.ResultCode).HasColumnName("ResultCode").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.CredentialRevision).HasColumnName("CredentialRevision").HasColumnType("bigint");
        builder.Property(entity => entity.InstallationRevision).HasColumnName("InstallationRevision").HasColumnType("bigint");
        builder.Property(entity => entity.RotationRevision).HasColumnName("RotationRevision").HasColumnType("bigint");
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.CompletedAtUtc).HasColumnName("CompletedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeBootstrapRedemptionEventsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeBootstrapRedemptionEventsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeBootstrapRedemptionEventsRow> builder)
    {
        builder.ToTable("capture_runtime_bootstrap_redemption_events", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.EventId);
        builder.Property(entity => entity.EventId).HasColumnName("EventId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BootstrapIssuanceId).HasColumnName("BootstrapIssuanceId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RedeemOperationId).HasColumnName("RedeemOperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.EventType).HasColumnName("EventType").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.CaptureAgentId).HasColumnName("CaptureAgentId").HasColumnType("uuid");
        builder.Property(entity => entity.DeviceInstallationId).HasColumnName("DeviceInstallationId").HasColumnType("uuid");
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.Generation).HasColumnName("Generation").HasColumnType("bigint");
        builder.Property(entity => entity.RecordedAtUtc).HasColumnName("RecordedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeRotationCompletionEventsRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeRotationCompletionEventsRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeRotationCompletionEventsRow> builder)
    {
        builder.ToTable("capture_runtime_rotation_completion_events", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.EventId);
        builder.Property(entity => entity.EventId).HasColumnName("EventId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.RotationAuthorizationId).HasColumnName("RotationAuthorizationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.BusinessIdempotencyKey).HasColumnName("BusinessIdempotencyKey").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.EventType).HasColumnName("EventType").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.CredentialId).HasColumnName("CredentialId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.PredecessorGeneration).HasColumnName("PredecessorGeneration").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.SuccessorGeneration).HasColumnName("SuccessorGeneration").HasColumnType("bigint");
        builder.Property(entity => entity.RecordedAtUtc).HasColumnName("RecordedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class PlatformOperatorRootOperationsRowConfiguration : IEntityTypeConfiguration<PlatformOperatorRootOperationsRow>
{
    public void Configure(EntityTypeBuilder<PlatformOperatorRootOperationsRow> builder)
    {
        builder.ToTable("platform_operator_root_operations", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.OperationId);
        builder.Property(entity => entity.OperationId).HasColumnName("OperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.OperationKind).HasColumnName("OperationKind").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.RequestFingerprint).HasColumnName("RequestFingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(entity => entity.OperatorPrincipalId).HasColumnName("OperatorPrincipalId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.TargetCredentialId).HasColumnName("TargetCredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.ResultCode).HasColumnName("ResultCode").HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.ResultRevision).HasColumnName("ResultRevision").HasColumnType("bigint");
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("CreatedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.CompletedAtUtc).HasColumnName("CompletedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class PlatformOperatorRootEventsRowConfiguration : IEntityTypeConfiguration<PlatformOperatorRootEventsRow>
{
    public void Configure(EntityTypeBuilder<PlatformOperatorRootEventsRow> builder)
    {
        builder.ToTable("platform_operator_root_events", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.EventId);
        builder.Property(entity => entity.EventId).HasColumnName("EventId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.OperationId).HasColumnName("OperationId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.EventType).HasColumnName("EventType").HasColumnType("varchar(24)").IsRequired();
        builder.Property(entity => entity.OperatorPrincipalId).HasColumnName("OperatorPrincipalId").HasColumnType("uuid").IsRequired();
        builder.Property(entity => entity.TargetCredentialId).HasColumnName("TargetCredentialId").HasColumnType("uuid");
        builder.Property(entity => entity.BeforeRevision).HasColumnName("BeforeRevision").HasColumnType("bigint");
        builder.Property(entity => entity.AfterRevision).HasColumnName("AfterRevision").HasColumnType("bigint");
        builder.Property(entity => entity.RecordedAtUtc).HasColumnName("RecordedAtUtc").HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class CaptureRuntimeCutoverStateRowConfiguration : IEntityTypeConfiguration<CaptureRuntimeCutoverStateRow>
{
    public void Configure(EntityTypeBuilder<CaptureRuntimeCutoverStateRow> builder)
    {
        builder.ToTable("capture_runtime_cutover_state", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(entity => entity.Profile);
        builder.Property(entity => entity.Profile).HasColumnName("Profile").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.State).HasColumnName("State").HasColumnType("varchar(16)").IsRequired();
        builder.Property(entity => entity.Revision).HasColumnName("Revision").HasColumnType("bigint").IsRequired();
        builder.Property(entity => entity.PreparedAtUtc).HasColumnName("PreparedAtUtc").HasColumnType("timestamptz").IsRequired();
        builder.Property(entity => entity.ActivatedAtUtc).HasColumnName("ActivatedAtUtc").HasColumnType("timestamptz");
        builder.Property(entity => entity.ActivatedByCredentialId).HasColumnName("ActivatedByCredentialId").HasColumnType("uuid");
    }
}

