using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportRecipientPackageDeliveryEventConfig : IEntityTypeConfiguration<RawExportRecipientPackageDeliveryEventRow>
{
    public void Configure(EntityTypeBuilder<RawExportRecipientPackageDeliveryEventRow> entity)
    {
        entity.ToTable("raw_export_recipient_package_delivery_events", table =>
        {
            table.HasCheckConstraint("ck_raw_export_recipient_package_delivery_event_shape",
                "\"DeliveryEventId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"AuthenticatedApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"AuthenticatedPrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"Revision\">0 AND octet_length(\"CorrelationDigest\")=32 AND octet_length(\"EvidenceDigest\")=32 AND ((\"EventType\"='Authorized' AND \"DeliveryAttemptNumber\" IS NULL AND \"DeliveryFence\" IS NULL) OR (\"EventType\" IN ('StreamingStarted','Interrupted','IntegrityUnavailable','OutcomeUnknown','ServerStreamCompleted') AND \"DeliveryAttemptNumber\" IS NOT NULL AND \"DeliveryFence\" IS NOT NULL AND \"DeliveryAttemptNumber\">0 AND \"DeliveryFence\">0) OR (\"EventType\"='Expired' AND ((\"DeliveryAttemptNumber\" IS NULL AND \"DeliveryFence\" IS NULL) OR (\"DeliveryAttemptNumber\" IS NOT NULL AND \"DeliveryFence\" IS NOT NULL AND \"DeliveryAttemptNumber\">0 AND \"DeliveryFence\">0))))");
        });
        entity.HasKey(row => row.DeliveryEventId).HasName("pk_raw_export_recipient_package_delivery_event");
        entity.HasIndex(row => new { row.DeliveryId, row.Revision }).IsUnique()
            .HasDatabaseName("uq_raw_export_recipient_package_delivery_event_revision");
        entity.Property(row => row.EventType).HasMaxLength(32).IsRequired();
        entity.HasOne<RawExportRecipientPackageDeliveryRow>().WithMany().HasForeignKey(row => row.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_recipient_package_delivery_event_delivery");
    }
}
