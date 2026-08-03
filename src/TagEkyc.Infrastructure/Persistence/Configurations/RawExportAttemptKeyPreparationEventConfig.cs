using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportAttemptKeyPreparationEventConfig
    : IEntityTypeConfiguration<RawExportAttemptKeyPreparationEventRow>
{
    public void Configure(EntityTypeBuilder<RawExportAttemptKeyPreparationEventRow> entity)
    {
        entity.ToTable("raw_export_attempt_key_preparation_events", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_attempt_key_event_values", """
                "EventSequence" >= 1
                AND "EventKind" IN ('Opened','Expired','DirectActivated','RecoveredActivated','ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ResolvedCleanupRequired','ProviderUnavailableObserved','CleanupAttemptObserved','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked')
                AND ("ProviderResolutionEvidenceDigest" IS NULL OR octet_length("ProviderResolutionEvidenceDigest")=32)
                AND ("ProviderCleanupEvidenceDigest" IS NULL OR octet_length("ProviderCleanupEvidenceDigest")=32)
                AND ("CleanupObservationEvidenceDigest" IS NULL OR octet_length("CleanupObservationEvidenceDigest")=32)
                AND ("WrappedDekMetadataDigest" IS NULL OR octet_length("WrappedDekMetadataDigest")=32)
                AND ("RevocationEvidenceDigest" IS NULL OR octet_length("RevocationEvidenceDigest")=32)
                AND ("AbandonmentEvidenceDigest" IS NULL OR octet_length("AbandonmentEvidenceDigest")=32)
                AND CASE
                  WHEN "EventKind"='Opened' THEN "ProviderOperationToken" IS NOT NULL AND "WrappingSuiteId"='AES-256-GCM' AND "WrappingSuiteVersion"=1
                    AND "ResolutionKind" IS NULL AND "CleanupResultKind" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "RevocationReasonCode" IS NULL AND "RevocationEvidenceDigest" IS NULL AND "OperatorReasonCode" IS NULL AND "RequestingActorEvidence" IS NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NULL
                  WHEN "EventKind"='Expired' THEN "ResolutionKind" IS NULL AND "CleanupResultKind" IS NULL AND "ProviderOperationToken" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "RevocationReasonCode" IS NULL AND "RevocationEvidenceDigest" IS NULL AND "OperatorReasonCode" IS NULL AND "RequestingActorEvidence" IS NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NULL
                  WHEN "EventKind"='DirectActivated' THEN "ProviderOperationReceipt" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL AND "WrappingSuiteId"='AES-256-GCM' AND "WrappingSuiteVersion"=1 AND "ResolutionKind" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL
                  WHEN "EventKind"='RecoveredActivated' THEN "ProviderOperationReceipt" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL AND "WrappingSuiteId"='AES-256-GCM' AND "WrappingSuiteVersion"=1 AND "ResolutionKind"='WrappedResultRecovered' AND "ProviderResolutionEvidenceDigest" IS NOT NULL
                  WHEN "EventKind" IN ('ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ProviderUnavailableObserved') THEN "ResolutionKind" IS NOT NULL AND "ProviderResolutionEvidenceDigest" IS NOT NULL AND "CleanupResultKind" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL
                  WHEN "EventKind"='ResolvedCleanupRequired' THEN "ResolutionKind"='ProviderResourceCleanupRequired' AND "ProviderResolutionEvidenceDigest" IS NOT NULL AND "ProviderCleanupReference" IS NOT NULL
                  WHEN "EventKind"='CleanupAttemptObserved' THEN "CleanupResultKind" IS NOT NULL AND "ProviderCleanupReference" IS NOT NULL AND "CleanupObservationEvidenceDigest" IS NOT NULL
                  WHEN "EventKind"='CleanupAcknowledged' THEN "CleanupResultKind" IN ('Cleaned','AlreadyAbsent') AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderCleanupEvidenceDigest" IS NOT NULL
                  WHEN "EventKind"='AbandonRequested' THEN "ProviderOperationToken" IS NOT NULL AND "OperatorReasonCode" IS NOT NULL AND "RequestingActorEvidence" IS NOT NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NOT NULL
                  WHEN "EventKind"='ProviderOperationAbandoned' THEN "ProviderOperationToken" IS NOT NULL AND "OperatorReasonCode" IS NOT NULL AND "RequestingActorEvidence" IS NOT NULL AND "FinalizingActorEvidence" IS NOT NULL AND "AbandonRequestPreparationEventId" IS NOT NULL AND "AbandonmentEvidenceDigest" IS NOT NULL AND "HeadRowRevision" IS NOT NULL
                  WHEN "EventKind"='Revoked' THEN "RevocationReasonCode" IS NOT NULL AND "RevocationEvidenceDigest" IS NOT NULL
                  ELSE FALSE
                END
                """);
            table.HasCheckConstraint("ck_raw_export_attempt_key_event_text", """
                ("ProviderOperationToken" IS NULL OR "ProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$')
                AND ("ProviderOperationReceipt" IS NULL OR ("ProviderOperationReceipt"=btrim("ProviderOperationReceipt") AND "ProviderOperationReceipt"=normalize("ProviderOperationReceipt",NFC) AND octet_length("ProviderOperationReceipt") BETWEEN 1 AND 512 AND "ProviderOperationReceipt" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderCleanupReference" IS NULL OR ("ProviderCleanupReference"=btrim("ProviderCleanupReference") AND "ProviderCleanupReference"=normalize("ProviderCleanupReference",NFC) AND octet_length("ProviderCleanupReference") BETWEEN 1 AND 512 AND "ProviderCleanupReference" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderCleanupReceipt" IS NULL OR ("ProviderCleanupReceipt"=btrim("ProviderCleanupReceipt") AND "ProviderCleanupReceipt"=normalize("ProviderCleanupReceipt",NFC) AND octet_length("ProviderCleanupReceipt") BETWEEN 1 AND 512 AND "ProviderCleanupReceipt" !~ '[\x00-\x1f\x7f]'))
                AND ("RevocationReasonCode" IS NULL OR ("RevocationReasonCode"=btrim("RevocationReasonCode") AND "RevocationReasonCode"=normalize("RevocationReasonCode",NFC) AND octet_length("RevocationReasonCode") BETWEEN 1 AND 512 AND "RevocationReasonCode" !~ '[\x00-\x1f\x7f]'))
                AND ("OperatorReasonCode" IS NULL OR ("OperatorReasonCode"=btrim("OperatorReasonCode") AND "OperatorReasonCode"=normalize("OperatorReasonCode",NFC) AND octet_length("OperatorReasonCode") BETWEEN 1 AND 512 AND "OperatorReasonCode" !~ '[\x00-\x1f\x7f]'))
                AND ("RequestingActorEvidence" IS NULL OR ("RequestingActorEvidence"=btrim("RequestingActorEvidence") AND "RequestingActorEvidence"=normalize("RequestingActorEvidence",NFC) AND octet_length("RequestingActorEvidence") BETWEEN 1 AND 512 AND "RequestingActorEvidence" !~ '[\x00-\x1f\x7f]'))
                AND ("FinalizingActorEvidence" IS NULL OR ("FinalizingActorEvidence"=btrim("FinalizingActorEvidence") AND "FinalizingActorEvidence"=normalize("FinalizingActorEvidence",NFC) AND octet_length("FinalizingActorEvidence") BETWEEN 1 AND 512 AND "FinalizingActorEvidence" !~ '[\x00-\x1f\x7f]'))
                """);
        });
        entity.HasKey(x => x.PreparationEventId).HasName("pk_raw_export_attempt_key_preparation_events");
        entity.Property(x => x.PreparationEventId).ValueGeneratedNever();
        foreach (var p in new[] { nameof(RawExportAttemptKeyPreparationEventRow.ProviderOperationToken), nameof(RawExportAttemptKeyPreparationEventRow.ProviderOperationReceipt), nameof(RawExportAttemptKeyPreparationEventRow.ProviderCleanupReference), nameof(RawExportAttemptKeyPreparationEventRow.ProviderCleanupReceipt), nameof(RawExportAttemptKeyPreparationEventRow.RevocationReasonCode), nameof(RawExportAttemptKeyPreparationEventRow.OperatorReasonCode), nameof(RawExportAttemptKeyPreparationEventRow.RequestingActorEvidence), nameof(RawExportAttemptKeyPreparationEventRow.FinalizingActorEvidence) })
            entity.Property<string?>(p).HasMaxLength(512);
        entity.HasIndex(x => new { x.AttemptKeyReservationId, x.EventSequence }).IsUnique().HasDatabaseName("uq_raw_export_attempt_key_event_sequence");
        entity.HasIndex(x => new { x.AttemptKeyReservationId, x.PreparationId, x.PreparationFence, x.EventKind })
            .IsUnique()
            .HasFilter("\"EventKind\" IN ('Opened','Expired','DirectActivated','RecoveredActivated','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked')")
            .HasDatabaseName("uq_raw_export_attempt_key_event_singleton");
        entity.HasIndex(x => x.AbandonRequestPreparationEventId)
            .HasDatabaseName("ix_raw_export_attempt_key_event_abandon_req");
        entity.HasOne<RawExportAttemptKeyReservationRow>().WithMany().HasForeignKey(x => x.AttemptKeyReservationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_attempt_key_event_reservation");
        entity.HasOne<RawExportAttemptKeyPreparationEventRow>().WithMany().HasForeignKey(x => x.AbandonRequestPreparationEventId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_abandon_request_event");
    }
}
