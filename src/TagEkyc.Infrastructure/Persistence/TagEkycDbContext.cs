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

    public DbSet<ApiKeyRow> ApiKeys => Set<ApiKeyRow>();

    public DbSet<RawExportPolicyVersionRow> RawExportPolicyVersions => Set<RawExportPolicyVersionRow>();

    public DbSet<RawExportPolicyAllowedClassRow> RawExportPolicyAllowedClasses => Set<RawExportPolicyAllowedClassRow>();

    public DbSet<RawExportPolicyRequirementRow> RawExportPolicyRequirements => Set<RawExportPolicyRequirementRow>();

    public DbSet<RawExportPolicyClosureRow> RawExportPolicyClosures => Set<RawExportPolicyClosureRow>();

    public DbSet<RawExportRequirementRuleSetRow> RawExportRequirementRuleSets => Set<RawExportRequirementRuleSetRow>();

    public DbSet<RawExportRequirementRuleRow> RawExportRequirementRules => Set<RawExportRequirementRuleRow>();

    public DbSet<RawExportGrantRow> RawExportGrants => Set<RawExportGrantRow>();

    public DbSet<RawExportControlAuthorityRow> RawExportControlAuthorities => Set<RawExportControlAuthorityRow>();

    public DbSet<RawExportFulfillmentRow> RawExportFulfillments => Set<RawExportFulfillmentRow>();

    public DbSet<RawExportPolicyLifecycleRow> RawExportPolicyLifecycles => Set<RawExportPolicyLifecycleRow>();

    public DbSet<RawExportSubjectConsentEventRow> RawExportSubjectConsentEvents => Set<RawExportSubjectConsentEventRow>();

    public DbSet<RawExportSubjectConsentClassRow> RawExportSubjectConsentClasses => Set<RawExportSubjectConsentClassRow>();

    public DbSet<RawExportSubjectConsentAuthorityRow> RawExportSubjectConsentAuthorities => Set<RawExportSubjectConsentAuthorityRow>();

    public DbSet<RawExportAuthorizationDecisionRow> RawExportAuthorizationDecisions => Set<RawExportAuthorizationDecisionRow>();
    public DbSet<RawExportAuthorizationIdempotencyRow> RawExportAuthorizationIdempotency => Set<RawExportAuthorizationIdempotencyRow>();
    public DbSet<RawExportDecisionEligibilityCauseRow> RawExportDecisionEligibilityCauses => Set<RawExportDecisionEligibilityCauseRow>();
    public DbSet<RawExportDecisionFulfillmentRefRow> RawExportDecisionFulfillmentRefs => Set<RawExportDecisionFulfillmentRefRow>();
    public DbSet<RawExportDecisionClassRow> RawExportDecisionClasses => Set<RawExportDecisionClassRow>();
    public DbSet<RawExportAuthorizationPermitRow> RawExportAuthorizationPermits => Set<RawExportAuthorizationPermitRow>();
    public DbSet<RawExportPermitClassRow> RawExportPermitClasses => Set<RawExportPermitClassRow>();

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

        modelBuilder.Entity<ApiKeyRow>(entity =>
        {
            entity.ToTable("api_keys", table =>
                table.HasCheckConstraint("CK_api_keys_KeyHash_Length", "octet_length(\"KeyHash\") = 32"));
#pragma warning disable CS0618
            entity.UseXminAsConcurrencyToken();
#pragma warning restore CS0618
            entity.HasKey(row => row.ApiKeyId);
            entity.Property(row => row.PrincipalId).IsRequired();
            entity.Property(row => row.CredentialRef).HasMaxLength(128).IsRequired();
            entity.Property(row => row.CredentialType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CredentialStatus).HasMaxLength(64).IsRequired();
            entity.Property(row => row.KeyPrefix).HasMaxLength(16).IsRequired();
            entity.Property(row => row.KeyHash).HasColumnType("bytea").IsRequired();
            entity.Property(row => row.ScopesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.CallerCategory).HasMaxLength(64).IsRequired();
            entity.Property(row => row.AllowedClientApplicationIdsJson).HasColumnType("jsonb");
            entity.Property(row => row.AllowedCaptureAgentIdsJson).HasColumnType("jsonb");
            entity.Property(row => row.OAuthClientId).HasMaxLength(128);
            entity.Property(row => row.MtlsSubjectDn).HasMaxLength(512);
            entity.HasIndex(row => row.KeyPrefix).IsUnique();
        });

        modelBuilder.Entity<RawExportRequirementRuleSetRow>(entity =>
        {
            entity.ToTable("raw_export_requirement_rule_sets", table =>
            {
                table.HasCheckConstraint("CK_raw_export_requirement_rule_sets_RuleSetId", "\"RuleSetId\" = 'RAW_EXPORT_REQUIREMENTS'");
                table.HasCheckConstraint("CK_raw_export_requirement_rule_sets_HomeJurisdictionCode", "\"HomeJurisdictionCode\" ~ '^[A-Z]{2}$'");
            });
            entity.HasKey(row => new { row.RuleSetId, row.RuleSetVersion });
            entity.Property(row => row.RuleSetId).HasMaxLength(64).IsRequired();
            entity.Property(row => row.MigrationRef).HasMaxLength(128).IsRequired();
            entity.Property(row => row.HomeJurisdictionCode).HasMaxLength(2).IsRequired();
        });

        modelBuilder.Entity<RawExportRequirementRuleRow>(entity =>
        {
            entity.ToTable("raw_export_requirement_rules", table =>
            {
                table.HasCheckConstraint("CK_raw_export_requirement_rules_RuleSetId", "\"RuleSetId\" = 'RAW_EXPORT_REQUIREMENTS'");
                table.HasCheckConstraint("CK_raw_export_requirement_rules_RuleSelector", "\"RuleSelector\" IN ('Always','ModeEquals','ConsentRequired','AnyJurisdictionForeign')");
                table.HasCheckConstraint("CK_raw_export_requirement_rules_RequirementType", "\"RequirementType\" IN ('LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule')");
                table.HasCheckConstraint("CK_raw_export_requirement_rules_SelectorOperand", @"(
                    ""RuleSelector"" = 'ModeEquals' AND ""SelectorOperand"" IN ('ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained')
                ) OR (
                    ""RuleSelector"" IN ('Always','ConsentRequired','AnyJurisdictionForeign') AND ""SelectorOperand"" IS NULL
                )");
            });
            entity.HasKey(row => row.Id);
            entity.Property(row => row.RuleSetId).HasMaxLength(64).IsRequired();
            entity.Property(row => row.RuleSelector).HasMaxLength(64).IsRequired();
            entity.Property(row => row.SelectorOperand).HasMaxLength(64);
            entity.Property(row => row.RequirementType).HasMaxLength(64).IsRequired();
            entity.HasOne<RawExportRequirementRuleSetRow>()
                .WithMany()
                .HasForeignKey(row => new { row.RuleSetId, row.RuleSetVersion })
                .OnDelete(DeleteBehavior.Restrict);
            var ruleSetId = entity.Metadata.FindProperty(nameof(RawExportRequirementRuleRow.RuleSetId));
            var ruleSetVersion = entity.Metadata.FindProperty(nameof(RawExportRequirementRuleRow.RuleSetVersion));
            if (ruleSetId is not null && ruleSetVersion is not null)
            {
                var conventionIndex = entity.Metadata.FindIndex([ruleSetId, ruleSetVersion]);
                if (conventionIndex is not null)
                {
                    entity.Metadata.RemoveIndex(conventionIndex);
                }
            }
        });

        modelBuilder.Entity<RawExportPolicyVersionRow>(entity =>
        {
            entity.ToTable("raw_export_policy_versions", table =>
            {
                table.HasCheckConstraint("CK_raw_export_policy_versions_Mode", "\"Mode\" IN ('ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained')");
                table.HasCheckConstraint("CK_raw_export_policy_versions_ConsentRequirement", "\"ConsentRequirement\" IN ('Required','NotRequired')");
                table.HasCheckConstraint("CK_raw_export_policy_versions_Retention", @"(
                    ""Mode"" = 'ExternalExportOnlyNoRetain' AND ""RetentionProfileRef"" IS NULL
                ) OR (
                    ""Mode"" IN ('EncryptedExportPacket','EncryptedRawVaultRetained') AND ""RetentionProfileRef"" IS NOT NULL
                )");
            });
            entity.HasKey(row => new { row.PolicyId, row.PolicyVersion });
            entity.Property(row => row.Mode).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Purpose).HasMaxLength(128).IsRequired();
            entity.Property(row => row.RetentionProfileRef).HasMaxLength(128);
            entity.Property(row => row.RetentionPurposeCode).HasMaxLength(128);
            entity.Property(row => row.ConsentRequirement).HasMaxLength(64).IsRequired();
            entity.Property(row => row.RecipientCategory).HasMaxLength(128);
            entity.Property(row => row.RecipientAssuranceRequirement).HasMaxLength(128);
            entity.Property(row => row.ControllerRole).HasMaxLength(128);
            entity.Property(row => row.ControllerEntityRef).HasMaxLength(256);
            entity.Property(row => row.ControllerJurisdiction).HasMaxLength(2);
            entity.Property(row => row.RecipientJurisdiction).HasMaxLength(2);
            entity.Property(row => row.ProcessingInfrastructureJurisdiction).HasMaxLength(2);
            entity.Property(row => row.TransferScenarioCode).HasMaxLength(128);
            entity.Property(row => row.TransferLegalBasisCode).HasMaxLength(128);
            entity.Property(row => row.RequirementRuleSetId).HasMaxLength(64).IsRequired();
            entity.Property(row => row.PermitTtlSeconds).IsRequired(false);
            entity.HasOne<RawExportRequirementRuleSetRow>()
                .WithMany()
                .HasForeignKey(row => new { row.RequirementRuleSetId, row.RequirementRuleSetVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportPolicyAllowedClassRow>(entity =>
        {
            entity.ToTable("raw_export_policy_allowed_classes", table =>
                table.HasCheckConstraint("CK_raw_export_policy_allowed_classes_RawClass", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')"));
            entity.HasKey(row => new { row.PolicyId, row.PolicyVersion, row.RawClass });
            entity.Property(row => row.RawClass).HasMaxLength(64).IsRequired();
            entity.HasOne<RawExportPolicyVersionRow>()
                .WithMany()
                .HasForeignKey(row => new { row.PolicyId, row.PolicyVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportPolicyRequirementRow>(entity =>
        {
            entity.ToTable("raw_export_policy_requirements", table =>
                table.HasCheckConstraint("CK_raw_export_policy_requirements_RequirementType", "\"RequirementType\" IN ('LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule')"));
            entity.HasKey(row => new { row.PolicyId, row.PolicyVersion, row.RequirementType });
            entity.Property(row => row.RequirementType).HasMaxLength(64).IsRequired();
            entity.HasOne<RawExportPolicyVersionRow>()
                .WithMany()
                .HasForeignKey(row => new { row.PolicyId, row.PolicyVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportPolicyClosureRow>(entity =>
        {
            entity.ToTable("raw_export_policy_closures", table =>
            {
                table.HasCheckConstraint("CK_raw_export_policy_closures_ClosureType", "\"ClosureType\" IN ('CatalogApproved','Abandoned')");
                table.HasCheckConstraint("CK_raw_export_policy_closures_Principal_NotBlank", "btrim(\"ClosedByPrincipalId\") <> ''");
                table.HasCheckConstraint("CK_raw_export_policy_closures_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
            });
            entity.HasKey(row => new { row.PolicyId, row.PolicyVersion });
            entity.Property(row => row.ClosureType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ClosedByPrincipalId).HasMaxLength(128).IsRequired();
            entity.Property(row => row.DecisionRef).HasMaxLength(256).IsRequired();
            entity.HasOne<RawExportPolicyVersionRow>()
                .WithOne()
                .HasForeignKey<RawExportPolicyClosureRow>(row => new { row.PolicyId, row.PolicyVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportGrantRow>(entity =>
        {
            entity.ToTable("raw_export_grants", table =>
            {
                table.HasCheckConstraint("CK_raw_export_grants_EventType", "\"EventType\" IN ('Granted','Revoked')");
                table.HasCheckConstraint("CK_raw_export_grants_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
            });
            entity.HasKey(row => new { row.PrincipalId, row.PolicyId, row.PolicyVersion, row.Revision });
            entity.Property(row => row.EventType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.DecisionRef).HasMaxLength(256).IsRequired();
            entity.HasOne<RawExportPolicyVersionRow>()
                .WithMany()
                .HasForeignKey(row => new { row.PolicyId, row.PolicyVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportControlAuthorityRow>(entity =>
        {
            entity.ToTable("raw_export_control_authorities", table =>
            {
                table.HasCheckConstraint("CK_raw_export_control_authorities_AuthorityType", "\"AuthorityType\" IN ('GrantAdmin','RecorderAuthorityAdmin','ActivationAuthority','FulfillmentRecorder')");
                table.HasCheckConstraint("CK_raw_export_control_authorities_ScopeType", "\"ScopeType\" IN ('Policy','Global')");
                table.HasCheckConstraint("CK_raw_export_control_authorities_EventType", "\"EventType\" IN ('Granted','Revoked')");
                table.HasCheckConstraint("CK_raw_export_control_authorities_ScopeShape", @"(
                    ""ScopeType"" = 'Global' AND ""ScopeId"" IS NULL
                ) OR (
                    ""ScopeType"" = 'Policy' AND ""ScopeId"" IS NOT NULL
                )");
                table.HasCheckConstraint("CK_raw_export_control_authorities_RequirementShape", @"(
                    ""AuthorityType"" = 'FulfillmentRecorder'
                    AND ""RequirementType"" IN ('LegalApproval','Dpia','CrossBorderAssessment','RetentionSchedule')
                ) OR (
                    ""AuthorityType"" <> 'FulfillmentRecorder'
                    AND ""RequirementType"" IS NULL
                )");
                table.HasCheckConstraint("CK_raw_export_control_authorities_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
            });
            entity.HasKey(row => row.AuthorityEventId);
            entity.Property(row => row.AuthorityType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ScopeType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.RequirementType).HasMaxLength(64);
            entity.Property(row => row.EventType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.DecisionRef).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<RawExportFulfillmentRow>(entity =>
        {
            entity.ToTable("raw_export_fulfillments", table =>
            {
                table.HasCheckConstraint("CK_raw_export_fulfillments_RequirementType", "\"RequirementType\" IN ('LegalApproval','Dpia','CrossBorderAssessment','RetentionSchedule')");
                table.HasCheckConstraint("CK_raw_export_fulfillments_EventType", "\"EventType\" IN ('Accepted','Withdrawn')");
                table.HasCheckConstraint("CK_raw_export_fulfillments_EventShape", @"(
                    ""EventType"" = 'Accepted'
                    AND ""ArtifactRef"" IS NOT NULL
                    AND btrim(""ArtifactRef"") <> ''
                    AND ""ArtifactVersion"" IS NOT NULL
                    AND btrim(""ArtifactVersion"") <> ''
                    AND ""ValidFromUtc"" IS NOT NULL
                    AND ""TargetRevision"" IS NULL
                ) OR (
                    ""EventType"" = 'Withdrawn'
                    AND ""TargetRevision"" IS NOT NULL
                    AND ""ArtifactRef"" IS NULL
                    AND ""ArtifactVersion"" IS NULL
                    AND ""ValidFromUtc"" IS NULL
                    AND ""ValidUntilUtc"" IS NULL
                    AND ""SupersedesRevision"" IS NULL
                )");
                table.HasCheckConstraint("CK_raw_export_fulfillments_Validity", "\"ValidUntilUtc\" IS NULL OR \"ValidFromUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\"");
                table.HasCheckConstraint("CK_raw_export_fulfillments_DecisionRef_NotBlank", "btrim(\"FulfillmentDecisionRef\") <> ''");
            });
            entity.HasKey(row => row.FulfillmentEventId);
            entity.Property(row => row.RequirementType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EventType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ArtifactRef).HasMaxLength(256);
            entity.Property(row => row.ArtifactVersion).HasMaxLength(128);
            entity.Property(row => row.FulfillmentDecisionRef).HasMaxLength(256).IsRequired();
            entity.HasIndex(row => new { row.PolicyId, row.PolicyVersion, row.RequirementType, row.Revision }).IsUnique();
            entity.HasOne<RawExportPolicyVersionRow>()
                .WithMany()
                .HasForeignKey(row => new { row.PolicyId, row.PolicyVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportPolicyLifecycleRow>(entity =>
        {
            entity.ToTable("raw_export_policy_lifecycle", table =>
            {
                table.HasCheckConstraint("CK_raw_export_policy_lifecycle_EventType", "\"EventType\" IN ('Activated','Suspended','Revoked')");
                table.HasCheckConstraint("CK_raw_export_policy_lifecycle_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
            });
            entity.HasKey(row => new { row.PolicyId, row.PolicyVersion, row.Revision });
            entity.Property(row => row.EventType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.DecisionRef).HasMaxLength(256).IsRequired();
            entity.HasOne<RawExportPolicyVersionRow>()
                .WithMany()
                .HasForeignKey(row => new { row.PolicyId, row.PolicyVersion })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportSubjectConsentEventRow>(entity =>
        {
            entity.ToTable("raw_export_subject_consent_events", table =>
            {
                table.HasCheckConstraint("CK_raw_export_subject_consent_events_EventType", "\"EventType\" IN ('Granted','Withdrawn')");
                table.HasCheckConstraint("CK_raw_export_subject_consent_events_PurposeCode", "\"PurposeCode\" = 'SubjectRawBiometricExport'");
                table.HasCheckConstraint("CK_raw_export_subject_consent_events_HashLength", "octet_length(\"ConsentScopeHash\") = 32");
                table.HasCheckConstraint(
                    "CK_raw_export_subject_consent_events_EventShape",
                    """
                    (
                        "EventType" = 'Granted'
                        AND "TargetRevision" IS NULL
                        AND "ConsentTextVersion" IS NOT NULL AND btrim("ConsentTextVersion") <> ''
                        AND "ConsentTextContentHash" IS NOT NULL AND btrim("ConsentTextContentHash") <> ''
                        AND "ExternalConsentArtifactRef" IS NOT NULL AND btrim("ExternalConsentArtifactRef") <> ''
                        AND ("DecisionRef" IS NULL OR btrim("DecisionRef") <> '')
                        AND "ValidFromUtc" IS NOT NULL
                        AND "CapturedAtUtc" IS NOT NULL
                        AND "CapturedByPrincipalId" IS NOT NULL
                        AND "WithdrawnByPrincipalId" IS NULL
                        AND ("ValidUntilUtc" IS NULL OR "ValidUntilUtc" > "ValidFromUtc")
                    ) OR (
                        "EventType" = 'Withdrawn'
                        AND "TargetRevision" IS NOT NULL
                        AND ("DecisionRef" IS NULL OR btrim("DecisionRef") <> '')
                        AND "ConsentTextVersion" IS NULL
                        AND "ConsentTextContentHash" IS NULL
                        AND "ValidFromUtc" IS NULL
                        AND "ValidUntilUtc" IS NULL
                        AND "CapturedAtUtc" IS NULL
                        AND "CapturedByPrincipalId" IS NULL
                        AND "WithdrawnByPrincipalId" IS NOT NULL
                    )
                    """);
            });
            entity.HasKey(row => row.SubjectConsentRecordId);
            entity.HasIndex(row => new
            {
                row.VerificationSessionId,
                row.SubjectRef,
                row.PolicyId,
                row.PolicyVersion,
                row.PurposeCode,
                row.RecipientClientApplicationId,
                row.Revision,
            }).IsUnique();
            entity.Property(row => row.SubjectRef).HasMaxLength(256).IsRequired();
            entity.Property(row => row.EventType).HasMaxLength(32).IsRequired();
            entity.Property(row => row.PurposeCode).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ConsentTextVersion).HasMaxLength(128);
            entity.Property(row => row.ConsentTextContentHash).HasMaxLength(256);
            entity.Property(row => row.ExternalConsentArtifactRef).HasMaxLength(512);
            entity.Property(row => row.DecisionRef).HasMaxLength(256);
            entity.Property(row => row.RecordedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RawExportSubjectConsentClassRow>(entity =>
        {
            entity.ToTable("raw_export_subject_consent_classes", table =>
                table.HasCheckConstraint("CK_raw_export_subject_consent_classes_RawClass", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')"));
            entity.HasKey(row => new { row.SubjectConsentRecordId, row.RawClass });
            entity.Property(row => row.RawClass).HasMaxLength(64).IsRequired();
            entity.HasOne<RawExportSubjectConsentEventRow>()
                .WithMany()
                .HasForeignKey(row => row.SubjectConsentRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawExportSubjectConsentAuthorityRow>(entity =>
        {
            entity.ToTable("raw_export_subject_consent_authorities", table =>
            {
                table.HasCheckConstraint("CK_raw_export_subject_consent_authorities_AuthorityType", "\"AuthorityType\" IN ('SubjectConsentRecorder','SubjectConsentWithdrawer')");
                table.HasCheckConstraint("CK_raw_export_subject_consent_authorities_EventType", "\"EventType\" IN ('Granted','Revoked')");
                table.HasCheckConstraint("CK_raw_export_subject_consent_authorities_DecisionRef_NotBlank", "\"DecisionRef\" IS NULL OR btrim(\"DecisionRef\") <> ''");
                table.HasCheckConstraint(
                    "CK_raw_export_subject_consent_authorities_EventShape",
                    """
                    (
                        "EventType" = 'Granted'
                        AND "TargetRevision" IS NULL
                        AND "ValidFromUtc" IS NOT NULL
                        AND ("ValidUntilUtc" IS NULL OR "ValidUntilUtc" > "ValidFromUtc")
                    ) OR (
                        "EventType" = 'Revoked'
                        AND "TargetRevision" IS NOT NULL
                        AND "ValidFromUtc" IS NULL
                        AND "ValidUntilUtc" IS NULL
                    )
                    """);
            });
            entity.HasKey(row => row.AuthorityEventId);
            entity.HasIndex(row => new
            {
                row.AuthorityPrincipalId,
                row.ClientApplicationId,
                row.AuthorityType,
                row.Revision,
            }).IsUnique();
            entity.Property(row => row.AuthorityType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EventType).HasMaxLength(32).IsRequired();
            entity.Property(row => row.DecisionRef).HasMaxLength(256);
            entity.Property(row => row.RecordedByPrincipalId).IsRequired();
            entity.Property(row => row.RecordedAtUtc).IsRequired();
        });

        ConfigureRawExportAuthorization(modelBuilder);
    }

    private static void ConfigureRawExportAuthorization(ModelBuilder modelBuilder)
    {
        const string rawClassEnum = "'ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage'";
        const string eligibilityEnum = "'GrantMissing','GrantRevoked','PolicyNotActive','PolicyRevoked','PolicySuspended','NotCatalogApproved','StaleRuleSet','MissingOrInvalidFulfillment'";

        modelBuilder.Entity<RawExportAuthorizationDecisionRow>(entity =>
        {
            entity.ToTable("raw_export_authorization_decisions", table =>
            {
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_row_shape", "(\"Outcome\" = 'Authorized' AND \"PrimaryCause\" IS NULL AND \"ResolvedVerificationSessionId\" IS NOT NULL AND \"SessionOwnerClientApplicationId\" IS NOT NULL AND \"SessionSubjectRef\" IS NOT NULL AND \"SessionState\" IS NOT NULL AND \"BoundRuleSetVersion\" IS NOT NULL AND \"CurrentRuleSetVersion\" IS NOT NULL AND \"EligibilityEvaluatedAtUtc\" IS NOT NULL AND \"GrantPrincipalId\" IS NOT NULL AND \"GrantPolicyId\" IS NOT NULL AND \"GrantPolicyVersion\" IS NOT NULL AND \"GrantRevision\" IS NOT NULL AND \"LifecyclePolicyId\" IS NOT NULL AND \"LifecyclePolicyVersion\" IS NOT NULL AND \"LifecycleRevision\" IS NOT NULL AND \"PurposeCode\" IS NOT NULL AND \"RecipientClientApplicationId\" IS NOT NULL AND \"ConsentScopeHash\" IS NOT NULL AND \"SubjectConsentRecordId\" IS NOT NULL AND \"ConsentRevision\" IS NOT NULL AND \"ConsentValidFromUtc\" IS NOT NULL AND \"ConsentEvaluatedAtUtc\" IS NOT NULL AND \"PolicyPermitTtlSeconds\" IS NOT NULL AND \"DecisionExpiresAtUtc\" IS NOT NULL) OR (\"Outcome\" = 'Denied' AND \"PrimaryCause\" IS NOT NULL AND \"DecisionExpiresAtUtc\" IS NULL)");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_FingerprintHash_len", "octet_length(\"FingerprintHash\") = 32");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_ConsentScopeHash_len", "\"ConsentScopeHash\" IS NULL OR octet_length(\"ConsentScopeHash\") = 32");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_PolicyVersion_range", "\"PolicyVersion\" >= 1");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_SelectionMode_enum", "\"RawClassSelectionMode\" IN ('DefaultPolicySet','ExplicitSubset')");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_Outcome_enum", "\"Outcome\" IN ('Authorized','Denied')");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_PrimaryCause_enum", "\"PrimaryCause\" IS NULL OR \"PrimaryCause\" IN ('SESSION_NOT_FOUND','SESSION_NOT_OWNED','SESSION_NOT_COMPLETED','EXPORT_ELIGIBILITY_INACTIVE','POLICY_PERMIT_TTL_INVALID','REQUESTED_RAW_CLASSES_NOT_ALLOWED','SUBJECT_CONSENT_NOT_EFFECTIVE','SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT')");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_SessionState_enum", "\"SessionState\" IS NULL OR \"SessionState\" IN ('Created','InProgress','ReadyToComplete','Completed','Expired','Cancelled','TechnicalTerminal')");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_EligibilityCause_enum", $"\"EligibilityPrimaryCause\" IS NULL OR \"EligibilityPrimaryCause\" IN ({eligibilityEnum})");
                table.HasCheckConstraint("CK_raw_export_authorization_decisions_SubjectConsentCause_enum", "\"SubjectConsentCause\" IS NULL OR \"SubjectConsentCause\" IN ('Missing','Withdrawn','Expired')");
            });
            entity.HasKey(row => row.ExportDecisionId).HasName("PK_raw_export_authorization_decisions");
            entity.HasOne<VerificationSessionRow>().WithMany().HasForeignKey(row => row.ResolvedVerificationSessionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_authorization_decisions_session");
        });

        modelBuilder.Entity<RawExportAuthorizationIdempotencyRow>(entity =>
        {
            entity.ToTable("raw_export_authorization_idempotency", table => table.HasCheckConstraint("CK_raw_export_authorization_idempotency_FingerprintHash_len", "octet_length(\"FingerprintHash\") = 32"));
            entity.HasKey(row => new { row.PrincipalId, row.ClientApplicationId, row.RequestedVerificationSessionId, row.IdempotencyKey }).HasName("PK_raw_export_authorization_idempotency");
            entity.Property(row => row.IdempotencyKey).HasMaxLength(256);
            entity.HasAlternateKey(row => row.ExportDecisionId).HasName("UQ_raw_export_authorization_idempotency_ExportDecisionId");
            entity.HasOne<RawExportAuthorizationDecisionRow>().WithMany().HasForeignKey(row => row.ExportDecisionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_authorization_idempotency_decision");
        });

        modelBuilder.Entity<RawExportDecisionEligibilityCauseRow>(entity =>
        {
            entity.ToTable("raw_export_decision_eligibility_causes", table => { table.HasCheckConstraint("CK_raw_export_decision_eligibility_causes_Ordinal_nonneg", "\"Ordinal\" >= 0"); table.HasCheckConstraint("CK_raw_export_decision_eligibility_causes_Cause_enum", $"\"Cause\" IN ({eligibilityEnum})"); });
            entity.HasKey(row => new { row.ExportDecisionId, row.Ordinal }).HasName("PK_raw_export_decision_eligibility_causes");
            entity.HasOne<RawExportAuthorizationDecisionRow>().WithMany().HasForeignKey(row => row.ExportDecisionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_decision_eligibility_causes_decision");
        });

        modelBuilder.Entity<RawExportDecisionFulfillmentRefRow>(entity =>
        {
            entity.ToTable("raw_export_decision_fulfillment_refs", table => { table.HasCheckConstraint("CK_raw_export_decision_fulfillment_refs_Ordinal_nonneg", "\"Ordinal\" >= 0"); table.HasCheckConstraint("CK_raw_export_decision_fulfillment_refs_Revision_pos", "\"Revision\" >= 1"); table.HasCheckConstraint("CK_raw_export_decision_fulfillment_refs_RequirementType_enum", "\"RequirementType\" IN ('LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule')"); });
            entity.HasKey(row => new { row.ExportDecisionId, row.RequirementType }).HasName("PK_raw_export_decision_fulfillment_refs");
            entity.HasAlternateKey(row => new { row.ExportDecisionId, row.Ordinal }).HasName("UQ_raw_export_decision_fulfillment_refs_Ordinal");
            entity.HasOne<RawExportAuthorizationDecisionRow>().WithMany().HasForeignKey(row => row.ExportDecisionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_decision_fulfillment_refs_decision");
        });

        modelBuilder.Entity<RawExportDecisionClassRow>(entity =>
        {
            entity.ToTable("raw_export_decision_classes", table => { table.HasCheckConstraint("CK_raw_export_decision_classes_Ordinal_nonneg", "\"Ordinal\" >= 0"); table.HasCheckConstraint("CK_raw_export_decision_classes_ClassKind_enum", "\"ClassKind\" IN ('PolicyAllowed','Requested','Effective','Consented','Authorized')"); table.HasCheckConstraint("CK_raw_export_decision_classes_RawClass_enum", $"\"RawClass\" IN ({rawClassEnum})"); });
            entity.HasKey(row => new { row.ExportDecisionId, row.ClassKind, row.RawClass }).HasName("PK_raw_export_decision_classes");
            entity.HasAlternateKey(row => new { row.ExportDecisionId, row.ClassKind, row.Ordinal }).HasName("UQ_raw_export_decision_classes_Ordinal");
            entity.HasOne<RawExportAuthorizationDecisionRow>().WithMany().HasForeignKey(row => row.ExportDecisionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_decision_classes_decision");
        });

        modelBuilder.Entity<RawExportAuthorizationPermitRow>(entity =>
        {
            entity.ToTable("raw_export_authorization_permits", table => { table.HasCheckConstraint("CK_raw_export_authorization_permits_PolicyVersion_range", "\"PolicyVersion\" >= 1"); table.HasCheckConstraint("CK_raw_export_authorization_permits_SchemaVersion_eq1", "\"SchemaVersion\" = 1"); });
            entity.HasKey(row => row.PermitId).HasName("PK_raw_export_authorization_permits");
            entity.HasAlternateKey(row => row.AuthorizationDecisionId).HasName("UQ_raw_export_authorization_permits_AuthorizationDecisionId");
            entity.HasOne<RawExportAuthorizationDecisionRow>().WithMany().HasForeignKey(row => row.AuthorizationDecisionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_authorization_permits_decision");
            entity.HasOne<VerificationSessionRow>().WithMany().HasForeignKey(row => row.ResolvedVerificationSessionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_authorization_permits_session");
        });

        modelBuilder.Entity<RawExportPermitClassRow>(entity =>
        {
            entity.ToTable("raw_export_permit_classes", table => { table.HasCheckConstraint("CK_raw_export_permit_classes_Ordinal_nonneg", "\"Ordinal\" >= 0"); table.HasCheckConstraint("CK_raw_export_permit_classes_RawClass_enum", $"\"RawClass\" IN ({rawClassEnum})"); });
            entity.HasKey(row => new { row.PermitId, row.RawClass }).HasName("PK_raw_export_permit_classes");
            entity.HasAlternateKey(row => new { row.PermitId, row.Ordinal }).HasName("UQ_raw_export_permit_classes_Ordinal");
            entity.HasOne<RawExportAuthorizationPermitRow>().WithMany().HasForeignKey(row => row.PermitId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_raw_export_permit_classes_permit");
        });
    }
}
