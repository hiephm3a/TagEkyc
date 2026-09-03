using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportJobSourceBindingConfig : IEntityTypeConfiguration<RawExportJobSourceBindingRow>
{
    public void Configure(EntityTypeBuilder<RawExportJobSourceBindingRow> entity)
    {
        entity.ToTable("raw_export_job_source_bindings", table =>
        {
            table.HasCheckConstraint("ck_raw_export_job_source_binding_shape", "\"Ordinal\" >= 0 AND \"CaptureRevision\" >= 1 AND \"SourcePublicationRevision\" >= 1 AND \"EncryptionAttemptRevision\" >= 1 AND \"EncryptionAttemptFence\" >= 1 AND \"ObjectStateRevision\" >= 1 AND \"SubjectRefTokenSchemaVersion\" >= 1 AND \"SubjectRefTokenKeyVersion\" >= 1 AND octet_length(\"SubjectRefToken\") = 32 AND \"ContentCommitmentSchemaVersion\" >= 1 AND \"ContentCommitmentKeyVersion\" >= 1 AND octet_length(\"ContentCommitment\") = 32 AND \"PlaintextLength\" >= 1 AND \"AuthoritySnapshotSchemaVersion\" >= 1 AND \"AuthorityRevision\" >= 1 AND \"ConsentPolicyVersion\" >= 1 AND octet_length(\"BindingFingerprint\") = 32 AND \"SchemaVersion\" = 1");
        });
        entity.HasKey(row => row.JobSourceBindingId).HasName("pk_raw_export_job_source_bindings");
        entity.HasAlternateKey(row => new { row.JobId, row.Ordinal }).HasName("uq_raw_export_job_source_binding_ordinal");
        entity.HasAlternateKey(row => new { row.JobId, row.RawClass }).HasName("uq_raw_export_job_source_binding_class");
        entity.Property(row => row.RawClass).HasMaxLength(64).IsRequired();
        entity.Property(row => row.SubjectRefTokenKeyId).HasMaxLength(256).IsRequired();
        entity.Property(row => row.ContentCommitmentKeyId).HasMaxLength(256).IsRequired();
        entity.Property(row => row.MediaType).HasMaxLength(256).IsRequired();
        entity.Property(row => row.StableDataScopeId).HasMaxLength(256).IsRequired();
        entity.Property(row => row.ControllerIdentity).HasMaxLength(256).IsRequired();
        entity.HasOne<RawExportJobIdentityRow>().WithMany().HasForeignKey(row => row.JobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_job_source_binding_job");
        entity.HasOne<RawExportSessionCaptureSelectionRow>().WithMany().HasForeignKey(row => row.SessionCaptureSelectionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_job_source_binding_selection");
        entity.HasOne<RawExportSourcePublicationRow>().WithMany().HasForeignKey(row => row.SourcePublicationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_job_source_binding_publication");
        entity.HasOne<RawExportSourceEncryptionAttemptRow>().WithMany().HasForeignKey(row => row.EncryptionAttemptId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_job_source_binding_attempt");
        entity.HasOne<RawExportAttemptKeyReservationRow>().WithMany().HasForeignKey(row => row.AttemptKeyReservationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_job_source_binding_key");
        entity.HasOne<RawExportProvisionalObjectRow>().WithMany().HasForeignKey(row => row.ObjectCustodyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_job_source_binding_object");
    }
}
