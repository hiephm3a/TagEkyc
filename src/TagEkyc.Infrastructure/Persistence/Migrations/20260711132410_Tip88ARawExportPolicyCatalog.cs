using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88ARawExportPolicyCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_requirement_rule_sets",
                schema: "tagekyc",
                columns: table => new
                {
                    RuleSetId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleSetVersion = table.Column<int>(type: "integer", nullable: false),
                    MigrationRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HomeJurisdictionCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_requirement_rule_sets", x => new { x.RuleSetId, x.RuleSetVersion });
                    table.CheckConstraint("CK_raw_export_requirement_rule_sets_HomeJurisdictionCode", "\"HomeJurisdictionCode\" ~ '^[A-Z]{2}$'");
                    table.CheckConstraint("CK_raw_export_requirement_rule_sets_RuleSetId", "\"RuleSetId\" = 'RAW_EXPORT_REQUIREMENTS'");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_policy_versions",
                schema: "tagekyc",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RetentionProfileRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RetentionPurposeCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConsentRequirement = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecipientCategory = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RecipientAssuranceRequirement = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ControllerRole = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ControllerEntityRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ControllerJurisdiction = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    RecipientJurisdiction = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ProcessingInfrastructureJurisdiction = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    TransferScenarioCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TransferLegalBasisCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequirementRuleSetId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequirementRuleSetVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_policy_versions", x => new { x.PolicyId, x.PolicyVersion });
                    table.CheckConstraint("CK_raw_export_policy_versions_ConsentRequirement", "\"ConsentRequirement\" IN ('Required','NotRequired')");
                    table.CheckConstraint("CK_raw_export_policy_versions_Mode", "\"Mode\" IN ('ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained')");
                    table.CheckConstraint("CK_raw_export_policy_versions_Retention", "(\n                    \"Mode\" = 'ExternalExportOnlyNoRetain' AND \"RetentionProfileRef\" IS NULL\n                ) OR (\n                    \"Mode\" IN ('EncryptedExportPacket','EncryptedRawVaultRetained') AND \"RetentionProfileRef\" IS NOT NULL\n                )");
                    table.ForeignKey(
                        name: "FK_raw_export_policy_versions_raw_export_requirement_rule_sets~",
                        columns: x => new { x.RequirementRuleSetId, x.RequirementRuleSetVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_requirement_rule_sets",
                        principalColumns: new[] { "RuleSetId", "RuleSetVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_requirement_rules",
                schema: "tagekyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleSetId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleSetVersion = table.Column<int>(type: "integer", nullable: false),
                    RuleSelector = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SelectorOperand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RequirementType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_requirement_rules", x => x.Id);
                    table.CheckConstraint("CK_raw_export_requirement_rules_RequirementType", "\"RequirementType\" IN ('LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule')");
                    table.CheckConstraint("CK_raw_export_requirement_rules_RuleSelector", "\"RuleSelector\" IN ('Always','ModeEquals','ConsentRequired','AnyJurisdictionForeign')");
                    table.CheckConstraint("CK_raw_export_requirement_rules_RuleSetId", "\"RuleSetId\" = 'RAW_EXPORT_REQUIREMENTS'");
                    table.CheckConstraint("CK_raw_export_requirement_rules_SelectorOperand", "(\n                    \"RuleSelector\" = 'ModeEquals' AND \"SelectorOperand\" IN ('ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained')\n                ) OR (\n                    \"RuleSelector\" IN ('Always','ConsentRequired','AnyJurisdictionForeign') AND \"SelectorOperand\" IS NULL\n                )");
                    table.ForeignKey(
                        name: "FK_raw_export_requirement_rules_raw_export_requirement_rule_se~",
                        columns: x => new { x.RuleSetId, x.RuleSetVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_requirement_rule_sets",
                        principalColumns: new[] { "RuleSetId", "RuleSetVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_policy_allowed_classes",
                schema: "tagekyc",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_policy_allowed_classes", x => new { x.PolicyId, x.PolicyVersion, x.RawClass });
                    table.CheckConstraint("CK_raw_export_policy_allowed_classes_RawClass", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')");
                    table.ForeignKey(
                        name: "FK_raw_export_policy_allowed_classes_raw_export_policy_version~",
                        columns: x => new { x.PolicyId, x.PolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_policy_closures",
                schema: "tagekyc",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    ClosureType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedByPrincipalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_policy_closures", x => new { x.PolicyId, x.PolicyVersion });
                    table.CheckConstraint("CK_raw_export_policy_closures_ClosureType", "\"ClosureType\" IN ('CatalogApproved','Abandoned')");
                    table.CheckConstraint("CK_raw_export_policy_closures_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
                    table.CheckConstraint("CK_raw_export_policy_closures_Principal_NotBlank", "btrim(\"ClosedByPrincipalId\") <> ''");
                    table.ForeignKey(
                        name: "FK_raw_export_policy_closures_raw_export_policy_versions_Polic~",
                        columns: x => new { x.PolicyId, x.PolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_policy_requirements",
                schema: "tagekyc",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    RequirementType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_policy_requirements", x => new { x.PolicyId, x.PolicyVersion, x.RequirementType });
                    table.CheckConstraint("CK_raw_export_policy_requirements_RequirementType", "\"RequirementType\" IN ('LegalApproval','ConsentArtifact','Dpia','CrossBorderAssessment','RetentionSchedule')");
                    table.ForeignKey(
                        name: "FK_raw_export_policy_requirements_raw_export_policy_versions_P~",
                        columns: x => new { x.PolicyId, x.PolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_policy_versions_RequirementRuleSetId_Requirement~",
                schema: "tagekyc",
                table: "raw_export_policy_versions",
                columns: new[] { "RequirementRuleSetId", "RequirementRuleSetVersion" });

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS tagekyc."UX_raw_export_requirement_rules_tuple";
                CREATE UNIQUE INDEX "UX_raw_export_requirement_rules_tuple"
                    ON tagekyc.raw_export_requirement_rules
                    ("RuleSetId", "RuleSetVersion", "RuleSelector", COALESCE("SelectorOperand", ''), "RequirementType");

                INSERT INTO tagekyc.raw_export_requirement_rule_sets
                    ("RuleSetId", "RuleSetVersion", "MigrationRef", "HomeJurisdictionCode", "CreatedAt")
                VALUES
                    ('RAW_EXPORT_REQUIREMENTS', 1, '20260711132410_Tip88ARawExportPolicyCatalog', 'VN', transaction_timestamp());

                INSERT INTO tagekyc.raw_export_requirement_rules
                    ("Id", "RuleSetId", "RuleSetVersion", "RuleSelector", "SelectorOperand", "RequirementType", "CreatedAt")
                VALUES
                    ('00000000-0000-5000-8000-000000088a01', 'RAW_EXPORT_REQUIREMENTS', 1, 'Always', NULL, 'LegalApproval', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088a02', 'RAW_EXPORT_REQUIREMENTS', 1, 'ModeEquals', 'EncryptedExportPacket', 'RetentionSchedule', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088a03', 'RAW_EXPORT_REQUIREMENTS', 1, 'ModeEquals', 'EncryptedRawVaultRetained', 'RetentionSchedule', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088a04', 'RAW_EXPORT_REQUIREMENTS', 1, 'ModeEquals', 'EncryptedRawVaultRetained', 'Dpia', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088a05', 'RAW_EXPORT_REQUIREMENTS', 1, 'ConsentRequired', NULL, 'ConsentArtifact', transaction_timestamp()),
                    ('00000000-0000-5000-8000-000000088a06', 'RAW_EXPORT_REQUIREMENTS', 1, 'AnyJurisdictionForeign', NULL, 'CrossBorderAssessment', transaction_timestamp());

                CREATE OR REPLACE FUNCTION tagekyc.reject_raw_export_rule_runtime_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'RAW_EXPORT_RULE_TABLE_MUTATION_UNSUPPORTED';
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_policy_version_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    latest_version integer;
                    latest_closed boolean;
                    current_rule_version integer;
                    bound_rule_count integer;
                BEGIN
                    PERFORM pg_advisory_xact_lock(hashtext(NEW."PolicyId"::text));

                    SELECT MAX("PolicyVersion")
                    INTO latest_version
                    FROM tagekyc.raw_export_policy_versions
                    WHERE "PolicyId" = NEW."PolicyId";

                    IF latest_version IS NULL THEN
                        IF NEW."PolicyVersion" <> 1 THEN
                            RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_CHAIN_INVALID_FIRST';
                        END IF;
                    ELSE
                        IF NEW."PolicyVersion" <> latest_version + 1 THEN
                            RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_CHAIN_NON_CONTIGUOUS';
                        END IF;

                        SELECT EXISTS (
                            SELECT 1
                            FROM tagekyc.raw_export_policy_closures
                            WHERE "PolicyId" = NEW."PolicyId"
                              AND "PolicyVersion" = latest_version)
                        INTO latest_closed;

                        IF NOT latest_closed THEN
                            RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_CHAIN_OPEN_DRAFT';
                        END IF;
                    END IF;

                    IF NEW."RequirementRuleSetId" <> 'RAW_EXPORT_REQUIREMENTS' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REQUIREMENT_RULE_SET_FAMILY_INVALID';
                    END IF;

                    SELECT MAX("RuleSetVersion")
                    INTO current_rule_version
                    FROM tagekyc.raw_export_requirement_rule_sets
                    WHERE "RuleSetId" = 'RAW_EXPORT_REQUIREMENTS';

                    IF current_rule_version IS NULL OR NEW."RequirementRuleSetVersion" <> current_rule_version THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REQUIREMENT_RULE_SET_NOT_CURRENT';
                    END IF;

                    SELECT COUNT(*)
                    INTO bound_rule_count
                    FROM tagekyc.raw_export_requirement_rules
                    WHERE "RuleSetId" = NEW."RequirementRuleSetId"
                      AND "RuleSetVersion" = NEW."RequirementRuleSetVersion";

                    IF bound_rule_count = 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REQUIREMENT_RULE_SET_EMPTY';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_child_same_transaction()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    parent_xmin xid;
                BEGIN
                    SELECT xmin
                    INTO parent_xmin
                    FROM tagekyc.raw_export_policy_versions
                    WHERE "PolicyId" = NEW."PolicyId"
                      AND "PolicyVersion" = NEW."PolicyVersion";

                    IF parent_xmin IS NULL THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_PARENT_MISSING';
                    END IF;

                    -- pg_current_xact_id() is xid8 (epoch-extended); ::xid truncates to the
                    -- low 32 bits, matching xmin's basis across txid wraparound.
                    IF parent_xmin <> pg_current_xact_id()::xid THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_CHILD_APPEND_UNSUPPORTED';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_policy_closure_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    version_row tagekyc.raw_export_policy_versions%ROWTYPE;
                    home_jurisdiction text;
                    actual_count integer;
                    missing_count integer;
                    extra_count integer;
                BEGIN
                    NEW."ClosedAtUtc" := transaction_timestamp();

                    SELECT *
                    INTO version_row
                    FROM tagekyc.raw_export_policy_versions
                    WHERE "PolicyId" = NEW."PolicyId"
                      AND "PolicyVersion" = NEW."PolicyVersion";

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;

                    IF NEW."ClosureType" = 'Abandoned' THEN
                        RETURN NEW;
                    END IF;

                    IF NEW."ClosureType" <> 'CatalogApproved' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_CLOSURE_TYPE_INVALID';
                    END IF;

                    IF btrim(version_row."Purpose") = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CATALOG_INCOMPLETE_PURPOSE';
                    END IF;

                    IF version_row."ControllerRole" IS NULL OR btrim(version_row."ControllerRole") = ''
                        OR version_row."ControllerEntityRef" IS NULL OR btrim(version_row."ControllerEntityRef") = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CATALOG_INCOMPLETE_CONTROLLER';
                    END IF;

                    SELECT COUNT(*)
                    INTO actual_count
                    FROM tagekyc.raw_export_policy_allowed_classes
                    WHERE "PolicyId" = NEW."PolicyId"
                      AND "PolicyVersion" = NEW."PolicyVersion";

                    IF actual_count = 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CATALOG_INCOMPLETE_ALLOWED_CLASSES';
                    END IF;

                    SELECT "HomeJurisdictionCode"
                    INTO home_jurisdiction
                    FROM tagekyc.raw_export_requirement_rule_sets
                    WHERE "RuleSetId" = version_row."RequirementRuleSetId"
                      AND "RuleSetVersion" = version_row."RequirementRuleSetVersion";

                    IF home_jurisdiction IS NULL THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REQUIREMENT_RULE_SET_NOT_FOUND';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_requirement_rules
                        WHERE "RuleSetId" = version_row."RequirementRuleSetId"
                          AND "RuleSetVersion" = version_row."RequirementRuleSetVersion"
                          AND "RuleSelector" = 'AnyJurisdictionForeign')
                       AND (
                          version_row."ControllerJurisdiction" IS NULL
                          OR version_row."RecipientJurisdiction" IS NULL
                          OR version_row."ProcessingInfrastructureJurisdiction" IS NULL) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CATALOG_INCOMPLETE_JURISDICTION';
                    END IF;

                    WITH derived AS (
                        SELECT DISTINCT r."RequirementType"
                        FROM tagekyc.raw_export_requirement_rules r
                        WHERE r."RuleSetId" = version_row."RequirementRuleSetId"
                          AND r."RuleSetVersion" = version_row."RequirementRuleSetVersion"
                          AND (
                            r."RuleSelector" = 'Always'
                            OR (r."RuleSelector" = 'ModeEquals' AND r."SelectorOperand" = version_row."Mode")
                            OR (r."RuleSelector" = 'ConsentRequired' AND version_row."ConsentRequirement" = 'Required')
                            OR (
                                r."RuleSelector" = 'AnyJurisdictionForeign'
                                AND (
                                    version_row."ControllerJurisdiction" <> home_jurisdiction
                                    OR version_row."RecipientJurisdiction" <> home_jurisdiction
                                    OR version_row."ProcessingInfrastructureJurisdiction" <> home_jurisdiction
                                )
                            )
                          )
                    ),
                    actual AS (
                        SELECT "RequirementType"
                        FROM tagekyc.raw_export_policy_requirements
                        WHERE "PolicyId" = NEW."PolicyId"
                          AND "PolicyVersion" = NEW."PolicyVersion"
                    )
                    SELECT
                        (SELECT COUNT(*) FROM (SELECT "RequirementType" FROM derived EXCEPT SELECT "RequirementType" FROM actual) missing),
                        (SELECT COUNT(*) FROM (SELECT "RequirementType" FROM actual EXCEPT SELECT "RequirementType" FROM derived) extra)
                    INTO missing_count, extra_count;

                    IF missing_count > 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CATALOG_INCOMPLETE_REQUIREMENTS';
                    END IF;

                    IF extra_count > 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CATALOG_CONTRADICTORY_REQUIREMENTS';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_raw_export_policy_versions_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_policy_versions
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_policy_allowed_classes_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_policy_allowed_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_policy_requirements_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_policy_requirements
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_policy_closures_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_policy_closures
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_requirement_rule_sets_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_requirement_rule_sets
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_requirement_rules_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_requirement_rules
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_raw_export_policy_versions_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_policy_versions
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_policy_version_insert();
                CREATE TRIGGER tr_raw_export_allowed_classes_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_policy_allowed_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_child_same_transaction();
                CREATE TRIGGER tr_raw_export_requirements_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_policy_requirements
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_child_same_transaction();
                CREATE TRIGGER tr_raw_export_policy_closures_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_policy_closures
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_policy_closure_insert();

                CREATE TRIGGER tr_raw_export_rule_sets_runtime_mutation_reject
                BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_export_requirement_rule_sets
                FOR EACH ROW EXECUTE FUNCTION tagekyc.reject_raw_export_rule_runtime_mutation();
                CREATE TRIGGER tr_raw_export_rules_runtime_mutation_reject
                BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_export_requirement_rules
                FOR EACH ROW EXECUTE FUNCTION tagekyc.reject_raw_export_rule_runtime_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_raw_export_rule_sets_runtime_mutation_reject ON tagekyc.raw_export_requirement_rule_sets;
                DROP TRIGGER IF EXISTS tr_raw_export_rules_runtime_mutation_reject ON tagekyc.raw_export_requirement_rules;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_closures_insert_guard ON tagekyc.raw_export_policy_closures;
                DROP TRIGGER IF EXISTS tr_raw_export_requirements_insert_guard ON tagekyc.raw_export_policy_requirements;
                DROP TRIGGER IF EXISTS tr_raw_export_allowed_classes_insert_guard ON tagekyc.raw_export_policy_allowed_classes;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_versions_insert_guard ON tagekyc.raw_export_policy_versions;
                DROP TRIGGER IF EXISTS tr_raw_export_requirement_rules_append_only ON tagekyc.raw_export_requirement_rules;
                DROP TRIGGER IF EXISTS tr_raw_export_requirement_rule_sets_append_only ON tagekyc.raw_export_requirement_rule_sets;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_closures_append_only ON tagekyc.raw_export_policy_closures;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_requirements_append_only ON tagekyc.raw_export_policy_requirements;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_allowed_classes_append_only ON tagekyc.raw_export_policy_allowed_classes;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_versions_append_only ON tagekyc.raw_export_policy_versions;
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_policy_closure_insert();
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_child_same_transaction();
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_policy_version_insert();
                DROP FUNCTION IF EXISTS tagekyc.reject_raw_export_rule_runtime_mutation();
                """);

            migrationBuilder.DropTable(
                name: "raw_export_policy_allowed_classes",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_policy_closures",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_policy_requirements",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_requirement_rules",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_policy_versions",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_requirement_rule_sets",
                schema: "tagekyc");
        }
    }
}
