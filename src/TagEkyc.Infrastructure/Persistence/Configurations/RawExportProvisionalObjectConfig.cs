using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence.Configurations;

public sealed class RawExportProvisionalObjectConfig
    : IEntityTypeConfiguration<RawExportProvisionalObjectRow>
{
    public void Configure(EntityTypeBuilder<RawExportProvisionalObjectRow> entity)
    {
        entity.ToTable("raw_export_provisional_objects", "tagekyc", table =>
        {
            table.HasCheckConstraint("ck_raw_export_provisional_objects_state",
                "\"State\" IN ('Initiated','PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','NoObjectEstablished','ObjectConflict','CleanupPending','Deleted','Quarantined')");
            table.HasCheckConstraint("ck_raw_export_provisional_objects_values", """
                "SchemaVersion" = 1
                AND "EncryptionAttemptRevision" >= 1
                AND "AttemptFence" >= 1
                AND "StateRevision" >= 1
                AND octet_length("EncryptionAttemptFingerprint") = 32
                AND octet_length("ObjectBindingDigest") = 32
                AND "ObjectKey" ~ '^raw-export/c1/v1/[0-9a-f]{32}$'
                AND ("CiphertextLength" IS NULL OR "CiphertextLength" BETWEEN 1 AND 134217728)
                AND ("CiphertextDigest" IS NULL OR octet_length("CiphertextDigest") = 32)
                AND ("ProviderReceiptDigest" IS NULL OR octet_length("ProviderReceiptDigest") = 32)
                AND ("VerificationEvidenceDigest" IS NULL OR octet_length("VerificationEvidenceDigest") = 32)
                AND ("CleanupEvidenceDigest" IS NULL OR octet_length("CleanupEvidenceDigest") = 32)
                AND ("DeletionEvidenceDigest" IS NULL OR octet_length("DeletionEvidenceDigest") = 32)
                AND ("QuarantineEvidenceDigest" IS NULL OR octet_length("QuarantineEvidenceDigest") = 32)
                AND ("CleanupReasonCode" IS NULL OR ("CleanupReasonCode" ~ '^[A-Za-z]+$' AND octet_length("CleanupReasonCode") <= 64))
                AND ("QuarantineReasonCode" IS NULL OR ("QuarantineReasonCode" ~ '^[A-Za-z]+$' AND octet_length("QuarantineReasonCode") <= 64))
                AND ("DeletionEvidenceKind" IS NULL OR "DeletionEvidenceKind" IN ('DeleteAcknowledged','PositiveAbsenceConfirmed'))
                """);
            table.HasCheckConstraint("ck_raw_export_provisional_objects_sparse", SparseCheck);
        });

        entity.HasKey(x => x.ObjectCustodyId).HasName("pk_raw_export_provisional_objects");
        entity.HasAlternateKey(x => x.AttemptId).HasName("uq_raw_export_provisional_objects_attempt");
        entity.HasAlternateKey(x => x.ProvisionalObjectIdentity).HasName("uq_raw_export_provisional_objects_identity");
        entity.HasAlternateKey(x => x.ObjectKey).HasName("uq_raw_export_provisional_objects_key");
        entity.HasIndex(x => new { x.AttemptId, x.AttemptKeyReservationId, x.SourceArtifactId, x.ProvisionalObjectIdentity })
            .HasDatabaseName("ix_raw_export_provisional_objects_attempt_binding");
        entity.Property(x => x.ObjectKey).HasMaxLength(49).IsRequired();
        entity.Property(x => x.State).HasMaxLength(64).IsRequired();
        entity.Property(x => x.PutOutcomeKind).HasMaxLength(64);
        entity.Property(x => x.CleanupReasonCode).HasMaxLength(64);
        entity.Property(x => x.DeletionEvidenceKind).HasMaxLength(64);
        entity.Property(x => x.QuarantineReasonCode).HasMaxLength(64);
        entity.Property(x => x.EncryptionAttemptFingerprint).HasColumnType("bytea");
        entity.Property(x => x.ObjectBindingDigest).HasColumnType("bytea");
        entity.HasOne<RawExportSourceEncryptionAttemptRow>().WithMany()
            .HasForeignKey(x => new { x.AttemptId, x.AttemptKeyReservationId, x.SourceArtifactId, x.ProvisionalObjectIdentity })
            .HasPrincipalKey(x => new { x.AttemptId, x.AttemptKeyReservationId, x.SourceArtifactId, x.ProvisionalObjectIdentity })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_raw_export_provisional_objects_attempt_binding");
    }

    private const string SparseCheck = """
        CASE "State"
          WHEN 'Initiated' THEN "PutOperationId" IS NULL AND "PutArmedAtUtc" IS NULL AND "PutOutcomeKind" IS NULL AND "OutcomeObservedAtUtc" IS NULL AND "CiphertextLength" IS NULL AND "CiphertextDigest" IS NULL AND "ProviderReceiptDigest" IS NULL AND "VerificationEvidenceDigest" IS NULL AND "VerifiedAtUtc" IS NULL AND "CleanupReasonCode" IS NULL AND "CleanupEvidenceDigest" IS NULL AND "CleanupRequestedAtUtc" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "DeletionEvidenceKind" IS NULL AND "DeletedAtUtc" IS NULL AND "QuarantineReasonCode" IS NULL AND "QuarantineEvidenceDigest" IS NULL AND "QuarantinedAtUtc" IS NULL
          WHEN 'PutInFlight' THEN "PutOperationId" IS NOT NULL AND "PutArmedAtUtc" IS NOT NULL AND "PutOutcomeKind" IS NULL AND "OutcomeObservedAtUtc" IS NULL AND "CiphertextLength" IS NULL AND "CiphertextDigest" IS NULL AND "ProviderReceiptDigest" IS NULL AND "VerificationEvidenceDigest" IS NULL AND "VerifiedAtUtc" IS NULL AND "CleanupReasonCode" IS NULL AND "CleanupEvidenceDigest" IS NULL AND "CleanupRequestedAtUtc" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "DeletionEvidenceKind" IS NULL AND "DeletedAtUtc" IS NULL AND "QuarantineReasonCode" IS NULL AND "QuarantineEvidenceDigest" IS NULL AND "QuarantinedAtUtc" IS NULL
          WHEN 'PutOutcomeUnknown' THEN "PutOperationId" IS NOT NULL AND "PutArmedAtUtc" IS NOT NULL AND "PutOutcomeKind" IN ('OutcomeUnknown','ConditionalConflictObserved') AND "OutcomeObservedAtUtc" IS NOT NULL AND "CiphertextLength" IS NULL AND "CiphertextDigest" IS NULL AND ("ProviderReceiptDigest" IS NULL OR "PutOutcomeKind"='ConditionalConflictObserved') AND "VerificationEvidenceDigest" IS NULL AND "VerifiedAtUtc" IS NULL AND "CleanupReasonCode" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'ObjectPresentPendingVerification' THEN "PutOperationId" IS NOT NULL AND "PutArmedAtUtc" IS NOT NULL AND "PutOutcomeKind" IN ('Created','RecoveredPresent') AND "OutcomeObservedAtUtc" IS NOT NULL AND "CiphertextLength" IS NOT NULL AND "CiphertextDigest" IS NOT NULL AND "ProviderReceiptDigest" IS NOT NULL AND "VerificationEvidenceDigest" IS NULL AND "VerifiedAtUtc" IS NULL AND "CleanupReasonCode" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'VerifiedCompleted' THEN "PutOperationId" IS NOT NULL AND "PutOutcomeKind" IN ('Created','RecoveredPresent') AND "CiphertextLength" IS NOT NULL AND "CiphertextDigest" IS NOT NULL AND "ProviderReceiptDigest" IS NOT NULL AND "VerificationEvidenceDigest" IS NOT NULL AND "VerifiedAtUtc" IS NOT NULL AND "CleanupReasonCode" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'NoObjectEstablished' THEN (("PutOperationId" IS NULL AND "PutArmedAtUtc" IS NULL AND "PutOutcomeKind"='NotArmed') OR ("PutOperationId" IS NOT NULL AND "PutArmedAtUtc" IS NOT NULL AND "PutOutcomeKind"='PositiveAbsence')) AND "OutcomeObservedAtUtc" IS NOT NULL AND "CiphertextLength" IS NULL AND "CiphertextDigest" IS NULL AND "ProviderReceiptDigest" IS NULL AND "VerificationEvidenceDigest" IS NULL AND "CleanupReasonCode" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'ObjectConflict' THEN "PutOperationId" IS NOT NULL AND "PutOutcomeKind"='RecoveredMismatch' AND "OutcomeObservedAtUtc" IS NOT NULL AND (("CiphertextLength" IS NULL AND "CiphertextDigest" IS NULL) OR ("CiphertextLength" IS NOT NULL AND "CiphertextDigest" IS NOT NULL)) AND "ProviderReceiptDigest" IS NOT NULL AND "VerificationEvidenceDigest" IS NULL AND "CleanupReasonCode" IS NULL AND "DeletionEvidenceDigest" IS NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'CleanupPending' THEN "PutOperationId" IS NOT NULL AND "CiphertextLength" IS NOT NULL AND "CiphertextDigest" IS NOT NULL AND "ProviderReceiptDigest" IS NOT NULL AND "CleanupReasonCode" IS NOT NULL AND "CleanupEvidenceDigest" IS NOT NULL AND "CleanupRequestedAtUtc" IS NOT NULL AND "DeletionEvidenceDigest" IS NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'Deleted' THEN "PutOperationId" IS NOT NULL AND "CleanupReasonCode" IS NOT NULL AND "CleanupEvidenceDigest" IS NOT NULL AND "CleanupRequestedAtUtc" IS NOT NULL AND "DeletionEvidenceDigest" IS NOT NULL AND "DeletionEvidenceKind" IS NOT NULL AND "DeletedAtUtc" IS NOT NULL AND "QuarantineEvidenceDigest" IS NULL
          WHEN 'Quarantined' THEN "PutOperationId" IS NOT NULL AND "ProviderReceiptDigest" IS NOT NULL AND "QuarantineReasonCode" IS NOT NULL AND "QuarantineEvidenceDigest" IS NOT NULL AND "QuarantinedAtUtc" IS NOT NULL AND "DeletionEvidenceDigest" IS NULL
          ELSE false
        END
        """;
}
