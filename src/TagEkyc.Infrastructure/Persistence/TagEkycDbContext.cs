using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class TagEkycDbContext(DbContextOptions<TagEkycDbContext> options) : DbContext(options)
{
    public DbSet<VerificationSessionRow> Sessions => Set<VerificationSessionRow>();

    public DbSet<CaptureArtifactRow> CaptureArtifacts => Set<CaptureArtifactRow>();

    public DbSet<EvidenceResultRow> EvidenceResults => Set<EvidenceResultRow>();

    public DbSet<VerificationDecisionRow> VerificationDecisions => Set<VerificationDecisionRow>();

    public DbSet<EvidencePackageRow> EvidencePackages => Set<EvidencePackageRow>();

    public DbSet<EvidenceManifestRow> EvidenceManifests => Set<EvidenceManifestRow>();

    public DbSet<AuditEventRow> AuditEvents => Set<AuditEventRow>();

    public DbSet<AppendIdempotencyRecordRow> AppendIdempotencyRecords => Set<AppendIdempotencyRecordRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tagekyc");

        modelBuilder.Entity<VerificationSessionRow>(entity =>
        {
            entity.ToTable("verification_sessions");
#pragma warning disable CS0618
            entity.UseXminAsConcurrencyToken();
#pragma warning restore CS0618
            entity.HasKey(row => row.Id);
            entity.Property(row => row.SubjectRef).HasMaxLength(256).IsRequired();
            entity.Property(row => row.Profile).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Purpose).HasMaxLength(128).IsRequired();
            entity.Property(row => row.RequiredChecksJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.ExternalSessionId).HasMaxLength(128);
            entity.Property(row => row.ExternalTransactionId).HasMaxLength(128);
            entity.Property(row => row.BindingNonceHash).HasMaxLength(128);
            entity.Property(row => row.RequestId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.State).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Result).HasMaxLength(64).IsRequired();
            entity.Property(row => row.AssuranceLevel).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EvidencePackageHash).HasMaxLength(128);
            entity.Property(row => row.ManifestHash).HasMaxLength(128);
            entity.Property(row => row.PolicySnapshotId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.RetentionClass).HasMaxLength(64).IsRequired();
            entity.Property(row => row.DeletionEligibility).HasMaxLength(64).IsRequired();
            entity.Property(row => row.LegalHoldStatus).HasMaxLength(64).IsRequired();
            entity.Property(row => row.PurgeBlockReason).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ActorId).HasMaxLength(128);
            entity.Property(row => row.ActorCategory).HasMaxLength(64);
            entity.HasIndex(row => new { row.ClientApplicationId, row.ExternalSessionId })
                .IsUnique()
                .HasFilter("\"ExternalSessionId\" IS NOT NULL");
        });

        modelBuilder.Entity<CaptureArtifactRow>(entity =>
        {
            entity.ToTable("capture_artifacts");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.ArtifactType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CaptureSource).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CaptureAgentId).HasMaxLength(128);
            entity.Property(row => row.DeviceId).HasMaxLength(128);
            entity.Property(row => row.VaultRef).HasMaxLength(256);
            entity.Property(row => row.ArtifactHash).HasMaxLength(128);
            entity.Property(row => row.MetadataHash).HasMaxLength(128);
            entity.Property(row => row.QualityState).HasMaxLength(64).IsRequired();
            entity.Property(row => row.RetryReasonCode).HasMaxLength(128);
            entity.Property(row => row.RequestId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.CorrelationId).HasMaxLength(128).IsRequired();
            entity.HasIndex(row => row.VerificationSessionId);
            entity.HasOne<VerificationSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.VerificationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceResultRow>(entity =>
        {
            entity.ToTable("evidence_results");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.ResultType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.InputCaptureArtifactIdsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.Result).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ReasonCodesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.RetryReasonCode).HasMaxLength(128);
            entity.Property(row => row.SanitizedSummaryRef).HasMaxLength(512);
            entity.Property(row => row.PayloadHash).HasMaxLength(128);
            entity.Property(row => row.PayloadSignatureStatus).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EngineName).HasMaxLength(128).IsRequired();
            entity.Property(row => row.EngineVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.RequestId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.CorrelationId).HasMaxLength(128).IsRequired();
            entity.HasIndex(row => row.VerificationSessionId);
            entity.HasOne<VerificationSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.VerificationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VerificationDecisionRow>(entity =>
        {
            entity.ToTable("verification_decisions");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Result).HasMaxLength(64).IsRequired();
            entity.Property(row => row.AssuranceLevel).HasMaxLength(64).IsRequired();
            entity.Property(row => row.FailedChecksJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.CompletedChecksJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.DecisionReasonCodesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.RetryReasonCodesJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(row => row.VerificationSessionId);
            entity.HasOne<VerificationSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.VerificationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidencePackageRow>(entity =>
        {
            entity.ToTable("evidence_packages");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.PackageVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CanonicalizationScheme).HasMaxLength(64).IsRequired();
            entity.Property(row => row.HashAlgorithm).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ManifestHash).HasMaxLength(128).IsRequired();
            entity.Property(row => row.EvidenceRefsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.AuditEventRefsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.PackageHash).HasMaxLength(128).IsRequired();
            entity.Property(row => row.EvidencePackageSignatureStatus).HasMaxLength(64).IsRequired();
            entity.HasIndex(row => row.VerificationSessionId);
            entity.HasOne<VerificationSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.VerificationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceManifestRow>(entity =>
        {
            entity.ToTable("evidence_manifests");
            entity.HasKey(row => row.EvidencePackageId);
            entity.Property(row => row.VerificationSessionId).HasMaxLength(32).IsRequired();
            entity.Property(row => row.PackageVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CanonicalizationScheme).HasMaxLength(64).IsRequired();
            entity.Property(row => row.HashAlgorithm).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ManifestHash).HasMaxLength(128).IsRequired();
            entity.Property(row => row.PackageHash).HasMaxLength(128).IsRequired();
            entity.Property(row => row.EvidenceRefsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.AuditEventRefsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.EvidencePackageSignatureStatus).HasMaxLength(64).IsRequired();
            entity.Property(row => row.SignatureFormat).HasMaxLength(32);
            entity.Property(row => row.SignatureScheme).HasMaxLength(64);
            entity.Property(row => row.SignatureAlgorithm).HasMaxLength(64);
            entity.Property(row => row.KeyId).HasMaxLength(128);
            entity.Property(row => row.SignatureValue);
            entity.Property(row => row.PublicKeyJwk).HasMaxLength(1024);
            entity.Property(row => row.PublicKeyFingerprint).HasMaxLength(128);
            entity.HasIndex(row => row.SessionGuid);
            entity.HasOne<EvidencePackageRow>()
                .WithOne()
                .HasForeignKey<EvidenceManifestRow>(row => row.EvidencePackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEventRow>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.ActorType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ActorId).HasMaxLength(128);
            entity.Property(row => row.EventType).HasMaxLength(128).IsRequired();
            entity.Property(row => row.EventPayloadHash).HasMaxLength(128).IsRequired();
            entity.Property(row => row.EventPayloadRef).HasMaxLength(512);
            entity.Property(row => row.RequestId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.CorrelationId).HasMaxLength(128).IsRequired();
            entity.HasIndex(row => row.VerificationSessionId);
            entity.HasOne<VerificationSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.VerificationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppendIdempotencyRecordRow>(entity =>
        {
            entity.ToTable("append_idempotency_records");
            entity.HasKey(row => new { row.VerificationSessionId, row.IdempotencyKey });
            entity.Property(row => row.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.Property(row => row.EndpointKind).HasMaxLength(64).IsRequired();
            entity.Property(row => row.SubmissionSlot).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Fingerprint).HasMaxLength(128).IsRequired();
            entity.HasIndex(row => new { row.VerificationSessionId, row.IdempotencyKey }).IsUnique();
            entity.HasIndex(row => row.MintedId);
            entity.HasOne<VerificationSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.VerificationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
