using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportFixtureKekWrapJournalConfig
    : IEntityTypeConfiguration<RawExportFixtureKekWrapJournalRow>
{
    public void Configure(EntityTypeBuilder<RawExportFixtureKekWrapJournalRow> entity)
    {
        entity.ToTable("raw_export_fixture_kek_wrap_journal", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_fixture_kek_wrap_shape", """
                "KeyProviderId" = 'fixture-kek-provider-v1'
                AND "ProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$'
                AND octet_length("AttemptKeyContextFingerprint") = 32
                AND "WrappingSuiteId" = 'AES-256-GCM'
                AND "WrappingSuiteVersion" = 1
                AND octet_length("WrappedDekCiphertext") = 32
                AND octet_length("WrappedDekNonce") = 12
                AND octet_length("WrappedDekTag") = 16
                AND octet_length("WrappedDekMetadataDigest") = 32
                AND "ProviderResourceReference" = 'fixture-wrap:' || replace("FixtureWrapId"::text, '-', '')
                """);
            table.HasCheckConstraint("ck_raw_export_fixture_kek_wrap_text", """
                "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                  AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                AND "WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC)
                  AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'
                AND "ProviderResourceReference"=btrim("ProviderResourceReference") AND "ProviderResourceReference"=normalize("ProviderResourceReference",NFC)
                  AND octet_length("ProviderResourceReference") BETWEEN 1 AND 512 AND "ProviderResourceReference" !~ '[\x00-\x1f\x7f]'
                AND "ProviderOperationReceipt"=btrim("ProviderOperationReceipt") AND "ProviderOperationReceipt"=normalize("ProviderOperationReceipt",NFC)
                  AND octet_length("ProviderOperationReceipt") BETWEEN 1 AND 512 AND "ProviderOperationReceipt" !~ '[\x00-\x1f\x7f]'
                """);
        });
        entity.HasKey(x => x.FixtureWrapId).HasName("pk_raw_export_fixture_kek_wrap_journal");
        entity.Property(x => x.FixtureWrapId).ValueGeneratedNever();
        entity.Property(x => x.KeyProviderId).HasMaxLength(512);
        entity.Property(x => x.ProviderOperationToken).HasMaxLength(43);
        entity.Property(x => x.WrappingSuiteId).HasMaxLength(512);
        entity.Property(x => x.ProviderResourceReference).HasMaxLength(512);
        entity.Property(x => x.ProviderOperationReceipt).HasMaxLength(512);
        entity.HasIndex(x => new { x.KeyProviderId, x.ProviderOperationToken })
            .IsUnique().HasDatabaseName("uq_raw_export_fixture_kek_provider_token");
        entity.HasIndex(x => x.ProviderResourceReference)
            .IsUnique().HasDatabaseName("uq_raw_export_fixture_kek_resource_ref");
    }
}
