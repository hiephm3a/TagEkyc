using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportManagedRecipientPolicyConfig
    : IEntityTypeConfiguration<RawExportManagedRecipientPolicyRow>
{
    public void Configure(EntityTypeBuilder<RawExportManagedRecipientPolicyRow> entity)
    {
        entity.ToTable("raw_export_managed_recipient_policies", table =>
            table.HasCheckConstraint("ck_raw_export_managed_recipient_policy_shape",
                "\"ActivationProfile\" = 'C3C4RecipientV1' AND octet_length(\"ActivationScopesDigest\") = 32 AND \"State\" IN ('Active','Disabled') AND \"Revision\" > 0"));
        entity.HasKey(row => row.RecipientClientApplicationId)
            .HasName("pk_raw_export_managed_recipient_policy");
        entity.Property(row => row.ActivationProfile).HasMaxLength(64).IsRequired();
        entity.Property(row => row.ActivationScopesDigest).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.State).HasMaxLength(16).IsRequired();
        entity.HasOne<RawExportManagedRecipientIdentityRow>()
            .WithOne()
            .HasForeignKey<RawExportManagedRecipientPolicyRow>(row => row.RecipientClientApplicationId)
            .HasConstraintName("fk_c5_policy_managed_identity")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
