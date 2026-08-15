using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

internal sealed class RawExportSourcePublicationConfig : IEntityTypeConfiguration<RawExportSourcePublicationRow>
{
    public void Configure(EntityTypeBuilder<RawExportSourcePublicationRow> entity)
    {
        entity.ToTable("raw_export_source_publications", table =>
        {
            table.HasCheckConstraint(
                "ck_raw_export_source_publication_shape",
                """
                "SchemaVersion" = 1
                AND "StagedCiphertextFingerprintSchemaVersion" = 2
                AND pg_catalog.octet_length("StagedCiphertextFingerprint") = 32
                AND "CommittedAuthoritySnapshotSchemaVersion" >= 1
                AND "CommittedAuthorityRevision" >= 1
                AND "CommittedConsentPolicyVersion" >= 1
                AND pg_catalog.octet_length("CommitEvidenceDigest") = 32
                AND "PublicationState" IN ('Committed','Available')
                AND "CleanupDisposition" IN ('NotPlanned','Pending','NoObsoleteResidue','Completed')
                AND (
                  (
                    "PublicationState" = 'Committed'
                    AND "OpaqueCommittedLocatorId" IS NULL
                    AND "PublishedAuthoritySnapshotSchemaVersion" IS NULL
                    AND "PublishedAuthoritySnapshotId" IS NULL
                    AND "PublishedAuthorityRevision" IS NULL
                    AND "PublishedConsentPolicyId" IS NULL
                    AND "PublishedConsentPolicyVersion" IS NULL
                    AND "AvailableEvidenceDigest" IS NULL
                    AND "AvailableAtUtc" IS NULL
                    AND "CleanupDisposition" = 'NotPlanned'
                    AND "CleanupEvidenceDigest" IS NULL
                    AND "FinalizedAtUtc" IS NULL
                    AND "PublicationRevision" = 1
                  )
                  OR
                  (
                    "PublicationState" = 'Available'
                    AND "OpaqueCommittedLocatorId" IS NOT NULL
                    AND "PublishedAuthoritySnapshotSchemaVersion" IS NOT NULL
                    AND "PublishedAuthoritySnapshotSchemaVersion" >= 1
                    AND "PublishedAuthoritySnapshotId" IS NOT NULL
                    AND "PublishedAuthorityRevision" IS NOT NULL
                    AND "PublishedAuthorityRevision" >= 1
                    AND "PublishedConsentPolicyId" IS NOT NULL
                    AND "PublishedConsentPolicyVersion" IS NOT NULL
                    AND "PublishedConsentPolicyVersion" >= 1
                    AND "AvailableEvidenceDigest" IS NOT NULL
                    AND pg_catalog.octet_length("AvailableEvidenceDigest") = 32
                    AND "AvailableAtUtc" IS NOT NULL
                    AND (
                      (
                        "CleanupDisposition" = 'Pending'
                        AND "CleanupEvidenceDigest" IS NULL
                        AND "FinalizedAtUtc" IS NULL
                        AND "PublicationRevision" = 2
                      )
                      OR
                      (
                        "CleanupDisposition" = 'NoObsoleteResidue'
                        AND "CleanupEvidenceDigest" IS NOT NULL
                        AND pg_catalog.octet_length("CleanupEvidenceDigest") = 32
                        AND "FinalizedAtUtc" IS NOT NULL
                        AND "PublicationRevision" = 2
                      )
                      OR
                      (
                        "CleanupDisposition" = 'Completed'
                        AND "CleanupEvidenceDigest" IS NOT NULL
                        AND pg_catalog.octet_length("CleanupEvidenceDigest") = 32
                        AND "FinalizedAtUtc" IS NOT NULL
                        AND "PublicationRevision" = 3
                      )
                    )
                  )
                )
                """);
        });

        entity.HasKey(row => row.SourcePublicationId)
            .HasName("pk_raw_export_source_publications");
        entity.HasAlternateKey(row => row.SourceArtifactId)
            .HasName("uq_raw_export_source_publication_source");
        entity.HasAlternateKey(row => row.AttemptId)
            .HasName("uq_raw_export_source_publication_attempt");
        entity.HasAlternateKey(row => row.ObjectCustodyId)
            .HasName("uq_raw_export_source_publication_object");
        entity.Property(row => row.StagedCiphertextFingerprint).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.CommitEvidenceDigest).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.PublicationState).HasMaxLength(32).IsRequired();
        entity.Property(row => row.AvailableEvidenceDigest).HasColumnType("bytea");
        entity.Property(row => row.CleanupDisposition).HasMaxLength(32).IsRequired();
        entity.Property(row => row.CleanupEvidenceDigest).HasColumnType("bytea");

        entity.HasOne<RawExportSourceReservationRow>()
            .WithMany()
            .HasForeignKey(row => row.SourceArtifactId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_source_publication_reservation");
        entity.HasOne<RawExportSourceEncryptionAttemptRow>()
            .WithMany()
            .HasForeignKey(row => row.AttemptId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_source_publication_attempt");
        entity.HasOne<RawExportProvisionalObjectRow>()
            .WithMany()
            .HasForeignKey(row => row.ObjectCustodyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_source_publication_object");
        entity.HasOne<RawExportAttemptKeyReservationRow>()
            .WithMany()
            .HasForeignKey(row => row.AttemptKeyReservationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_source_publication_key");
        entity.HasOne<RawExportSourceEncryptionAttemptRow>()
            .WithMany()
            .HasForeignKey(row => new { row.AttemptId, row.AttemptKeyReservationId })
            .HasPrincipalKey(row => new { row.AttemptId, row.AttemptKeyReservationId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_source_publication_attempt_key_binding");

        entity.HasIndex(row => row.OpaqueCommittedLocatorId)
            .IsUnique()
            .HasFilter("\"OpaqueCommittedLocatorId\" IS NOT NULL")
            .HasDatabaseName("uq_raw_export_source_publication_locator");
        entity.HasIndex(row => row.AttemptKeyReservationId)
            .HasDatabaseName("ix_raw_export_source_publication_key");
        entity.HasIndex(row => new { row.AttemptId, row.AttemptKeyReservationId })
            .HasDatabaseName("ix_raw_export_source_publication_attempt_key");
    }
}
