using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportAssemblyPreparationDispositionConfig : IEntityTypeConfiguration<RawExportAssemblyPreparationDispositionRow>
{
    public void Configure(EntityTypeBuilder<RawExportAssemblyPreparationDispositionRow> entity)
    {
        entity.ToTable("raw_export_assembly_preparation_dispositions", table =>
        {
            table.HasCheckConstraint("ck_raw_export_assembly_preparation_shape", "\"FencingToken\" >= 1 AND octet_length(\"AssemblyFingerprint\") = 32 AND octet_length(\"PreparationFingerprint\") = 32 AND \"Disposition\" IN ('Preparing','Pending','SealCommitted','Finalized','AbortAuthorized','Aborted') AND (\"ProviderReceiptDigest\" IS NULL OR octet_length(\"ProviderReceiptDigest\") = 32) AND (\"AbortAuthorizationDigest\" IS NULL OR octet_length(\"AbortAuthorizationDigest\") = 32) AND \"RowRevision\" >= 1 AND \"SchemaVersion\" = 1");
            table.HasCheckConstraint("ck_raw_export_assembly_preparation_sparse", "(\"Disposition\" = 'Preparing' AND \"PendingAtUtc\" IS NULL AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'Pending' AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PendingAtUtc\" IS NOT NULL AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'SealCommitted' AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PendingAtUtc\" IS NOT NULL AND \"SealCommittedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'Finalized' AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PendingAtUtc\" IS NOT NULL AND \"SealCommittedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NOT NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'AbortAuthorized' AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NOT NULL AND \"AbortAuthorizedAtUtc\" IS NOT NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'Aborted' AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NOT NULL AND \"AbortAuthorizedAtUtc\" IS NOT NULL AND \"AbortedAtUtc\" IS NOT NULL)");
        });
        entity.HasKey(row => row.C2PreparationId).HasName("pk_raw_export_assembly_preparation");
        entity.HasAlternateKey(row => row.AssemblyId).HasName("uq_raw_export_assembly_preparation_assembly");
        entity.HasAlternateKey(row => row.JobId).HasName("uq_raw_export_assembly_preparation_job");
        entity.Property(row => row.Disposition).HasMaxLength(32).IsRequired();
        entity.HasOne<RawExportJobIdentityRow>().WithMany().HasForeignKey(row => row.JobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_preparation_job");
        entity.HasOne<RawExportJobAttemptRow>().WithMany().HasForeignKey(row => row.AttemptId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_assembly_preparation_attempt");
    }
}
