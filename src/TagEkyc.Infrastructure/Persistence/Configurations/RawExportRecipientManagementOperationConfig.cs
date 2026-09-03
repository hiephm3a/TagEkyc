using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportRecipientManagementOperationConfig
    : IEntityTypeConfiguration<RawExportRecipientManagementOperationRow>
{
    public void Configure(EntityTypeBuilder<RawExportRecipientManagementOperationRow> entity)
    {
        entity.ToTable("raw_export_recipient_management_operations", table =>
        {
            table.HasCheckConstraint("ck_raw_export_recipient_management_operation_shape", """
                "OperationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "ManagerApiKeyId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "ManagerPrincipalId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND "RecipientClientApplicationId" <> '00000000-0000-0000-0000-000000000000'::uuid
                AND octet_length("IdempotencyKeyDigest")=32
                AND octet_length("EqualityFingerprint")=32
                AND octet_length("PayloadDigest")=32
                AND "OperationKind" IN ('EnrollRecipient','IssueCredential','ReplaceCredential','RevokeCredential','EnrollKey','RotateKey','RevokeKey')
                """);
            table.HasCheckConstraint("ck_raw_export_recipient_management_operation_sparse", """
                ("Outcome" IS NULL AND "ResultSnapshot" IS NULL AND "CompletedAtUtc" IS NULL
                 AND "ResultIdentityRevision" IS NULL AND "ResultApiKeyId" IS NULL
                 AND "ResultCredentialVersion" IS NULL AND "ResultKeyId" IS NULL
                 AND "ResultKeyVersion" IS NULL AND "ResultRevision" IS NULL
                 AND "AuthorizedDeliveryCount" IS NULL AND "StreamingDeliveryCount" IS NULL
                 AND "InterruptedDeliveryCount" IS NULL)
                OR
                ("Outcome" IS NOT NULL AND "ResultSnapshot" IS NOT NULL
                 AND "CompletedAtUtc" IS NOT NULL AND "AdmissionAtUtc" IS NOT NULL
                 AND (
                   ("OperationKind"='EnrollRecipient' AND "Outcome"='Created' AND "ResultIdentityRevision"=1
                    AND "ResultApiKeyId" IS NULL AND "ResultCredentialVersion" IS NULL AND "ResultKeyId" IS NULL
                    AND "ResultKeyVersion" IS NULL AND "ResultRevision" IS NULL
                    AND "AuthorizedDeliveryCount" IS NULL AND "StreamingDeliveryCount" IS NULL AND "InterruptedDeliveryCount" IS NULL)
                   OR
                   ("OperationKind" IN ('IssueCredential','ReplaceCredential')
                    AND "Outcome" IN ('Created','Replaced') AND "ResultIdentityRevision" IS NULL
                    AND "ResultApiKeyId" IS NOT NULL AND "ResultCredentialVersion" IS NOT NULL
                    AND "ResultKeyId" IS NULL AND "ResultKeyVersion" IS NULL AND "ResultRevision"=1
                    AND "AuthorizedDeliveryCount" IS NULL AND "StreamingDeliveryCount" IS NULL AND "InterruptedDeliveryCount" IS NULL)
                   OR
                   ("OperationKind"='RevokeCredential' AND "Outcome"='Revoked' AND "ResultIdentityRevision" IS NULL
                    AND "ResultApiKeyId" IS NOT NULL AND "ResultCredentialVersion" IS NOT NULL
                    AND "ResultKeyId" IS NULL AND "ResultKeyVersion" IS NULL AND "ResultRevision">1
                    AND "AuthorizedDeliveryCount" IS NULL AND "StreamingDeliveryCount" IS NULL AND "InterruptedDeliveryCount" IS NULL)
                   OR
                   ("OperationKind" IN ('EnrollKey','RotateKey') AND "Outcome" IN ('Created','Rotated')
                    AND "ResultIdentityRevision" IS NULL AND "ResultApiKeyId" IS NULL AND "ResultCredentialVersion" IS NULL
                    AND "ResultKeyId" IS NOT NULL AND "ResultKeyVersion" IS NOT NULL AND "ResultRevision"=1
                    AND "AuthorizedDeliveryCount" IS NULL AND "StreamingDeliveryCount" IS NULL AND "InterruptedDeliveryCount" IS NULL)
                   OR
                   ("OperationKind"='RevokeKey' AND "Outcome"='Revoked'
                    AND "ResultIdentityRevision" IS NULL AND "ResultApiKeyId" IS NULL AND "ResultCredentialVersion" IS NULL
                    AND "ResultKeyId" IS NOT NULL AND "ResultKeyVersion" IS NOT NULL AND "ResultRevision">1
                    AND "AuthorizedDeliveryCount">=0 AND "StreamingDeliveryCount">=0 AND "InterruptedDeliveryCount">=0)
                 ))
                """);
        });
        entity.HasKey(row => row.OperationId).HasName("pk_raw_export_recipient_management_operation");
        entity.Property(row => row.IdempotencyKeyDigest).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.EqualityFingerprint).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.PayloadDigest).HasColumnType("bytea").IsRequired();
        entity.Property(row => row.OperationKind).HasMaxLength(32).IsRequired();
        entity.Property(row => row.Outcome).HasMaxLength(48);
        entity.Property(row => row.ResultKeyId).HasMaxLength(128);
        entity.Property(row => row.ResultSnapshot).HasColumnType("jsonb");
        entity.HasIndex(row => new { row.ManagerPrincipalId, row.IdempotencyKeyDigest })
            .IsUnique().HasDatabaseName("uq_raw_export_recipient_management_idempotency");
    }
}
