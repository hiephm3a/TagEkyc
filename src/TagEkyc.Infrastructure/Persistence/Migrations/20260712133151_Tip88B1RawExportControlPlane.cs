using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B1RawExportControlPlane : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_control_authorities",
                schema: "tagekyc",
                columns: table => new
                {
                    AuthorityEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequirementType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_control_authorities", x => x.AuthorityEventId);
                    table.CheckConstraint("CK_raw_export_control_authorities_AuthorityType", "\"AuthorityType\" IN ('GrantAdmin','RecorderAuthorityAdmin','ActivationAuthority','FulfillmentRecorder')");
                    table.CheckConstraint("CK_raw_export_control_authorities_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
                    table.CheckConstraint("CK_raw_export_control_authorities_EventType", "\"EventType\" IN ('Granted','Revoked')");
                    table.CheckConstraint("CK_raw_export_control_authorities_RequirementShape", "(\n                    \"AuthorityType\" = 'FulfillmentRecorder'\n                    AND \"RequirementType\" IN ('LegalApproval','Dpia','CrossBorderAssessment','RetentionSchedule')\n                ) OR (\n                    \"AuthorityType\" <> 'FulfillmentRecorder'\n                    AND \"RequirementType\" IS NULL\n                )");
                    table.CheckConstraint("CK_raw_export_control_authorities_ScopeShape", "(\n                    \"ScopeType\" = 'Global' AND \"ScopeId\" IS NULL\n                ) OR (\n                    \"ScopeType\" = 'Policy' AND \"ScopeId\" IS NOT NULL\n                )");
                    table.CheckConstraint("CK_raw_export_control_authorities_ScopeType", "\"ScopeType\" IN ('Policy','Global')");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_fulfillments",
                schema: "tagekyc",
                columns: table => new
                {
                    FulfillmentEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    RequirementType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupersedesRevision = table.Column<int>(type: "integer", nullable: true),
                    TargetRevision = table.Column<int>(type: "integer", nullable: true),
                    ArtifactRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ArtifactVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfillmentDecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_fulfillments", x => x.FulfillmentEventId);
                    table.CheckConstraint("CK_raw_export_fulfillments_DecisionRef_NotBlank", "btrim(\"FulfillmentDecisionRef\") <> ''");
                    table.CheckConstraint("CK_raw_export_fulfillments_EventShape", "(\n                    \"EventType\" = 'Accepted'\n                    AND \"ArtifactRef\" IS NOT NULL\n                    AND btrim(\"ArtifactRef\") <> ''\n                    AND \"ArtifactVersion\" IS NOT NULL\n                    AND btrim(\"ArtifactVersion\") <> ''\n                    AND \"ValidFromUtc\" IS NOT NULL\n                    AND \"TargetRevision\" IS NULL\n                ) OR (\n                    \"EventType\" = 'Withdrawn'\n                    AND \"TargetRevision\" IS NOT NULL\n                    AND \"ArtifactRef\" IS NULL\n                    AND \"ArtifactVersion\" IS NULL\n                    AND \"ValidFromUtc\" IS NULL\n                    AND \"ValidUntilUtc\" IS NULL\n                    AND \"SupersedesRevision\" IS NULL\n                )");
                    table.CheckConstraint("CK_raw_export_fulfillments_EventType", "\"EventType\" IN ('Accepted','Withdrawn')");
                    table.CheckConstraint("CK_raw_export_fulfillments_RequirementType", "\"RequirementType\" IN ('LegalApproval','Dpia','CrossBorderAssessment','RetentionSchedule')");
                    table.CheckConstraint("CK_raw_export_fulfillments_Validity", "\"ValidUntilUtc\" IS NULL OR \"ValidFromUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\"");
                    table.ForeignKey(
                        name: "FK_raw_export_fulfillments_raw_export_policy_versions_PolicyId~",
                        columns: x => new { x.PolicyId, x.PolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_grants",
                schema: "tagekyc",
                columns: table => new
                {
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_grants", x => new { x.PrincipalId, x.PolicyId, x.PolicyVersion, x.Revision });
                    table.CheckConstraint("CK_raw_export_grants_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
                    table.CheckConstraint("CK_raw_export_grants_EventType", "\"EventType\" IN ('Granted','Revoked')");
                    table.ForeignKey(
                        name: "FK_raw_export_grants_raw_export_policy_versions_PolicyId_Polic~",
                        columns: x => new { x.PolicyId, x.PolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_policy_lifecycle",
                schema: "tagekyc",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_policy_lifecycle", x => new { x.PolicyId, x.PolicyVersion, x.Revision });
                    table.CheckConstraint("CK_raw_export_policy_lifecycle_DecisionRef_NotBlank", "btrim(\"DecisionRef\") <> ''");
                    table.CheckConstraint("CK_raw_export_policy_lifecycle_EventType", "\"EventType\" IN ('Activated','Suspended','Revoked')");
                    table.ForeignKey(
                        name: "FK_raw_export_policy_lifecycle_raw_export_policy_versions_Poli~",
                        columns: x => new { x.PolicyId, x.PolicyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_policy_versions",
                        principalColumns: new[] { "PolicyId", "PolicyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_fulfillments_PolicyId_PolicyVersion_RequirementT~",
                schema: "tagekyc",
                table: "raw_export_fulfillments",
                columns: new[] { "PolicyId", "PolicyVersion", "RequirementType", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_grants_PolicyId_PolicyVersion",
                schema: "tagekyc",
                table: "raw_export_grants",
                columns: new[] { "PolicyId", "PolicyVersion" });

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    CREATE ROLE tagekyc_raw_export_deployer NOLOGIN;
                EXCEPTION WHEN duplicate_object THEN
                    NULL;
                END
                $$;

                DO $$
                BEGIN
                    CREATE ROLE tagekyc_runtime NOLOGIN;
                EXCEPTION WHEN duplicate_object THEN
                    NULL;
                END
                $$;

                DO $$
                BEGIN
                    CREATE ROLE tagekyc_raw_export_bootstrapper NOLOGIN;
                EXCEPTION WHEN duplicate_object THEN
                    NULL;
                END
                $$;

                CREATE UNIQUE INDEX "UX_raw_export_control_authorities_stream"
                    ON tagekyc.raw_export_control_authorities
                    ("PrincipalId", "AuthorityType", "ScopeType", COALESCE("ScopeId", '00000000-0000-0000-0000-000000000000'::uuid), COALESCE("RequirementType", ''), "Revision");

                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_raw_export_deployer;
                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_runtime;
                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_raw_export_bootstrapper;

                GRANT SELECT ON
                    tagekyc.raw_export_policy_versions,
                    tagekyc.raw_export_policy_requirements,
                    tagekyc.raw_export_policy_closures,
                    tagekyc.raw_export_requirement_rule_sets,
                    tagekyc.raw_export_grants,
                    tagekyc.raw_export_control_authorities,
                    tagekyc.raw_export_fulfillments,
                    tagekyc.raw_export_policy_lifecycle
                TO tagekyc_raw_export_deployer;

                GRANT INSERT ON
                    tagekyc.raw_export_grants,
                    tagekyc.raw_export_control_authorities,
                    tagekyc.raw_export_fulfillments,
                    tagekyc.raw_export_policy_lifecycle
                TO tagekyc_raw_export_deployer;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_current_actor()
                RETURNS uuid
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_text text;
                    actor_id uuid;
                BEGIN
                    actor_text := pg_catalog.current_setting('tagekyc.actor_principal_id', true);
                    IF actor_text IS NULL OR pg_catalog.btrim(actor_text) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_ACTOR_CONTEXT_MISSING';
                    END IF;

                    BEGIN
                        actor_id := actor_text::uuid;
                    EXCEPTION WHEN invalid_text_representation THEN
                        RAISE EXCEPTION 'RAW_EXPORT_ACTOR_CONTEXT_INVALID';
                    END;

                    IF actor_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
                        RAISE EXCEPTION 'RAW_EXPORT_ACTOR_CONTEXT_INVALID';
                    END IF;

                    RETURN actor_id;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_control_plane_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    append_context text;
                    expected_context text;
                BEGIN
                    expected_context := TG_ARGV[0];
                    append_context := pg_catalog.current_setting('tagekyc.raw_export_append_context', true);
                    IF current_user <> 'tagekyc_raw_export_deployer' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DIRECT_EVENT_INSERT_UNSUPPORTED';
                    END IF;
                    IF append_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DIRECT_EVENT_INSERT_UNSUPPORTED';
                    END IF;
                    NEW."RecordedAtUtc" := pg_catalog.transaction_timestamp();
                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_policy_exists(policy_id uuid)
                RETURNS boolean
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    SELECT EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id
                    );
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_has_current_authority(
                    actor_id uuid,
                    required_authority text,
                    policy_id uuid,
                    requirement_type text DEFAULT NULL)
                RETURNS boolean
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    WITH latest AS (
                        SELECT DISTINCT ON ("PrincipalId","AuthorityType","ScopeType","ScopeId","RequirementType")
                            "PrincipalId","AuthorityType","ScopeType","ScopeId","RequirementType","EventType"
                        FROM tagekyc.raw_export_control_authorities
                        WHERE "PrincipalId" = actor_id
                          AND "AuthorityType" = required_authority
                          AND (
                            ("ScopeType" = 'Global' AND "ScopeId" IS NULL)
                            OR ("ScopeType" = 'Policy' AND "ScopeId" = policy_id)
                          )
                          AND (
                            (required_authority = 'FulfillmentRecorder' AND "RequirementType" = requirement_type)
                            OR (required_authority <> 'FulfillmentRecorder' AND "RequirementType" IS NULL)
                          )
                        ORDER BY "PrincipalId","AuthorityType","ScopeType","ScopeId","RequirementType","Revision" DESC
                    )
                    SELECT EXISTS (SELECT 1 FROM latest WHERE "EventType" = 'Granted');
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_grant(
                    principal_id uuid,
                    policy_id uuid,
                    policy_version integer,
                    expected_revision integer,
                    event_type text,
                    client_application_id uuid,
                    decision_ref text)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    current_revision integer;
                    current_event text;
                    next_revision integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF event_type NOT IN ('Granted','Revoked') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_GRANT_EVENT_INVALID';
                    END IF;
                    IF event_type = 'Granted' AND actor_id = principal_id THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SELF_GRANT_DENIED';
                    END IF;
                    IF decision_ref IS NULL OR pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_REQUIRED';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;
                    IF NOT tagekyc.raw_export_has_current_authority(actor_id, 'GrantAdmin', policy_id, NULL) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_DENIED';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b1:grant:' || principal_id::text || ':' || policy_id::text || ':' || policy_version::text));
                    SELECT "Revision", "EventType"
                    INTO current_revision, current_event
                    FROM tagekyc.raw_export_grants
                    WHERE "PrincipalId" = principal_id AND "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);
                    IF current_revision <> expected_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REVISION_CONFLICT';
                    END IF;
                    IF current_event = event_type THEN
                        RETURN current_revision;
                    END IF;

                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_append_context', 'grant', true);
                    INSERT INTO tagekyc.raw_export_grants
                        ("PrincipalId","PolicyId","PolicyVersion","Revision","EventType","ClientApplicationId","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (principal_id, policy_id, policy_version, next_revision, event_type, client_application_id, decision_ref, actor_id, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_control_authority(
                    principal_id uuid,
                    authority_type text,
                    scope_type text,
                    scope_id uuid,
                    requirement_type text,
                    expected_revision integer,
                    event_type text,
                    decision_ref text)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    required_authority text;
                    current_revision integer;
                    current_event text;
                    next_revision integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF scope_type <> 'Policy' OR scope_id IS NULL THEN
                        RAISE EXCEPTION 'RAW_EXPORT_RUNTIME_GLOBAL_AUTHORITY_UNSUPPORTED';
                    END IF;
                    IF event_type NOT IN ('Granted','Revoked') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_EVENT_INVALID';
                    END IF;
                    IF event_type = 'Granted' AND actor_id = principal_id THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SELF_ESCALATION_DENIED';
                    END IF;
                    IF decision_ref IS NULL OR pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_REQUIRED';
                    END IF;
                    IF NOT tagekyc.raw_export_policy_exists(scope_id) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_POLICY_SCOPE_NOT_FOUND';
                    END IF;

                    required_authority := CASE authority_type
                        WHEN 'GrantAdmin' THEN 'GrantAdmin'
                        WHEN 'RecorderAuthorityAdmin' THEN 'RecorderAuthorityAdmin'
                        WHEN 'ActivationAuthority' THEN 'ActivationAuthority'
                        WHEN 'FulfillmentRecorder' THEN 'RecorderAuthorityAdmin'
                        ELSE NULL
                    END;
                    IF required_authority IS NULL THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_TYPE_INVALID';
                    END IF;
                    IF NOT tagekyc.raw_export_has_current_authority(actor_id, required_authority, scope_id, NULL) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_DENIED';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b1:authority:' || principal_id::text || ':' || authority_type || ':' || scope_type || ':' || COALESCE(scope_id::text,'') || ':' || COALESCE(requirement_type,'')));
                    SELECT "Revision", "EventType"
                    INTO current_revision, current_event
                    FROM tagekyc.raw_export_control_authorities
                    WHERE "PrincipalId" = principal_id
                      AND "AuthorityType" = authority_type
                      AND "ScopeType" = scope_type
                      AND "ScopeId" IS NOT DISTINCT FROM scope_id
                      AND "RequirementType" IS NOT DISTINCT FROM requirement_type
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);
                    IF current_revision <> expected_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REVISION_CONFLICT';
                    END IF;
                    IF current_event = event_type THEN
                        RETURN current_revision;
                    END IF;

                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_append_context', 'authority', true);
                    INSERT INTO tagekyc.raw_export_control_authorities
                        ("AuthorityEventId","PrincipalId","AuthorityType","ScopeType","ScopeId","RequirementType","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (pg_catalog.gen_random_uuid(), principal_id, authority_type, scope_type, scope_id, requirement_type, next_revision, event_type, decision_ref, actor_id, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_bootstrap_global_authority(
                    principal_id uuid,
                    authority_type text,
                    decision_ref text)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    current_revision integer;
                    next_revision integer;
                BEGIN
                    IF authority_type NOT IN ('GrantAdmin','RecorderAuthorityAdmin','ActivationAuthority') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_BOOTSTRAP_AUTHORITY_TYPE_INVALID';
                    END IF;
                    IF decision_ref IS NULL OR pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_REQUIRED';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b1:authority:' || principal_id::text || ':' || authority_type || ':Global::'));
                    SELECT COALESCE(MAX("Revision"), 0)
                    INTO current_revision
                    FROM tagekyc.raw_export_control_authorities
                    WHERE "PrincipalId" = principal_id
                      AND "AuthorityType" = authority_type
                      AND "ScopeType" = 'Global'
                      AND "ScopeId" IS NULL
                      AND "RequirementType" IS NULL;
                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_append_context', 'authority', true);
                    INSERT INTO tagekyc.raw_export_control_authorities
                        ("AuthorityEventId","PrincipalId","AuthorityType","ScopeType","ScopeId","RequirementType","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (pg_catalog.gen_random_uuid(), principal_id, authority_type, 'Global', NULL, NULL, next_revision, 'Granted', decision_ref, '00000000-0000-5000-8000-000000088b10'::uuid, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_fulfillment(
                    policy_id uuid,
                    policy_version integer,
                    requirement_type text,
                    expected_revision integer,
                    event_type text,
                    supersedes_revision integer,
                    target_revision integer,
                    artifact_ref text,
                    artifact_version text,
                    valid_from_utc timestamptz,
                    valid_until_utc timestamptz,
                    decision_ref text)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    current_revision integer;
                    current_event text;
                    latest_accepted_revision integer;
                    current_valid boolean;
                    next_revision integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF requirement_type NOT IN ('LegalApproval','Dpia','CrossBorderAssessment','RetentionSchedule') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_FULFILLMENT_REQUIREMENT_INVALID';
                    END IF;
                    IF NOT tagekyc.raw_export_has_current_authority(actor_id, 'FulfillmentRecorder', policy_id, requirement_type) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_DENIED';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;
                    IF decision_ref IS NULL OR pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_REQUIRED';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b1:fulfillment:' || policy_id::text || ':' || policy_version::text || ':' || requirement_type));
                    SELECT "Revision", "EventType",
                           ("EventType" = 'Accepted'
                            AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
                            AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
                    INTO current_revision, current_event, current_valid
                    FROM tagekyc.raw_export_fulfillments
                    WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version AND "RequirementType" = requirement_type
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);
                    IF current_revision <> expected_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REVISION_CONFLICT';
                    END IF;
                    SELECT MAX("Revision")
                    INTO latest_accepted_revision
                    FROM tagekyc.raw_export_fulfillments
                    WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version AND "RequirementType" = requirement_type AND "EventType" = 'Accepted';

                    IF event_type = 'Accepted' THEN
                        IF latest_accepted_revision IS NULL AND supersedes_revision IS NOT NULL THEN
                            RAISE EXCEPTION 'RAW_EXPORT_FULFILLMENT_SUPERSEDES_INVALID';
                        END IF;
                        IF latest_accepted_revision IS NOT NULL AND supersedes_revision IS DISTINCT FROM latest_accepted_revision THEN
                            RAISE EXCEPTION 'RAW_EXPORT_FULFILLMENT_SUPERSEDES_STALE';
                        END IF;
                    ELSIF event_type = 'Withdrawn' THEN
                        IF current_event IS DISTINCT FROM 'Accepted' OR current_valid IS DISTINCT FROM true OR target_revision IS DISTINCT FROM current_revision THEN
                            RAISE EXCEPTION 'RAW_EXPORT_FULFILLMENT_WITHDRAW_TARGET_INVALID';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'RAW_EXPORT_FULFILLMENT_EVENT_INVALID';
                    END IF;

                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_append_context', 'fulfillment', true);
                    INSERT INTO tagekyc.raw_export_fulfillments
                        ("FulfillmentEventId","PolicyId","PolicyVersion","RequirementType","Revision","EventType","SupersedesRevision","TargetRevision",
                         "ArtifactRef","ArtifactVersion","ValidFromUtc","ValidUntilUtc","RecordedByPrincipalId","FulfillmentDecisionRef","RecordedAtUtc")
                    VALUES
                        (pg_catalog.gen_random_uuid(), policy_id, policy_version, requirement_type, next_revision, event_type, supersedes_revision, target_revision,
                         artifact_ref, artifact_version, valid_from_utc, valid_until_utc, actor_id, decision_ref, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_activation_gates_hold(policy_id uuid, policy_version integer)
                RETURNS boolean
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    WITH policy AS (
                        SELECT *
                        FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ),
                    current_rule AS (
                        SELECT MAX("RuleSetVersion") AS version
                        FROM tagekyc.raw_export_requirement_rule_sets
                        WHERE "RuleSetId" = 'RAW_EXPORT_REQUIREMENTS'
                    ),
                    policy_requirements AS (
                        SELECT r."RequirementType"
                        FROM tagekyc.raw_export_policy_requirements r
                        WHERE r."PolicyId" = policy_id
                          AND r."PolicyVersion" = policy_version
                          AND r."RequirementType" <> 'ConsentArtifact'
                    ),
                    latest_fulfillments AS (
                        SELECT DISTINCT ON ("RequirementType")
                            "RequirementType","EventType","ValidFromUtc","ValidUntilUtc"
                        FROM tagekyc.raw_export_fulfillments
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                        ORDER BY "RequirementType","Revision" DESC
                    )
                    SELECT EXISTS (
                        SELECT 1
                        FROM policy p
                        JOIN tagekyc.raw_export_policy_closures c
                          ON c."PolicyId" = p."PolicyId" AND c."PolicyVersion" = p."PolicyVersion"
                        JOIN current_rule cr ON true
                        WHERE c."ClosureType" = 'CatalogApproved'
                          AND p."RequirementRuleSetVersion" = cr.version
                          AND NOT EXISTS (
                            SELECT 1
                            FROM policy_requirements pr
                            LEFT JOIN latest_fulfillments f ON f."RequirementType" = pr."RequirementType"
                            WHERE f."RequirementType" IS NULL
                               OR f."EventType" <> 'Accepted'
                               OR f."ValidFromUtc" > pg_catalog.transaction_timestamp()
                               OR (f."ValidUntilUtc" IS NOT NULL AND pg_catalog.transaction_timestamp() >= f."ValidUntilUtc")
                          )
                    );
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_lifecycle(
                    policy_id uuid,
                    policy_version integer,
                    expected_revision integer,
                    event_type text,
                    decision_ref text)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    current_revision integer;
                    current_event text;
                    next_revision integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF NOT tagekyc.raw_export_has_current_authority(actor_id, 'ActivationAuthority', policy_id, NULL) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_AUTHORITY_DENIED';
                    END IF;
                    IF decision_ref IS NULL OR pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_REQUIRED';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b1:lifecycle:' || policy_id::text || ':' || policy_version::text));
                    SELECT "Revision", "EventType"
                    INTO current_revision, current_event
                    FROM tagekyc.raw_export_policy_lifecycle
                    WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);
                    IF current_revision <> expected_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_REVISION_CONFLICT';
                    END IF;
                    IF current_event = 'Revoked' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_LIFECYCLE_REVOKED_TERMINAL';
                    END IF;
                    IF event_type = 'Activated' THEN
                        IF current_revision > 0 AND current_event <> 'Suspended' THEN
                            RAISE EXCEPTION 'RAW_EXPORT_LIFECYCLE_TRANSITION_INVALID';
                        END IF;
                        IF NOT tagekyc.raw_export_activation_gates_hold(policy_id, policy_version) THEN
                            RAISE EXCEPTION 'RAW_EXPORT_ACTIVATION_GATE_DENIED';
                        END IF;
                    ELSIF event_type = 'Suspended' THEN
                        IF current_event <> 'Activated' THEN
                            RAISE EXCEPTION 'RAW_EXPORT_LIFECYCLE_TRANSITION_INVALID';
                        END IF;
                    ELSIF event_type = 'Revoked' THEN
                        IF current_revision = 0 THEN
                            RAISE EXCEPTION 'RAW_EXPORT_LIFECYCLE_TRANSITION_INVALID';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'RAW_EXPORT_LIFECYCLE_EVENT_INVALID';
                    END IF;

                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_append_context', 'lifecycle', true);
                    INSERT INTO tagekyc.raw_export_policy_lifecycle
                        ("PolicyId","PolicyVersion","Revision","EventType","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (policy_id, policy_version, next_revision, event_type, decision_ref, actor_id, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE TRIGGER tr_raw_export_grants_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_grants
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_control_plane_insert('grant');
                CREATE TRIGGER tr_raw_export_control_authorities_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_control_authorities
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_control_plane_insert('authority');
                CREATE TRIGGER tr_raw_export_fulfillments_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_fulfillments
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_control_plane_insert('fulfillment');
                CREATE TRIGGER tr_raw_export_policy_lifecycle_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_policy_lifecycle
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_control_plane_insert('lifecycle');

                CREATE TRIGGER tr_raw_export_grants_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_grants
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_control_authorities_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_control_authorities
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_fulfillments_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_fulfillments
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();
                CREATE TRIGGER tr_raw_export_policy_lifecycle_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_policy_lifecycle
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                ALTER FUNCTION tagekyc.raw_export_current_actor() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_control_plane_insert() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_policy_exists(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_has_current_authority(uuid,text,uuid,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_grant(uuid,uuid,integer,integer,text,uuid,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_control_authority(uuid,text,text,uuid,text,integer,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_bootstrap_global_authority(uuid,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_fulfillment(uuid,integer,text,integer,text,integer,integer,text,text,timestamptz,timestamptz,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_activation_gates_hold(uuid,integer) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_lifecycle(uuid,integer,integer,text,text) OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_current_actor(),
                    tagekyc.enforce_raw_export_control_plane_insert(),
                    tagekyc.raw_export_policy_exists(uuid),
                    tagekyc.raw_export_has_current_authority(uuid,text,uuid,text),
                    tagekyc.raw_export_append_grant(uuid,uuid,integer,integer,text,uuid,text),
                    tagekyc.raw_export_append_control_authority(uuid,text,text,uuid,text,integer,text,text),
                    tagekyc.raw_export_bootstrap_global_authority(uuid,text,text),
                    tagekyc.raw_export_append_fulfillment(uuid,integer,text,integer,text,integer,integer,text,text,timestamptz,timestamptz,text),
                    tagekyc.raw_export_activation_gates_hold(uuid,integer),
                    tagekyc.raw_export_append_lifecycle(uuid,integer,integer,text,text)
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_append_grant(uuid,uuid,integer,integer,text,uuid,text),
                    tagekyc.raw_export_append_control_authority(uuid,text,text,uuid,text,integer,text,text),
                    tagekyc.raw_export_append_fulfillment(uuid,integer,text,integer,text,integer,integer,text,text,timestamptz,timestamptz,text),
                    tagekyc.raw_export_append_lifecycle(uuid,integer,integer,text,text)
                TO tagekyc_runtime;

                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_bootstrap_global_authority(uuid,text,text) TO tagekyc_raw_export_bootstrapper;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_raw_export_policy_lifecycle_append_only ON tagekyc.raw_export_policy_lifecycle;
                DROP TRIGGER IF EXISTS tr_raw_export_fulfillments_append_only ON tagekyc.raw_export_fulfillments;
                DROP TRIGGER IF EXISTS tr_raw_export_control_authorities_append_only ON tagekyc.raw_export_control_authorities;
                DROP TRIGGER IF EXISTS tr_raw_export_grants_append_only ON tagekyc.raw_export_grants;
                DROP TRIGGER IF EXISTS tr_raw_export_policy_lifecycle_insert_guard ON tagekyc.raw_export_policy_lifecycle;
                DROP TRIGGER IF EXISTS tr_raw_export_fulfillments_insert_guard ON tagekyc.raw_export_fulfillments;
                DROP TRIGGER IF EXISTS tr_raw_export_control_authorities_insert_guard ON tagekyc.raw_export_control_authorities;
                DROP TRIGGER IF EXISTS tr_raw_export_grants_insert_guard ON tagekyc.raw_export_grants;
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_lifecycle(uuid,integer,integer,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_activation_gates_hold(uuid,integer);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_fulfillment(uuid,integer,text,integer,text,integer,integer,text,text,timestamptz,timestamptz,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_bootstrap_global_authority(uuid,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_control_authority(uuid,text,text,uuid,text,integer,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_grant(uuid,uuid,integer,integer,text,uuid,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_has_current_authority(uuid,text,uuid,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_policy_exists(uuid);
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_control_plane_insert();
                DROP FUNCTION IF EXISTS tagekyc.raw_export_current_actor();
                DROP INDEX IF EXISTS tagekyc."UX_raw_export_control_authorities_stream";
                """);

            migrationBuilder.DropTable(
                name: "raw_export_control_authorities",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_fulfillments",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_grants",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_policy_lifecycle",
                schema: "tagekyc");

        }
    }
}
