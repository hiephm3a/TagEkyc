using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1ACaptureAcceptanceSurface : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_capture_acceptance_events",
                schema: "tagekyc",
                columns: table => new
                {
                    CaptureAcceptanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CaptureArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureRevision = table.Column<int>(type: "integer", nullable: false),
                    SessionChallengeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AcceptedEvidenceRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptancePolicyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AcceptancePolicyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_capture_acceptance_events", x => x.CaptureAcceptanceId);
                    table.UniqueConstraint("uq_raw_export_capture_acceptance_artifact", x => new { x.VerificationSessionId, x.RawClass, x.CaptureArtifactId, x.CaptureRevision });
                    table.UniqueConstraint("uq_raw_export_capture_acceptance_revision", x => new { x.VerificationSessionId, x.RawClass, x.CaptureRevision });
                    table.ForeignKey(
                        name: "fk_raw_export_capture_acceptance_artifact",
                        column: x => x.CaptureArtifactId,
                        principalSchema: "tagekyc",
                        principalTable: "capture_artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_capture_acceptance_session",
                        column: x => x.VerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_session_capture_selections",
                schema: "tagekyc",
                columns: table => new
                {
                    SessionCaptureSelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CaptureAcceptanceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_session_capture_selections", x => x.SessionCaptureSelectionId);
                    table.UniqueConstraint("uq_raw_export_session_capture_selection_class", x => new { x.VerificationSessionId, x.RawClass });
                    table.ForeignKey(
                        name: "fk_raw_export_session_selection_acceptance",
                        column: x => x.CaptureAcceptanceId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_capture_acceptance_events",
                        principalColumn: "CaptureAcceptanceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_capture_acceptance_events_CaptureArtifactId",
                schema: "tagekyc",
                table: "raw_export_capture_acceptance_events",
                column: "CaptureArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_session_capture_selections_CaptureAcceptanceId",
                schema: "tagekyc",
                table: "raw_export_session_capture_selections",
                column: "CaptureAcceptanceId");

            migrationBuilder.Sql(
                """
                REVOKE ALL ON
                    tagekyc.raw_export_capture_acceptance_events,
                    tagekyc.raw_export_session_capture_selections
                FROM PUBLIC, tagekyc_runtime;

                GRANT SELECT ON
                    tagekyc.capture_artifacts,
                    tagekyc.raw_export_capture_acceptance_events,
                    tagekyc.raw_export_session_capture_selections
                TO tagekyc_raw_export_deployer;

                GRANT INSERT ON
                    tagekyc.raw_export_capture_acceptance_events,
                    tagekyc.raw_export_session_capture_selections
                TO tagekyc_raw_export_deployer;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_capture_acceptance_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    expected_context text;
                    append_context text;
                BEGIN
                    expected_context := TG_ARGV[0];
                    append_context := pg_catalog.current_setting(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        true);

                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR append_context IS DISTINCT FROM expected_context THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_DIRECT_INSERT_UNSUPPORTED';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_append_capture_acceptance(
                    verification_session_id uuid,
                    client_application_id uuid,
                    raw_class text,
                    capture_artifact_id uuid,
                    capture_revision integer,
                    session_challenge_hash text,
                    accepted_evidence_ref text,
                    acceptance_policy_id text,
                    acceptance_policy_version integer)
                RETURNS uuid
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    artifact_session_id uuid;
                    latest_revision integer;
                    capture_acceptance_id uuid := pg_catalog.gen_random_uuid();
                    previous_context text;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();

                    SELECT artifact."VerificationSessionId"
                    INTO artifact_session_id
                    FROM tagekyc.capture_artifacts AS artifact
                    WHERE artifact."Id" = capture_artifact_id;

                    IF artifact_session_id IS DISTINCT FROM verification_session_id THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ARTIFACT_SESSION_MISMATCH';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtextextended(
                            'tip88c1a:capture-acceptance:' ||
                            verification_session_id::text || ':' || raw_class,
                            0));

                    SELECT COALESCE(MAX(event."CaptureRevision"), 0)
                    INTO latest_revision
                    FROM tagekyc.raw_export_capture_acceptance_events AS event
                    WHERE event."VerificationSessionId" = verification_session_id
                      AND event."RawClass" = raw_class;

                    IF capture_revision IS NULL
                       OR capture_revision < 1
                       OR capture_revision <> latest_revision + 1 THEN
                        RAISE EXCEPTION 'RAW_EXPORT_CAPTURE_ACCEPTANCE_REVISION_CONFLICT';
                    END IF;

                    previous_context := pg_catalog.current_setting(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        true);
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        'event',
                        true);

                    BEGIN
                        INSERT INTO tagekyc.raw_export_capture_acceptance_events
                            ("CaptureAcceptanceId",
                             "VerificationSessionId",
                             "ClientApplicationId",
                             "RawClass",
                             "CaptureArtifactId",
                             "CaptureRevision",
                             "SessionChallengeHash",
                             "AcceptedEvidenceRef",
                             "AcceptedAtUtc",
                             "AcceptancePolicyId",
                             "AcceptancePolicyVersion")
                        VALUES
                            (capture_acceptance_id,
                             verification_session_id,
                             client_application_id,
                             raw_class,
                             capture_artifact_id,
                             capture_revision,
                             session_challenge_hash,
                             accepted_evidence_ref,
                             pg_catalog.transaction_timestamp(),
                             acceptance_policy_id,
                             acceptance_policy_version);
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_capture_acceptance_append_context',
                            COALESCE(previous_context, ''),
                            true);
                        RAISE;
                    END;

                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        COALESCE(previous_context, ''),
                        true);
                    RETURN capture_acceptance_id;
                END;
                $$;

                CREATE OR REPLACE FUNCTION tagekyc.raw_export_select_session_capture_acceptance(
                    verification_session_id uuid,
                    raw_class text,
                    capture_acceptance_id uuid)
                RETURNS uuid
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    accepted_session_id uuid;
                    accepted_raw_class text;
                    session_capture_selection_id uuid := pg_catalog.gen_random_uuid();
                    previous_context text;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();

                    SELECT event."VerificationSessionId", event."RawClass"
                    INTO accepted_session_id, accepted_raw_class
                    FROM tagekyc.raw_export_capture_acceptance_events AS event
                    WHERE event."CaptureAcceptanceId" = capture_acceptance_id;

                    IF FOUND AND (
                        accepted_session_id IS DISTINCT FROM verification_session_id
                        OR accepted_raw_class IS DISTINCT FROM raw_class) THEN
                        RAISE EXCEPTION 'RAW_EXPORT_SESSION_CAPTURE_SELECTION_MISMATCH';
                    END IF;

                    previous_context := pg_catalog.current_setting(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        true);
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        'selection',
                        true);

                    BEGIN
                        INSERT INTO tagekyc.raw_export_session_capture_selections
                            ("SessionCaptureSelectionId",
                             "VerificationSessionId",
                             "RawClass",
                             "CaptureAcceptanceId")
                        VALUES
                            (session_capture_selection_id,
                             verification_session_id,
                             raw_class,
                             capture_acceptance_id);
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_capture_acceptance_append_context',
                            COALESCE(previous_context, ''),
                            true);
                        RAISE;
                    END;

                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_capture_acceptance_append_context',
                        COALESCE(previous_context, ''),
                        true);
                    RETURN session_capture_selection_id;
                END;
                $$;

                CREATE TRIGGER tr_raw_export_capture_acceptance_events_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_capture_acceptance_events
                FOR EACH ROW EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_capture_acceptance_insert('event');

                CREATE TRIGGER tr_raw_export_session_capture_selections_insert_guard
                BEFORE INSERT ON tagekyc.raw_export_session_capture_selections
                FOR EACH ROW EXECUTE FUNCTION
                    tagekyc.enforce_raw_export_capture_acceptance_insert('selection');

                CREATE TRIGGER tr_raw_export_capture_acceptance_events_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_capture_acceptance_events
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                CREATE TRIGGER tr_raw_export_session_capture_selections_append_only
                BEFORE UPDATE OR DELETE ON tagekyc.raw_export_session_capture_selections
                FOR EACH ROW EXECUTE FUNCTION tagekyc.deny_append_only_mutation();

                ALTER FUNCTION tagekyc.enforce_raw_export_capture_acceptance_insert()
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_append_capture_acceptance(
                    uuid,uuid,text,uuid,integer,text,text,text,integer)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_select_session_capture_acceptance(
                    uuid,text,uuid)
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.enforce_raw_export_capture_acceptance_insert(),
                    tagekyc.raw_export_append_capture_acceptance(
                        uuid,uuid,text,uuid,integer,text,text,text,integer),
                    tagekyc.raw_export_select_session_capture_acceptance(uuid,text,uuid)
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_append_capture_acceptance(
                        uuid,uuid,text,uuid,integer,text,text,text,integer),
                    tagekyc.raw_export_select_session_capture_acceptance(uuid,text,uuid)
                TO tagekyc_runtime;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS
                    tr_raw_export_session_capture_selections_append_only
                    ON tagekyc.raw_export_session_capture_selections;
                DROP TRIGGER IF EXISTS
                    tr_raw_export_capture_acceptance_events_append_only
                    ON tagekyc.raw_export_capture_acceptance_events;
                DROP TRIGGER IF EXISTS
                    tr_raw_export_session_capture_selections_insert_guard
                    ON tagekyc.raw_export_session_capture_selections;
                DROP TRIGGER IF EXISTS
                    tr_raw_export_capture_acceptance_events_insert_guard
                    ON tagekyc.raw_export_capture_acceptance_events;

                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_select_session_capture_acceptance(uuid,text,uuid);
                DROP FUNCTION IF EXISTS
                    tagekyc.raw_export_append_capture_acceptance(
                        uuid,uuid,text,uuid,integer,text,text,text,integer);
                DROP FUNCTION IF EXISTS
                    tagekyc.enforce_raw_export_capture_acceptance_insert();
                """);

            migrationBuilder.DropTable(
                name: "raw_export_session_capture_selections",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_capture_acceptance_events",
                schema: "tagekyc");
        }
    }
}
