using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportAttemptKeyReservationConfig
    : IEntityTypeConfiguration<RawExportAttemptKeyReservationRow>
{
    public void Configure(EntityTypeBuilder<RawExportAttemptKeyReservationRow> entity)
    {
        entity.ToTable("raw_export_attempt_key_reservations", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_attempt_key_reservation_values", """
                octet_length("EncryptionAttemptFingerprint") = 32
                AND octet_length("AttemptKeyContextFingerprint") = 32
                AND "KekVersion" >= 1
                AND "WrappingSuiteId" = 'AES-256-GCM'
                AND "WrappingSuiteVersion" = 1
                AND "CurrentPreparationFence" >= 1
                AND "ResolutionAttemptCount" >= 0
                AND "CleanupAttemptCount" >= 0
                AND "RowRevision" >= 1
                AND "PreparationDisposition" IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown','ProviderCorruptOrUnverifiable','ProviderCleanupRequired','ReadyForFreshPreparation','Active','Revoked','AbandonRequested','ReservationAbandoned')
                AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL)
                  OR (octet_length("WrappedDekCiphertext") = 32 AND octet_length("WrappedDekNonce") = 12 AND octet_length("WrappedDekTag") = 16 AND octet_length("WrappedDekMetadataDigest") = 32))
                AND ("CurrentProviderOperationToken" IS NULL OR "CurrentProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$')
                """);
            table.HasCheckConstraint("ck_raw_export_attempt_key_reservation_text", """
                "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                  AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                AND "KekId"=btrim("KekId") AND "KekId"=normalize("KekId",NFC)
                  AND octet_length("KekId") BETWEEN 1 AND 512 AND "KekId" !~ '[\x00-\x1f\x7f]'
                AND "KekFingerprint"=btrim("KekFingerprint") AND "KekFingerprint"=normalize("KekFingerprint",NFC)
                  AND octet_length("KekFingerprint") BETWEEN 1 AND 512 AND "KekFingerprint" !~ '[\x00-\x1f\x7f]'
                AND "WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC)
                  AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'
                AND ("RevocationReasonCode" IS NULL OR (
                  "RevocationReasonCode"=btrim("RevocationReasonCode") AND "RevocationReasonCode"=normalize("RevocationReasonCode",NFC)
                  AND octet_length("RevocationReasonCode") BETWEEN 1 AND 512 AND "RevocationReasonCode" !~ '[\x00-\x1f\x7f]'))
                """);
            table.HasCheckConstraint("ck_raw_export_attempt_key_reservation_sparse", """
                CASE
                  WHEN "PreparationDisposition" = 'PreparingLive'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                      AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                      AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'PreparingExpiredAwaitingResolution'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                      AND "ResolutionDeadlineUtc" IS NOT NULL AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired"
                      AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'ProviderOutcomeUnknown'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                      AND "ResolutionDeadlineUtc" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NOT NULL AND "ResolutionAttemptCount">0
                      AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                      AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'Active'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekCiphertext" IS NOT NULL
                      AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                      AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'Revoked'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekCiphertext" IS NOT NULL
                      AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                      AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NOT NULL AND "RevocationReasonCode" IS NOT NULL
                  WHEN "PreparationDisposition" = 'ProviderCleanupRequired'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "CleanupDeadlineUtc" IS NOT NULL
                      AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'ReadyForFreshPreparation'
                    THEN "CurrentPreparationId" IS NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NULL
                      AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                      AND NOT "CleanupOperatorInterventionRequired" AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'ProviderCorruptOrUnverifiable'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                      AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'AbandonRequested'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                      AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  WHEN "PreparationDisposition" = 'ReservationAbandoned'
                    THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NOT NULL
                      AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                      AND NOT "CleanupOperatorInterventionRequired" AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                  ELSE FALSE
                END
                """);
        });
        entity.HasKey(x => x.AttemptKeyReservationId).HasName("pk_raw_export_attempt_key_reservations");
        entity.Property(x => x.AttemptKeyReservationId).ValueGeneratedNever();
        entity.Property(x => x.EncryptionAttemptFingerprint).HasColumnType("bytea");
        entity.Property(x => x.AttemptKeyContextFingerprint).HasColumnType("bytea");
        foreach (var p in new[] { nameof(RawExportAttemptKeyReservationRow.KeyProviderId), nameof(RawExportAttemptKeyReservationRow.KekId), nameof(RawExportAttemptKeyReservationRow.KekFingerprint), nameof(RawExportAttemptKeyReservationRow.WrappingSuiteId), nameof(RawExportAttemptKeyReservationRow.RevocationReasonCode) })
            entity.Property<string?>(p).HasMaxLength(512);
        entity.Property(x => x.CurrentProviderOperationToken).HasMaxLength(43);
        entity.Property(x => x.CurrentPreparationFence).HasDefaultValue(1L);
        entity.Property(x => x.ResolutionAttemptCount).HasDefaultValue(0L);
        entity.Property(x => x.CleanupAttemptCount).HasDefaultValue(0L);
        entity.Property(x => x.CleanupOperatorInterventionRequired).HasDefaultValue(false);
        entity.Property(x => x.RowRevision).HasDefaultValue(1L);
        entity.HasIndex(x => new { x.AttemptId, x.AttemptKeyReservationId })
            .HasDatabaseName("ix_raw_export_attempt_key_resv_attempt");
        entity.HasOne<RawExportSourceEncryptionAttemptRow>().WithMany()
            .HasForeignKey(x => new { x.AttemptId, x.AttemptKeyReservationId })
            .HasPrincipalKey(x => new { x.AttemptId, x.AttemptKeyReservationId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_attempt_key_resv_attempt_composite");
    }
}
