using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B2SubjectExportConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_subject_consent_authorities",
                schema: "tagekyc",
                columns: table => new
                {
                    AuthorityEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorityPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetRevision = table.Column<int>(type: "integer", nullable: true),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RecordedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_subject_consent_authorities", x => x.AuthorityEventId);
                    table.CheckConstraint("CK_raw_export_subject_consent_authorities_AuthorityType", "\"AuthorityType\" IN ('SubjectConsentRecorder','SubjectConsentWithdrawer')");
                    table.CheckConstraint("CK_raw_export_subject_consent_authorities_DecisionRef_NotBlank", "\"DecisionRef\" IS NULL OR btrim(\"DecisionRef\") <> ''");
                    table.CheckConstraint("CK_raw_export_subject_consent_authorities_EventShape", "(\n    \"EventType\" = 'Granted'\n    AND \"TargetRevision\" IS NULL\n    AND \"ValidFromUtc\" IS NOT NULL\n    AND (\"ValidUntilUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\")\n) OR (\n    \"EventType\" = 'Revoked'\n    AND \"TargetRevision\" IS NOT NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n)");
                    table.CheckConstraint("CK_raw_export_subject_consent_authorities_EventType", "\"EventType\" IN ('Granted','Revoked')");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_subject_consent_events",
                schema: "tagekyc",
                columns: table => new
                {
                    SubjectConsentRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentScopeHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    PurposeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetRevision = table.Column<int>(type: "integer", nullable: true),
                    ConsentTextVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConsentTextContentHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExternalConsentArtifactRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DecisionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CapturedByPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    WithdrawnByPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_subject_consent_events", x => x.SubjectConsentRecordId);
                    table.CheckConstraint("CK_raw_export_subject_consent_events_EventShape", "(\n    \"EventType\" = 'Granted'\n    AND \"TargetRevision\" IS NULL\n    AND \"ConsentTextVersion\" IS NOT NULL AND btrim(\"ConsentTextVersion\") <> ''\n    AND \"ConsentTextContentHash\" IS NOT NULL AND btrim(\"ConsentTextContentHash\") <> ''\n    AND \"ExternalConsentArtifactRef\" IS NOT NULL AND btrim(\"ExternalConsentArtifactRef\") <> ''\n    AND (\"DecisionRef\" IS NULL OR btrim(\"DecisionRef\") <> '')\n    AND \"ValidFromUtc\" IS NOT NULL\n    AND \"CapturedAtUtc\" IS NOT NULL\n    AND \"CapturedByPrincipalId\" IS NOT NULL\n    AND \"WithdrawnByPrincipalId\" IS NULL\n    AND (\"ValidUntilUtc\" IS NULL OR \"ValidUntilUtc\" > \"ValidFromUtc\")\n) OR (\n    \"EventType\" = 'Withdrawn'\n    AND \"TargetRevision\" IS NOT NULL\n    AND (\"DecisionRef\" IS NULL OR btrim(\"DecisionRef\") <> '')\n    AND \"ConsentTextVersion\" IS NULL\n    AND \"ConsentTextContentHash\" IS NULL\n    AND \"ValidFromUtc\" IS NULL\n    AND \"ValidUntilUtc\" IS NULL\n    AND \"CapturedAtUtc\" IS NULL\n    AND \"CapturedByPrincipalId\" IS NULL\n    AND \"WithdrawnByPrincipalId\" IS NOT NULL\n)");
                    table.CheckConstraint("CK_raw_export_subject_consent_events_EventType", "\"EventType\" IN ('Granted','Withdrawn')");
                    table.CheckConstraint("CK_raw_export_subject_consent_events_HashLength", "octet_length(\"ConsentScopeHash\") = 32");
                    table.CheckConstraint("CK_raw_export_subject_consent_events_PurposeCode", "\"PurposeCode\" = 'SubjectRawBiometricExport'");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_subject_consent_classes",
                schema: "tagekyc",
                columns: table => new
                {
                    SubjectConsentRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_export_subject_consent_classes", x => new { x.SubjectConsentRecordId, x.RawClass });
                    table.CheckConstraint("CK_raw_export_subject_consent_classes_RawClass", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')");
                    table.ForeignKey(
                        name: "FK_raw_export_subject_consent_classes_raw_export_subject_conse~",
                        column: x => x.SubjectConsentRecordId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_subject_consent_events",
                        principalColumn: "SubjectConsentRecordId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_subject_consent_authorities_AuthorityPrincipalId~",
                schema: "tagekyc",
                table: "raw_export_subject_consent_authorities",
                columns: new[] { "AuthorityPrincipalId", "ClientApplicationId", "AuthorityType", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_subject_consent_events_VerificationSessionId_Sub~",
                schema: "tagekyc",
                table: "raw_export_subject_consent_events",
                columns: new[] { "VerificationSessionId", "SubjectRef", "PolicyId", "PolicyVersion", "PurposeCode", "RecipientClientApplicationId", "Revision" },
                unique: true);

            migrationBuilder.Sql(
                """
                GRANT SELECT ON
                    tagekyc.verification_sessions,
                    tagekyc.raw_export_policy_versions,
                    tagekyc.raw_export_subject_consent_authorities,
                    tagekyc.raw_export_subject_consent_events,
                    tagekyc.raw_export_subject_consent_classes
                TO tagekyc_raw_export_deployer;

                GRANT SELECT ON
                    tagekyc.verification_sessions,
                    tagekyc.raw_export_subject_consent_authorities,
                    tagekyc.raw_export_subject_consent_events,
                    tagekyc.raw_export_subject_consent_classes
                TO tagekyc_runtime;

                GRANT UPDATE ON
                    tagekyc.verification_sessions
                TO tagekyc_raw_export_deployer;

                GRANT INSERT ON
                    tagekyc.raw_export_subject_consent_authorities,
                    tagekyc.raw_export_subject_consent_events,
                    tagekyc.raw_export_subject_consent_classes
                TO tagekyc_raw_export_deployer;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_consent_scope_hash(
                    verification_session_id uuid,
                    subject_ref text,
                    policy_id uuid,
                    policy_version integer,
                    purpose_code text,
                    recipient_client_application_id uuid)
                RETURNS bytea
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    f1 bytea;
                    f2 bytea;
                    f3 bytea;
                    f4 bytea;
                    f5 bytea;
                    f6 bytea;
                BEGIN
                    f1 := pg_catalog.uuid_send(verification_session_id);
                    f2 := pg_catalog.convert_to(pg_catalog.normalize(subject_ref), 'UTF8');
                    f3 := pg_catalog.uuid_send(policy_id);
                    f4 := pg_catalog.int8send(policy_version::bigint);
                    f5 := pg_catalog.convert_to(purpose_code, 'UTF8');
                    f6 := pg_catalog.uuid_send(recipient_client_application_id);
                    RETURN pg_catalog.sha256(
                        pg_catalog.convert_to('tagekyc:consent-scope:v1', 'UTF8') ||
                        pg_catalog.int4send(pg_catalog.octet_length(f1)) || f1 ||
                        pg_catalog.int4send(pg_catalog.octet_length(f2)) || f2 ||
                        pg_catalog.int4send(pg_catalog.octet_length(f3)) || f3 ||
                        pg_catalog.int4send(pg_catalog.octet_length(f4)) || f4 ||
                        pg_catalog.int4send(pg_catalog.octet_length(f5)) || f5 ||
                        pg_catalog.int4send(pg_catalog.octet_length(f6)) || f6);
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_consent_lock_key(scope_hash bytea)
                RETURNS bigint
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    value numeric := 0;
                    index integer;
                BEGIN
                    IF scope_hash IS NULL OR pg_catalog.octet_length(scope_hash) < 8 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_SCOPE_HASH_INVALID';
                    END IF;
                    FOR index IN 0..7 LOOP
                        value := value * 256 + pg_catalog.get_byte(scope_hash, index);
                    END LOOP;
                    IF value >= 9223372036854775808 THEN
                        value := value - 18446744073709551616;
                    END IF;
                    RETURN value::bigint;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_subject_consent_has_current_authority(
                    actor_id uuid,
                    client_application_id uuid,
                    required_authority text)
                RETURNS boolean
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    WITH latest AS (
                        SELECT "EventType","ValidFromUtc","ValidUntilUtc"
                        FROM tagekyc.raw_export_subject_consent_authorities
                        WHERE "AuthorityPrincipalId" = actor_id
                          AND "ClientApplicationId" = client_application_id
                          AND "AuthorityType" = required_authority
                        ORDER BY "Revision" DESC
                        LIMIT 1
                    )
                    SELECT EXISTS (
                        SELECT 1
                        FROM latest
                        WHERE "EventType" = 'Granted'
                          AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
                          AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc")
                    );
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_lock_verification_session_for_subject_consent(
                    verification_session_id uuid)
                RETURNS TABLE("ClientApplicationId" uuid, "SubjectRef" text, "State" text)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    SELECT s."ClientApplicationId", s."SubjectRef", s."State"
                    FROM tagekyc.verification_sessions s
                    WHERE s."Id" = verification_session_id
                    FOR UPDATE;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_resolve_subject_consent_for_authorization(
                    verification_session_id uuid,
                    policy_id uuid,
                    policy_version integer)
                RETURNS TABLE(
                    "State" text,
                    "Cause" text,
                    "SubjectConsentRecordId" uuid,
                    "ConsentScopeHash" bytea,
                    "Revision" integer,
                    "VerificationSessionId" uuid,
                    "SubjectRef" text,
                    "PolicyId" uuid,
                    "PolicyVersion" integer,
                    "PurposeCode" text,
                    "RecipientClientApplicationId" uuid,
                    "RawClass" text,
                    "ValidFromUtc" timestamptz,
                    "ValidUntilUtc" timestamptz,
                    "EvaluatedAtUtc" timestamptz,
                    "ConsentTextVersion" text,
                    "ConsentTextContentHash" text,
                    "ExternalConsentArtifactRef" text,
                    "DecisionRef" text)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    session_subject text;
                    session_owner uuid;
                    purpose_code text := 'SubjectRawBiometricExport';
                    scope_hash bytea;
                    evaluated_at timestamptz := pg_catalog.transaction_timestamp();
                    latest_row tagekyc.raw_export_subject_consent_events%ROWTYPE;
                    snapshot_state text;
                    snapshot_cause text;
                BEGIN
                    SELECT locked."SubjectRef", locked."ClientApplicationId"
                    INTO session_subject, session_owner
                    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id) AS locked;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
                    END IF;

                    scope_hash := tagekyc.raw_export_consent_scope_hash(
                        verification_session_id, session_subject, policy_id, policy_version, purpose_code, session_owner);
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(tagekyc.raw_export_consent_lock_key(scope_hash));

                    SELECT *
                    INTO latest_row
                    FROM tagekyc.raw_export_subject_consent_events e
                    WHERE e."VerificationSessionId" = verification_session_id
                      AND e."SubjectRef" = session_subject
                      AND e."PolicyId" = policy_id
                      AND e."PolicyVersion" = policy_version
                      AND e."PurposeCode" = purpose_code
                      AND e."RecipientClientApplicationId" = session_owner
                    ORDER BY e."Revision" DESC
                    LIMIT 1;

                    IF NOT FOUND THEN
                        RETURN QUERY SELECT
                            'NonEffective'::text,
                            'Missing'::text,
                            NULL::uuid,
                            scope_hash,
                            NULL::integer,
                            verification_session_id,
                            session_subject,
                            policy_id,
                            policy_version,
                            purpose_code,
                            session_owner,
                            NULL::text,
                            NULL::timestamptz,
                            NULL::timestamptz,
                            evaluated_at,
                            NULL::text,
                            NULL::text,
                            NULL::text,
                            NULL::text;
                        RETURN;
                    END IF;

                    IF latest_row."EventType" = 'Withdrawn' THEN
                        snapshot_state := 'NonEffective';
                        snapshot_cause := 'Withdrawn';
                    ELSIF latest_row."ValidFromUtc" IS NULL
                       OR latest_row."ValidFromUtc" > evaluated_at
                       OR (latest_row."ValidUntilUtc" IS NOT NULL AND evaluated_at >= latest_row."ValidUntilUtc") THEN
                        snapshot_state := 'NonEffective';
                        snapshot_cause := 'Expired';
                    ELSE
                        snapshot_state := 'Effective';
                        snapshot_cause := NULL;
                    END IF;

                    IF snapshot_state = 'Effective' THEN
                        RETURN QUERY
                            SELECT
                                snapshot_state,
                                snapshot_cause,
                                latest_row."SubjectConsentRecordId",
                                latest_row."ConsentScopeHash",
                                latest_row."Revision",
                                latest_row."VerificationSessionId",
                                latest_row."SubjectRef"::text,
                                latest_row."PolicyId",
                                latest_row."PolicyVersion",
                                latest_row."PurposeCode"::text,
                                latest_row."RecipientClientApplicationId",
                                c."RawClass"::text,
                                latest_row."ValidFromUtc",
                                latest_row."ValidUntilUtc",
                                evaluated_at,
                                latest_row."ConsentTextVersion"::text,
                                latest_row."ConsentTextContentHash"::text,
                                latest_row."ExternalConsentArtifactRef"::text,
                                latest_row."DecisionRef"::text
                            FROM tagekyc.raw_export_subject_consent_classes c
                            WHERE c."SubjectConsentRecordId" = latest_row."SubjectConsentRecordId"
                            ORDER BY c."RawClass";
                    ELSE
                        RETURN QUERY SELECT
                            snapshot_state,
                            snapshot_cause,
                            latest_row."SubjectConsentRecordId",
                            latest_row."ConsentScopeHash",
                            latest_row."Revision",
                            latest_row."VerificationSessionId",
                            latest_row."SubjectRef"::text,
                            latest_row."PolicyId",
                            latest_row."PolicyVersion",
                            latest_row."PurposeCode"::text,
                            latest_row."RecipientClientApplicationId",
                            NULL::text,
                            latest_row."ValidFromUtc",
                            latest_row."ValidUntilUtc",
                            evaluated_at,
                            latest_row."ConsentTextVersion"::text,
                            latest_row."ConsentTextContentHash"::text,
                            latest_row."ExternalConsentArtifactRef"::text,
                            latest_row."DecisionRef"::text;
                    END IF;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_subject_consent_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    expected_context text;
                    append_context text;
                    session_subject text;
                    session_owner uuid;
                    session_state text;
                    expected_hash bytea;
                    actor_id uuid;
                    latest_revision integer;
                    latest_event text;
                    latest_valid boolean;
                    current_revision integer;
                    current_event text;
                    current_valid boolean;
                BEGIN
                    expected_context := TG_ARGV[0];
                    append_context := pg_catalog.current_setting('tagekyc.raw_export_subject_consent_append_context', true);
                    IF current_user <> 'tagekyc_raw_export_deployer' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_DIRECT_INSERT_UNSUPPORTED';
                    END IF;
                    IF append_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_DIRECT_INSERT_UNSUPPORTED';
                    END IF;

                    IF TG_TABLE_NAME = 'raw_export_subject_consent_authorities' THEN
                        actor_id := tagekyc.raw_export_current_actor();
                        IF NEW."AuthorityType" NOT IN ('SubjectConsentRecorder','SubjectConsentWithdrawer') THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_TYPE_INVALID';
                        END IF;
                        IF NEW."EventType" NOT IN ('Granted','Revoked') THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_EVENT_INVALID';
                        END IF;
                        IF NEW."DecisionRef" IS NOT NULL AND pg_catalog.btrim(NEW."DecisionRef") = '' THEN
                            RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                        END IF;
                        IF NEW."ClientApplicationId" = '00000000-0000-0000-0000-000000000000'::uuid THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLIENT_INVALID';
                        END IF;
                        IF NEW."EventType" = 'Granted' AND actor_id = NEW."AuthorityPrincipalId" THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SELF_ESCALATION_DENIED';
                        END IF;

                        PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || NEW."AuthorityPrincipalId"::text || ':' || NEW."ClientApplicationId"::text || ':' || NEW."AuthorityType"));
                        SELECT "Revision", "EventType",
                               ("EventType" = 'Granted'
                                AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
                                AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
                        INTO current_revision, current_event, current_valid
                        FROM tagekyc.raw_export_subject_consent_authorities
                        WHERE "AuthorityPrincipalId" = NEW."AuthorityPrincipalId"
                          AND "ClientApplicationId" = NEW."ClientApplicationId"
                          AND "AuthorityType" = NEW."AuthorityType"
                        ORDER BY "Revision" DESC
                        LIMIT 1;
                        current_revision := COALESCE(current_revision, 0);
                        IF NEW."Revision" <> current_revision + 1 THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_REVISION_CONFLICT';
                        END IF;
                        IF NEW."EventType" = 'Granted' THEN
                            IF NEW."TargetRevision" IS NOT NULL THEN
                                RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_TARGET_INVALID';
                            END IF;
                            NEW."ValidFromUtc" := pg_catalog.transaction_timestamp();
                        ELSE
                            IF current_event IS DISTINCT FROM 'Granted' OR current_valid IS DISTINCT FROM true OR NEW."TargetRevision" IS DISTINCT FROM current_revision THEN
                                RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_STALE_TARGET_REVISION';
                            END IF;
                            NEW."ValidFromUtc" := NULL;
                            NEW."ValidUntilUtc" := NULL;
                        END IF;
                        NEW."RecordedByPrincipalId" := actor_id;
                        NEW."RecordedAtUtc" := pg_catalog.transaction_timestamp();
                        RETURN NEW;
                    END IF;

                    SELECT "SubjectRef", "ClientApplicationId", "State"
                    INTO session_subject, session_owner, session_state
                    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(NEW."VerificationSessionId");
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
                    END IF;
                    IF session_subject IS DISTINCT FROM NEW."SubjectRef" OR session_owner IS DISTINCT FROM NEW."RecipientClientApplicationId" THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SCOPE_MISMATCH';
                    END IF;
                    IF NEW."PurposeCode" <> 'SubjectRawBiometricExport' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_PURPOSE_INVALID';
                    END IF;
                    expected_hash := tagekyc.raw_export_consent_scope_hash(
                        NEW."VerificationSessionId", NEW."SubjectRef", NEW."PolicyId", NEW."PolicyVersion", NEW."PurposeCode", NEW."RecipientClientApplicationId");
                    IF expected_hash <> NEW."ConsentScopeHash" THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SCOPE_HASH_MISMATCH';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = NEW."PolicyId" AND "PolicyVersion" = NEW."PolicyVersion"
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;
                    IF NEW."DecisionRef" IS NOT NULL AND pg_catalog.btrim(NEW."DecisionRef") = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                    END IF;

                    IF NEW."EventType" = 'Granted' THEN
                        IF session_state <> 'Completed' THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SESSION_NOT_COMPLETED';
                        END IF;
                        actor_id := tagekyc.raw_export_current_actor();
                        IF NEW."CapturedByPrincipalId" IS DISTINCT FROM actor_id THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_ACTOR_MISMATCH';
                        END IF;
                        IF actor_id IS NOT NULL THEN
                            PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentRecorder'));
                        END IF;
                        IF actor_id IS NULL OR NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentRecorder') THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
                        END IF;
                        NEW."ValidFromUtc" := pg_catalog.transaction_timestamp();
                        NEW."CapturedAtUtc" := pg_catalog.transaction_timestamp();
                    ELSIF NEW."EventType" = 'Withdrawn' THEN
                        actor_id := tagekyc.raw_export_current_actor();
                        IF NEW."WithdrawnByPrincipalId" IS DISTINCT FROM actor_id THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_ACTOR_MISMATCH';
                        END IF;
                        IF actor_id IS NOT NULL THEN
                            PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentWithdrawer'));
                        END IF;
                        IF actor_id IS NULL OR NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentWithdrawer') THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_EVENT_INVALID';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(expected_hash));
                    SELECT "Revision", "EventType",
                           ("EventType" = 'Granted'
                            AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
                            AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
                    INTO latest_revision, latest_event, latest_valid
                    FROM tagekyc.raw_export_subject_consent_events
                    WHERE "VerificationSessionId" = NEW."VerificationSessionId"
                      AND "SubjectRef" = NEW."SubjectRef"
                      AND "PolicyId" = NEW."PolicyId"
                      AND "PolicyVersion" = NEW."PolicyVersion"
                      AND "PurposeCode" = NEW."PurposeCode"
                      AND "RecipientClientApplicationId" = NEW."RecipientClientApplicationId"
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    latest_revision := COALESCE(latest_revision, 0);
                    IF NEW."Revision" <> latest_revision + 1 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_REVISION_CONFLICT';
                    END IF;
                    IF NEW."EventType" = 'Withdrawn'
                       AND (latest_event IS DISTINCT FROM 'Granted' OR latest_valid IS DISTINCT FROM true OR NEW."TargetRevision" IS DISTINCT FROM latest_revision) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_STALE_TARGET_REVISION';
                    END IF;
                    NEW."RecordedAtUtc" := pg_catalog.transaction_timestamp();
                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_subject_consent_child_same_transaction()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    parent_xmin xid;
                    parent_event text;
                BEGIN
                    SELECT xmin, "EventType"
                    INTO parent_xmin, parent_event
                    FROM tagekyc.raw_export_subject_consent_events
                    WHERE "SubjectConsentRecordId" = NEW."SubjectConsentRecordId";
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_PARENT_MISSING';
                    END IF;
                    IF parent_event <> 'Granted' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASS_PARENT_INVALID';
                    END IF;
                    IF parent_xmin <> pg_catalog.pg_current_xact_id()::xid THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CHILD_APPEND_UNSUPPORTED';
                    END IF;
                    NEW."CreatedAt" := pg_catalog.transaction_timestamp();
                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_subject_consent_granted_has_classes()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                BEGIN
                    IF NEW."EventType" = 'Granted' AND NOT EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_subject_consent_classes
                        WHERE "SubjectConsentRecordId" = NEW."SubjectConsentRecordId"
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED';
                    END IF;
                    RETURN NULL;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_authority(
                    authority_principal_id uuid,
                    client_application_id uuid,
                    authority_type text,
                    expected_revision integer,
                    event_type text,
                    target_revision integer,
                    decision_ref text,
                    valid_until_utc timestamptz)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    current_revision integer;
                    current_event text;
                    current_valid boolean;
                    next_revision integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF authority_type NOT IN ('SubjectConsentRecorder','SubjectConsentWithdrawer') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_TYPE_INVALID';
                    END IF;
                    IF event_type NOT IN ('Granted','Revoked') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_EVENT_INVALID';
                    END IF;
                    IF event_type = 'Granted' AND actor_id = authority_principal_id THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SELF_ESCALATION_DENIED';
                    END IF;
                    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                    END IF;
                    IF client_application_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLIENT_INVALID';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || authority_principal_id::text || ':' || client_application_id::text || ':' || authority_type));
                    SELECT "Revision", "EventType",
                           ("EventType" = 'Granted'
                            AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
                            AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
                    INTO current_revision, current_event, current_valid
                    FROM tagekyc.raw_export_subject_consent_authorities
                    WHERE "AuthorityPrincipalId" = authority_principal_id
                      AND "ClientApplicationId" = client_application_id
                      AND "AuthorityType" = authority_type
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);
                    IF current_revision <> expected_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_REVISION_CONFLICT';
                    END IF;
                    IF event_type = 'Granted' THEN
                        IF target_revision IS NOT NULL THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_TARGET_INVALID';
                        END IF;
                    ELSE
                        IF current_event IS DISTINCT FROM 'Granted' OR current_valid IS DISTINCT FROM true OR target_revision IS DISTINCT FROM current_revision THEN
                            RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_STALE_TARGET_REVISION';
                        END IF;
                    END IF;

                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'authority', true);
                    INSERT INTO tagekyc.raw_export_subject_consent_authorities
                        ("AuthorityEventId","AuthorityPrincipalId","ClientApplicationId","AuthorityType","Revision","EventType","TargetRevision","ValidFromUtc","ValidUntilUtc","DecisionRef","RecordedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (pg_catalog.gen_random_uuid(), authority_principal_id, client_application_id, authority_type, next_revision, event_type, target_revision,
                         CASE WHEN event_type = 'Granted' THEN pg_catalog.transaction_timestamp() ELSE NULL END,
                         CASE WHEN event_type = 'Granted' THEN valid_until_utc ELSE NULL END,
                         decision_ref, actor_id, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_granted(
                    verification_session_id uuid,
                    policy_id uuid,
                    policy_version integer,
                    raw_classes text[],
                    consent_text_version text,
                    consent_text_content_hash text,
                    external_consent_artifact_ref text,
                    decision_ref text,
                    valid_until_utc timestamptz)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    session_subject text;
                    session_owner uuid;
                    session_state text;
                    scope_hash bytea;
                    current_revision integer;
                    next_revision integer;
                    record_id uuid;
                    raw_class text;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    SELECT "SubjectRef", "ClientApplicationId", "State"
                    INTO session_subject, session_owner, session_state
                    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id);
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
                    END IF;
                    IF session_state <> 'Completed' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_SESSION_NOT_COMPLETED';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentRecorder'));
                    IF NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentRecorder') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM tagekyc.raw_export_policy_versions
                        WHERE "PolicyId" = policy_id AND "PolicyVersion" = policy_version
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_POLICY_VERSION_NOT_FOUND';
                    END IF;
                    IF raw_classes IS NULL OR pg_catalog.cardinality(raw_classes) = 0 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_REQUIRED';
                    END IF;
                    IF (SELECT pg_catalog.count(DISTINCT item) FROM pg_catalog.unnest(raw_classes) AS item) <> pg_catalog.cardinality(raw_classes) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_CLASSES_DUPLICATE';
                    END IF;
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.unnest(raw_classes) AS item
                        WHERE item NOT IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')
                    ) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_RAW_CLASS_INVALID';
                    END IF;
                    IF consent_text_version IS NULL OR pg_catalog.btrim(consent_text_version) = ''
                       OR consent_text_content_hash IS NULL OR pg_catalog.btrim(consent_text_content_hash) = ''
                       OR external_consent_artifact_ref IS NULL OR pg_catalog.btrim(external_consent_artifact_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_PROVENANCE_REQUIRED';
                    END IF;
                    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                    END IF;

                    scope_hash := tagekyc.raw_export_consent_scope_hash(
                        verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner);
                    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(scope_hash));
                    SELECT COALESCE(MAX("Revision"), 0)
                    INTO current_revision
                    FROM tagekyc.raw_export_subject_consent_events
                    WHERE "VerificationSessionId" = verification_session_id
                      AND "SubjectRef" = session_subject
                      AND "PolicyId" = policy_id
                      AND "PolicyVersion" = policy_version
                      AND "PurposeCode" = 'SubjectRawBiometricExport'
                      AND "RecipientClientApplicationId" = session_owner;
                    next_revision := current_revision + 1;
                    record_id := pg_catalog.gen_random_uuid();
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'consent', true);
                    INSERT INTO tagekyc.raw_export_subject_consent_events
                        ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
                         "Revision","EventType","TargetRevision","ConsentTextVersion","ConsentTextContentHash","ExternalConsentArtifactRef","DecisionRef",
                         "ValidFromUtc","ValidUntilUtc","CapturedAtUtc","CapturedByPrincipalId","RecordedAtUtc")
                    VALUES
                        (record_id, scope_hash, verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner,
                         next_revision, 'Granted', NULL, consent_text_version, consent_text_content_hash, external_consent_artifact_ref, decision_ref,
                         pg_catalog.transaction_timestamp(), valid_until_utc, pg_catalog.transaction_timestamp(), actor_id, pg_catalog.transaction_timestamp());
                    FOREACH raw_class IN ARRAY raw_classes LOOP
                        INSERT INTO tagekyc.raw_export_subject_consent_classes
                            ("SubjectConsentRecordId","RawClass","CreatedAt")
                        VALUES
                            (record_id, raw_class, pg_catalog.transaction_timestamp());
                    END LOOP;
                    RETURN next_revision;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_subject_consent_withdrawn(
                    verification_session_id uuid,
                    policy_id uuid,
                    policy_version integer,
                    expected_revision integer,
                    target_revision integer,
                    decision_ref text,
                    external_consent_artifact_ref text)
                RETURNS integer
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    session_subject text;
                    session_owner uuid;
                    scope_hash bytea;
                    current_revision integer;
                    current_event text;
                    current_valid boolean;
                    next_revision integer;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    SELECT "SubjectRef", "ClientApplicationId"
                    INTO session_subject, session_owner
                    FROM tagekyc.raw_export_lock_verification_session_for_subject_consent(verification_session_id);
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'RAW_EXPORT_VERIFICATION_SESSION_NOT_FOUND';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88b2:subject-consent-authority:' || actor_id::text || ':' || session_owner::text || ':SubjectConsentWithdrawer'));
                    IF NOT tagekyc.raw_export_subject_consent_has_current_authority(actor_id, session_owner, 'SubjectConsentWithdrawer') THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SUBJECT_CONSENT_AUTHORITY_DENIED';
                    END IF;
                    IF decision_ref IS NOT NULL AND pg_catalog.btrim(decision_ref) = '' THEN
                        RAISE EXCEPTION 'RAW_EXPORT_DECISION_REF_INVALID';
                    END IF;

                    scope_hash := tagekyc.raw_export_consent_scope_hash(
                        verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner);
                    PERFORM pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(scope_hash));
                    SELECT "Revision", "EventType",
                           ("EventType" = 'Granted'
                            AND "ValidFromUtc" <= pg_catalog.transaction_timestamp()
                            AND ("ValidUntilUtc" IS NULL OR pg_catalog.transaction_timestamp() < "ValidUntilUtc"))
                    INTO current_revision, current_event, current_valid
                    FROM tagekyc.raw_export_subject_consent_events
                    WHERE "VerificationSessionId" = verification_session_id
                      AND "SubjectRef" = session_subject
                      AND "PolicyId" = policy_id
                      AND "PolicyVersion" = policy_version
                      AND "PurposeCode" = 'SubjectRawBiometricExport'
                      AND "RecipientClientApplicationId" = session_owner
                    ORDER BY "Revision" DESC
                    LIMIT 1;
                    current_revision := COALESCE(current_revision, 0);
                    IF current_revision <> expected_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_REVISION_CONFLICT';
                    END IF;
                    IF current_event IS DISTINCT FROM 'Granted' OR current_valid IS DISTINCT FROM true OR target_revision IS DISTINCT FROM current_revision THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CONSENT_STALE_TARGET_REVISION';
                    END IF;
                    next_revision := current_revision + 1;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_subject_consent_append_context', 'consent', true);
                    INSERT INTO tagekyc.raw_export_subject_consent_events
                        ("SubjectConsentRecordId","ConsentScopeHash","VerificationSessionId","SubjectRef","PolicyId","PolicyVersion","PurposeCode","RecipientClientApplicationId",
                         "Revision","EventType","TargetRevision","ExternalConsentArtifactRef","DecisionRef","WithdrawnByPrincipalId","RecordedAtUtc")
                    VALUES
                        (pg_catalog.gen_random_uuid(), scope_hash, verification_session_id, session_subject, policy_id, policy_version, 'SubjectRawBiometricExport', session_owner,
                         next_revision, 'Withdrawn', target_revision, external_consent_artifact_ref, decision_ref, actor_id, pg_catalog.transaction_timestamp());
                    RETURN next_revision;
                END;
                $$;

                CREATE TRIGGER tr_raw_export_subject_consent_authorities_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_subject_consent_authorities
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_subject_consent_insert('authority');

                CREATE TRIGGER tr_raw_export_subject_consent_events_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_subject_consent_events
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_subject_consent_insert('consent');

                CREATE TRIGGER tr_raw_export_subject_consent_classes_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_subject_consent_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_subject_consent_child_same_transaction();

                CREATE CONSTRAINT TRIGGER tr_raw_export_subject_consent_granted_classes_required
                AFTER INSERT ON tagekyc.raw_export_subject_consent_events
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_subject_consent_granted_has_classes();

                CREATE TRIGGER tr_raw_export_subject_consent_authorities_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_subject_consent_authorities
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_raw_export_subject_consent_events_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_subject_consent_events
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_raw_export_subject_consent_classes_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_subject_consent_classes
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                ALTER FUNCTION tagekyc.raw_export_consent_scope_hash(uuid,text,uuid,integer,text,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_consent_lock_key(bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_subject_consent_has_current_authority(uuid,uuid,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_lock_verification_session_for_subject_consent(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_resolve_subject_consent_for_authorization(uuid,uuid,integer) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_subject_consent_insert() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_subject_consent_child_same_transaction() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_subject_consent_granted_has_classes() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_subject_consent_authority(uuid,uuid,text,integer,text,integer,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text) OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_consent_scope_hash(uuid,text,uuid,integer,text,uuid),
                    tagekyc.raw_export_consent_lock_key(bytea),
                    tagekyc.raw_export_subject_consent_has_current_authority(uuid,uuid,text),
                    tagekyc.raw_export_lock_verification_session_for_subject_consent(uuid),
                    tagekyc.raw_export_resolve_subject_consent_for_authorization(uuid,uuid,integer),
                    tagekyc.enforce_raw_export_subject_consent_insert(),
                    tagekyc.enforce_raw_export_subject_consent_child_same_transaction(),
                    tagekyc.enforce_raw_export_subject_consent_granted_has_classes(),
                    tagekyc.raw_export_append_subject_consent_authority(uuid,uuid,text,integer,text,integer,text,timestamptz),
                    tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz),
                    tagekyc.raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text)
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_resolve_subject_consent_for_authorization(uuid,uuid,integer),
                    tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz),
                    tagekyc.raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text)
                TO tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_classes_append_only ON tagekyc.raw_export_subject_consent_classes;
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_events_append_only ON tagekyc.raw_export_subject_consent_events;
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_authorities_append_only ON tagekyc.raw_export_subject_consent_authorities;
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_granted_classes_required ON tagekyc.raw_export_subject_consent_events;
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_classes_insert_guard ON tagekyc.raw_export_subject_consent_classes;
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_events_insert_guard ON tagekyc.raw_export_subject_consent_events;
                DROP TRIGGER IF EXISTS tr_raw_export_subject_consent_authorities_insert_guard ON tagekyc.raw_export_subject_consent_authorities;
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_subject_consent_withdrawn(uuid,uuid,integer,integer,integer,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_subject_consent_granted(uuid,uuid,integer,text[],text,text,text,text,timestamptz);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_append_subject_consent_authority(uuid,uuid,text,integer,text,integer,text,timestamptz);
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_subject_consent_granted_has_classes();
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_subject_consent_child_same_transaction();
                DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_subject_consent_insert();
                DROP FUNCTION IF EXISTS tagekyc.raw_export_resolve_subject_consent_for_authorization(uuid,uuid,integer);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_subject_consent_has_current_authority(uuid,uuid,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_lock_verification_session_for_subject_consent(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_consent_lock_key(bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_consent_scope_hash(uuid,text,uuid,integer,text,uuid);
                REVOKE UPDATE ON tagekyc.verification_sessions FROM tagekyc_raw_export_deployer;
                REVOKE SELECT ON tagekyc.verification_sessions FROM tagekyc_raw_export_deployer, tagekyc_runtime;
                """);

            migrationBuilder.DropTable(
                name: "raw_export_subject_consent_authorities",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_subject_consent_classes",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_subject_consent_events",
                schema: "tagekyc");
        }
    }
}
