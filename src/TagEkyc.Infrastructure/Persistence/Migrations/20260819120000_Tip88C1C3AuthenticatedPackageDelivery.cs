using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1C3AuthenticatedPackageDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_recipient_package_deliveries",
                schema: "tagekyc",
                columns: table => new
                {
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKeyDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    DeliveryEqualityFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    C2PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageEqualityFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    RecipientKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    RecipientKeyFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    RecipientKeyRevision = table.Column<long>(type: "bigint", nullable: false),
                    PackageRevisionAtAuthorization = table.Column<long>(type: "bigint", nullable: false),
                    EncryptedPackageLength = table.Column<long>(type: "bigint", nullable: false),
                    PackageCiphertextDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    EnvelopeDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    ObjectBindingDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatorApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationCorrelationDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    AuthorizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuthorizationExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StreamAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    DeliveryFence = table.Column<long>(type: "bigint", nullable: false),
                    StreamStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StreamLeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StreamApiKeyId = table.Column<Guid>(type: "uuid", nullable: true),
                    StreamPrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    StreamCorrelationDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    InterruptionKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InterruptionEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    InterruptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IntegrityFailureKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IntegrityEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    IntegrityUnavailableAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OutcomeUnknownAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ServerStreamCompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedByteCount = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedPackageCiphertextDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    DeliveryReceiptDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    ExpiredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_package_delivery", x => x.DeliveryId);
                    table.CheckConstraint("ck_raw_export_recipient_package_delivery_shape", "\"DeliveryId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PackageId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND octet_length(\"IdempotencyKeyDigest\")=32 AND octet_length(\"DeliveryEqualityFingerprint\")=32 AND octet_length(\"PackageEqualityFingerprint\")=32 AND \"RecipientKeyVersion\">0 AND octet_length(\"RecipientKeyFingerprint\")=32 AND \"RecipientKeyRevision\">0 AND \"PackageRevisionAtAuthorization\">0 AND \"EncryptedPackageLength\" BETWEEN 1 AND 33557106 AND octet_length(\"PackageCiphertextDigest\")=32 AND octet_length(\"EnvelopeDigest\")=32 AND octet_length(\"ObjectBindingDigest\")=32 AND \"CreatorApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"CreatorPrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND octet_length(\"AuthorizationCorrelationDigest\")=32 AND \"Revision\">0 AND \"AuthorizationExpiresAtUtc\"=\"AuthorizedAtUtc\"+interval '30 minutes' AND \"StreamAttemptCount\">=0 AND \"DeliveryFence\">=0 AND \"State\" IN ('Authorized','Streaming','Interrupted','IntegrityUnavailable','OutcomeUnknown','ServerStreamCompleted','Expired')");
                    table.CheckConstraint("ck_raw_export_recipient_package_delivery_sparse", "(\"StreamCorrelationDigest\" IS NULL OR octet_length(\"StreamCorrelationDigest\")=32) AND (\"InterruptionEvidenceDigest\" IS NULL OR octet_length(\"InterruptionEvidenceDigest\")=32) AND (\"IntegrityEvidenceDigest\" IS NULL OR octet_length(\"IntegrityEvidenceDigest\")=32) AND (\"VerifiedPackageCiphertextDigest\" IS NULL OR octet_length(\"VerifiedPackageCiphertextDigest\")=32) AND (\"DeliveryReceiptDigest\" IS NULL OR octet_length(\"DeliveryReceiptDigest\")=32) AND ((\"State\"='Authorized' AND \"StreamAttemptCount\"=0 AND \"DeliveryFence\"=0 AND \"StreamStartedAtUtc\" IS NULL AND \"StreamLeaseExpiresAtUtc\" IS NULL AND \"StreamApiKeyId\" IS NULL AND \"StreamPrincipalId\" IS NULL AND \"StreamCorrelationDigest\" IS NULL AND \"InterruptionKind\" IS NULL AND \"InterruptionEvidenceDigest\" IS NULL AND \"InterruptedAtUtc\" IS NULL AND \"IntegrityFailureKind\" IS NULL AND \"IntegrityEvidenceDigest\" IS NULL AND \"IntegrityUnavailableAtUtc\" IS NULL AND \"OutcomeUnknownAtUtc\" IS NULL AND \"ServerStreamCompletedAtUtc\" IS NULL AND \"VerifiedByteCount\" IS NULL AND \"VerifiedPackageCiphertextDigest\" IS NULL AND \"DeliveryReceiptDigest\" IS NULL AND \"ExpiredAtUtc\" IS NULL) OR (\"State\"='Streaming' AND \"StreamAttemptCount\">0 AND \"DeliveryFence\">0 AND \"StreamStartedAtUtc\" IS NOT NULL AND \"StreamLeaseExpiresAtUtc\"=\"StreamStartedAtUtc\"+interval '35 minutes' AND \"StreamApiKeyId\" IS NOT NULL AND \"StreamPrincipalId\" IS NOT NULL AND \"StreamCorrelationDigest\" IS NOT NULL AND \"InterruptionKind\" IS NULL AND \"InterruptionEvidenceDigest\" IS NULL AND \"InterruptedAtUtc\" IS NULL AND \"IntegrityFailureKind\" IS NULL AND \"IntegrityEvidenceDigest\" IS NULL AND \"IntegrityUnavailableAtUtc\" IS NULL AND \"OutcomeUnknownAtUtc\" IS NULL AND \"ServerStreamCompletedAtUtc\" IS NULL AND \"VerifiedByteCount\" IS NULL AND \"VerifiedPackageCiphertextDigest\" IS NULL AND \"DeliveryReceiptDigest\" IS NULL AND \"ExpiredAtUtc\" IS NULL) OR (\"State\"='Interrupted' AND \"StreamAttemptCount\">0 AND \"DeliveryFence\">0 AND \"StreamStartedAtUtc\" IS NOT NULL AND \"StreamLeaseExpiresAtUtc\"=\"StreamStartedAtUtc\"+interval '35 minutes' AND \"StreamApiKeyId\" IS NOT NULL AND \"StreamPrincipalId\" IS NOT NULL AND \"StreamCorrelationDigest\" IS NOT NULL AND \"InterruptionKind\" IS NOT NULL AND \"InterruptionEvidenceDigest\" IS NOT NULL AND \"InterruptedAtUtc\" IS NOT NULL AND \"IntegrityFailureKind\" IS NULL AND \"IntegrityEvidenceDigest\" IS NULL AND \"IntegrityUnavailableAtUtc\" IS NULL AND \"OutcomeUnknownAtUtc\" IS NULL AND \"ServerStreamCompletedAtUtc\" IS NULL AND \"VerifiedByteCount\" IS NULL AND \"VerifiedPackageCiphertextDigest\" IS NULL AND \"DeliveryReceiptDigest\" IS NULL AND \"ExpiredAtUtc\" IS NULL) OR (\"State\"='IntegrityUnavailable' AND \"StreamAttemptCount\">0 AND \"DeliveryFence\">0 AND \"StreamStartedAtUtc\" IS NOT NULL AND \"StreamLeaseExpiresAtUtc\"=\"StreamStartedAtUtc\"+interval '35 minutes' AND \"StreamApiKeyId\" IS NOT NULL AND \"StreamPrincipalId\" IS NOT NULL AND \"StreamCorrelationDigest\" IS NOT NULL AND \"InterruptionKind\" IS NULL AND \"InterruptionEvidenceDigest\" IS NULL AND \"InterruptedAtUtc\" IS NULL AND \"IntegrityFailureKind\" IS NOT NULL AND \"IntegrityEvidenceDigest\" IS NOT NULL AND \"IntegrityUnavailableAtUtc\" IS NOT NULL AND \"OutcomeUnknownAtUtc\" IS NULL AND \"ServerStreamCompletedAtUtc\" IS NULL AND \"VerifiedByteCount\" IS NULL AND \"VerifiedPackageCiphertextDigest\" IS NULL AND \"DeliveryReceiptDigest\" IS NULL AND \"ExpiredAtUtc\" IS NULL) OR (\"State\"='OutcomeUnknown' AND \"StreamAttemptCount\">0 AND \"DeliveryFence\">0 AND \"StreamStartedAtUtc\" IS NOT NULL AND \"StreamLeaseExpiresAtUtc\"=\"StreamStartedAtUtc\"+interval '35 minutes' AND \"StreamApiKeyId\" IS NOT NULL AND \"StreamPrincipalId\" IS NOT NULL AND \"StreamCorrelationDigest\" IS NOT NULL AND \"InterruptionKind\" IS NULL AND \"InterruptionEvidenceDigest\" IS NULL AND \"InterruptedAtUtc\" IS NULL AND \"IntegrityFailureKind\" IS NULL AND \"IntegrityEvidenceDigest\" IS NULL AND \"IntegrityUnavailableAtUtc\" IS NULL AND \"OutcomeUnknownAtUtc\" IS NOT NULL AND \"ServerStreamCompletedAtUtc\" IS NULL AND \"VerifiedByteCount\" IS NULL AND \"VerifiedPackageCiphertextDigest\" IS NULL AND \"DeliveryReceiptDigest\" IS NULL AND \"ExpiredAtUtc\" IS NULL) OR (\"State\"='ServerStreamCompleted' AND \"StreamAttemptCount\">0 AND \"DeliveryFence\">0 AND \"StreamStartedAtUtc\" IS NOT NULL AND \"StreamLeaseExpiresAtUtc\"=\"StreamStartedAtUtc\"+interval '35 minutes' AND \"StreamApiKeyId\" IS NOT NULL AND \"StreamPrincipalId\" IS NOT NULL AND \"StreamCorrelationDigest\" IS NOT NULL AND \"InterruptionKind\" IS NULL AND \"InterruptionEvidenceDigest\" IS NULL AND \"InterruptedAtUtc\" IS NULL AND \"IntegrityFailureKind\" IS NULL AND \"IntegrityEvidenceDigest\" IS NULL AND \"IntegrityUnavailableAtUtc\" IS NULL AND \"OutcomeUnknownAtUtc\" IS NULL AND \"ServerStreamCompletedAtUtc\" IS NOT NULL AND \"VerifiedByteCount\"=\"EncryptedPackageLength\" AND \"VerifiedPackageCiphertextDigest\"=\"PackageCiphertextDigest\" AND \"DeliveryReceiptDigest\" IS NOT NULL AND \"ExpiredAtUtc\" IS NULL) OR (\"State\"='Expired' AND \"ExpiredAtUtc\" IS NOT NULL AND \"IntegrityFailureKind\" IS NULL AND \"IntegrityEvidenceDigest\" IS NULL AND \"IntegrityUnavailableAtUtc\" IS NULL AND \"OutcomeUnknownAtUtc\" IS NULL AND \"ServerStreamCompletedAtUtc\" IS NULL AND \"VerifiedByteCount\" IS NULL AND \"VerifiedPackageCiphertextDigest\" IS NULL AND \"DeliveryReceiptDigest\" IS NULL AND ((\"StreamAttemptCount\"=0 AND \"DeliveryFence\"=0 AND \"StreamStartedAtUtc\" IS NULL AND \"StreamLeaseExpiresAtUtc\" IS NULL AND \"StreamApiKeyId\" IS NULL AND \"StreamPrincipalId\" IS NULL AND \"StreamCorrelationDigest\" IS NULL AND \"InterruptionKind\" IS NULL AND \"InterruptionEvidenceDigest\" IS NULL AND \"InterruptedAtUtc\" IS NULL) OR (\"StreamAttemptCount\">0 AND \"DeliveryFence\">0 AND \"StreamStartedAtUtc\" IS NOT NULL AND \"StreamLeaseExpiresAtUtc\"=\"StreamStartedAtUtc\"+interval '35 minutes' AND \"StreamApiKeyId\" IS NOT NULL AND \"StreamPrincipalId\" IS NOT NULL AND \"StreamCorrelationDigest\" IS NOT NULL AND \"InterruptionKind\" IS NOT NULL AND \"InterruptionEvidenceDigest\" IS NOT NULL AND \"InterruptedAtUtc\" IS NOT NULL))))");
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_delivery_key",
                        columns: x => new { x.RecipientClientApplicationId, x.RecipientKeyId, x.RecipientKeyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_recipient_key_registrations",
                        principalColumns: new[] { "RecipientClientApplicationId", "RecipientKeyId", "RecipientKeyVersion" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_delivery_package",
                        column: x => x.PackageId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_recipient_package_preparations",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_recipient_package_delivery_events",
                schema: "tagekyc",
                columns: table => new
                {
                    DeliveryEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeliveryAttemptNumber = table.Column<int>(type: "integer", nullable: true),
                    DeliveryFence = table.Column<long>(type: "bigint", nullable: true),
                    AuthenticatedApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticatedPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    EvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_package_delivery_event", x => x.DeliveryEventId);
                    table.CheckConstraint("ck_raw_export_recipient_package_delivery_event_shape", "\"DeliveryEventId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"AuthenticatedApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"AuthenticatedPrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"Revision\">0 AND octet_length(\"CorrelationDigest\")=32 AND octet_length(\"EvidenceDigest\")=32 AND ((\"EventType\"='Authorized' AND \"DeliveryAttemptNumber\" IS NULL AND \"DeliveryFence\" IS NULL) OR (\"EventType\" IN ('StreamingStarted','Interrupted','IntegrityUnavailable','OutcomeUnknown','ServerStreamCompleted') AND \"DeliveryAttemptNumber\" IS NOT NULL AND \"DeliveryFence\" IS NOT NULL AND \"DeliveryAttemptNumber\">0 AND \"DeliveryFence\">0) OR (\"EventType\"='Expired' AND ((\"DeliveryAttemptNumber\" IS NULL AND \"DeliveryFence\" IS NULL) OR (\"DeliveryAttemptNumber\" IS NOT NULL AND \"DeliveryFence\" IS NOT NULL AND \"DeliveryAttemptNumber\">0 AND \"DeliveryFence\">0))))");
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_delivery_event_delivery",
                        column: x => x.DeliveryId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_recipient_package_deliveries",
                        principalColumn: "DeliveryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_recipient_package_deliveries_PackageId",
                schema: "tagekyc",
                table: "raw_export_recipient_package_deliveries",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_recipient_package_deliveries_RecipientClientAppl~",
                schema: "tagekyc",
                table: "raw_export_recipient_package_deliveries",
                columns: new[] { "RecipientClientApplicationId", "RecipientKeyId", "RecipientKeyVersion" });

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_recipient_package_delivery_reconcile",
                schema: "tagekyc",
                table: "raw_export_recipient_package_deliveries",
                columns: new[] { "State", "StreamLeaseExpiresAtUtc", "AuthorizationExpiresAtUtc", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_recipient_package_delivery_equality",
                schema: "tagekyc",
                table: "raw_export_recipient_package_deliveries",
                column: "DeliveryEqualityFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_recipient_package_delivery_idempotency",
                schema: "tagekyc",
                table: "raw_export_recipient_package_deliveries",
                columns: new[] { "RecipientClientApplicationId", "IdempotencyKeyDigest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_recipient_package_delivery_event_revision",
                schema: "tagekyc",
                table: "raw_export_recipient_package_delivery_events",
                columns: new[] { "DeliveryId", "Revision" },
                unique: true);

            migrationBuilder.Sql("""
                DO $c3_roles$
                DECLARE role_row pg_catalog.pg_roles%ROWTYPE;
                BEGIN
                  SELECT * INTO role_row FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_package_delivery';
                  IF FOUND THEN
                    IF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolcreatedb OR role_row.rolcreaterole
                       OR role_row.rolreplication OR role_row.rolbypassrls OR NOT role_row.rolinherit THEN
                      RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='PROD_RAW_EXPORT_PACKAGE_DELIVERY_ROLE_TOPOLOGY_INVALID';
                    END IF;
                  ELSE
                    CREATE ROLE tagekyc_raw_export_package_delivery NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_package_delivery_login') THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='PROD_RAW_EXPORT_PACKAGE_DELIVERY_ROLE_TOPOLOGY_INVALID';
                  END IF;
                END $c3_roles$;
                GRANT tagekyc_raw_export_package_delivery TO tagekyc_raw_export_package_delivery_login WITH INHERIT TRUE, SET FALSE;
                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_raw_export_package_delivery;
                ALTER TABLE tagekyc.raw_export_recipient_package_deliveries OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_recipient_package_delivery_events OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON TABLE tagekyc.raw_export_recipient_package_deliveries FROM PUBLIC,tagekyc_raw_export_package_delivery;
                REVOKE ALL ON TABLE tagekyc.raw_export_recipient_package_delivery_events FROM PUBLIC,tagekyc_raw_export_package_delivery;

                CREATE FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event()
                RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                BEGIN
                  RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_RECIPIENT_PACKAGE_DELIVERY_EVENT_APPEND_ONLY';
                END $fn$;
                ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() FROM PUBLIC,tagekyc_raw_export_package_delivery;
                CREATE TRIGGER trg_raw_export_recipient_package_delivery_event_append_only
                  BEFORE UPDATE OR DELETE ON tagekyc.raw_export_recipient_package_delivery_events
                  FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event();

                CREATE FUNCTION tagekyc.raw_export_read_recipient_package_delivery(p_recipient uuid,p_delivery uuid)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                  SELECT CASE WHEN d."DeliveryId" IS NULL THEN 'NotFound' ELSE 'ExistingMatch' END::text,
                    d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",
                    d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",d."StreamAttemptCount",d."DeliveryFence",
                    d."StreamStartedAtUtc",d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",
                    d."PackageCiphertextDigest",d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,
                    p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest"
                  FROM (SELECT 1) seed
                  LEFT JOIN tagekyc.raw_export_recipient_package_deliveries d
                    ON d."DeliveryId"=p_delivery AND d."RecipientClientApplicationId"=p_recipient
                  LEFT JOIN tagekyc.raw_export_recipient_package_preparations p ON p."PackageId"=d."PackageId";
                $fn$;

                CREATE FUNCTION tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient uuid,p_delivery uuid)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                  SELECT CASE
                    WHEN d."DeliveryId" IS NULL THEN 'NotFound'
                    WHEN d."State"='Authorized' THEN 'Authorized'
                    WHEN d."State"='Interrupted' THEN 'Interrupted'
                    WHEN d."State"='Streaming' THEN 'InProgress'
                    WHEN d."State"='Expired' THEN 'Expired'
                    WHEN d."State"='IntegrityUnavailable' THEN 'IntegrityUnavailable'
                    WHEN d."State"='OutcomeUnknown' THEN 'OutcomeUnknown'
                    WHEN d."State"='ServerStreamCompleted' THEN 'Completed'
                    ELSE 'Unavailable' END::text,
                    d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",
                    d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",d."StreamAttemptCount",d."DeliveryFence",
                    d."StreamStartedAtUtc",d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",
                    d."PackageCiphertextDigest",d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,
                    p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest"
                  FROM (SELECT 1) seed
                  LEFT JOIN tagekyc.raw_export_recipient_package_deliveries d
                    ON d."DeliveryId"=p_delivery AND d."RecipientClientApplicationId"=p_recipient
                  LEFT JOIN tagekyc.raw_export_recipient_package_preparations p ON p."PackageId"=d."PackageId";
                $fn$;

                CREATE FUNCTION tagekyc.raw_export_create_recipient_package_delivery(
                  p_recipient uuid,p_package uuid,p_delivery uuid,p_idempotency bytea,p_api_key uuid,p_principal uuid,p_correlation bytea)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE k tagekyc.raw_export_recipient_key_registrations%ROWTYPE; p tagekyc.raw_export_recipient_package_preparations%ROWTYPE;
                  d tagekyc.raw_export_recipient_package_deliveries%ROWTYPE; now_utc timestamptz; equality bytea; evidence bytea;
                  event_bytes bytea; event_id uuid; ts_authorized text; ts_expires text;
                BEGIN
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x
                    WHERE x."RecipientClientApplicationId"=p_recipient AND x."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    outcome:=CASE WHEN d."PackageId"=p_package THEN 'ExistingMatch' ELSE 'IdempotencyConflict' END;
                    RETURN QUERY SELECT outcome,d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",
                      d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",d."StreamAttemptCount",d."DeliveryFence",d."StreamStartedAtUtc",
                      d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",d."PackageCiphertextDigest",
                      d."EnvelopeDigest",d."ObjectBindingDigest",rp."ProviderConfigurationId"::text,rp."BucketName"::text,rp."ObjectKey"::text,d."DeliveryReceiptDigest"
                    FROM tagekyc.raw_export_recipient_package_preparations rp WHERE rp."PackageId"=d."PackageId"; RETURN;
                  END IF;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x
                    WHERE x."PackageId"=p_package AND x."RecipientClientApplicationId"=p_recipient;
                  IF NOT FOUND OR p."State"<>'Finalized' OR p."EnvelopeDigest" IS NULL OR p."EncryptedPackageLength" IS NULL
                    OR p."PackageCiphertextDigest" IS NULL THEN
                    RETURN QUERY SELECT * FROM tagekyc.raw_export_read_recipient_package_delivery(p_recipient,p_delivery); RETURN;
                  END IF;
                  SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x
                    WHERE x."RecipientClientApplicationId"=p."RecipientClientApplicationId" AND x."RecipientKeyId"=p."RecipientKeyId"
                      AND x."RecipientKeyVersion"=p."RecipientKeyVersion" FOR UPDATE;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=p_package FOR UPDATE;
                  now_utc:=pg_catalog.clock_timestamp();
                  IF k."State"<>'Active' OR k."PublicKeyFingerprint" IS DISTINCT FROM p."RecipientKeyFingerprint"
                    OR k."Revision"<>p."RecipientKeyRevision" OR k."ValidFromUtc">now_utc OR k."ValidUntilUtc"<=now_utc
                    OR p."State"<>'Finalized' OR p."RecipientClientApplicationId"<>p_recipient THEN
                    RETURN QUERY SELECT 'Ineligible'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz,NULL::timestamptz,
                      NULL::integer,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::timestamptz,NULL::bigint,NULL::bytea,NULL::bytea,
                      NULL::bytea,NULL::text,NULL::text,NULL::text,NULL::bytea; RETURN;
                  END IF;
                  equality:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-equality-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-equality-v1','UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_delivery::text,'-',''),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_package::text,'-',''),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_idempotency,'hex'),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p."PackageEqualityFingerprint",'hex'),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p."C2PreparationId"::text,'-',''),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p."RecipientKeyId",'UTF8')))||pg_catalog.convert_to(p."RecipientKeyId",'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p."RecipientKeyVersion"::text,'UTF8')))||pg_catalog.convert_to(p."RecipientKeyVersion"::text,'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p."RecipientKeyFingerprint",'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p."RecipientKeyRevision"::text,'UTF8')))||pg_catalog.convert_to(p."RecipientKeyRevision"::text,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p."EncryptedPackageLength"::text,'UTF8')))||pg_catalog.convert_to(p."EncryptedPackageLength"::text,'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p."PackageCiphertextDigest",'hex'),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p."EnvelopeDigest",'hex'),'UTF8'),'sha256');
                  ts_authorized:=pg_catalog.to_char(now_utc AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                  ts_expires:=pg_catalog.to_char((now_utc+interval '30 minutes') AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                  evidence:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-authorization-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-authorization-v1','UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_delivery::text,'-',''),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_package::text,'-',''),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(equality,'hex'),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_api_key::text,'-',''),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_principal::text,'-',''),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_correlation,'hex'),'UTF8')||
                    pg_catalog.int4send(28)||pg_catalog.convert_to(ts_authorized,'UTF8')||
                    pg_catalog.int4send(28)||pg_catalog.convert_to(ts_expires,'UTF8'),'sha256');
                  INSERT INTO tagekyc.raw_export_recipient_package_deliveries(
                    "DeliveryId","PackageId","RecipientClientApplicationId","IdempotencyKeyDigest","DeliveryEqualityFingerprint",
                    "C2PreparationId","PackageEqualityFingerprint","RecipientKeyId","RecipientKeyVersion","RecipientKeyFingerprint",
                    "RecipientKeyRevision","PackageRevisionAtAuthorization","EncryptedPackageLength","PackageCiphertextDigest","EnvelopeDigest",
                    "ObjectBindingDigest","CreatorApiKeyId","CreatorPrincipalId","AuthorizationCorrelationDigest","State","Revision",
                    "AuthorizedAtUtc","AuthorizationExpiresAtUtc","StreamAttemptCount","DeliveryFence")
                  VALUES(p_delivery,p_package,p_recipient,p_idempotency,equality,p."C2PreparationId",p."PackageEqualityFingerprint",p."RecipientKeyId",
                    p."RecipientKeyVersion",p."RecipientKeyFingerprint",p."RecipientKeyRevision",p."Revision",p."EncryptedPackageLength",
                    p."PackageCiphertextDigest",p."EnvelopeDigest",p."ObjectBindingDigest",p_api_key,p_principal,p_correlation,'Authorized',1,
                    now_utc,now_utc+interval '30 minutes',0,0) ON CONFLICT DO NOTHING;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x
                    WHERE x."RecipientClientApplicationId"=p_recipient AND x."IdempotencyKeyDigest"=p_idempotency;
                  IF d."PackageId"<>p_package OR d."DeliveryEqualityFingerprint" IS DISTINCT FROM equality THEN outcome:='IdempotencyConflict';
                  ELSIF d."Revision"=1 AND d."AuthorizedAtUtc"=now_utc THEN
                    event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                      'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":1}',
                      'UTF8'),'sha256'),1,16);
                    event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);
                    event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                    event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||
                      pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||
                      pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||
                      pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                    INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(
                      event_id,p_delivery,p_recipient,1,'Authorized',NULL,NULL,
                      p_api_key,p_principal,p_correlation,evidence,now_utc); outcome:='Created';
                  ELSE outcome:='ExistingMatch'; END IF;
                  RETURN QUERY SELECT outcome,d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",
                    d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",d."StreamAttemptCount",d."DeliveryFence",d."StreamStartedAtUtc",
                    d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",d."PackageCiphertextDigest",
                    d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest";
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_begin_recipient_package_delivery_stream(
                  p_recipient uuid,p_delivery uuid,p_api_key uuid,p_principal uuid,p_correlation bytea)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE probe record;k tagekyc.raw_export_recipient_key_registrations%ROWTYPE;p tagekyc.raw_export_recipient_package_preparations%ROWTYPE;
                  d tagekyc.raw_export_recipient_package_deliveries%ROWTYPE;now_utc timestamptz;evidence bytea;
                  event_bytes bytea;event_id uuid;ts_started text;ts_lease text;predecessor text;ts_expires text;ts_event text;
                  actor_api uuid;actor_principal uuid;actor_correlation bytea;
                BEGIN
                  SELECT "PackageId","RecipientKeyId","RecipientKeyVersion" INTO probe FROM tagekyc.raw_export_recipient_package_deliveries
                    WHERE "DeliveryId"=p_delivery AND "RecipientClientApplicationId"=p_recipient;
                  IF NOT FOUND THEN RETURN QUERY SELECT * FROM tagekyc.raw_export_probe_recipient_package_delivery_content(p_recipient,p_delivery);RETURN;END IF;
                  SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x WHERE x."RecipientClientApplicationId"=p_recipient
                    AND x."RecipientKeyId"=probe."RecipientKeyId" AND x."RecipientKeyVersion"=probe."RecipientKeyVersion" FOR UPDATE;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=probe."PackageId" FOR UPDATE;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery FOR UPDATE;
                  now_utc:=pg_catalog.clock_timestamp();
                  IF d."State"='Streaming' AND d."StreamLeaseExpiresAtUtc">now_utc THEN outcome:='InProgress';
                  ELSIF d."State" NOT IN ('Authorized','Interrupted') THEN outcome:=CASE d."State" WHEN 'Expired' THEN 'Expired' WHEN 'IntegrityUnavailable' THEN 'IntegrityUnavailable' WHEN 'OutcomeUnknown' THEN 'OutcomeUnknown' WHEN 'ServerStreamCompleted' THEN 'Completed' ELSE 'Terminal' END;
                  ELSIF d."AuthorizationExpiresAtUtc"<=now_utc THEN
                    predecessor:=d."State";d."Revision":=d."Revision"+1;
                    actor_api:=CASE WHEN predecessor='Authorized' THEN d."CreatorApiKeyId" ELSE d."StreamApiKeyId" END;
                    actor_principal:=CASE WHEN predecessor='Authorized' THEN d."CreatorPrincipalId" ELSE d."StreamPrincipalId" END;
                    actor_correlation:=CASE WHEN predecessor='Authorized' THEN d."AuthorizationCorrelationDigest" ELSE d."StreamCorrelationDigest" END;
                    ts_expires:=pg_catalog.to_char(d."AuthorizationExpiresAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    ts_event:=pg_catalog.to_char(now_utc AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-expiry-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-expiry-v1','UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(predecessor,'UTF8')))||pg_catalog.convert_to(predecessor,'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(actor_api::text,'-',''),'UTF8')||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(actor_principal::text,'-',''),'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(actor_correlation,'hex'),'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_expires,'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_event,'UTF8'),'sha256');
                    UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='Expired',"Revision"=d."Revision","ExpiredAtUtc"=now_utc WHERE "DeliveryId"=p_delivery;
                    event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                      'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":'||d."Revision"::text||'}','UTF8'),'sha256'),1,16);
                    event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                    event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                    INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(event_id,
                      p_delivery,d."RecipientClientApplicationId",d."Revision",'Expired',
                      CASE WHEN d."StreamAttemptCount">0 THEN d."StreamAttemptCount" ELSE NULL END,CASE WHEN d."DeliveryFence">0 THEN d."DeliveryFence" ELSE NULL END,
                      actor_api,actor_principal,actor_correlation,evidence,now_utc);outcome:='Expired';
                  ELSIF k."State"<>'Active' OR k."Revision"<>d."RecipientKeyRevision" OR k."PublicKeyFingerprint" IS DISTINCT FROM d."RecipientKeyFingerprint"
                    OR k."ValidFromUtc">now_utc OR k."ValidUntilUtc"<=now_utc OR p."State"<>'Finalized' OR p."Revision"<>d."PackageRevisionAtAuthorization" THEN outcome:='Ineligible';
                  ELSE
                    d."Revision":=d."Revision"+1; d."StreamAttemptCount":=d."StreamAttemptCount"+1;d."DeliveryFence":=d."DeliveryFence"+1;
                    UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='Streaming',"Revision"=d."Revision",
                      "StreamAttemptCount"=d."StreamAttemptCount","DeliveryFence"=d."DeliveryFence","StreamStartedAtUtc"=now_utc,
                      "StreamLeaseExpiresAtUtc"=now_utc+interval '35 minutes',"StreamApiKeyId"=p_api_key,"StreamPrincipalId"=p_principal,
                      "StreamCorrelationDigest"=p_correlation,"InterruptionKind"=NULL,"InterruptionEvidenceDigest"=NULL,"InterruptedAtUtc"=NULL
                    WHERE "DeliveryId"=p_delivery;
                    ts_started:=pg_catalog.to_char(now_utc AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    ts_lease:=pg_catalog.to_char((now_utc+interval '35 minutes') AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-stream-admission-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-stream-admission-v1','UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_delivery::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')))||pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')))||pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_api_key::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_principal::text,'-',''),'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_correlation,'hex'),'UTF8')||
                      pg_catalog.int4send(28)||pg_catalog.convert_to(ts_started,'UTF8')||
                      pg_catalog.int4send(28)||pg_catalog.convert_to(ts_lease,'UTF8'),'sha256');
                    event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                      'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":'||d."Revision"::text||'}',
                      'UTF8'),'sha256'),1,16);
                    event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);
                    event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                    event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||
                      pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||
                      pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||
                      pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                    INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(
                      event_id,p_delivery,p_recipient,d."Revision",'StreamingStarted',
                      d."StreamAttemptCount",d."DeliveryFence",p_api_key,p_principal,p_correlation,evidence,now_utc); outcome:='Started';
                  END IF;
                  RETURN QUERY SELECT outcome,x."DeliveryId",x."PackageId",x."RecipientClientApplicationId",x."State"::text,x."Revision",
                    x."AuthorizedAtUtc",x."AuthorizationExpiresAtUtc",x."StreamAttemptCount",x."DeliveryFence",x."StreamStartedAtUtc",x."StreamLeaseExpiresAtUtc",
                    x."ServerStreamCompletedAtUtc",x."EncryptedPackageLength",x."PackageCiphertextDigest",x."EnvelopeDigest",x."ObjectBindingDigest",
                    p."ProviderConfigurationId"::text,p."BucketName"::text,p."ObjectKey"::text,x."DeliveryReceiptDigest"
                  FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_recipient_package_delivery_interrupted(
                  p_delivery uuid,p_revision bigint,p_fence bigint,p_kind text,p_evidence bytea)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE d tagekyc.raw_export_recipient_package_deliveries%ROWTYPE;p tagekyc.raw_export_recipient_package_preparations%ROWTYPE;t timestamptz;
                  obs jsonb;evidence bytea;event_bytes bytea;event_id uuid;ts_event text;
                BEGIN
                  BEGIN obs:=pg_catalog.convert_from(p_evidence,'UTF8')::jsonb; EXCEPTION WHEN others THEN obs:=NULL; END;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery FOR UPDATE;
                  IF NOT FOUND THEN outcome:='StateConflict';
                  ELSIF d."State"='Interrupted' AND d."Revision"=p_revision+1 AND d."DeliveryFence"=p_fence
                    AND d."InterruptionKind"=p_kind AND obs IS NOT NULL
                    AND obs ?& ARRAY['observedLength','observedLengthPresent','observedPackageDigest','observedPackageDigestPresent','observedEnvelopeDigest','observedEnvelopeDigestPresent']
                    AND NOT ((obs->>'observedLengthPresent')::boolean AND (obs->>'observedLength') !~ '^[0-9]+$')
                    AND NOT ((obs->>'observedPackageDigestPresent')::boolean AND (obs->>'observedPackageDigest') !~ '^[0-9a-f]{64}$')
                    AND NOT ((obs->>'observedEnvelopeDigestPresent')::boolean AND (obs->>'observedEnvelopeDigest') !~ '^[0-9a-f]{64}$') THEN outcome:='ExistingMatch';
                  ELSIF d."State"<>'Streaming' OR d."Revision"<>p_revision OR d."DeliveryFence"<>p_fence OR obs IS NULL
                    OR NOT (obs ?& ARRAY['observedLength','observedLengthPresent','observedPackageDigest','observedPackageDigestPresent','observedEnvelopeDigest','observedEnvelopeDigestPresent'])
                    OR ((obs->>'observedLengthPresent')::boolean AND (obs->>'observedLength') !~ '^[0-9]+$')
                    OR ((obs->>'observedPackageDigestPresent')::boolean AND (obs->>'observedPackageDigest') !~ '^[0-9a-f]{64}$')
                    OR ((obs->>'observedEnvelopeDigestPresent')::boolean AND (obs->>'observedEnvelopeDigest') !~ '^[0-9a-f]{64}$') THEN outcome:='StateConflict';
                  ELSE t:=pg_catalog.clock_timestamp();ts_event:=pg_catalog.to_char(t AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-interruption-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-interruption-v1','UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')))||pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')))||pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_kind,'UTF8')))||pg_catalog.convert_to(p_kind,'UTF8')||
                      pg_catalog.int4send(1)||pg_catalog.convert_to(CASE WHEN (obs->>'observedLengthPresent')::boolean THEN '1' ELSE '0' END,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(CASE WHEN (obs->>'observedLengthPresent')::boolean THEN obs->>'observedLength' ELSE '' END,'UTF8')))||pg_catalog.convert_to(CASE WHEN (obs->>'observedLengthPresent')::boolean THEN obs->>'observedLength' ELSE '' END,'UTF8')||
                      pg_catalog.int4send(1)||pg_catalog.convert_to(CASE WHEN (obs->>'observedPackageDigestPresent')::boolean THEN '1' ELSE '0' END,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(CASE WHEN (obs->>'observedPackageDigestPresent')::boolean THEN obs->>'observedPackageDigest' ELSE '' END,'UTF8')))||pg_catalog.convert_to(CASE WHEN (obs->>'observedPackageDigestPresent')::boolean THEN obs->>'observedPackageDigest' ELSE '' END,'UTF8')||
                      pg_catalog.int4send(1)||pg_catalog.convert_to(CASE WHEN (obs->>'observedEnvelopeDigestPresent')::boolean THEN '1' ELSE '0' END,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(CASE WHEN (obs->>'observedEnvelopeDigestPresent')::boolean THEN obs->>'observedEnvelopeDigest' ELSE '' END,'UTF8')))||pg_catalog.convert_to(CASE WHEN (obs->>'observedEnvelopeDigestPresent')::boolean THEN obs->>'observedEnvelopeDigest' ELSE '' END,'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(d."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(28)||pg_catalog.convert_to(ts_event,'UTF8'),'sha256');
                    d."Revision":=d."Revision"+1;UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='Interrupted',"Revision"=d."Revision",
                    "InterruptionKind"=p_kind,"InterruptionEvidenceDigest"=evidence,"InterruptedAtUtc"=t WHERE "DeliveryId"=p_delivery;
                    event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                      'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":'||d."Revision"::text||'}','UTF8'),'sha256'),1,16);
                    event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                    event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                    INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(event_id,
                      p_delivery,d."RecipientClientApplicationId",d."Revision",'Interrupted',d."StreamAttemptCount",d."DeliveryFence",d."StreamApiKeyId",d."StreamPrincipalId",d."StreamCorrelationDigest",evidence,t);outcome:='Interrupted';END IF;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=d."PackageId";
                  RETURN QUERY SELECT outcome,d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",
                    d."StreamAttemptCount",d."DeliveryFence",d."StreamStartedAtUtc",d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",
                    d."PackageCiphertextDigest",d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest";
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_recipient_package_integrity_unavailable(
                  p_delivery uuid,p_revision bigint,p_fence bigint,p_kind text,p_evidence bytea)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE d tagekyc.raw_export_recipient_package_deliveries%ROWTYPE;p tagekyc.raw_export_recipient_package_preparations%ROWTYPE;t timestamptz;
                  obs jsonb;evidence bytea;event_bytes bytea;event_id uuid;ts_event text;
                BEGIN
                  BEGIN obs:=pg_catalog.convert_from(p_evidence,'UTF8')::jsonb; EXCEPTION WHEN others THEN obs:=NULL; END;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery FOR UPDATE;
                  IF NOT FOUND THEN outcome:='StateConflict';
                  ELSIF d."State"='IntegrityUnavailable' AND d."Revision"=p_revision+1 AND d."DeliveryFence"=p_fence
                    AND d."IntegrityFailureKind"=p_kind AND obs IS NOT NULL
                    AND obs ?& ARRAY['observedLength','observedLengthPresent','observedPackageDigest','observedPackageDigestPresent','observedEnvelopeDigest','observedEnvelopeDigestPresent']
                    AND NOT ((obs->>'observedLengthPresent')::boolean AND (obs->>'observedLength') !~ '^[0-9]+$')
                    AND NOT ((obs->>'observedPackageDigestPresent')::boolean AND (obs->>'observedPackageDigest') !~ '^[0-9a-f]{64}$')
                    AND NOT ((obs->>'observedEnvelopeDigestPresent')::boolean AND (obs->>'observedEnvelopeDigest') !~ '^[0-9a-f]{64}$') THEN outcome:='ExistingMatch';
                  ELSIF d."State"<>'Streaming' OR d."Revision"<>p_revision OR d."DeliveryFence"<>p_fence OR obs IS NULL
                    OR NOT (obs ?& ARRAY['observedLength','observedLengthPresent','observedPackageDigest','observedPackageDigestPresent','observedEnvelopeDigest','observedEnvelopeDigestPresent'])
                    OR ((obs->>'observedLengthPresent')::boolean AND (obs->>'observedLength') !~ '^[0-9]+$')
                    OR ((obs->>'observedPackageDigestPresent')::boolean AND (obs->>'observedPackageDigest') !~ '^[0-9a-f]{64}$')
                    OR ((obs->>'observedEnvelopeDigestPresent')::boolean AND (obs->>'observedEnvelopeDigest') !~ '^[0-9a-f]{64}$') THEN outcome:='StateConflict';
                  ELSE t:=pg_catalog.clock_timestamp();ts_event:=pg_catalog.to_char(t AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-integrity-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-integrity-v1','UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')))||pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')))||pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_kind,'UTF8')))||pg_catalog.convert_to(p_kind,'UTF8')||
                      pg_catalog.int4send(1)||pg_catalog.convert_to(CASE WHEN (obs->>'observedLengthPresent')::boolean THEN '1' ELSE '0' END,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(CASE WHEN (obs->>'observedLengthPresent')::boolean THEN obs->>'observedLength' ELSE '' END,'UTF8')))||pg_catalog.convert_to(CASE WHEN (obs->>'observedLengthPresent')::boolean THEN obs->>'observedLength' ELSE '' END,'UTF8')||
                      pg_catalog.int4send(1)||pg_catalog.convert_to(CASE WHEN (obs->>'observedPackageDigestPresent')::boolean THEN '1' ELSE '0' END,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(CASE WHEN (obs->>'observedPackageDigestPresent')::boolean THEN obs->>'observedPackageDigest' ELSE '' END,'UTF8')))||pg_catalog.convert_to(CASE WHEN (obs->>'observedPackageDigestPresent')::boolean THEN obs->>'observedPackageDigest' ELSE '' END,'UTF8')||
                      pg_catalog.int4send(1)||pg_catalog.convert_to(CASE WHEN (obs->>'observedEnvelopeDigestPresent')::boolean THEN '1' ELSE '0' END,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(CASE WHEN (obs->>'observedEnvelopeDigestPresent')::boolean THEN obs->>'observedEnvelopeDigest' ELSE '' END,'UTF8')))||pg_catalog.convert_to(CASE WHEN (obs->>'observedEnvelopeDigestPresent')::boolean THEN obs->>'observedEnvelopeDigest' ELSE '' END,'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(d."ObjectBindingDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(28)||pg_catalog.convert_to(ts_event,'UTF8'),'sha256');
                    d."Revision":=d."Revision"+1;UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='IntegrityUnavailable',"Revision"=d."Revision",
                    "IntegrityFailureKind"=p_kind,"IntegrityEvidenceDigest"=evidence,"IntegrityUnavailableAtUtc"=t WHERE "DeliveryId"=p_delivery;
                    event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                      'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":'||d."Revision"::text||'}','UTF8'),'sha256'),1,16);
                    event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                    event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                    INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(event_id,
                      p_delivery,d."RecipientClientApplicationId",d."Revision",'IntegrityUnavailable',d."StreamAttemptCount",d."DeliveryFence",d."StreamApiKeyId",d."StreamPrincipalId",d."StreamCorrelationDigest",evidence,t);outcome:='IntegrityUnavailable';END IF;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=d."PackageId";
                  RETURN QUERY SELECT outcome,d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",
                    d."StreamAttemptCount",d."DeliveryFence",d."StreamStartedAtUtc",d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",
                    d."PackageCiphertextDigest",d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest";
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_complete_recipient_package_delivery(
                  p_delivery uuid,p_revision bigint,p_fence bigint,p_length bigint,p_digest bytea)
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE d tagekyc.raw_export_recipient_package_deliveries%ROWTYPE;p tagekyc.raw_export_recipient_package_preparations%ROWTYPE;t timestamptz;receipt bytea;evidence bytea;
                  ts_authorized text;ts_started text;ts_completed text;ts_lease text;event_bytes bytea;event_id uuid;
                BEGIN
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery FOR UPDATE;
                  IF NOT FOUND THEN outcome:='StateConflict'; ELSIF d."State"='ServerStreamCompleted' AND d."VerifiedByteCount"=p_length AND d."VerifiedPackageCiphertextDigest" IS NOT DISTINCT FROM p_digest THEN outcome:='ExistingMatch';
                  ELSIF d."State"<>'Streaming' OR d."Revision"<>p_revision OR d."DeliveryFence"<>p_fence OR p_length<>d."EncryptedPackageLength" OR p_digest IS DISTINCT FROM d."PackageCiphertextDigest" THEN outcome:='StateConflict';
                  ELSE t:=pg_catalog.clock_timestamp();
                    IF t>=d."StreamLeaseExpiresAtUtc" THEN
                      ts_started:=pg_catalog.to_char(d."StreamStartedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                      ts_lease:=pg_catalog.to_char(d."StreamLeaseExpiresAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                      ts_completed:=pg_catalog.to_char(t AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                      evidence:=tagekyc_extensions.digest(
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-outcome-unknown-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-outcome-unknown-v1','UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')))||pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')||
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')))||pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."StreamApiKeyId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."StreamPrincipalId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(d."StreamCorrelationDigest",'hex'),'UTF8')||
                        pg_catalog.int4send(28)||pg_catalog.convert_to(ts_started,'UTF8')||
                        pg_catalog.int4send(28)||pg_catalog.convert_to(ts_lease,'UTF8')||
                        pg_catalog.int4send(28)||pg_catalog.convert_to(ts_completed,'UTF8')||
                        pg_catalog.int4send(40)||pg_catalog.convert_to('LeaseExpiredAfterRestartOrLateCompletion','UTF8'),'sha256');
                      d."Revision":=d."Revision"+1;UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='OutcomeUnknown',"Revision"=d."Revision","OutcomeUnknownAtUtc"=t WHERE "DeliveryId"=p_delivery;
                      event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                        'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":'||d."Revision"::text||'}','UTF8'),'sha256'),1,16);
                      event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                      event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                      INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(event_id,p_delivery,d."RecipientClientApplicationId",d."Revision",'OutcomeUnknown',d."StreamAttemptCount",d."DeliveryFence",d."StreamApiKeyId",d."StreamPrincipalId",d."StreamCorrelationDigest",evidence,t);
                      outcome:='OutcomeUnknown';
                    ELSE
                      ts_authorized:=pg_catalog.to_char(d."AuthorizedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                      ts_started:=pg_catalog.to_char(d."StreamStartedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                      ts_completed:=pg_catalog.to_char(t AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                      receipt:=tagekyc_extensions.digest(
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-receipt-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-receipt-v1','UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')))||pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')||
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')))||pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')||
                        pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(d."PackageCiphertextDigest",'hex'),'UTF8')||
                        pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."EncryptedPackageLength"::text,'UTF8')))||pg_catalog.convert_to(d."EncryptedPackageLength"::text,'UTF8')||
                        pg_catalog.int4send(28)||pg_catalog.convert_to(ts_authorized,'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_started,'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_completed,'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."StreamApiKeyId"::text,'-',''),'UTF8')||
                        pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."StreamPrincipalId"::text,'-',''),'UTF8'),'sha256');
                      d."Revision":=d."Revision"+1;UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='ServerStreamCompleted',"Revision"=d."Revision",
                        "ServerStreamCompletedAtUtc"=t,"VerifiedByteCount"=p_length,"VerifiedPackageCiphertextDigest"=p_digest,"DeliveryReceiptDigest"=receipt WHERE "DeliveryId"=p_delivery;
                      event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                        'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(p_delivery::text,'-','')||'","revision":'||d."Revision"::text||'}','UTF8'),'sha256'),1,16);
                      event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                      event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                      INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(event_id,
                        p_delivery,d."RecipientClientApplicationId",d."Revision",'ServerStreamCompleted',d."StreamAttemptCount",d."DeliveryFence",d."StreamApiKeyId",d."StreamPrincipalId",d."StreamCorrelationDigest",receipt,t);outcome:='Completed';
                    END IF;
                  END IF;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=p_delivery;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=d."PackageId";
                  RETURN QUERY SELECT outcome,d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",
                    d."StreamAttemptCount",d."DeliveryFence",d."StreamStartedAtUtc",d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",
                    d."PackageCiphertextDigest",d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest";
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_reconcile_next_recipient_package_delivery()
                RETURNS TABLE(outcome text,delivery_id uuid,package_id uuid,recipient_client_application_id uuid,state text,revision bigint,
                  authorized_at_utc timestamptz,authorization_expires_at_utc timestamptz,stream_attempt_count integer,delivery_fence bigint,
                  stream_started_at_utc timestamptz,stream_lease_expires_at_utc timestamptz,server_stream_completed_at_utc timestamptz,
                  encrypted_package_length bigint,package_ciphertext_digest bytea,envelope_digest bytea,object_binding_digest bytea,
                  provider_configuration_id text,bucket_name text,object_key text,delivery_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE probe record;k tagekyc.raw_export_recipient_key_registrations%ROWTYPE;
                  d tagekyc.raw_export_recipient_package_deliveries%ROWTYPE;p tagekyc.raw_export_recipient_package_preparations%ROWTYPE;
                  t timestamptz;evidence bytea;event_bytes bytea;event_id uuid;predecessor text;
                  ts_started text;ts_lease text;ts_event text;ts_expires text;actor_api uuid;actor_principal uuid;actor_correlation bytea;
                BEGIN
                  SELECT x."DeliveryId",x."PackageId",x."RecipientClientApplicationId",x."RecipientKeyId",x."RecipientKeyVersion" INTO probe
                  FROM tagekyc.raw_export_recipient_package_deliveries x WHERE
                    (x."State"='Streaming' AND x."StreamLeaseExpiresAtUtc"<=pg_catalog.clock_timestamp()) OR
                    (x."State" IN ('Authorized','Interrupted') AND x."AuthorizationExpiresAtUtc"<=pg_catalog.clock_timestamp())
                    ORDER BY COALESCE(x."StreamLeaseExpiresAtUtc",x."AuthorizationExpiresAtUtc"),x."DeliveryId" LIMIT 1;
                  IF NOT FOUND THEN
                    RETURN QUERY SELECT 'None'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::integer,NULL::bigint,
                      NULL::timestamptz,NULL::timestamptz,NULL::timestamptz,NULL::bigint,NULL::bytea,NULL::bytea,NULL::bytea,NULL::text,NULL::text,NULL::text,NULL::bytea;RETURN;
                  END IF;
                  SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x WHERE x."RecipientClientApplicationId"=probe."RecipientClientApplicationId"
                    AND x."RecipientKeyId"=probe."RecipientKeyId" AND x."RecipientKeyVersion"=probe."RecipientKeyVersion" FOR UPDATE;
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=probe."PackageId" FOR UPDATE;
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=probe."DeliveryId" FOR UPDATE SKIP LOCKED;
                  t:=pg_catalog.clock_timestamp();
                  IF NOT FOUND OR NOT ((d."State"='Streaming' AND d."StreamLeaseExpiresAtUtc"<=t) OR
                    (d."State" IN ('Authorized','Interrupted') AND d."AuthorizationExpiresAtUtc"<=t)) THEN
                    RETURN QUERY SELECT 'None'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::integer,NULL::bigint,
                      NULL::timestamptz,NULL::timestamptz,NULL::timestamptz,NULL::bigint,NULL::bytea,NULL::bytea,NULL::bytea,NULL::text,NULL::text,NULL::text,NULL::bytea;RETURN;
                  END IF;
                  predecessor:=d."State";d."Revision":=d."Revision"+1;ts_event:=pg_catalog.to_char(t AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                  IF predecessor='Streaming' THEN outcome:='OutcomeUnknown';
                    ts_started:=pg_catalog.to_char(d."StreamStartedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    ts_lease:=pg_catalog.to_char(d."StreamLeaseExpiresAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-outcome-unknown-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-outcome-unknown-v1','UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')))||pg_catalog.convert_to(d."StreamAttemptCount"::text,'UTF8')||
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')))||pg_catalog.convert_to(d."DeliveryFence"::text,'UTF8')||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."StreamApiKeyId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."StreamPrincipalId"::text,'-',''),'UTF8')||pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(d."StreamCorrelationDigest",'hex'),'UTF8')||
                      pg_catalog.int4send(28)||pg_catalog.convert_to(ts_started,'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_lease,'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_event,'UTF8')||
                      pg_catalog.int4send(40)||pg_catalog.convert_to('LeaseExpiredAfterRestartOrLateCompletion','UTF8'),'sha256');
                    UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='OutcomeUnknown',"Revision"=d."Revision","OutcomeUnknownAtUtc"=t WHERE "DeliveryId"=d."DeliveryId";
                    actor_api:=d."StreamApiKeyId";actor_principal:=d."StreamPrincipalId";actor_correlation:=d."StreamCorrelationDigest";
                  ELSE outcome:='Expired';
                    actor_api:=CASE WHEN predecessor='Authorized' THEN d."CreatorApiKeyId" ELSE d."StreamApiKeyId" END;
                    actor_principal:=CASE WHEN predecessor='Authorized' THEN d."CreatorPrincipalId" ELSE d."StreamPrincipalId" END;
                    actor_correlation:=CASE WHEN predecessor='Authorized' THEN d."AuthorizationCorrelationDigest" ELSE d."StreamCorrelationDigest" END;
                    ts_expires:=pg_catalog.to_char(d."AuthorizationExpiresAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z';
                    evidence:=tagekyc_extensions.digest(
                      pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c3-delivery-expiry-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c3-delivery-expiry-v1','UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."DeliveryId"::text,'-',''),'UTF8')||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."PackageId"::text,'-',''),'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(d."RecipientClientApplicationId"::text,'-',''),'UTF8')||pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(predecessor,'UTF8')))||pg_catalog.convert_to(predecessor,'UTF8')||
                      pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(actor_api::text,'-',''),'UTF8')||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(actor_principal::text,'-',''),'UTF8')||
                      pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(actor_correlation,'hex'),'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_expires,'UTF8')||pg_catalog.int4send(28)||pg_catalog.convert_to(ts_event,'UTF8'),'sha256');
                    UPDATE tagekyc.raw_export_recipient_package_deliveries SET "State"='Expired',"Revision"=d."Revision","ExpiredAtUtc"=t WHERE "DeliveryId"=d."DeliveryId";END IF;
                  event_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                    'tip-88c1-c3-delivery-event-id-v1'||pg_catalog.chr(10)||'{"deliveryId":"'||pg_catalog.replace(d."DeliveryId"::text,'-','')||'","revision":'||d."Revision"::text||'}','UTF8'),'sha256'),1,16);
                  event_bytes:=pg_catalog.set_byte(event_bytes,7,(pg_catalog.get_byte(event_bytes,7)&15)|80);event_bytes:=pg_catalog.set_byte(event_bytes,8,(pg_catalog.get_byte(event_bytes,8)&63)|128);
                  event_id:=pg_catalog.encode(pg_catalog.substring(event_bytes,4,1)||pg_catalog.substring(event_bytes,3,1)||pg_catalog.substring(event_bytes,2,1)||pg_catalog.substring(event_bytes,1,1)||pg_catalog.substring(event_bytes,6,1)||pg_catalog.substring(event_bytes,5,1)||pg_catalog.substring(event_bytes,8,1)||pg_catalog.substring(event_bytes,7,1)||pg_catalog.substring(event_bytes,9,8),'hex')::uuid;
                  INSERT INTO tagekyc.raw_export_recipient_package_delivery_events VALUES(event_id,
                    d."DeliveryId",d."RecipientClientApplicationId",d."Revision",CASE outcome WHEN 'Expired' THEN 'Expired' ELSE 'OutcomeUnknown' END,
                    CASE WHEN d."StreamAttemptCount">0 THEN d."StreamAttemptCount" ELSE NULL END,CASE WHEN d."DeliveryFence">0 THEN d."DeliveryFence" ELSE NULL END,
                    actor_api,actor_principal,actor_correlation,evidence,t);
                  SELECT * INTO d FROM tagekyc.raw_export_recipient_package_deliveries x WHERE x."DeliveryId"=d."DeliveryId";
                  SELECT * INTO p FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."PackageId"=d."PackageId";
                  RETURN QUERY SELECT outcome,d."DeliveryId",d."PackageId",d."RecipientClientApplicationId",d."State"::text,d."Revision",d."AuthorizedAtUtc",d."AuthorizationExpiresAtUtc",
                    d."StreamAttemptCount",d."DeliveryFence",d."StreamStartedAtUtc",d."StreamLeaseExpiresAtUtc",d."ServerStreamCompletedAtUtc",d."EncryptedPackageLength",
                    d."PackageCiphertextDigest",d."EnvelopeDigest",d."ObjectBindingDigest",p."ProviderConfigurationId"::text,p."BucketName"::text,p."ObjectKey"::text,d."DeliveryReceiptDigest";
                END $fn$;

                ALTER FUNCTION tagekyc.raw_export_create_recipient_package_delivery(uuid,uuid,uuid,bytea,uuid,uuid,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_recipient_package_delivery(uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_probe_recipient_package_delivery_content(uuid,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recipient_package_delivery_interrupted(uuid,bigint,bigint,text,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recipient_package_integrity_unavailable(uuid,bigint,bigint,text,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_complete_recipient_package_delivery(uuid,bigint,bigint,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_reconcile_next_recipient_package_delivery() OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_create_recipient_package_delivery(uuid,uuid,uuid,bytea,uuid,uuid,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_read_recipient_package_delivery(uuid,uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_probe_recipient_package_delivery_content(uuid,uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recipient_package_delivery_interrupted(uuid,bigint,bigint,text,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recipient_package_integrity_unavailable(uuid,bigint,bigint,text,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_complete_recipient_package_delivery(uuid,bigint,bigint,bigint,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_reconcile_next_recipient_package_delivery() FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_create_recipient_package_delivery(uuid,uuid,uuid,bytea,uuid,uuid,bytea) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_recipient_package_delivery(uuid,uuid) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_probe_recipient_package_delivery_content(uuid,uuid) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recipient_package_delivery_interrupted(uuid,bigint,bigint,text,bytea) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recipient_package_integrity_unavailable(uuid,bigint,bigint,text,bytea) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_complete_recipient_package_delivery(uuid,bigint,bigint,bigint,bytea) TO tagekyc_raw_export_package_delivery;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_reconcile_next_recipient_package_delivery() TO tagekyc_raw_export_package_delivery;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_raw_export_recipient_package_delivery_event_append_only
                  ON tagekyc.raw_export_recipient_package_delivery_events;
                DROP FUNCTION IF EXISTS tagekyc.raw_export_reconcile_next_recipient_package_delivery();
                DROP FUNCTION IF EXISTS tagekyc.raw_export_complete_recipient_package_delivery(uuid,bigint,bigint,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recipient_package_integrity_unavailable(uuid,bigint,bigint,text,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recipient_package_delivery_interrupted(uuid,bigint,bigint,text,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_probe_recipient_package_delivery_content(uuid,uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_recipient_package_delivery(uuid,uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_create_recipient_package_delivery(uuid,uuid,uuid,bytea,uuid,uuid,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_guard_recipient_package_delivery_event();
                REVOKE USAGE ON SCHEMA tagekyc FROM tagekyc_raw_export_package_delivery;
                """);
            migrationBuilder.DropTable(
                name: "raw_export_recipient_package_delivery_events",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_recipient_package_deliveries",
                schema: "tagekyc");
        }
    }
}
