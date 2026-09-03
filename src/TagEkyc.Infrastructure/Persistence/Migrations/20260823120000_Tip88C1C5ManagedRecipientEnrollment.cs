using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1C5ManagedRecipientEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $c5_preflight$
                DECLARE capability pg_catalog.pg_roles%ROWTYPE; login_role pg_catalog.pg_roles%ROWTYPE;
                BEGIN
                  SELECT * INTO login_role FROM pg_catalog.pg_roles
                    WHERE rolname='tagekyc_raw_export_recipient_manager_login';
                  IF NOT FOUND OR NOT login_role.rolcanlogin OR login_role.rolsuper
                     OR login_role.rolcreatedb OR login_role.rolcreaterole
                     OR login_role.rolreplication OR login_role.rolbypassrls
                     OR NOT login_role.rolinherit THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',
                      MESSAGE='PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID';
                  END IF;
                  SELECT * INTO capability FROM pg_catalog.pg_roles
                    WHERE rolname='tagekyc_raw_export_recipient_manager';
                  IF FOUND AND (capability.rolcanlogin OR capability.rolsuper
                     OR capability.rolcreatedb OR capability.rolcreaterole
                     OR capability.rolreplication OR capability.rolbypassrls
                     OR NOT capability.rolinherit) THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',
                      MESSAGE='PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID';
                  END IF;
                  IF NOT FOUND THEN
                    CREATE ROLE tagekyc_raw_export_recipient_manager NOLOGIN
                      NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION
                      NOBYPASSRLS INHERIT;
                  END IF;
                  IF EXISTS (
                    SELECT 1 FROM tagekyc.api_keys
                    GROUP BY "ApiKeyId","ClientApplicationId","PrincipalId"
                    HAVING count(*)<>1) THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',
                      MESSAGE='PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_API_KEY_IDENTITY_DUPLICATE';
                  END IF;
                END $c5_preflight$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_recipient_key_registration_sparse",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations");

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tagekyc.raw_export_recipient_key_registrations
                SET "RevocationReason"='PRE_C5_UNSPECIFIED'
                WHERE "State"='Revoked' AND "RevocationReason" IS NULL;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "uq_api_keys_managed_identity",
                schema: "tagekyc",
                table: "api_keys",
                columns: new[] { "ApiKeyId", "ClientApplicationId", "PrincipalId" });

            migrationBuilder.CreateTable(
                name: "raw_export_managed_recipient_identities",
                schema: "tagekyc",
                columns: table => new
                {
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_managed_recipient_identity", x => x.RecipientClientApplicationId);
                    table.UniqueConstraint("uq_raw_export_managed_recipient_identity_pair", x => new { x.RecipientClientApplicationId, x.PrincipalId });
                    table.CheckConstraint("ck_raw_export_managed_recipient_identity_shape", "\"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PrincipalId\" <> \"RecipientClientApplicationId\" AND \"State\" IN ('Active','Disabled') AND \"Revision\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_recipient_management_operations",
                schema: "tagekyc",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKeyDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    EqualityFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    PayloadDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    AdmissionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OperationKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: true),
                    ResultIdentityRevision = table.Column<long>(type: "bigint", nullable: true),
                    ResultApiKeyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultCredentialVersion = table.Column<int>(type: "integer", nullable: true),
                    ResultKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResultKeyVersion = table.Column<int>(type: "integer", nullable: true),
                    ResultRevision = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizedDeliveryCount = table.Column<int>(type: "integer", nullable: true),
                    StreamingDeliveryCount = table.Column<int>(type: "integer", nullable: true),
                    InterruptedDeliveryCount = table.Column<int>(type: "integer", nullable: true),
                    ResultSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_management_operation", x => x.OperationId);
                    table.CheckConstraint("ck_raw_export_recipient_management_operation_shape", "\"OperationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"ManagerApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"ManagerPrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND octet_length(\"IdempotencyKeyDigest\")=32\nAND octet_length(\"EqualityFingerprint\")=32\nAND octet_length(\"PayloadDigest\")=32\nAND \"OperationKind\" IN ('EnrollRecipient','IssueCredential','ReplaceCredential','RevokeCredential','EnrollKey','RotateKey','RevokeKey')");
                    table.CheckConstraint("ck_raw_export_recipient_management_operation_sparse", "(\"Outcome\" IS NULL AND \"ResultSnapshot\" IS NULL AND \"CompletedAtUtc\" IS NULL\n AND \"ResultIdentityRevision\" IS NULL AND \"ResultApiKeyId\" IS NULL\n AND \"ResultCredentialVersion\" IS NULL AND \"ResultKeyId\" IS NULL\n AND \"ResultKeyVersion\" IS NULL AND \"ResultRevision\" IS NULL\n AND \"AuthorizedDeliveryCount\" IS NULL AND \"StreamingDeliveryCount\" IS NULL\n AND \"InterruptedDeliveryCount\" IS NULL)\nOR\n(\"Outcome\" IS NOT NULL AND \"ResultSnapshot\" IS NOT NULL\n AND \"CompletedAtUtc\" IS NOT NULL AND \"AdmissionAtUtc\" IS NOT NULL\n AND (\n   (\"OperationKind\"='EnrollRecipient' AND \"Outcome\"='Created' AND \"ResultIdentityRevision\"=1\n    AND \"ResultApiKeyId\" IS NULL AND \"ResultCredentialVersion\" IS NULL AND \"ResultKeyId\" IS NULL\n    AND \"ResultKeyVersion\" IS NULL AND \"ResultRevision\" IS NULL\n    AND \"AuthorizedDeliveryCount\" IS NULL AND \"StreamingDeliveryCount\" IS NULL AND \"InterruptedDeliveryCount\" IS NULL)\n   OR\n   (\"OperationKind\" IN ('IssueCredential','ReplaceCredential')\n    AND \"Outcome\" IN ('Created','Replaced') AND \"ResultIdentityRevision\" IS NULL\n    AND \"ResultApiKeyId\" IS NOT NULL AND \"ResultCredentialVersion\" IS NOT NULL\n    AND \"ResultKeyId\" IS NULL AND \"ResultKeyVersion\" IS NULL AND \"ResultRevision\"=1\n    AND \"AuthorizedDeliveryCount\" IS NULL AND \"StreamingDeliveryCount\" IS NULL AND \"InterruptedDeliveryCount\" IS NULL)\n   OR\n   (\"OperationKind\"='RevokeCredential' AND \"Outcome\"='Revoked' AND \"ResultIdentityRevision\" IS NULL\n    AND \"ResultApiKeyId\" IS NOT NULL AND \"ResultCredentialVersion\" IS NOT NULL\n    AND \"ResultKeyId\" IS NULL AND \"ResultKeyVersion\" IS NULL AND \"ResultRevision\">1\n    AND \"AuthorizedDeliveryCount\" IS NULL AND \"StreamingDeliveryCount\" IS NULL AND \"InterruptedDeliveryCount\" IS NULL)\n   OR\n   (\"OperationKind\" IN ('EnrollKey','RotateKey') AND \"Outcome\" IN ('Created','Rotated')\n    AND \"ResultIdentityRevision\" IS NULL AND \"ResultApiKeyId\" IS NULL AND \"ResultCredentialVersion\" IS NULL\n    AND \"ResultKeyId\" IS NOT NULL AND \"ResultKeyVersion\" IS NOT NULL AND \"ResultRevision\"=1\n    AND \"AuthorizedDeliveryCount\" IS NULL AND \"StreamingDeliveryCount\" IS NULL AND \"InterruptedDeliveryCount\" IS NULL)\n   OR\n   (\"OperationKind\"='RevokeKey' AND \"Outcome\"='Revoked'\n    AND \"ResultIdentityRevision\" IS NULL AND \"ResultApiKeyId\" IS NULL AND \"ResultCredentialVersion\" IS NULL\n    AND \"ResultKeyId\" IS NOT NULL AND \"ResultKeyVersion\" IS NOT NULL AND \"ResultRevision\">1\n    AND \"AuthorizedDeliveryCount\">=0 AND \"StreamingDeliveryCount\">=0 AND \"InterruptedDeliveryCount\">=0)\n ))");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_managed_recipient_credentials",
                schema: "tagekyc",
                columns: table => new
                {
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialVersion = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReplacedByApiKeyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_managed_recipient_credential", x => x.ApiKeyId);
                    table.CheckConstraint("ck_raw_export_managed_recipient_credential_shape", "\"ApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"CredentialVersion\" > 0 AND \"Revision\" > 0 AND \"State\" IN ('Active','Revoked')");
                    table.CheckConstraint("ck_raw_export_managed_recipient_credential_sparse", "(\"State\"='Active' AND \"RevokedAtUtc\" IS NULL AND \"RevocationReason\" IS NULL AND \"ReplacedByApiKeyId\" IS NULL) OR (\"State\"='Revoked' AND \"RevokedAtUtc\" IS NOT NULL AND \"RevocationReason\" IS NOT NULL AND length(\"RevocationReason\") BETWEEN 1 AND 128)");
                    table.ForeignKey(
                        name: "fk_c5_credential_api_key_identity",
                        columns: x => new { x.ApiKeyId, x.RecipientClientApplicationId, x.PrincipalId },
                        principalSchema: "tagekyc",
                        principalTable: "api_keys",
                        principalColumns: new[] { "ApiKeyId", "ClientApplicationId", "PrincipalId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_c5_credential_managed_identity",
                        columns: x => new { x.RecipientClientApplicationId, x.PrincipalId },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_managed_recipient_identities",
                        principalColumns: new[] { "RecipientClientApplicationId", "PrincipalId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_c5_credential_replacement",
                        column: x => x.ReplacedByApiKeyId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_managed_recipient_credentials",
                        principalColumn: "ApiKeyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_managed_recipient_policies",
                schema: "tagekyc",
                columns: table => new
                {
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivationProfile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActivationScopesDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_managed_recipient_policy", x => x.RecipientClientApplicationId);
                    table.CheckConstraint("ck_raw_export_managed_recipient_policy_shape", "\"ActivationProfile\" = 'C3C4RecipientV1' AND octet_length(\"ActivationScopesDigest\") = 32 AND \"State\" IN ('Active','Disabled') AND \"Revision\" > 0");
                    table.ForeignKey(
                        name: "fk_c5_policy_managed_identity",
                        column: x => x.RecipientClientApplicationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_managed_recipient_identities",
                        principalColumn: "RecipientClientApplicationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_recipient_management_events",
                schema: "tagekyc",
                columns: table => new
                {
                    ManagementEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    TargetIdentity = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PriorRevision = table.Column<long>(type: "bigint", nullable: false),
                    NewRevision = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AuthorizedDeliveryCount = table.Column<int>(type: "integer", nullable: true),
                    StreamingDeliveryCount = table.Column<int>(type: "integer", nullable: true),
                    InterruptedDeliveryCount = table.Column<int>(type: "integer", nullable: true),
                    PayloadDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    PriorScopesDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    NewScopesDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    EvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_management_event", x => x.ManagementEventId);
                    table.CheckConstraint("ck_raw_export_recipient_management_event_shape", "\"ManagementEventId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"OperationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"ManagerApiKeyId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"ManagerPrincipalId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid\nAND \"EventType\" IN ('ManagedRecipientEnrolled','ManagedCredentialIssued','ManagedCredentialReplaced','ManagedCredentialRevoked','RecipientPublicKeyEnrolled','RecipientPublicKeyRotated','RecipientPublicKeyRevoked')\nAND length(\"TargetIdentity\") BETWEEN 1 AND 512 AND length(\"Reason\") BETWEEN 1 AND 128\nAND \"PriorRevision\">=0 AND \"NewRevision\">0\nAND octet_length(\"PayloadDigest\")=32 AND octet_length(\"EvidenceDigest\")=32\nAND (\"PriorScopesDigest\" IS NULL OR octet_length(\"PriorScopesDigest\")=32)\nAND (\"NewScopesDigest\" IS NULL OR octet_length(\"NewScopesDigest\")=32)");
                    table.CheckConstraint("ck_raw_export_recipient_management_event_sparse", "(\"EventType\"='RecipientPublicKeyRevoked' AND \"AuthorizedDeliveryCount\">=0\n AND \"StreamingDeliveryCount\">=0 AND \"InterruptedDeliveryCount\">=0)\nOR (\"EventType\"<>'RecipientPublicKeyRevoked' AND \"AuthorizedDeliveryCount\" IS NULL\n    AND \"StreamingDeliveryCount\" IS NULL AND \"InterruptedDeliveryCount\" IS NULL)");
                    table.ForeignKey(
                        name: "fk_c5_event_operation",
                        column: x => x.OperationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_recipient_management_operations",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_recipient_key_registration_sparse",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations",
                sql: "(\"State\" = 'Active' AND \"RevokedAtUtc\" IS NULL AND \"RevocationReason\" IS NULL) OR (\"State\" = 'Revoked' AND \"RevokedAtUtc\" IS NOT NULL AND \"RevocationReason\" IS NOT NULL AND length(\"RevocationReason\") BETWEEN 1 AND 128)");

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_managed_credential_active",
                schema: "tagekyc",
                table: "raw_export_managed_recipient_credentials",
                column: "RecipientClientApplicationId",
                unique: true,
                filter: "\"State\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_managed_credential_version",
                schema: "tagekyc",
                table: "raw_export_managed_recipient_credentials",
                columns: new[] { "RecipientClientApplicationId", "CredentialVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_recipient_management_event_operation",
                schema: "tagekyc",
                table: "raw_export_recipient_management_events",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_recipient_management_idempotency",
                schema: "tagekyc",
                table: "raw_export_recipient_management_operations",
                columns: new[] { "ManagerPrincipalId", "IdempotencyKeyDigest" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE FUNCTION tagekyc.raw_export_guard_recipient_management_event()
                RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER
                SET search_path=pg_catalog AS $fn$
                BEGIN
                  RAISE EXCEPTION USING ERRCODE='P0001',
                    MESSAGE='RAW_EXPORT_RECIPIENT_MANAGEMENT_EVENT_APPEND_ONLY';
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_enroll_recipient_key(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_key_id text,p_key_version integer,
                  p_algorithm text,p_spki bytea,p_fingerprint bytea,p_valid_from timestamptz,p_valid_until timestamptz)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb; v_max integer; v_target text; target_bytes bytea;
                BEGIN
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false;
                    ELSE RETURN QUERY SELECT 'ExistingMatch',op."OperationId",op."ResultSnapshot",false; END IF; RETURN;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  INSERT INTO tagekyc.raw_export_recipient_management_operations(
                    "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest","EqualityFingerprint",
                    "PayloadDigest","OperationKind","RecipientClientApplicationId")
                  VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,'EnrollKey',p_recipient);
                  PERFORM 1 FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient AND i."State"='Active' FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_operation,NULL::jsonb,false; RETURN; END IF;
                  PERFORM 1 FROM tagekyc.raw_export_recipient_key_registrations k
                    WHERE k."RecipientClientApplicationId"=p_recipient AND k."State"='Active' FOR UPDATE;
                  IF FOUND THEN RETURN QUERY SELECT 'KeyConflict',p_operation,NULL::jsonb,false; RETURN; END IF;
                  SELECT COALESCE(max(k."RecipientKeyVersion"),0) INTO v_max
                    FROM tagekyc.raw_export_recipient_key_registrations k WHERE k."RecipientClientApplicationId"=p_recipient;
                  v_now:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  IF p_key_id IS NULL OR p_key_id!~'^[A-Za-z0-9._:-]{1,128}$' OR p_algorithm<>'RSA-OAEP-256'
                     OR p_key_version<=v_max OR (v_max=0 AND p_key_version<>1)
                     OR octet_length(p_fingerprint)<>32 OR tagekyc_extensions.digest(p_spki,'sha256')<>p_fingerprint
                     OR p_valid_from>=p_valid_until OR p_valid_until<=v_now OR p_valid_until<v_now+interval '5 minutes'
                     OR EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_key_registrations k
                       WHERE k."RecipientClientApplicationId"=p_recipient AND k."PublicKeyFingerprint"=p_fingerprint) THEN
                    RETURN QUERY SELECT 'KeyConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                    "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
                    "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
                  VALUES(p_recipient,p_key_id,p_key_version,p_algorithm,p_spki,p_fingerprint,p_valid_from,p_valid_until,'Active',1,v_now);
                  v_snapshot:=pg_catalog.jsonb_build_object('key',pg_catalog.jsonb_build_object(
                    'recipientClientApplicationId',p_recipient,'recipientKeyId',p_key_id,'recipientKeyVersion',p_key_version,
                    'publicKeyAlgorithm',p_algorithm,'publicKeyFingerprintHex',pg_catalog.encode(p_fingerprint,'hex'),
                    'state','Active','revision',1,'validFromUtc',p_valid_from,'validUntilUtc',p_valid_until,
                    'registeredAtUtc',v_now,'revokedAtUtc',NULL),'warning',NULL,
                    'authorizedDeliveryCount',NULL,'streamingDeliveryCount',NULL,'interruptedDeliveryCount',NULL);
                  target_bytes:=pg_catalog.int4send(21)||pg_catalog.convert_to('tip-88c1-c5-target-v1','UTF8')
                    ||pg_catalog.int4send(9)||pg_catalog.convert_to('EnrollKey','UTF8')
                    ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_key_id,'UTF8')))||pg_catalog.convert_to(p_key_id,'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_key_version::text,'UTF8')))||pg_catalog.convert_to(p_key_version::text,'UTF8')
                    ||pg_catalog.int4send(1)||pg_catalog.convert_to('-','UTF8');
                  v_target:='v1.'||pg_catalog.rtrim(pg_catalog.translate(pg_catalog.replace(pg_catalog.encode(target_bytes,'base64'),E'\n',''),'+/','-_'),'=');
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId","RecipientClientApplicationId",
                    "EventType","TargetIdentity","PriorRevision","NewRevision","Reason","PayloadDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'RecipientPublicKeyEnrolled',v_target,0,1,'RECIPIENT_PUBLIC_KEY_ENROLLED',p_payload,
                    tagekyc_extensions.digest(pg_catalog.int4send(octet_length(pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')))||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('EnrollKey','UTF8')))||pg_catalog.convert_to('EnrollKey','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RecipientPublicKeyEnrolled','UTF8')))||pg_catalog.convert_to('RecipientPublicKeyEnrolled','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(v_target,'UTF8')))||pg_catalog.convert_to(v_target,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RECIPIENT_PUBLIC_KEY_ENROLLED','UTF8')))||pg_catalog.convert_to('RECIPIENT_PUBLIC_KEY_ENROLLED','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('0','UTF8')))||pg_catalog.convert_to('0','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('1','UTF8')))||pg_catalog.convert_to('1','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')))||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8'),'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "Outcome"='Created',"ResultKeyId"=p_key_id,
                    "ResultKeyVersion"=p_key_version,"ResultRevision"=1,"ResultSnapshot"=v_snapshot,"CompletedAtUtc"=v_now
                    WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Created',p_operation,v_snapshot,false;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_rotate_recipient_key(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_key_id text,p_current_version integer,
                  p_current_revision bigint,p_new_version integer,p_algorithm text,p_spki bytea,p_fingerprint bytea,
                  p_valid_from timestamptz,p_valid_until timestamptz,p_reason text)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  k1 tagekyc.raw_export_recipient_key_registrations%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb; a_count integer; s_count integer; i_count integer;
                  v_target text; target_bytes bytea;
                BEGIN
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false;
                    ELSE RETURN QUERY SELECT 'ExistingMatch',op."OperationId",op."ResultSnapshot",false; END IF; RETURN;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  INSERT INTO tagekyc.raw_export_recipient_management_operations(
                    "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest","EqualityFingerprint",
                    "PayloadDigest","OperationKind","RecipientClientApplicationId")
                  VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,'RotateKey',p_recipient);
                  PERFORM 1 FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient AND i."State"='Active' FOR UPDATE;
                  SELECT * INTO k1 FROM tagekyc.raw_export_recipient_key_registrations k
                    WHERE k."RecipientClientApplicationId"=p_recipient AND k."RecipientKeyId"=p_key_id
                      AND k."RecipientKeyVersion"=p_current_version FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_operation,NULL::jsonb,false; RETURN; END IF;
                  IF k1."State"<>'Active' OR k1."Revision"<>p_current_revision OR p_new_version<=p_current_version
                     OR p_algorithm<>'RSA-OAEP-256' OR tagekyc_extensions.digest(p_spki,'sha256')<>p_fingerprint
                     OR EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_key_registrations k
                       WHERE k."RecipientClientApplicationId"=p_recipient AND k."PublicKeyFingerprint"=p_fingerprint)
                     OR p_reason IS NULL OR length(p_reason) NOT BETWEEN 1 AND 128 THEN
                    RETURN QUERY SELECT 'KeyConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT count(*) FILTER(WHERE d."State"='Authorized')::integer,
                         count(*) FILTER(WHERE d."State"='Streaming')::integer,
                         count(*) FILTER(WHERE d."State"='Interrupted')::integer
                    INTO a_count,s_count,i_count FROM tagekyc.raw_export_recipient_package_deliveries d
                    WHERE d."RecipientClientApplicationId"=p_recipient AND d."RecipientKeyId"=p_key_id
                      AND d."RecipientKeyVersion"=p_current_version AND d."State" IN('Authorized','Streaming','Interrupted');
                  IF a_count+s_count+i_count>0 THEN RETURN QUERY SELECT 'DeliveryDrainRequired',p_operation,NULL::jsonb,false; RETURN; END IF;
                  v_now:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  IF p_valid_from>=p_valid_until OR p_valid_until<v_now+interval '5 minutes' THEN
                    RETURN QUERY SELECT 'KeyConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  UPDATE tagekyc.raw_export_recipient_key_registrations SET "State"='Revoked',"Revision"="Revision"+1,
                    "RevokedAtUtc"=v_now,"RevocationReason"=p_reason WHERE "RecipientClientApplicationId"=p_recipient
                      AND "RecipientKeyId"=p_key_id AND "RecipientKeyVersion"=p_current_version;
                  INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                    "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm","PublicKeySpki",
                    "PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
                  VALUES(p_recipient,p_key_id,p_new_version,p_algorithm,p_spki,p_fingerprint,p_valid_from,p_valid_until,'Active',1,v_now);
                  v_snapshot:=pg_catalog.jsonb_build_object('key',pg_catalog.jsonb_build_object(
                    'recipientClientApplicationId',p_recipient,'recipientKeyId',p_key_id,'recipientKeyVersion',p_new_version,
                    'publicKeyAlgorithm',p_algorithm,'publicKeyFingerprintHex',pg_catalog.encode(p_fingerprint,'hex'),
                    'state','Active','revision',1,'validFromUtc',p_valid_from,'validUntilUtc',p_valid_until,
                    'registeredAtUtc',v_now,'revokedAtUtc',NULL),'warning',NULL,
                    'authorizedDeliveryCount',NULL,'streamingDeliveryCount',NULL,'interruptedDeliveryCount',NULL);
                  target_bytes:=pg_catalog.int4send(21)||pg_catalog.convert_to('tip-88c1-c5-target-v1','UTF8')
                    ||pg_catalog.int4send(9)||pg_catalog.convert_to('RotateKey','UTF8')
                    ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_key_id,'UTF8')))||pg_catalog.convert_to(p_key_id,'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_current_version::text,'UTF8')))||pg_catalog.convert_to(p_current_version::text,'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_new_version::text,'UTF8')))||pg_catalog.convert_to(p_new_version::text,'UTF8');
                  v_target:='v1.'||pg_catalog.rtrim(pg_catalog.translate(pg_catalog.replace(pg_catalog.encode(target_bytes,'base64'),E'\n',''),'+/','-_'),'=');
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId","RecipientClientApplicationId",
                    "EventType","TargetIdentity","PriorRevision","NewRevision","Reason","PayloadDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'RecipientPublicKeyRotated',v_target,k1."Revision",k1."Revision"+1,
                    p_reason,p_payload,tagekyc_extensions.digest(pg_catalog.int4send(octet_length(pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')))||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RotateKey','UTF8')))||pg_catalog.convert_to('RotateKey','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RecipientPublicKeyRotated','UTF8')))||pg_catalog.convert_to('RecipientPublicKeyRotated','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(v_target,'UTF8')))||pg_catalog.convert_to(v_target,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_reason,'UTF8')))||pg_catalog.convert_to(p_reason,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(k1."Revision"::text,'UTF8')))||pg_catalog.convert_to(k1."Revision"::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to((k1."Revision"+1)::text,'UTF8')))||pg_catalog.convert_to((k1."Revision"+1)::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')))||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8'),'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "Outcome"='Rotated',"ResultKeyId"=p_key_id,
                    "ResultKeyVersion"=p_new_version,"ResultRevision"=1,"ResultSnapshot"=v_snapshot,"CompletedAtUtc"=v_now
                    WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Rotated',p_operation,v_snapshot,false;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_revoke_recipient_key(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_key_id text,p_key_version integer,
                  p_expected_revision bigint,p_reason text)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  k tagekyc.raw_export_recipient_key_registrations%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb; a_count integer; s_count integer; i_count integer;
                  v_target text; target_bytes bytea;
                BEGIN
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false;
                    ELSE RETURN QUERY SELECT 'ExistingMatch',op."OperationId",op."ResultSnapshot",false; END IF; RETURN;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  INSERT INTO tagekyc.raw_export_recipient_management_operations(
                    "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest","EqualityFingerprint",
                    "PayloadDigest","OperationKind","RecipientClientApplicationId")
                  VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,'RevokeKey',p_recipient);
                  PERFORM 1 FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient AND i."State"='Active' FOR UPDATE;
                  SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x
                    WHERE x."RecipientClientApplicationId"=p_recipient AND x."RecipientKeyId"=p_key_id
                      AND x."RecipientKeyVersion"=p_key_version FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_operation,NULL::jsonb,false; RETURN; END IF;
                  IF k."State"<>'Active' OR k."Revision"<>p_expected_revision OR p_reason IS NULL OR length(p_reason) NOT BETWEEN 1 AND 128 THEN
                    RETURN QUERY SELECT 'KeyConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT count(*) FILTER(WHERE d."State"='Authorized')::integer,
                         count(*) FILTER(WHERE d."State"='Streaming')::integer,
                         count(*) FILTER(WHERE d."State"='Interrupted')::integer
                    INTO a_count,s_count,i_count FROM tagekyc.raw_export_recipient_package_deliveries d
                    WHERE d."RecipientClientApplicationId"=p_recipient AND d."RecipientKeyId"=p_key_id
                      AND d."RecipientKeyVersion"=p_key_version AND d."State" IN('Authorized','Streaming','Interrupted');
                  v_now:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  UPDATE tagekyc.raw_export_recipient_key_registrations SET "State"='Revoked',"Revision"="Revision"+1,
                    "RevokedAtUtc"=v_now,"RevocationReason"=p_reason WHERE "RecipientClientApplicationId"=p_recipient
                      AND "RecipientKeyId"=p_key_id AND "RecipientKeyVersion"=p_key_version;
                  v_snapshot:=pg_catalog.jsonb_build_object('key',pg_catalog.jsonb_build_object(
                    'recipientClientApplicationId',p_recipient,'recipientKeyId',p_key_id,'recipientKeyVersion',p_key_version,
                    'publicKeyAlgorithm',k."PublicKeyAlgorithm",'publicKeyFingerprintHex',pg_catalog.encode(k."PublicKeyFingerprint",'hex'),
                    'state','Revoked','revision',k."Revision"+1,'validFromUtc',k."ValidFromUtc",'validUntilUtc',k."ValidUntilUtc",
                    'registeredAtUtc',k."RegisteredAtUtc",'revokedAtUtc',v_now),
                    'warning','EXISTING_DELIVERIES_MAY_BE_STRANDED','authorizedDeliveryCount',a_count,
                    'streamingDeliveryCount',s_count,'interruptedDeliveryCount',i_count);
                  target_bytes:=pg_catalog.int4send(21)||pg_catalog.convert_to('tip-88c1-c5-target-v1','UTF8')
                    ||pg_catalog.int4send(9)||pg_catalog.convert_to('RevokeKey','UTF8')
                    ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_key_id,'UTF8')))||pg_catalog.convert_to(p_key_id,'UTF8')
                    ||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_key_version::text,'UTF8')))||pg_catalog.convert_to(p_key_version::text,'UTF8')
                    ||pg_catalog.int4send(1)||pg_catalog.convert_to('-','UTF8');
                  v_target:='v1.'||pg_catalog.rtrim(pg_catalog.translate(pg_catalog.replace(pg_catalog.encode(target_bytes,'base64'),E'\n',''),'+/','-_'),'=');
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId","RecipientClientApplicationId",
                    "EventType","TargetIdentity","PriorRevision","NewRevision","Reason","AuthorizedDeliveryCount",
                    "StreamingDeliveryCount","InterruptedDeliveryCount","PayloadDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'RecipientPublicKeyRevoked',v_target,k."Revision",k."Revision"+1,p_reason,
                    a_count,s_count,i_count,p_payload,tagekyc_extensions.digest(pg_catalog.int4send(octet_length(pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')))||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RevokeKey','UTF8')))||pg_catalog.convert_to('RevokeKey','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RecipientPublicKeyRevoked','UTF8')))||pg_catalog.convert_to('RecipientPublicKeyRevoked','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(v_target,'UTF8')))||pg_catalog.convert_to(v_target,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_reason,'UTF8')))||pg_catalog.convert_to(p_reason,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(k."Revision"::text,'UTF8')))||pg_catalog.convert_to(k."Revision"::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to((k."Revision"+1)::text,'UTF8')))||pg_catalog.convert_to((k."Revision"+1)::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')))||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(a_count::text,'UTF8')))||pg_catalog.convert_to(a_count::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(s_count::text,'UTF8')))||pg_catalog.convert_to(s_count::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(i_count::text,'UTF8')))||pg_catalog.convert_to(i_count::text,'UTF8'),'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "Outcome"='Revoked',"ResultKeyId"=p_key_id,
                    "ResultKeyVersion"=p_key_version,"ResultRevision"=k."Revision"+1,
                    "AuthorizedDeliveryCount"=a_count,"StreamingDeliveryCount"=s_count,"InterruptedDeliveryCount"=i_count,
                    "ResultSnapshot"=v_snapshot,"CompletedAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Revoked',p_operation,v_snapshot,false;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_replace_recipient_credential(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_current_api_key uuid,
                  p_current_revision bigint,p_new_api_key uuid,p_key_prefix text,p_key_hash bytea,
                  p_expires timestamptz,p_reason text)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  ident tagekyc.raw_export_managed_recipient_identities%ROWTYPE;
                  oldc tagekyc.raw_export_managed_recipient_credentials%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb; v_version integer; v_constraint text;
                  active_scope bytea:=pg_catalog.decode('406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex');
                BEGIN
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND AND op."Outcome" IS NOT NULL THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN
                      RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false;
                    ELSE RETURN QUERY SELECT 'ExistingMatchSecretUnavailable',op."OperationId",op."ResultSnapshot",false; END IF;
                    RETURN;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF NOT FOUND THEN
                    INSERT INTO tagekyc.raw_export_recipient_management_operations(
                      "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest","EqualityFingerprint",
                      "PayloadDigest","OperationKind","RecipientClientApplicationId")
                    VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,
                      'ReplaceCredential',p_recipient);
                  ELSIF op."OperationId"<>p_operation OR op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN
                    RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT * INTO ident FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient FOR UPDATE;
                  SELECT * INTO oldc FROM tagekyc.raw_export_managed_recipient_credentials c
                    WHERE c."ApiKeyId"=p_current_api_key AND c."RecipientClientApplicationId"=p_recipient FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_operation,NULL::jsonb,false; RETURN; END IF;
                  IF ident."State"<>'Active' OR oldc."State"<>'Active' OR oldc."Revision"<>p_current_revision
                     OR oldc."PrincipalId"<>ident."PrincipalId" OR p_reason IS NULL OR length(p_reason) NOT BETWEEN 1 AND 128 THEN
                    RETURN QUERY SELECT 'CredentialConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT "AdmissionAtUtc" INTO v_now FROM tagekyc.raw_export_recipient_management_operations AS target WHERE target."OperationId"=p_operation;
                  IF v_now IS NULL THEN v_now:=pg_catalog.clock_timestamp();
                    UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  END IF;
                  IF p_new_api_key IS NULL THEN RETURN QUERY SELECT 'CandidateRequired',p_operation,NULL::jsonb,false; RETURN; END IF;
                  IF p_new_api_key='00000000-0000-0000-0000-000000000000'::uuid OR length(p_key_prefix)<>16
                     OR octet_length(p_key_hash)<>32 OR (p_expires IS NOT NULL AND p_expires<=v_now) THEN
                    RETURN QUERY SELECT 'CredentialConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT COALESCE(max(c."CredentialVersion"),0)+1 INTO v_version
                    FROM tagekyc.raw_export_managed_recipient_credentials c WHERE c."RecipientClientApplicationId"=p_recipient;
                  BEGIN
                    INSERT INTO tagekyc.api_keys("ApiKeyId","ClientApplicationId","PrincipalId","CredentialRef","CredentialType",
                      "CredentialStatus","KeyPrefix","KeyHash","ScopesJson","ExpiresAt","CallerCategory",
                      "AllowedClientApplicationIdsJson","AllowedCaptureAgentIdsJson","OAuthClientId","MtlsSubjectDn","CreatedAt")
                    VALUES(p_new_api_key,p_recipient,ident."PrincipalId",'managed-api-key:'||pg_catalog.replace(p_new_api_key::text,'-',''),
                      'ManagedApiKey','Active',p_key_prefix,p_key_hash,
                      '["business.raw-export.package.download","business.raw-export.package.references.read"]'::jsonb,
                      p_expires,'BusinessConsumer',NULL,NULL,NULL,NULL,v_now);
                  EXCEPTION WHEN unique_violation THEN
                    GET STACKED DIAGNOSTICS v_constraint=CONSTRAINT_NAME;
                    IF v_constraint='IX_api_keys_KeyPrefix' THEN
                      RETURN QUERY SELECT 'CandidateConflict',p_operation,NULL::jsonb,false; RETURN;
                    END IF;
                    RAISE;
                  END;
                  UPDATE tagekyc.raw_export_managed_recipient_credentials SET "State"='Revoked',
                    "Revision"="Revision"+1,"RevokedAtUtc"=v_now,"RevocationReason"=p_reason
                    WHERE "ApiKeyId"=oldc."ApiKeyId";
                  UPDATE tagekyc.api_keys SET "CredentialStatus"='Revoked' WHERE "ApiKeyId"=oldc."ApiKeyId";
                  INSERT INTO tagekyc.raw_export_managed_recipient_credentials(
                    "ApiKeyId","RecipientClientApplicationId","PrincipalId","CredentialVersion","State","Revision","IssuedAtUtc")
                  VALUES(p_new_api_key,p_recipient,ident."PrincipalId",v_version,'Active',1,v_now);
                  UPDATE tagekyc.raw_export_managed_recipient_credentials SET "ReplacedByApiKeyId"=p_new_api_key
                    WHERE "ApiKeyId"=oldc."ApiKeyId";
                  v_snapshot:=pg_catalog.jsonb_build_object('apiKeyId',p_new_api_key,
                    'recipientClientApplicationId',p_recipient,'principalId',ident."PrincipalId",'credentialVersion',v_version,
                    'state','Active','revision',1,'issuedAtUtc',v_now,'expiresAtUtc',p_expires);
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId","RecipientClientApplicationId",
                    "EventType","TargetIdentity","PriorRevision","NewRevision","Reason","PayloadDigest",
                    "PriorScopesDigest","NewScopesDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'ManagedCredentialReplaced',pg_catalog.replace(p_new_api_key::text,'-',''),oldc."Revision",oldc."Revision"+1,
                    p_reason,p_payload,active_scope,active_scope,
                    tagekyc_extensions.digest(pg_catalog.int4send(octet_length(pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')))||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('ReplaceCredential','UTF8')))||pg_catalog.convert_to('ReplaceCredential','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('ManagedCredentialReplaced','UTF8')))||pg_catalog.convert_to('ManagedCredentialReplaced','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_new_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_new_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_reason,'UTF8')))||pg_catalog.convert_to(p_reason,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(oldc."Revision"::text,'UTF8')))||pg_catalog.convert_to(oldc."Revision"::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to((oldc."Revision"+1)::text,'UTF8')))||pg_catalog.convert_to((oldc."Revision"+1)::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')))||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8'),'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "Outcome"='Replaced',
                    "ResultApiKeyId"=p_new_api_key,"ResultCredentialVersion"=v_version,"ResultRevision"=1,
                    "ResultSnapshot"=v_snapshot,"CompletedAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Replaced',p_operation,v_snapshot,true;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_revoke_recipient_credential(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_api_key uuid,p_expected_revision bigint,p_reason text)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  c tagekyc.raw_export_managed_recipient_credentials%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb;
                  empty_scope bytea:=pg_catalog.decode('9db0afc8ff0af27ff5ce08ae785783750dc03db8679d3746dc4049815ff4459f','hex');
                  active_scope bytea:=pg_catalog.decode('406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex');
                BEGIN
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false;
                    ELSE RETURN QUERY SELECT 'ExistingMatch',op."OperationId",op."ResultSnapshot",false; END IF; RETURN;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  INSERT INTO tagekyc.raw_export_recipient_management_operations(
                    "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest","EqualityFingerprint",
                    "PayloadDigest","OperationKind","RecipientClientApplicationId")
                  VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,'RevokeCredential',p_recipient);
                  PERFORM 1 FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient FOR UPDATE;
                  SELECT * INTO c FROM tagekyc.raw_export_managed_recipient_credentials x
                    WHERE x."ApiKeyId"=p_api_key AND x."RecipientClientApplicationId"=p_recipient FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound',p_operation,NULL::jsonb,false; RETURN; END IF;
                  IF c."State"<>'Active' OR c."Revision"<>p_expected_revision OR p_reason IS NULL OR length(p_reason) NOT BETWEEN 1 AND 128 THEN
                    RETURN QUERY SELECT 'CredentialConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  v_now:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  UPDATE tagekyc.raw_export_managed_recipient_credentials SET "State"='Revoked',"Revision"="Revision"+1,
                    "RevokedAtUtc"=v_now,"RevocationReason"=p_reason WHERE "ApiKeyId"=p_api_key;
                  UPDATE tagekyc.api_keys SET "CredentialStatus"='Revoked' WHERE "ApiKeyId"=p_api_key;
                  v_snapshot:=pg_catalog.jsonb_build_object('apiKeyId',p_api_key,'recipientClientApplicationId',p_recipient,
                    'principalId',c."PrincipalId",'credentialVersion',c."CredentialVersion",'state','Revoked',
                    'revision',c."Revision"+1,'issuedAtUtc',c."IssuedAtUtc",'expiresAtUtc',
                    (SELECT "ExpiresAt" FROM tagekyc.api_keys WHERE "ApiKeyId"=p_api_key));
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId","RecipientClientApplicationId",
                    "EventType","TargetIdentity","PriorRevision","NewRevision","Reason","PayloadDigest",
                    "PriorScopesDigest","NewScopesDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'ManagedCredentialRevoked',pg_catalog.replace(p_api_key::text,'-',''),c."Revision",c."Revision"+1,p_reason,
                    p_payload,active_scope,empty_scope,tagekyc_extensions.digest(pg_catalog.int4send(octet_length(pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')))||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('RevokeCredential','UTF8')))||pg_catalog.convert_to('RevokeCredential','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('ManagedCredentialRevoked','UTF8')))||pg_catalog.convert_to('ManagedCredentialRevoked','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(p_reason,'UTF8')))||pg_catalog.convert_to(p_reason,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(c."Revision"::text,'UTF8')))||pg_catalog.convert_to(c."Revision"::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to((c."Revision"+1)::text,'UTF8')))||pg_catalog.convert_to((c."Revision"+1)::text,'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(empty_scope,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(empty_scope,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')))||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8'),'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "Outcome"='Revoked',"ResultApiKeyId"=p_api_key,
                    "ResultCredentialVersion"=c."CredentialVersion","ResultRevision"=c."Revision"+1,
                    "ResultSnapshot"=v_snapshot,"CompletedAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Revoked',p_operation,v_snapshot,false;
                END $fn$;

                CREATE TRIGGER trg_raw_export_recipient_management_event_append
                  BEFORE UPDATE OR DELETE ON tagekyc.raw_export_recipient_management_events
                  FOR EACH ROW EXECUTE FUNCTION tagekyc.raw_export_guard_recipient_management_event();

                CREATE FUNCTION tagekyc.raw_export_enroll_managed_recipient(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_principal uuid)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  ident tagekyc.raw_export_managed_recipient_identities%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb;
                  empty_scope bytea:=pg_catalog.decode('9db0afc8ff0af27ff5ce08ae785783750dc03db8679d3746dc4049815ff4459f','hex');
                  active_scope bytea:=pg_catalog.decode('406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex');
                BEGIN
                  IF p_operation IS NULL OR p_operation='00000000-0000-0000-0000-000000000000'::uuid
                     OR p_manager_api_key IS NULL OR p_manager_api_key='00000000-0000-0000-0000-000000000000'::uuid
                     OR p_manager_principal IS NULL OR p_manager_principal='00000000-0000-0000-0000-000000000000'::uuid
                     OR p_recipient IS NULL OR p_recipient='00000000-0000-0000-0000-000000000000'::uuid
                     OR p_principal IS NULL OR p_principal='00000000-0000-0000-0000-000000000000'::uuid
                     OR p_principal=p_recipient OR octet_length(p_idempotency)<>32
                     OR octet_length(p_equality)<>32 OR octet_length(p_payload)<>32 THEN
                    RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_RECIPIENT_MANAGEMENT_ARGUMENT_INVALID';
                  END IF;
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN
                      RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false; RETURN;
                    END IF;
                    IF op."Outcome" IS NOT NULL THEN
                      RETURN QUERY SELECT 'ExistingMatch',op."OperationId",op."ResultSnapshot",false; RETURN;
                    END IF;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN
                      RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false; RETURN;
                    ELSIF op."Outcome" IS NOT NULL THEN
                      RETURN QUERY SELECT 'ExistingMatch',op."OperationId",op."ResultSnapshot",false; RETURN;
                    END IF;
                  ELSE
                    INSERT INTO tagekyc.raw_export_recipient_management_operations(
                      "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest",
                      "EqualityFingerprint","PayloadDigest","OperationKind","RecipientClientApplicationId")
                    VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,
                      'EnrollRecipient',p_recipient);
                  END IF;
                  SELECT * INTO ident FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient FOR UPDATE;
                  IF FOUND THEN
                    RETURN QUERY SELECT 'PrincipalConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  v_now:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now
                    WHERE target."OperationId"=p_operation AND "AdmissionAtUtc" IS NULL;
                  INSERT INTO tagekyc.raw_export_managed_recipient_identities
                    ("RecipientClientApplicationId","PrincipalId","State","Revision","CreatedAtUtc","UpdatedAtUtc")
                  VALUES(p_recipient,p_principal,'Active',1,v_now,v_now);
                  INSERT INTO tagekyc.raw_export_managed_recipient_policies
                    ("RecipientClientApplicationId","ActivationProfile","ActivationScopesDigest","State","Revision","CreatedAtUtc","UpdatedAtUtc")
                  VALUES(p_recipient,'C3C4RecipientV1',active_scope,'Active',1,v_now,v_now);
                  v_snapshot:=pg_catalog.jsonb_build_object(
                    'recipientClientApplicationId',p_recipient,'principalId',p_principal,'state','Active','revision',1);
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId",
                    "RecipientClientApplicationId","EventType","TargetIdentity","PriorRevision","NewRevision",
                    "Reason","PayloadDigest","PriorScopesDigest","NewScopesDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'ManagedRecipientEnrolled',pg_catalog.replace(p_recipient::text,'-',''),0,1,
                    'MANAGED_RECIPIENT_ENROLLED',p_payload,empty_scope,active_scope,
                    tagekyc_extensions.digest(
                      pg_catalog.int4send(20)||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')
                      ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')
                      ||pg_catalog.int4send(15)||pg_catalog.convert_to('EnrollRecipient','UTF8')
                      ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')
                      ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')
                      ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')
                      ||pg_catalog.int4send(24)||pg_catalog.convert_to('ManagedRecipientEnrolled','UTF8')
                      ||pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')
                      ||pg_catalog.int4send(26)||pg_catalog.convert_to('MANAGED_RECIPIENT_ENROLLED','UTF8')
                      ||pg_catalog.int4send(1)||pg_catalog.convert_to('0','UTF8')
                      ||pg_catalog.int4send(1)||pg_catalog.convert_to('1','UTF8')
                      ||pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')
                      ||pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(empty_scope,'hex'),'UTF8')
                      ||pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')
                      ||pg_catalog.int4send(28)||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US')||'0Z','UTF8')
                      ||pg_catalog.int4send(1)||pg_catalog.convert_to('-','UTF8')
                      ||pg_catalog.int4send(1)||pg_catalog.convert_to('-','UTF8')
                      ||pg_catalog.int4send(1)||pg_catalog.convert_to('-','UTF8'),
                      'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET
                    "Outcome"='Created',"ResultIdentityRevision"=1,"ResultSnapshot"=v_snapshot,
                    "CompletedAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Created',p_operation,v_snapshot,false;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_issue_recipient_credential(
                  p_operation uuid,p_manager_api_key uuid,p_manager_principal uuid,p_recipient uuid,
                  p_idempotency bytea,p_equality bytea,p_payload bytea,p_new_api_key uuid,
                  p_key_prefix text,p_key_hash bytea,p_expires timestamptz)
                RETURNS TABLE("Outcome" text,"OperationId" uuid,"ResultSnapshot" jsonb,"CandidateCommitted" boolean)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE op tagekyc.raw_export_recipient_management_operations%ROWTYPE;
                  ident tagekyc.raw_export_managed_recipient_identities%ROWTYPE;
                  v_now timestamptz; v_snapshot jsonb; v_version integer; v_constraint text;
                  empty_scope bytea:=pg_catalog.decode('9db0afc8ff0af27ff5ce08ae785783750dc03db8679d3746dc4049815ff4459f','hex');
                  active_scope bytea:=pg_catalog.decode('406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex');
                BEGIN
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF FOUND AND op."Outcome" IS NOT NULL THEN
                    IF op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN
                      RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false;
                    ELSE RETURN QUERY SELECT 'ExistingMatchSecretUnavailable',op."OperationId",op."ResultSnapshot",false; END IF;
                    RETURN;
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_recipient::text,842005));
                  IF NOT FOUND THEN NULL; END IF;
                  SELECT * INTO op FROM tagekyc.raw_export_recipient_management_operations o
                    WHERE o."ManagerPrincipalId"=p_manager_principal AND o."IdempotencyKeyDigest"=p_idempotency;
                  IF NOT FOUND THEN
                    INSERT INTO tagekyc.raw_export_recipient_management_operations(
                      "OperationId","ManagerApiKeyId","ManagerPrincipalId","IdempotencyKeyDigest","EqualityFingerprint",
                      "PayloadDigest","OperationKind","RecipientClientApplicationId")
                    VALUES(p_operation,p_manager_api_key,p_manager_principal,p_idempotency,p_equality,p_payload,
                      'IssueCredential',p_recipient);
                  ELSIF op."OperationId"<>p_operation OR op."EqualityFingerprint" IS DISTINCT FROM p_equality THEN
                    RETURN QUERY SELECT 'IdempotencyConflict',op."OperationId",NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT * INTO ident FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient FOR UPDATE;
                  IF NOT FOUND OR ident."State"<>'Active' OR NOT EXISTS(
                    SELECT 1 FROM tagekyc.raw_export_managed_recipient_policies p
                    WHERE p."RecipientClientApplicationId"=p_recipient AND p."State"='Active'
                      AND p."ActivationProfile"='C3C4RecipientV1' AND p."ActivationScopesDigest"=active_scope) THEN
                    RETURN QUERY SELECT 'NotFound',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  IF EXISTS(SELECT 1 FROM tagekyc.raw_export_managed_recipient_credentials c
                    WHERE c."RecipientClientApplicationId"=p_recipient AND c."State"='Active') THEN
                    RETURN QUERY SELECT 'CredentialConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT target."AdmissionAtUtc" INTO v_now
                    FROM tagekyc.raw_export_recipient_management_operations AS target
                    WHERE target."OperationId"=p_operation;
                  IF v_now IS NULL THEN
                    v_now:=pg_catalog.clock_timestamp();
                    UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "AdmissionAtUtc"=v_now
                      WHERE target."OperationId"=p_operation;
                  END IF;
                  IF p_new_api_key IS NULL THEN
                    RETURN QUERY SELECT 'CandidateRequired',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  IF p_new_api_key='00000000-0000-0000-0000-000000000000'::uuid
                     OR octet_length(p_key_hash)<>32 OR length(p_key_prefix)<>16
                     OR (p_expires IS NOT NULL AND p_expires<=v_now) THEN
                    RETURN QUERY SELECT 'CredentialConflict',p_operation,NULL::jsonb,false; RETURN;
                  END IF;
                  SELECT COALESCE(max(c."CredentialVersion"),0)+1 INTO v_version
                    FROM tagekyc.raw_export_managed_recipient_credentials c
                    WHERE c."RecipientClientApplicationId"=p_recipient;
                  BEGIN
                    INSERT INTO tagekyc.api_keys("ApiKeyId","ClientApplicationId","PrincipalId","CredentialRef",
                      "CredentialType","CredentialStatus","KeyPrefix","KeyHash","ScopesJson","ExpiresAt",
                      "CallerCategory","AllowedClientApplicationIdsJson","AllowedCaptureAgentIdsJson","OAuthClientId",
                      "MtlsSubjectDn","CreatedAt")
                    VALUES(p_new_api_key,p_recipient,ident."PrincipalId",'managed-api-key:'||pg_catalog.replace(p_new_api_key::text,'-',''),
                      'ManagedApiKey','Active',p_key_prefix,p_key_hash,
                      '["business.raw-export.package.download","business.raw-export.package.references.read"]'::jsonb,
                      p_expires,'BusinessConsumer',NULL,NULL,NULL,NULL,v_now);
                  EXCEPTION WHEN unique_violation THEN
                    GET STACKED DIAGNOSTICS v_constraint=CONSTRAINT_NAME;
                    IF v_constraint='IX_api_keys_KeyPrefix' THEN
                      RETURN QUERY SELECT 'CandidateConflict',p_operation,NULL::jsonb,false; RETURN;
                    END IF;
                    RAISE;
                  END;
                  INSERT INTO tagekyc.raw_export_managed_recipient_credentials(
                    "ApiKeyId","RecipientClientApplicationId","PrincipalId","CredentialVersion","State","Revision","IssuedAtUtc")
                  VALUES(p_new_api_key,p_recipient,ident."PrincipalId",v_version,'Active',1,v_now);
                  v_snapshot:=pg_catalog.jsonb_build_object('apiKeyId',p_new_api_key,
                    'recipientClientApplicationId',p_recipient,'principalId',ident."PrincipalId",
                    'credentialVersion',v_version,'state','Active','revision',1,'issuedAtUtc',v_now,
                    'expiresAtUtc',p_expires);
                  INSERT INTO tagekyc.raw_export_recipient_management_events(
                    "ManagementEventId","OperationId","ManagerApiKeyId","ManagerPrincipalId","RecipientClientApplicationId",
                    "EventType","TargetIdentity","PriorRevision","NewRevision","Reason","PayloadDigest",
                    "PriorScopesDigest","NewScopesDigest","EvidenceDigest","OccurredAtUtc")
                  VALUES(pg_catalog.gen_random_uuid(),p_operation,p_manager_api_key,p_manager_principal,p_recipient,
                    'ManagedCredentialIssued',pg_catalog.replace(p_new_api_key::text,'-',''),0,1,
                    'MANAGED_CREDENTIAL_ISSUED',p_payload,empty_scope,active_scope,
                    tagekyc_extensions.digest(pg_catalog.int4send(octet_length(pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')))||pg_catalog.convert_to('tip-88c1-c5-audit-v2','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_operation::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('IssueCredential','UTF8')))||pg_catalog.convert_to('IssueCredential','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_manager_principal::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('ManagedCredentialIssued','UTF8')))||pg_catalog.convert_to('ManagedCredentialIssued','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.replace(p_new_api_key::text,'-',''),'UTF8')))||pg_catalog.convert_to(pg_catalog.replace(p_new_api_key::text,'-',''),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('MANAGED_CREDENTIAL_ISSUED','UTF8')))||pg_catalog.convert_to('MANAGED_CREDENTIAL_ISSUED','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('0','UTF8')))||pg_catalog.convert_to('0','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('1','UTF8')))||pg_catalog.convert_to('1','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(p_payload,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(empty_scope,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(empty_scope,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')))||pg_catalog.convert_to(pg_catalog.encode(active_scope,'hex'),'UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')))||pg_catalog.convert_to(pg_catalog.to_char(v_now AT TIME ZONE 'UTC','YYYY-MM-DD')||'T'||pg_catalog.to_char(v_now AT TIME ZONE 'UTC','HH24:MI:SS.US')||'0Z','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8')||pg_catalog.int4send(octet_length(pg_catalog.convert_to('-','UTF8')))||pg_catalog.convert_to('-','UTF8'),'sha256'),v_now);
                  UPDATE tagekyc.raw_export_recipient_management_operations AS target SET "Outcome"='Created',
                    "ResultApiKeyId"=p_new_api_key,"ResultCredentialVersion"=v_version,"ResultRevision"=1,
                    "ResultSnapshot"=v_snapshot,"CompletedAtUtc"=v_now WHERE target."OperationId"=p_operation;
                  RETURN QUERY SELECT 'Created',p_operation,v_snapshot,true;
                END $fn$;
                CREATE FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(p_recipient uuid)
                RETURNS TABLE("Ready" boolean,"Codes" text[],"AuthorizedDeliveryCount" integer,
                  "StreamingDeliveryCount" integer,"InterruptedDeliveryCount" integer)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE codes text[]:=ARRAY[]::text[]; a_count integer; s_count integer; i_count integer;
                  active_scope bytea:=pg_catalog.decode('406372e4455de83f44404b972f7f9411894d74b84bb5c2af73c8deb25a35fe91','hex');
                BEGIN
                  IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_managed_recipient_identities i
                    WHERE i."RecipientClientApplicationId"=p_recipient) THEN RETURN; END IF;
                  IF NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_managed_recipient_identities i
                    JOIN tagekyc.raw_export_managed_recipient_policies p USING("RecipientClientApplicationId")
                    WHERE i."RecipientClientApplicationId"=p_recipient AND i."State"='Active'
                      AND i."PrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid
                      AND i."PrincipalId"<>i."RecipientClientApplicationId" AND p."State"='Active'
                      AND p."ActivationProfile"='C3C4RecipientV1' AND p."ActivationScopesDigest"=active_scope
                      AND EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_management_operations o
                        JOIN tagekyc.raw_export_recipient_management_events e ON e."OperationId"=o."OperationId"
                        WHERE o."RecipientClientApplicationId"=p_recipient
                          AND o."OperationKind"='EnrollRecipient' AND o."Outcome"='Created'
                          AND o."ResultIdentityRevision"=p."Revision"
                          AND e."EventType"='ManagedRecipientEnrolled' AND e."NewRevision"=p."Revision")) THEN
                    codes:=pg_catalog.array_append(codes,'PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID');
                  END IF;
                  IF (SELECT count(*) FROM tagekyc.raw_export_managed_recipient_credentials c
                    JOIN tagekyc.api_keys a ON a."ApiKeyId"=c."ApiKeyId"
                      AND a."ClientApplicationId"=c."RecipientClientApplicationId" AND a."PrincipalId"=c."PrincipalId"
                    WHERE c."RecipientClientApplicationId"=p_recipient AND c."State"='Active'
                      AND a."CredentialStatus"='Active' AND a."CredentialType"='ManagedApiKey'
                      AND a."CallerCategory"='BusinessConsumer'
                      AND a."ScopesJson"='["business.raw-export.package.download","business.raw-export.package.references.read"]'::jsonb
                      AND (a."ExpiresAt" IS NULL OR a."ExpiresAt">pg_catalog.clock_timestamp())
                      AND EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_management_operations o
                        JOIN tagekyc.raw_export_recipient_management_events e ON e."OperationId"=o."OperationId"
                        WHERE o."RecipientClientApplicationId"=p_recipient
                          AND o."OperationKind" IN ('IssueCredential','ReplaceCredential')
                          AND o."Outcome" IN ('Created','Replaced')
                          AND o."ResultApiKeyId"=c."ApiKeyId"
                          AND o."ResultCredentialVersion"=c."CredentialVersion"
                          AND o."ResultRevision"=c."Revision"
                          AND e."OperationId"=o."OperationId"))<>1 THEN
                    codes:=pg_catalog.array_append(codes,'PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID');
                  END IF;
                  IF (SELECT count(*) FROM tagekyc.raw_export_recipient_key_registrations k
                    WHERE k."RecipientClientApplicationId"=p_recipient AND k."State"='Active'
                      AND k."PublicKeyAlgorithm"='RSA-OAEP-256'
                      AND k."ValidFromUtc"<=pg_catalog.clock_timestamp()
                      AND k."ValidUntilUtc">pg_catalog.clock_timestamp()+interval '5 minutes'
                      AND tagekyc_extensions.digest(k."PublicKeySpki",'sha256')=k."PublicKeyFingerprint"
                      AND EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_management_operations o
                        JOIN tagekyc.raw_export_recipient_management_events e ON e."OperationId"=o."OperationId"
                        WHERE o."RecipientClientApplicationId"=p_recipient
                          AND o."OperationKind" IN ('EnrollKey','RotateKey')
                          AND o."Outcome" IN ('Created','Rotated')
                          AND o."ResultKeyId"=k."RecipientKeyId"
                          AND o."ResultKeyVersion"=k."RecipientKeyVersion"
                          AND o."ResultRevision"=k."Revision"
                          AND e."OperationId"=o."OperationId"))<>1 THEN
                    codes:=pg_catalog.array_append(codes,'PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID');
                  END IF;
                  SELECT count(*) FILTER(WHERE d."State"='Authorized')::integer,
                         count(*) FILTER(WHERE d."State"='Streaming')::integer,
                         count(*) FILTER(WHERE d."State"='Interrupted')::integer
                    INTO a_count,s_count,i_count FROM tagekyc.raw_export_recipient_package_deliveries d
                    WHERE d."RecipientClientApplicationId"=p_recipient AND d."State" IN('Authorized','Streaming','Interrupted');
                  RETURN QUERY SELECT pg_catalog.cardinality(codes)=0,codes,a_count,s_count,i_count;
                END $fn$;

                ALTER TABLE tagekyc.raw_export_managed_recipient_identities OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_managed_recipient_policies OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_managed_recipient_credentials OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_recipient_management_operations OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_recipient_management_events OWNER TO tagekyc_raw_export_deployer;

                GRANT SELECT,INSERT,UPDATE ON TABLE tagekyc.api_keys TO tagekyc_raw_export_deployer;
                GRANT SELECT ON TABLE tagekyc.raw_export_managed_recipient_identities,
                  tagekyc.raw_export_managed_recipient_policies,
                  tagekyc.raw_export_managed_recipient_credentials TO tagekyc_runtime;

                ALTER FUNCTION tagekyc.raw_export_guard_recipient_management_event() OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_enroll_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,text,bytea,bytea,timestamptz,timestamptz) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_rotate_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,integer,text,bytea,bytea,timestamptz,timestamptz,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_revoke_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(uuid) OWNER TO tagekyc_raw_export_deployer;

                GRANT tagekyc_raw_export_recipient_manager TO tagekyc_raw_export_recipient_manager_login WITH INHERIT TRUE, SET FALSE;
                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_raw_export_recipient_manager;
                REVOKE ALL ON ALL TABLES IN SCHEMA tagekyc FROM tagekyc_raw_export_recipient_manager,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_guard_recipient_management_event() FROM PUBLIC,tagekyc_raw_export_recipient_manager,tagekyc_raw_export_recipient_manager_login;

                REVOKE ALL ON FUNCTION tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_enroll_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,text,bytea,bytea,timestamptz,timestamptz) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_rotate_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,integer,text,bytea,bytea,timestamptz,timestamptz,text) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_revoke_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,text) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(uuid) FROM PUBLIC,tagekyc_raw_export_recipient_manager_login;

                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_enroll_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,text,bytea,bytea,timestamptz,timestamptz) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_rotate_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,integer,text,bytea,bytea,timestamptz,timestamptz,text) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_revoke_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,text) TO tagekyc_raw_export_recipient_manager;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_recipient_activation_readiness(uuid) TO tagekyc_raw_export_recipient_manager;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_recipient_activation_readiness(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_revoke_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_rotate_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,bigint,integer,text,bytea,bytea,timestamptz,timestamptz,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_enroll_recipient_key(uuid,uuid,uuid,uuid,bytea,bytea,bytea,text,integer,text,bytea,bytea,timestamptz,timestamptz);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_revoke_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_replace_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,bigint,uuid,text,bytea,timestamptz,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_issue_recipient_credential(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid,text,bytea,timestamptz);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid);
                DROP TRIGGER IF EXISTS trg_raw_export_recipient_management_event_append ON tagekyc.raw_export_recipient_management_events;
                DROP FUNCTION IF EXISTS tagekyc.raw_export_guard_recipient_management_event();
                REVOKE tagekyc_raw_export_recipient_manager FROM tagekyc_raw_export_recipient_manager_login;
                REVOKE USAGE ON SCHEMA tagekyc FROM tagekyc_raw_export_recipient_manager;
                """);

            migrationBuilder.DropTable(
                name: "raw_export_managed_recipient_credentials",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_managed_recipient_policies",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_recipient_management_events",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_managed_recipient_identities",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_recipient_management_operations",
                schema: "tagekyc");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_recipient_key_registration_sparse",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_api_keys_managed_identity",
                schema: "tagekyc",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_recipient_key_registration_sparse",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations",
                sql: "(\"State\" = 'Active' AND \"RevokedAtUtc\" IS NULL) OR (\"State\" = 'Revoked' AND \"RevokedAtUtc\" IS NOT NULL)");
        }
    }
}
