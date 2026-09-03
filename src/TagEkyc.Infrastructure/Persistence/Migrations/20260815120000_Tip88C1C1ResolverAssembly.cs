using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1C1ResolverAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_assembly_preparation_dispositions",
                schema: "tagekyc",
                columns: table => new
                {
                    C2PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    AssemblyFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    PreparationFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    Disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderReceiptDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    AbortAuthorizationDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    RowRevision = table.Column<long>(type: "bigint", nullable: false),
                    PreparingAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PendingAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SealCommittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AbortAuthorizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AbortedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_assembly_preparation", x => x.C2PreparationId);
                    table.UniqueConstraint("uq_raw_export_assembly_preparation_assembly", x => x.AssemblyId);
                    table.UniqueConstraint("uq_raw_export_assembly_preparation_job", x => x.JobId);
                    table.CheckConstraint("ck_raw_export_assembly_preparation_shape", "\"FencingToken\" >= 1 AND octet_length(\"AssemblyFingerprint\") = 32 AND octet_length(\"PreparationFingerprint\") = 32 AND \"Disposition\" IN ('Preparing','Pending','SealCommitted','Finalized','AbortAuthorized','Aborted') AND (\"ProviderReceiptDigest\" IS NULL OR octet_length(\"ProviderReceiptDigest\") = 32) AND (\"AbortAuthorizationDigest\" IS NULL OR octet_length(\"AbortAuthorizationDigest\") = 32) AND \"RowRevision\" >= 1 AND \"SchemaVersion\" = 1");
                    table.CheckConstraint("ck_raw_export_assembly_preparation_sparse", "(\"Disposition\" = 'Preparing' AND \"PendingAtUtc\" IS NULL AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'Pending' AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PendingAtUtc\" IS NOT NULL AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'SealCommitted' AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PendingAtUtc\" IS NOT NULL AND \"SealCommittedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'Finalized' AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PendingAtUtc\" IS NOT NULL AND \"SealCommittedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NOT NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'AbortAuthorized' AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NOT NULL AND \"AbortAuthorizedAtUtc\" IS NOT NULL AND \"AbortedAtUtc\" IS NULL) OR (\"Disposition\" = 'Aborted' AND \"SealCommittedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NOT NULL AND \"AbortAuthorizedAtUtc\" IS NOT NULL AND \"AbortedAtUtc\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_preparation_attempt",
                        column: x => x.AttemptId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_attempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_preparation_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_job_source_bindings",
                schema: "tagekyc",
                columns: table => new
                {
                    JobSourceBindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionCaptureSelectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureAcceptanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureRevision = table.Column<int>(type: "integer", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePublicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePublicationRevision = table.Column<long>(type: "bigint", nullable: false),
                    EncryptionAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptionAttemptRevision = table.Column<long>(type: "bigint", nullable: false),
                    EncryptionAttemptFence = table.Column<long>(type: "bigint", nullable: false),
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectCustodyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectStateRevision = table.Column<long>(type: "bigint", nullable: false),
                    SubjectRefTokenSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefTokenKeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SubjectRefTokenKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentCommitmentSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ContentCommitmentKeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentCommitmentKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    ContentCommitment = table.Column<byte[]>(type: "bytea", nullable: false),
                    PlaintextLength = table.Column<long>(type: "bigint", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AuthoritySnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    AuthoritySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorityRevision = table.Column<long>(type: "bigint", nullable: false),
                    ConsentPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentPolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    AbsoluteSourceExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectivePlaintextRetentionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StableDataScopeId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ControllerIdentity = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BindingFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_job_source_bindings", x => x.JobSourceBindingId);
                    table.UniqueConstraint("uq_raw_export_job_source_binding_class", x => new { x.JobId, x.RawClass });
                    table.UniqueConstraint("uq_raw_export_job_source_binding_ordinal", x => new { x.JobId, x.Ordinal });
                    table.CheckConstraint("ck_raw_export_job_source_binding_shape", "\"Ordinal\" >= 0 AND \"CaptureRevision\" >= 1 AND \"SourcePublicationRevision\" >= 1 AND \"EncryptionAttemptRevision\" >= 1 AND \"EncryptionAttemptFence\" >= 1 AND \"ObjectStateRevision\" >= 1 AND \"SubjectRefTokenSchemaVersion\" >= 1 AND \"SubjectRefTokenKeyVersion\" >= 1 AND octet_length(\"SubjectRefToken\") = 32 AND \"ContentCommitmentSchemaVersion\" >= 1 AND \"ContentCommitmentKeyVersion\" >= 1 AND octet_length(\"ContentCommitment\") = 32 AND \"PlaintextLength\" >= 1 AND \"AuthoritySnapshotSchemaVersion\" >= 1 AND \"AuthorityRevision\" >= 1 AND \"ConsentPolicyVersion\" >= 1 AND octet_length(\"BindingFingerprint\") = 32 AND \"SchemaVersion\" = 1");
                    table.ForeignKey(
                        name: "fk_raw_export_job_source_binding_attempt",
                        column: x => x.EncryptionAttemptId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_encryption_attempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_job_source_binding_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_job_source_binding_key",
                        column: x => x.AttemptKeyReservationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_attempt_key_reservations",
                        principalColumn: "AttemptKeyReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_job_source_binding_object",
                        column: x => x.ObjectCustodyId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_provisional_objects",
                        principalColumn: "ObjectCustodyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_job_source_binding_publication",
                        column: x => x.SourcePublicationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_publications",
                        principalColumn: "SourcePublicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_job_source_binding_selection",
                        column: x => x.SessionCaptureSelectionId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_session_capture_selections",
                        principalColumn: "SessionCaptureSelectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_assembly_identities",
                schema: "tagekyc",
                columns: table => new
                {
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    PermitId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    PurposeCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExportMode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ManifestVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefTokenSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefTokenKeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SubjectRefTokenKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    SubjectRefToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    AssemblyAuthenticationKeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AssemblyAuthenticationKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    AssemblyDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    ManifestDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    AssemblyAuthenticationValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    AssemblyFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    CompleteAssemblyLength = table.Column<long>(type: "bigint", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    C2PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    JobExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SealedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_assembly_identities", x => x.AssemblyId);
                    table.UniqueConstraint("uq_raw_export_assembly_identity_job", x => x.JobId);
                    table.UniqueConstraint("uq_raw_export_assembly_identity_preparation", x => x.C2PreparationId);
                    table.CheckConstraint("ck_raw_export_assembly_identity_shape", "\"FencingToken\" >= 1 AND \"PolicyVersion\" >= 1 AND \"ManifestVersion\" = 1 AND \"SubjectRefTokenSchemaVersion\" >= 1 AND \"SubjectRefTokenKeyVersion\" >= 1 AND octet_length(\"SubjectRefToken\") = 32 AND \"AssemblyAuthenticationKeyVersion\" >= 1 AND octet_length(\"AssemblyDigest\") = 32 AND octet_length(\"ManifestDigest\") = 32 AND octet_length(\"AssemblyAuthenticationValue\") = 32 AND octet_length(\"AssemblyFingerprint\") = 32 AND \"CompleteAssemblyLength\" >= 1 AND \"ItemCount\" >= 1 AND \"SchemaVersion\" = 1");
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_identity_attempt",
                        column: x => x.AttemptId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_attempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_identity_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_identity_preparation",
                        column: x => x.C2PreparationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_assembly_preparation_dispositions",
                        principalColumn: "C2PreparationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_assembly_items",
                schema: "tagekyc",
                columns: table => new
                {
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    RawClass = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JobSourceBindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureRevision = table.Column<int>(type: "integer", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlaintextLength = table.Column<long>(type: "bigint", nullable: false),
                    ContentCommitmentSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ContentCommitmentKeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentCommitmentKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    ContentCommitment = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_assembly_items", x => new { x.AssemblyId, x.Ordinal });
                    table.UniqueConstraint("uq_raw_export_assembly_item_class", x => new { x.AssemblyId, x.RawClass });
                    table.CheckConstraint("ck_raw_export_assembly_item_shape", "\"Ordinal\" >= 0 AND \"CaptureRevision\" >= 1 AND \"PlaintextLength\" >= 1 AND \"ContentCommitmentSchemaVersion\" >= 1 AND \"ContentCommitmentKeyVersion\" >= 1 AND octet_length(\"ContentCommitment\") = 32");
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_item_binding",
                        column: x => x.JobSourceBindingId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_source_bindings",
                        principalColumn: "JobSourceBindingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_assembly_item_identity",
                        column: x => x.AssemblyId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_assembly_identities",
                        principalColumn: "AssemblyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_assembly_identities_AttemptId",
                schema: "tagekyc",
                table: "raw_export_assembly_identities",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_assembly_items_JobSourceBindingId",
                schema: "tagekyc",
                table: "raw_export_assembly_items",
                column: "JobSourceBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_assembly_preparation_dispositions_AttemptId",
                schema: "tagekyc",
                table: "raw_export_assembly_preparation_dispositions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_job_source_bindings_AttemptKeyReservationId",
                schema: "tagekyc",
                table: "raw_export_job_source_bindings",
                column: "AttemptKeyReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_job_source_bindings_EncryptionAttemptId",
                schema: "tagekyc",
                table: "raw_export_job_source_bindings",
                column: "EncryptionAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_job_source_bindings_ObjectCustodyId",
                schema: "tagekyc",
                table: "raw_export_job_source_bindings",
                column: "ObjectCustodyId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_job_source_bindings_SessionCaptureSelectionId",
                schema: "tagekyc",
                table: "raw_export_job_source_bindings",
                column: "SessionCaptureSelectionId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_job_source_bindings_SourcePublicationId",
                schema: "tagekyc",
                table: "raw_export_job_source_bindings",
                column: "SourcePublicationId");

            migrationBuilder.Sql(
                """
                DO $c1_roles$
                DECLARE role_name text; login_name text; role_row record;
                BEGIN
                  FOREACH login_name IN ARRAY ARRAY[
                    'tagekyc_raw_export_assembly_resolver_login',
                    'tagekyc_raw_export_assembly_sealer_login']
                  LOOP
                    SELECT * INTO role_row FROM pg_catalog.pg_roles WHERE rolname=login_name;
                    IF NOT FOUND OR NOT role_row.rolcanlogin OR role_row.rolsuper
                       OR role_row.rolcreatedb OR role_row.rolcreaterole
                       OR role_row.rolreplication OR role_row.rolbypassrls
                       OR NOT role_row.rolinherit THEN
                      RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='PROD_RAW_EXPORT_ASSEMBLY_ROLE_TOPOLOGY_INVALID';
                    END IF;
                  END LOOP;
                  FOREACH role_name IN ARRAY ARRAY[
                    'tagekyc_raw_export_assembly_resolver',
                    'tagekyc_raw_export_assembly_sealer']
                  LOOP
                    SELECT * INTO role_row FROM pg_catalog.pg_roles WHERE rolname=role_name;
                    IF FOUND THEN
                      IF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolcreatedb
                         OR role_row.rolcreaterole OR role_row.rolreplication
                         OR role_row.rolbypassrls OR NOT role_row.rolinherit THEN
                        RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='PROD_RAW_EXPORT_ASSEMBLY_ROLE_TOPOLOGY_INVALID';
                      END IF;
                    ELSE
                      EXECUTE pg_catalog.format(
                        'CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',role_name);
                    END IF;
                  END LOOP;
                END $c1_roles$;

                GRANT tagekyc_raw_export_assembly_resolver TO tagekyc_raw_export_assembly_resolver_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                GRANT tagekyc_raw_export_assembly_sealer TO tagekyc_raw_export_assembly_sealer_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                GRANT USAGE ON SCHEMA tagekyc TO
                  tagekyc_raw_export_assembly_resolver,
                  tagekyc_raw_export_assembly_sealer;

                ALTER TABLE tagekyc.raw_export_job_source_bindings OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_assembly_preparation_dispositions OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_assembly_identities OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_assembly_items OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON TABLE
                  tagekyc.raw_export_job_source_bindings,
                  tagekyc.raw_export_assembly_preparation_dispositions,
                  tagekyc.raw_export_assembly_identities,
                  tagekyc.raw_export_assembly_items
                FROM PUBLIC,tagekyc_runtime,
                  tagekyc_raw_export_assembly_resolver,
                  tagekyc_raw_export_assembly_sealer;

                ALTER TABLE tagekyc.raw_export_job_transitions
                  DROP CONSTRAINT "CK_b4_job_transition_event_shape";
                ALTER TABLE tagekyc.raw_export_job_transitions
                  ADD CONSTRAINT "CK_b4_job_transition_event_shape" CHECK (
                    ("EventType" = 'JobBound' AND "FromState" IS NULL AND "ToState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0 AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" IS NULL) OR
                    ("EventType" = 'LeaseAcquired' AND "FromState" = 'Claimed' AND "ToState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NOT NULL AND "ResultingLeaseExpiresAt" IS NOT NULL AND "FailureCode" IS NULL) OR
                    ("EventType" IN ('LeaseRenewed','LeaseAcquiredAfterRetryableFailure','LeaseReclaimed') AND "FromState" = 'Assembling' AND "ToState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NOT NULL AND "ResultingLeaseExpiresAt" IS NOT NULL AND "FailureCode" IS NULL) OR
                    ("EventType" = 'AttemptFailedRetryable' AND "FromState" = 'Assembling' AND "ToState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" = 'ATTEMPT_EXECUTION_FAILED_RETRYABLE') OR
                    ("EventType" = 'JobTerminalFailed' AND (("FromState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0) OR ("FromState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1)) AND "ToState" = 'TerminalFailed' AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" IN ('AUTHORITY_REVALIDATION_FAILED','ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE','JOB_GRAPH_INVARIANT_FAILURE','MODE_RETRY_NOT_AUTHORIZED')) OR
                    ("EventType" = 'JobCancelled' AND (("FromState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0) OR ("FromState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1)) AND "ToState" = 'Cancelled' AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" = 'REQUEST_CANCELLED') OR
                    ("EventType" = 'JobExpired' AND (("FromState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0) OR ("FromState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1)) AND "ToState" = 'Expired' AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" = 'PERMIT_OR_JOB_EXPIRED') OR
                    ("EventType" = 'AssemblySealed' AND "FromState" = 'Assembling' AND "ToState" = 'AssemblySealed' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" IS NULL));

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_job_transition_insert()
                RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
                DECLARE h tagekyc.raw_export_job_operational_heads%ROWTYPE; previous_revision bigint; previous_state text;
                BEGIN
                  IF TG_OP <> 'INSERT' OR current_user <> 'tagekyc_raw_export_deployer'
                     OR current_setting('tagekyc.raw_export_job_mutation_context',true) IS DISTINCT FROM 'job_transition'
                  THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSITION_MUTATION_UNSUPPORTED'; END IF;
                  SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=NEW."JobId" FOR SHARE;
                  SELECT "ResultingRevision","ToState" INTO previous_revision,previous_state
                  FROM tagekyc.raw_export_job_transitions WHERE "JobId"=NEW."JobId"
                  ORDER BY "ResultingRevision" DESC LIMIT 1;
                  IF h."JobId" IS NULL OR NEW."ResultingRevision"<>h."Revision"
                     OR NEW."ToState"<>h."CurrentState" OR NEW."AttemptId" IS DISTINCT FROM h."CurrentAttemptId"
                     OR NEW."FencingToken"<>h."FencingToken" OR NEW."ResultingLeaseOwnerId" IS DISTINCT FROM h."LeaseOwnerId"
                     OR NEW."ResultingLeaseExpiresAt" IS DISTINCT FROM h."LeaseExpiresAt" OR NEW."OccurredAt"<>h."UpdatedAt"
                     OR (NEW."ResultingRevision"=0 AND (previous_revision IS NOT NULL OR NEW."FromState" IS NOT NULL))
                     OR (NEW."ResultingRevision">0 AND (previous_revision IS NULL OR previous_revision<>NEW."ResultingRevision"-1 OR previous_state IS DISTINCT FROM NEW."FromState"))
                  THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSITION_GRAPH_INVALID'; END IF;
                  RETURN NEW;
                END $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_job_head_mutation()
                RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
                DECLARE c text; px xid;
                BEGIN
                  c := current_setting('tagekyc.raw_export_job_mutation_context',true);
                  IF current_user <> 'tagekyc_raw_export_deployer' OR c IS NULL
                     OR c NOT IN ('job_head_insert','job_head_acquire','job_head_renew','job_head_release','job_head_terminal','job_head_assembly_sealed')
                     OR TG_OP = 'DELETE' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED'; END IF;
                  IF TG_OP='INSERT' THEN
                    SELECT xmin INTO px FROM tagekyc.raw_export_job_identities WHERE "JobId"=NEW."JobId";
                    IF c<>'job_head_insert' OR NOT FOUND OR px<>pg_current_xact_id()::xid OR NEW."CurrentState"<>'Claimed' OR NEW."Revision"<>0
                       OR NEW."CurrentAttemptId" IS NOT NULL OR NEW."LeaseOwnerId" IS NOT NULL OR NEW."LeaseExpiresAt" IS NOT NULL OR NEW."FencingToken"<>0
                    THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID'; END IF;
                  ELSIF NEW."JobId"<>OLD."JobId" OR NEW."Revision"<>OLD."Revision"+1 OR NEW."UpdatedAt"<OLD."UpdatedAt" THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_acquire' AND NOT (OLD."CurrentState" IN ('Claimed','Assembling') AND NEW."CurrentState"='Assembling' AND NEW."CurrentAttemptId" IS NOT NULL AND NEW."CurrentAttemptId" IS DISTINCT FROM OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken"+1 AND NEW."LeaseOwnerId" IS NOT NULL AND NEW."LeaseExpiresAt">NEW."UpdatedAt") THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_renew' AND NOT (OLD."CurrentState"='Assembling' AND NEW."CurrentState"='Assembling' AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId"=OLD."LeaseOwnerId" AND NEW."LeaseOwnerId" IS NOT NULL AND NEW."LeaseExpiresAt">NEW."UpdatedAt") THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_release' AND NOT (OLD."CurrentState"='Assembling' AND NEW."CurrentState"='Assembling' AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_terminal' AND NOT (OLD."CurrentState" IN ('Claimed','Assembling') AND NEW."CurrentState" IN ('TerminalFailed','Cancelled','Expired') AND NEW."CurrentAttemptId" IS NOT DISTINCT FROM OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_assembly_sealed' AND NOT (OLD."CurrentState"='Assembling' AND NEW."CurrentState"='AssemblySealed' AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_insert' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED'; END IF;
                  RETURN NEW;
                END $$;

                ALTER FUNCTION tagekyc.enforce_raw_export_job_transition_insert() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_job_head_mutation() OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.enforce_raw_export_job_transition_insert(),tagekyc.enforce_raw_export_job_head_mutation() FROM PUBLIC;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_freeze_job_source_bindings(
                  p_job_id uuid,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,p_actor_principal_id uuid)
                RETURNS TABLE("Outcome" text,"BindingCount" integer)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE
                  head tagekyc.raw_export_job_operational_heads%ROWTYPE;
                  job tagekyc.raw_export_job_identities%ROWTYPE;
                  class_row record; source_row record; authority record; consent record;
                  now_ timestamptz; binding_id uuid; binding_hash text; binding_digest bytea;
                  binding_preimage bytea; field_bytes bytea; field_ text;
                  existing tagekyc.raw_export_job_source_bindings%ROWTYPE; count_ integer:=0;
                  created_count integer:=0; inserted_count integer:=0;
                BEGIN
                  IF tagekyc.raw_export_current_actor() IS DISTINCT FROM p_actor_principal_id THEN
                    RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,0; RETURN;
                  END IF;
                  SELECT * INTO head FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,0; RETURN; END IF;
                  IF head."CurrentState"<>'Assembling' OR head."CurrentAttemptId" IS DISTINCT FROM p_attempt_id
                     OR head."Revision"<>p_expected_revision OR head."FencingToken"<>p_expected_fence THEN
                    RETURN QUERY SELECT 'LeaseLost'::text,0; RETURN;
                  END IF;
                  PERFORM 1 FROM tagekyc.raw_export_job_attempts
                    WHERE "JobId"=p_job_id AND "AttemptId"=p_attempt_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'LeaseLost'::text,0; RETURN; END IF;
                  SELECT * INTO job FROM tagekyc.raw_export_job_identities WHERE "JobId"=p_job_id FOR SHARE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,0; RETURN; END IF;
                  -- Lock every selected source and both freshness domains before taking the one admission clock.
                  FOR class_row IN
                    SELECT * FROM tagekyc.raw_export_job_classes WHERE "JobId"=p_job_id ORDER BY "Ordinal"
                  LOOP
                    SELECT s."CaptureAcceptanceId",r."ConsentPolicyId",r."ConsentPolicyVersion"
                    INTO source_row
                    FROM tagekyc.raw_export_session_capture_selections s
                    JOIN tagekyc.raw_export_capture_acceptance_events a0 ON a0."CaptureAcceptanceId"=s."CaptureAcceptanceId"
                    JOIN tagekyc.raw_export_source_ingress_claims c ON c."VerificationSessionId"=s."VerificationSessionId" AND c."CaptureAcceptanceId"=s."CaptureAcceptanceId" AND c."CaptureArtifactId"=a0."CaptureArtifactId" AND c."CaptureRevision"=a0."CaptureRevision" AND c."RawClass"=s."RawClass"
                    JOIN tagekyc.raw_export_source_reservations r ON r."IngressClaimId"=c."IngressClaimId"
                    JOIN tagekyc.raw_export_source_publications p ON p."SourceArtifactId"=r."SourceArtifactId"
                    JOIN tagekyc.raw_export_source_encryption_attempts ea ON ea."AttemptId"=p."AttemptId"
                    JOIN tagekyc.raw_export_attempt_key_reservations k ON k."AttemptKeyReservationId"=p."AttemptKeyReservationId"
                    JOIN tagekyc.raw_export_provisional_objects o ON o."ObjectCustodyId"=p."ObjectCustodyId"
                    WHERE s."VerificationSessionId"=job."VerificationSessionId" AND s."RawClass"=class_row."RawClass"
                      AND a0."VerificationSessionId"=job."VerificationSessionId" AND a0."RawClass"=class_row."RawClass"
                      AND p."PublicationState"='Available' AND p."AvailableAtUtc" IS NOT NULL
                      AND ea."StagedAtUtc" IS NOT NULL AND ea."R2TerminationDisposition" IS NULL
                      AND o."State"='VerifiedCompleted' AND k."PreparationDisposition"='Active'
                    FOR SHARE OF p,ea,k,o;
                    IF NOT FOUND THEN RETURN QUERY SELECT 'SourceUnavailable'::text,0; RETURN; END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||job."ClientApplicationId"::text||':'||job."VerificationSessionId"::text||':'||source_row."CaptureAcceptanceId"::text||':'||class_row."RawClass"));
                    PERFORM 1 FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(job."VerificationSessionId",source_row."ConsentPolicyId",source_row."ConsentPolicyVersion") WHERE "RawClass"=class_row."RawClass" AND "RecipientClientApplicationId"=job."RecipientClientApplicationId";
                  END LOOP;
                  now_:=pg_catalog.clock_timestamp();
                  IF head."LeaseExpiresAt" IS NULL OR head."LeaseExpiresAt"<=now_
                     OR job."PermitExpiresAt"<=now_ OR job."JobExpiresAt"<=now_ THEN
                    RETURN QUERY SELECT 'Expired'::text,0; RETURN;
                  END IF;

                  FOR class_row IN
                    SELECT * FROM tagekyc.raw_export_job_classes WHERE "JobId"=p_job_id ORDER BY "Ordinal"
                  LOOP
                    SELECT s."SessionCaptureSelectionId",s."CaptureAcceptanceId",a0."CaptureArtifactId",a0."CaptureRevision",
                           r."SourceArtifactId",p."SourcePublicationId",p."PublicationRevision",p."AttemptId",ea."EncryptionAttemptRevision",ea."Fence",
                           p."AttemptKeyReservationId",p."ObjectCustodyId",o."StateRevision",
                           r."SubjectRefTokenSchemaVersion",r."SubjectRefTokenKeyId",r."SubjectRefTokenKeyVersion",r."SubjectRefToken",
                           r."ContentCommitmentSchemaVersion",r."ContentCommitmentKeyId",r."ContentCommitmentKeyVersion",r."ContentCommitment",
                           r."ClaimedPlaintextLength",r."MediaType",r."AuthoritySnapshotSchemaVersion",r."AuthoritySnapshotId",
                           p."PublishedAuthorityRevision",r."ConsentPolicyId",r."ConsentPolicyVersion",r."AbsoluteSourceExpiresAtUtc",
                           r."EffectivePlaintextRetentionExpiresAtUtc",r."StableDataScopeId",r."ControllerIdentity"
                    INTO source_row
                    FROM tagekyc.raw_export_session_capture_selections s
                    JOIN tagekyc.raw_export_capture_acceptance_events a0 ON a0."CaptureAcceptanceId"=s."CaptureAcceptanceId"
                    JOIN tagekyc.raw_export_source_ingress_claims c ON c."VerificationSessionId"=s."VerificationSessionId" AND c."CaptureAcceptanceId"=s."CaptureAcceptanceId" AND c."CaptureArtifactId"=a0."CaptureArtifactId" AND c."CaptureRevision"=a0."CaptureRevision" AND c."RawClass"=s."RawClass"
                    JOIN tagekyc.raw_export_source_reservations r ON r."IngressClaimId"=c."IngressClaimId"
                    JOIN tagekyc.raw_export_source_publications p ON p."SourceArtifactId"=r."SourceArtifactId"
                    JOIN tagekyc.raw_export_source_encryption_attempts ea ON ea."AttemptId"=p."AttemptId"
                    JOIN tagekyc.raw_export_attempt_key_reservations k ON k."AttemptKeyReservationId"=p."AttemptKeyReservationId"
                    JOIN tagekyc.raw_export_provisional_objects o ON o."ObjectCustodyId"=p."ObjectCustodyId"
                    WHERE s."VerificationSessionId"=job."VerificationSessionId" AND s."RawClass"=class_row."RawClass"
                      AND a0."VerificationSessionId"=job."VerificationSessionId" AND a0."RawClass"=class_row."RawClass"
                      AND p."PublicationState"='Available' AND p."AvailableAtUtc" IS NOT NULL
                      AND ea."StagedAtUtc" IS NOT NULL AND ea."R2TerminationDisposition" IS NULL
                      AND o."State"='VerifiedCompleted' AND k."PreparationDisposition"='Active'
                    FOR SHARE OF p,ea,k,o;
                    IF NOT FOUND THEN RETURN QUERY SELECT 'SourceUnavailable'::text,0; RETURN; END IF;
                    IF source_row."AbsoluteSourceExpiresAtUtc"<=now_ OR source_row."EffectivePlaintextRetentionExpiresAtUtc"<=now_ THEN
                      RETURN QUERY SELECT 'Expired'::text,0; RETURN;
                    END IF;

                    SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(job."VerificationSessionId",source_row."ConsentPolicyId",source_row."ConsentPolicyVersion") WHERE "RawClass"=class_row."RawClass" AND "RecipientClientApplicationId"=job."RecipientClientApplicationId";
                    SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(job."ClientApplicationId",job."VerificationSessionId",source_row."CaptureAcceptanceId",class_row."RawClass",now_);
                    IF authority."AuthoritySnapshotId" IS NULL OR consent."State" IS DISTINCT FROM 'Effective'
                       OR consent."ValidFromUtc">now_ OR consent."ValidUntilUtc"<=now_
                       OR consent."PolicyId"<>source_row."ConsentPolicyId" OR consent."PolicyVersion"<>source_row."ConsentPolicyVersion"
                       OR authority."AuthoritySnapshotSchemaVersion"<>source_row."AuthoritySnapshotSchemaVersion"
                       OR authority."AuthoritySnapshotId"<>source_row."AuthoritySnapshotId"
                       OR authority."Revision"<>source_row."PublishedAuthorityRevision"
                       OR authority."ReuseDisposition"<>'FreshAuthorityRequired' OR authority."ExtensionDisposition"<>'Forbidden'
                       OR authority."ConsentPolicyId"<>source_row."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>source_row."ConsentPolicyVersion" THEN
                      RETURN QUERY SELECT 'AuthorityInvalid'::text,0; RETURN;
                    END IF;

                    binding_preimage:=''::bytea;
                    FOREACH field_ IN ARRAY ARRAY[
                      'tip-88c1-job-source-binding-v1',pg_catalog.replace(p_job_id::text,'-',''),class_row."Ordinal"::text,class_row."RawClass",
                      pg_catalog.replace(job."VerificationSessionId"::text,'-',''),pg_catalog.replace(source_row."SessionCaptureSelectionId"::text,'-',''),
                      pg_catalog.replace(source_row."CaptureAcceptanceId"::text,'-',''),pg_catalog.replace(source_row."CaptureArtifactId"::text,'-',''),source_row."CaptureRevision"::text,
                      pg_catalog.replace(source_row."SourceArtifactId"::text,'-',''),pg_catalog.replace(source_row."SourcePublicationId"::text,'-',''),source_row."PublicationRevision"::text,
                      pg_catalog.replace(source_row."AttemptId"::text,'-',''),source_row."EncryptionAttemptRevision"::text,source_row."Fence"::text,
                      pg_catalog.replace(source_row."AttemptKeyReservationId"::text,'-',''),pg_catalog.replace(source_row."ObjectCustodyId"::text,'-',''),source_row."StateRevision"::text,
                      source_row."SubjectRefTokenSchemaVersion"::text,source_row."SubjectRefTokenKeyId",source_row."SubjectRefTokenKeyVersion"::text,pg_catalog.encode(source_row."SubjectRefToken",'hex'),
                      source_row."ContentCommitmentSchemaVersion"::text,source_row."ContentCommitmentKeyId",source_row."ContentCommitmentKeyVersion"::text,pg_catalog.encode(source_row."ContentCommitment",'hex'),
                      source_row."ClaimedPlaintextLength"::text,source_row."MediaType",source_row."AuthoritySnapshotSchemaVersion"::text,pg_catalog.replace(source_row."AuthoritySnapshotId"::text,'-',''),
                      authority."Revision"::text,pg_catalog.replace(source_row."ConsentPolicyId"::text,'-',''),source_row."ConsentPolicyVersion"::text,
                      pg_catalog.to_char(source_row."AbsoluteSourceExpiresAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                      pg_catalog.to_char(source_row."EffectivePlaintextRetentionExpiresAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"'),
                      source_row."StableDataScopeId",source_row."ControllerIdentity",'1']
                    LOOP
                      field_bytes:=pg_catalog.convert_to(field_,'UTF8');
                      binding_preimage:=binding_preimage||pg_catalog.int4send(pg_catalog.octet_length(field_bytes))||field_bytes;
                    END LOOP;
                    binding_digest:=tagekyc_extensions.digest(binding_preimage,'sha256');
                    field_bytes:=tagekyc_extensions.digest(pg_catalog.convert_to(
                      'tip-88c1-job-source-binding-id-v1'||pg_catalog.chr(10)||
                      '{"JobId":"'||p_job_id::text||'","Ordinal":'||class_row."Ordinal"::text||',"RawClass":'||pg_catalog.to_json(class_row."RawClass")::text||'}','UTF8'),'sha256');
                    field_bytes:=pg_catalog.set_byte(field_bytes,7,(pg_catalog.get_byte(field_bytes,7)&15)|80);
                    field_bytes:=pg_catalog.set_byte(field_bytes,8,(pg_catalog.get_byte(field_bytes,8)&63)|128);
                    binding_hash:=pg_catalog.encode(pg_catalog.substring(field_bytes,1,16),'hex');
                    binding_id:=(pg_catalog.substr(binding_hash,7,2)||pg_catalog.substr(binding_hash,5,2)||pg_catalog.substr(binding_hash,3,2)||pg_catalog.substr(binding_hash,1,2)||'-'||
                      pg_catalog.substr(binding_hash,11,2)||pg_catalog.substr(binding_hash,9,2)||'-'||pg_catalog.substr(binding_hash,15,2)||pg_catalog.substr(binding_hash,13,2)||'-'||
                      pg_catalog.substr(binding_hash,17,4)||'-'||pg_catalog.substr(binding_hash,21,12))::uuid;

                    INSERT INTO tagekyc.raw_export_job_source_bindings(
                      "JobSourceBindingId","JobId","Ordinal","RawClass","VerificationSessionId","SessionCaptureSelectionId","CaptureAcceptanceId","CaptureArtifactId","CaptureRevision","SourceArtifactId","SourcePublicationId","SourcePublicationRevision","EncryptionAttemptId","EncryptionAttemptRevision","EncryptionAttemptFence","AttemptKeyReservationId","ObjectCustodyId","ObjectStateRevision","SubjectRefTokenSchemaVersion","SubjectRefTokenKeyId","SubjectRefTokenKeyVersion","SubjectRefToken","ContentCommitmentSchemaVersion","ContentCommitmentKeyId","ContentCommitmentKeyVersion","ContentCommitment","PlaintextLength","MediaType","AuthoritySnapshotSchemaVersion","AuthoritySnapshotId","AuthorityRevision","ConsentPolicyId","ConsentPolicyVersion","AbsoluteSourceExpiresAtUtc","EffectivePlaintextRetentionExpiresAtUtc","StableDataScopeId","ControllerIdentity","BindingFingerprint","CreatedAtUtc","SchemaVersion")
                    VALUES(binding_id,p_job_id,class_row."Ordinal",class_row."RawClass",job."VerificationSessionId",source_row."SessionCaptureSelectionId",source_row."CaptureAcceptanceId",source_row."CaptureArtifactId",source_row."CaptureRevision",source_row."SourceArtifactId",source_row."SourcePublicationId",source_row."PublicationRevision",source_row."AttemptId",source_row."EncryptionAttemptRevision",source_row."Fence",source_row."AttemptKeyReservationId",source_row."ObjectCustodyId",source_row."StateRevision",source_row."SubjectRefTokenSchemaVersion",source_row."SubjectRefTokenKeyId",source_row."SubjectRefTokenKeyVersion",source_row."SubjectRefToken",source_row."ContentCommitmentSchemaVersion",source_row."ContentCommitmentKeyId",source_row."ContentCommitmentKeyVersion",source_row."ContentCommitment",source_row."ClaimedPlaintextLength",source_row."MediaType",source_row."AuthoritySnapshotSchemaVersion",source_row."AuthoritySnapshotId",authority."Revision",source_row."ConsentPolicyId",source_row."ConsentPolicyVersion",source_row."AbsoluteSourceExpiresAtUtc",source_row."EffectivePlaintextRetentionExpiresAtUtc",source_row."StableDataScopeId",source_row."ControllerIdentity",binding_digest,now_,1)
                    ON CONFLICT DO NOTHING;
                    GET DIAGNOSTICS inserted_count=ROW_COUNT;
                    created_count:=created_count+inserted_count;
                    SELECT * INTO existing FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=p_job_id AND "Ordinal"=class_row."Ordinal";
                    IF existing."BindingFingerprint" IS DISTINCT FROM binding_digest THEN RETURN QUERY SELECT 'BindingConflict'::text,count_; RETURN; END IF;
                    count_:=count_+1;
                  END LOOP;
                  IF count_=0 THEN RETURN QUERY SELECT 'SourceUnavailable'::text,0;
                  ELSIF created_count=count_ THEN RETURN QUERY SELECT 'Frozen'::text,count_;
                  ELSIF created_count=0 THEN RETURN QUERY SELECT 'ExistingMatch'::text,count_;
                  ELSE RETURN QUERY SELECT 'BindingConflict'::text,count_;
                  END IF;
                END $$;

                CREATE FUNCTION tagekyc.raw_export_read_job_source_verification_context(
                  p_job_id uuid,p_ordinal integer,p_attempt_id uuid,p_expected_revision bigint,p_expected_fence bigint,p_actor_principal_id uuid)
                RETURNS TABLE(
                  "JobSourceBindingId" uuid,"JobId" uuid,"Ordinal" integer,"RawClass" text,"SourceArtifactId" uuid,
                  "AttemptId" uuid,"EncryptionAttemptRevision" bigint,"Fence" bigint,"AttemptKeyReservationId" uuid,
                  "ProvisionalObjectIdentity" uuid,"EncryptionAttemptFingerprint" bytea,"KeyProviderId" text,"KekId" text,
                  "KekVersion" integer,"KekFingerprint" text,"EncryptionSuiteId" text,"EncryptionFramingVersion" integer,
                  "ChunkSize" integer,"NonceStrategyId" text,"FramingParametersDigest" bytea,"WrappedDekMetadataDigest" bytea,
                  "VerificationSessionId" uuid,"CaptureArtifactId" uuid,"CaptureRevision" integer,"StableDataScopeId" text,
                  "ControllerIdentity" text,"ClaimedPlaintextLength" bigint,"MediaType" text,"ContentCommitmentSchemaVersion" integer,
                  "ContentCommitmentKeyId" text,"ContentCommitmentKeyVersion" integer,"ContentCommitment" bytea,
                  "ObjectCustodyId" uuid,"ObjectKey" text,"ObjectBindingDigest" bytea,"ObjectState" text,"ObjectStateRevision" bigint,
                  "CiphertextLength" bigint,"CiphertextDigest" bytea,"BindingFingerprint" bytea,
                  "PermitId" uuid,"ClientApplicationId" uuid,"RecipientClientApplicationId" uuid,"PolicyId" uuid,
                  "PolicyVersion" integer,"PurposeCode" text,"ExportMode" text,"JobExpiresAtUtc" timestamptz,
                  "JobCreatedAtUtc" timestamptz,"SubjectRefTokenSchemaVersion" integer,"SubjectRefTokenKeyId" text,
                  "SubjectRefTokenKeyVersion" integer,"SubjectRefToken" bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $$
                  SELECT b."JobSourceBindingId",b."JobId",b."Ordinal",b."RawClass",b."SourceArtifactId",
                    a."AttemptId",a."EncryptionAttemptRevision",a."Fence",a."AttemptKeyReservationId",a."ProvisionalObjectIdentity",
                    a."EncryptionAttemptFingerprint",a."KeyProviderId",a."KekId",a."KekVersion",a."KekFingerprint",a."EncryptionSuiteId",
                    a."EncryptionFramingVersion",a."ChunkSize",a."NonceStrategyId",a."FramingParametersDigest",k."WrappedDekMetadataDigest",
                    b."VerificationSessionId",b."CaptureArtifactId",b."CaptureRevision",b."StableDataScopeId",b."ControllerIdentity",
                    b."PlaintextLength",b."MediaType",b."ContentCommitmentSchemaVersion",b."ContentCommitmentKeyId",b."ContentCommitmentKeyVersion",b."ContentCommitment",
                    o."ObjectCustodyId",o."ObjectKey",o."ObjectBindingDigest",o."State",o."StateRevision",o."CiphertextLength",o."CiphertextDigest",b."BindingFingerprint",
                    j."PermitId",j."ClientApplicationId",j."RecipientClientApplicationId",j."PolicyId",j."PolicyVersion",
                    j."PurposeCode",j."ExportMode",j."JobExpiresAt",j."CreatedAt",b."SubjectRefTokenSchemaVersion",
                    b."SubjectRefTokenKeyId",b."SubjectRefTokenKeyVersion",b."SubjectRefToken"
                  FROM tagekyc.raw_export_job_source_bindings b
                  JOIN tagekyc.raw_export_job_identities j ON j."JobId"=b."JobId"
                  JOIN tagekyc.raw_export_job_operational_heads h ON h."JobId"=b."JobId"
                  JOIN tagekyc.raw_export_source_encryption_attempts a ON a."AttemptId"=b."EncryptionAttemptId"
                  JOIN tagekyc.raw_export_attempt_key_reservations k ON k."AttemptKeyReservationId"=b."AttemptKeyReservationId"
                  JOIN tagekyc.raw_export_provisional_objects o ON o."ObjectCustodyId"=b."ObjectCustodyId"
                  WHERE b."JobId"=p_job_id AND b."Ordinal"=p_ordinal AND a."AttemptId"=b."EncryptionAttemptId"
                    AND h."CurrentState"='Assembling' AND h."CurrentAttemptId"=p_attempt_id
                    AND h."Revision"=p_expected_revision AND h."FencingToken"=p_expected_fence
                    AND h."LeaseExpiresAt">pg_catalog.clock_timestamp()
                    AND tagekyc.raw_export_current_actor()=p_actor_principal_id
                    AND o."State"='VerifiedCompleted' AND k."PreparationDisposition"='Active';
                $$;

                CREATE FUNCTION tagekyc.raw_export_register_assembly_preparing(
                  p_c2_preparation_id uuid,p_assembly_id uuid,p_job_id uuid,p_expected_revision bigint,p_expected_fence bigint,p_assembly_fingerprint bytea,p_preparation_fingerprint bytea)
                RETURNS TABLE("Outcome" text,"RowRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE h tagekyc.raw_export_job_operational_heads%ROWTYPE; now_ timestamptz; row_ tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE; inserted_count integer:=0;
                BEGIN
                  SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint; RETURN; END IF;
                  IF h."CurrentState"<>'Assembling' OR h."Revision"<>p_expected_revision OR h."FencingToken"<>p_expected_fence OR h."CurrentAttemptId" IS NULL THEN RETURN QUERY SELECT 'LeaseLost'::text,NULL::bigint; RETURN; END IF;
                  now_:=pg_catalog.clock_timestamp();
                  INSERT INTO tagekyc.raw_export_assembly_preparation_dispositions VALUES(p_c2_preparation_id,p_assembly_id,p_job_id,h."CurrentAttemptId",p_expected_fence,p_assembly_fingerprint,p_preparation_fingerprint,'Preparing',NULL,NULL,1,now_,NULL,NULL,NULL,NULL,NULL,1) ON CONFLICT DO NOTHING;
                  GET DIAGNOSTICS inserted_count=ROW_COUNT;
                  SELECT * INTO row_ FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id;
                  IF row_."AssemblyId"=p_assembly_id AND row_."JobId"=p_job_id AND row_."AttemptId"=h."CurrentAttemptId" AND row_."FencingToken"=p_expected_fence AND row_."AssemblyFingerprint"=p_assembly_fingerprint AND row_."PreparationFingerprint"=p_preparation_fingerprint THEN
                    RETURN QUERY SELECT CASE WHEN inserted_count=1 THEN 'Created' ELSE 'ExistingMatch' END,row_."RowRevision"; RETURN;
                  END IF;
                  RETURN QUERY SELECT 'PreparationConflict'::text,NULL::bigint;
                END $$;

                CREATE FUNCTION tagekyc.raw_export_record_assembly_pending(p_c2_preparation_id uuid,p_expected_row_revision bigint,p_provider_receipt_digest bytea)
                RETURNS TABLE("Outcome" text,"RowRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE row_ tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE; now_ timestamptz;
                BEGIN
                  SELECT * INTO row_ FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint; RETURN; END IF;
                  IF row_."Disposition"='Pending' AND row_."ProviderReceiptDigest"=p_provider_receipt_digest THEN RETURN QUERY SELECT 'ExistingMatch'::text,row_."RowRevision"; RETURN; END IF;
                  IF row_."Disposition"<>'Preparing' OR row_."RowRevision"<>p_expected_row_revision OR pg_catalog.octet_length(p_provider_receipt_digest)<>32 THEN RETURN QUERY SELECT 'PreparationConflict'::text,NULL::bigint; RETURN; END IF;
                  now_:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_assembly_preparation_dispositions AS d SET "Disposition"='Pending',"ProviderReceiptDigest"=p_provider_receipt_digest,"RowRevision"=d."RowRevision"+1,"PendingAtUtc"=now_ WHERE d."C2PreparationId"=p_c2_preparation_id;
                  RETURN QUERY SELECT 'Pending'::text,row_."RowRevision"+1;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_seal_authenticated_assembly(
                  p_c2_preparation_id uuid,p_job_id uuid,p_expected_preparation_revision bigint,p_expected_job_revision bigint,p_expected_fence bigint,p_attempt_id uuid,
                  p_assembly_digest bytea,p_manifest_digest bytea,p_authentication_value bytea,p_authentication_key_id text,p_authentication_key_version integer,
                  p_assembly_fingerprint bytea,p_complete_assembly_length bigint,p_item_count integer,p_items jsonb)
                RETURNS TABLE("Outcome" text,"JobRevision" bigint,"PreparationRevision" bigint,"SealedAtUtc" timestamptz)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE
                  prep tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE; h tagekyc.raw_export_job_operational_heads%ROWTYPE;
                  j tagekyc.raw_export_job_identities%ROWTYPE; b record; authority record; consent record; now_ timestamptz;
                  first_binding tagekyc.raw_export_job_source_bindings%ROWTYPE; actual_count integer;
                  previous_head_context text; previous_transition_context text;
                BEGIN
                  SELECT p.* INTO prep FROM tagekyc.raw_export_assembly_preparation_dispositions p
                  JOIN tagekyc.raw_export_assembly_identities i ON i."C2PreparationId"=p."C2PreparationId"
                  WHERE p."C2PreparationId"=p_c2_preparation_id AND p."Disposition" IN ('SealCommitted','Finalized')
                    AND p."JobId"=p_job_id AND p."AttemptId"=p_attempt_id AND p."FencingToken"=p_expected_fence
                    AND p."AssemblyFingerprint"=p_assembly_fingerprint AND i."JobId"=p_job_id
                    AND i."AttemptId"=p_attempt_id AND i."FencingToken"=p_expected_fence AND i."AssemblyDigest"=p_assembly_digest
                    AND i."ManifestDigest"=p_manifest_digest AND i."AssemblyAuthenticationValue"=p_authentication_value
                    AND i."AssemblyAuthenticationKeyId"=p_authentication_key_id AND i."AssemblyAuthenticationKeyVersion"=p_authentication_key_version
                    AND i."CompleteAssemblyLength"=p_complete_assembly_length AND i."ItemCount"=p_item_count
                    AND (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                          'Ordinal',a."Ordinal",'RawClass',a."RawClass",'SourceArtifactId',a."SourceArtifactId",
                          'CaptureArtifactId',a."CaptureArtifactId",'CaptureRevision',a."CaptureRevision",'MediaType',a."MediaType",
                          'PlaintextLength',a."PlaintextLength",'ContentCommitmentSchemaVersion',a."ContentCommitmentSchemaVersion",
                          'ContentCommitmentKeyId',a."ContentCommitmentKeyId",'ContentCommitmentKeyVersion',a."ContentCommitmentKeyVersion",
                          'ContentCommitment',pg_catalog.encode(a."ContentCommitment",'base64')) ORDER BY a."Ordinal")
                         FROM tagekyc.raw_export_assembly_items a WHERE a."AssemblyId"=i."AssemblyId")=p_items
                    AND EXISTS (SELECT 1 FROM tagekyc.raw_export_job_transitions t
                                WHERE t."JobId"=p_job_id AND t."ResultingRevision"=p_expected_job_revision+1
                                  AND t."EventType"='AssemblySealed' AND t."FromState"='Assembling' AND t."ToState"='AssemblySealed'
                                  AND t."AttemptId"=p_attempt_id AND t."FencingToken"=p_expected_fence);
                  IF FOUND THEN RETURN QUERY SELECT 'ExistingMatch'::text,p_expected_job_revision+1,prep."RowRevision",prep."SealCommittedAtUtc"; RETURN; END IF;
                  IF EXISTS (
                    SELECT 1 FROM tagekyc.raw_export_assembly_preparation_dispositions p
                    LEFT JOIN tagekyc.raw_export_assembly_identities i ON i."C2PreparationId"=p."C2PreparationId"
                    WHERE p."C2PreparationId"=p_c2_preparation_id
                      AND (p."Disposition" IN ('SealCommitted','Finalized') OR i."AssemblyId" IS NOT NULL))
                  THEN RETURN QUERY SELECT 'AssemblyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

                  SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=p_job_id FOR UPDATE;
                  IF h."JobId" IS NULL THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
                  PERFORM 1 FROM tagekyc.raw_export_job_attempts WHERE "AttemptId"=p_attempt_id AND "JobId"=p_job_id FOR UPDATE;
                  SELECT * INTO j FROM tagekyc.raw_export_job_identities WHERE "JobId"=p_job_id FOR SHARE;
                  IF j."JobId" IS NULL THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
                  IF NOT FOUND OR h."CurrentState"<>'Assembling' OR h."CurrentAttemptId" IS DISTINCT FROM p_attempt_id OR h."Revision"<>p_expected_job_revision OR h."FencingToken"<>p_expected_fence THEN RETURN QUERY SELECT 'LeaseLost'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

                  PERFORM 1 FROM tagekyc.raw_export_source_publications p JOIN tagekyc.raw_export_job_source_bindings x ON x."SourcePublicationId"=p."SourcePublicationId" WHERE x."JobId"=p_job_id ORDER BY p."SourceArtifactId" FOR UPDATE OF p;
                  PERFORM 1 FROM tagekyc.raw_export_source_encryption_attempts a JOIN tagekyc.raw_export_job_source_bindings x ON x."EncryptionAttemptId"=a."AttemptId" WHERE x."JobId"=p_job_id ORDER BY a."AttemptId" FOR UPDATE OF a;
                  PERFORM 1 FROM tagekyc.raw_export_source_head s JOIN tagekyc.raw_export_job_source_bindings x ON x."SourceArtifactId"=s."SourceArtifactId" WHERE x."JobId"=p_job_id ORDER BY s."SourceArtifactId" FOR UPDATE OF s;
                  PERFORM 1 FROM tagekyc.raw_export_source_reservations r JOIN tagekyc.raw_export_job_source_bindings x ON x."SourceArtifactId"=r."SourceArtifactId" WHERE x."JobId"=p_job_id ORDER BY r."SourceArtifactId" FOR UPDATE OF r;
                  PERFORM 1 FROM tagekyc.raw_export_source_ingress_claims c JOIN tagekyc.raw_export_job_source_bindings x ON x."VerificationSessionId"=c."VerificationSessionId" AND x."CaptureAcceptanceId"=c."CaptureAcceptanceId" AND x."CaptureArtifactId"=c."CaptureArtifactId" AND x."CaptureRevision"=c."CaptureRevision" AND x."RawClass"=c."RawClass" WHERE x."JobId"=p_job_id ORDER BY c."IngressClaimId" FOR UPDATE OF c;
                  PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations k JOIN tagekyc.raw_export_job_source_bindings x ON x."AttemptKeyReservationId"=k."AttemptKeyReservationId" WHERE x."JobId"=p_job_id ORDER BY k."AttemptKeyReservationId" FOR UPDATE OF k;
                  PERFORM 1 FROM tagekyc.raw_export_provisional_objects o JOIN tagekyc.raw_export_job_source_bindings x ON x."ObjectCustodyId"=o."ObjectCustodyId" WHERE x."JobId"=p_job_id ORDER BY o."ObjectCustodyId" FOR UPDATE OF o;
                  PERFORM 1 FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=p_job_id ORDER BY "Ordinal" FOR UPDATE;
                  SELECT * INTO prep FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id FOR UPDATE;
                  IF NOT FOUND OR prep."Disposition"<>'Pending' OR prep."RowRevision"<>p_expected_preparation_revision OR prep."JobId"<>p_job_id OR prep."AttemptId"<>p_attempt_id OR prep."FencingToken"<>p_expected_fence OR prep."AssemblyFingerprint"<>p_assembly_fingerprint THEN RETURN QUERY SELECT 'PreparationConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

                  actual_count:=(SELECT pg_catalog.count(*)::integer FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=p_job_id);
                  IF actual_count<>p_item_count OR p_item_count<1 OR pg_catalog.jsonb_typeof(p_items)<>'array' OR pg_catalog.jsonb_array_length(p_items)<>p_item_count OR pg_catalog.octet_length(p_assembly_digest)<>32 OR pg_catalog.octet_length(p_manifest_digest)<>32 OR pg_catalog.octet_length(p_authentication_value)<>32 OR pg_catalog.octet_length(p_assembly_fingerprint)<>32 OR p_authentication_key_version<1 OR p_complete_assembly_length<1 THEN RETURN QUERY SELECT 'AssemblyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
                  IF (SELECT pg_catalog.jsonb_agg(pg_catalog.jsonb_build_object(
                        'Ordinal',x."Ordinal",'RawClass',x."RawClass",'SourceArtifactId',x."SourceArtifactId",
                        'CaptureArtifactId',x."CaptureArtifactId",'CaptureRevision',x."CaptureRevision",'MediaType',x."MediaType",
                        'PlaintextLength',x."PlaintextLength",'ContentCommitmentSchemaVersion',x."ContentCommitmentSchemaVersion",
                        'ContentCommitmentKeyId',x."ContentCommitmentKeyId",'ContentCommitmentKeyVersion',x."ContentCommitmentKeyVersion",
                        'ContentCommitment',pg_catalog.encode(x."ContentCommitment",'base64')) ORDER BY x."Ordinal")
                      FROM tagekyc.raw_export_job_source_bindings x WHERE x."JobId"=p_job_id) IS DISTINCT FROM p_items
                  THEN RETURN QUERY SELECT 'AssemblyConflict'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;

                  -- Acquire every authority and consent lock before the single admission clock.
                  FOR b IN SELECT x.*,c."ClientApplicationId" FROM tagekyc.raw_export_job_source_bindings x JOIN tagekyc.raw_export_source_ingress_claims c ON c."VerificationSessionId"=x."VerificationSessionId" AND c."CaptureAcceptanceId"=x."CaptureAcceptanceId" AND c."CaptureArtifactId"=x."CaptureArtifactId" AND c."CaptureRevision"=x."CaptureRevision" AND c."RawClass"=x."RawClass" WHERE x."JobId"=p_job_id ORDER BY x."Ordinal"
                  LOOP
                    PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||b."ClientApplicationId"::text||':'||b."VerificationSessionId"::text||':'||b."CaptureAcceptanceId"::text||':'||b."RawClass"));
                    PERFORM 1 FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(b."VerificationSessionId",b."ConsentPolicyId",b."ConsentPolicyVersion") WHERE "RawClass"=b."RawClass" AND "RecipientClientApplicationId"=j."RecipientClientApplicationId";
                  END LOOP;
                  now_:=pg_catalog.clock_timestamp();
                  FOR b IN SELECT x.*,c."ClientApplicationId" FROM tagekyc.raw_export_job_source_bindings x JOIN tagekyc.raw_export_source_ingress_claims c ON c."VerificationSessionId"=x."VerificationSessionId" AND c."CaptureAcceptanceId"=x."CaptureAcceptanceId" AND c."CaptureArtifactId"=x."CaptureArtifactId" AND c."CaptureRevision"=x."CaptureRevision" AND c."RawClass"=x."RawClass" WHERE x."JobId"=p_job_id ORDER BY x."Ordinal"
                  LOOP
                    SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(b."VerificationSessionId",b."ConsentPolicyId",b."ConsentPolicyVersion") WHERE "RawClass"=b."RawClass" AND "RecipientClientApplicationId"=j."RecipientClientApplicationId";
                    SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(b."ClientApplicationId",b."VerificationSessionId",b."CaptureAcceptanceId",b."RawClass",now_);
                    IF consent."State" IS DISTINCT FROM 'Effective'
                       OR consent."ValidFromUtc">now_ OR consent."ValidUntilUtc"<=now_
                       OR consent."PolicyId"<>b."ConsentPolicyId" OR consent."PolicyVersion"<>b."ConsentPolicyVersion"
                       OR authority."AuthoritySnapshotId" IS NULL OR authority."AuthoritySnapshotId"<>b."AuthoritySnapshotId"
                       OR authority."Revision"<>b."AuthorityRevision" OR authority."ReuseDisposition"<>'FreshAuthorityRequired'
                       OR authority."ExtensionDisposition"<>'Forbidden' OR authority."ConsentPolicyId"<>b."ConsentPolicyId"
                       OR authority."ConsentPolicyVersion"<>b."ConsentPolicyVersion"
                       OR b."AbsoluteSourceExpiresAtUtc"<=now_ OR b."EffectivePlaintextRetentionExpiresAtUtc"<=now_
                    THEN RETURN QUERY SELECT 'AuthorityInvalid'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
                  END LOOP;
                  IF h."LeaseExpiresAt" IS NULL OR h."LeaseExpiresAt"<=now_ OR j."PermitExpiresAt"<=now_ OR j."JobExpiresAt"<=now_ THEN RETURN QUERY SELECT 'Expired'::text,NULL::bigint,NULL::bigint,NULL::timestamptz; RETURN; END IF;
                  SELECT * INTO first_binding FROM tagekyc.raw_export_job_source_bindings WHERE "JobId"=p_job_id ORDER BY "Ordinal" LIMIT 1;

                  UPDATE tagekyc.raw_export_assembly_preparation_dispositions AS d SET "Disposition"='SealCommitted',"RowRevision"=d."RowRevision"+1,"SealCommittedAtUtc"=now_ WHERE d."C2PreparationId"=p_c2_preparation_id;
                  INSERT INTO tagekyc.raw_export_assembly_identities(
                    "AssemblyId","JobId","AttemptId","FencingToken","PermitId","VerificationSessionId","ClientApplicationId","RecipientClientApplicationId","PolicyId","PolicyVersion","PurposeCode","ExportMode","ManifestVersion","SubjectRefTokenSchemaVersion","SubjectRefTokenKeyId","SubjectRefTokenKeyVersion","SubjectRefToken","AssemblyAuthenticationKeyId","AssemblyAuthenticationKeyVersion","AssemblyDigest","ManifestDigest","AssemblyAuthenticationValue","AssemblyFingerprint","CompleteAssemblyLength","ItemCount","C2PreparationId","CreatedAtUtc","JobExpiresAtUtc","SealedAtUtc","SchemaVersion")
                  VALUES(prep."AssemblyId",p_job_id,p_attempt_id,p_expected_fence,j."PermitId",j."VerificationSessionId",j."ClientApplicationId",j."RecipientClientApplicationId",j."PolicyId",j."PolicyVersion",j."PurposeCode",j."ExportMode",1,first_binding."SubjectRefTokenSchemaVersion",first_binding."SubjectRefTokenKeyId",first_binding."SubjectRefTokenKeyVersion",first_binding."SubjectRefToken",p_authentication_key_id,p_authentication_key_version,p_assembly_digest,p_manifest_digest,p_authentication_value,p_assembly_fingerprint,p_complete_assembly_length,p_item_count,p_c2_preparation_id,now_,j."JobExpiresAt",now_,1);
                  INSERT INTO tagekyc.raw_export_assembly_items("AssemblyId","Ordinal","RawClass","JobSourceBindingId","SourceArtifactId","CaptureArtifactId","CaptureRevision","MediaType","PlaintextLength","ContentCommitmentSchemaVersion","ContentCommitmentKeyId","ContentCommitmentKeyVersion","ContentCommitment")
                  SELECT prep."AssemblyId",x."Ordinal",x."RawClass",x."JobSourceBindingId",x."SourceArtifactId",x."CaptureArtifactId",x."CaptureRevision",x."MediaType",x."PlaintextLength",x."ContentCommitmentSchemaVersion",x."ContentCommitmentKeyId",x."ContentCommitmentKeyVersion",x."ContentCommitment" FROM tagekyc.raw_export_job_source_bindings x WHERE x."JobId"=p_job_id ORDER BY x."Ordinal";

                  previous_head_context:=pg_catalog.current_setting('tagekyc.raw_export_job_mutation_context',true);
                  previous_transition_context:=previous_head_context;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_job_mutation_context','job_head_assembly_sealed',true);
                  UPDATE tagekyc.raw_export_job_operational_heads SET "CurrentState"='AssemblySealed',"Revision"="Revision"+1,"LeaseOwnerId"=NULL,"LeaseExpiresAt"=NULL,"UpdatedAt"=now_ WHERE "JobId"=p_job_id;
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_job_mutation_context','job_transition',true);
                  INSERT INTO tagekyc.raw_export_job_transitions VALUES(pg_catalog.gen_random_uuid(),p_job_id,p_expected_job_revision+1,'AssemblySealed','Assembling','AssemblySealed',p_attempt_id,p_expected_fence,NULL,NULL,NULL,now_);
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_job_mutation_context',COALESCE(previous_head_context,''),true);
                  RETURN QUERY SELECT 'Sealed'::text,p_expected_job_revision+1,p_expected_preparation_revision+1,now_;
                EXCEPTION WHEN OTHERS THEN
                  PERFORM pg_catalog.set_config('tagekyc.raw_export_job_mutation_context',COALESCE(previous_head_context,''),true);
                  RAISE;
                END $$;

                CREATE FUNCTION tagekyc.raw_export_authorize_assembly_abort(p_c2_preparation_id uuid,p_expected_row_revision bigint,p_abort_authorization_digest bytea)
                RETURNS TABLE("Outcome" text,"RowRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE r tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE; now_ timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint; RETURN; END IF;
                  IF r."Disposition"='AbortAuthorized' AND r."AbortAuthorizationDigest"=p_abort_authorization_digest THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."RowRevision"; RETURN; END IF;
                  IF r."Disposition" NOT IN ('Preparing','Pending') OR r."RowRevision"<>p_expected_row_revision OR pg_catalog.octet_length(p_abort_authorization_digest)<>32 THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::bigint; RETURN; END IF;
                  now_:=pg_catalog.clock_timestamp(); UPDATE tagekyc.raw_export_assembly_preparation_dispositions AS d SET "Disposition"='AbortAuthorized',"AbortAuthorizationDigest"=p_abort_authorization_digest,"AbortAuthorizedAtUtc"=now_,"RowRevision"=d."RowRevision"+1 WHERE d."C2PreparationId"=p_c2_preparation_id;
                  RETURN QUERY SELECT 'AbortAuthorized'::text,p_expected_row_revision+1;
                END $$;

                CREATE FUNCTION tagekyc.raw_export_record_assembly_finalized(p_c2_preparation_id uuid,p_expected_row_revision bigint,p_assembly_fingerprint bytea)
                RETURNS TABLE("Outcome" text,"RowRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE r tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE; now_ timestamptz;
                BEGIN SELECT * INTO r FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint; RETURN; END IF;
                  IF r."Disposition"='Finalized' AND r."AssemblyFingerprint"=p_assembly_fingerprint THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."RowRevision"; RETURN; END IF;
                  IF r."Disposition"<>'SealCommitted' OR r."RowRevision"<>p_expected_row_revision OR r."AssemblyFingerprint"<>p_assembly_fingerprint THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::bigint; RETURN; END IF;
                  now_:=pg_catalog.clock_timestamp(); UPDATE tagekyc.raw_export_assembly_preparation_dispositions AS d SET "Disposition"='Finalized',"FinalizedAtUtc"=now_,"RowRevision"=d."RowRevision"+1 WHERE d."C2PreparationId"=p_c2_preparation_id; RETURN QUERY SELECT 'Finalized'::text,p_expected_row_revision+1;
                END $$;

                CREATE FUNCTION tagekyc.raw_export_record_assembly_aborted(p_c2_preparation_id uuid,p_expected_row_revision bigint,p_abort_authorization_digest bytea)
                RETURNS TABLE("Outcome" text,"RowRevision" bigint)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
                DECLARE r tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE; now_ timestamptz;
                BEGIN SELECT * INTO r FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFoundOrNotAllowed'::text,NULL::bigint; RETURN; END IF;
                  IF r."Disposition"='Aborted' AND r."AbortAuthorizationDigest"=p_abort_authorization_digest THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."RowRevision"; RETURN; END IF;
                  IF r."Disposition"<>'AbortAuthorized' OR r."RowRevision"<>p_expected_row_revision OR r."AbortAuthorizationDigest"<>p_abort_authorization_digest THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::bigint; RETURN; END IF;
                  now_:=pg_catalog.clock_timestamp(); UPDATE tagekyc.raw_export_assembly_preparation_dispositions AS d SET "Disposition"='Aborted',"AbortedAtUtc"=now_,"RowRevision"=d."RowRevision"+1 WHERE d."C2PreparationId"=p_c2_preparation_id; RETURN QUERY SELECT 'Aborted'::text,p_expected_row_revision+1;
                END $$;

                CREATE FUNCTION tagekyc.raw_export_read_assembly_recovery_context(p_c2_preparation_id uuid)
                RETURNS TABLE("C2PreparationId" uuid,"AssemblyId" uuid,"JobId" uuid,"AttemptId" uuid,"FencingToken" bigint,"AssemblyFingerprint" bytea,"PreparationFingerprint" bytea,"Disposition" text,"ProviderReceiptDigest" bytea,"AbortAuthorizationDigest" bytea,"RowRevision" bigint)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $$
                  SELECT "C2PreparationId","AssemblyId","JobId","AttemptId","FencingToken","AssemblyFingerprint","PreparationFingerprint","Disposition","ProviderReceiptDigest","AbortAuthorizationDigest","RowRevision" FROM tagekyc.raw_export_assembly_preparation_dispositions WHERE "C2PreparationId"=p_c2_preparation_id;
                $$;

                CREATE FUNCTION tagekyc.raw_export_read_committed_assembly_recovery_context(
                  p_job_id uuid,p_attempt_id uuid,p_expected_job_revision bigint,p_expected_fence bigint)
                RETURNS TABLE("C2PreparationId" uuid,"AssemblyId" uuid,"JobId" uuid,"AttemptId" uuid,"FencingToken" bigint,"AssemblyFingerprint" bytea,"PreparationFingerprint" bytea,"Disposition" text,"ProviderReceiptDigest" bytea,"AbortAuthorizationDigest" bytea,"RowRevision" bigint,"JobRevision" bigint)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $$
                  SELECT p."C2PreparationId",p."AssemblyId",p."JobId",p."AttemptId",p."FencingToken",
                         p."AssemblyFingerprint",p."PreparationFingerprint",p."Disposition",
                         p."ProviderReceiptDigest",p."AbortAuthorizationDigest",p."RowRevision",h."Revision"
                  FROM tagekyc.raw_export_assembly_preparation_dispositions p
                  JOIN tagekyc.raw_export_assembly_identities i
                    ON i."C2PreparationId"=p."C2PreparationId"
                   AND i."AssemblyId"=p."AssemblyId"
                   AND i."JobId"=p."JobId"
                   AND i."AttemptId"=p."AttemptId"
                   AND i."FencingToken"=p."FencingToken"
                   AND i."AssemblyFingerprint"=p."AssemblyFingerprint"
                  JOIN tagekyc.raw_export_job_operational_heads h
                    ON h."JobId"=p."JobId"
                  WHERE p."JobId"=p_job_id
                    AND p."AttemptId"=p_attempt_id
                    AND p."FencingToken"=p_expected_fence
                    AND p."Disposition" IN ('SealCommitted','Finalized')
                    AND h."CurrentState"='AssemblySealed'
                    AND h."CurrentAttemptId"=p_attempt_id
                    AND h."FencingToken"=p_expected_fence
                    AND h."Revision"=p_expected_job_revision+1
                    AND i."ItemCount"=(SELECT pg_catalog.count(*) FROM tagekyc.raw_export_assembly_items a WHERE a."AssemblyId"=i."AssemblyId")
                    AND EXISTS (
                      SELECT 1 FROM tagekyc.raw_export_job_transitions t
                      WHERE t."JobId"=p_job_id
                        AND t."ResultingRevision"=p_expected_job_revision+1
                        AND t."EventType"='AssemblySealed'
                        AND t."FromState"='Assembling'
                        AND t."ToState"='AssemblySealed'
                        AND t."AttemptId"=p_attempt_id
                        AND t."FencingToken"=p_expected_fence);
                $$;

                ALTER FUNCTION tagekyc.raw_export_freeze_job_source_bindings(uuid,uuid,bigint,bigint,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_job_source_verification_context(uuid,integer,uuid,bigint,bigint,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_register_assembly_preparing(uuid,uuid,uuid,bigint,bigint,bytea,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_assembly_pending(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_seal_authenticated_assembly(uuid,uuid,bigint,bigint,bigint,uuid,bytea,bytea,bytea,text,integer,bytea,bigint,integer,jsonb) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_authorize_assembly_abort(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_assembly_finalized(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_assembly_aborted(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_assembly_recovery_context(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                  tagekyc.raw_export_freeze_job_source_bindings(uuid,uuid,bigint,bigint,uuid),
                  tagekyc.raw_export_read_job_source_verification_context(uuid,integer,uuid,bigint,bigint,uuid),
                  tagekyc.raw_export_register_assembly_preparing(uuid,uuid,uuid,bigint,bigint,bytea,bytea),
                  tagekyc.raw_export_record_assembly_pending(uuid,bigint,bytea),
                  tagekyc.raw_export_seal_authenticated_assembly(uuid,uuid,bigint,bigint,bigint,uuid,bytea,bytea,bytea,text,integer,bytea,bigint,integer,jsonb),
                  tagekyc.raw_export_authorize_assembly_abort(uuid,bigint,bytea),
                  tagekyc.raw_export_record_assembly_finalized(uuid,bigint,bytea),
                  tagekyc.raw_export_record_assembly_aborted(uuid,bigint,bytea),
                  tagekyc.raw_export_read_assembly_recovery_context(uuid),
                  tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint)
                FROM PUBLIC,tagekyc_runtime,tagekyc_raw_export_assembly_resolver,tagekyc_raw_export_assembly_sealer;
                GRANT EXECUTE ON FUNCTION
                  tagekyc.raw_export_freeze_job_source_bindings(uuid,uuid,bigint,bigint,uuid),
                  tagekyc.raw_export_read_job_source_verification_context(uuid,integer,uuid,bigint,bigint,uuid)
                TO tagekyc_raw_export_assembly_resolver;
                GRANT EXECUTE ON FUNCTION
                  tagekyc.raw_export_register_assembly_preparing(uuid,uuid,uuid,bigint,bigint,bytea,bytea),
                  tagekyc.raw_export_record_assembly_pending(uuid,bigint,bytea),
                  tagekyc.raw_export_seal_authenticated_assembly(uuid,uuid,bigint,bigint,bigint,uuid,bytea,bytea,bytea,text,integer,bytea,bigint,integer,jsonb),
                  tagekyc.raw_export_authorize_assembly_abort(uuid,bigint,bytea),
                  tagekyc.raw_export_record_assembly_finalized(uuid,bigint,bytea),
                  tagekyc.raw_export_record_assembly_aborted(uuid,bigint,bytea),
                  tagekyc.raw_export_read_assembly_recovery_context(uuid),
                  tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint)
                TO tagekyc_raw_export_assembly_sealer;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_assembly_recovery_context(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_committed_assembly_recovery_context(uuid,uuid,bigint,bigint);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_assembly_aborted(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_assembly_finalized(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_authorize_assembly_abort(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_seal_authenticated_assembly(uuid,uuid,bigint,bigint,bigint,uuid,bytea,bytea,bytea,text,integer,bytea,bigint,integer,jsonb);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_assembly_pending(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_register_assembly_preparing(uuid,uuid,uuid,bigint,bigint,bytea,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_job_source_verification_context(uuid,integer,uuid,bigint,bigint,uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_freeze_job_source_bindings(uuid,uuid,bigint,bigint,uuid);

                ALTER TABLE tagekyc.raw_export_job_transitions DROP CONSTRAINT "CK_b4_job_transition_event_shape";
                ALTER TABLE tagekyc.raw_export_job_transitions ADD CONSTRAINT "CK_b4_job_transition_event_shape" CHECK (
                  ("EventType" = 'JobBound' AND "FromState" IS NULL AND "ToState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0 AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" IS NULL) OR
                  ("EventType" = 'LeaseAcquired' AND "FromState" = 'Claimed' AND "ToState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NOT NULL AND "ResultingLeaseExpiresAt" IS NOT NULL AND "FailureCode" IS NULL) OR
                  ("EventType" IN ('LeaseRenewed','LeaseAcquiredAfterRetryableFailure','LeaseReclaimed') AND "FromState" = 'Assembling' AND "ToState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NOT NULL AND "ResultingLeaseExpiresAt" IS NOT NULL AND "FailureCode" IS NULL) OR
                  ("EventType" = 'AttemptFailedRetryable' AND "FromState" = 'Assembling' AND "ToState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1 AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" = 'ATTEMPT_EXECUTION_FAILED_RETRYABLE') OR
                  ("EventType" = 'JobTerminalFailed' AND (("FromState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0) OR ("FromState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1)) AND "ToState" = 'TerminalFailed' AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" IN ('AUTHORITY_REVALIDATION_FAILED','ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE','JOB_GRAPH_INVARIANT_FAILURE','MODE_RETRY_NOT_AUTHORIZED')) OR
                  ("EventType" = 'JobCancelled' AND (("FromState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0) OR ("FromState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1)) AND "ToState" = 'Cancelled' AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" = 'REQUEST_CANCELLED') OR
                  ("EventType" = 'JobExpired' AND (("FromState" = 'Claimed' AND "AttemptId" IS NULL AND "FencingToken" = 0) OR ("FromState" = 'Assembling' AND "AttemptId" IS NOT NULL AND "FencingToken" >= 1)) AND "ToState" = 'Expired' AND "ResultingLeaseOwnerId" IS NULL AND "ResultingLeaseExpiresAt" IS NULL AND "FailureCode" = 'PERMIT_OR_JOB_EXPIRED'));

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_job_transition_insert()
                RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
                DECLARE h tagekyc.raw_export_job_operational_heads%ROWTYPE; previous_revision bigint; previous_state text;
                BEGIN
                  IF TG_OP <> 'INSERT' OR current_user <> 'tagekyc_raw_export_deployer' OR current_setting('tagekyc.raw_export_job_mutation_context',true) IS DISTINCT FROM 'job_transition' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSITION_MUTATION_UNSUPPORTED'; END IF;
                  SELECT * INTO h FROM tagekyc.raw_export_job_operational_heads WHERE "JobId"=NEW."JobId" FOR SHARE;
                  SELECT "ResultingRevision","ToState" INTO previous_revision,previous_state FROM tagekyc.raw_export_job_transitions WHERE "JobId"=NEW."JobId" ORDER BY "ResultingRevision" DESC LIMIT 1;
                  IF h."JobId" IS NULL OR NEW."ResultingRevision"<>h."Revision" OR NEW."ToState"<>h."CurrentState" OR NEW."AttemptId" IS DISTINCT FROM h."CurrentAttemptId" OR NEW."FencingToken"<>h."FencingToken" OR NEW."ResultingLeaseOwnerId" IS DISTINCT FROM h."LeaseOwnerId" OR NEW."ResultingLeaseExpiresAt" IS DISTINCT FROM h."LeaseExpiresAt" OR NEW."OccurredAt"<>h."UpdatedAt" OR (NEW."ResultingRevision"=0 AND (previous_revision IS NOT NULL OR NEW."FromState" IS NOT NULL)) OR (NEW."ResultingRevision">0 AND (previous_revision IS NULL OR previous_revision<>NEW."ResultingRevision"-1 OR previous_state IS DISTINCT FROM NEW."FromState")) THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_TRANSITION_GRAPH_INVALID'; END IF;
                  RETURN NEW;
                END $$;

                CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_job_head_mutation()
                RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
                DECLARE c text; px xid;
                BEGIN
                  c := current_setting('tagekyc.raw_export_job_mutation_context',true);
                  IF current_user <> 'tagekyc_raw_export_deployer' OR c IS NULL OR c NOT IN ('job_head_insert','job_head_acquire','job_head_renew','job_head_release','job_head_terminal') OR TG_OP = 'DELETE' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED'; END IF;
                  IF TG_OP='INSERT' THEN
                    SELECT xmin INTO px FROM tagekyc.raw_export_job_identities WHERE "JobId"=NEW."JobId";
                    IF c<>'job_head_insert' OR NOT FOUND OR px<>pg_current_xact_id()::xid OR NEW."CurrentState"<>'Claimed' OR NEW."Revision"<>0 OR NEW."CurrentAttemptId" IS NOT NULL OR NEW."LeaseOwnerId" IS NOT NULL OR NEW."LeaseExpiresAt" IS NOT NULL OR NEW."FencingToken"<>0 THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID'; END IF;
                  ELSIF NEW."JobId"<>OLD."JobId" OR NEW."Revision"<>OLD."Revision"+1 OR NEW."UpdatedAt"<OLD."UpdatedAt" THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_acquire' AND NOT (OLD."CurrentState" IN ('Claimed','Assembling') AND NEW."CurrentState"='Assembling' AND NEW."CurrentAttemptId" IS NOT NULL AND NEW."CurrentAttemptId" IS DISTINCT FROM OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken"+1 AND NEW."LeaseOwnerId" IS NOT NULL AND NEW."LeaseExpiresAt">NEW."UpdatedAt") THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_renew' AND NOT (OLD."CurrentState"='Assembling' AND NEW."CurrentState"='Assembling' AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId"=OLD."LeaseOwnerId" AND NEW."LeaseOwnerId" IS NOT NULL AND NEW."LeaseExpiresAt">NEW."UpdatedAt") THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_release' AND NOT (OLD."CurrentState"='Assembling' AND NEW."CurrentState"='Assembling' AND NEW."CurrentAttemptId"=OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_terminal' AND NOT (OLD."CurrentState" IN ('Claimed','Assembling') AND NEW."CurrentState" IN ('TerminalFailed','Cancelled','Expired') AND NEW."CurrentAttemptId" IS NOT DISTINCT FROM OLD."CurrentAttemptId" AND NEW."FencingToken"=OLD."FencingToken" AND NEW."LeaseOwnerId" IS NULL AND NEW."LeaseExpiresAt" IS NULL) THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_GRAPH_INVALID';
                  ELSIF c='job_head_insert' THEN RAISE EXCEPTION 'RAW_EXPORT_JOB_HEAD_MUTATION_UNSUPPORTED'; END IF;
                  RETURN NEW;
                END $$;
                ALTER FUNCTION tagekyc.enforce_raw_export_job_transition_insert() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.enforce_raw_export_job_head_mutation() OWNER TO tagekyc_raw_export_deployer;

                REVOKE USAGE ON SCHEMA tagekyc FROM
                  tagekyc_raw_export_assembly_resolver,
                  tagekyc_raw_export_assembly_sealer;
                REVOKE tagekyc_raw_export_assembly_resolver FROM tagekyc_raw_export_assembly_resolver_login;
                REVOKE tagekyc_raw_export_assembly_sealer FROM tagekyc_raw_export_assembly_sealer_login;
                DROP ROLE IF EXISTS tagekyc_raw_export_assembly_resolver;
                DROP ROLE IF EXISTS tagekyc_raw_export_assembly_sealer;
                """);

            migrationBuilder.DropTable(
                name: "raw_export_assembly_items",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_job_source_bindings",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_assembly_identities",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_assembly_preparation_dispositions",
                schema: "tagekyc");
        }
    }
}
