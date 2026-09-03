using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportRecipientKeyRegistrationConfig : IEntityTypeConfiguration<RawExportRecipientKeyRegistrationRow>
{
    public void Configure(EntityTypeBuilder<RawExportRecipientKeyRegistrationRow> entity)
    {
        entity.ToTable("raw_export_recipient_key_registrations", table =>
        {
            table.HasCheckConstraint("ck_raw_export_recipient_key_registration_shape", "\"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"RecipientKeyId\" ~ '^[A-Za-z0-9._:-]{1,128}$' AND \"RecipientKeyVersion\" > 0 AND \"PublicKeyAlgorithm\" = 'RSA-OAEP-256' AND octet_length(\"PublicKeySpki\") BETWEEN 384 AND 1024 AND octet_length(\"PublicKeyFingerprint\") = 32 AND \"ValidFromUtc\" < \"ValidUntilUtc\" AND \"State\" IN ('Active','Revoked') AND \"Revision\" > 0");
            table.HasCheckConstraint("ck_raw_export_recipient_key_registration_sparse", "(\"State\" = 'Active' AND \"RevokedAtUtc\" IS NULL AND \"RevocationReason\" IS NULL) OR (\"State\" = 'Revoked' AND \"RevokedAtUtc\" IS NOT NULL AND \"RevocationReason\" IS NOT NULL AND length(\"RevocationReason\") BETWEEN 1 AND 128)");
        });
        entity.HasKey(row => new { row.RecipientClientApplicationId, row.RecipientKeyId, row.RecipientKeyVersion })
            .HasName("pk_raw_export_recipient_key_registration");
        entity.HasAlternateKey(row => new { row.RecipientClientApplicationId, row.PublicKeyFingerprint })
            .HasName("uq_raw_export_recipient_key_registration_fingerprint");
        entity.HasIndex(row => row.RecipientClientApplicationId)
            .IsUnique()
            .HasFilter("\"State\" = 'Active'")
            .HasDatabaseName("uq_raw_export_recipient_key_registration_active");
        entity.Property(row => row.RecipientKeyId).HasMaxLength(128).IsRequired();
        entity.Property(row => row.PublicKeyAlgorithm).HasMaxLength(32).IsRequired();
        entity.Property(row => row.State).HasMaxLength(16).IsRequired();
        entity.Property(row => row.RevocationReason).HasMaxLength(128);
    }
}
