using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportAssemblyIdentityConfig : IEntityTypeConfiguration<RawExportAssemblyIdentityRow>
{
    public void Configure(EntityTypeBuilder<RawExportAssemblyIdentityRow> entity)
    {
        entity.ToTable("raw_export_assembly_identities", table =>
        {
            table.HasCheckConstraint("ck_raw_export_assembly_identity_shape", "\"FencingToken\" >= 1 AND \"PolicyVersion\" >= 1 AND \"ManifestVersion\" = 1 AND \"SubjectRefTokenSchemaVersion\" >= 1 AND \"SubjectRefTokenKeyVersion\" >= 1 AND octet_length(\"SubjectRefToken\") = 32 AND \"AssemblyAuthenticationKeyVersion\" >= 1 AND octet_length(\"AssemblyDigest\") = 32 AND octet_length(\"ManifestDigest\") = 32 AND octet_length(\"AssemblyAuthenticationValue\") = 32 AND octet_length(\"AssemblyFingerprint\") = 32 AND \"CompleteAssemblyLength\" >= 1 AND \"ItemCount\" >= 1 AND \"SchemaVersion\" = 1");
        });
        entity.HasKey(row => row.AssemblyId).HasName("pk_raw_export_assembly_identities");
        entity.HasAlternateKey(row => row.JobId).HasName("uq_raw_export_assembly_identity_job");
        entity.HasAlternateKey(row => row.C2PreparationId).HasName("uq_raw_export_assembly_identity_preparation");
        entity.Property(row => row.PurposeCode).HasMaxLength(128).IsRequired();
        entity.Property(row => row.ExportMode).HasMaxLength(64).IsRequired();
        entity.Property(row => row.SubjectRefTokenKeyId).HasMaxLength(256).IsRequired();
        entity.Property(row => row.AssemblyAuthenticationKeyId).HasMaxLength(256).IsRequired();
        entity.HasOne<RawExportJobIdentityRow>().WithMany().HasForeignKey(row => row.JobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_identity_job");
        entity.HasOne<RawExportJobAttemptRow>().WithMany().HasForeignKey(row => row.AttemptId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_identity_attempt");
        entity.HasOne<RawExportAssemblyPreparationDispositionRow>().WithOne().HasForeignKey<RawExportAssemblyIdentityRow>(row => row.C2PreparationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_identity_preparation");
    }
}
