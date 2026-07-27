using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B4RawExportJobFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_job_identities",
                schema: "tagekyc",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectRef = table.Column<string>(type: "text", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    PurposeCode = table.Column<string>(type: "text", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportMode = table.Column<string>(type: "text", nullable: false),
                    PermitExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    JobExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IdempotencyFingerprintHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_b4_job_identities", x => x.JobId);
                    table.UniqueConstraint("UQ_b4_job_identity_permit", x => x.PermitId);
                    table.CheckConstraint("CK_b4_job_identity_deadlines", "\"JobExpiresAt\" = \"PermitExpiresAt\"");
                    table.CheckConstraint("CK_b4_job_identity_export_mode", "\"ExportMode\" IN ('ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained')");
                    table.CheckConstraint("CK_b4_job_identity_fingerprint_length", "octet_length(\"IdempotencyFingerprintHash\") = 32");
                    table.CheckConstraint("CK_b4_job_identity_policy_version", "\"PolicyVersion\" >= 1");
                    table.CheckConstraint("CK_b4_job_identity_schema_version", "\"SchemaVersion\" = 1");
                    table.CheckConstraint("CK_raw_export_job_identities_IdempotencyKey", "length(\"IdempotencyKey\") BETWEEN 1 AND 256 AND \"IdempotencyKey\" COLLATE \"C\" ~ '^[A-Za-z0-9._:-]+$'");
                    table.ForeignKey(
                        name: "FK_b4_job_identity_decision",
                        column: x => x.AuthorizationDecisionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_decisions",
                        principalColumn: "ExportDecisionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_b4_job_identity_permit",
                        column: x => x.PermitId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_authorization_permits",
                        principalColumn: "PermitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_b4_job_identity_session",
                        column: x => x.VerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_job_attempts",
                schema: "tagekyc",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptOrdinal = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<string>(type: "text", nullable: false),
                    LeaseOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    AcquiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InitialLeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_b4_job_attempts", x => x.AttemptId);
                    table.UniqueConstraint("UQ_b4_job_attempt_fence", x => new { x.JobId, x.AttemptId, x.FencingToken });
                    table.UniqueConstraint("UQ_b4_job_attempt_ordinal", x => new { x.JobId, x.AttemptOrdinal });
                    table.CheckConstraint("CK_raw_export_job_attempts_AttemptOrdinal", "\"AttemptOrdinal\" >= 0");
                    table.CheckConstraint("CK_raw_export_job_attempts_FencingToken", "\"FencingToken\" >= 1");
                    table.CheckConstraint("CK_raw_export_job_attempts_LeaseTime", "\"InitialLeaseExpiresAt\" > \"AcquiredAt\"");
                    table.CheckConstraint("CK_raw_export_job_attempts_Phase", "\"Phase\" = 'Assembling'");
                    table.ForeignKey(
                        name: "FK_b4_job_attempt_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_job_classes",
                schema: "tagekyc",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawClass = table.Column<string>(type: "text", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_b4_job_classes", x => new { x.JobId, x.RawClass });
                    table.UniqueConstraint("UQ_b4_job_class_ordinal", x => new { x.JobId, x.Ordinal });
                    table.CheckConstraint("CK_b4_job_class_ordinal", "\"Ordinal\" >= 0");
                    table.CheckConstraint("CK_b4_job_class_raw_class", "\"RawClass\" IN ('ChipDg1','ChipDg2Portrait','ChipDg13','ChipDg15','ChipSod','AaChallenge','AaResponse','LiveSelfieImage','LivenessMedia','HandSignatureImage')");
                    table.ForeignKey(
                        name: "FK_b4_job_class_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_job_operational_heads",
                schema: "tagekyc",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseOwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_b4_job_operational_heads", x => x.JobId);
                    table.CheckConstraint("CK_b4_job_head_shape", "\"CurrentState\" IN ('Claimed','Assembling','AssemblySealed','Protecting','PackageSealed','ReadyForDelivery','DeliveryInProgress','DeliveryOutcomeUnknown','Delivered','ReconciliationExpired','TerminalFailed','Cancelled','Expired') AND \"Revision\" >= 0 AND \"FencingToken\" >= 0 AND ((\"CurrentAttemptId\" IS NULL AND \"FencingToken\" = 0) OR (\"CurrentAttemptId\" IS NOT NULL AND \"FencingToken\" >= 1)) AND ((\"LeaseOwnerId\" IS NULL AND \"LeaseExpiresAt\" IS NULL) OR (\"LeaseOwnerId\" IS NOT NULL AND \"LeaseExpiresAt\" IS NOT NULL)) AND ((\"CurrentState\" = 'Claimed' AND \"Revision\" = 0 AND \"CurrentAttemptId\" IS NULL AND \"LeaseOwnerId\" IS NULL) OR (\"CurrentState\" = 'Assembling' AND \"CurrentAttemptId\" IS NOT NULL) OR (\"CurrentState\" IN ('AssemblySealed','Protecting','PackageSealed','ReadyForDelivery','DeliveryInProgress','DeliveryOutcomeUnknown','Delivered','ReconciliationExpired') AND \"CurrentAttemptId\" IS NOT NULL) OR (\"CurrentState\" IN ('TerminalFailed','Cancelled','Expired') AND \"LeaseOwnerId\" IS NULL))");
                    table.ForeignKey(
                        name: "FK_b4_job_head_attempt",
                        columns: x => new { x.JobId, x.CurrentAttemptId, x.FencingToken },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_attempts",
                        principalColumns: new[] { "JobId", "AttemptId", "FencingToken" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_b4_job_head_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_job_transitions",
                schema: "tagekyc",
                columns: table => new
                {
                    TransitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultingRevision = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    FromState = table.Column<string>(type: "text", nullable: true),
                    ToState = table.Column<string>(type: "text", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    ResultingLeaseOwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultingLeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_b4_job_transitions", x => x.TransitionId);
                    table.UniqueConstraint("UQ_b4_job_transition_revision", x => new { x.JobId, x.ResultingRevision });
                    table.CheckConstraint("CK_b4_job_transition_event_shape", "(\"EventType\" = 'JobBound' AND \"FromState\" IS NULL AND \"ToState\" = 'Claimed' AND \"AttemptId\" IS NULL AND \"FencingToken\" = 0 AND \"ResultingLeaseOwnerId\" IS NULL AND \"ResultingLeaseExpiresAt\" IS NULL AND \"FailureCode\" IS NULL) OR (\"EventType\" = 'LeaseAcquired' AND \"FromState\" = 'Claimed' AND \"ToState\" = 'Assembling' AND \"AttemptId\" IS NOT NULL AND \"FencingToken\" >= 1 AND \"ResultingLeaseOwnerId\" IS NOT NULL AND \"ResultingLeaseExpiresAt\" IS NOT NULL AND \"FailureCode\" IS NULL) OR (\"EventType\" IN ('LeaseRenewed','LeaseAcquiredAfterRetryableFailure','LeaseReclaimed') AND \"FromState\" = 'Assembling' AND \"ToState\" = 'Assembling' AND \"AttemptId\" IS NOT NULL AND \"FencingToken\" >= 1 AND \"ResultingLeaseOwnerId\" IS NOT NULL AND \"ResultingLeaseExpiresAt\" IS NOT NULL AND \"FailureCode\" IS NULL) OR (\"EventType\" = 'AttemptFailedRetryable' AND \"FromState\" = 'Assembling' AND \"ToState\" = 'Assembling' AND \"AttemptId\" IS NOT NULL AND \"FencingToken\" >= 1 AND \"ResultingLeaseOwnerId\" IS NULL AND \"ResultingLeaseExpiresAt\" IS NULL AND \"FailureCode\" = 'ATTEMPT_EXECUTION_FAILED_RETRYABLE') OR (\"EventType\" = 'JobTerminalFailed' AND ((\"FromState\" = 'Claimed' AND \"AttemptId\" IS NULL AND \"FencingToken\" = 0) OR (\"FromState\" = 'Assembling' AND \"AttemptId\" IS NOT NULL AND \"FencingToken\" >= 1)) AND \"ToState\" = 'TerminalFailed' AND \"ResultingLeaseOwnerId\" IS NULL AND \"ResultingLeaseExpiresAt\" IS NULL AND \"FailureCode\" IN ('AUTHORITY_REVALIDATION_FAILED','ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE','JOB_GRAPH_INVARIANT_FAILURE','MODE_RETRY_NOT_AUTHORIZED')) OR (\"EventType\" = 'JobCancelled' AND ((\"FromState\" = 'Claimed' AND \"AttemptId\" IS NULL AND \"FencingToken\" = 0) OR (\"FromState\" = 'Assembling' AND \"AttemptId\" IS NOT NULL AND \"FencingToken\" >= 1)) AND \"ToState\" = 'Cancelled' AND \"ResultingLeaseOwnerId\" IS NULL AND \"ResultingLeaseExpiresAt\" IS NULL AND \"FailureCode\" = 'REQUEST_CANCELLED') OR (\"EventType\" = 'JobExpired' AND ((\"FromState\" = 'Claimed' AND \"AttemptId\" IS NULL AND \"FencingToken\" = 0) OR (\"FromState\" = 'Assembling' AND \"AttemptId\" IS NOT NULL AND \"FencingToken\" >= 1)) AND \"ToState\" = 'Expired' AND \"ResultingLeaseOwnerId\" IS NULL AND \"ResultingLeaseExpiresAt\" IS NULL AND \"FailureCode\" = 'PERMIT_OR_JOB_EXPIRED')");
                    table.CheckConstraint("CK_b4_job_transition_revision", "\"ResultingRevision\" >= 0 AND \"FencingToken\" >= 0");
                    table.ForeignKey(
                        name: "FK_b4_job_transition_attempt",
                        columns: x => new { x.JobId, x.AttemptId, x.FencingToken },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_attempts",
                        principalColumns: new[] { "JobId", "AttemptId", "FencingToken" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_b4_job_transition_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_b4_job_identity_decision",
                schema: "tagekyc",
                table: "raw_export_job_identities",
                column: "AuthorizationDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_b4_job_identity_session",
                schema: "tagekyc",
                table: "raw_export_job_identities",
                column: "VerificationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_b4_job_head_attempt_fence",
                schema: "tagekyc",
                table: "raw_export_job_operational_heads",
                columns: new[] { "JobId", "CurrentAttemptId", "FencingToken" });

            migrationBuilder.CreateIndex(
                name: "IX_b4_job_transition_attempt_fence",
                schema: "tagekyc",
                table: "raw_export_job_transitions",
                columns: new[] { "JobId", "AttemptId", "FencingToken" });

            migrationBuilder.Sql(B4Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(B4DownSql);

            migrationBuilder.DropTable(
                name: "raw_export_job_classes",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_job_operational_heads",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_job_transitions",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_job_attempts",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_job_identities",
                schema: "tagekyc");
        }

        private const string B4Sql =
            """
            CREATE FUNCTION tagekyc.enforce_raw_export_job_identity_insert()
            RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
            BEGIN
              IF TG_OP <> 'INSERT' OR current_user <> 'tagekyc_raw_export_deployer'
                 OR current_setting('tagekyc.raw_export_job_mutation_context',true) IS DISTINCT FROM 'job_identity'
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_IDENTITY_MUTATION_UNSUPPORTED'; END IF;
              RETURN NEW;
            END $$;
            CREATE FUNCTION tagekyc.enforce_raw_export_job_class_insert()
            RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
            DECLARE px xid;
            BEGIN
              IF TG_OP <> 'INSERT' OR current_user <> 'tagekyc_raw_export_deployer'
                 OR current_setting('tagekyc.raw_export_job_mutation_context',true) IS DISTINCT FROM 'job_class'
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_CLASS_MUTATION_UNSUPPORTED'; END IF;
              SELECT xmin INTO px FROM tagekyc.raw_export_job_identities WHERE "JobId"=NEW."JobId";
              IF NOT FOUND OR px <> pg_current_xact_id()::xid THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_CLASS_APPEND_UNSUPPORTED';
              END IF;
              RETURN NEW;
            END $$;
            CREATE FUNCTION tagekyc.enforce_raw_export_job_attempt_insert()
            RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
            DECLARE h tagekyc.raw_export_job_operational_heads%ROWTYPE; next_ordinal integer;
            BEGIN
              IF TG_OP <> 'INSERT' OR current_user <> 'tagekyc_raw_export_deployer'
                 OR current_setting('tagekyc.raw_export_job_mutation_context',true) IS DISTINCT FROM 'job_attempt'
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ATTEMPT_MUTATION_UNSUPPORTED'; END IF;
              SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=NEW."JobId" FOR SHARE;
              SELECT COALESCE(max("AttemptOrdinal"),-1)+1 INTO next_ordinal
              FROM tagekyc.raw_export_job_attempts WHERE "JobId"=NEW."JobId";
              IF h."JobId" IS NULL OR h."CurrentState" NOT IN ('Claimed','Assembling')
                 OR NEW."AttemptOrdinal"<>next_ordinal
                 OR NEW."AttemptId" IS NOT DISTINCT FROM h."CurrentAttemptId"
                 OR NEW."FencingToken"<>h."FencingToken"+1
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ATTEMPT_GRAPH_INVALID'; END IF;
              RETURN NEW;
            END $$;
            CREATE FUNCTION tagekyc.enforce_raw_export_job_transition_insert()
            RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
            DECLARE h tagekyc.raw_export_job_operational_heads%ROWTYPE; previous_revision bigint; previous_state text;
            BEGIN
              IF TG_OP <> 'INSERT' OR current_user <> 'tagekyc_raw_export_deployer'
                 OR current_setting('tagekyc.raw_export_job_mutation_context',true) IS DISTINCT FROM 'job_transition'
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSITION_MUTATION_UNSUPPORTED'; END IF;
              SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=NEW."JobId" FOR SHARE;
              SELECT "ResultingRevision","ToState" INTO previous_revision,previous_state
              FROM tagekyc.raw_export_job_transitions
              WHERE "JobId"=NEW."JobId"
              ORDER BY "ResultingRevision" DESC LIMIT 1;
              IF h."JobId" IS NULL
                 OR NEW."ResultingRevision"<>h."Revision"
                 OR NEW."ToState"<>h."CurrentState"
                 OR NEW."AttemptId" IS DISTINCT FROM h."CurrentAttemptId"
                 OR NEW."FencingToken"<>h."FencingToken"
                 OR NEW."ResultingLeaseOwnerId" IS DISTINCT FROM h."LeaseOwnerId"
                 OR NEW."ResultingLeaseExpiresAt" IS DISTINCT FROM h."LeaseExpiresAt"
                 OR NEW."OccurredAt"<>h."UpdatedAt"
                 OR (NEW."ResultingRevision"=0 AND (previous_revision IS NOT NULL OR NEW."FromState" IS NOT NULL))
                 OR (NEW."ResultingRevision">0 AND
                     (previous_revision IS NULL OR previous_revision<>NEW."ResultingRevision"-1
                      OR previous_state IS DISTINCT FROM NEW."FromState"))
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSITION_GRAPH_INVALID'; END IF;
              RETURN NEW;
            END $$;
            CREATE FUNCTION tagekyc.enforce_raw_export_job_head_mutation()
            RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
            DECLARE c text; px xid;
            BEGIN
              c := current_setting('tagekyc.raw_export_job_mutation_context',true);
              IF current_user <> 'tagekyc_raw_export_deployer'
                 OR c IS NULL
                 OR c NOT IN ('job_head_insert','job_head_acquire','job_head_renew','job_head_release','job_head_terminal')
                 OR TG_OP = 'DELETE'
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED'; END IF;
              IF TG_OP='INSERT' THEN
                SELECT xmin INTO px FROM tagekyc.raw_export_job_identities WHERE "JobId"=NEW."JobId";
                IF c<>'job_head_insert' OR NOT FOUND OR px<>pg_current_xact_id()::xid
                   OR NEW."CurrentState"<>'Claimed' OR NEW."Revision"<>0
                   OR NEW."CurrentAttemptId" IS NOT NULL OR NEW."LeaseOwnerId" IS NOT NULL
                   OR NEW."LeaseExpiresAt" IS NOT NULL OR NEW."FencingToken"<>0
                THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID'; END IF;
              ELSIF NEW."JobId"<>OLD."JobId" OR NEW."Revision"<>OLD."Revision"+1 OR NEW."UpdatedAt"<OLD."UpdatedAt" THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
              ELSIF c='job_head_acquire' AND NOT (
                OLD."CurrentState" IN ('Claimed','Assembling') AND NEW."CurrentState"='Assembling'
                AND NEW."CurrentAttemptId" IS NOT NULL
                AND NEW."CurrentAttemptId" IS DISTINCT FROM OLD."CurrentAttemptId"
                AND NEW."FencingToken"=OLD."FencingToken"+1
                AND NEW."LeaseOwnerId" IS NOT NULL AND NEW."LeaseExpiresAt">NEW."UpdatedAt") THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
              ELSIF c='job_head_renew' AND NOT (
                OLD."CurrentState"='Assembling' AND NEW."CurrentState"='Assembling'
                AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId"
                AND NEW."FencingToken"=OLD."FencingToken"
                AND NEW."LeaseOwnerId"=OLD."LeaseOwnerId"
                AND NEW."LeaseOwnerId" IS NOT NULL AND NEW."LeaseExpiresAt">NEW."UpdatedAt") THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
              ELSIF c='job_head_release' AND NOT (
                OLD."CurrentState"='Assembling' AND NEW."CurrentState"='Assembling'
                AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId"
                AND NEW."FencingToken"=OLD."FencingToken"
                AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
              ELSIF c='job_head_terminal' AND NOT (
                OLD."CurrentState" IN ('Claimed','Assembling')
                AND NEW."CurrentState" IN ('TerminalFailed','Cancelled','Expired')
                AND NEW."CurrentAttemptId" IS NOT DISTINCT FROM OLD."CurrentAttemptId"
                AND NEW."FencingToken"=OLD."FencingToken"
                AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
              ELSIF c='job_head_insert' THEN
                RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED';
              END IF;
              RETURN NEW;
            END $$;
            CREATE FUNCTION tagekyc.enforce_raw_export_job_identity_has_classes()
            RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM tagekyc.raw_export_job_classes WHERE "JobId"=NEW."JobId")
              THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_CLASSES_REQUIRED'; END IF;
              RETURN NULL;
            END $$;

            CREATE TRIGGER tr_b4_job_identity_insert_guard BEFORE INSERT ON tagekyc.raw_export_job_identities FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_identity_insert();
            CREATE TRIGGER tr_b4_job_identity_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_job_identities FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_identity_insert();
            CREATE TRIGGER tr_b4_job_class_insert_guard BEFORE INSERT ON tagekyc.raw_export_job_classes FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_class_insert();
            CREATE TRIGGER tr_b4_job_class_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_job_classes FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_class_insert();
            CREATE TRIGGER tr_b4_job_attempt_insert_guard BEFORE INSERT ON tagekyc.raw_export_job_attempts FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_attempt_insert();
            CREATE TRIGGER tr_b4_job_attempt_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_job_attempts FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_attempt_insert();
            CREATE TRIGGER tr_b4_job_transition_insert_guard BEFORE INSERT ON tagekyc.raw_export_job_transitions FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_transition_insert();
            CREATE TRIGGER tr_b4_job_transition_append_only BEFORE UPDATE OR DELETE ON tagekyc.raw_export_job_transitions FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_transition_insert();
            CREATE TRIGGER tr_b4_job_head_mutation_guard BEFORE INSERT OR UPDATE OR DELETE ON tagekyc.raw_export_job_operational_heads FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_head_mutation();
            CREATE CONSTRAINT TRIGGER tr_b4_job_identity_has_classes AFTER INSERT ON tagekyc.raw_export_job_identities DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_job_identity_has_classes();

            CREATE FUNCTION tagekyc.raw_export_read_job_binding_inputs(principal_id uuid,client_application_id uuid,permit_id uuid)
            RETURNS TABLE("ProjectionOutcome" text,"ExistingJobId" uuid,"ExistingFingerprintHash" bytea,"AuthorizationDecisionId" uuid,"DecisionPrincipalId" uuid,"DecisionClientApplicationId" uuid,"VerificationSessionId" uuid,"SubjectRef" text,"PolicyId" uuid,"PolicyVersion" integer,"PurposeCode" text,"DecisionRecipientClientApplicationId" uuid,"PermitRecipientClientApplicationId" uuid,"PermitExpiresAt" timestamptz,"PermitSchemaVersion" integer,"ExportMode" text,"ClosureType" text,"ClassOrdinal" integer,"RawClass" text,"EvaluatedAtUtc" timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; owned boolean; valid_graph boolean;
            BEGIN
              a:=tagekyc.raw_export_current_actor();
              IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              SELECT EXISTS(SELECT 1 FROM tagekyc.raw_export_authorization_permits p JOIN tagekyc.raw_export_authorization_decisions d ON d."ExportDecisionId"=p."AuthorizationDecisionId" WHERE p."PermitId"=permit_id AND d."PrincipalId"=principal_id AND d."ClientApplicationId"=client_application_id) INTO owned;
              IF NOT owned THEN RETURN; END IF;
              SELECT EXISTS(
                SELECT 1 FROM tagekyc.raw_export_authorization_permits p
                JOIN tagekyc.raw_export_authorization_decisions d ON d."ExportDecisionId"=p."AuthorizationDecisionId"
                JOIN tagekyc.raw_export_policy_versions v ON v."PolicyId"=p."PolicyId" AND v."PolicyVersion"=p."PolicyVersion"
                JOIN LATERAL (SELECT c."ClosureType" FROM tagekyc.raw_export_policy_closures c WHERE c."PolicyId"=p."PolicyId" AND c."PolicyVersion"=p."PolicyVersion" ORDER BY c."ClosedAtUtc" DESC LIMIT 1) c ON true
                WHERE p."PermitId"=permit_id AND d."Outcome"='Authorized' AND d."DecisionExpiresAtUtc"=p."DecisionExpiresAtUtc"
                  AND d."ResolvedVerificationSessionId"=p."ResolvedVerificationSessionId" AND d."RecipientClientApplicationId"=p."RecipientClientApplicationId"
                  AND c."ClosureType"='CatalogApproved' AND EXISTS(SELECT 1 FROM tagekyc.raw_export_permit_classes pc WHERE pc."PermitId"=p."PermitId")
              ) INTO valid_graph;
              IF NOT valid_graph THEN
                RETURN QUERY SELECT 'GraphInvalid'::text,j."JobId",NULL::bytea,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::text,NULL::uuid,NULL::integer,NULL::text,NULL::uuid,NULL::uuid,NULL::timestamptz,NULL::integer,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::timestamptz
                FROM (SELECT NULL::uuid "JobId") z LEFT JOIN tagekyc.raw_export_job_identities j ON j."PermitId"=permit_id;
                RETURN;
              END IF;
              RETURN QUERY
              SELECT 'Valid'::text,j."JobId",j."IdempotencyFingerprintHash",p."AuthorizationDecisionId",d."PrincipalId",d."ClientApplicationId",p."ResolvedVerificationSessionId",p."SubjectRef",p."PolicyId",p."PolicyVersion",p."PurposeCode",d."RecipientClientApplicationId",p."RecipientClientApplicationId",p."DecisionExpiresAtUtc",p."SchemaVersion",v."Mode"::text,c."ClosureType"::text,pc."Ordinal",pc."RawClass",d."EligibilityEvaluatedAtUtc"
              FROM tagekyc.raw_export_authorization_permits p
              JOIN tagekyc.raw_export_authorization_decisions d ON d."ExportDecisionId"=p."AuthorizationDecisionId"
              JOIN tagekyc.raw_export_policy_versions v ON v."PolicyId"=p."PolicyId" AND v."PolicyVersion"=p."PolicyVersion"
              JOIN LATERAL (SELECT x."ClosureType" FROM tagekyc.raw_export_policy_closures x WHERE x."PolicyId"=p."PolicyId" AND x."PolicyVersion"=p."PolicyVersion" ORDER BY x."ClosedAtUtc" DESC LIMIT 1) c ON true
              JOIN tagekyc.raw_export_permit_classes pc ON pc."PermitId"=p."PermitId"
              LEFT JOIN tagekyc.raw_export_job_identities j ON j."PermitId"=p."PermitId"
              WHERE p."PermitId"=permit_id ORDER BY pc."Ordinal";
            END $$;

            CREATE FUNCTION tagekyc.raw_export_claim_or_read_job(prospective_job_id uuid,permit_id uuid,authorization_decision_id uuid,principal_id uuid,client_application_id uuid,created_by_api_key_id uuid,verification_session_id uuid,subject_ref text,policy_id uuid,policy_version integer,purpose_code text,recipient_client_application_id uuid,export_mode text,permit_expires_at timestamptz,job_expires_at timestamptz,idempotency_key text,fingerprint_hash bytea,raw_classes text[])
            RETURNS TABLE(outcome text,job_id uuid) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; p tagekyc.raw_export_authorization_permits%ROWTYPE; d tagekyc.raw_export_authorization_decisions%ROWTYPE; existing tagekyc.raw_export_job_identities%ROWTYPE; actual_classes text[]; mode text; closure text; now_ timestamptz; session_owner uuid; session_subject text; session_state text;
            BEGIN
              IF current_setting('transaction_isolation') <> 'read committed' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID'; END IF;
              SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              IF idempotency_key IS NULL OR length(idempotency_key) NOT BETWEEN 1 AND 256 OR idempotency_key COLLATE "C" !~ '^[A-Za-z0-9._:-]+$' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED'; END IF;
              SELECT "ClientApplicationId","SubjectRef","State"
              INTO session_owner,session_subject,session_state
              FROM tagekyc.raw_export_lock_verification_session_for_authorization(verification_session_id);
              IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE'; END IF;
              SELECT * INTO p FROM tagekyc.raw_export_authorization_permits WHERE "PermitId"=permit_id FOR UPDATE;
              SELECT * INTO existing
              FROM tagekyc.raw_export_job_identities
              WHERE "PermitId"=permit_id
                AND "PrincipalId"=principal_id
                AND "ClientApplicationId"=client_application_id;
              IF FOUND THEN
                IF existing."IdempotencyFingerprintHash"=fingerprint_hash AND existing."IdempotencyKey"=idempotency_key THEN
                  SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
                  RETURN QUERY SELECT 'ExistingMatch'::text,existing."JobId";
                ELSE
                  SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
                  RETURN QUERY SELECT 'FingerprintConflict'::text,NULL::uuid;
                END IF;
                RETURN;
              END IF;
              SELECT * INTO d FROM tagekyc.raw_export_authorization_decisions WHERE "ExportDecisionId"=authorization_decision_id;
              SELECT array_agg(pc."RawClass" ORDER BY pc."Ordinal") INTO actual_classes FROM tagekyc.raw_export_permit_classes pc WHERE pc."PermitId"=permit_id;
              SELECT v."Mode"::text INTO mode FROM tagekyc.raw_export_policy_versions v WHERE v."PolicyId"=policy_id AND v."PolicyVersion"=policy_version;
              SELECT c."ClosureType"::text INTO closure FROM tagekyc.raw_export_policy_closures c WHERE c."PolicyId"=policy_id AND c."PolicyVersion"=policy_version ORDER BY c."ClosedAtUtc" DESC LIMIT 1;
              IF p."PermitId" IS NULL OR d."Outcome"<>'Authorized' OR d."PrincipalId"<>principal_id OR d."ClientApplicationId"<>client_application_id OR p."AuthorizationDecisionId"<>authorization_decision_id OR p."ResolvedVerificationSessionId"<>verification_session_id OR p."SubjectRef"<>subject_ref OR p."PolicyId"<>policy_id OR p."PolicyVersion"<>policy_version OR p."PurposeCode"<>purpose_code OR p."RecipientClientApplicationId"<>recipient_client_application_id OR d."RecipientClientApplicationId"<>recipient_client_application_id OR session_owner<>client_application_id OR session_subject<>subject_ref OR p."DecisionExpiresAtUtc"<>permit_expires_at OR permit_expires_at<>job_expires_at OR p."SchemaVersion"<>1 OR mode<>export_mode OR closure<>'CatalogApproved' OR actual_classes IS DISTINCT FROM raw_classes OR octet_length(fingerprint_hash)<>32 THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE'; END IF;
              IF session_state<>'Completed' THEN
                SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
                RETURN QUERY SELECT 'AuthorityNotEffective'::text,NULL::uuid; RETURN;
              END IF;
              now_:=clock_timestamp(); IF now_>=permit_expires_at THEN
                SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
                RETURN QUERY SELECT 'AuthorityNotEffective'::text,NULL::uuid; RETURN;
              END IF;
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_identity',true);
              INSERT INTO tagekyc.raw_export_job_identities VALUES(prospective_job_id,permit_id,authorization_decision_id,principal_id,client_application_id,created_by_api_key_id,verification_session_id,subject_ref,policy_id,policy_version,purpose_code,recipient_client_application_id,export_mode,permit_expires_at,job_expires_at,idempotency_key,fingerprint_hash,1,now_);
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_class',true);
              INSERT INTO tagekyc.raw_export_job_classes("JobId","RawClass","Ordinal") SELECT prospective_job_id,x,(ord-1)::integer FROM unnest(raw_classes) WITH ORDINALITY q(x,ord);
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_insert',true);
              INSERT INTO tagekyc.raw_export_job_operational_heads VALUES(prospective_job_id,'Claimed',0,NULL,NULL,NULL,0,now_);
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true);
              INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),prospective_job_id,0,'JobBound',NULL,'Claimed',NULL,0,NULL,NULL,NULL,now_);
              SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes IMMEDIATE;
              SET CONSTRAINTS tagekyc.tr_b4_job_identity_has_classes DEFERRED;
              RETURN QUERY SELECT 'NewJob'::text,prospective_job_id;
            END $$;

            CREATE FUNCTION tagekyc.raw_export_read_job(principal_id uuid,client_application_id uuid,job_id uuid)
            RETURNS TABLE("JobId" uuid,"PermitId" uuid,"AuthorizationDecisionId" uuid,"PrincipalId" uuid,"ClientApplicationId" uuid,"CreatedByApiKeyId" uuid,"VerificationSessionId" uuid,"SubjectRef" text,"PolicyId" uuid,"PolicyVersion" integer,"PurposeCode" text,"RecipientClientApplicationId" uuid,"ExportMode" text,"PermitExpiresAt" timestamptz,"JobExpiresAt" timestamptz,"SchemaVersion" integer,"CreatedAt" timestamptz,"ClassOrdinal" integer,"RawClass" text,"CurrentState" text,"Revision" bigint,"CurrentAttemptId" uuid,"LeaseOwnerId" uuid,"LeaseExpiresAt" timestamptz,"FencingToken" bigint,"LatestEventType" text,"LatestFailureCode" text,"LatestOccurredAt" timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid;
            BEGIN
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              RETURN QUERY SELECT i."JobId",i."PermitId",i."AuthorizationDecisionId",i."PrincipalId",i."ClientApplicationId",i."CreatedByApiKeyId",i."VerificationSessionId",i."SubjectRef",i."PolicyId",i."PolicyVersion",i."PurposeCode",i."RecipientClientApplicationId",i."ExportMode",i."PermitExpiresAt",i."JobExpiresAt",i."SchemaVersion",i."CreatedAt",c."Ordinal",c."RawClass",h."CurrentState",h."Revision",h."CurrentAttemptId",h."LeaseOwnerId",h."LeaseExpiresAt",h."FencingToken",t."EventType",t."FailureCode",t."OccurredAt"
              FROM tagekyc.raw_export_job_identities i JOIN tagekyc.raw_export_job_classes c ON c."JobId"=i."JobId" JOIN tagekyc.raw_export_job_operational_heads h ON h."JobId"=i."JobId" JOIN LATERAL(SELECT x.* FROM tagekyc.raw_export_job_transitions x WHERE x."JobId"=i."JobId" ORDER BY x."ResultingRevision" DESC LIMIT 1)t ON true
              WHERE i."JobId"=job_id AND i."PrincipalId"=principal_id AND i."ClientApplicationId"=client_application_id ORDER BY c."Ordinal";
            END $$;

            CREATE FUNCTION tagekyc.raw_export_lock_job_for_attempt(job_id uuid,principal_id uuid,client_application_id uuid,expected_revision bigint,expected_fencing_token bigint)
            RETURNS TABLE(outcome text,current_state text,current_revision bigint,current_fencing_token bigint)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; h tagekyc.raw_export_job_operational_heads%ROWTYPE; deadline timestamptz; now_ timestamptz;
            BEGIN
              IF current_setting('transaction_isolation') <> 'read committed' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID'; END IF;
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              SELECT x.* INTO h FROM tagekyc.raw_export_job_operational_heads x JOIN tagekyc.raw_export_job_identities i ON i."JobId"=x."JobId" WHERE x."JobId"=job_id AND i."PrincipalId"=principal_id AND i."ClientApplicationId"=client_application_id FOR UPDATE OF x;
              IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::text,NULL::bigint,NULL::bigint; RETURN; END IF;
              IF expected_revision IS NULL OR h."Revision"<>expected_revision THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::text,NULL::bigint,NULL::bigint; RETURN; END IF;
              IF expected_fencing_token IS NULL OR h."FencingToken"<>expected_fencing_token THEN RETURN QUERY SELECT 'FenceStale'::text,NULL::text,NULL::bigint,NULL::bigint; RETURN; END IF;
              IF h."CurrentState" IN ('TerminalFailed','Cancelled','Expired') THEN RETURN QUERY SELECT 'AlreadyTerminal'::text,h."CurrentState",h."Revision",h."FencingToken"; RETURN; END IF;
              SELECT i."JobExpiresAt" INTO deadline FROM tagekyc.raw_export_job_identities i WHERE i."JobId"=job_id;
              now_:=clock_timestamp();
              IF h."LeaseOwnerId" IS NOT NULL AND h."LeaseExpiresAt">now_ AND deadline>now_ THEN RETURN QUERY SELECT 'LeaseHeld'::text,h."CurrentState",h."Revision",h."FencingToken"; RETURN; END IF;
              RETURN QUERY SELECT 'Locked'::text,h."CurrentState",h."Revision",h."FencingToken";
            END $$;

            CREATE FUNCTION tagekyc.raw_export_acquire_or_reclaim_job_lease(job_id uuid,principal_id uuid,client_application_id uuid,expected_revision bigint,expected_fencing_token bigint,attempt_id uuid,lease_owner_id uuid,lease_seconds integer)
            RETURNS TABLE(outcome text,resulting_revision bigint,resulting_fencing_token bigint,resulting_lease_expires_at timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; h tagekyc.raw_export_job_operational_heads%ROWTYPE; i tagekyc.raw_export_job_identities%ROWTYPE; now_ timestamptz; expiry timestamptz; nextrev bigint; nextfence bigint; ordinal integer; event text; out_ text;
            BEGIN
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              IF lease_seconds IS NULL OR lease_seconds NOT BETWEEN 10 AND 300 THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_LEASE_CONFIG_INVALID'; END IF;
              SELECT x.* INTO h FROM tagekyc.raw_export_job_operational_heads x JOIN tagekyc.raw_export_job_identities j ON j."JobId"=x."JobId" WHERE x."JobId"=job_id AND j."PrincipalId"=principal_id AND j."ClientApplicationId"=client_application_id FOR UPDATE OF x;
              IF NOT FOUND OR expected_revision IS NULL OR h."Revision"<>expected_revision THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              SELECT * INTO i FROM tagekyc.raw_export_job_identities WHERE "JobId"=job_id;
              IF expected_fencing_token IS NULL OR h."FencingToken"<>expected_fencing_token THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              now_:=clock_timestamp();
              IF h."CurrentState" IN ('TerminalFailed','Cancelled','Expired') THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              IF now_>=i."JobExpiresAt" THEN
                nextrev:=h."Revision"+1; PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_terminal',true); UPDATE tagekyc.raw_export_job_operational_heads SET "CurrentState"='Expired',"Revision"=nextrev,"LeaseOwnerId"=NULL,"LeaseExpiresAt"=NULL,"UpdatedAt"=now_ WHERE "JobId"=job_id;
                PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,'JobExpired',h."CurrentState",'Expired',h."CurrentAttemptId",h."FencingToken",NULL,NULL,'PERMIT_OR_JOB_EXPIRED',now_);
                RETURN QUERY SELECT 'Expired'::text,nextrev,h."FencingToken",NULL::timestamptz; RETURN;
              END IF;
              IF h."LeaseOwnerId" IS NOT NULL AND h."LeaseExpiresAt">now_ THEN RETURN QUERY SELECT 'LeaseHeld'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              IF h."CurrentState"='Assembling' AND i."ExportMode"<>'EncryptedExportPacket' THEN
                nextrev:=h."Revision"+1; PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_terminal',true); UPDATE tagekyc.raw_export_job_operational_heads SET "CurrentState"='TerminalFailed',"Revision"=nextrev,"LeaseOwnerId"=NULL,"LeaseExpiresAt"=NULL,"UpdatedAt"=now_ WHERE "JobId"=job_id;
                PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,'JobTerminalFailed',h."CurrentState",'TerminalFailed',h."CurrentAttemptId",h."FencingToken",NULL,NULL,'MODE_RETRY_NOT_AUTHORIZED',now_);
                RETURN QUERY SELECT 'TerminalizedModeRetryForbidden'::text,nextrev,h."FencingToken",NULL::timestamptz; RETURN;
              END IF;
              SELECT COALESCE(max("AttemptOrdinal"),-1)+1 INTO ordinal FROM tagekyc.raw_export_job_attempts WHERE "JobId"=job_id;
              nextfence:=h."FencingToken"+1; nextrev:=h."Revision"+1; expiry:=LEAST(now_+make_interval(secs=>lease_seconds),i."JobExpiresAt");
              IF h."CurrentState"='Claimed' THEN event:='LeaseAcquired';out_:='Acquired'; ELSIF h."LeaseExpiresAt" IS NOT NULL THEN event:='LeaseReclaimed';out_:='Reclaimed'; ELSE event:='LeaseAcquiredAfterRetryableFailure';out_:='AcquiredAfterRetryableFailure'; END IF;
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_attempt',true); INSERT INTO tagekyc.raw_export_job_attempts VALUES(attempt_id,job_id,ordinal,'Assembling',lease_owner_id,nextfence,now_,expiry);
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_acquire',true); UPDATE tagekyc.raw_export_job_operational_heads SET "CurrentState"='Assembling',"Revision"=nextrev,"CurrentAttemptId"=attempt_id,"LeaseOwnerId"=lease_owner_id,"LeaseExpiresAt"=expiry,"FencingToken"=nextfence,"UpdatedAt"=now_ WHERE "JobId"=job_id;
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,event,h."CurrentState",'Assembling',attempt_id,nextfence,lease_owner_id,expiry,NULL,now_);
              RETURN QUERY SELECT out_,nextrev,nextfence,expiry;
            END $$;

            CREATE FUNCTION tagekyc.raw_export_renew_job_lease(job_id uuid,principal_id uuid,client_application_id uuid,expected_revision bigint,expected_fencing_token bigint,attempt_id uuid,lease_owner_id uuid,lease_seconds integer)
            RETURNS TABLE(outcome text,resulting_revision bigint,resulting_fencing_token bigint,resulting_lease_expires_at timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; h tagekyc.raw_export_job_operational_heads%ROWTYPE; deadline timestamptz; now_ timestamptz; expiry timestamptz; nextrev bigint;
            BEGIN
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              IF lease_seconds IS NULL OR lease_seconds NOT BETWEEN 10 AND 300 THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_LEASE_CONFIG_INVALID'; END IF;
              SELECT x.* INTO h FROM tagekyc.raw_export_job_operational_heads x JOIN tagekyc.raw_export_job_identities i ON i."JobId"=x."JobId" WHERE x."JobId"=job_id AND i."PrincipalId"=principal_id AND i."ClientApplicationId"=client_application_id FOR UPDATE OF x;
              IF NOT FOUND OR expected_revision IS NULL OR h."Revision"<>expected_revision THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              SELECT "JobExpiresAt" INTO deadline FROM tagekyc.raw_export_job_identities WHERE "JobId"=job_id;
              IF expected_fencing_token IS NULL OR h."FencingToken"<>expected_fencing_token THEN RETURN QUERY SELECT 'FenceStale'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              now_:=clock_timestamp(); IF h."CurrentAttemptId" IS DISTINCT FROM attempt_id OR h."LeaseOwnerId" IS DISTINCT FROM lease_owner_id OR h."LeaseExpiresAt" IS NULL OR h."LeaseExpiresAt"<=now_ THEN RETURN QUERY SELECT 'LeaseNotHeld'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              nextrev:=h."Revision"+1; expiry:=LEAST(now_+make_interval(secs=>lease_seconds),deadline);
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_renew',true); UPDATE tagekyc.raw_export_job_operational_heads SET "Revision"=nextrev,"LeaseExpiresAt"=expiry,"UpdatedAt"=now_ WHERE "JobId"=job_id;
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,'LeaseRenewed','Assembling','Assembling',attempt_id,h."FencingToken",lease_owner_id,expiry,NULL,now_);
              RETURN QUERY SELECT 'Renewed'::text,nextrev,h."FencingToken",expiry;
            END $$;

            CREATE FUNCTION tagekyc.raw_export_record_job_attempt_failure(job_id uuid,principal_id uuid,client_application_id uuid,expected_revision bigint,expected_fencing_token bigint,attempt_id uuid,lease_owner_id uuid,failure_code text)
            RETURNS TABLE(outcome text,resulting_revision bigint,resulting_fencing_token bigint,resulting_lease_expires_at timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; h tagekyc.raw_export_job_operational_heads%ROWTYPE; mode text; now_ timestamptz; nextrev bigint;
            BEGIN
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              SELECT x.* INTO h FROM tagekyc.raw_export_job_operational_heads x JOIN tagekyc.raw_export_job_identities i ON i."JobId"=x."JobId" WHERE x."JobId"=job_id AND i."PrincipalId"=principal_id AND i."ClientApplicationId"=client_application_id FOR UPDATE OF x;
              IF NOT FOUND OR expected_revision IS NULL OR h."Revision"<>expected_revision THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              SELECT "ExportMode" INTO mode FROM tagekyc.raw_export_job_identities WHERE "JobId"=job_id;
              IF expected_fencing_token IS NULL OR h."FencingToken"<>expected_fencing_token THEN RETURN QUERY SELECT 'FenceStale'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              now_:=clock_timestamp(); IF h."CurrentAttemptId" IS DISTINCT FROM attempt_id OR h."LeaseOwnerId" IS DISTINCT FROM lease_owner_id OR h."LeaseExpiresAt" IS NULL OR h."LeaseExpiresAt"<=now_ THEN RETURN QUERY SELECT 'LeaseNotHeld'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              IF failure_code IS NULL OR failure_code<>'ATTEMPT_EXECUTION_FAILED_RETRYABLE' THEN RETURN QUERY SELECT 'TransitionInvalid'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              nextrev:=h."Revision"+1;
              IF mode<>'EncryptedExportPacket' THEN
                PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_terminal',true); UPDATE tagekyc.raw_export_job_operational_heads SET "CurrentState"='TerminalFailed',"Revision"=nextrev,"LeaseOwnerId"=NULL,"LeaseExpiresAt"=NULL,"UpdatedAt"=now_ WHERE "JobId"=job_id;
                PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,'JobTerminalFailed','Assembling','TerminalFailed',attempt_id,h."FencingToken",NULL,NULL,'MODE_RETRY_NOT_AUTHORIZED',now_);
                RETURN QUERY SELECT 'TerminalizedModeRetryForbidden'::text,nextrev,h."FencingToken",NULL::timestamptz;
              ELSE
                PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_release',true); UPDATE tagekyc.raw_export_job_operational_heads SET "Revision"=nextrev,"LeaseOwnerId"=NULL,"LeaseExpiresAt"=NULL,"UpdatedAt"=now_ WHERE "JobId"=job_id;
                PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,'AttemptFailedRetryable','Assembling','Assembling',attempt_id,h."FencingToken",NULL,NULL,failure_code,now_);
                RETURN QUERY SELECT 'Recorded'::text,nextrev,h."FencingToken",NULL::timestamptz;
              END IF;
            END $$;

            CREATE FUNCTION tagekyc.raw_export_terminalize_job(job_id uuid,principal_id uuid,client_application_id uuid,expected_revision bigint,expected_fencing_token bigint,attempt_id uuid,lease_owner_id uuid,terminal_state text,reason_code text)
            RETURNS TABLE(outcome text,resulting_revision bigint,resulting_fencing_token bigint,resulting_lease_expires_at timestamptz)
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
            DECLARE a uuid; h tagekyc.raw_export_job_operational_heads%ROWTYPE; deadline timestamptz; now_ timestamptz; nextrev bigint; state_ text; reason_ text; event_ text;
            BEGIN
              a:=tagekyc.raw_export_current_actor(); IF a IS DISTINCT FROM principal_id THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_ACTOR_MISMATCH'; END IF;
              SELECT x.* INTO h FROM tagekyc.raw_export_job_operational_heads x JOIN tagekyc.raw_export_job_identities i ON i."JobId"=x."JobId" WHERE x."JobId"=job_id AND i."PrincipalId"=principal_id AND i."ClientApplicationId"=client_application_id FOR UPDATE OF x;
              IF NOT FOUND OR expected_revision IS NULL OR h."Revision"<>expected_revision THEN RETURN QUERY SELECT 'ConcurrencyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              SELECT "JobExpiresAt" INTO deadline FROM tagekyc.raw_export_job_identities WHERE "JobId"=job_id;
              IF expected_fencing_token IS NULL OR h."FencingToken"<>expected_fencing_token THEN RETURN QUERY SELECT 'FenceStale'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              IF h."CurrentState" IN ('TerminalFailed','Cancelled','Expired') THEN RETURN QUERY SELECT 'AlreadyTerminal'::text,h."Revision",h."FencingToken",NULL::timestamptz; RETURN; END IF;
              IF h."CurrentAttemptId" IS DISTINCT FROM attempt_id OR h."LeaseOwnerId" IS DISTINCT FROM lease_owner_id THEN RETURN QUERY SELECT 'LeaseNotHeld'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              IF terminal_state IS NULL OR reason_code IS NULL OR terminal_state NOT IN ('TerminalFailed','Cancelled','Expired') THEN RETURN QUERY SELECT 'TransitionInvalid'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              now_:=clock_timestamp(); state_:=terminal_state; reason_:=reason_code;
              IF terminal_state='Expired' AND now_<deadline THEN RETURN QUERY SELECT 'TransitionInvalid'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              IF now_>=deadline THEN state_:='Expired';reason_:='PERMIT_OR_JOB_EXPIRED'; END IF;
              IF (state_='Cancelled' AND reason_<>'REQUEST_CANCELLED') OR (state_='Expired' AND reason_<>'PERMIT_OR_JOB_EXPIRED') OR (state_='TerminalFailed' AND reason_ NOT IN ('AUTHORITY_REVALIDATION_FAILED','ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE','JOB_GRAPH_INVARIANT_FAILURE','MODE_RETRY_NOT_AUTHORIZED')) THEN RETURN QUERY SELECT 'TransitionInvalid'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
              nextrev:=h."Revision"+1; event_:=CASE state_ WHEN 'Expired' THEN 'JobExpired' WHEN 'Cancelled' THEN 'JobCancelled' ELSE 'JobTerminalFailed' END;
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_head_terminal',true); UPDATE tagekyc.raw_export_job_operational_heads SET "CurrentState"=state_,"Revision"=nextrev,"LeaseOwnerId"=NULL,"LeaseExpiresAt"=NULL,"UpdatedAt"=now_ WHERE "JobId"=job_id;
              PERFORM set_config('tagekyc.raw_export_job_mutation_context','job_transition',true); INSERT INTO tagekyc.raw_export_job_transitions VALUES(gen_random_uuid(),job_id,nextrev,event_,h."CurrentState",state_,h."CurrentAttemptId",h."FencingToken",NULL,NULL,reason_,now_);
              RETURN QUERY SELECT CASE WHEN state_='Expired' THEN 'Expired' ELSE 'Terminalized' END,nextrev,h."FencingToken",NULL::timestamptz;
            END $$;

            ALTER TABLE tagekyc.raw_export_job_identities OWNER TO tagekyc_raw_export_deployer;
            ALTER TABLE tagekyc.raw_export_job_classes OWNER TO tagekyc_raw_export_deployer;
            ALTER TABLE tagekyc.raw_export_job_attempts OWNER TO tagekyc_raw_export_deployer;
            ALTER TABLE tagekyc.raw_export_job_transitions OWNER TO tagekyc_raw_export_deployer;
            ALTER TABLE tagekyc.raw_export_job_operational_heads OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.enforce_raw_export_job_identity_insert() OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.enforce_raw_export_job_class_insert() OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.enforce_raw_export_job_attempt_insert() OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.enforce_raw_export_job_transition_insert() OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.enforce_raw_export_job_head_mutation() OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.enforce_raw_export_job_identity_has_classes() OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_read_job_binding_inputs(uuid,uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[]) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_read_job(uuid,uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text) OWNER TO tagekyc_raw_export_deployer;
            ALTER FUNCTION tagekyc.raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON TABLE tagekyc.raw_export_job_identities,tagekyc.raw_export_job_classes,tagekyc.raw_export_job_attempts,tagekyc.raw_export_job_transitions,tagekyc.raw_export_job_operational_heads FROM PUBLIC,tagekyc_runtime;
            REVOKE ALL ON FUNCTION tagekyc.enforce_raw_export_job_identity_insert(),tagekyc.enforce_raw_export_job_class_insert(),tagekyc.enforce_raw_export_job_attempt_insert(),tagekyc.enforce_raw_export_job_transition_insert(),tagekyc.enforce_raw_export_job_head_mutation(),tagekyc.enforce_raw_export_job_identity_has_classes(),tagekyc.raw_export_read_job_binding_inputs(uuid,uuid,uuid),tagekyc.raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[]),tagekyc.raw_export_read_job(uuid,uuid,uuid),tagekyc.raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint),tagekyc.raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer),tagekyc.raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer),tagekyc.raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text),tagekyc.raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text) FROM PUBLIC,tagekyc_runtime;
            SET LOCAL ROLE tagekyc_raw_export_deployer;
            GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_job_binding_inputs(uuid,uuid,uuid),tagekyc.raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[]),tagekyc.raw_export_read_job(uuid,uuid,uuid),tagekyc.raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint),tagekyc.raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer),tagekyc.raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer),tagekyc.raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text),tagekyc.raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text) TO tagekyc_runtime;
            RESET ROLE;
            DO $$
            DECLARE function_count integer; expected_function_count integer; expected_grants integer; bad_function_acl integer; bad_function_owner integer;
                    table_count integer; bad_table_acl integer; bad_column_acl integer;
            BEGIN
              SELECT count(*) INTO function_count
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc'
                AND (p.proname LIKE 'raw_export_%job%' OR p.proname LIKE 'enforce_raw_export_job_%');
              SELECT count(*) INTO expected_function_count
              FROM unnest(ARRAY[
                'tagekyc.enforce_raw_export_job_identity_insert()'::regprocedure,
                'tagekyc.enforce_raw_export_job_class_insert()'::regprocedure,
                'tagekyc.enforce_raw_export_job_attempt_insert()'::regprocedure,
                'tagekyc.enforce_raw_export_job_transition_insert()'::regprocedure,
                'tagekyc.enforce_raw_export_job_head_mutation()'::regprocedure,
                'tagekyc.enforce_raw_export_job_identity_has_classes()'::regprocedure,
                'tagekyc.raw_export_read_job_binding_inputs(uuid,uuid,uuid)'::regprocedure,
                'tagekyc.raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[])'::regprocedure,
                'tagekyc.raw_export_read_job(uuid,uuid,uuid)'::regprocedure,
                'tagekyc.raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint)'::regprocedure,
                'tagekyc.raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer)'::regprocedure,
                'tagekyc.raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer)'::regprocedure,
                'tagekyc.raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text)'::regprocedure,
                'tagekyc.raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text)'::regprocedure
              ]);
              IF function_count<>14 OR expected_function_count<>14 THEN RAISE EXCEPTION 'TIP88B4_FUNCTION_ACL_INVALID'; END IF;

              SELECT count(*) INTO expected_grants
              FROM pg_proc p
              JOIN pg_namespace n ON n.oid=p.pronamespace
              CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) x
              WHERE n.nspname='tagekyc'
                AND p.proname IN ('raw_export_read_job_binding_inputs','raw_export_claim_or_read_job','raw_export_read_job','raw_export_lock_job_for_attempt','raw_export_acquire_or_reclaim_job_lease','raw_export_renew_job_lease','raw_export_record_job_attempt_failure','raw_export_terminalize_job')
                AND x.grantee=(SELECT oid FROM pg_roles WHERE rolname='tagekyc_runtime')
                AND x.privilege_type='EXECUTE' AND NOT x.is_grantable
                AND x.grantor=(SELECT oid FROM pg_roles WHERE rolname='tagekyc_raw_export_deployer');
              SELECT count(*) INTO bad_function_acl
              FROM pg_proc p
              JOIN pg_namespace n ON n.oid=p.pronamespace
              JOIN pg_roles owner_role ON owner_role.oid=p.proowner
              CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) x
              WHERE n.nspname='tagekyc'
                AND (p.proname LIKE 'raw_export_%job%' OR p.proname LIKE 'enforce_raw_export_job_%')
                AND (owner_role.rolname<>'tagekyc_raw_export_deployer'
                     OR (x.grantee<>p.proowner AND NOT (
                         p.proname IN ('raw_export_read_job_binding_inputs','raw_export_claim_or_read_job','raw_export_read_job','raw_export_lock_job_for_attempt','raw_export_acquire_or_reclaim_job_lease','raw_export_renew_job_lease','raw_export_record_job_attempt_failure','raw_export_terminalize_job')
                         AND x.grantee=(SELECT oid FROM pg_roles WHERE rolname='tagekyc_runtime')
                         AND x.grantor=(SELECT oid FROM pg_roles WHERE rolname='tagekyc_raw_export_deployer')
                         AND x.privilege_type='EXECUTE' AND NOT x.is_grantable)));
              SELECT count(*) INTO bad_function_owner
              FROM pg_proc p
              JOIN pg_namespace n ON n.oid=p.pronamespace
              JOIN pg_roles owner_role ON owner_role.oid=p.proowner
              WHERE n.nspname='tagekyc'
                AND (p.proname LIKE 'raw_export_%job%' OR p.proname LIKE 'enforce_raw_export_job_%')
                AND (owner_role.rolname<>'tagekyc_raw_export_deployer'
                     OR owner_role.rolcanlogin OR NOT owner_role.rolinherit
                     OR owner_role.rolsuper OR owner_role.rolcreatedb
                     OR owner_role.rolcreaterole OR owner_role.rolreplication
                     OR owner_role.rolbypassrls);
              IF expected_grants<>8 OR bad_function_acl<>0 OR bad_function_owner<>0 THEN
                RAISE EXCEPTION 'TIP88B4_FUNCTION_ACL_INVALID';
              END IF;

              SELECT count(*),count(*) FILTER (WHERE owner_role.rolname<>'tagekyc_raw_export_deployer')
              INTO table_count,bad_table_acl
              FROM pg_class c
              JOIN pg_namespace n ON n.oid=c.relnamespace
              JOIN pg_roles owner_role ON owner_role.oid=c.relowner
              WHERE n.nspname='tagekyc'
                AND c.relname IN ('raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts','raw_export_job_transitions','raw_export_job_operational_heads')
                AND c.relkind='r';
              SELECT bad_table_acl+count(*) INTO bad_table_acl
              FROM pg_class c
              JOIN pg_namespace n ON n.oid=c.relnamespace
              CROSS JOIN LATERAL aclexplode(COALESCE(c.relacl,acldefault('r',c.relowner))) x
              WHERE n.nspname='tagekyc'
                AND c.relname IN ('raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts','raw_export_job_transitions','raw_export_job_operational_heads')
                AND x.grantee<>c.relowner;
              SELECT count(*) INTO bad_column_acl
              FROM pg_class c
              JOIN pg_namespace n ON n.oid=c.relnamespace
              JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
              CROSS JOIN LATERAL aclexplode(a.attacl) x
              WHERE n.nspname='tagekyc'
                AND c.relname IN ('raw_export_job_identities','raw_export_job_classes','raw_export_job_attempts','raw_export_job_transitions','raw_export_job_operational_heads')
                AND a.attacl IS NOT NULL
                AND x.grantee<>c.relowner;
              IF table_count<>5 OR bad_table_acl<>0 OR bad_column_acl<>0 THEN
                RAISE EXCEPTION 'TIP88B4_TABLE_ACL_INVALID';
              END IF;
            END $$;
            """;

        private const string B4DownSql =
            """
            DROP FUNCTION IF EXISTS tagekyc.raw_export_terminalize_job(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text,text);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_record_job_attempt_failure(uuid,uuid,uuid,bigint,bigint,uuid,uuid,text);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_renew_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_acquire_or_reclaim_job_lease(uuid,uuid,uuid,bigint,bigint,uuid,uuid,integer);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_lock_job_for_attempt(uuid,uuid,uuid,bigint,bigint);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_read_job(uuid,uuid,uuid);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_claim_or_read_job(uuid,uuid,uuid,uuid,uuid,uuid,uuid,text,uuid,integer,text,uuid,text,timestamptz,timestamptz,text,bytea,text[]);
            DROP FUNCTION IF EXISTS tagekyc.raw_export_read_job_binding_inputs(uuid,uuid,uuid);
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_job_identity_insert() CASCADE;
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_job_class_insert() CASCADE;
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_job_attempt_insert() CASCADE;
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_job_transition_insert() CASCADE;
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_job_head_mutation() CASCADE;
            DROP FUNCTION IF EXISTS tagekyc.enforce_raw_export_job_identity_has_classes() CASCADE;
            """;
    }
}
