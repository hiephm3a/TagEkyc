using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B31RawExportAuthorizationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_authorization_decisions",
                schema: "tagekyc",
                columns: table => new
                {
                    ExportDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedVerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    FingerprintHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    RawClassSelectionMode = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    PrimaryCause = table.Column<string>(type: "text", nullable: true),
                    ResolvedVerificationSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionOwnerClientApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionSubjectRef = table.Column<string>(type: "text", nullable: true),
                    SessionState = table.Column<string>(type: "text", nullable: true),
                    BoundRuleSetVersion = table.Column<int>(type: "integer", nullable: true),
                    CurrentRuleSetVersion = table.Column<int>(type: "integer", nullable: true),
                    EligibilityPrimaryCause = table.Column<string>(type: "text", nullable: true),
                    EligibilityEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GrantPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantPolicyVersion = table.Column<int>(type: "integer", nullable: true),
                    GrantRevision = table.Column<int>(type: "integer", nullable: true),
                    LifecyclePolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    LifecyclePolicyVersion = table.Column<int>(type: "integer", nullable: true),
                    LifecycleRevision = table.Column<int>(type: "integer", nullable: true),
                    PurposeCode = table.Column<string>(type: "text", nullable: true),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectConsentCause = table.Column<string>(type: "text", nullable: true),
                    ConsentScopeHash = table.Column<byte[]>(type: "bytea", nullable: true),
                    SubjectConsentRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsentRevision = table.Column<int>(type: "integer", nullable: true),
                    ConsentValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsentEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PolicyPermitTtlSeconds = table.Column<int>(type: "integer", nullable: true),
                    DecisionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_authorization_decisions", x => x.ExportDecisionId);
                    table.CheckConstraint("CK_raw_export_authorization_decisions_ConsentScopeHash_len", "\"ConsentScopeHash\" IS NULL OR octet_length(\"ConsentScopeHash\") = 32");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_EligibilityCause_enum", "\"EligibilityPrimaryCause\" IS NULL OR \"EligibilityPrimaryCause\" IN ('GrantMissing','GrantRevoked','PolicyNotActive','PolicyRevoked','PolicySuspended','NotCatalogApproved','StaleRuleSet','MissingOrInvalidFulfillment')");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_FingerprintHash_len", "octet_length(\"FingerprintHash\") = 32");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_Outcome_enum", "\"Outcome\" IN ('Authorized','Denied')");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_PolicyVersion_range", "\"PolicyVersion\" >= 1");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_PrimaryCause_enum", "\"PrimaryCause\" IS NULL OR \"PrimaryCause\" IN ('SESSION_NOT_FOUND','SESSION_NOT_OWNED','SESSION_NOT_COMPLETED','EXPORT_ELIGIBILITY_INACTIVE','POLICY_PERMIT_TTL_INVALID','REQUESTED_RAW_CLASSES_NOT_ALLOWED','SUBJECT_CONSENT_NOT_EFFECTIVE','SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT')");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_row_shape", "(\"Outcome\" = 'Authorized' AND \"PrimaryCause\" IS NULL AND \"ResolvedVerificationSessionId\" IS NOT NULL AND \"SessionOwnerClientApplicationId\" IS NOT NULL AND \"SessionSubjectRef\" IS NOT NULL AND \"SessionState\" IS NOT NULL AND \"BoundRuleSetVersion\" IS NOT NULL AND \"CurrentRuleSetVersion\" IS NOT NULL AND \"EligibilityEvaluatedAtUtc\" IS NOT NULL AND \"GrantPrincipalId\" IS NOT NULL AND \"GrantPolicyId\" IS NOT NULL AND \"GrantPolicyVersion\" IS NOT NULL AND \"GrantRevision\" IS NOT NULL AND \"LifecyclePolicyId\" IS NOT NULL AND \"LifecyclePolicyVersion\" IS NOT NULL AND \"LifecycleRevision\" IS NOT NULL AND \"PurposeCode\" IS NOT NULL AND \"RecipientClientApplicationId\" IS NOT NULL AND \"ConsentScopeHash\" IS NOT NULL AND \"SubjectConsentRecordId\" IS NOT NULL AND \"ConsentRevision\" IS NOT NULL AND \"ConsentValidFromUtc\" IS NOT NULL AND \"ConsentEvaluatedAtUtc\" IS NOT NULL AND \"PolicyPermitTtlSeconds\" IS NOT NULL AND \"DecisionExpiresAtUtc\" IS NOT NULL) OR (\"Outcome\" = 'Denied' AND \"PrimaryCause\" IS NOT NULL AND \"DecisionExpiresAtUtc\" IS NULL)");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_SelectionMode_enum", "\"RawClassSelectionMode\" IN ('DefaultPolicySet','ExplicitSubset')");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_SessionState_enum", "\"SessionState\" IS NULL OR \"SessionState\" IN ('Created','InProgress','ReadyToComplete','Completed','Expired','Cancelled','TechnicalTerminal')");
                    table.CheckConstraint("CK_raw_export_authorization_decisions_SubjectConsentCause_enum", "\"SubjectConsentCause\" IS NULL OR \"SubjectConsentCause\" IN ('Missing','Withdrawn','Expired')");
                    table.ForeignKey(
                        name: "FK_raw_export_authorization_decisions_session",
                        column: x => x.ResolvedVerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_authorization_idempotency",
                schema: "tagekyc",
                columns: table => new
                {
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedVerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FingerprintHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    ExportDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_authorization_idempotency", x => new { x.PrincipalId, x.ClientApplicationId, x.RequestedVerificationSessionId, x.IdempotencyKey });
                    table.CheckConstraint("CK_raw_export_authorization_idempotency_FingerprintHash_len", "octet_length(\"FingerprintHash\") = 32");
                    table.ForeignKey(
                        name: "FK_raw_export_authorization_idempotency_decision",
                        column: x => x.ExportDecisionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_decisions",
                        principalColumn: "ExportDecisionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_authorization_permits",
                schema: "tagekyc",
                columns: table => new
                {
                    PermitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedVerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRef = table.Column<string>(type: "text", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    PurposeCode = table.Column<string>(type: "text", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_authorization_permits", x => x.PermitId);
                    table.CheckConstraint("CK_raw_export_authorization_permits_PolicyVersion_range", "\"PolicyVersion\" >= 1");
                    table.CheckConstraint("CK_raw_export_authorization_permits_SchemaVersion_eq1", "\"SchemaVersion\" = 1");
                    table.ForeignKey(
                        name: "FK_raw_export_authorization_permits_decision",
                        column: x => x.AuthorizationDecisionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_decisions",
                        principalColumn: "ExportDecisionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_raw_export_authorization_permits_session",
                        column: x => x.ResolvedVerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_decision_classes",
                schema: "tagekyc",
                columns: table => new
                {
                    ExportDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassKind = table.Column<string>(type: "text", nullable: false),
                    RawClass = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_decision_classes", x => new { x.ExportDecisionId, x.ClassKind, x.RawClass });
                    table.CheckConstraint("CK_raw_export_decision_classes_ClassKind_enum", "\"ClassKind\" IN ('PolicyAllowed','Requested','Effective','Consented','Authorized')");
                    table.CheckConstraint("CK_raw_export_decision_classes_Ordinal_nonneg", "\"Ordinal\" >= 0");
                    table.CheckConstraint("CK_raw_export_decision_classes_RawClass_enum", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')");
                    table.ForeignKey(
                        name: "FK_raw_export_decision_classes_decision",
                        column: x => x.ExportDecisionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_decisions",
                        principalColumn: "ExportDecisionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_decision_eligibility_causes",
                schema: "tagekyc",
                columns: table => new
                {
                    ExportDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Cause = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_decision_eligibility_causes", x => new { x.ExportDecisionId, x.Ordinal });
                    table.CheckConstraint("CK_raw_export_decision_eligibility_causes_Cause_enum", "\"Cause\" IN ('GrantMissing','GrantRevoked','PolicyNotActive','PolicyRevoked','PolicySuspended','NotCatalogApproved','StaleRuleSet','MissingOrInvalidFulfillment')");
                    table.CheckConstraint("CK_raw_export_decision_eligibility_causes_Ordinal_nonneg", "\"Ordinal\" >= 0");
                    table.ForeignKey(
                        name: "FK_raw_export_decision_eligibility_causes_decision",
                        column: x => x.ExportDecisionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_decisions",
                        principalColumn: "ExportDecisionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_decision_fulfillment_refs",
                schema: "tagekyc",
                columns: table => new
                {
                    ExportDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementType = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    FulfillmentEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ArtifactRef = table.Column<string>(type: "text", nullable: false),
                    ArtifactVersion = table.Column<string>(type: "text", nullable: false),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_decision_fulfillment_refs", x => new { x.ExportDecisionId, x.RequirementType });
                    table.CheckConstraint("CK_raw_export_decision_fulfillment_refs_Ordinal_nonneg", "\"Ordinal\" >= 0");
                    table.CheckConstraint("CK_raw_export_decision_fulfillment_refs_RequirementType_enum", "\"RequirementType\" IN ('LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule')");
                    table.CheckConstraint("CK_raw_export_decision_fulfillment_refs_Revision_pos", "\"Revision\" >= 1");
                    table.ForeignKey(
                        name: "FK_raw_export_decision_fulfillment_refs_decision",
                        column: x => x.ExportDecisionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_decisions",
                        principalColumn: "ExportDecisionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_permit_classes",
                schema: "tagekyc",
                columns: table => new
                {
                    PermitId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawClass = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_permit_classes", x => new { x.PermitId, x.RawClass });
                    table.CheckConstraint("CK_raw_export_permit_classes_Ordinal_nonneg", "\"Ordinal\" >= 0");
                    table.CheckConstraint("CK_raw_export_permit_classes_RawClass_enum", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')");
                    table.ForeignKey(
                        name: "FK_raw_export_permit_classes_permit",
                        column: x => x.PermitId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_permits",
                        principalColumn: "PermitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_authorization_decisions_ResolvedVerificationSess~",
                schema: "tagekyc",
                table: "raw_export_authorization_decisions",
                column: "ResolvedVerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "UQ_raw_export_authorization_idempotency_ExportDecisionId",
                schema: "tagekyc",
                table: "raw_export_authorization_idempotency",
                column: "ExportDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_authorization_permits_ResolvedVerificationSessio~",
                schema: "tagekyc",
                table: "raw_export_authorization_permits",
                column: "ResolvedVerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "UQ_raw_export_authorization_permits_AuthorizationDecisionId",
                schema: "tagekyc",
                table: "raw_export_authorization_permits",
                column: "AuthorizationDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_raw_export_decision_classes_Ordinal",
                schema: "tagekyc",
                table: "raw_export_decision_classes",
                columns: new[] { "ExportDecisionId", "ClassKind", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_raw_export_decision_fulfillment_refs_Ordinal",
                schema: "tagekyc",
                table: "raw_export_decision_fulfillment_refs",
                columns: new[] { "ExportDecisionId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_raw_export_permit_classes_Ordinal",
                schema: "tagekyc",
                table: "raw_export_permit_classes",
                columns: new[] { "PermitId", "Ordinal" },
                unique: true);

            migrationBuilder.DropIndex(name: "UQ_raw_export_permit_classes_Ordinal", schema: "tagekyc", table: "raw_export_permit_classes");
            migrationBuilder.DropIndex(name: "UQ_raw_export_decision_fulfillment_refs_Ordinal", schema: "tagekyc", table: "raw_export_decision_fulfillment_refs");
            migrationBuilder.DropIndex(name: "UQ_raw_export_decision_classes_Ordinal", schema: "tagekyc", table: "raw_export_decision_classes");
            migrationBuilder.DropIndex(name: "UQ_raw_export_authorization_permits_AuthorizationDecisionId", schema: "tagekyc", table: "raw_export_authorization_permits");
            migrationBuilder.DropIndex(name: "UQ_raw_export_authorization_idempotency_ExportDecisionId", schema: "tagekyc", table: "raw_export_authorization_idempotency");
            migrationBuilder.AddUniqueConstraint(name: "UQ_raw_export_permit_classes_Ordinal", schema: "tagekyc", table: "raw_export_permit_classes", columns: new[] { "PermitId", "Ordinal" });
            migrationBuilder.AddUniqueConstraint(name: "UQ_raw_export_decision_fulfillment_refs_Ordinal", schema: "tagekyc", table: "raw_export_decision_fulfillment_refs", columns: new[] { "ExportDecisionId", "Ordinal" });
            migrationBuilder.AddUniqueConstraint(name: "UQ_raw_export_decision_classes_Ordinal", schema: "tagekyc", table: "raw_export_decision_classes", columns: new[] { "ExportDecisionId", "ClassKind", "Ordinal" });
            migrationBuilder.AddUniqueConstraint(name: "UQ_raw_export_authorization_permits_AuthorizationDecisionId", schema: "tagekyc", table: "raw_export_authorization_permits", column: "AuthorizationDecisionId");
            migrationBuilder.AddUniqueConstraint(name: "UQ_raw_export_authorization_idempotency_ExportDecisionId", schema: "tagekyc", table: "raw_export_authorization_idempotency", column: "ExportDecisionId");

            migrationBuilder.Sql(
                """
                ALTER TABLE tagekyc.raw_export_authorization_idempotency
                    DROP CONSTRAINT "FK_raw_export_authorization_idempotency_decision",
                    ADD CONSTRAINT "FK_raw_export_authorization_idempotency_decision"
                    FOREIGN KEY ("ExportDecisionId")
                    REFERENCES tagekyc.raw_export_authorization_decisions("ExportDecisionId")
                    ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED;

                CREATE FUNCTION tagekyc.enforce_raw_export_authorization_insert()
                RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog AS $$
                DECLARE expected_context text; insert_context text;
                BEGIN
                    expected_context := TG_ARGV[0];
                    insert_context := pg_catalog.current_setting('tagekyc.raw_export_authorization_insert_context', true);
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR insert_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_DIRECT_INSERT_UNSUPPORTED';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction()
                RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog AS $$
                DECLARE parent_xmin xid;
                BEGIN
                    IF TG_TABLE_NAME = 'raw_export_authorization_permits' THEN
                        SELECT xmin INTO parent_xmin
                        FROM tagekyc.raw_export_authorization_decisions
                        WHERE "ExportDecisionId" = NEW."AuthorizationDecisionId";
                    ELSE
                        SELECT xmin INTO parent_xmin
                        FROM tagekyc.raw_export_authorization_decisions
                        WHERE "ExportDecisionId" = NEW."ExportDecisionId";
                    END IF;
                    IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_DECISION_PARENT_MISSING'; END IF;
                    IF parent_xmin <> pg_catalog.pg_current_xact_id()::xid THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_CHILD_APPEND_UNSUPPORTED';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE FUNCTION tagekyc.enforce_raw_export_permit_child_same_transaction()
                RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog AS $$
                DECLARE parent_xmin xid;
                BEGIN
                    SELECT xmin INTO parent_xmin
                    FROM tagekyc.raw_export_authorization_permits
                    WHERE "PermitId" = NEW."PermitId";
                    IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PERMIT_PARENT_MISSING'; END IF;
                    IF parent_xmin <> pg_catalog.pg_current_xact_id()::xid THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_CHILD_APPEND_UNSUPPORTED';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE FUNCTION tagekyc.enforce_raw_export_permit_has_classes()
                RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_permit_classes
                        WHERE "PermitId" = NEW."PermitId"
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORIZATION_PERMIT_CLASSES_REQUIRED';
                    END IF;
                    RETURN NULL;
                END;
                $$;

                CREATE TRIGGER tr_b3_authz_idempotency_insert
                BEFORE INSERT ON tagekyc.raw_export_authorization_idempotency
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('idempotency');
                CREATE TRIGGER tr_b3_authz_decisions_insert
                BEFORE INSERT ON tagekyc.raw_export_authorization_decisions
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('decision');
                CREATE TRIGGER tr_b3_authz_eligibility_causes_insert
                BEFORE INSERT ON tagekyc.raw_export_decision_eligibility_causes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('eligibility_cause');
                CREATE TRIGGER tr_b3_authz_fulfillment_refs_insert
                BEFORE INSERT ON tagekyc.raw_export_decision_fulfillment_refs
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('fulfillment_ref');
                CREATE TRIGGER tr_b3_authz_decision_classes_insert
                BEFORE INSERT ON tagekyc.raw_export_decision_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('decision_class');
                CREATE TRIGGER tr_b3_authz_permits_insert
                BEFORE INSERT ON tagekyc.raw_export_authorization_permits
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('permit');
                CREATE TRIGGER tr_b3_authz_permit_classes_insert
                BEFORE INSERT ON tagekyc.raw_export_permit_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_authorization_insert('permit_class');

                CREATE TRIGGER tr_b3_authz_eligibility_causes_xmin
                BEFORE INSERT ON tagekyc.raw_export_decision_eligibility_causes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction();
                CREATE TRIGGER tr_b3_authz_fulfillment_refs_xmin
                BEFORE INSERT ON tagekyc.raw_export_decision_fulfillment_refs
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction();
                CREATE TRIGGER tr_b3_authz_decision_classes_xmin
                BEFORE INSERT ON tagekyc.raw_export_decision_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction();
                CREATE TRIGGER tr_b3_authz_permits_xmin
                BEFORE INSERT ON tagekyc.raw_export_authorization_permits
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction();
                CREATE TRIGGER tr_b3_authz_permit_classes_xmin
                BEFORE INSERT ON tagekyc.raw_export_permit_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_permit_child_same_transaction();
                CREATE CONSTRAINT TRIGGER tr_b3_authz_permit_has_classes
                AFTER INSERT ON tagekyc.raw_export_authorization_permits
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                EXECUTE FUNCTION tagekyc.enforce_raw_export_permit_has_classes();

                CREATE TRIGGER tr_b3_authz_idempotency_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_authorization_idempotency FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_b3_authz_decisions_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_authorization_decisions FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_b3_authz_eligibility_causes_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_decision_eligibility_causes FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_b3_authz_fulfillment_refs_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_decision_fulfillment_refs FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_b3_authz_decision_classes_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_decision_classes FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_b3_authz_permits_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_authorization_permits FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_b3_authz_permit_classes_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_permit_classes FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                ALTER TABLE tagekyc.raw_export_authorization_idempotency OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_authorization_decisions OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_decision_eligibility_causes OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_decision_fulfillment_refs OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_decision_classes OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_authorization_permits OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_permit_classes OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_authorization_insert() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_permit_child_same_transaction() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_permit_has_classes() OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON TABLE
                    tagekyc.raw_export_authorization_idempotency,
                    tagekyc.raw_export_authorization_decisions,
                    tagekyc.raw_export_decision_eligibility_causes,
                    tagekyc.raw_export_decision_fulfillment_refs,
                    tagekyc.raw_export_decision_classes,
                    tagekyc.raw_export_authorization_permits,
                    tagekyc.raw_export_permit_classes
                FROM PUBLIC, tagekyc_runtime;
                GRANT SELECT ON TABLE
                    tagekyc.raw_export_authorization_idempotency,
                    tagekyc.raw_export_authorization_decisions,
                    tagekyc.raw_export_decision_eligibility_causes,
                    tagekyc.raw_export_decision_fulfillment_refs,
                    tagekyc.raw_export_decision_classes,
                    tagekyc.raw_export_authorization_permits,
                    tagekyc.raw_export_permit_classes
                TO tagekyc_runtime;
                REVOKE ALL ON FUNCTION
                    tagekyc.enforce_raw_export_authorization_insert(),
                    tagekyc.enforce_raw_export_decision_child_same_transaction(),
                    tagekyc.enforce_raw_export_permit_child_same_transaction(),
                    tagekyc.enforce_raw_export_permit_has_classes()
                FROM PUBLIC, tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE SELECT ON TABLE
                    tagekyc.raw_export_authorization_idempotency,
                    tagekyc.raw_export_authorization_decisions,
                    tagekyc.raw_export_decision_eligibility_causes,
                    tagekyc.raw_export_decision_fulfillment_refs,
                    tagekyc.raw_export_decision_classes,
                    tagekyc.raw_export_authorization_permits,
                    tagekyc.raw_export_permit_classes
                FROM tagekyc_runtime;
                """);

            migrationBuilder.DropTable(
                name: "raw_export_authorization_idempotency",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_decision_classes",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_decision_eligibility_causes",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_decision_fulfillment_refs",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_permit_classes",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_authorization_permits",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_authorization_decisions",
                schema: "tagekyc");

            migrationBuilder.Sql(
                """
                DROP FUNCTION tagekyc.enforce_raw_export_permit_has_classes();
                DROP FUNCTION tagekyc.enforce_raw_export_permit_child_same_transaction();
                DROP FUNCTION tagekyc.enforce_raw_export_decision_child_same_transaction();
                DROP FUNCTION tagekyc.enforce_raw_export_authorization_insert();
                """);

        }
    }
}
