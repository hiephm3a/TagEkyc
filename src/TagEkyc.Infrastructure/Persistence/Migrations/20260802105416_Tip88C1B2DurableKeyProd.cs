using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2DurableKeyProd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $dkprod_prerequisite$
                DECLARE
                    role_name text;
                    login_name text;
                    role_row record;
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_extension e
                        JOIN pg_catalog.pg_namespace n ON n.oid = e.extnamespace
                        JOIN pg_catalog.pg_proc p
                          ON p.pronamespace = n.oid
                         AND p.proname = 'gen_random_bytes'
                         AND p.pronargs = 1
                        WHERE e.extname = 'pgcrypto'
                          AND n.nspname = 'tagekyc_extensions'
                          AND p.oid = 'tagekyc_extensions.gen_random_bytes(integer)'::pg_catalog.regprocedure
                          AND pg_catalog.pg_get_function_result(p.oid) = 'bytea') THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE';
                    END IF;

                    FOREACH login_name IN ARRAY ARRAY[
                        'tagekyc_raw_export_encryptor_login',
                        'tagekyc_raw_export_reconciler_login',
                        'tagekyc_raw_export_lifecycle_login']
                    LOOP
                        SELECT * INTO role_row
                        FROM pg_catalog.pg_roles
                        WHERE rolname = login_name;
                        IF NOT FOUND OR NOT role_row.rolcanlogin OR role_row.rolsuper
                           OR role_row.rolcreatedb OR role_row.rolcreaterole
                           OR role_row.rolreplication OR role_row.rolbypassrls
                           OR NOT role_row.rolinherit THEN
                            RAISE EXCEPTION USING
                                ERRCODE = 'P0001',
                                MESSAGE = 'PROD_RAW_EXPORT_CUSTODY_LOGIN_ATTRIBUTE_INVALID';
                        END IF;
                    END LOOP;

                    FOREACH role_name IN ARRAY ARRAY[
                        'tagekyc_raw_export_custody_encryptor',
                        'tagekyc_raw_export_reconciler',
                        'tagekyc_raw_export_lifecycle']
                    LOOP
                        SELECT * INTO role_row FROM pg_catalog.pg_roles
                        WHERE rolname = role_name;
                        IF FOUND THEN
                            IF role_row.rolcanlogin OR role_row.rolsuper
                               OR role_row.rolcreatedb OR role_row.rolcreaterole
                               OR role_row.rolreplication OR role_row.rolbypassrls
                               OR NOT role_row.rolinherit THEN
                                RAISE EXCEPTION USING
                                    ERRCODE = 'P0001',
                                    MESSAGE = 'PROD_RAW_EXPORT_CUSTODY_ROLE_ATTRIBUTE_INVALID';
                            END IF;
                        ELSE
                            EXECUTE pg_catalog.format(
                                'CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',
                                role_name);
                        END IF;
                    END LOOP;
                END
                $dkprod_prerequisite$;

                GRANT tagekyc_raw_export_custody_encryptor
                  TO tagekyc_raw_export_encryptor_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                GRANT tagekyc_raw_export_reconciler
                  TO tagekyc_raw_export_reconciler_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                GRANT tagekyc_raw_export_lifecycle
                  TO tagekyc_raw_export_lifecycle_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "uq_raw_export_enc_attempt_attemptid_keyresvid",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts",
                columns: new[] { "AttemptId", "AttemptKeyReservationId" });

            migrationBuilder.CreateTable(
                name: "raw_export_attempt_key_reservations",
                schema: "tagekyc",
                columns: table => new
                {
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptionAttemptFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyProviderId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    KekId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    KekVersion = table.Column<int>(type: "integer", nullable: false),
                    KekFingerprint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AttemptKeyContextFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    WrappingSuiteId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    WrappingSuiteVersion = table.Column<int>(type: "integer", nullable: false),
                    PreparationDisposition = table.Column<string>(type: "text", nullable: false),
                    CurrentPreparationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentPreparationFence = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CurrentPreparationLeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentProviderOperationToken = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: true),
                    ResolutionAttemptCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    NextResolutionAttemptNotBeforeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionDeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CleanupAttemptCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    NextCleanupAttemptNotBeforeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CleanupDeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CleanupOperatorInterventionRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    WrappedDekCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekTag = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekMetadataDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    RowRevision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReasonCode = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_attempt_key_reservations", x => x.AttemptKeyReservationId);
                    table.CheckConstraint("ck_raw_export_attempt_key_reservation_sparse", "CASE\n  WHEN \"PreparationDisposition\" IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown')\n    THEN \"CurrentPreparationId\" IS NOT NULL AND \"CurrentPreparationLeaseExpiresAtUtc\" IS NOT NULL AND \"CurrentProviderOperationToken\" IS NOT NULL\n  WHEN \"PreparationDisposition\" IN ('Active','Revoked')\n    THEN \"CurrentPreparationId\" IS NOT NULL AND \"CurrentProviderOperationToken\" IS NOT NULL AND \"PreparedAtUtc\" IS NOT NULL AND \"WrappedDekCiphertext\" IS NOT NULL\n  WHEN \"PreparationDisposition\" = 'ProviderCleanupRequired'\n    THEN \"CurrentPreparationId\" IS NOT NULL AND \"CurrentProviderOperationToken\" IS NOT NULL AND \"CleanupDeadlineUtc\" IS NOT NULL\n  WHEN \"PreparationDisposition\" = 'ReadyForFreshPreparation'\n    THEN \"CurrentPreparationId\" IS NULL AND \"CurrentPreparationLeaseExpiresAtUtc\" IS NULL AND \"CurrentProviderOperationToken\" IS NULL\n  WHEN \"PreparationDisposition\" IN ('AbandonRequested','ReservationAbandoned','ProviderCorruptOrUnverifiable')\n    THEN \"CurrentPreparationId\" IS NOT NULL AND \"CurrentProviderOperationToken\" IS NOT NULL\n  ELSE FALSE\nEND");
                    table.CheckConstraint("ck_raw_export_attempt_key_reservation_values", "octet_length(\"EncryptionAttemptFingerprint\") = 32\nAND octet_length(\"AttemptKeyContextFingerprint\") = 32\nAND \"KekVersion\" >= 1\nAND \"WrappingSuiteId\" = 'AES-256-GCM'\nAND \"WrappingSuiteVersion\" = 1\nAND \"CurrentPreparationFence\" >= 1\nAND \"ResolutionAttemptCount\" >= 0\nAND \"CleanupAttemptCount\" >= 0\nAND \"RowRevision\" >= 1\nAND \"PreparationDisposition\" IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown','ProviderCorruptOrUnverifiable','ProviderCleanupRequired','ReadyForFreshPreparation','Active','Revoked','AbandonRequested','ReservationAbandoned')\nAND ((\"WrappedDekCiphertext\" IS NULL AND \"WrappedDekNonce\" IS NULL AND \"WrappedDekTag\" IS NULL AND \"WrappedDekMetadataDigest\" IS NULL)\n  OR (octet_length(\"WrappedDekCiphertext\") = 32 AND octet_length(\"WrappedDekNonce\") = 12 AND octet_length(\"WrappedDekTag\") = 16 AND octet_length(\"WrappedDekMetadataDigest\") = 32))\nAND (\"CurrentProviderOperationToken\" IS NULL OR \"CurrentProviderOperationToken\" ~ '^[A-Za-z0-9_-]{43}$')");
                    table.ForeignKey(
                        name: "fk_raw_export_attempt_key_resv_attempt_composite",
                        columns: x => new { x.AttemptId, x.AttemptKeyReservationId },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_encryption_attempts",
                        principalColumns: new[] { "AttemptId", "AttemptKeyReservationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_attempt_key_preparation_events",
                schema: "tagekyc",
                columns: table => new
                {
                    PreparationEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparationFence = table.Column<long>(type: "bigint", nullable: false),
                    EventSequence = table.Column<long>(type: "bigint", nullable: false),
                    EventKind = table.Column<string>(type: "text", nullable: false),
                    ResolutionKind = table.Column<string>(type: "text", nullable: true),
                    CleanupResultKind = table.Column<string>(type: "text", nullable: true),
                    ProviderOperationToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderOperationReceipt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderCleanupReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderCleanupReceipt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderResolutionEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    ProviderCleanupEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    CleanupObservationEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekMetadataDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappingSuiteId = table.Column<string>(type: "text", nullable: true),
                    WrappingSuiteVersion = table.Column<int>(type: "integer", nullable: true),
                    RevocationReasonCode = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RevocationEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    OperatorReasonCode = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RequestingActorEvidence = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FinalizingActorEvidence = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AbandonRequestPreparationEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    AbandonmentEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    HeadRowRevision = table.Column<long>(type: "bigint", nullable: true),
                    EventAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_attempt_key_preparation_events", x => x.PreparationEventId);
                    table.CheckConstraint("ck_raw_export_attempt_key_event_values", "\"EventSequence\" >= 1\nAND \"EventKind\" IN ('Opened','Expired','DirectActivated','RecoveredActivated','ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ResolvedCleanupRequired','ProviderUnavailableObserved','CleanupAttemptObserved','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked')\nAND (\"ProviderResolutionEvidenceDigest\" IS NULL OR octet_length(\"ProviderResolutionEvidenceDigest\")=32)\nAND (\"ProviderCleanupEvidenceDigest\" IS NULL OR octet_length(\"ProviderCleanupEvidenceDigest\")=32)\nAND (\"CleanupObservationEvidenceDigest\" IS NULL OR octet_length(\"CleanupObservationEvidenceDigest\")=32)\nAND (\"WrappedDekMetadataDigest\" IS NULL OR octet_length(\"WrappedDekMetadataDigest\")=32)\nAND (\"RevocationEvidenceDigest\" IS NULL OR octet_length(\"RevocationEvidenceDigest\")=32)\nAND (\"AbandonmentEvidenceDigest\" IS NULL OR octet_length(\"AbandonmentEvidenceDigest\")=32)\nAND CASE\n  WHEN \"EventKind\"='Opened' THEN \"ProviderOperationToken\" IS NOT NULL AND \"WrappingSuiteId\"='AES-256-GCM' AND \"WrappingSuiteVersion\"=1\n    AND \"ResolutionKind\" IS NULL AND \"CleanupResultKind\" IS NULL AND \"ProviderOperationReceipt\" IS NULL AND \"ProviderCleanupReference\" IS NULL AND \"ProviderCleanupReceipt\" IS NULL AND \"ProviderResolutionEvidenceDigest\" IS NULL AND \"ProviderCleanupEvidenceDigest\" IS NULL AND \"CleanupObservationEvidenceDigest\" IS NULL AND \"WrappedDekMetadataDigest\" IS NULL AND \"RevocationReasonCode\" IS NULL AND \"RevocationEvidenceDigest\" IS NULL AND \"OperatorReasonCode\" IS NULL AND \"RequestingActorEvidence\" IS NULL AND \"FinalizingActorEvidence\" IS NULL AND \"AbandonRequestPreparationEventId\" IS NULL AND \"AbandonmentEvidenceDigest\" IS NULL AND \"HeadRowRevision\" IS NULL\n  WHEN \"EventKind\"='Expired' THEN \"ResolutionKind\" IS NULL AND \"CleanupResultKind\" IS NULL AND \"ProviderOperationToken\" IS NULL AND \"ProviderOperationReceipt\" IS NULL AND \"ProviderCleanupReference\" IS NULL AND \"ProviderCleanupReceipt\" IS NULL AND \"ProviderResolutionEvidenceDigest\" IS NULL AND \"ProviderCleanupEvidenceDigest\" IS NULL AND \"CleanupObservationEvidenceDigest\" IS NULL AND \"WrappedDekMetadataDigest\" IS NULL AND \"WrappingSuiteId\" IS NULL AND \"WrappingSuiteVersion\" IS NULL AND \"RevocationReasonCode\" IS NULL AND \"RevocationEvidenceDigest\" IS NULL AND \"OperatorReasonCode\" IS NULL AND \"RequestingActorEvidence\" IS NULL AND \"FinalizingActorEvidence\" IS NULL AND \"AbandonRequestPreparationEventId\" IS NULL AND \"AbandonmentEvidenceDigest\" IS NULL AND \"HeadRowRevision\" IS NULL\n  WHEN \"EventKind\"='DirectActivated' THEN \"ProviderOperationReceipt\" IS NOT NULL AND \"WrappedDekMetadataDigest\" IS NOT NULL AND \"WrappingSuiteId\"='AES-256-GCM' AND \"WrappingSuiteVersion\"=1 AND \"ResolutionKind\" IS NULL AND \"ProviderResolutionEvidenceDigest\" IS NULL\n  WHEN \"EventKind\"='RecoveredActivated' THEN \"ProviderOperationReceipt\" IS NOT NULL AND \"WrappedDekMetadataDigest\" IS NOT NULL AND \"WrappingSuiteId\"='AES-256-GCM' AND \"WrappingSuiteVersion\"=1 AND \"ResolutionKind\"='WrappedResultRecovered' AND \"ProviderResolutionEvidenceDigest\" IS NOT NULL\n  WHEN \"EventKind\" IN ('ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ProviderUnavailableObserved') THEN \"ResolutionKind\" IS NOT NULL AND \"ProviderResolutionEvidenceDigest\" IS NOT NULL AND \"CleanupResultKind\" IS NULL AND \"ProviderCleanupEvidenceDigest\" IS NULL AND \"CleanupObservationEvidenceDigest\" IS NULL\n  WHEN \"EventKind\"='ResolvedCleanupRequired' THEN \"ResolutionKind\"='ProviderResourceCleanupRequired' AND \"ProviderResolutionEvidenceDigest\" IS NOT NULL AND \"ProviderCleanupReference\" IS NOT NULL\n  WHEN \"EventKind\"='CleanupAttemptObserved' THEN \"CleanupResultKind\" IS NOT NULL AND \"ProviderCleanupReference\" IS NOT NULL AND \"CleanupObservationEvidenceDigest\" IS NOT NULL\n  WHEN \"EventKind\"='CleanupAcknowledged' THEN \"CleanupResultKind\" IN ('Cleaned','AlreadyAbsent') AND \"ProviderCleanupReference\" IS NOT NULL AND \"ProviderCleanupReceipt\" IS NOT NULL AND \"ProviderCleanupEvidenceDigest\" IS NOT NULL\n  WHEN \"EventKind\"='AbandonRequested' THEN \"ProviderOperationToken\" IS NOT NULL AND \"OperatorReasonCode\" IS NOT NULL AND \"RequestingActorEvidence\" IS NOT NULL AND \"FinalizingActorEvidence\" IS NULL AND \"AbandonRequestPreparationEventId\" IS NULL AND \"AbandonmentEvidenceDigest\" IS NULL AND \"HeadRowRevision\" IS NOT NULL\n  WHEN \"EventKind\"='ProviderOperationAbandoned' THEN \"ProviderOperationToken\" IS NOT NULL AND \"OperatorReasonCode\" IS NOT NULL AND \"RequestingActorEvidence\" IS NOT NULL AND \"FinalizingActorEvidence\" IS NOT NULL AND \"AbandonRequestPreparationEventId\" IS NOT NULL AND \"AbandonmentEvidenceDigest\" IS NOT NULL AND \"HeadRowRevision\" IS NOT NULL\n  WHEN \"EventKind\"='Revoked' THEN \"RevocationReasonCode\" IS NOT NULL AND \"RevocationEvidenceDigest\" IS NOT NULL\n  ELSE FALSE\nEND");
                    table.ForeignKey(
                        name: "fk_raw_export_abandon_request_event",
                        column: x => x.AbandonRequestPreparationEventId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_attempt_key_preparation_events",
                        principalColumn: "PreparationEventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_attempt_key_event_reservation",
                        column: x => x.AttemptKeyReservationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_attempt_key_reservations",
                        principalColumn: "AttemptKeyReservationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_key_provider_operations",
                schema: "tagekyc",
                columns: table => new
                {
                    ProviderOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyProviderId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProviderOperationToken = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparationFence = table.Column<long>(type: "bigint", nullable: false),
                    AttemptKeyContextFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ProviderOperationState = table.Column<string>(type: "text", nullable: false),
                    WrappedDekCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekTag = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappedDekMetadataDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    WrappingSuiteId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    WrappingSuiteVersion = table.Column<int>(type: "integer", nullable: true),
                    ProviderResourceReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderOperationReceipt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ResultObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderCleanupReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderCleanupReceipt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ProviderAbsenceProofReceipt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_key_provider_operations", x => x.ProviderOperationId);
                    table.CheckConstraint("ck_raw_export_key_provider_operation_sparse", "CASE \"ProviderOperationState\"\n  WHEN 'Issued' THEN \"WrappedDekCiphertext\" IS NULL AND \"ProviderCleanupReference\" IS NULL AND \"ProviderAbsenceProofReceipt\" IS NULL\n  WHEN 'ResultObserved' THEN octet_length(\"WrappedDekCiphertext\")=32 AND octet_length(\"WrappedDekNonce\")=12 AND octet_length(\"WrappedDekTag\")=16 AND octet_length(\"WrappedDekMetadataDigest\")=32 AND \"WrappingSuiteId\" IS NOT NULL AND \"WrappingSuiteVersion\" IS NOT NULL AND \"ProviderResourceReference\" IS NOT NULL AND \"ProviderOperationReceipt\" IS NOT NULL AND \"ResultObservedAtUtc\" IS NOT NULL AND \"ProviderCleanupReference\" IS NULL AND \"ProviderAbsenceProofReceipt\" IS NULL\n  WHEN 'CleanupRequired' THEN \"ProviderCleanupReference\" IS NOT NULL AND \"ProviderCleanupReceipt\" IS NULL AND \"ProviderAbsenceProofReceipt\" IS NULL\n  WHEN 'CleanedUp' THEN \"ProviderCleanupReference\" IS NOT NULL AND \"ProviderCleanupReceipt\" IS NOT NULL AND \"ProviderAbsenceProofReceipt\" IS NULL\n  WHEN 'AbsenceProven' THEN \"ProviderAbsenceProofReceipt\" IS NOT NULL AND \"WrappedDekCiphertext\" IS NULL AND \"ProviderCleanupReference\" IS NULL\n  ELSE FALSE\nEND");
                    table.CheckConstraint("ck_raw_export_key_provider_operation_values", "\"ProviderOperationState\" IN ('Issued','ResultObserved','CleanupRequired','CleanedUp','AbsenceProven')\nAND \"PreparationFence\" >= 1\nAND octet_length(\"AttemptKeyContextFingerprint\") = 32\nAND \"ProviderOperationToken\" ~ '^[A-Za-z0-9_-]{43}$'");
                    table.ForeignKey(
                        name: "fk_raw_export_key_provider_op_reservation",
                        column: x => x.AttemptKeyReservationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_attempt_key_reservations",
                        principalColumn: "AttemptKeyReservationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_attempt_key_event_abandon_req",
                schema: "tagekyc",
                table: "raw_export_attempt_key_preparation_events",
                column: "AbandonRequestPreparationEventId");

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_attempt_key_event_sequence",
                schema: "tagekyc",
                table: "raw_export_attempt_key_preparation_events",
                columns: new[] { "AttemptKeyReservationId", "EventSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_attempt_key_event_singleton",
                schema: "tagekyc",
                table: "raw_export_attempt_key_preparation_events",
                columns: new[] { "AttemptKeyReservationId", "PreparationId", "PreparationFence", "EventKind" },
                unique: true,
                filter: "\"EventKind\" IN ('Opened','Expired','DirectActivated','RecoveredActivated','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked')");

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_attempt_key_resv_attempt",
                schema: "tagekyc",
                table: "raw_export_attempt_key_reservations",
                columns: new[] { "AttemptId", "AttemptKeyReservationId" });

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_key_provider_op_provider_token",
                schema: "tagekyc",
                table: "raw_export_key_provider_operations",
                columns: new[] { "KeyProviderId", "ProviderOperationToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_key_provider_op_reservation_fence",
                schema: "tagekyc",
                table: "raw_export_key_provider_operations",
                columns: new[] { "AttemptKeyReservationId", "PreparationFence" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE tagekyc.raw_export_attempt_key_reservations
                    DROP CONSTRAINT ck_raw_export_attempt_key_reservation_sparse;
                ALTER TABLE tagekyc.raw_export_attempt_key_reservations
                    ADD CONSTRAINT ck_raw_export_attempt_key_reservation_sparse CHECK (
                    CASE
                      WHEN "PreparationDisposition"='PreparingLive' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='PreparingExpiredAwaitingResolution' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "ResolutionDeadlineUtc" IS NOT NULL AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='ProviderOutcomeUnknown' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "ResolutionDeadlineUtc" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NOT NULL AND "ResolutionAttemptCount">0 AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='Active' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekCiphertext" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='Revoked' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekCiphertext" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NOT NULL AND "RevocationReasonCode" IS NOT NULL
                      WHEN "PreparationDisposition"='ProviderCleanupRequired' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "CleanupDeadlineUtc" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='ReadyForFreshPreparation' THEN "CurrentPreparationId" IS NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NULL AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND NOT "CleanupOperatorInterventionRequired" AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='ProviderCorruptOrUnverifiable' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='AbandonRequested' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      WHEN "PreparationDisposition"='ReservationAbandoned' THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND NOT "CleanupOperatorInterventionRequired" AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                      ELSE FALSE END);

                ALTER TABLE tagekyc.raw_export_key_provider_operations
                    DROP CONSTRAINT ck_raw_export_key_provider_operation_sparse;
                ALTER TABLE tagekyc.raw_export_key_provider_operations
                    ADD CONSTRAINT ck_raw_export_key_provider_operation_sparse CHECK (
                    CASE "ProviderOperationState"
                      WHEN 'Issued' THEN "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                      WHEN 'ResultObserved' THEN octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                      WHEN 'CleanupRequired' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL) OR (octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                      WHEN 'CleanedUp' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL) OR (octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                      WHEN 'AbsenceProven' THEN "ProviderAbsenceProofReceipt" IS NOT NULL AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                      ELSE FALSE END);

                ALTER TABLE tagekyc.raw_export_attempt_key_reservations
                    ADD CONSTRAINT ck_raw_export_attempt_key_reservation_text CHECK (
                      "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                        AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                      AND "KekId"=btrim("KekId") AND "KekId"=normalize("KekId",NFC)
                        AND octet_length("KekId") BETWEEN 1 AND 512 AND "KekId" !~ '[\x00-\x1f\x7f]'
                      AND "KekFingerprint"=btrim("KekFingerprint") AND "KekFingerprint"=normalize("KekFingerprint",NFC)
                        AND octet_length("KekFingerprint") BETWEEN 1 AND 512 AND "KekFingerprint" !~ '[\x00-\x1f\x7f]'
                      AND "WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC)
                        AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'
                      AND ("RevocationReasonCode" IS NULL OR (
                        "RevocationReasonCode"=btrim("RevocationReasonCode") AND "RevocationReasonCode"=normalize("RevocationReasonCode",NFC)
                        AND octet_length("RevocationReasonCode") BETWEEN 1 AND 512 AND "RevocationReasonCode" !~ '[\x00-\x1f\x7f]')));

                ALTER TABLE tagekyc.raw_export_key_provider_operations
                    ADD CONSTRAINT ck_raw_export_key_provider_operation_text CHECK (
                      "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                        AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                      AND ("WrappingSuiteId" IS NULL OR ("WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC) AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderResourceReference" IS NULL OR ("ProviderResourceReference"=btrim("ProviderResourceReference") AND "ProviderResourceReference"=normalize("ProviderResourceReference",NFC) AND octet_length("ProviderResourceReference") BETWEEN 1 AND 512 AND "ProviderResourceReference" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderOperationReceipt" IS NULL OR ("ProviderOperationReceipt"=btrim("ProviderOperationReceipt") AND "ProviderOperationReceipt"=normalize("ProviderOperationReceipt",NFC) AND octet_length("ProviderOperationReceipt") BETWEEN 1 AND 512 AND "ProviderOperationReceipt" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderCleanupReference" IS NULL OR ("ProviderCleanupReference"=btrim("ProviderCleanupReference") AND "ProviderCleanupReference"=normalize("ProviderCleanupReference",NFC) AND octet_length("ProviderCleanupReference") BETWEEN 1 AND 512 AND "ProviderCleanupReference" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderCleanupReceipt" IS NULL OR ("ProviderCleanupReceipt"=btrim("ProviderCleanupReceipt") AND "ProviderCleanupReceipt"=normalize("ProviderCleanupReceipt",NFC) AND octet_length("ProviderCleanupReceipt") BETWEEN 1 AND 512 AND "ProviderCleanupReceipt" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderAbsenceProofReceipt" IS NULL OR ("ProviderAbsenceProofReceipt"=btrim("ProviderAbsenceProofReceipt") AND "ProviderAbsenceProofReceipt"=normalize("ProviderAbsenceProofReceipt",NFC) AND octet_length("ProviderAbsenceProofReceipt") BETWEEN 1 AND 512 AND "ProviderAbsenceProofReceipt" !~ '[\x00-\x1f\x7f]')));

                ALTER TABLE tagekyc.raw_export_attempt_key_preparation_events
                    ADD CONSTRAINT ck_raw_export_attempt_key_event_text CHECK (
                      ("ProviderOperationToken" IS NULL OR "ProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$')
                      AND ("ProviderOperationReceipt" IS NULL OR ("ProviderOperationReceipt"=btrim("ProviderOperationReceipt") AND "ProviderOperationReceipt"=normalize("ProviderOperationReceipt",NFC) AND octet_length("ProviderOperationReceipt") BETWEEN 1 AND 512 AND "ProviderOperationReceipt" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderCleanupReference" IS NULL OR ("ProviderCleanupReference"=btrim("ProviderCleanupReference") AND "ProviderCleanupReference"=normalize("ProviderCleanupReference",NFC) AND octet_length("ProviderCleanupReference") BETWEEN 1 AND 512 AND "ProviderCleanupReference" !~ '[\x00-\x1f\x7f]'))
                      AND ("ProviderCleanupReceipt" IS NULL OR ("ProviderCleanupReceipt"=btrim("ProviderCleanupReceipt") AND "ProviderCleanupReceipt"=normalize("ProviderCleanupReceipt",NFC) AND octet_length("ProviderCleanupReceipt") BETWEEN 1 AND 512 AND "ProviderCleanupReceipt" !~ '[\x00-\x1f\x7f]'))
                      AND ("RevocationReasonCode" IS NULL OR ("RevocationReasonCode"=btrim("RevocationReasonCode") AND "RevocationReasonCode"=normalize("RevocationReasonCode",NFC) AND octet_length("RevocationReasonCode") BETWEEN 1 AND 512 AND "RevocationReasonCode" !~ '[\x00-\x1f\x7f]'))
                      AND ("OperatorReasonCode" IS NULL OR ("OperatorReasonCode"=btrim("OperatorReasonCode") AND "OperatorReasonCode"=normalize("OperatorReasonCode",NFC) AND octet_length("OperatorReasonCode") BETWEEN 1 AND 512 AND "OperatorReasonCode" !~ '[\x00-\x1f\x7f]'))
                      AND ("RequestingActorEvidence" IS NULL OR ("RequestingActorEvidence"=btrim("RequestingActorEvidence") AND "RequestingActorEvidence"=normalize("RequestingActorEvidence",NFC) AND octet_length("RequestingActorEvidence") BETWEEN 1 AND 512 AND "RequestingActorEvidence" !~ '[\x00-\x1f\x7f]'))
                      AND ("FinalizingActorEvidence" IS NULL OR ("FinalizingActorEvidence"=btrim("FinalizingActorEvidence") AND "FinalizingActorEvidence"=normalize("FinalizingActorEvidence",NFC) AND octet_length("FinalizingActorEvidence") BETWEEN 1 AND 512 AND "FinalizingActorEvidence" !~ '[\x00-\x1f\x7f]')));

                ALTER TABLE tagekyc.raw_export_attempt_key_reservations
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_key_provider_operations
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_attempt_key_preparation_events
                    OWNER TO tagekyc_raw_export_deployer;

                CREATE FUNCTION tagekyc.raw_export_attempt_key_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $guard$
                DECLARE
                    write_context text := pg_catalog.current_setting(
                        'tagekyc.raw_export_attempt_key_write_context', true);
                BEGIN
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR write_context IS DISTINCT FROM 'active' THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_KEY_WRITE_CONTEXT_INVALID';
                    END IF;

                    IF TG_TABLE_NAME = 'raw_export_attempt_key_preparation_events' THEN
                        IF TG_OP <> 'INSERT' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',
                                MESSAGE='RAW_EXPORT_ATTEMPT_KEY_HISTORY_APPEND_ONLY';
                        END IF;
                        RETURN NEW;
                    ELSIF TG_TABLE_NAME = 'raw_export_attempt_key_reservations' THEN
                        IF TG_OP = 'DELETE' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',
                                MESSAGE='RAW_EXPORT_ATTEMPT_KEY_RESERVATION_DELETE_FORBIDDEN';
                        END IF;
                        IF TG_OP = 'UPDATE' AND (
                            NEW."AttemptKeyReservationId" IS DISTINCT FROM OLD."AttemptKeyReservationId"
                            OR NEW."AttemptId" IS DISTINCT FROM OLD."AttemptId"
                            OR NEW."EncryptionAttemptFingerprint" IS DISTINCT FROM OLD."EncryptionAttemptFingerprint"
                            OR NEW."KeyProviderId" IS DISTINCT FROM OLD."KeyProviderId"
                            OR NEW."KekId" IS DISTINCT FROM OLD."KekId"
                            OR NEW."KekVersion" IS DISTINCT FROM OLD."KekVersion"
                            OR NEW."KekFingerprint" IS DISTINCT FROM OLD."KekFingerprint"
                            OR NEW."AttemptKeyContextFingerprint" IS DISTINCT FROM OLD."AttemptKeyContextFingerprint"
                            OR NEW."WrappingSuiteId" IS DISTINCT FROM OLD."WrappingSuiteId"
                            OR NEW."WrappingSuiteVersion" IS DISTINCT FROM OLD."WrappingSuiteVersion"
                            OR NEW."CreatedAtUtc" IS DISTINCT FROM OLD."CreatedAtUtc") THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',
                                MESSAGE='RAW_EXPORT_ATTEMPT_KEY_IMMUTABLE_IDENTITY';
                        END IF;
                        RETURN NEW;
                    ELSIF TG_TABLE_NAME = 'raw_export_key_provider_operations' THEN
                        IF TG_OP = 'DELETE' THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',
                                MESSAGE='RAW_EXPORT_KEY_PROVIDER_OPERATION_DELETE_FORBIDDEN';
                        END IF;
                        IF TG_OP = 'UPDATE' AND (
                            NEW."ProviderOperationId" IS DISTINCT FROM OLD."ProviderOperationId"
                            OR NEW."KeyProviderId" IS DISTINCT FROM OLD."KeyProviderId"
                            OR NEW."ProviderOperationToken" IS DISTINCT FROM OLD."ProviderOperationToken"
                            OR NEW."AttemptKeyReservationId" IS DISTINCT FROM OLD."AttemptKeyReservationId"
                            OR NEW."PreparationId" IS DISTINCT FROM OLD."PreparationId"
                            OR NEW."PreparationFence" IS DISTINCT FROM OLD."PreparationFence"
                            OR NEW."AttemptKeyContextFingerprint" IS DISTINCT FROM OLD."AttemptKeyContextFingerprint"
                            OR NEW."IssuedAtUtc" IS DISTINCT FROM OLD."IssuedAtUtc") THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',
                                MESSAGE='RAW_EXPORT_KEY_PROVIDER_OPERATION_IMMUTABLE_IDENTITY';
                        END IF;
                        RETURN NEW;
                    END IF;

                    RAISE EXCEPTION USING ERRCODE='P0001',
                        MESSAGE='RAW_EXPORT_ATTEMPT_KEY_GUARD_UNKNOWN_TABLE';
                END
                $guard$;

                ALTER FUNCTION tagekyc.raw_export_attempt_key_guard()
                    OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_attempt_key_guard()
                    FROM PUBLIC, tagekyc_runtime;

                CREATE TRIGGER trg_raw_export_attempt_key_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_attempt_key_reservations
                FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_attempt_key_guard();
                CREATE TRIGGER trg_raw_export_attempt_key_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_key_provider_operations
                FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_attempt_key_guard();
                CREATE TRIGGER trg_raw_export_attempt_key_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_attempt_key_preparation_events
                FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_attempt_key_guard();

                CREATE FUNCTION tagekyc.raw_export_enforce_attempt_key_pair()
                RETURNS trigger
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $pair$
                DECLARE
                    reservation_id uuid := CASE
                        WHEN TG_TABLE_NAME = 'raw_export_attempt_key_reservations'
                            THEN NEW."AttemptKeyReservationId"
                        WHEN TG_TABLE_NAME = 'raw_export_key_provider_operations'
                            THEN NEW."AttemptKeyReservationId"
                        ELSE NULL END;
                    head_state text;
                    mapping_state text;
                BEGIN
                    IF reservation_id IS NULL THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_KEY_STATE_PAIR_INVALID';
                    END IF;

                    SELECT h."PreparationDisposition", m."ProviderOperationState"
                    INTO head_state, mapping_state
                    FROM tagekyc.raw_export_attempt_key_reservations h
                    LEFT JOIN tagekyc.raw_export_key_provider_operations m
                      ON m."AttemptKeyReservationId" = h."AttemptKeyReservationId"
                     AND m."PreparationFence" = h."CurrentPreparationFence"
                    WHERE h."AttemptKeyReservationId" = reservation_id;

                    IF NOT FOUND OR mapping_state IS NULL OR NOT (
                        (head_state='PreparingLive' AND mapping_state IN ('Issued','ResultObserved')) OR
                        (head_state='PreparingExpiredAwaitingResolution' AND mapping_state='Issued') OR
                        (head_state='ProviderOutcomeUnknown' AND mapping_state='Issued') OR
                        (head_state='ProviderCorruptOrUnverifiable' AND mapping_state='Issued') OR
                        (head_state='ProviderCleanupRequired' AND mapping_state='CleanupRequired') OR
                        (head_state='ReadyForFreshPreparation' AND mapping_state IN ('CleanedUp','AbsenceProven')) OR
                        (head_state='Active' AND mapping_state='ResultObserved') OR
                        (head_state='Revoked' AND mapping_state='ResultObserved') OR
                        (head_state='AbandonRequested' AND mapping_state IN ('Issued','ResultObserved','CleanupRequired','CleanedUp','AbsenceProven')) OR
                        (head_state='ReservationAbandoned' AND mapping_state IN ('CleanedUp','AbsenceProven'))
                    ) THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_KEY_STATE_PAIR_INVALID';
                    END IF;
                    RETURN NEW;
                END
                $pair$;

                ALTER FUNCTION tagekyc.raw_export_enforce_attempt_key_pair()
                    OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_enforce_attempt_key_pair()
                    FROM PUBLIC, tagekyc_runtime,
                         tagekyc_raw_export_custody_encryptor,
                         tagekyc_raw_export_reconciler,
                         tagekyc_raw_export_lifecycle;

                CREATE CONSTRAINT TRIGGER trg_raw_export_attempt_key_pair_from_head
                AFTER INSERT OR UPDATE ON tagekyc.raw_export_attempt_key_reservations
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_enforce_attempt_key_pair();
                CREATE CONSTRAINT TRIGGER trg_raw_export_attempt_key_pair_from_operation
                AFTER INSERT OR UPDATE ON tagekyc.raw_export_key_provider_operations
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_enforce_attempt_key_pair();
                """);

            AddOperationalFunctions(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_raw_export_attempt_key_pair_from_head
                    ON tagekyc.raw_export_attempt_key_reservations;
                DROP TRIGGER IF EXISTS trg_raw_export_attempt_key_pair_from_operation
                    ON tagekyc.raw_export_key_provider_operations;
                DROP TRIGGER IF EXISTS trg_raw_export_attempt_key_guard
                    ON tagekyc.raw_export_attempt_key_reservations;
                DROP TRIGGER IF EXISTS trg_raw_export_attempt_key_guard
                    ON tagekyc.raw_export_key_provider_operations;
                DROP TRIGGER IF EXISTS trg_raw_export_attempt_key_guard
                    ON tagekyc.raw_export_attempt_key_preparation_events;

                DROP FUNCTION IF EXISTS tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_mark_attempt_key_preparation_expired(uuid,uuid,bigint);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_resolve_attempt_key_provider_outcome(uuid,uuid,bigint,text,text,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_mark_key_provider_cleanup_required(uuid,uuid,uuid,bigint,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_key_provider_cleanup_observation(uuid,uuid,uuid,bigint,text,text,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_acknowledge_key_provider_cleanup(uuid,uuid,uuid,bigint,text,text,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_request_abandon_attempt_key_reservation(uuid,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_finalize_abandon_attempt_key_reservation(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_inspect_attempt_key_reservation(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_current_attempt_key_recovery_context(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_active_attempt_key_envelope(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_activate_recovered_attempt_key_reservation_internal(uuid,uuid,bigint,bytea,bytea,bytea,bytea,text,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_enforce_attempt_key_pair();
                DROP FUNCTION IF EXISTS tagekyc.raw_export_attempt_key_guard();

                ALTER DEFAULT PRIVILEGES FOR ROLE tagekyc_raw_export_deployer IN SCHEMA tagekyc
                    GRANT EXECUTE ON FUNCTIONS TO PUBLIC;

                REVOKE USAGE ON SCHEMA tagekyc FROM
                    tagekyc_raw_export_custody_encryptor,
                    tagekyc_raw_export_reconciler,
                    tagekyc_raw_export_lifecycle;

                REVOKE tagekyc_raw_export_custody_encryptor
                  FROM tagekyc_raw_export_encryptor_login;
                REVOKE tagekyc_raw_export_reconciler
                  FROM tagekyc_raw_export_reconciler_login;
                REVOKE tagekyc_raw_export_lifecycle
                  FROM tagekyc_raw_export_lifecycle_login;
                DROP ROLE tagekyc_raw_export_custody_encryptor;
                DROP ROLE tagekyc_raw_export_reconciler;
                DROP ROLE tagekyc_raw_export_lifecycle;
                """);

            migrationBuilder.DropTable(
                name: "raw_export_attempt_key_preparation_events",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_key_provider_operations",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_attempt_key_reservations",
                schema: "tagekyc");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_raw_export_enc_attempt_attemptid_keyresvid",
                schema: "tagekyc",
                table: "raw_export_source_encryption_attempts");
        }

        private static void AddOperationalFunctions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(
                    p_attempt_key_reservation_id uuid,
                    p_attempt_id uuid,
                    p_source_artifact_id uuid)
                RETURNS TABLE(
                    outcome text,
                    provider_operation_id uuid,
                    preparation_id uuid,
                    preparation_fence bigint,
                    provider_operation_token text,
                    preparation_lease_expires_at_utc timestamptz,
                    attempt_key_context_fingerprint bytea,
                    key_provider_id text,
                    kek_id text,
                    kek_version integer,
                    kek_fingerprint text,
                    wrapping_suite_id text,
                    wrapping_suite_version integer)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $prepare$
                DECLARE
                    attempt_row record;
                    existing_head record;
                    operation_row record;
                    now_utc timestamptz := pg_catalog.statement_timestamp();
                    new_preparation_id uuid := pg_catalog.gen_random_uuid();
                    new_operation_id uuid := pg_catalog.gen_random_uuid();
                    new_fence bigint;
                    new_token text;
                    context_fingerprint bytea;
                    prior_context text := pg_catalog.current_setting(
                        'tagekyc.raw_export_attempt_key_write_context', true);
                BEGIN
                    SELECT a.*, h."CustodyState", h."CurrentEncryptionAttemptId",
                           r."ReservationExpiresAtUtc"
                    INTO attempt_row
                    FROM tagekyc.raw_export_source_encryption_attempts a
                    JOIN tagekyc.raw_export_source_head h
                      ON h."SourceArtifactId"=a."SourceArtifactId"
                    JOIN tagekyc.raw_export_source_reservations r
                      ON r."SourceArtifactId"=a."SourceArtifactId"
                    WHERE a."AttemptId"=p_attempt_id
                      AND a."SourceArtifactId"=p_source_artifact_id
                      AND a."AttemptKeyReservationId"=p_attempt_key_reservation_id
                    FOR UPDATE OF a,h;

                    IF NOT FOUND OR attempt_row."CustodyState" <> 'Reserved'
                       OR attempt_row."CurrentEncryptionAttemptId" <> p_attempt_id
                       OR attempt_row."R2TerminationDisposition" IS NOT NULL
                       OR attempt_row."OwnershipLeaseExpiresAtUtc" <= now_utc
                       OR attempt_row."ReservationExpiresAtUtc" <= now_utc THEN
                        RETURN QUERY SELECT 'HeadNotReserved'::text,
                            NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                            NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
                        RETURN;
                    END IF;

                    context_fingerprint := tagekyc.raw_export_c1_hash_canonical(
                        'tip-88c1-attempt-key-context-v1',
                        pg_catalog.replace(p_attempt_key_reservation_id::text,'-',''),
                        pg_catalog.replace(p_attempt_id::text,'-',''),
                        pg_catalog.encode(attempt_row."EncryptionAttemptFingerprint",'hex'),
                        attempt_row."KeyProviderId",attempt_row."KekId",
                        attempt_row."KekVersion"::text,attempt_row."KekFingerprint",
                        'AES-256-GCM','1');

                    SELECT * INTO existing_head
                    FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
                    FOR UPDATE;

                    IF FOUND THEN
                        IF existing_head."AttemptId" <> p_attempt_id
                           OR existing_head."EncryptionAttemptFingerprint" IS DISTINCT FROM attempt_row."EncryptionAttemptFingerprint"
                           OR existing_head."AttemptKeyContextFingerprint" IS DISTINCT FROM context_fingerprint THEN
                            RETURN QUERY SELECT 'Conflict'::text,
                                NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                                NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
                            RETURN;
                        END IF;
                        IF existing_head."PreparationDisposition"='Active' THEN
                            SELECT * INTO operation_row
                            FROM tagekyc.raw_export_key_provider_operations
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
                              AND "PreparationFence"=existing_head."CurrentPreparationFence";
                            RETURN QUERY SELECT 'ExistingMatch'::text,
                                operation_row."ProviderOperationId",existing_head."CurrentPreparationId",
                                existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,
                                existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",
                                existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",
                                existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion";
                            RETURN;
                        END IF;
                        IF existing_head."PreparationDisposition" IN
                           ('Revoked','ProviderCorruptOrUnverifiable','AbandonRequested','ReservationAbandoned') THEN
                            RETURN QUERY SELECT 'Terminated'::text,
                                NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                                NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
                            RETURN;
                        END IF;
                        IF existing_head."PreparationDisposition" <> 'ReadyForFreshPreparation' THEN
                            SELECT * INTO operation_row
                            FROM tagekyc.raw_export_key_provider_operations
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
                              AND "PreparationFence"=existing_head."CurrentPreparationFence";
                            RETURN QUERY SELECT 'InProgress'::text,
                                operation_row."ProviderOperationId",existing_head."CurrentPreparationId",
                                existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,
                                existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",
                                existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",
                                existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion";
                            RETURN;
                        END IF;
                        new_fence := existing_head."CurrentPreparationFence" + 1;
                    ELSE
                        new_fence := 1;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM tagekyc.raw_export_key_provider_operations history
                        WHERE history."AttemptKeyReservationId"=p_attempt_key_reservation_id
                          AND history."PreparationFence">=new_fence) THEN
                        RETURN QUERY SELECT 'StaleFence'::text,
                            NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,
                            NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer;
                        RETURN;
                    END IF;

                    new_token := pg_catalog.rtrim(pg_catalog.translate(
                        pg_catalog.encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'=');
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        IF existing_head."AttemptKeyReservationId" IS NULL THEN
                            INSERT INTO tagekyc.raw_export_attempt_key_reservations(
                                "AttemptKeyReservationId","AttemptId","EncryptionAttemptFingerprint",
                                "KeyProviderId","KekId","KekVersion","KekFingerprint",
                                "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion",
                                "PreparationDisposition","CurrentPreparationId","CurrentPreparationFence",
                                "CurrentPreparationLeaseExpiresAtUtc","CurrentProviderOperationToken",
                                "ResolutionAttemptCount","CleanupAttemptCount","CleanupOperatorInterventionRequired",
                                "RowRevision","CreatedAtUtc","UpdatedAtUtc")
                            VALUES(p_attempt_key_reservation_id,p_attempt_id,attempt_row."EncryptionAttemptFingerprint",
                                attempt_row."KeyProviderId",attempt_row."KekId",attempt_row."KekVersion",attempt_row."KekFingerprint",
                                context_fingerprint,'AES-256-GCM',1,'PreparingLive',new_preparation_id,new_fence,
                                now_utc + interval '15 minutes',new_token,0,0,false,1,now_utc,now_utc);
                        ELSE
                            UPDATE tagekyc.raw_export_attempt_key_reservations SET
                                "PreparationDisposition"='PreparingLive',
                                "CurrentPreparationId"=new_preparation_id,
                                "CurrentPreparationFence"=new_fence,
                                "CurrentPreparationLeaseExpiresAtUtc"=now_utc + interval '15 minutes',
                                "CurrentProviderOperationToken"=new_token,
                                "ResolutionAttemptCount"=0,"NextResolutionAttemptNotBeforeUtc"=NULL,
                                "ResolutionDeadlineUtc"=NULL,"CleanupAttemptCount"=0,
                                "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                                "CleanupOperatorInterventionRequired"=false,
                                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        END IF;

                        INSERT INTO tagekyc.raw_export_key_provider_operations(
                            "ProviderOperationId","KeyProviderId","ProviderOperationToken",
                            "AttemptKeyReservationId","PreparationId","PreparationFence",
                            "AttemptKeyContextFingerprint","ProviderOperationState","IssuedAtUtc","UpdatedAtUtc")
                        VALUES(new_operation_id,attempt_row."KeyProviderId",new_token,
                            p_attempt_key_reservation_id,new_preparation_id,new_fence,
                            context_fingerprint,'Issued',now_utc,now_utc);

                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence",
                            "EventSequence","EventKind","ProviderOperationToken","WrappingSuiteId",
                            "WrappingSuiteVersion","EventAtUtc")
                        SELECT pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,new_preparation_id,new_fence,
                            COALESCE(MAX("EventSequence"),0)+1,'Opened',new_token,'AES-256-GCM',1,now_utc
                        FROM tagekyc.raw_export_attempt_key_preparation_events
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                        RAISE;
                    END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);

                    RETURN QUERY SELECT 'PreparingLive'::text,new_operation_id,new_preparation_id,new_fence,new_token,
                        now_utc + interval '15 minutes',context_fingerprint,attempt_row."KeyProviderId"::text,
                        attempt_row."KekId"::text,attempt_row."KekVersion",attempt_row."KekFingerprint"::text,'AES-256-GCM'::text,1;
                END
                $prepare$;

                CREATE FUNCTION tagekyc.raw_export_record_key_provider_wrapped_result(
                    p_provider_operation_id uuid,p_attempt_key_reservation_id uuid,p_preparation_id uuid,
                    p_preparation_fence bigint,p_provider_operation_token text,p_wrapped_dek_ciphertext bytea,
                    p_wrapped_dek_nonce bytea,p_wrapped_dek_tag bytea,p_wrapping_suite_id text,
                    p_wrapping_suite_version integer,p_provider_operation_receipt text,p_provider_resource_reference text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $record$
                DECLARE
                    h record;
                    op record;
                    digest bytea;
                    prior_context text := pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF pg_catalog.octet_length(p_wrapped_dek_ciphertext)<>32
                       OR pg_catalog.octet_length(p_wrapped_dek_nonce)<>12
                       OR pg_catalog.octet_length(p_wrapped_dek_tag)<>16
                       OR p_provider_operation_receipt IS NULL OR pg_catalog.btrim(p_provider_operation_receipt)=''
                       OR p_provider_resource_reference IS NULL OR pg_catalog.btrim(p_provider_resource_reference)='' THEN
                        RETURN 'ShapeInvalid';
                    END IF;
                    IF p_wrapping_suite_id<>'AES-256-GCM' OR p_wrapping_suite_version<>1 THEN
                        RETURN 'SuiteMismatch';
                    END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    IF NOT FOUND THEN RETURN 'StaleOperation'; END IF;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations
                    WHERE "ProviderOperationId"=p_provider_operation_id FOR UPDATE;
                    IF NOT FOUND OR op."AttemptKeyReservationId"<>p_attempt_key_reservation_id
                       OR op."PreparationId"<>p_preparation_id OR op."PreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationToken"<>p_provider_operation_token THEN RETURN 'StaleOperation'; END IF;
                    digest := tagekyc.raw_export_c1_hash_canonical(
                        'tip-88c1-wrapped-dek-metadata-v1',pg_catalog.encode(op."AttemptKeyContextFingerprint",'hex'),
                        p_wrapping_suite_id,p_wrapping_suite_version::text,pg_catalog.encode(p_wrapped_dek_nonce,'hex'),
                        pg_catalog.encode(p_wrapped_dek_ciphertext,'hex'),pg_catalog.encode(p_wrapped_dek_tag,'hex'));
                    IF op."ProviderOperationState"='ResultObserved' THEN
                        RETURN CASE WHEN op."WrappedDekCiphertext"=p_wrapped_dek_ciphertext
                            AND op."WrappedDekNonce"=p_wrapped_dek_nonce AND op."WrappedDekTag"=p_wrapped_dek_tag
                            AND op."WrappedDekMetadataDigest"=digest AND op."ProviderOperationReceipt"=p_provider_operation_receipt
                            AND op."ProviderResourceReference"=p_provider_resource_reference
                            THEN 'ExistingMatch' ELSE 'StateConflict' END;
                    END IF;
                    IF op."ProviderOperationState"<>'Issued' THEN RETURN 'StateConflict'; END IF;
                    IF h."PreparationDisposition"<>'PreparingLive'
                       OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id
                       OR h."CurrentPreparationFence"<>p_preparation_fence THEN RETURN 'StateConflict'; END IF;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_key_provider_operations SET
                            "ProviderOperationState"='ResultObserved',"WrappedDekCiphertext"=p_wrapped_dek_ciphertext,
                            "WrappedDekNonce"=p_wrapped_dek_nonce,"WrappedDekTag"=p_wrapped_dek_tag,
                            "WrappedDekMetadataDigest"=digest,"WrappingSuiteId"=p_wrapping_suite_id,
                            "WrappingSuiteVersion"=p_wrapping_suite_version,"ProviderResourceReference"=p_provider_resource_reference,
                            "ProviderOperationReceipt"=p_provider_operation_receipt,"ResultObservedAtUtc"=pg_catalog.statement_timestamp(),
                            "UpdatedAtUtc"=pg_catalog.statement_timestamp()
                        WHERE "ProviderOperationId"=p_provider_operation_id;
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE;
                    END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'ResultObserved';
                END
                $record$;

                CREATE FUNCTION tagekyc.raw_export_activate_attempt_key_reservation(
                    p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $activate$
                DECLARE
                    h record; op record; seq bigint; now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text := pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    IF NOT FOUND THEN RETURN 'HeadNotReserved'; END IF;
                    IF h."PreparationDisposition"='Active' THEN RETURN 'AlreadyActivated'; END IF;
                    IF h."PreparationDisposition" IN ('Revoked','AbandonRequested','ReservationAbandoned','ProviderCorruptOrUnverifiable') THEN RETURN 'Terminated'; END IF;
                    IF h."CurrentPreparationId"<>p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence THEN RETURN 'StalePreparation'; END IF;
                    IF h."PreparationDisposition"<>'PreparingLive' THEN RETURN 'HeadNotCurrent'; END IF;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=p_preparation_fence FOR UPDATE;
                    IF NOT FOUND OR op."ProviderOperationState"<>'ResultObserved' THEN RETURN 'DigestMismatch'; END IF;
                    IF op."AttemptKeyContextFingerprint"<>h."AttemptKeyContextFingerprint" THEN RETURN 'DigestMismatch'; END IF;
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET
                            "PreparationDisposition"='Active',"WrappedDekCiphertext"=op."WrappedDekCiphertext",
                            "WrappedDekNonce"=op."WrappedDekNonce","WrappedDekTag"=op."WrappedDekTag",
                            "WrappedDekMetadataDigest"=op."WrappedDekMetadataDigest","PreparedAtUtc"=now_utc,
                            "NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,
                            "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                            "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence",
                            "EventKind","ProviderOperationReceipt","WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,
                            'DirectActivated',op."ProviderOperationReceipt",op."WrappedDekMetadataDigest",op."WrappingSuiteId",op."WrappingSuiteVersion",now_utc);
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE;
                    END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'Activated';
                END
                $activate$;
                """);

            AddRecoveryAndLifecycleFunctions(migrationBuilder);
            AddReadSurfacesAndPrivileges(migrationBuilder);
        }

        private static void AddRecoveryAndLifecycleFunctions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_mark_attempt_key_preparation_expired(
                    p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $expired$
                DECLARE h record; seq bigint; now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    IF NOT FOUND OR h."CurrentPreparationId"<>p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence
                       OR h."PreparationDisposition"<>'PreparingLive' THEN RETURN 'StalePreparation'; END IF;
                    IF h."CurrentPreparationLeaseExpiresAtUtc">now_utc THEN RETURN 'NotDue'; END IF;
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET
                            "PreparationDisposition"='PreparingExpiredAwaitingResolution',
                            "ResolutionDeadlineUtc"=COALESCE("ResolutionDeadlineUtc",now_utc+interval '24 hours'),
                            "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,'Expired',now_utc);
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE;
                    END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'Expired';
                END $expired$;

                CREATE FUNCTION tagekyc.raw_export_resolve_attempt_key_provider_outcome(
                    p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,
                    p_resolution text,p_provider_cleanup_reference text,p_provider_operation_receipt text,
                    p_provider_absence_proof_receipt text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $resolve$
                DECLARE h record; op record; seq bigint; evidence bytea; event_kind text; result text;
                    now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF p_resolution NOT IN ('ProviderOutcomeUnknown','ProviderUnavailable','NoProviderResult','CorruptOrUnverifiable')
                        THEN RETURN 'IllegalResolution'; END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=p_preparation_fence FOR UPDATE;
                    IF h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationState"<>'Issued'
                       OR h."CurrentProviderOperationToken" IS DISTINCT FROM op."ProviderOperationToken"
                       THEN RETURN 'StalePreparation'; END IF;
                    IF p_resolution='NoProviderResult' THEN
                        IF h."PreparationDisposition" NOT IN ('PreparingExpiredAwaitingResolution','AbandonRequested') THEN
                            RETURN 'IllegalResolution';
                        END IF;
                    ELSIF h."PreparationDisposition" NOT IN
                        ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown') THEN
                        RETURN 'StalePreparation';
                    END IF;
                    IF h."NextResolutionAttemptNotBeforeUtc" IS NOT NULL AND h."NextResolutionAttemptNotBeforeUtc">now_utc
                        THEN RETURN 'RetryTooSoon'; END IF;
                    IF h."ResolutionDeadlineUtc" IS NOT NULL AND h."ResolutionDeadlineUtc"<now_utc
                       AND p_resolution<>'NoProviderResult' THEN p_resolution:='CorruptOrUnverifiable'; result:='DeadlineExceeded'; END IF;
                    IF p_resolution='NoProviderResult' AND (p_provider_absence_proof_receipt IS NULL OR pg_catalog.btrim(p_provider_absence_proof_receipt)='')
                        THEN RETURN 'IllegalResolution'; END IF;
                    IF p_resolution='NoProviderResult' AND h."CurrentPreparationLeaseExpiresAtUtc">now_utc THEN RETURN 'RetryTooSoon'; END IF;
                    evidence := CASE WHEN p_resolution='NoProviderResult' THEN
                        tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-provider-absence-evidence-v1',
                            pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),
                            p_preparation_fence::text,'AbsenceProven',op."ProviderOperationToken",p_provider_absence_proof_receipt)
                    ELSE tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-provider-resolution-evidence-v1',
                            pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),
                            p_preparation_fence::text,p_resolution,op."ProviderOperationToken",
                            CASE WHEN p_provider_operation_receipt IS NULL THEN '0' ELSE '1' END,
                            COALESCE(p_provider_operation_receipt,''),'0','0') END;
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        IF p_resolution='NoProviderResult' THEN
                            UPDATE tagekyc.raw_export_key_provider_operations SET "ProviderOperationState"='AbsenceProven',
                                "ProviderAbsenceProofReceipt"=p_provider_absence_proof_receipt,"UpdatedAtUtc"=now_utc
                            WHERE "ProviderOperationId"=op."ProviderOperationId";
                            UPDATE tagekyc.raw_export_attempt_key_reservations SET
                                "PreparationDisposition"=CASE WHEN "PreparationDisposition"='AbandonRequested'
                                    THEN 'AbandonRequested' ELSE 'ReadyForFreshPreparation' END,
                                "CurrentPreparationId"=CASE WHEN "PreparationDisposition"='AbandonRequested'
                                    THEN "CurrentPreparationId" ELSE NULL END,
                                "CurrentPreparationLeaseExpiresAtUtc"=CASE WHEN "PreparationDisposition"='AbandonRequested'
                                    THEN "CurrentPreparationLeaseExpiresAtUtc" ELSE NULL END,
                                "CurrentProviderOperationToken"=CASE WHEN "PreparationDisposition"='AbandonRequested'
                                    THEN "CurrentProviderOperationToken" ELSE NULL END,
                                "ResolutionAttemptCount"="ResolutionAttemptCount"+1,"NextResolutionAttemptNotBeforeUtc"=NULL,
                                "ResolutionDeadlineUtc"=NULL,"NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                            event_kind:='ResolvedNoResult'; result:='ResolvedNoResult';
                        ELSIF p_resolution='ProviderOutcomeUnknown' THEN
                            UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='ProviderOutcomeUnknown',
                                "ResolutionAttemptCount"="ResolutionAttemptCount"+1,
                                "ResolutionDeadlineUtc"=COALESCE("ResolutionDeadlineUtc",now_utc+interval '24 hours'),
                                "NextResolutionAttemptNotBeforeUtc"=now_utc+LEAST(
                                    interval '30 minutes',
                                    interval '30 seconds' * pg_catalog.power(2.0,h."ResolutionAttemptCount"::double precision)),
                                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                            event_kind:='ResolvedOutcomeUnknown'; result:='ResolvedOutcomeUnknown';
                        ELSIF p_resolution='ProviderUnavailable' THEN
                            UPDATE tagekyc.raw_export_attempt_key_reservations SET
                                "ResolutionAttemptCount"="ResolutionAttemptCount"+1,
                                "ResolutionDeadlineUtc"=COALESCE("ResolutionDeadlineUtc",now_utc+interval '24 hours'),
                                "NextResolutionAttemptNotBeforeUtc"=now_utc+LEAST(
                                    interval '30 minutes',
                                    interval '30 seconds' * pg_catalog.power(2.0,h."ResolutionAttemptCount"::double precision)),
                                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                            event_kind:='ProviderUnavailableObserved'; result:='ProviderUnavailableObserved';
                        ELSE
                            UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='ProviderCorruptOrUnverifiable',
                                "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                            event_kind:='ResolvedCorrupt'; result:=COALESCE(result,'ResolvedCorrupt');
                        END IF;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence",
                            "EventKind","ResolutionKind","ProviderResolutionEvidenceDigest","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,
                            event_kind,p_resolution,evidence,now_utc);
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE;
                    END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN result;
                END $resolve$;

                CREATE FUNCTION tagekyc.raw_export_mark_key_provider_cleanup_required(
                    p_provider_operation_id uuid,p_attempt_key_reservation_id uuid,p_preparation_id uuid,
                    p_preparation_fence bigint,p_provider_operation_token text,p_provider_cleanup_reference text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $cleanup_required$
                DECLARE h record; op record; seq bigint; evidence bytea; now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF p_provider_cleanup_reference IS NULL OR pg_catalog.btrim(p_provider_cleanup_reference)='' THEN RETURN 'StateConflict'; END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationId"=p_provider_operation_id FOR UPDATE;
                    IF NOT FOUND OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence
                       OR op."AttemptKeyReservationId"<>p_attempt_key_reservation_id
                       OR op."PreparationId"<>p_preparation_id OR op."PreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationToken"<>p_provider_operation_token THEN RETURN 'StaleOperation'; END IF;
                    IF op."ProviderOperationState"='CleanupRequired' THEN RETURN CASE WHEN op."ProviderCleanupReference"=p_provider_cleanup_reference THEN 'ExistingMatch' ELSE 'StateConflict' END; END IF;
                    IF op."ProviderOperationState" NOT IN ('Issued','ResultObserved') OR h."PreparationDisposition" NOT IN
                       ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown','AbandonRequested') THEN RETURN 'StateConflict'; END IF;
                    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-provider-resolution-evidence-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),
                        p_preparation_fence::text,'ProviderResourceCleanupRequired',p_provider_operation_token,'0','0','1',p_provider_cleanup_reference,'0');
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_key_provider_operations SET "ProviderOperationState"='CleanupRequired',
                            "ProviderCleanupReference"=p_provider_cleanup_reference,"UpdatedAtUtc"=now_utc
                        WHERE "ProviderOperationId"=p_provider_operation_id;
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET
                            "PreparationDisposition"=CASE WHEN "PreparationDisposition"='AbandonRequested' THEN 'AbandonRequested' ELSE 'ProviderCleanupRequired' END,
                            "CleanupDeadlineUtc"=COALESCE("CleanupDeadlineUtc",now_utc+interval '7 days'),
                            "ResolutionDeadlineUtc"=NULL,"NextResolutionAttemptNotBeforeUtc"=NULL,
                            "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "ResolutionKind","ProviderCleanupReference","ProviderResolutionEvidenceDigest","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,
                            'ResolvedCleanupRequired','ProviderResourceCleanupRequired',p_provider_cleanup_reference,evidence,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'CleanupRequired';
                END $cleanup_required$;

                CREATE FUNCTION tagekyc.raw_export_record_key_provider_cleanup_observation(
                    p_provider_operation_id uuid,p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,
                    p_provider_operation_token text,p_cleanup_result_kind text,p_provider_cleanup_reference text,p_provider_cleanup_receipt text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $cleanup_observation$
                DECLARE h record; op record; seq bigint; evidence bytea; now_utc timestamptz:=pg_catalog.statement_timestamp(); result text:='CleanupObserved';
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF p_cleanup_result_kind NOT IN ('CleanupUnavailable','CleanupOutcomeUnknown','CleanupFailed') THEN RETURN 'IllegalCleanupKind'; END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationId"=p_provider_operation_id FOR UPDATE;
                    IF op."AttemptKeyReservationId"<>p_attempt_key_reservation_id
                       OR op."PreparationId"<>p_preparation_id OR op."PreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationToken" IS DISTINCT FROM p_provider_operation_token OR op."ProviderCleanupReference" IS DISTINCT FROM p_provider_cleanup_reference
                       OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationState"<>'CleanupRequired' THEN RETURN 'StaleOperation'; END IF;
                    IF h."NextCleanupAttemptNotBeforeUtc" IS NOT NULL AND h."NextCleanupAttemptNotBeforeUtc">now_utc THEN RETURN 'RetryTooSoon'; END IF;
                    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-cleanup-observation-evidence-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),p_preparation_fence::text,
                        p_provider_cleanup_reference,p_cleanup_result_kind,CASE WHEN p_provider_cleanup_receipt IS NULL THEN '0' ELSE '1' END,COALESCE(p_provider_cleanup_receipt,''));
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    IF h."CleanupDeadlineUtc"<now_utc THEN result:='DeadlineIntervention'; END IF;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET "CleanupAttemptCount"="CleanupAttemptCount"+1,
                            "NextCleanupAttemptNotBeforeUtc"=now_utc+LEAST(
                                interval '1 hour',
                                interval '1 minute' * pg_catalog.power(2.0,h."CleanupAttemptCount"::double precision)),
                            "CleanupOperatorInterventionRequired"=(result='DeadlineIntervention'),
                            "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "CleanupResultKind","ProviderCleanupReference","ProviderCleanupReceipt","CleanupObservationEvidenceDigest","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,'CleanupAttemptObserved',
                            p_cleanup_result_kind,p_provider_cleanup_reference,p_provider_cleanup_receipt,evidence,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN result;
                END $cleanup_observation$;

                CREATE FUNCTION tagekyc.raw_export_acknowledge_key_provider_cleanup(
                    p_provider_operation_id uuid,p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,
                    p_provider_operation_token text,p_cleanup_result_kind text,p_provider_cleanup_reference text,p_provider_cleanup_receipt text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $cleanup_ack$
                DECLARE h record; op record; seq bigint; evidence bytea; now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF p_cleanup_result_kind NOT IN ('Cleaned','AlreadyAbsent') THEN RETURN 'IllegalCleanupKind'; END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationId"=p_provider_operation_id FOR UPDATE;
                    IF op."AttemptKeyReservationId"<>p_attempt_key_reservation_id
                       OR op."PreparationId"<>p_preparation_id OR op."PreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationToken" IS DISTINCT FROM p_provider_operation_token OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id
                       OR h."CurrentPreparationFence"<>p_preparation_fence THEN RETURN 'StaleOperation'; END IF;
                    IF op."ProviderOperationState"<>'CleanupRequired' OR op."ProviderCleanupReference" IS DISTINCT FROM p_provider_cleanup_reference
                       OR p_provider_cleanup_receipt IS NULL OR pg_catalog.btrim(p_provider_cleanup_receipt)='' THEN RETURN 'EvidenceMissing'; END IF;
                    IF h."PreparationDisposition" NOT IN ('ProviderCleanupRequired','AbandonRequested') THEN RETURN 'StateConflict'; END IF;
                    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-provider-cleanup-evidence-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),p_preparation_fence::text,
                        p_cleanup_result_kind,p_provider_cleanup_reference,p_provider_cleanup_receipt);
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_key_provider_operations SET "ProviderOperationState"='CleanedUp',
                            "ProviderCleanupReceipt"=p_provider_cleanup_receipt,"UpdatedAtUtc"=now_utc
                        WHERE "ProviderOperationId"=p_provider_operation_id;
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET
                            "PreparationDisposition"=CASE WHEN "PreparationDisposition"='AbandonRequested' THEN 'AbandonRequested' ELSE 'ReadyForFreshPreparation' END,
                            "CurrentPreparationId"=CASE WHEN "PreparationDisposition"='AbandonRequested' THEN "CurrentPreparationId" ELSE NULL END,
                            "CurrentPreparationLeaseExpiresAtUtc"=CASE WHEN "PreparationDisposition"='AbandonRequested' THEN "CurrentPreparationLeaseExpiresAtUtc" ELSE NULL END,
                            "CurrentProviderOperationToken"=CASE WHEN "PreparationDisposition"='AbandonRequested' THEN "CurrentProviderOperationToken" ELSE NULL END,
                            "NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,
                            "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                            "CleanupOperatorInterventionRequired"=false,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "CleanupResultKind","ProviderCleanupReference","ProviderCleanupReceipt","ProviderCleanupEvidenceDigest","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,'CleanupAcknowledged',
                            p_cleanup_result_kind,p_provider_cleanup_reference,p_provider_cleanup_receipt,evidence,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'Acknowledged';
                END $cleanup_ack$;
                """);

            AddTerminalFunctions(migrationBuilder);
        }

        private static void AddTerminalFunctions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_activate_recovered_attempt_key_reservation_internal(
                    p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,
                    p_wrapped_dek_ciphertext bytea,p_wrapped_dek_nonce bytea,p_wrapped_dek_tag bytea,
                    p_wrapped_dek_metadata_digest bytea,p_provider_operation_receipt text,
                    p_provider_resolution_evidence_digest bytea)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $activate_recovered_internal$
                DECLARE seq bigint; now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='Active',
                            "WrappedDekCiphertext"=p_wrapped_dek_ciphertext,"WrappedDekNonce"=p_wrapped_dek_nonce,
                            "WrappedDekTag"=p_wrapped_dek_tag,"WrappedDekMetadataDigest"=p_wrapped_dek_metadata_digest,
                            "PreparedAtUtc"=now_utc,"NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,
                            "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                            "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
                          AND "CurrentPreparationId"=p_preparation_id AND "CurrentPreparationFence"=p_preparation_fence
                          AND "PreparationDisposition" IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown');
                        IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "ResolutionKind","ProviderOperationReceipt","ProviderResolutionEvidenceDigest","WrappedDekMetadataDigest",
                            "WrappingSuiteId","WrappingSuiteVersion","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,
                            'RecoveredActivated','WrappedResultRecovered',p_provider_operation_receipt,
                            p_provider_resolution_evidence_digest,p_wrapped_dek_metadata_digest,'AES-256-GCM',1,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'RecoveredActivated';
                END $activate_recovered_internal$;

                CREATE FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(
                    p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,p_provider_operation_token text,
                    p_wrapped_dek_ciphertext bytea,p_wrapped_dek_nonce bytea,p_wrapped_dek_tag bytea,
                    p_wrapping_suite_id text,p_wrapping_suite_version integer,p_provider_resource_reference text,p_provider_operation_receipt text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $record_recovered$
                DECLARE h record; op record; metadata bytea; evidence bytea; result text;
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF pg_catalog.octet_length(p_wrapped_dek_ciphertext)<>32 OR pg_catalog.octet_length(p_wrapped_dek_nonce)<>12
                       OR pg_catalog.octet_length(p_wrapped_dek_tag)<>16 OR p_provider_resource_reference IS NULL
                       OR p_provider_operation_receipt IS NULL THEN RETURN 'ShapeInvalid'; END IF;
                    IF p_wrapping_suite_id<>'AES-256-GCM' OR p_wrapping_suite_version<>1 THEN RETURN 'SuiteMismatch'; END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
                        AND "PreparationFence"=p_preparation_fence FOR UPDATE;
                    IF NOT FOUND OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence
                       OR op."ProviderOperationToken" IS DISTINCT FROM p_provider_operation_token OR op."ProviderOperationState"<>'Issued'
                       OR h."PreparationDisposition" NOT IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown')
                       THEN RETURN 'StaleOperation'; END IF;
                    metadata:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-wrapped-dek-metadata-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),p_wrapping_suite_id,p_wrapping_suite_version::text,
                        pg_catalog.encode(p_wrapped_dek_nonce,'hex'),pg_catalog.encode(p_wrapped_dek_ciphertext,'hex'),pg_catalog.encode(p_wrapped_dek_tag,'hex'));
                    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-provider-resolution-evidence-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),
                        p_preparation_fence::text,'WrappedResultRecovered',p_provider_operation_token,'1',p_provider_operation_receipt,
                        '0','1',pg_catalog.encode(metadata,'hex'));
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_key_provider_operations SET "ProviderOperationState"='ResultObserved',
                            "WrappedDekCiphertext"=p_wrapped_dek_ciphertext,"WrappedDekNonce"=p_wrapped_dek_nonce,
                            "WrappedDekTag"=p_wrapped_dek_tag,"WrappedDekMetadataDigest"=metadata,
                            "WrappingSuiteId"=p_wrapping_suite_id,"WrappingSuiteVersion"=p_wrapping_suite_version,
                            "ProviderResourceReference"=p_provider_resource_reference,"ProviderOperationReceipt"=p_provider_operation_receipt,
                            "ResultObservedAtUtc"=pg_catalog.statement_timestamp(),"UpdatedAtUtc"=pg_catalog.statement_timestamp()
                        WHERE "ProviderOperationId"=op."ProviderOperationId";
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    result:=tagekyc.raw_export_activate_recovered_attempt_key_reservation_internal(
                        p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,p_wrapped_dek_ciphertext,
                        p_wrapped_dek_nonce,p_wrapped_dek_tag,metadata,p_provider_operation_receipt,evidence);
                    RETURN result;
                END $record_recovered$;

                CREATE FUNCTION tagekyc.raw_export_request_abandon_attempt_key_reservation(
                    p_attempt_key_reservation_id uuid,p_operator_reason_code text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $request_abandon$
                DECLARE h record; seq bigint; actor text:=tagekyc.raw_export_current_actor()::text;
                    now_utc timestamptz:=pg_catalog.statement_timestamp(); prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    IF p_operator_reason_code IS NULL OR pg_catalog.btrim(p_operator_reason_code)='' THEN RETURN 'StateConflict'; END IF;
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
                    IF h."PreparationDisposition"='AbandonRequested' THEN RETURN 'AlreadyRequested'; END IF;
                    IF h."PreparationDisposition" IN ('ReservationAbandoned','Revoked','Active','ProviderCorruptOrUnverifiable','ReadyForFreshPreparation')
                        THEN RETURN 'StateConflict'; END IF;
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='AbandonRequested',
                            "NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,
                            "NextCleanupAttemptNotBeforeUtc"=NULL,
                            "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "ProviderOperationToken","OperatorReasonCode","RequestingActorEvidence","HeadRowRevision","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,h."CurrentPreparationId",h."CurrentPreparationFence",seq,
                            'AbandonRequested',h."CurrentProviderOperationToken",p_operator_reason_code,actor,h."RowRevision"+1,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'AbandonRequested';
                END $request_abandon$;

                CREATE FUNCTION tagekyc.raw_export_finalize_abandon_attempt_key_reservation(p_attempt_key_reservation_id uuid)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $finalize_abandon$
                DECLARE h record; op record; req record; seq bigint; actor text:=tagekyc.raw_export_current_actor()::text;
                    evidence bytea; now_utc timestamptz:=pg_catalog.statement_timestamp(); final_revision bigint;
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    IF NOT FOUND THEN RETURN 'StateConflict'; END IF;
                    IF h."PreparationDisposition"='ReservationAbandoned' THEN RETURN 'AlreadyAbandoned'; END IF;
                    IF h."PreparationDisposition"<>'AbandonRequested' THEN RETURN 'NotRequested'; END IF;
                    SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id
                        AND "PreparationFence"=h."CurrentPreparationFence" FOR UPDATE;
                    IF op."ProviderOperationState" NOT IN ('CleanedUp','AbsenceProven') THEN RETURN 'CleanupNotComplete'; END IF;
                    SELECT * INTO req FROM tagekyc.raw_export_attempt_key_preparation_events
                    WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationId"=h."CurrentPreparationId"
                      AND "PreparationFence"=h."CurrentPreparationFence" AND "EventKind"='AbandonRequested' FOR UPDATE;
                    IF NOT FOUND OR req."ProviderOperationToken" IS DISTINCT FROM h."CurrentProviderOperationToken" THEN RETURN 'StateConflict'; END IF;
                    final_revision:=h."RowRevision"+1;
                    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-abandonment-evidence-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(h."CurrentPreparationId"::text,'-',''),
                        h."CurrentPreparationFence"::text,h."CurrentProviderOperationToken",
                        pg_catalog.replace(req."PreparationEventId"::text,'-',''),req."OperatorReasonCode",
                        req."RequestingActorEvidence",actor,final_revision::text);
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='ReservationAbandoned',
                            "CurrentPreparationLeaseExpiresAtUtc"=NULL,"NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,
                            "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,"CleanupOperatorInterventionRequired"=false,
                            "RowRevision"=final_revision,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "ProviderOperationToken","OperatorReasonCode","RequestingActorEvidence","FinalizingActorEvidence",
                            "AbandonRequestPreparationEventId","AbandonmentEvidenceDigest","HeadRowRevision","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,h."CurrentPreparationId",h."CurrentPreparationFence",seq,
                            'ProviderOperationAbandoned',h."CurrentProviderOperationToken",req."OperatorReasonCode",req."RequestingActorEvidence",
                            actor,req."PreparationEventId",evidence,final_revision,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'ReservationAbandoned';
                END $finalize_abandon$;

                CREATE FUNCTION tagekyc.raw_export_revoke_attempt_key_reservation(
                    p_attempt_key_reservation_id uuid,p_revocation_reason_code text)
                RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
                AS $revoke$
                DECLARE h record; seq bigint; evidence bytea; now_utc timestamptz:=pg_catalog.statement_timestamp();
                    prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
                BEGIN
                    SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
                    IF NOT FOUND OR h."PreparationDisposition"<>'Active' THEN RETURN CASE WHEN h."PreparationDisposition"='Revoked' THEN 'AlreadyRevoked' ELSE 'NotActive' END; END IF;
                    IF p_revocation_reason_code IS NULL OR pg_catalog.btrim(p_revocation_reason_code)='' THEN RETURN 'NotActive'; END IF;
                    evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-revocation-evidence-v1',
                        pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(h."CurrentPreparationId"::text,'-',''),
                        h."CurrentPreparationFence"::text,p_revocation_reason_code);
                    SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
                    BEGIN
                        UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='Revoked',"RevokedAtUtc"=now_utc,
                            "RevocationReasonCode"=p_revocation_reason_code,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                        WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                        INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                            "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind",
                            "RevocationReasonCode","RevocationEvidenceDigest","EventAtUtc")
                        VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,h."CurrentPreparationId",h."CurrentPreparationFence",seq,
                            'Revoked',p_revocation_reason_code,evidence,now_utc);
                    EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
                    PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
                    RETURN 'Revoked';
                END $revoke$;
                """);
        }

        private static void AddReadSurfacesAndPrivileges(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_inspect_attempt_key_reservation(p_attempt_key_reservation_id uuid)
                RETURNS TABLE(preparation_disposition text,attempt_id uuid,current_preparation_id uuid,
                    current_preparation_fence bigint,wrapped_dek_metadata_digest bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog
                AS $inspect$
                    SELECT h."PreparationDisposition",h."AttemptId",h."CurrentPreparationId",
                           h."CurrentPreparationFence",h."WrappedDekMetadataDigest"
                    FROM tagekyc.raw_export_attempt_key_reservations h
                    WHERE h."AttemptKeyReservationId"=p_attempt_key_reservation_id
                $inspect$;

                CREATE FUNCTION tagekyc.raw_export_read_active_attempt_key_envelope(p_attempt_key_reservation_id uuid)
                RETURNS TABLE(attempt_id uuid,attempt_key_context_fingerprint bytea,key_provider_id text,kek_id text,
                    kek_version integer,kek_fingerprint text,wrapping_suite_id text,wrapping_suite_version integer,
                    wrapped_dek_ciphertext bytea,wrapped_dek_nonce bytea,wrapped_dek_tag bytea,wrapped_dek_metadata_digest bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog
                AS $active_envelope$
                    SELECT h."AttemptId",h."AttemptKeyContextFingerprint",h."KeyProviderId",h."KekId",h."KekVersion",
                           h."KekFingerprint",h."WrappingSuiteId",h."WrappingSuiteVersion",h."WrappedDekCiphertext",
                           h."WrappedDekNonce",h."WrappedDekTag",h."WrappedDekMetadataDigest"
                    FROM tagekyc.raw_export_attempt_key_reservations h
                    WHERE h."AttemptKeyReservationId"=p_attempt_key_reservation_id
                      AND h."PreparationDisposition"='Active'
                $active_envelope$;

                CREATE FUNCTION tagekyc.raw_export_read_current_attempt_key_recovery_context(p_attempt_key_reservation_id uuid)
                RETURNS TABLE(
                    preparation_disposition text,attempt_id uuid,attempt_key_context_fingerprint bytea,
                    current_preparation_id uuid,current_preparation_fence bigint,
                    current_preparation_lease_expires_at_utc timestamptz,current_provider_operation_token text,
                    resolution_attempt_count bigint,next_resolution_attempt_not_before_utc timestamptz,resolution_deadline_utc timestamptz,
                    cleanup_attempt_count bigint,next_cleanup_attempt_not_before_utc timestamptz,cleanup_deadline_utc timestamptz,
                    cleanup_operator_intervention_required boolean,provider_operation_id uuid,provider_operation_state text,
                    key_provider_id text,kek_id text,kek_version integer,kek_fingerprint text,wrapping_suite_id text,
                    wrapping_suite_version integer,wrapped_dek_ciphertext bytea,wrapped_dek_nonce bytea,wrapped_dek_tag bytea,
                    wrapped_dek_metadata_digest bytea,provider_resource_reference text,provider_operation_receipt text,
                    provider_cleanup_reference text,provider_cleanup_receipt text,provider_absence_proof_receipt text,
                    latest_resolution_kind text,latest_resolution_evidence_digest bytea,latest_cleanup_result_kind text,
                    latest_cleanup_observation_evidence_digest bytea,abandon_request_preparation_event_id uuid,
                    abandonment_provider_operation_token text,abandonment_operator_reason_code text,
                    requesting_actor_evidence text,finalizing_actor_evidence text,abandonment_head_row_revision bigint,
                    abandonment_event_at_utc timestamptz,abandonment_evidence_digest bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog
                AS $recovery_context$
                    SELECT h."PreparationDisposition",h."AttemptId",h."AttemptKeyContextFingerprint",h."CurrentPreparationId",
                           h."CurrentPreparationFence",h."CurrentPreparationLeaseExpiresAtUtc",h."CurrentProviderOperationToken",
                           h."ResolutionAttemptCount",h."NextResolutionAttemptNotBeforeUtc",h."ResolutionDeadlineUtc",
                           h."CleanupAttemptCount",h."NextCleanupAttemptNotBeforeUtc",h."CleanupDeadlineUtc",
                           h."CleanupOperatorInterventionRequired",m."ProviderOperationId",m."ProviderOperationState",
                           h."KeyProviderId",h."KekId",h."KekVersion",h."KekFingerprint",h."WrappingSuiteId",h."WrappingSuiteVersion",
                           m."WrappedDekCiphertext",m."WrappedDekNonce",m."WrappedDekTag",m."WrappedDekMetadataDigest",
                           m."ProviderResourceReference",m."ProviderOperationReceipt",m."ProviderCleanupReference",
                           m."ProviderCleanupReceipt",m."ProviderAbsenceProofReceipt",
                           resolution_event."ResolutionKind",resolution_event."ProviderResolutionEvidenceDigest",
                           cleanup_event."CleanupResultKind",cleanup_event."CleanupObservationEvidenceDigest",
                           abandon_event."AbandonRequestPreparationEventId",abandon_event."ProviderOperationToken",
                           abandon_event."OperatorReasonCode",abandon_event."RequestingActorEvidence",
                           abandon_event."FinalizingActorEvidence",abandon_event."HeadRowRevision",
                           abandon_event."EventAtUtc",abandon_event."AbandonmentEvidenceDigest"
                    FROM tagekyc.raw_export_attempt_key_reservations h
                    LEFT JOIN tagekyc.raw_export_key_provider_operations m
                      ON m."AttemptKeyReservationId"=h."AttemptKeyReservationId"
                     AND m."PreparationFence"=h."CurrentPreparationFence"
                    LEFT JOIN LATERAL (
                        SELECT e."ResolutionKind",e."ProviderResolutionEvidenceDigest"
                        FROM tagekyc.raw_export_attempt_key_preparation_events e
                        WHERE e."AttemptKeyReservationId"=h."AttemptKeyReservationId"
                          AND e."PreparationFence"=h."CurrentPreparationFence"
                          AND e."ResolutionKind" IS NOT NULL
                        ORDER BY e."EventSequence" DESC LIMIT 1) resolution_event ON true
                    LEFT JOIN LATERAL (
                        SELECT e."CleanupResultKind",e."CleanupObservationEvidenceDigest"
                        FROM tagekyc.raw_export_attempt_key_preparation_events e
                        WHERE e."AttemptKeyReservationId"=h."AttemptKeyReservationId"
                          AND e."PreparationFence"=h."CurrentPreparationFence"
                          AND e."EventKind"='CleanupAttemptObserved'
                        ORDER BY e."EventSequence" DESC LIMIT 1) cleanup_event ON true
                    LEFT JOIN LATERAL (
                        SELECT COALESCE(e."AbandonRequestPreparationEventId",e."PreparationEventId") AS "AbandonRequestPreparationEventId",
                               e."ProviderOperationToken",e."OperatorReasonCode",e."RequestingActorEvidence",
                               e."FinalizingActorEvidence",e."HeadRowRevision",e."EventAtUtc",e."AbandonmentEvidenceDigest"
                        FROM tagekyc.raw_export_attempt_key_preparation_events e
                        WHERE e."AttemptKeyReservationId"=h."AttemptKeyReservationId"
                          AND e."PreparationFence"=h."CurrentPreparationFence"
                          AND e."EventKind" IN ('AbandonRequested','ProviderOperationAbandoned')
                        ORDER BY CASE WHEN e."EventKind"='ProviderOperationAbandoned' THEN 1 ELSE 0 END DESC,
                                 e."EventSequence" DESC LIMIT 1) abandon_event ON true
                    WHERE h."AttemptKeyReservationId"=p_attempt_key_reservation_id
                $recovery_context$;

                ALTER FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_mark_attempt_key_preparation_expired(uuid,uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_resolve_attempt_key_provider_outcome(uuid,uuid,bigint,text,text,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_mark_key_provider_cleanup_required(uuid,uuid,uuid,bigint,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_key_provider_cleanup_observation(uuid,uuid,uuid,bigint,text,text,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_acknowledge_key_provider_cleanup(uuid,uuid,uuid,bigint,text,text,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_request_abandon_attempt_key_reservation(uuid,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_finalize_abandon_attempt_key_reservation(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_inspect_attempt_key_reservation(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_current_attempt_key_recovery_context(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_active_attempt_key_envelope(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_activate_recovered_attempt_key_reservation_internal(uuid,uuid,bigint,bytea,bytea,bytea,bytea,text,bytea) OWNER TO tagekyc_raw_export_deployer;

                ALTER DEFAULT PRIVILEGES FOR ROLE tagekyc_raw_export_deployer IN SCHEMA tagekyc
                    REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
                REVOKE ALL ON ALL TABLES IN SCHEMA tagekyc FROM
                    tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;
                REVOKE ALL ON ALL FUNCTIONS IN SCHEMA tagekyc FROM
                    tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;

                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid),
                    tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text),
                    tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint),
                    tagekyc.raw_export_mark_attempt_key_preparation_expired(uuid,uuid,bigint),
                    tagekyc.raw_export_resolve_attempt_key_provider_outcome(uuid,uuid,bigint,text,text,text,text),
                    tagekyc.raw_export_mark_key_provider_cleanup_required(uuid,uuid,uuid,bigint,text,text),
                    tagekyc.raw_export_record_key_provider_cleanup_observation(uuid,uuid,uuid,bigint,text,text,text,text),
                    tagekyc.raw_export_acknowledge_key_provider_cleanup(uuid,uuid,uuid,bigint,text,text,text,text),
                    tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text),
                    tagekyc.raw_export_request_abandon_attempt_key_reservation(uuid,text),
                    tagekyc.raw_export_finalize_abandon_attempt_key_reservation(uuid),
                    tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text),
                    tagekyc.raw_export_inspect_attempt_key_reservation(uuid),
                    tagekyc.raw_export_read_current_attempt_key_recovery_context(uuid),
                    tagekyc.raw_export_read_active_attempt_key_envelope(uuid),
                    tagekyc.raw_export_activate_recovered_attempt_key_reservation_internal(uuid,uuid,bigint,bytea,bytea,bytea,bytea,text,bytea)
                    FROM PUBLIC,tagekyc_runtime,
                         tagekyc_raw_export_custody_encryptor,
                         tagekyc_raw_export_reconciler,
                         tagekyc_raw_export_lifecycle;

                GRANT USAGE ON SCHEMA tagekyc TO
                    tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;

                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid),
                    tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text),
                    tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint),
                    tagekyc.raw_export_inspect_attempt_key_reservation(uuid),
                    tagekyc.raw_export_read_active_attempt_key_envelope(uuid)
                    TO tagekyc_raw_export_custody_encryptor;

                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_mark_attempt_key_preparation_expired(uuid,uuid,bigint),
                    tagekyc.raw_export_resolve_attempt_key_provider_outcome(uuid,uuid,bigint,text,text,text,text),
                    tagekyc.raw_export_mark_key_provider_cleanup_required(uuid,uuid,uuid,bigint,text,text),
                    tagekyc.raw_export_record_key_provider_cleanup_observation(uuid,uuid,uuid,bigint,text,text,text,text),
                    tagekyc.raw_export_acknowledge_key_provider_cleanup(uuid,uuid,uuid,bigint,text,text,text,text),
                    tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text),
                    tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text),
                    tagekyc.raw_export_inspect_attempt_key_reservation(uuid),
                    tagekyc.raw_export_read_current_attempt_key_recovery_context(uuid),
                    tagekyc.raw_export_read_active_attempt_key_envelope(uuid)
                    TO tagekyc_raw_export_reconciler;

                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_request_abandon_attempt_key_reservation(uuid,text),
                    tagekyc.raw_export_finalize_abandon_attempt_key_reservation(uuid),
                    tagekyc.raw_export_revoke_attempt_key_reservation(uuid,text)
                    TO tagekyc_raw_export_lifecycle;
                """);
        }
    }
}
