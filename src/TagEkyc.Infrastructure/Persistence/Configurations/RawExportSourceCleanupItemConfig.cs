using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

internal sealed class RawExportSourceCleanupItemConfig : IEntityTypeConfiguration<RawExportSourceCleanupItemRow>
{
    public void Configure(EntityTypeBuilder<RawExportSourceCleanupItemRow> entity)
    {
        entity.ToTable("raw_export_source_cleanup_items", table =>
        {
            table.HasCheckConstraint(
                "ck_raw_export_source_cleanup_item_shape",
                """
                "SchemaVersion" = 1
                AND "ResourceKind" IN ('ProvisionalObject','AttemptKeyReservation')
                AND "PlannedResourceRevision" >= 1
                AND "CleanupState" IN ('Pending','Completed')
                AND (
                  (
                    "CleanupState" = 'Pending'
                    AND "CompletionDisposition" IS NULL
                    AND "CleanupEvidenceDigest" IS NULL
                    AND "CompletedAtUtc" IS NULL
                    AND "RowRevision" = 1
                  )
                  OR
                  (
                    "CleanupState" = 'Completed'
                    AND "CompletionDisposition" IS NOT NULL
                    AND "CleanupEvidenceDigest" IS NOT NULL
                    AND pg_catalog.octet_length("CleanupEvidenceDigest") = 32
                    AND "CompletedAtUtc" IS NOT NULL
                    AND "RowRevision" = 2
                    AND (
                      ("ResourceKind" = 'ProvisionalObject'
                       AND "CompletionDisposition" IN ('Deleted','Quarantined'))
                      OR
                      ("ResourceKind" = 'AttemptKeyReservation'
                       AND "CompletionDisposition" IN ('Revoked','ReservationAbandoned'))
                    )
                  )
                )
                """);
        });

        entity.HasKey(row => row.CleanupItemId)
            .HasName("pk_raw_export_source_cleanup_items");
        entity.Property(row => row.ResourceKind).HasMaxLength(32).IsRequired();
        entity.Property(row => row.CleanupState).HasMaxLength(32).IsRequired();
        entity.Property(row => row.CompletionDisposition).HasMaxLength(32);
        entity.Property(row => row.CleanupEvidenceDigest).HasColumnType("bytea");
        entity.HasOne<RawExportSourcePublicationRow>()
            .WithMany()
            .HasForeignKey(row => row.SourcePublicationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_source_cleanup_item_publication");
        entity.HasIndex(row => new { row.SourcePublicationId, row.ResourceKind, row.ResourceId })
            .IsUnique()
            .HasDatabaseName("uq_raw_export_source_cleanup_item_resource");
        entity.HasIndex(row => new { row.SourcePublicationId, row.CleanupState, row.CleanupItemId })
            .HasDatabaseName("ix_raw_export_source_cleanup_item_pending");
    }
}
