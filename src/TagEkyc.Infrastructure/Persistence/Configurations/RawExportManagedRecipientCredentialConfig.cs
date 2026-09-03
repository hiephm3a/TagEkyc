using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportManagedRecipientCredentialConfig
    : IEntityTypeConfiguration<RawExportManagedRecipientCredentialRow>
{
    public void Configure(EntityTypeBuilder<RawExportManagedRecipientCredentialRow> entity)
    {
        entity.ToTable("raw_export_managed_recipient_credentials", table =>
        {
            table.HasCheckConstraint("ck_raw_export_managed_recipient_credential_shape",
                "\"ApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"CredentialVersion\" > 0 AND \"Revision\" > 0 AND \"State\" IN ('Active','Revoked')");
            table.HasCheckConstraint("ck_raw_export_managed_recipient_credential_sparse",
                "(\"State\"='Active' AND \"RevokedAtUtc\" IS NULL AND \"RevocationReason\" IS NULL AND \"ReplacedByApiKeyId\" IS NULL) OR (\"State\"='Revoked' AND \"RevokedAtUtc\" IS NOT NULL AND \"RevocationReason\" IS NOT NULL AND length(\"RevocationReason\") BETWEEN 1 AND 128)");
        });
        entity.HasKey(row => row.ApiKeyId).HasName("pk_raw_export_managed_recipient_credential");
        entity.Property(row => row.State).HasMaxLength(16).IsRequired();
        entity.Property(row => row.RevocationReason).HasMaxLength(128);
        entity.HasIndex(row => new { row.RecipientClientApplicationId, row.CredentialVersion })
            .IsUnique().HasDatabaseName("uq_raw_export_managed_credential_version");
        entity.HasIndex(row => row.RecipientClientApplicationId)
            .IsUnique().HasFilter("\"State\" = 'Active'")
            .HasDatabaseName("uq_raw_export_managed_credential_active");
        entity.HasOne<RawExportManagedRecipientIdentityRow>()
            .WithMany()
            .HasForeignKey(row => new { row.RecipientClientApplicationId, row.PrincipalId })
            .HasPrincipalKey(row => new { row.RecipientClientApplicationId, row.PrincipalId })
            .HasConstraintName("fk_c5_credential_managed_identity")
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ApiKeyRow>()
            .WithMany()
            .HasForeignKey(row => new { row.ApiKeyId, ClientApplicationId = row.RecipientClientApplicationId, row.PrincipalId })
            .HasPrincipalKey(row => new { row.ApiKeyId, row.ClientApplicationId, row.PrincipalId })
            .HasConstraintName("fk_c5_credential_api_key_identity")
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RawExportManagedRecipientCredentialRow>()
            .WithMany()
            .HasForeignKey(row => row.ReplacedByApiKeyId)
            .HasConstraintName("fk_c5_credential_replacement")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
