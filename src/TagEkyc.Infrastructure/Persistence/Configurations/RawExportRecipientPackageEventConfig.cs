using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportRecipientPackageEventConfig : IEntityTypeConfiguration<RawExportRecipientPackageEventRow>
{
    public void Configure(EntityTypeBuilder<RawExportRecipientPackageEventRow> entity)
    {
        entity.ToTable("raw_export_recipient_package_events", table =>
        {
            table.HasCheckConstraint("ck_raw_export_recipient_package_event_shape", "\"EventRevision\" > 0 AND \"EventKind\" IN ('SnapshotFrozen','PutStarted','PutOutcomeUnknown','Prepared','Finalized','AbortAuthorized','CleanupPending','Aborted','Quarantined') AND \"State\" IN ('Reserved','PutInFlight','PutOutcomeUnknown','Prepared','Finalized','AbortAuthorized','CleanupPending','Aborted','Quarantined') AND (\"EvidenceDigest\" IS NULL OR octet_length(\"EvidenceDigest\") = 32) AND ((\"EventKind\" IN ('SnapshotFrozen','PutStarted') AND \"EvidenceDigest\" IS NULL) OR (\"EventKind\" NOT IN ('SnapshotFrozen','PutStarted') AND \"EvidenceDigest\" IS NOT NULL))");
        });
        entity.HasKey(row => new { row.C2PreparationId, row.EventRevision })
            .HasName("pk_raw_export_recipient_package_event");
        entity.Property(row => row.EventKind).HasMaxLength(32).IsRequired();
        entity.Property(row => row.State).HasMaxLength(32).IsRequired();
        entity.HasOne<RawExportRecipientPackagePreparationRow>().WithMany()
            .HasForeignKey(row => row.C2PreparationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_recipient_package_event_preparation");
    }
}
