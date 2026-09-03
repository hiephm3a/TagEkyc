using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportManagedRecipientIdentityConfig
    : IEntityTypeConfiguration<RawExportManagedRecipientIdentityRow>
{
    public void Configure(EntityTypeBuilder<RawExportManagedRecipientIdentityRow> entity)
    {
        entity.ToTable("raw_export_managed_recipient_identities", table =>
            table.HasCheckConstraint("ck_raw_export_managed_recipient_identity_shape",
                "\"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PrincipalId\" <> \"RecipientClientApplicationId\" AND \"State\" IN ('Active','Disabled') AND \"Revision\" > 0"));
        entity.HasKey(row => row.RecipientClientApplicationId)
            .HasName("pk_raw_export_managed_recipient_identity");
        entity.HasAlternateKey(row => new { row.RecipientClientApplicationId, row.PrincipalId })
            .HasName("uq_raw_export_managed_recipient_identity_pair");
        entity.Property(row => row.State).HasMaxLength(16).IsRequired();
    }
}
