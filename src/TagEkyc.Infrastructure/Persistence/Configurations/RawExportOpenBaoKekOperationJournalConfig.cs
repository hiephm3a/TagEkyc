using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportOpenBaoKekOperationJournalConfig
    : IEntityTypeConfiguration<RawExportOpenBaoKekOperationJournalRow>
{
    public void Configure(EntityTypeBuilder<RawExportOpenBaoKekOperationJournalRow> entity)
    {
        entity.ToTable("raw_export_openbao_kek_operation_journal", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_openbao_kek_journal_values", """
                octet_length("ProviderOperationTokenDigest")=32
                AND octet_length("AttemptKeyContextFingerprint")=32
                AND "PreparationFence">=1
                AND "KekVersion">=1
                AND "JournalState" IN ('Issued','Wrapped','AbsenceProven','CleanupRequired','CleanedUp')
                AND "MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT'
                AND "MaterialRepresentationVersion"=1
                AND "WrappingSchemeVersion">=1
                AND "RowRevision">=1
                AND CASE "JournalState"
                  WHEN 'Issued' THEN "OpaqueWrappedDekPayload" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                  WHEN 'Wrapped' THEN octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL AND "ProviderCleanupReceipt" IS NULL
                  WHEN 'AbsenceProven' THEN "OpaqueWrappedDekPayload" IS NULL AND "ProviderAbsenceProofReceipt" IS NOT NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                  WHEN 'CleanupRequired' THEN octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                  WHEN 'CleanedUp' THEN "OpaqueWrappedDekPayload" IS NULL AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL
                  ELSE FALSE
                END
                """);
        });
        entity.HasKey(x => x.ProviderOperationId).HasName("pk_raw_export_openbao_kek_operation_journal");
        entity.Property(x => x.ProviderOperationId).ValueGeneratedNever();
        foreach (var property in new[]
        {
            nameof(RawExportOpenBaoKekOperationJournalRow.KeyProviderId),
            nameof(RawExportOpenBaoKekOperationJournalRow.KekId),
            nameof(RawExportOpenBaoKekOperationJournalRow.KekFingerprint),
            nameof(RawExportOpenBaoKekOperationJournalRow.MaterialRepresentationId),
            nameof(RawExportOpenBaoKekOperationJournalRow.WrappingSchemeId),
            nameof(RawExportOpenBaoKekOperationJournalRow.ProviderResourceReference),
            nameof(RawExportOpenBaoKekOperationJournalRow.ProviderOperationReceipt),
            nameof(RawExportOpenBaoKekOperationJournalRow.ProviderAbsenceProofReceipt),
            nameof(RawExportOpenBaoKekOperationJournalRow.ProviderCleanupReference),
            nameof(RawExportOpenBaoKekOperationJournalRow.ProviderCleanupReceipt),
        })
            entity.Property<string?>(property).HasMaxLength(512);
        entity.HasOne<RawExportKeyProviderOperationRow>().WithOne()
            .HasForeignKey<RawExportOpenBaoKekOperationJournalRow>(x => x.ProviderOperationId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_openbao_kek_journal_provider_operation");
    }
}
