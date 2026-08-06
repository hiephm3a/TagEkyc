using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportKeyProviderOperationConfig
    : IEntityTypeConfiguration<RawExportKeyProviderOperationRow>
{
    public void Configure(EntityTypeBuilder<RawExportKeyProviderOperationRow> entity)
    {
        entity.ToTable("raw_export_key_provider_operations", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_key_provider_operation_values", """
                "ProviderOperationState" IN ('Issued','ResultObserved','CleanupRequired','CleanedUp','AbsenceProven')
                AND "PreparationFence" >= 1
                AND octet_length("AttemptKeyContextFingerprint") = 32
                AND "ProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$'
                """);
            table.HasCheckConstraint("ck_raw_export_key_provider_operation_text", """
                "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                  AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                AND ("WrappingSuiteId" IS NULL OR ("WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC) AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderResourceReference" IS NULL OR ("ProviderResourceReference"=btrim("ProviderResourceReference") AND "ProviderResourceReference"=normalize("ProviderResourceReference",NFC) AND octet_length("ProviderResourceReference") BETWEEN 1 AND 512 AND "ProviderResourceReference" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderOperationReceipt" IS NULL OR ("ProviderOperationReceipt"=btrim("ProviderOperationReceipt") AND "ProviderOperationReceipt"=normalize("ProviderOperationReceipt",NFC) AND octet_length("ProviderOperationReceipt") BETWEEN 1 AND 512 AND "ProviderOperationReceipt" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderCleanupReference" IS NULL OR ("ProviderCleanupReference"=btrim("ProviderCleanupReference") AND "ProviderCleanupReference"=normalize("ProviderCleanupReference",NFC) AND octet_length("ProviderCleanupReference") BETWEEN 1 AND 512 AND "ProviderCleanupReference" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderCleanupReceipt" IS NULL OR ("ProviderCleanupReceipt"=btrim("ProviderCleanupReceipt") AND "ProviderCleanupReceipt"=normalize("ProviderCleanupReceipt",NFC) AND octet_length("ProviderCleanupReceipt") BETWEEN 1 AND 512 AND "ProviderCleanupReceipt" !~ '[\x00-\x1f\x7f]'))
                AND ("ProviderAbsenceProofReceipt" IS NULL OR ("ProviderAbsenceProofReceipt"=btrim("ProviderAbsenceProofReceipt") AND "ProviderAbsenceProofReceipt"=normalize("ProviderAbsenceProofReceipt",NFC) AND octet_length("ProviderAbsenceProofReceipt") BETWEEN 1 AND 512 AND "ProviderAbsenceProofReceipt" !~ '[\x00-\x1f\x7f]'))
                """.ReplaceLineEndings("\r\n"));
            table.HasCheckConstraint("ck_raw_export_key_provider_operation_sparse", """
                CASE "ProviderOperationState"
                  WHEN 'Issued' THEN "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL
                    AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL
                    AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL
                    AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                  WHEN 'ResultObserved' THEN octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL AND "ProviderCleanupReference" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                  WHEN 'CleanupRequired' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                    AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL)
                      OR (octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                  WHEN 'CleanedUp' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL
                    AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL)
                      OR (octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                  WHEN 'AbsenceProven' THEN "ProviderAbsenceProofReceipt" IS NOT NULL AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                  ELSE FALSE
                END
                """);
        });
        entity.HasKey(x => x.ProviderOperationId).HasName("pk_raw_export_key_provider_operations");
        entity.Property(x => x.ProviderOperationId).ValueGeneratedNever();
        foreach (var p in new[] { nameof(RawExportKeyProviderOperationRow.KeyProviderId), nameof(RawExportKeyProviderOperationRow.WrappingSuiteId), nameof(RawExportKeyProviderOperationRow.ProviderResourceReference), nameof(RawExportKeyProviderOperationRow.ProviderOperationReceipt), nameof(RawExportKeyProviderOperationRow.ProviderCleanupReference), nameof(RawExportKeyProviderOperationRow.ProviderCleanupReceipt), nameof(RawExportKeyProviderOperationRow.ProviderAbsenceProofReceipt) })
            entity.Property<string?>(p).HasMaxLength(512);
        entity.Property(x => x.ProviderOperationToken).HasMaxLength(43);
        entity.HasIndex(x => new { x.KeyProviderId, x.ProviderOperationToken }).IsUnique().HasDatabaseName("uq_raw_export_key_provider_op_provider_token");
        entity.HasIndex(x => new { x.AttemptKeyReservationId, x.PreparationFence }).IsUnique().HasDatabaseName("uq_raw_export_key_provider_op_reservation_fence");
        entity.HasOne<RawExportAttemptKeyReservationRow>().WithMany().HasForeignKey(x => x.AttemptKeyReservationId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_raw_export_key_provider_op_reservation");
    }
}
