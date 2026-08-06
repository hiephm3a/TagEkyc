using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportProvisionalObjectEventConfig
    : IEntityTypeConfiguration<RawExportProvisionalObjectEventRow>
{
    public void Configure(EntityTypeBuilder<RawExportProvisionalObjectEventRow> entity)
    {
        entity.ToTable("raw_export_provisional_object_events", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_provisional_object_events_values", """
                "SchemaVersion" = 1
                AND "EventSequence" >= 1
                AND "StateRevision" >= 1
                AND "EventSequence" = "StateRevision"
                AND "ToState" IN ('Initiated','PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','NoObjectEstablished','ObjectConflict','CleanupPending','Deleted','Quarantined')
                AND ("FromState" IS NULL OR "FromState" IN ('Initiated','PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','ObjectConflict','CleanupPending'))
                AND "ActorKind" IN ('Writer','Reconciler','Lifecycle')
                AND octet_length("EvidenceDigest") = 32
                """);
        });
        entity.HasKey(x => x.ObjectCustodyEventId).HasName("pk_raw_export_provisional_object_events");
        entity.HasAlternateKey(x => new { x.ObjectCustodyId, x.EventSequence })
            .HasName("uq_raw_export_provisional_object_events_sequence");
        entity.HasAlternateKey(x => new { x.ObjectCustodyId, x.StateRevision })
            .HasName("uq_raw_export_provisional_object_events_revision");
        entity.Property(x => x.FromState).HasMaxLength(64);
        entity.Property(x => x.ToState).HasMaxLength(64).IsRequired();
        entity.Property(x => x.ActorKind).HasMaxLength(32).IsRequired();
        entity.Property(x => x.EvidenceDigest).HasColumnType("bytea");
        entity.HasOne<RawExportProvisionalObjectRow>().WithMany()
            .HasForeignKey(x => x.ObjectCustodyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_provisional_object_events_head");
    }
}
