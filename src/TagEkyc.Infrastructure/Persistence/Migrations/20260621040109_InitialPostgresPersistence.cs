using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tagekyc");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventPayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventPayloadRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "capture_artifacts",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CaptureSource = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CaptureAgentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    VaultRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ArtifactHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QualityState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RetryReasonCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capture_artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_manifests",
                schema: "tagekyc",
                columns: table => new
                {
                    EvidencePackageId = table.Column<string>(type: "text", nullable: false),
                    SessionGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PackageVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ManifestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PackageHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceRefsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AuditEventRefsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultRef = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EvidencePackageSignatureStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_manifests", x => x.EvidencePackageId);
                });

            migrationBuilder.CreateTable(
                name: "evidence_packages",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ManifestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceRefsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AuditEventRefsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultRef = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidencePackageSignatureStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_results",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationCheckId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputCaptureArtifactIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    ReasonCodesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RetryReasonCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SanitizedSummaryRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PayloadSignatureStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EngineName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EngineVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verification_decisions",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssuranceLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: true),
                    FailedChecksJson = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedChecksJson = table.Column<string>(type: "jsonb", nullable: false),
                    DecisionReasonCodesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RetryReasonCodesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verification_sessions",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Profile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequiredChecksJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BindingNonceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssuranceLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinalDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidencePackageId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidencePackageHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ManifestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PolicySnapshotId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RetentionClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeletionEligibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LegalHoldStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PurgeBlockReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AccessAuditRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_VerificationSessionId",
                schema: "tagekyc",
                table: "audit_events",
                column: "VerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_capture_artifacts_VerificationSessionId",
                schema: "tagekyc",
                table: "capture_artifacts",
                column: "VerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_manifests_SessionGuid",
                schema: "tagekyc",
                table: "evidence_manifests",
                column: "SessionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_packages_VerificationSessionId",
                schema: "tagekyc",
                table: "evidence_packages",
                column: "VerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_results_VerificationSessionId",
                schema: "tagekyc",
                table: "evidence_results",
                column: "VerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_verification_decisions_VerificationSessionId",
                schema: "tagekyc",
                table: "verification_decisions",
                column: "VerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_verification_sessions_ClientApplicationId_ExternalSessionId",
                schema: "tagekyc",
                table: "verification_sessions",
                columns: new[] { "ClientApplicationId", "ExternalSessionId" },
                unique: true,
                filter: "\"ExternalSessionId\" IS NOT NULL");

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION tagekyc.deny_append_only_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'append-only table % cannot be updated or deleted', TG_TABLE_NAME;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER tr_capture_artifacts_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.capture_artifacts
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_evidence_results_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.evidence_results
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_verification_decisions_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.verification_decisions
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_evidence_packages_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.evidence_packages
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_evidence_manifests_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.evidence_manifests
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_audit_events_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.audit_events
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS tr_capture_artifacts_append_only ON tagekyc.capture_artifacts;
                DROP TRIGGER IF EXISTS tr_evidence_results_append_only ON tagekyc.evidence_results;
                DROP TRIGGER IF EXISTS tr_verification_decisions_append_only ON tagekyc.verification_decisions;
                DROP TRIGGER IF EXISTS tr_evidence_packages_append_only ON tagekyc.evidence_packages;
                DROP TRIGGER IF EXISTS tr_evidence_manifests_append_only ON tagekyc.evidence_manifests;
                DROP TRIGGER IF EXISTS tr_audit_events_append_only ON tagekyc.audit_events;
                DROP FUNCTION IF EXISTS tagekyc.deny_append_only_mutation();
                """);

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "capture_artifacts",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "evidence_manifests",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "evidence_packages",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "evidence_results",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "verification_decisions",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "verification_sessions",
                schema: "tagekyc");
        }
    }
}
