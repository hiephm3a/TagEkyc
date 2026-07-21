using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88AE2PolicyPermitTtlField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PermitTtlSeconds",
                schema: "tagekyc",
                table: "raw_export_policy_versions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE tagekyc.raw_export_policy_versions
                ADD CONSTRAINT "CK_raw_export_policy_versions_PermitTtlSeconds"
                CHECK ("PermitTtlSeconds" IS NOT NULL AND "PermitTtlSeconds" > 0) NOT VALID;
                """);

            migrationBuilder.Sql(E2ClosureFunction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PreE2ClosureFunction);

            migrationBuilder.Sql(
                """
                ALTER TABLE tagekyc.raw_export_policy_versions
                DROP CONSTRAINT IF EXISTS "CK_raw_export_policy_versions_PermitTtlSeconds";
                """);

            migrationBuilder.DropColumn(
                name: "PermitTtlSeconds",
                schema: "tagekyc",
                table: "raw_export_policy_versions");
        }

        private const string E2ClosureFunction =
            """
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

                IF version_row."PermitTtlSeconds" IS NULL OR version_row."PermitTtlSeconds" <= 0 THEN
                    RAISE EXCEPTION 'RAW_EXPORT_POLICY_PERMIT_TTL_INVALID';
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
            """;

        private const string PreE2ClosureFunction =
            """
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
            """;
    }
}
