using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence;

// SQL-owned graph: the A3 migration owns deferred constraints, write fences and ACLs.
public sealed class RawSourceConsentReferenceRowConfiguration : IEntityTypeConfiguration<RawSourceConsentReferenceRow>
{
    public void Configure(EntityTypeBuilder<RawSourceConsentReferenceRow> builder)
    {
        builder.ToTable("raw_source_consent_references", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(x => x.ConsentReferenceId).HasName("PK_a3_consent_reference");
        builder.Property(x => x.ConsentReferenceId).HasColumnType("uuid").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.ClientApplicationId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.SubjectRef).HasColumnType("text").IsRequired();
        builder.Property(x => x.ExternalConsentArtifactRef).HasColumnType("varchar(512)").IsRequired();
        builder.Property(x => x.CurrentRevision).HasColumnType("bigint").IsRequired();
    }
}

public sealed class RawSourceConsentReferenceEventRowConfiguration : IEntityTypeConfiguration<RawSourceConsentReferenceEventRow>
{
    public void Configure(EntityTypeBuilder<RawSourceConsentReferenceEventRow> builder)
    {
        builder.ToTable("raw_source_consent_reference_events", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(x => new { x.ConsentReferenceId, x.Revision }).HasName("PK_a3_consent_reference_event");
        builder.Property(x => x.ConsentReferenceId).HasColumnType("uuid").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.Revision).HasColumnType("bigint").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.EventType).HasColumnType("varchar(16)").IsRequired();
        builder.Property(x => x.SourceVersion).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.ConsentTextVersion).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.ConsentTextContentHash).HasColumnType("varchar(256)").IsRequired();
        builder.Property(x => x.ValidFromUtc).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ValidUntilUtc).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RecordedByPrincipalId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.RecordedAtUtc).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.OperationDomain).HasColumnType("varchar(16)").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.RequestFingerprint).HasColumnType("bytea").IsRequired();
        builder.Property(x => x.DecisionRef).HasColumnType("varchar(256)");
    }
}

public sealed class RawSourceConsentBindingRowConfiguration : IEntityTypeConfiguration<RawSourceConsentBindingRow>
{
    public void Configure(EntityTypeBuilder<RawSourceConsentBindingRow> builder)
    {
        builder.ToTable("raw_source_consent_bindings", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(x => x.ConsentBindingId).HasName("PK_a3_consent_binding");
        builder.Property(x => x.ConsentBindingId).HasColumnType("uuid").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.ConsentReferenceId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.ConsentReferenceRevision).HasColumnType("bigint").IsRequired();
        builder.Property(x => x.PrincipalId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.ClientApplicationId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.VerificationSessionId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.SubjectRef).HasColumnType("text").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.RequestFingerprint).HasColumnType("bytea").IsRequired();
        builder.Property(x => x.RecordedAtUtc).HasColumnType("timestamptz").IsRequired();
    }
}

public sealed class RawSourceRetentionPermitRowConfiguration : IEntityTypeConfiguration<RawSourceRetentionPermitRow>
{
    public void Configure(EntityTypeBuilder<RawSourceRetentionPermitRow> builder)
    {
        builder.ToTable("raw_source_retention_permits", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(x => new { x.RetentionAuthorityId, x.Revision }).HasName("PK_a3_retention_permit");
        builder.Property(x => x.RetentionAuthorityId).HasColumnType("uuid").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.Revision).HasColumnType("bigint").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.ConsentBindingId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.PrincipalId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.ClientApplicationId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.VerificationSessionId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.PolicyId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.PolicyVersion).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Purpose).HasColumnType("varchar(32)").IsRequired();
        builder.Property(x => x.IssuedAtUtc).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ControllerIdentity).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.StableDataScopeId).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.RetentionPolicyId).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.RetentionPolicyVersion).HasColumnType("integer").IsRequired();
        builder.Property(x => x.RetentionClass).HasColumnType("varchar(64)").IsRequired();
        builder.Property(x => x.RetentionStartEvent).HasColumnType("varchar(32)").IsRequired();
        builder.Property(x => x.RevocationPolicyId).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.PurgePolicyId).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.LegalHoldPolicyId).HasColumnType("varchar(128)").IsRequired();
        builder.Property(x => x.MaximumRetentionSeconds).HasColumnType("integer").IsRequired();
        builder.Property(x => x.IssueOperationId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.RequestFingerprint).HasColumnType("bytea").IsRequired();
    }
}

public sealed class RawSourceRetentionPermitClassRowConfiguration : IEntityTypeConfiguration<RawSourceRetentionPermitClassRow>
{
    public void Configure(EntityTypeBuilder<RawSourceRetentionPermitClassRow> builder)
    {
        builder.ToTable("raw_source_retention_permit_classes", "tagekyc", table => table.ExcludeFromMigrations());
        builder.HasKey(x => new { x.RetentionAuthorityId, x.Revision, x.RawClass }).HasName("PK_a3_retention_permit_class");
        builder.Property(x => x.RetentionAuthorityId).HasColumnType("uuid").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.Revision).HasColumnType("bigint").IsRequired().ValueGeneratedNever();
        builder.Property(x => x.RawClass).HasColumnType("varchar(32)").IsRequired().ValueGeneratedNever();
    }
}
