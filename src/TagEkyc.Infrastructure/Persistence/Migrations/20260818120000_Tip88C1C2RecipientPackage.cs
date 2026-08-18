using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1C2RecipientPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "raw_export_recipient_key_registrations",
                schema: "tagekyc",
                columns: table => new
                {
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    PublicKeyAlgorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PublicKeySpki = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKeyFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    RegisteredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_key_registration", x => new { x.RecipientClientApplicationId, x.RecipientKeyId, x.RecipientKeyVersion });
                    table.UniqueConstraint("uq_raw_export_recipient_key_registration_fingerprint", x => new { x.RecipientClientApplicationId, x.PublicKeyFingerprint });
                    table.CheckConstraint("ck_raw_export_recipient_key_registration_shape", "\"RecipientClientApplicationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"RecipientKeyId\" ~ '^[A-Za-z0-9._:-]{1,128}$' AND \"RecipientKeyVersion\" > 0 AND \"PublicKeyAlgorithm\" = 'RSA-OAEP-256' AND octet_length(\"PublicKeySpki\") BETWEEN 384 AND 1024 AND octet_length(\"PublicKeyFingerprint\") = 32 AND \"ValidFromUtc\" < \"ValidUntilUtc\" AND \"State\" IN ('Active','Revoked') AND \"Revision\" > 0");
                    table.CheckConstraint("ck_raw_export_recipient_key_registration_sparse", "(\"State\" = 'Active' AND \"RevokedAtUtc\" IS NULL) OR (\"State\" = 'Revoked' AND \"RevokedAtUtc\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "raw_export_recipient_package_preparations",
                schema: "tagekyc",
                columns: table => new
                {
                    C2PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageEqualityFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    AssemblyFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    ManifestDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    AssemblyDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    AssemblyAuthenticationValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    CompleteAssemblyLength = table.Column<long>(type: "bigint", nullable: false),
                    RecipientClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientKeyVersion = table.Column<int>(type: "integer", nullable: false),
                    RecipientKeyFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    RecipientPublicKeySpki = table.Column<byte[]>(type: "bytea", nullable: false),
                    RecipientKeyRevision = table.Column<long>(type: "bigint", nullable: false),
                    RecipientKeyValidFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecipientKeyValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PackageProfile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderOperationTokenDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    ProviderKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderConfigurationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderEndpointFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    BucketName = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectBindingDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    EnvelopeDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    EncryptedPackageLength = table.Column<long>(type: "bigint", nullable: true),
                    PackageCiphertextDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    ConditionalCreateEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    ProviderReceiptDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    AbortAuthorizationDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    PositiveAbsenceEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    CleanupProgressEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    QuarantineEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotFrozenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PutStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AbortAuthorizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CleanupPendingAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AbortedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QuarantinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_package_preparation", x => x.C2PreparationId);
                    table.UniqueConstraint("uq_raw_export_recipient_package_assembly", x => x.AssemblyId);
                    table.UniqueConstraint("uq_raw_export_recipient_package_equality", x => x.PackageEqualityFingerprint);
                    table.UniqueConstraint("uq_raw_export_recipient_package_operation", x => x.ProviderOperationTokenDigest);
                    table.UniqueConstraint("uq_raw_export_recipient_package_package", x => x.PackageId);
                    table.CheckConstraint("ck_raw_export_recipient_package_shape", "\"C2PreparationId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"PackageId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND octet_length(\"PackageEqualityFingerprint\") = 32 AND octet_length(\"AssemblyFingerprint\") = 32 AND octet_length(\"ManifestDigest\") = 32 AND octet_length(\"AssemblyDigest\") = 32 AND octet_length(\"AssemblyAuthenticationValue\") = 32 AND \"CompleteAssemblyLength\" BETWEEN 1 AND 33554432 AND \"RecipientKeyId\" ~ '^[A-Za-z0-9._:-]{1,128}$' AND \"RecipientKeyVersion\" > 0 AND octet_length(\"RecipientKeyFingerprint\") = 32 AND octet_length(\"RecipientPublicKeySpki\") BETWEEN 384 AND 1024 AND \"RecipientKeyRevision\" > 0 AND \"RecipientKeyValidFromUtc\" < \"RecipientKeyValidUntilUtc\" AND \"PackageProfile\" = 'tip-88c1-c2-package-profile-v1' AND octet_length(\"ProviderOperationTokenDigest\") = 32 AND \"ProviderKind\" = 's3-compatible-single-part-v1' AND octet_length(\"ProviderEndpointFingerprint\") = 32 AND \"ObjectKey\" ~ '^raw-export/c2-package/v1/[0-9a-f]{32}$' AND octet_length(\"ObjectBindingDigest\") = 32 AND \"State\" IN ('Reserved','PutInFlight','PutOutcomeUnknown','Prepared','Finalized','AbortAuthorized','CleanupPending','Aborted','Quarantined') AND \"Revision\" > 0");
                    table.CheckConstraint("ck_raw_export_recipient_package_sparse", "(\"EnvelopeDigest\" IS NULL OR octet_length(\"EnvelopeDigest\") = 32) AND (\"PackageCiphertextDigest\" IS NULL OR octet_length(\"PackageCiphertextDigest\") = 32) AND (\"ConditionalCreateEvidenceDigest\" IS NULL OR octet_length(\"ConditionalCreateEvidenceDigest\") = 32) AND (\"ProviderReceiptDigest\" IS NULL OR octet_length(\"ProviderReceiptDigest\") = 32) AND (\"AbortAuthorizationDigest\" IS NULL OR octet_length(\"AbortAuthorizationDigest\") = 32) AND (\"PositiveAbsenceEvidenceDigest\" IS NULL OR octet_length(\"PositiveAbsenceEvidenceDigest\") = 32) AND (\"CleanupProgressEvidenceDigest\" IS NULL OR octet_length(\"CleanupProgressEvidenceDigest\") = 32) AND (\"QuarantineEvidenceDigest\" IS NULL OR octet_length(\"QuarantineEvidenceDigest\") = 32) AND ((\"State\" = 'Reserved' AND \"EnvelopeDigest\" IS NULL AND \"EncryptedPackageLength\" IS NULL AND \"PackageCiphertextDigest\" IS NULL AND \"PutStartedAtUtc\" IS NULL AND \"PreparedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortAuthorizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" IN ('PutInFlight','PutOutcomeUnknown') AND \"EnvelopeDigest\" IS NOT NULL AND \"EncryptedPackageLength\" BETWEEN 1 AND 33557106 AND \"PackageCiphertextDigest\" IS NOT NULL AND \"PutStartedAtUtc\" IS NOT NULL AND \"PreparedAtUtc\" IS NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortedAtUtc\" IS NULL AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" IN ('Prepared','Finalized') AND \"EnvelopeDigest\" IS NOT NULL AND \"EncryptedPackageLength\" BETWEEN 1 AND 33557106 AND \"PackageCiphertextDigest\" IS NOT NULL AND \"ConditionalCreateEvidenceDigest\" IS NOT NULL AND \"ProviderReceiptDigest\" IS NOT NULL AND \"PutStartedAtUtc\" IS NOT NULL AND \"PreparedAtUtc\" IS NOT NULL AND (\"State\" = 'Prepared' AND \"FinalizedAtUtc\" IS NULL OR \"State\" = 'Finalized' AND \"FinalizedAtUtc\" IS NOT NULL) AND \"AbortAuthorizationDigest\" IS NULL AND \"AbortedAtUtc\" IS NULL AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" IN ('AbortAuthorized','CleanupPending','Aborted') AND \"AbortAuthorizationDigest\" IS NOT NULL AND \"AbortAuthorizedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NULL AND (\"State\" <> 'Aborted' OR \"AbortedAtUtc\" IS NOT NULL) AND \"QuarantinedAtUtc\" IS NULL) OR (\"State\" = 'Quarantined' AND \"QuarantineEvidenceDigest\" IS NOT NULL AND \"QuarantinedAtUtc\" IS NOT NULL AND \"FinalizedAtUtc\" IS NULL AND \"AbortedAtUtc\" IS NULL))");
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_attempt_fence",
                        columns: x => new { x.JobId, x.AttemptId, x.FencingToken },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_attempts",
                        principalColumns: new[] { "JobId", "AttemptId", "FencingToken" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_job",
                        column: x => x.JobId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_job_identities",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_key",
                        columns: x => new { x.RecipientClientApplicationId, x.RecipientKeyId, x.RecipientKeyVersion },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_recipient_key_registrations",
                        principalColumns: new[] { "RecipientClientApplicationId", "RecipientKeyId", "RecipientKeyVersion" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_recipient_package_events",
                schema: "tagekyc",
                columns: table => new
                {
                    C2PreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventRevision = table.Column<long>(type: "bigint", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_recipient_package_event", x => new { x.C2PreparationId, x.EventRevision });
                    table.CheckConstraint("ck_raw_export_recipient_package_event_shape", "\"EventRevision\" > 0 AND \"EventKind\" IN ('SnapshotFrozen','PutStarted','PutOutcomeUnknown','Prepared','Finalized','AbortAuthorized','CleanupPending','Aborted','Quarantined') AND \"State\" IN ('Reserved','PutInFlight','PutOutcomeUnknown','Prepared','Finalized','AbortAuthorized','CleanupPending','Aborted','Quarantined') AND (\"EvidenceDigest\" IS NULL OR octet_length(\"EvidenceDigest\") = 32) AND ((\"EventKind\" IN ('SnapshotFrozen','PutStarted') AND \"EvidenceDigest\" IS NULL) OR (\"EventKind\" NOT IN ('SnapshotFrozen','PutStarted') AND \"EvidenceDigest\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_raw_export_recipient_package_event_preparation",
                        column: x => x.C2PreparationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_recipient_package_preparations",
                        principalColumn: "C2PreparationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_recipient_key_registration_active",
                schema: "tagekyc",
                table: "raw_export_recipient_key_registrations",
                column: "RecipientClientApplicationId",
                unique: true,
                filter: "\"State\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_recipient_package_attempt_fence",
                schema: "tagekyc",
                table: "raw_export_recipient_package_preparations",
                columns: new[] { "JobId", "AttemptId", "FencingToken" });

            migrationBuilder.CreateIndex(
                name: "IX_raw_export_recipient_package_preparations_RecipientClientAp~",
                schema: "tagekyc",
                table: "raw_export_recipient_package_preparations",
                columns: new[] { "RecipientClientApplicationId", "RecipientKeyId", "RecipientKeyVersion" });

            migrationBuilder.Sql("""
                DO $c2_roles$
                DECLARE role_name text; role_row record;
                BEGIN
                  FOREACH role_name IN ARRAY ARRAY[
                    'tagekyc_raw_export_package_preparer',
                    'tagekyc_raw_export_package_reconciler',
                    'tagekyc_raw_export_package_lifecycle']
                  LOOP
                    SELECT * INTO role_row FROM pg_catalog.pg_roles WHERE rolname=role_name;
                    IF FOUND THEN
                      IF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolcreatedb
                         OR role_row.rolcreaterole OR role_row.rolreplication
                         OR role_row.rolbypassrls OR NOT role_row.rolinherit THEN
                        RAISE EXCEPTION USING ERRCODE='P0001', MESSAGE='PROD_RAW_EXPORT_RECIPIENT_PACKAGE_ROLE_TOPOLOGY_INVALID';
                      END IF;
                    ELSE
                      EXECUTE pg_catalog.format(
                        'CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',role_name);
                    END IF;
                  END LOOP;
                END $c2_roles$;
                GRANT tagekyc_raw_export_package_preparer TO tagekyc_raw_export_package_preparer_login WITH INHERIT TRUE, SET FALSE;
                GRANT tagekyc_raw_export_package_reconciler TO tagekyc_raw_export_package_reconciler_login WITH INHERIT TRUE, SET FALSE;
                GRANT tagekyc_raw_export_package_lifecycle TO tagekyc_raw_export_package_lifecycle_login WITH INHERIT TRUE, SET FALSE;
                GRANT USAGE ON SCHEMA tagekyc TO tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;
                ALTER TABLE tagekyc.raw_export_recipient_key_registrations OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_recipient_package_preparations OWNER TO tagekyc_raw_export_deployer;
                ALTER TABLE tagekyc.raw_export_recipient_package_events OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON TABLE tagekyc.raw_export_recipient_key_registrations FROM PUBLIC, tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;
                REVOKE ALL ON TABLE tagekyc.raw_export_recipient_package_preparations FROM PUBLIC, tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;
                REVOKE ALL ON TABLE tagekyc.raw_export_recipient_package_events FROM PUBLIC, tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;

                CREATE FUNCTION tagekyc.raw_export_select_active_recipient_key(p_recipient uuid)
                RETURNS TABLE(outcome text, recipient_client_application_id uuid, recipient_key_id text,
                  recipient_key_version integer, recipient_key_fingerprint bytea, recipient_public_key_spki bytea,
                  recipient_key_revision bigint, recipient_key_valid_from_utc timestamptz, recipient_key_valid_until_utc timestamptz)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                BEGIN
                  IF p_recipient IS NULL OR p_recipient='00000000-0000-0000-0000-000000000000'::uuid THEN
                    RETURN QUERY SELECT 'Unavailable'::text,NULL::uuid,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz; RETURN;
                  END IF;
                  RETURN QUERY SELECT 'Selected'::text,k."RecipientClientApplicationId",k."RecipientKeyId"::text,k."RecipientKeyVersion",
                    k."PublicKeyFingerprint",k."PublicKeySpki",k."Revision",k."ValidFromUtc",k."ValidUntilUtc"
                  FROM tagekyc.raw_export_recipient_key_registrations k
                  WHERE k."RecipientClientApplicationId"=p_recipient AND k."State"='Active'
                    AND k."ValidFromUtc"<=pg_catalog.clock_timestamp() AND k."ValidUntilUtc">pg_catalog.clock_timestamp()
                  ORDER BY k."RecipientKeyVersion" DESC LIMIT 1;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Unavailable'::text,NULL::uuid,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz; END IF;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_reserve_recipient_package(
                  p_id uuid,p_package uuid,p_assembly uuid,p_job uuid,p_attempt uuid,p_fence bigint,
                  p_assembly_fp bytea,p_manifest bytea,p_assembly_digest bytea,p_auth bytea,p_complete bigint,
                  p_recipient uuid,p_key_id text,p_key_version integer,p_key_fp bytea,p_key_revision bigint,
                  p_equality bytea,p_token_digest bytea,p_provider_kind text,p_provider_config text,p_endpoint_fp bytea,
                  p_bucket text,p_object_key text,p_object_binding bytea,p_profile text)
                RETURNS TABLE(outcome text,row_revision bigint,state text,package_id uuid,package_equality_fingerprint bytea,
                  recipient_key_id text,recipient_key_version integer,recipient_key_fingerprint bytea,recipient_public_key_spki bytea,
                  recipient_key_revision bigint,recipient_key_valid_from_utc timestamptz,recipient_key_valid_until_utc timestamptz,
                  provider_operation_token_digest bytea,object_binding_digest bytea,provider_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE
                  k tagekyc.raw_export_recipient_key_registrations%ROWTYPE;
                  r tagekyc.raw_export_recipient_package_preparations%ROWTYPE;
                  a tagekyc.raw_export_assembly_preparation_dispositions%ROWTYPE;
                  now_utc timestamptz;
                  expected_equality bytea;
                  guid_bytes bytea;
                  expected_package uuid;
                  expected_object_key text;
                  expected_object_binding bytea;
                BEGIN
                  now_utc:=pg_catalog.clock_timestamp();
                  SELECT * INTO k FROM tagekyc.raw_export_recipient_key_registrations x
                    WHERE x."RecipientClientApplicationId"=p_recipient AND x."RecipientKeyId"=p_key_id AND x."RecipientKeyVersion"=p_key_version FOR UPDATE;
                  IF NOT FOUND THEN
                    RETURN QUERY SELECT 'Unavailable'::text,NULL::bigint,NULL::text,NULL::uuid,NULL::bytea,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::bytea,NULL::bytea,NULL::bytea; RETURN;
                  END IF;
                  expected_equality:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c2-package-equality-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c2-package-equality-v1','UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_id::text,'-',''),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_assembly::text,'-',''),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_assembly_fp,'hex'),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_manifest,'hex'),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_assembly_digest,'hex'),'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_auth,'hex'),'UTF8')||
                    pg_catalog.int4send(32)||pg_catalog.convert_to(pg_catalog.replace(p_recipient::text,'-',''),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_key_id,'UTF8')))||pg_catalog.convert_to(p_key_id,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_key_version::text,'UTF8')))||pg_catalog.convert_to(p_key_version::text,'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_key_fp,'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_profile,'UTF8')))||pg_catalog.convert_to(p_profile,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_complete::text,'UTF8')))||pg_catalog.convert_to(p_complete::text,'UTF8'),
                    'sha256');
                  guid_bytes:=pg_catalog.substring(tagekyc_extensions.digest(pg_catalog.convert_to(
                    'tip-88c1-c2-package-id-v1'||pg_catalog.chr(10)||'{"c2PreparationId":"'||pg_catalog.replace(p_id::text,'-','')||'","packageEqualityFingerprint":"'||pg_catalog.encode(expected_equality,'hex')||'"}',
                    'UTF8'),'sha256'),1,16);
                  guid_bytes:=pg_catalog.set_byte(guid_bytes,7,(pg_catalog.get_byte(guid_bytes,7)&15)|80);
                  guid_bytes:=pg_catalog.set_byte(guid_bytes,8,(pg_catalog.get_byte(guid_bytes,8)&63)|128);
                  expected_package:=pg_catalog.encode(
                    pg_catalog.substring(guid_bytes,4,1)||pg_catalog.substring(guid_bytes,3,1)||
                    pg_catalog.substring(guid_bytes,2,1)||pg_catalog.substring(guid_bytes,1,1)||
                    pg_catalog.substring(guid_bytes,6,1)||pg_catalog.substring(guid_bytes,5,1)||
                    pg_catalog.substring(guid_bytes,8,1)||pg_catalog.substring(guid_bytes,7,1)||
                    pg_catalog.substring(guid_bytes,9,8),'hex')::uuid;
                  expected_object_key:='raw-export/c2-package/v1/'||pg_catalog.replace(expected_package::text,'-','');
                  expected_object_binding:=tagekyc_extensions.digest(
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to('tip-88c1-c2-object-binding-v1','UTF8')))||pg_catalog.convert_to('tip-88c1-c2-object-binding-v1','UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_provider_kind,'UTF8')))||pg_catalog.convert_to(p_provider_kind,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_provider_config,'UTF8')))||pg_catalog.convert_to(p_provider_config,'UTF8')||
                    pg_catalog.int4send(64)||pg_catalog.convert_to(pg_catalog.encode(p_endpoint_fp,'hex'),'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(p_bucket,'UTF8')))||pg_catalog.convert_to(p_bucket,'UTF8')||
                    pg_catalog.int4send(pg_catalog.octet_length(pg_catalog.convert_to(expected_object_key,'UTF8')))||pg_catalog.convert_to(expected_object_key,'UTF8'),
                    'sha256');
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF FOUND THEN
                    IF p_equality IS DISTINCT FROM expected_equality OR p_package<>expected_package OR p_object_key<>expected_object_key
                      OR p_object_binding IS DISTINCT FROM expected_object_binding
                      OR r."PackageEqualityFingerprint" IS DISTINCT FROM expected_equality OR r."PackageId"<>expected_package
                      OR r."AssemblyId"<>p_assembly OR r."JobId"<>p_job OR r."AttemptId"<>p_attempt OR r."FencingToken"<>p_fence
                      OR r."AssemblyFingerprint" IS DISTINCT FROM p_assembly_fp OR r."ManifestDigest" IS DISTINCT FROM p_manifest
                      OR r."AssemblyDigest" IS DISTINCT FROM p_assembly_digest OR r."AssemblyAuthenticationValue" IS DISTINCT FROM p_auth
                      OR r."CompleteAssemblyLength"<>p_complete OR r."RecipientClientApplicationId"<>p_recipient
                      OR r."RecipientKeyId"<>p_key_id OR r."RecipientKeyVersion"<>p_key_version
                      OR r."RecipientKeyFingerprint" IS DISTINCT FROM p_key_fp OR r."RecipientPublicKeySpki" IS DISTINCT FROM k."PublicKeySpki"
                      OR r."RecipientKeyRevision"<>p_key_revision OR r."ProviderOperationTokenDigest" IS DISTINCT FROM p_token_digest
                      OR r."ProviderKind"<>p_provider_kind OR r."ProviderConfigurationId"<>p_provider_config
                      OR r."ProviderEndpointFingerprint" IS DISTINCT FROM p_endpoint_fp OR r."BucketName"<>p_bucket
                      OR r."ObjectKey"<>expected_object_key OR r."ObjectBindingDigest" IS DISTINCT FROM expected_object_binding
                      OR r."PackageProfile"<>p_profile THEN
                      RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text,r."PackageId",r."PackageEqualityFingerprint",r."RecipientKeyId"::text,r."RecipientKeyVersion",r."RecipientKeyFingerprint",r."RecipientPublicKeySpki",r."RecipientKeyRevision",r."RecipientKeyValidFromUtc",r."RecipientKeyValidUntilUtc",r."ProviderOperationTokenDigest",r."ObjectBindingDigest",r."ProviderReceiptDigest"; RETURN;
                    END IF;
                    RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text,r."PackageId",r."PackageEqualityFingerprint",r."RecipientKeyId"::text,r."RecipientKeyVersion",r."RecipientKeyFingerprint",r."RecipientPublicKeySpki",r."RecipientKeyRevision",r."RecipientKeyValidFromUtc",r."RecipientKeyValidUntilUtc",r."ProviderOperationTokenDigest",r."ObjectBindingDigest",r."ProviderReceiptDigest"; RETURN;
                  END IF;
                  IF k."State"<>'Active' OR k."Revision"<>p_key_revision OR k."PublicKeyFingerprint" IS DISTINCT FROM p_key_fp
                    OR tagekyc_extensions.digest(k."PublicKeySpki",'sha256') IS DISTINCT FROM k."PublicKeyFingerprint"
                    OR k."ValidFromUtc">now_utc OR k."ValidUntilUtc"<now_utc+interval '5 minutes' THEN
                    RETURN QUERY SELECT 'Unavailable'::text,NULL::bigint,NULL::text,NULL::uuid,NULL::bytea,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::bytea,NULL::bytea,NULL::bytea; RETURN;
                  END IF;
                  IF p_profile<>'tip-88c1-c2-package-profile-v1' OR p_provider_kind<>'s3-compatible-single-part-v1'
                    OR pg_catalog.octet_length(p_token_digest)<>32 OR pg_catalog.octet_length(p_endpoint_fp)<>32
                    OR p_equality IS DISTINCT FROM expected_equality OR p_package<>expected_package OR p_object_key<>expected_object_key
                    OR p_object_binding IS DISTINCT FROM expected_object_binding THEN
                    RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text,NULL::uuid,NULL::bytea,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::bytea,NULL::bytea,NULL::bytea; RETURN;
                  END IF;
                  SELECT * INTO a FROM tagekyc.raw_export_assembly_preparation_dispositions x WHERE x."C2PreparationId"=p_id;
                  IF NOT FOUND OR a."AssemblyId"<>p_assembly OR a."JobId"<>p_job OR a."AttemptId"<>p_attempt OR a."FencingToken"<>p_fence
                    OR a."AssemblyFingerprint" IS DISTINCT FROM p_assembly_fp THEN
                    RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text,NULL::uuid,NULL::bytea,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::bytea,NULL::bytea,NULL::bytea; RETURN;
                  END IF;
                  INSERT INTO tagekyc.raw_export_recipient_package_preparations(
                    "C2PreparationId","PackageId","PackageEqualityFingerprint","AssemblyId","JobId","AttemptId","FencingToken",
                    "AssemblyFingerprint","ManifestDigest","AssemblyDigest","AssemblyAuthenticationValue","CompleteAssemblyLength",
                    "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","RecipientKeyFingerprint","RecipientPublicKeySpki",
                    "RecipientKeyRevision","RecipientKeyValidFromUtc","RecipientKeyValidUntilUtc","PackageProfile","ProviderOperationTokenDigest",
                    "ProviderKind","ProviderConfigurationId","ProviderEndpointFingerprint","BucketName","ObjectKey","ObjectBindingDigest","State","Revision","SnapshotFrozenAtUtc")
                  VALUES(p_id,p_package,p_equality,p_assembly,p_job,p_attempt,p_fence,p_assembly_fp,p_manifest,p_assembly_digest,p_auth,p_complete,
                    p_recipient,p_key_id,p_key_version,p_key_fp,k."PublicKeySpki",p_key_revision,k."ValidFromUtc",k."ValidUntilUtc",p_profile,p_token_digest,
                    p_provider_kind,p_provider_config,p_endpoint_fp,p_bucket,p_object_key,p_object_binding,'Reserved',1,now_utc)
                  ON CONFLICT DO NOTHING;
                  IF NOT FOUND THEN
                    SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                    IF NOT FOUND OR r."PackageEqualityFingerprint" IS DISTINCT FROM p_equality THEN
                      RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text,NULL::uuid,NULL::bytea,NULL::text,NULL::integer,NULL::bytea,NULL::bytea,NULL::bigint,NULL::timestamptz,NULL::timestamptz,NULL::bytea,NULL::bytea,NULL::bytea; RETURN;
                    END IF;
                    RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text,r."PackageId",r."PackageEqualityFingerprint",r."RecipientKeyId"::text,r."RecipientKeyVersion",r."RecipientKeyFingerprint",r."RecipientPublicKeySpki",r."RecipientKeyRevision",r."RecipientKeyValidFromUtc",r."RecipientKeyValidUntilUtc",r."ProviderOperationTokenDigest",r."ObjectBindingDigest",r."ProviderReceiptDigest"; RETURN;
                  END IF;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,1,'SnapshotFrozen','Reserved',NULL,now_utc);
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id;
                  RETURN QUERY SELECT 'Reserved'::text,r."Revision",r."State"::text,r."PackageId",r."PackageEqualityFingerprint",r."RecipientKeyId"::text,r."RecipientKeyVersion",r."RecipientKeyFingerprint",r."RecipientPublicKeySpki",r."RecipientKeyRevision",r."RecipientKeyValidFromUtc",r."RecipientKeyValidUntilUtc",r."ProviderOperationTokenDigest",r."ObjectBindingDigest",r."ProviderReceiptDigest";
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_begin_recipient_package_put(p_id uuid,p_revision bigint,p_envelope bytea,p_length bigint,p_digest bytea)
                RETURNS TABLE(outcome text,row_revision bigint,state text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE; n bigint; t timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text; RETURN; END IF;
                  IF r."State" IN ('PutInFlight','PutOutcomeUnknown','Prepared','Finalized') AND r."EnvelopeDigest" IS NOT DISTINCT FROM p_envelope
                    AND r."EncryptedPackageLength"=p_length AND r."PackageCiphertextDigest" IS NOT DISTINCT FROM p_digest THEN
                    RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text; RETURN;
                  END IF;
                  IF r."State"<>'Reserved' OR r."Revision"<>p_revision THEN RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text; RETURN; END IF;
                  n:=r."Revision"+1; t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"='PutInFlight',"Revision"=n,"EnvelopeDigest"=p_envelope,
                    "EncryptedPackageLength"=p_length,"PackageCiphertextDigest"=p_digest,"PutStartedAtUtc"=t WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,'PutStarted','PutInFlight',NULL,t);
                  RETURN QUERY SELECT 'PutInFlight'::text,n,'PutInFlight'::text;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_recipient_package_put_unknown(p_id uuid,p_revision bigint,p_evidence bytea)
                RETURNS TABLE(outcome text,row_revision bigint,state text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE; n bigint; t timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text; RETURN; END IF;
                  IF r."State"='PutOutcomeUnknown' THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text; RETURN; END IF;
                  IF r."State"<>'PutInFlight' OR r."Revision"<>p_revision OR pg_catalog.octet_length(p_evidence)<>32 THEN RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text; RETURN; END IF;
                  n:=r."Revision"+1;t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"='PutOutcomeUnknown',"Revision"=n WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,'PutOutcomeUnknown','PutOutcomeUnknown',p_evidence,t);
                  RETURN QUERY SELECT 'PutOutcomeUnknown'::text,n,'PutOutcomeUnknown'::text;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_recipient_package_prepared(p_id uuid,p_revision bigint,p_etag bytea,p_length bigint,p_digest bytea,p_conditional bytea,p_receipt bytea)
                RETURNS TABLE(outcome text,row_revision bigint,state text,provider_receipt_digest bytea)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE; n bigint;t timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text,NULL::bytea; RETURN; END IF;
                  IF r."State" IN ('Prepared','Finalized') AND r."EncryptedPackageLength"=p_length AND r."PackageCiphertextDigest" IS NOT DISTINCT FROM p_digest
                    AND r."ConditionalCreateEvidenceDigest" IS NOT DISTINCT FROM p_conditional AND r."ProviderReceiptDigest" IS NOT DISTINCT FROM p_receipt THEN
                    RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text,r."ProviderReceiptDigest"; RETURN;
                  END IF;
                  IF r."State" NOT IN ('PutInFlight','PutOutcomeUnknown') OR r."Revision"<>p_revision OR r."EncryptedPackageLength"<>p_length
                    OR r."PackageCiphertextDigest" IS DISTINCT FROM p_digest OR pg_catalog.octet_length(p_etag)<>32
                    OR pg_catalog.octet_length(p_conditional)<>32 OR pg_catalog.octet_length(p_receipt)<>32 THEN
                    RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text,r."ProviderReceiptDigest"; RETURN;
                  END IF;
                  n:=r."Revision"+1;t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"='Prepared',"Revision"=n,
                    "ConditionalCreateEvidenceDigest"=p_conditional,"ProviderReceiptDigest"=p_receipt,"PreparedAtUtc"=t WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,'Prepared','Prepared',p_receipt,t);
                  RETURN QUERY SELECT 'Prepared'::text,n,'Prepared'::text,p_receipt;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_read_recipient_package_recovery_context(p_id uuid)
                RETURNS TABLE(outcome text,row_revision bigint,state text,c2_preparation_id uuid,package_id uuid,package_equality_fingerprint bytea,
                  assembly_id uuid,job_id uuid,attempt_id uuid,fencing_token bigint,assembly_fingerprint bytea,manifest_digest bytea,assembly_digest bytea,
                  assembly_authentication_value bytea,complete_assembly_length bigint,recipient_client_application_id uuid,recipient_key_id text,
                  recipient_key_version integer,recipient_key_fingerprint bytea,recipient_public_key_spki bytea,recipient_key_revision bigint,
                  recipient_key_valid_from_utc timestamptz,recipient_key_valid_until_utc timestamptz,package_profile text,
                  provider_operation_token_digest bytea,provider_kind text,provider_configuration_id text,provider_endpoint_fingerprint bytea,
                  bucket_name text,object_key text,object_binding_digest bytea,envelope_digest bytea,encrypted_package_length bigint,
                  package_ciphertext_digest bytea,conditional_create_evidence_digest bytea,provider_receipt_digest bytea,abort_authorization_digest bytea,
                  positive_absence_evidence_digest bytea,cleanup_progress_evidence_digest bytea,quarantine_evidence_digest bytea)
                LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                  SELECT 'Found'::text,r."Revision",r."State"::text,r."C2PreparationId",r."PackageId",r."PackageEqualityFingerprint",r."AssemblyId",r."JobId",r."AttemptId",r."FencingToken",
                    r."AssemblyFingerprint",r."ManifestDigest",r."AssemblyDigest",r."AssemblyAuthenticationValue",r."CompleteAssemblyLength",r."RecipientClientApplicationId",
                    r."RecipientKeyId"::text,r."RecipientKeyVersion",r."RecipientKeyFingerprint",r."RecipientPublicKeySpki",r."RecipientKeyRevision",r."RecipientKeyValidFromUtc",
                    r."RecipientKeyValidUntilUtc",r."PackageProfile"::text,r."ProviderOperationTokenDigest",r."ProviderKind"::text,r."ProviderConfigurationId"::text,r."ProviderEndpointFingerprint",
                    r."BucketName"::text,r."ObjectKey"::text,r."ObjectBindingDigest",r."EnvelopeDigest",r."EncryptedPackageLength",r."PackageCiphertextDigest",
                    r."ConditionalCreateEvidenceDigest",r."ProviderReceiptDigest",r."AbortAuthorizationDigest",r."PositiveAbsenceEvidenceDigest",
                    r."CleanupProgressEvidenceDigest",r."QuarantineEvidenceDigest"
                  FROM tagekyc.raw_export_recipient_package_preparations r WHERE r."C2PreparationId"=p_id
                $fn$;

                CREATE FUNCTION tagekyc.raw_export_finalize_recipient_package(p_id uuid,p_revision bigint,p_assembly bytea)
                RETURNS TABLE(outcome text,row_revision bigint,state text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE;n bigint;t timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text; RETURN; END IF;
                  IF r."State"='Finalized' AND r."AssemblyFingerprint" IS NOT DISTINCT FROM p_assembly THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text; RETURN; END IF;
                  IF r."State"<>'Prepared' OR r."Revision"<>p_revision OR r."AssemblyFingerprint" IS DISTINCT FROM p_assembly THEN RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text; RETURN; END IF;
                  n:=r."Revision"+1;t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"='Finalized',"Revision"=n,"FinalizedAtUtc"=t WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,'Finalized','Finalized',r."ProviderReceiptDigest",t);
                  RETURN QUERY SELECT 'Finalized'::text,n,'Finalized'::text;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_authorize_recipient_package_abort(p_id uuid,p_revision bigint,p_digest bytea)
                RETURNS TABLE(outcome text,row_revision bigint,state text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE;n bigint;t timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text; RETURN; END IF;
                  IF r."State" IN ('AbortAuthorized','CleanupPending','Aborted') AND r."AbortAuthorizationDigest" IS NOT DISTINCT FROM p_digest THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text; RETURN; END IF;
                  IF r."State" NOT IN ('Reserved','Prepared') OR r."Revision"<>p_revision OR pg_catalog.octet_length(p_digest)<>32 THEN RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text; RETURN; END IF;
                  n:=r."Revision"+1;t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"='AbortAuthorized',"Revision"=n,"AbortAuthorizationDigest"=p_digest,"AbortAuthorizedAtUtc"=t WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,'AbortAuthorized','AbortAuthorized',p_digest,t);
                  RETURN QUERY SELECT 'AbortAuthorized'::text,n,'AbortAuthorized'::text;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_recipient_package_abort_result(p_id uuid,p_revision bigint,p_kind text,p_evidence bytea)
                RETURNS TABLE(outcome text,row_revision bigint,state text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE;n bigint;t timestamptz;next_state text;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text; RETURN; END IF;
                  IF r."State"='Aborted' AND p_kind='PositiveAbsenceConfirmed' AND r."PositiveAbsenceEvidenceDigest" IS NOT DISTINCT FROM p_evidence THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text; RETURN; END IF;
                  IF r."State" NOT IN ('AbortAuthorized','CleanupPending') OR r."Revision"<>p_revision OR pg_catalog.octet_length(p_evidence)<>32
                    OR p_kind NOT IN ('PositiveAbsenceConfirmed','DeleteOutcomeUnknown','ProviderUnavailable') THEN RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text; RETURN; END IF;
                  next_state:=CASE WHEN p_kind='PositiveAbsenceConfirmed' THEN 'Aborted' ELSE 'CleanupPending' END;n:=r."Revision"+1;t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"=next_state,"Revision"=n,
                    "PositiveAbsenceEvidenceDigest"=CASE WHEN next_state='Aborted' THEN p_evidence ELSE "PositiveAbsenceEvidenceDigest" END,
                    "CleanupProgressEvidenceDigest"=CASE WHEN next_state='CleanupPending' THEN p_evidence ELSE "CleanupProgressEvidenceDigest" END,
                    "CleanupPendingAtUtc"=CASE WHEN next_state='CleanupPending' THEN t ELSE "CleanupPendingAtUtc" END,
                    "AbortedAtUtc"=CASE WHEN next_state='Aborted' THEN t ELSE "AbortedAtUtc" END WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,CASE WHEN next_state='Aborted' THEN 'Aborted' ELSE 'CleanupPending' END,next_state,p_evidence,t);
                  RETURN QUERY SELECT next_state,n,next_state;
                END $fn$;

                CREATE FUNCTION tagekyc.raw_export_record_recipient_package_quarantined(p_id uuid,p_revision bigint,p_evidence bytea,p_reason text)
                RETURNS TABLE(outcome text,row_revision bigint,state text)
                LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
                DECLARE r tagekyc.raw_export_recipient_package_preparations%ROWTYPE;n bigint;t timestamptz;
                BEGIN
                  SELECT * INTO r FROM tagekyc.raw_export_recipient_package_preparations x WHERE x."C2PreparationId"=p_id FOR UPDATE;
                  IF NOT FOUND THEN RETURN QUERY SELECT 'Conflict'::text,NULL::bigint,NULL::text; RETURN; END IF;
                  IF r."State"='Quarantined' AND r."QuarantineEvidenceDigest" IS NOT DISTINCT FROM p_evidence THEN RETURN QUERY SELECT 'ExistingMatch'::text,r."Revision",r."State"::text; RETURN; END IF;
                  IF r."State" IN ('Finalized','Aborted') OR r."Revision"<>p_revision OR pg_catalog.octet_length(p_evidence)<>32
                    OR p_reason NOT IN ('ExistingObjectMismatch','MetadataConflict','ProviderIdentityConflict') THEN RETURN QUERY SELECT 'Conflict'::text,r."Revision",r."State"::text; RETURN; END IF;
                  n:=r."Revision"+1;t:=pg_catalog.clock_timestamp();
                  UPDATE tagekyc.raw_export_recipient_package_preparations SET "State"='Quarantined',"Revision"=n,"QuarantineEvidenceDigest"=p_evidence,"QuarantinedAtUtc"=t WHERE "C2PreparationId"=p_id;
                  INSERT INTO tagekyc.raw_export_recipient_package_events VALUES(p_id,n,'Quarantined','Quarantined',p_evidence,t);
                  RETURN QUERY SELECT 'Quarantined'::text,n,'Quarantined'::text;
                END $fn$;

                ALTER FUNCTION tagekyc.raw_export_select_active_recipient_key(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_reserve_recipient_package(uuid,uuid,uuid,uuid,uuid,bigint,bytea,bytea,bytea,bytea,bigint,uuid,text,integer,bytea,bigint,bytea,bytea,text,text,bytea,text,text,bytea,text) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_begin_recipient_package_put(uuid,bigint,bytea,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recipient_package_put_unknown(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recipient_package_prepared(uuid,bigint,bytea,bigint,bytea,bytea,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_read_recipient_package_recovery_context(uuid) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_finalize_recipient_package(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_authorize_recipient_package_abort(uuid,bigint,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recipient_package_abort_result(uuid,bigint,text,bytea) OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_record_recipient_package_quarantined(uuid,bigint,bytea,text) OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION tagekyc.raw_export_select_active_recipient_key(uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_reserve_recipient_package(uuid,uuid,uuid,uuid,uuid,bigint,bytea,bytea,bytea,bytea,bigint,uuid,text,integer,bytea,bigint,bytea,bytea,text,text,bytea,text,text,bytea,text) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_begin_recipient_package_put(uuid,bigint,bytea,bigint,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recipient_package_put_unknown(uuid,bigint,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recipient_package_prepared(uuid,bigint,bytea,bigint,bytea,bytea,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_read_recipient_package_recovery_context(uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_finalize_recipient_package(uuid,bigint,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_authorize_recipient_package_abort(uuid,bigint,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recipient_package_abort_result(uuid,bigint,text,bytea) FROM PUBLIC;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recipient_package_quarantined(uuid,bigint,bytea,text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_select_active_recipient_key(uuid) TO tagekyc_raw_export_package_preparer;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_reserve_recipient_package(uuid,uuid,uuid,uuid,uuid,bigint,bytea,bytea,bytea,bytea,bigint,uuid,text,integer,bytea,bigint,bytea,bytea,text,text,bytea,text,text,bytea,text) TO tagekyc_raw_export_package_preparer;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_begin_recipient_package_put(uuid,bigint,bytea,bigint,bytea) TO tagekyc_raw_export_package_preparer;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recipient_package_put_unknown(uuid,bigint,bytea) TO tagekyc_raw_export_package_preparer;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recipient_package_prepared(uuid,bigint,bytea,bigint,bytea,bytea,bytea) TO tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_recipient_package_recovery_context(uuid) TO tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_finalize_recipient_package(uuid,bigint,bytea) TO tagekyc_raw_export_package_preparer;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_authorize_recipient_package_abort(uuid,bigint,bytea) TO tagekyc_raw_export_package_lifecycle;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recipient_package_abort_result(uuid,bigint,text,bytea) TO tagekyc_raw_export_package_lifecycle;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recipient_package_quarantined(uuid,bigint,bytea,text) TO tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recipient_package_quarantined(uuid,bigint,bytea,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recipient_package_abort_result(uuid,bigint,text,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_authorize_recipient_package_abort(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_finalize_recipient_package(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_read_recipient_package_recovery_context(uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recipient_package_prepared(uuid,bigint,bytea,bigint,bytea,bytea,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_record_recipient_package_put_unknown(uuid,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_begin_recipient_package_put(uuid,bigint,bytea,bigint,bytea);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_reserve_recipient_package(uuid,uuid,uuid,uuid,uuid,bigint,bytea,bytea,bytea,bytea,bigint,uuid,text,integer,bytea,bigint,bytea,bytea,text,text,bytea,text,text,bytea,text);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_select_active_recipient_key(uuid);
                REVOKE tagekyc_raw_export_package_preparer FROM tagekyc_raw_export_package_preparer_login;
                REVOKE tagekyc_raw_export_package_reconciler FROM tagekyc_raw_export_package_reconciler_login;
                REVOKE tagekyc_raw_export_package_lifecycle FROM tagekyc_raw_export_package_lifecycle_login;
                REVOKE USAGE ON SCHEMA tagekyc FROM tagekyc_raw_export_package_preparer, tagekyc_raw_export_package_reconciler, tagekyc_raw_export_package_lifecycle;
                """);
            migrationBuilder.DropTable(
                name: "raw_export_recipient_package_events",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_recipient_package_preparations",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_recipient_key_registrations",
                schema: "tagekyc");

            migrationBuilder.Sql("""
                DROP ROLE tagekyc_raw_export_package_preparer;
                DROP ROLE tagekyc_raw_export_package_reconciler;
                DROP ROLE tagekyc_raw_export_package_lifecycle;
                """);
        }
    }
}
