using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportAssemblyItemConfig : IEntityTypeConfiguration<RawExportAssemblyItemRow>
{
    public void Configure(EntityTypeBuilder<RawExportAssemblyItemRow> entity)
    {
        entity.ToTable("raw_export_assembly_items", table =>
        {
            table.HasCheckConstraint("ck_raw_export_assembly_item_shape", "\"Ordinal\" >= 0 AND \"CaptureRevision\" >= 1 AND \"PlaintextLength\" >= 1 AND \"ContentCommitmentSchemaVersion\" >= 1 AND \"ContentCommitmentKeyVersion\" >= 1 AND octet_length(\"ContentCommitment\") = 32");
        });
        entity.HasKey(row => new { row.AssemblyId, row.Ordinal }).HasName("pk_raw_export_assembly_items");
        entity.HasAlternateKey(row => new { row.AssemblyId, row.RawClass }).HasName("uq_raw_export_assembly_item_class");
        entity.Property(row => row.RawClass).HasMaxLength(64).IsRequired();
        entity.Property(row => row.MediaType).HasMaxLength(256).IsRequired();
        entity.Property(row => row.ContentCommitmentKeyId).HasMaxLength(256).IsRequired();
        entity.HasOne<RawExportAssemblyIdentityRow>().WithMany().HasForeignKey(row => row.AssemblyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_item_identity");
        entity.HasOne<RawExportJobSourceBindingRow>().WithMany().HasForeignKey(row => row.JobSourceBindingId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_item_binding");
    }
}
