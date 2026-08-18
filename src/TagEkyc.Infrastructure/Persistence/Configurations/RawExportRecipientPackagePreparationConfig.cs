using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportRecipientPackagePreparationConfig : IEntityTypeConfiguration<RawExportRecipientPackagePreparationRow>
{
    public void Configure(EntityTypeBuilder<RawExportRecipientPackagePreparationRow> entity)
    {
        entity.ToTable("raw_export_recipient_package_preparations", table =>
        {
            table.HasCheckConstraint("ck_raw_export_recipient_package_shape", "\"C2PreparationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PackageId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND octet_length(\"PackageEqualityFingerprint\") = 32 AND octet_length(\"AssemblyFingerprint\") = 32 AND octet_length(\"ManifestDigest\") = 32 AND octet_length(\"AssemblyDigest\") = 32 AND octet_length(\"AssemblyAuthenticationValue\") = 32 AND \"CompleteAssemblyLength\" BETWEEN 1 AND 33554432 AND \"RecipientKeyId\" ~ '^[A-Za-z0-9._:-]{1,128}$' AND \"RecipientKeyVersion\" > 0 AND octet_length(\"RecipientKeyFingerprint\") = 32 AND octet_length(\"RecipientPublicKeySpki\") BETWEEN 384 AND 1024 AND \"RecipientKeyRevision\" > 0 AND \"RecipientKeyValidFromUtc\" < \"RecipientKeyValidUntilUtc\" AND \"PackageProfile\" = 'tip-88c1-c2-package-profile-v1' AND octet_length(\"ProviderOperationTokenDigest\") = 32 AND \"ProviderKind\" = 's3-compatible-single-part-v1' AND octet_length(\"ProviderEndpointFingerprint\") = 32 AND \"ObjectKey\" ~ '^raw-export/c2-package/v1/[0-9a-f]{32}$' AND octet_length(\"ObjectBindingDigest\") = 32 AND \"State\" IN ('Reserved','PutInFlight','PutOutcomeUnknown','Prepared','Finalized','AbortAuthorized','CleanupPending','Aborted','Quarantined') AND \"Revision\" > 0");
            table.HasCheckConstraint("ck_raw_export_recipient_package_sparse", "(\"EnvelopeDigest\" IS NULL OR octet_length(\"EnvelopeDigest\") = 32) AND (\"PackageCiphertextDigest\" IS NULL OR octet_length(\"PackageCiphertextDigest\") = 32) AND (\"ConditionalCreateEvidenceDigest\" IS NULL OR octet_length(\"ConditionalCreateEvidenceDigest\") = 32) AND (\"ProviderReceiptDigest\" IS NULL OR octet_length(\"ProviderReceiptDigest\") = 32) AND (\"AbortAuthorizationDigest\" IS NULL OR octet_length(\"AbortAuthorizationDigest\") = 32) AND (\"PositiveAbsenceEvidenceDigest\" IS NULL OR octet_length(\"PositiveAbsenceEvidenceDigest\") = 32) AND (\"CleanupProgressEvidenceDigest\" IS NULL OR octet_length(\"CleanupProgressEvidenceDigest\") = 32) AND (\"QuarantineEvidenceDigest\" IS NULL OR octet_length(\"QuarantineEvidenceDigest\") = 32) AND ((\"State\" = 'Reserved' AND \"EnvelopeDigest\" IS NULL AND \"EncryptedPackageLength\" IS NULL AND \"PackageCiphertextDigest\" IS NULL AND \"PutStartedAtUtc\" IS NULL AND \"PreparedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" IN ('PutInFlight','PutOutcomeUnknown') AND \"EnvelopeDigest\" IS NOT NULL AND \"EncryptedPackageLength\" BETWEEN 1 AND 33557106 AND \"PackageCiphertextDigest\" IS NOT NULL AND \"PutStartedAtUtc\" IS NOT NULL AND \"PreparedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortedAtUtc\" IS NULL AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" IN ('Prepared','Finalized') AND \"EnvelopeDigest\" IS NOT NULL AND \"EncryptedPackageLength\" BETWEEN 1 AND 33557106 AND \"PackageCiphertextDigest\" IS NOT NULL AND \"ConditionalCreateEvidenceDigest\" IS NOT NULL AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PutStartedAtUtc\" IS NOT NULL AND \"PreparedAtUtc\" IS NOT NULL AND (\"State\" = 'Prepared' AND \"FinalizedAtUtc\" IS NULL OR \"State\" = 'Finalized' AND \"FinalizedAtUtc\" IS NOT NULL) AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortedAtUtc\" IS NULL AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" IN ('AbortAuthorized','CleanupPending','Aborted') AND \"AbortAuthorizationDigest\" IS NOT NULL AND \"AbortAuthorizedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NULL AND (\"State\" <> 'Aborted' OR \"AbortedAtUtc\" IS NOT NULL) AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" = 'Quarantined' AND \"QuarantineEvidenceDigest\" IS NOT NULL AND \"QuarantinedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL))");
        });
        entity.HasKey(row => row.C2PreparationId).HasName("pk_raw_export_recipient_package_preparation");
        entity.HasAlternateKey(row => row.PackageId).HasName("uq_raw_export_recipient_package_package");
        entity.HasAlternateKey(row => row.PackageEqualityFingerprint).HasName("uq_raw_export_recipient_package_equality");
        entity.HasAlternateKey(row => row.AssemblyId).HasName("uq_raw_export_recipient_package_assembly");
        entity.HasAlternateKey(row => row.ProviderOperationTokenDigest).HasName("uq_raw_export_recipient_package_operation");
        entity.HasIndex(row => new { row.JobId, row.AttemptId, row.FencingToken })
            .HasDatabaseName("ix_raw_export_recipient_package_attempt_fence");
        entity.Property(row => row.RecipientKeyId).HasMaxLength(128).IsRequired();
        entity.Property(row => row.PackageProfile).HasMaxLength(64).IsRequired();
        entity.Property(row => row.ProviderKind).HasMaxLength(64).IsRequired();
        entity.Property(row => row.ProviderConfigurationId).HasMaxLength(128).IsRequired();
        entity.Property(row => row.BucketName).HasMaxLength(63).IsRequired();
        entity.Property(row => row.ObjectKey).HasMaxLength(128).IsRequired();
        entity.Property(row => row.State).HasMaxLength(32).IsRequired();
        entity.HasOne<RawExportRecipientKeyRegistrationRow>().WithMany()
            .HasForeignKey(row => new { row.RecipientClientApplicationId, row.RecipientKeyId, row.RecipientKeyVersion })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_recipient_package_key");
        entity.HasOne<RawExportJobIdentityRow>().WithMany().HasForeignKey(row => row.JobId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_recipient_package_job");
        entity.HasOne<RawExportJobAttemptRow>().WithMany()
            .HasForeignKey(row => new { row.JobId, row.AttemptId, row.FencingToken })
            .HasPrincipalKey(row => new { row.JobId, row.AttemptId, row.FencingToken })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_recipient_package_attempt_fence");
    }
}
