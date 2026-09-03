using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportRecipientManagementEventConfig
    : IEntityTypeConfiguration<RawExportRecipientManagementEventRow>
{
    public void Configure(EntityTypeBuilder<RawExportRecipientManagementEventRow> entity)
    {
        entity.ToTable("raw_export_recipient_management_events", table =>
        {
            table.HasCheckConstraint("ck_raw_export_recipient_management_event_shape", """
                "ManagementEventId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "OperationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "ManagerApiKeyId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "ManagerPrincipalId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "RecipientClientApplicationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "EventType" IN ('ManagedRecipientEnrolled','ManagedCredentialIssued','ManagedCredentialReplaced','ManagedCredentialRevoked','RecipientPublicKeyEnrolled','RecipientPublicKeyRotated','RecipientPublicKeyRevoked')
                AND length("TargetIdentity") BETWEEN 1 AND 512 AND length("Reason") BETWEEN 1 AND 128
                AND "PriorRevision">=0 AND "NewRevision">0
                AND octet_length("PayloadDigest")=32 AND octet_length("EvidenceDigest")=32
                AND ("PriorScopesDigest" IS NULL OR octet_length("PriorScopesDigest")=32)
                AND ("NewScopesDigest" IS NULL OR octet_length("NewScopesDigest")=32)
                """);
            table.HasCheckConstraint("ck_raw_export_recipient_management_event_sparse", """
                ("EventType"='RecipientPublicKeyRevoked' AND "AuthorizedDeliveryCount">=0
                 AND "StreamingDeliveryCount">=0 AND "InterruptedDeliveryCount">=0)
                OR ("EventType"<>'RecipientPublicKeyRevoked' AND "AuthorizedDeliveryCount" IS NULL
                    AND "StreamingDeliveryCount" IS NULL AND "InterruptedDeliveryCount" IS NULL)
                """);
        });
        entity.HasKey(row => row.ManagementEventId).HasName("pk_raw_export_recipient_management_event");
        entity.Property(row => row.EventType).HasMaxLength(48).IsRequired();
        entity.Property(row => row.TargetIdentity).HasMaxLength(512).IsRequired();
        entity.Property(row => row.Reason).HasMaxLength(128).IsRequired();
        entity.Property(row => row.PayloadDigest).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.PriorScopesDigest).HasColumnType("bytea");
        entity.Property(row => row.NewScopesDigest).HasColumnType("bytea");
        entity.Property(row => row.EvidenceDigest).HasColumnType("bytea").IsRequired();
        entity.HasIndex(row => row.OperationId).IsUnique()
            .HasDatabaseName("uq_raw_export_recipient_management_event_operation");
        entity.HasOne<RawExportRecipientManagementOperationRow>()
            .WithOne()
            .HasForeignKey<RawExportRecipientManagementEventRow>(row => row.OperationId)
            .HasConstraintName("fk_c5_event_operation")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
