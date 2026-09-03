using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2R4R6SourceFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_head_values",
                schema: "tagekyc",
                table: "raw_export_source_head");

            migrationBuilder.CreateTable(
                name: "raw_export_source_publications",
                schema: "tagekyc",
                columns: table => new
                {
                    SourcePublicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectCustodyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptKeyReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StagedCiphertextFingerprintSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    StagedCiphertextFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    CommittedAuthoritySnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CommittedAuthoritySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommittedAuthorityRevision = table.Column<long>(type: "bigint", nullable: false),
                    CommittedConsentPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommittedConsentPolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    CommitEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    CommittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublicationState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OpaqueCommittedLocatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAuthoritySnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: true),
                    PublishedAuthoritySnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAuthorityRevision = table.Column<long>(type: "bigint", nullable: true),
                    PublishedConsentPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedConsentPolicyVersion = table.Column<int>(type: "integer", nullable: true),
                    AvailableEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    AvailableAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CleanupDisposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CleanupEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublicationRevision = table.Column<long>(type: "bigint", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_publications", x => x.SourcePublicationId);
                    table.UniqueConstraint("uq_raw_export_source_publication_attempt", x => x.AttemptId);
                    table.UniqueConstraint("uq_raw_export_source_publication_object", x => x.ObjectCustodyId);
                    table.UniqueConstraint("uq_raw_export_source_publication_source", x => x.SourceArtifactId);
                    table.CheckConstraint("ck_raw_export_source_publication_shape", "\"SchemaVersion\" = 1\nAND \"StagedCiphertextFingerprintSchemaVersion\" = 2\nAND pg_catalog.octet_length(\"StagedCiphertextFingerprint\") = 32\nAND \"CommittedAuthoritySnapshotSchemaVersion\" >= 1\nAND \"CommittedAuthorityRevision\" >= 1\nAND \"CommittedConsentPolicyVersion\" >= 1\nAND pg_catalog.octet_length(\"CommitEvidenceDigest\") = 32\nAND \"PublicationState\" IN ('Committed','Available')\nAND \"CleanupDisposition\" IN ('NotPlanned','Pending','NoObsoleteResidue','Completed')\nAND (\n  (\n    \"PublicationState\" = 'Committed'\n    AND \"OpaqueCommittedLocatorId\" IS NULL\n    AND \"PublishedAuthoritySnapshotSchemaVersion\" IS NULL\n    AND \"PublishedAuthoritySnapshotId\" IS NULL\n    AND \"PublishedAuthorityRevision\" IS NULL\n    AND \"PublishedConsentPolicyId\" IS NULL\n    AND \"PublishedConsentPolicyVersion\" IS NULL\n    AND \"AvailableEvidenceDigest\" IS NULL\n    AND \"AvailableAtUtc\" IS NULL\n    AND \"CleanupDisposition\" = 'NotPlanned'\n    AND \"CleanupEvidenceDigest\" IS NULL\n    AND \"FinalizedAtUtc\" IS NULL\n    AND \"PublicationRevision\" = 1\n  )\n  OR\n  (\n    \"PublicationState\" = 'Available'\n    AND \"OpaqueCommittedLocatorId\" IS NOT NULL\n    AND \"PublishedAuthoritySnapshotSchemaVersion\" IS NOT NULL\n    AND \"PublishedAuthoritySnapshotSchemaVersion\" >= 1\n    AND \"PublishedAuthoritySnapshotId\" IS NOT NULL\n    AND \"PublishedAuthorityRevision\" IS NOT NULL\n    AND \"PublishedAuthorityRevision\" >= 1\n    AND \"PublishedConsentPolicyId\" IS NOT NULL\n    AND \"PublishedConsentPolicyVersion\" IS NOT NULL\n    AND \"PublishedConsentPolicyVersion\" >= 1\n    AND \"AvailableEvidenceDigest\" IS NOT NULL\n    AND pg_catalog.octet_length(\"AvailableEvidenceDigest\") = 32\n    AND \"AvailableAtUtc\" IS NOT NULL\n    AND (\n      (\n        \"CleanupDisposition\" = 'Pending'\n        AND \"CleanupEvidenceDigest\" IS NULL\n        AND \"FinalizedAtUtc\" IS NULL\n        AND \"PublicationRevision\" = 2\n      )\n      OR\n      (\n        \"CleanupDisposition\" = 'NoObsoleteResidue'\n        AND \"CleanupEvidenceDigest\" IS NOT NULL\n        AND pg_catalog.octet_length(\"CleanupEvidenceDigest\") = 32\n        AND \"FinalizedAtUtc\" IS NOT NULL\n        AND \"PublicationRevision\" = 2\n      )\n      OR\n      (\n        \"CleanupDisposition\" = 'Completed'\n        AND \"CleanupEvidenceDigest\" IS NOT NULL\n        AND pg_catalog.octet_length(\"CleanupEvidenceDigest\") = 32\n        AND \"FinalizedAtUtc\" IS NOT NULL\n        AND \"PublicationRevision\" = 3\n      )\n    )\n  )\n)");
                    table.ForeignKey(
                        name: "fk_raw_export_source_publication_attempt",
                        column: x => x.AttemptId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_encryption_attempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_publication_attempt_key_binding",
                        columns: x => new { x.AttemptId, x.AttemptKeyReservationId },
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_encryption_attempts",
                        principalColumns: new[] { "AttemptId", "AttemptKeyReservationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_publication_key",
                        column: x => x.AttemptKeyReservationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_attempt_key_reservations",
                        principalColumn: "AttemptKeyReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_publication_object",
                        column: x => x.ObjectCustodyId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_provisional_objects",
                        principalColumn: "ObjectCustodyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_raw_export_source_publication_reservation",
                        column: x => x.SourceArtifactId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_reservations",
                        principalColumn: "SourceArtifactId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "raw_export_source_cleanup_items",
                schema: "tagekyc",
                columns: table => new
                {
                    CleanupItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePublicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedResourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    CleanupState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletionDisposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CleanupEvidenceDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowRevision = table.Column<long>(type: "bigint", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_source_cleanup_items", x => x.CleanupItemId);
                    table.CheckConstraint("ck_raw_export_source_cleanup_item_shape", "\"SchemaVersion\" = 1\nAND \"ResourceKind\" IN ('ProvisionalObject','AttemptKeyReservation')\nAND \"PlannedResourceRevision\" >= 1\nAND \"CleanupState\" IN ('Pending','Completed')\nAND (\n  (\n    \"CleanupState\" = 'Pending'\n    AND \"CompletionDisposition\" IS NULL\n    AND \"CleanupEvidenceDigest\" IS NULL\n    AND \"CompletedAtUtc\" IS NULL\n    AND \"RowRevision\" = 1\n  )\n  OR\n  (\n    \"CleanupState\" = 'Completed'\n    AND \"CompletionDisposition\" IS NOT NULL\n    AND \"CleanupEvidenceDigest\" IS NOT NULL\n    AND pg_catalog.octet_length(\"CleanupEvidenceDigest\") = 32\n    AND \"CompletedAtUtc\" IS NOT NULL\n    AND \"RowRevision\" = 2\n    AND (\n      (\"ResourceKind\" = 'ProvisionalObject'\n       AND \"CompletionDisposition\" IN ('Deleted','Quarantined'))\n      OR\n      (\"ResourceKind\" = 'AttemptKeyReservation'\n       AND \"CompletionDisposition\" IN ('Revoked','ReservationAbandoned'))\n    )\n  )\n)");
                    table.ForeignKey(
                        name: "fk_raw_export_source_cleanup_item_publication",
                        column: x => x.SourcePublicationId,
                        principalSchema: "tagekyc",
                        principalTable: "raw_export_source_publications",
                        principalColumn: "SourcePublicationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_head_values",
                schema: "tagekyc",
                table: "raw_export_source_head",
                sql: "\"CustodyState\" IN ('Reserved','Staged','Available') AND \"ReservationRevision\" >= 1 AND \"Fence\" >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_source_cleanup_item_pending",
                schema: "tagekyc",
                table: "raw_export_source_cleanup_items",
                columns: new[] { "SourcePublicationId", "CleanupState", "CleanupItemId" });

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_source_cleanup_item_resource",
                schema: "tagekyc",
                table: "raw_export_source_cleanup_items",
                columns: new[] { "SourcePublicationId", "ResourceKind", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_source_publication_attempt_key",
                schema: "tagekyc",
                table: "raw_export_source_publications",
                columns: new[] { "AttemptId", "AttemptKeyReservationId" });

            migrationBuilder.CreateIndex(
                name: "ix_raw_export_source_publication_key",
                schema: "tagekyc",
                table: "raw_export_source_publications",
                column: "AttemptKeyReservationId");

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_source_publication_locator",
                schema: "tagekyc",
                table: "raw_export_source_publications",
                column: "OpaqueCommittedLocatorId",
                unique: true,
                filter: "\"OpaqueCommittedLocatorId\" IS NOT NULL");

            migrationBuilder.Sql(UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DownPreflightSql);

            migrationBuilder.DropTable(
                name: "raw_export_source_cleanup_items",
                schema: "tagekyc");

            migrationBuilder.DropTable(
                name: "raw_export_source_publications",
                schema: "tagekyc");

            migrationBuilder.DropCheckConstraint(
                name: "ck_raw_export_source_head_values",
                schema: "tagekyc",
                table: "raw_export_source_head");

            migrationBuilder.AddCheckConstraint(
                name: "ck_raw_export_source_head_values",
                schema: "tagekyc",
                table: "raw_export_source_head",
                sql: "\"CustodyState\" IN ('Reserved','Staged') AND \"ReservationRevision\" >= 1 AND \"Fence\" >= 1");

            migrationBuilder.Sql(DownRestoreSql);
        }

        private const string UpSql = """
        ALTER TABLE tagekyc.raw_export_source_publications OWNER TO tagekyc_raw_export_deployer;
        ALTER TABLE tagekyc.raw_export_source_cleanup_items OWNER TO tagekyc_raw_export_deployer;

        CREATE FUNCTION tagekyc.enforce_raw_export_source_publication_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        DECLARE ctx text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
        BEGIN
          IF current_user<>'tagekyc_raw_export_deployer' THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_PUBLICATION_WRITE_FORBIDDEN'; END IF;
          IF TG_OP='INSERT' AND ctx='tip88c1-r4-commit-v1' THEN RETURN NEW; END IF;
          IF TG_OP='UPDATE' AND ctx='tip88c1-r5-publish-v1'
             AND OLD."PublicationState"='Committed' AND NEW."PublicationState"='Available'
             AND OLD."CleanupDisposition"='NotPlanned' AND NEW."CleanupDisposition" IN ('Pending','NoObsoleteResidue')
             AND NEW."PublicationRevision"=OLD."PublicationRevision"+1
             AND ROW(NEW."SourcePublicationId",NEW."SourceArtifactId",NEW."AttemptId",NEW."ObjectCustodyId",NEW."AttemptKeyReservationId",
                 NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."CommittedAuthoritySnapshotSchemaVersion",
                 NEW."CommittedAuthoritySnapshotId",NEW."CommittedAuthorityRevision",NEW."CommittedConsentPolicyId",NEW."CommittedConsentPolicyVersion",
                 NEW."CommitEvidenceDigest",NEW."CommittedAtUtc",NEW."SchemaVersion")
                 IS NOT DISTINCT FROM
                 ROW(OLD."SourcePublicationId",OLD."SourceArtifactId",OLD."AttemptId",OLD."ObjectCustodyId",OLD."AttemptKeyReservationId",
                 OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."CommittedAuthoritySnapshotSchemaVersion",
                 OLD."CommittedAuthoritySnapshotId",OLD."CommittedAuthorityRevision",OLD."CommittedConsentPolicyId",OLD."CommittedConsentPolicyVersion",
                 OLD."CommitEvidenceDigest",OLD."CommittedAtUtc",OLD."SchemaVersion") THEN RETURN NEW;
          END IF;
          IF TG_OP='UPDATE' AND ctx='tip88c1-r6-cleanup-v1'
             AND OLD."PublicationState"='Available' AND NEW."PublicationState"='Available'
             AND OLD."CleanupDisposition"='Pending' AND NEW."CleanupDisposition"='Completed'
             AND OLD."PublicationRevision"=2 AND NEW."PublicationRevision"=3
             AND OLD."CleanupEvidenceDigest" IS NULL AND NEW."CleanupEvidenceDigest" IS NOT NULL
             AND OLD."FinalizedAtUtc" IS NULL AND NEW."FinalizedAtUtc" IS NOT NULL
             AND ROW(NEW."SourcePublicationId",NEW."SourceArtifactId",NEW."AttemptId",NEW."ObjectCustodyId",NEW."AttemptKeyReservationId",
                 NEW."StagedCiphertextFingerprintSchemaVersion",NEW."StagedCiphertextFingerprint",NEW."CommittedAuthoritySnapshotSchemaVersion",
                 NEW."CommittedAuthoritySnapshotId",NEW."CommittedAuthorityRevision",NEW."CommittedConsentPolicyId",NEW."CommittedConsentPolicyVersion",
                 NEW."CommitEvidenceDigest",NEW."CommittedAtUtc",NEW."OpaqueCommittedLocatorId",NEW."PublishedAuthoritySnapshotSchemaVersion",
                 NEW."PublishedAuthoritySnapshotId",NEW."PublishedAuthorityRevision",NEW."PublishedConsentPolicyId",NEW."PublishedConsentPolicyVersion",
                 NEW."AvailableEvidenceDigest",NEW."AvailableAtUtc",NEW."SchemaVersion")
                 IS NOT DISTINCT FROM
                 ROW(OLD."SourcePublicationId",OLD."SourceArtifactId",OLD."AttemptId",OLD."ObjectCustodyId",OLD."AttemptKeyReservationId",
                 OLD."StagedCiphertextFingerprintSchemaVersion",OLD."StagedCiphertextFingerprint",OLD."CommittedAuthoritySnapshotSchemaVersion",
                 OLD."CommittedAuthoritySnapshotId",OLD."CommittedAuthorityRevision",OLD."CommittedConsentPolicyId",OLD."CommittedConsentPolicyVersion",
                 OLD."CommitEvidenceDigest",OLD."CommittedAtUtc",OLD."OpaqueCommittedLocatorId",OLD."PublishedAuthoritySnapshotSchemaVersion",
                 OLD."PublishedAuthoritySnapshotId",OLD."PublishedAuthorityRevision",OLD."PublishedConsentPolicyId",OLD."PublishedConsentPolicyVersion",
                 OLD."AvailableEvidenceDigest",OLD."AvailableAtUtc",OLD."SchemaVersion") THEN RETURN NEW;
          END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_PUBLICATION_WRITE_FORBIDDEN';
        END $guard$;

        CREATE FUNCTION tagekyc.enforce_raw_export_source_cleanup_item_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        DECLARE ctx text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
        BEGIN
          IF current_user<>'tagekyc_raw_export_deployer' THEN RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CLEANUP_ITEM_WRITE_FORBIDDEN'; END IF;
          IF TG_OP='INSERT' AND ctx='tip88c1-r5-publish-v1' THEN RETURN NEW; END IF;
          IF TG_OP='UPDATE' AND ctx='tip88c1-r6-cleanup-v1'
             AND OLD."CleanupState"='Pending' AND NEW."CleanupState"='Completed'
             AND OLD."RowRevision"=1 AND NEW."RowRevision"=2
             AND OLD."CompletionDisposition" IS NULL AND NEW."CompletionDisposition" IS NOT NULL
             AND OLD."CleanupEvidenceDigest" IS NULL AND NEW."CleanupEvidenceDigest" IS NOT NULL
             AND OLD."CompletedAtUtc" IS NULL AND NEW."CompletedAtUtc" IS NOT NULL
             AND ROW(NEW."CleanupItemId",NEW."SourcePublicationId",NEW."ResourceKind",NEW."ResourceId",NEW."ResourceAttemptId",
                 NEW."PlannedResourceRevision",NEW."SchemaVersion")
                 IS NOT DISTINCT FROM ROW(OLD."CleanupItemId",OLD."SourcePublicationId",OLD."ResourceKind",OLD."ResourceId",OLD."ResourceAttemptId",
                 OLD."PlannedResourceRevision",OLD."SchemaVersion") THEN RETURN NEW;
          END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_CLEANUP_ITEM_WRITE_FORBIDDEN';
        END $guard$;

        CREATE TRIGGER tr_raw_export_source_publication_guard BEFORE INSERT OR UPDATE OR DELETE
          ON tagekyc.raw_export_source_publications FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_source_publication_write();
        CREATE TRIGGER tr_raw_export_source_cleanup_item_guard BEFORE INSERT OR UPDATE OR DELETE
          ON tagekyc.raw_export_source_cleanup_items FOR EACH ROW EXECUTE FUNCTION tagekyc.enforce_raw_export_source_cleanup_item_write();

        CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_head_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        BEGIN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='complete-r1'
             AND TG_OP<>'DELETE' THEN
            RETURN NEW;
          END IF;
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
             AND TG_OP='UPDATE'
             AND OLD."CustodyState"='Reserved' AND NEW."CustodyState"='Staged'
             AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
             AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
                 IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN
            RETURN NEW;
          END IF;
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r5-publish-v1'
             AND TG_OP='UPDATE' AND OLD."CustodyState"='Staged' AND NEW."CustodyState"='Available'
             AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
             AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
                 IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN RETURN NEW; END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
        END $guard$;

        CREATE FUNCTION tagekyc.raw_export_commit_staged_source(
          p_attempt_id uuid,p_expected_reservation_revision bigint,p_expected_attempt_revision bigint,
          p_expected_fence bigint,p_expected_object_state_revision bigint)
        RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"SourceArtifactId" uuid,"AttemptId" uuid,
          "ObjectCustodyId" uuid,"CommitEvidenceDigest" bytea,"CommittedAtUtc" timestamptz,"ReservationRevision" bigint,"Fence" bigint)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE actor_id uuid; probe record; p record; a record; h record; r record; c record; k record; o record;
          consent record; authority record; now_ timestamptz; evidence bytea; publication_id uuid:=pg_catalog.gen_random_uuid();
          prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
        BEGIN
          IF p_attempt_id IS NULL OR p_attempt_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
             OR p_expected_attempt_revision IS NULL OR p_expected_attempt_revision<1
             OR p_expected_fence IS NULL OR p_expected_fence<1
             OR p_expected_object_state_revision IS NULL OR p_expected_object_state_revision<1 THEN
            RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R4_ARGUMENT_INVALID'; END IF;
          actor_id:=tagekyc.raw_export_current_actor();
          SELECT x.* INTO probe FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p_attempt_id;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
          SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."AttemptId"=p_attempt_id FOR UPDATE;
          IF NOT FOUND AND (probe."StagedCiphertextFingerprintSchemaVersion"<>2 OR probe."StagedCiphertextFingerprint" IS NULL
             OR probe."StagedObjectCustodyId" IS NULL OR probe."StagedObjectStateRevision" IS NULL OR probe."StagedAtUtc" IS NULL) THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
          SELECT x.* INTO a FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p_attempt_id FOR UPDATE;
          SELECT x.* INTO h FROM tagekyc.raw_export_source_head x WHERE x."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
          SELECT x.* INTO r FROM tagekyc.raw_export_source_reservations x WHERE x."SourceArtifactId"=a."SourceArtifactId" FOR UPDATE;
          SELECT x.* INTO c FROM tagekyc.raw_export_source_ingress_claims x WHERE x."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
          SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=a."AttemptKeyReservationId" FOR UPDATE;
          SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=a."StagedObjectCustodyId" FOR UPDATE;
          IF a IS NULL OR h IS NULL OR r IS NULL OR c IS NULL OR k IS NULL OR o IS NULL
             OR h."CurrentEncryptionAttemptId"<>a."AttemptId" OR h."Fence"<>a."Fence"
             OR k."AttemptId"<>a."AttemptId" OR k."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
             OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
             OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
             OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
             OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
             OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
             OR o."State"<>'VerifiedCompleted' OR o."StateRevision"<>a."StagedObjectStateRevision"
             OR a."StagedCiphertextFingerprintSchemaVersion"<>2 OR a."StagedCiphertextFingerprint" IS NULL
             OR a."StagedObjectCustodyId"<>o."ObjectCustodyId" OR a."StagedAtUtc" IS NULL
             OR a."VerifiedPlaintextLength"<>r."ClaimedPlaintextLength"
             OR a."StagedCiphertextLength"<>o."CiphertextLength"
             OR a."StagedCiphertextDigest" IS DISTINCT FROM o."CiphertextDigest"
             OR a."StagedProviderReceiptDigest" IS DISTINCT FROM o."ProviderReceiptDigest"
             OR a."StagedVerificationEvidenceDigest" IS DISTINCT FROM o."VerificationEvidenceDigest" THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
          IF p."SourcePublicationId" IS NOT NULL THEN
            evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
              pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
              pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
              p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
              pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
              pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
              pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
            IF p."SourceArtifactId"=a."SourceArtifactId" AND p."ObjectCustodyId"=o."ObjectCustodyId"
               AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=k."AttemptKeyReservationId"
               AND p."StagedCiphertextFingerprintSchemaVersion"=a."StagedCiphertextFingerprintSchemaVersion"
               AND p."StagedCiphertextFingerprint"=a."StagedCiphertextFingerprint"
               AND p."CommitEvidenceDigest"=evidence
               AND a."StagedFromReservationRevision"+1=p_expected_reservation_revision
               AND a."EncryptionAttemptRevision"=p_expected_attempt_revision AND a."Fence"=p_expected_fence
               AND a."StagedObjectStateRevision"=p_expected_object_state_revision THEN
              RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."AttemptId",p."ObjectCustodyId",p."CommitEvidenceDigest",p."CommittedAtUtc",a."StagedFromReservationRevision"+1,h."Fence"; RETURN;
            END IF;
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN;
          END IF;
          IF h."CustodyState"<>'Staged' OR h."ReservationRevision"<>p_expected_reservation_revision
             OR a."EncryptionAttemptRevision"<>p_expected_attempt_revision OR a."Fence"<>p_expected_fence
             OR a."StagedObjectStateRevision"<>p_expected_object_state_revision OR k."PreparationDisposition"<>'Active'
             OR a."StagedFromReservationRevision"+1<>h."ReservationRevision" THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
          PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
          SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
          now_:=pg_catalog.clock_timestamp();
          SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",now_);
          IF NOT FOUND OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion" OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
             OR authority."ControllerIdentity"<>r."ControllerIdentity" OR authority."StableDataScopeId"<>r."StableDataScopeId"
             OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
             OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
             OR authority."ApprovedPurpose"<>'SubjectRawBiometricExport'
             OR consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
             OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
             OR consent."PurposeCode"<>'SubjectRawBiometricExport'
             OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
             OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">now_ OR (consent."ValidUntilUtc" IS NOT NULL AND now_>=consent."ValidUntilUtc")
             OR now_>=r."ReservationExpiresAtUtc" OR now_>=r."AbsoluteSourceExpiresAtUtc" THEN
            RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN; END IF;
          evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
            pg_catalog.replace(pg_catalog.lower(a."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(a."AttemptId"::text),'-',''),
            pg_catalog.replace(pg_catalog.lower(o."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(k."AttemptKeyReservationId"::text),'-',''),
            a."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(a."StagedCiphertextFingerprint",'hex'),authority."AuthoritySnapshotSchemaVersion"::text,
            pg_catalog.replace(pg_catalog.lower(authority."AuthoritySnapshotId"::text),'-',''),authority."Revision"::text,
            pg_catalog.replace(pg_catalog.lower(r."ConsentPolicyId"::text),'-',''),r."ConsentPolicyVersion"::text,
            pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r4-commit-v1',true);
          INSERT INTO tagekyc.raw_export_source_publications VALUES(publication_id,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",k."AttemptKeyReservationId",
            2,a."StagedCiphertextFingerprint",authority."AuthoritySnapshotSchemaVersion",authority."AuthoritySnapshotId",authority."Revision",r."ConsentPolicyId",r."ConsentPolicyVersion",
            evidence,now_,'Committed',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'NotPlanned',NULL,NULL,1,1)
          ON CONFLICT DO NOTHING;
          IF NOT FOUND THEN
            SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."AttemptId"=p_attempt_id;
            evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
              pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
              pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
              p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
              pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
              pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
              pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
            IF p."SourceArtifactId"=a."SourceArtifactId" AND p."ObjectCustodyId"=o."ObjectCustodyId"
               AND p."AttemptId"=a."AttemptId" AND p."AttemptKeyReservationId"=k."AttemptKeyReservationId"
               AND p."StagedCiphertextFingerprintSchemaVersion"=a."StagedCiphertextFingerprintSchemaVersion"
               AND p."StagedCiphertextFingerprint"=a."StagedCiphertextFingerprint"
               AND p."CommitEvidenceDigest"=evidence
               AND a."StagedFromReservationRevision"+1=p_expected_reservation_revision
               AND a."EncryptionAttemptRevision"=p_expected_attempt_revision AND a."Fence"=p_expected_fence
               AND a."StagedObjectStateRevision"=p_expected_object_state_revision THEN
              PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
              RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."AttemptId",p."ObjectCustodyId",p."CommitEvidenceDigest",p."CommittedAtUtc",a."StagedFromReservationRevision"+1,h."Fence"; RETURN;
            END IF;
            PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::timestamptz,NULL::bigint,NULL::bigint; RETURN;
          END IF;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
          RETURN QUERY SELECT 'Committed'::text,publication_id,a."SourceArtifactId",a."AttemptId",o."ObjectCustodyId",evidence,now_,h."ReservationRevision",h."Fence";
        EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_publish_available_source(p_source_publication_id uuid,p_expected_reservation_revision bigint,p_expected_fence bigint)
        RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"SourceArtifactId" uuid,"OpaqueCommittedLocatorId" uuid,
          "AvailableEvidenceDigest" bytea,"ReservationRevision" bigint,"Fence" bigint,"AvailableAtUtc" timestamptz,"CleanupDisposition" text)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE actor_id uuid; p record; a record; h record; r record; c record; k record; o record; consent record; authority record;
          now_ timestamptz; locator uuid:=pg_catalog.gen_random_uuid(); evidence bytea; commit_evidence bytea; cleanup_digest bytea; item_count bigint;
          prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
          core_prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true);
        BEGIN
          IF p_source_publication_id IS NULL OR p_source_publication_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_expected_reservation_revision IS NULL OR p_expected_reservation_revision<1
             OR p_expected_fence IS NULL OR p_expected_fence<1 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R5_ARGUMENT_INVALID'; END IF;
          actor_id:=tagekyc.raw_export_current_actor();
          SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."SourcePublicationId"=p_source_publication_id FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
          PERFORM 1 FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."AttemptId" FOR UPDATE;
          SELECT x.* INTO a FROM tagekyc.raw_export_source_encryption_attempts x WHERE x."AttemptId"=p."AttemptId";
          SELECT x.* INTO h FROM tagekyc.raw_export_source_head x WHERE x."SourceArtifactId"=p."SourceArtifactId" FOR UPDATE;
          SELECT x.* INTO r FROM tagekyc.raw_export_source_reservations x WHERE x."SourceArtifactId"=p."SourceArtifactId" FOR UPDATE;
          SELECT x.* INTO c FROM tagekyc.raw_export_source_ingress_claims x WHERE x."IngressClaimId"=r."IngressClaimId" FOR UPDATE;
          PERFORM 1 FROM tagekyc.raw_export_attempt_key_reservations x JOIN tagekyc.raw_export_source_encryption_attempts y ON y."AttemptId"=x."AttemptId" WHERE y."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."AttemptKeyReservationId" FOR UPDATE OF x;
          SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=p."AttemptKeyReservationId";
          PERFORM 1 FROM tagekyc.raw_export_provisional_objects x WHERE x."SourceArtifactId"=p."SourceArtifactId" ORDER BY x."ObjectCustodyId" FOR UPDATE;
          SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=p."ObjectCustodyId";
          IF p."PublicationState"='Available' THEN
            evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-available-v1',VARIADIC ARRAY[
              pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
              pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),
              pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."OpaqueCommittedLocatorId"::text),'-',''),
              pg_catalog.encode(p."CommitEvidenceDigest",'hex'),p."PublishedAuthoritySnapshotSchemaVersion"::text,
              pg_catalog.replace(pg_catalog.lower(p."PublishedAuthoritySnapshotId"::text),'-',''),p."PublishedAuthorityRevision"::text,
              pg_catalog.replace(pg_catalog.lower(p."PublishedConsentPolicyId"::text),'-',''),p."PublishedConsentPolicyVersion"::text,
              pg_catalog.to_char(p."AvailableAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
            IF h."CustodyState"='Available' AND h."CurrentEncryptionAttemptId"=p."AttemptId"
               AND h."ReservationRevision"=p_expected_reservation_revision+1 AND h."Fence"=p_expected_fence
               AND a."SourceArtifactId"=p."SourceArtifactId" AND a."AttemptKeyReservationId"=p."AttemptKeyReservationId"
               AND a."StagedObjectCustodyId"=p."ObjectCustodyId"
               AND a."StagedCiphertextFingerprintSchemaVersion"=p."StagedCiphertextFingerprintSchemaVersion"
               AND a."StagedCiphertextFingerprint"=p."StagedCiphertextFingerprint"
               AND k."AttemptId"=p."AttemptId" AND o."AttemptId"=p."AttemptId"
               AND p."AvailableEvidenceDigest"=evidence THEN
              RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."SourceArtifactId",p."OpaqueCommittedLocatorId",p."AvailableEvidenceDigest",h."ReservationRevision",h."Fence",p."AvailableAtUtc",p."CleanupDisposition"::text; RETURN; END IF;
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN;
          END IF;
          commit_evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-commit-v1',VARIADIC ARRAY[
            pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),
            pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),
            p."StagedCiphertextFingerprintSchemaVersion"::text,pg_catalog.encode(p."StagedCiphertextFingerprint",'hex'),p."CommittedAuthoritySnapshotSchemaVersion"::text,
            pg_catalog.replace(pg_catalog.lower(p."CommittedAuthoritySnapshotId"::text),'-',''),p."CommittedAuthorityRevision"::text,
            pg_catalog.replace(pg_catalog.lower(p."CommittedConsentPolicyId"::text),'-',''),p."CommittedConsentPolicyVersion"::text,
            pg_catalog.to_char(p."CommittedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
          IF p."PublicationState"<>'Committed' OR p."PublicationRevision"<>1 OR p."CleanupDisposition"<>'NotPlanned'
             OR h."CustodyState"<>'Staged' OR h."ReservationRevision"<>p_expected_reservation_revision OR h."Fence"<>p_expected_fence
             OR h."CurrentEncryptionAttemptId"<>p."AttemptId" OR a."SourceArtifactId"<>p."SourceArtifactId"
             OR a."AttemptKeyReservationId"<>p."AttemptKeyReservationId" OR a."StagedObjectCustodyId"<>p."ObjectCustodyId"
             OR a."StagedCiphertextFingerprintSchemaVersion"<>p."StagedCiphertextFingerprintSchemaVersion"
             OR a."StagedCiphertextFingerprint" IS DISTINCT FROM p."StagedCiphertextFingerprint"
             OR a."StagedFromReservationRevision"+1<>h."ReservationRevision"
             OR k."AttemptId"<>a."AttemptId" OR k."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
             OR o."AttemptId"<>a."AttemptId" OR o."AttemptKeyReservationId"<>a."AttemptKeyReservationId"
             OR o."SourceArtifactId"<>a."SourceArtifactId" OR o."ProvisionalObjectIdentity"<>a."ProvisionalObjectIdentity"
             OR o."EncryptionAttemptRevision"<>a."EncryptionAttemptRevision" OR o."AttemptFence"<>a."Fence"
             OR o."EncryptionAttemptFingerprint" IS DISTINCT FROM a."EncryptionAttemptFingerprint"
             OR o."State"<>'VerifiedCompleted' OR o."StateRevision"<>a."StagedObjectStateRevision"
             OR o."VerificationEvidenceDigest" IS DISTINCT FROM a."StagedVerificationEvidenceDigest"
             OR p."CommitEvidenceDigest"<>commit_evidence OR k."PreparationDisposition"<>'Active' THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
          PERFORM pg_catalog.pg_advisory_xact_lock_shared(pg_catalog.hashtext('tip88c1:b2-authority:'||c."ClientApplicationId"::text||':'||c."VerificationSessionId"::text||':'||c."CaptureAcceptanceId"::text||':'||c."RawClass"));
          SELECT * INTO consent FROM tagekyc.raw_export_resolve_subject_consent_for_authorization(c."VerificationSessionId",r."ConsentPolicyId",r."ConsentPolicyVersion");
          now_:=pg_catalog.clock_timestamp();
          SELECT * INTO authority FROM tagekyc.raw_export_resolve_current_authority_for_source(c."ClientApplicationId",c."VerificationSessionId",c."CaptureAcceptanceId",c."RawClass",now_);
          IF NOT FOUND OR authority."AuthoritySnapshotSchemaVersion"<>r."AuthoritySnapshotSchemaVersion"
             OR authority."AuthoritySnapshotId"<>r."AuthoritySnapshotId"
             OR authority."ControllerIdentity"<>r."ControllerIdentity" OR authority."StableDataScopeId"<>r."StableDataScopeId"
             OR authority."ConsentPolicyId"<>r."ConsentPolicyId" OR authority."ConsentPolicyVersion"<>r."ConsentPolicyVersion"
             OR authority."AbsoluteSourceExpiresAtUtc"<>r."AbsoluteSourceExpiresAtUtc"
             OR authority."ApprovedPurpose"<>'SubjectRawBiometricExport'
             OR consent."State"<>'Effective' OR consent."VerificationSessionId"<>c."VerificationSessionId"
             OR consent."PolicyId"<>r."ConsentPolicyId" OR consent."PolicyVersion"<>r."ConsentPolicyVersion"
             OR consent."PurposeCode"<>'SubjectRawBiometricExport'
             OR consent."RecipientClientApplicationId"<>c."ClientApplicationId" OR consent."RawClass"<>c."RawClass"
             OR consent."ValidFromUtc" IS NULL OR consent."ValidFromUtc">now_ OR (consent."ValidUntilUtc" IS NOT NULL AND now_>=consent."ValidUntilUtc")
             OR now_>=r."ReservationExpiresAtUtc" OR now_>=r."AbsoluteSourceExpiresAtUtc" THEN
            RETURN QUERY SELECT 'SourceRetentionNotAuthorized'::text,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::timestamptz,NULL::text; RETURN; END IF;
          evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-available-v1',VARIADIC ARRAY[
            pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
            pg_catalog.replace(pg_catalog.lower(p."AttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."ObjectCustodyId"::text),'-',''),
            pg_catalog.replace(pg_catalog.lower(p."AttemptKeyReservationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(locator::text),'-',''),
            pg_catalog.encode(p."CommitEvidenceDigest",'hex'),authority."AuthoritySnapshotSchemaVersion"::text,
            pg_catalog.replace(pg_catalog.lower(authority."AuthoritySnapshotId"::text),'-',''),authority."Revision"::text,
            pg_catalog.replace(pg_catalog.lower(r."ConsentPolicyId"::text),'-',''),r."ConsentPolicyVersion"::text,
            pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r5-publish-v1',true);
          INSERT INTO tagekyc.raw_export_source_cleanup_items
          SELECT pg_catalog.gen_random_uuid(),p."SourcePublicationId",'ProvisionalObject',x."ObjectCustodyId",x."AttemptId",x."StateRevision",'Pending',NULL,NULL,NULL,1,1
          FROM tagekyc.raw_export_provisional_objects x WHERE x."SourceArtifactId"=p."SourceArtifactId" AND x."ObjectCustodyId"<>p."ObjectCustodyId"
            AND x."State" IN ('PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','ObjectConflict','CleanupPending','Deleted','Quarantined');
          INSERT INTO tagekyc.raw_export_source_cleanup_items
          SELECT pg_catalog.gen_random_uuid(),p."SourcePublicationId",'AttemptKeyReservation',x."AttemptKeyReservationId",x."AttemptId",x."RowRevision",'Pending',NULL,NULL,NULL,1,1
          FROM tagekyc.raw_export_attempt_key_reservations x JOIN tagekyc.raw_export_source_encryption_attempts y ON y."AttemptId"=x."AttemptId"
          WHERE y."SourceArtifactId"=p."SourceArtifactId" AND x."AttemptKeyReservationId"<>p."AttemptKeyReservationId"
            AND NOT (x."PreparationDisposition"='ReadyForFreshPreparation' AND x."WrappedDekCiphertext" IS NULL AND x."CurrentProviderOperationToken" IS NULL);
          SELECT pg_catalog.count(*) INTO item_count FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId";
          IF item_count=0 THEN cleanup_digest:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-finalization-cleanup-v1',VARIADIC ARRAY[
              pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),'NoObsoleteResidue','0',
              pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]); END IF;
          UPDATE tagekyc.raw_export_source_publications x SET "PublicationState"='Available',"OpaqueCommittedLocatorId"=locator,
            "PublishedAuthoritySnapshotSchemaVersion"=authority."AuthoritySnapshotSchemaVersion","PublishedAuthoritySnapshotId"=authority."AuthoritySnapshotId",
            "PublishedAuthorityRevision"=authority."Revision","PublishedConsentPolicyId"=r."ConsentPolicyId","PublishedConsentPolicyVersion"=r."ConsentPolicyVersion",
            "AvailableEvidenceDigest"=evidence,"AvailableAtUtc"=now_,"CleanupDisposition"=CASE WHEN item_count>0 THEN 'Pending' ELSE 'NoObsoleteResidue' END,
            "CleanupEvidenceDigest"=cleanup_digest,"FinalizedAtUtc"=CASE WHEN item_count=0 THEN now_ ELSE NULL END,"PublicationRevision"=2
            WHERE x."SourcePublicationId"=p."SourcePublicationId";
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r5-publish-v1',true);
          UPDATE tagekyc.raw_export_source_head x SET "CustodyState"='Available',"ReservationRevision"=x."ReservationRevision"+1
            WHERE x."SourceArtifactId"=p."SourceArtifactId" AND x."CustodyState"='Staged' AND x."ReservationRevision"=p_expected_reservation_revision AND x."Fence"=p_expected_fence;
          IF NOT FOUND THEN RAISE EXCEPTION 'RAW_EXPORT_R5_HEAD_CAS_FAILED'; END IF;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(core_prior,''),true);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
          RETURN QUERY SELECT 'Available'::text,p."SourcePublicationId",p."SourceArtifactId",locator,evidence,p_expected_reservation_revision+1,p_expected_fence,now_,CASE WHEN item_count>0 THEN 'Pending' ELSE 'NoObsoleteResidue' END;
        EXCEPTION WHEN OTHERS THEN
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_core_write_context',COALESCE(core_prior,''),true);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_read_next_source_cleanup_item(p_source_publication_id uuid,p_expected_publication_revision bigint)
        RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"PublicationRevision" bigint,"CleanupItemId" uuid,"ResourceKind" text,
          "ResourceId" uuid,"ResourceAttemptId" uuid,"CleanupItemRevision" bigint,"PlannedResourceRevision" bigint)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE actor_id uuid; p record; i record;
        BEGIN
          IF p_source_publication_id IS NULL OR p_source_publication_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_expected_publication_revision IS NULL OR p_expected_publication_revision<1 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R6_ARGUMENT_INVALID'; END IF;
          actor_id:=tagekyc.raw_export_current_actor();
          SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."SourcePublicationId"=p_source_publication_id FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
          IF p."PublicationState"<>'Available' OR p."PublicationRevision"<>p_expected_publication_revision THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
          SELECT x.* INTO i FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId" AND x."CleanupState"='Pending' ORDER BY x."CleanupItemId" LIMIT 1 FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NoPendingItem'::text,p."SourcePublicationId",p."PublicationRevision",NULL::uuid,NULL::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF;
          IF (i."ResourceKind"='ProvisionalObject' AND i."ResourceId"=p."ObjectCustodyId")
             OR (i."ResourceKind"='AttemptKeyReservation' AND i."ResourceId"=p."AttemptKeyReservationId") THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint; RETURN;
          END IF;
          RETURN QUERY SELECT 'ItemAvailable'::text,p."SourcePublicationId",p."PublicationRevision",i."CleanupItemId",i."ResourceKind"::text,i."ResourceId",i."ResourceAttemptId",i."RowRevision",i."PlannedResourceRevision";
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_complete_source_cleanup_item(p_cleanup_item_id uuid,p_expected_cleanup_item_revision bigint)
        RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"PublicationRevision" bigint,"CleanupItemId" uuid,"ResourceKind" text,
          "ResourceId" uuid,"CompletionDisposition" text,"CleanupEvidenceDigest" bytea,"CompletedAtUtc" timestamptz,"CleanupItemRevision" bigint)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE actor_id uuid; probe record; p record; i record; o record; k record; e record; terminal text; underlying bytea; now_ timestamptz; evidence bytea;
          prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
        BEGIN
          IF p_cleanup_item_id IS NULL OR p_cleanup_item_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_expected_cleanup_item_revision IS NULL OR p_expected_cleanup_item_revision<1 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R6_ARGUMENT_INVALID'; END IF;
          actor_id:=tagekyc.raw_export_current_actor(); SELECT x.* INTO probe FROM tagekyc.raw_export_source_cleanup_items x WHERE x."CleanupItemId"=p_cleanup_item_id;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN; END IF;
          SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."SourcePublicationId"=probe."SourcePublicationId" FOR UPDATE;
          IF probe."ResourceKind"='ProvisionalObject' THEN
            SELECT x.* INTO o FROM tagekyc.raw_export_provisional_objects x WHERE x."ObjectCustodyId"=probe."ResourceId" FOR UPDATE;
          ELSE SELECT x.* INTO k FROM tagekyc.raw_export_attempt_key_reservations x WHERE x."AttemptKeyReservationId"=probe."ResourceId" FOR UPDATE; END IF;
          SELECT x.* INTO i FROM tagekyc.raw_export_source_cleanup_items x WHERE x."CleanupItemId"=p_cleanup_item_id FOR UPDATE;
          IF i."ResourceKind"='ProvisionalObject' THEN
            IF o."State"='Deleted' THEN terminal:='Deleted'; underlying:=o."DeletionEvidenceDigest";
            ELSIF o."State"='Quarantined' THEN terminal:='Quarantined'; underlying:=o."QuarantineEvidenceDigest"; END IF;
          ELSE
            SELECT x.* INTO e FROM tagekyc.raw_export_attempt_key_preparation_events x WHERE x."AttemptKeyReservationId"=k."AttemptKeyReservationId" AND x."EventKind" IN ('Revoked','ProviderOperationAbandoned') ORDER BY x."EventSequence" DESC LIMIT 1 FOR UPDATE;
            IF k."PreparationDisposition"='Revoked' AND e."EventKind"='Revoked'
               AND e."RevocationReasonCode"='SourceFinalizationSupersededAttempt' THEN terminal:='Revoked'; underlying:=e."RevocationEvidenceDigest";
            ELSIF k."PreparationDisposition"='ReservationAbandoned' AND e."EventKind"='ProviderOperationAbandoned'
               AND e."OperatorReasonCode"='SourceFinalizationSupersededAttempt' THEN terminal:='ReservationAbandoned'; underlying:=e."AbandonmentEvidenceDigest"; END IF;
          END IF;
          IF i."SourcePublicationId"<>p."SourcePublicationId" OR i."ResourceKind"<>probe."ResourceKind"
             OR i."ResourceId"<>probe."ResourceId" OR i."ResourceAttemptId"<>probe."ResourceAttemptId" THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
          END IF;
          IF (i."ResourceKind"='ProvisionalObject' AND i."ResourceId"=p."ObjectCustodyId")
             OR (i."ResourceKind"='AttemptKeyReservation' AND i."ResourceId"=p."AttemptKeyReservationId") THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
          END IF;
          IF i."CleanupState"='Completed' THEN
            IF p_expected_cleanup_item_revision=1 AND terminal=i."CompletionDisposition" AND underlying IS NOT NULL THEN
              evidence:=tagekyc.raw_export_c1_hash_canonical(CASE WHEN i."ResourceKind"='ProvisionalObject' THEN 'tip-88c1-source-object-cleanup-item-v1' ELSE 'tip-88c1-source-key-cleanup-item-v1' END,VARIADIC ARRAY[
                pg_catalog.replace(pg_catalog.lower(i."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(i."ResourceAttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(i."ResourceId"::text),'-',''),terminal,pg_catalog.encode(underlying,'hex'),pg_catalog.to_char(i."CompletedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
              IF evidence=i."CleanupEvidenceDigest" THEN RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."PublicationRevision",i."CleanupItemId",i."ResourceKind"::text,i."ResourceId",i."CompletionDisposition"::text,i."CleanupEvidenceDigest",i."CompletedAtUtc",i."RowRevision"; RETURN; END IF;
            END IF;
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
          END IF;
          IF p."PublicationState"<>'Available' OR p."CleanupDisposition"<>'Pending'
             OR i."RowRevision"<>p_expected_cleanup_item_revision OR i."RowRevision"<>1 THEN
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
          END IF;
          IF i."ResourceKind"='ProvisionalObject' THEN
            IF o."AttemptId"<>i."ResourceAttemptId" OR o."StateRevision"<i."PlannedResourceRevision" THEN
              RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
            END IF;
          ELSIF i."ResourceKind"='AttemptKeyReservation' THEN
            IF k."AttemptId"<>i."ResourceAttemptId" OR k."RowRevision"<i."PlannedResourceRevision" THEN
              RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
            END IF;
          ELSE
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::uuid,NULL::text,NULL::uuid,NULL::text,NULL::bytea,NULL::timestamptz,NULL::bigint; RETURN;
          END IF;
          IF terminal IS NULL OR underlying IS NULL OR pg_catalog.octet_length(underlying)<>32 THEN
            RETURN QUERY SELECT 'ResourceNotTerminal'::text,p."SourcePublicationId",p."PublicationRevision",i."CleanupItemId",i."ResourceKind"::text,i."ResourceId",NULL::text,NULL::bytea,NULL::timestamptz,i."RowRevision"; RETURN;
          END IF;
          now_:=pg_catalog.clock_timestamp(); evidence:=tagekyc.raw_export_c1_hash_canonical(CASE WHEN i."ResourceKind"='ProvisionalObject' THEN 'tip-88c1-source-object-cleanup-item-v1' ELSE 'tip-88c1-source-key-cleanup-item-v1' END,VARIADIC ARRAY[
            pg_catalog.replace(pg_catalog.lower(i."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(i."ResourceAttemptId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(i."ResourceId"::text),'-',''),terminal,pg_catalog.encode(underlying,'hex'),pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r6-cleanup-v1',true);
          UPDATE tagekyc.raw_export_source_cleanup_items x SET "CleanupState"='Completed',"CompletionDisposition"=terminal,"CleanupEvidenceDigest"=evidence,"CompletedAtUtc"=now_,"RowRevision"=2 WHERE x."CleanupItemId"=i."CleanupItemId";
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
          RETURN QUERY SELECT 'Completed'::text,p."SourcePublicationId",p."PublicationRevision",i."CleanupItemId",i."ResourceKind"::text,i."ResourceId",terminal,evidence,now_,2::bigint;
        EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_finalize_source_cleanup(p_source_publication_id uuid,p_expected_publication_revision bigint)
        RETURNS TABLE("Outcome" text,"SourcePublicationId" uuid,"PublicationRevision" bigint,"CleanupDisposition" text,"CleanupEvidenceDigest" bytea,"FinalizedAtUtc" timestamptz)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE actor_id uuid; p record; pending bigint; completed bigint; now_ timestamptz; evidence bytea;
          prior text:=pg_catalog.current_setting('tagekyc.raw_export_source_finalization_write_context',true);
        BEGIN
          IF p_source_publication_id IS NULL OR p_source_publication_id='00000000-0000-0000-0000-000000000000'::uuid
             OR p_expected_publication_revision IS NULL OR p_expected_publication_revision<1 THEN RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='RAW_EXPORT_R6_ARGUMENT_INVALID'; END IF;
          actor_id:=tagekyc.raw_export_current_actor(); SELECT x.* INTO p FROM tagekyc.raw_export_source_publications x WHERE x."SourcePublicationId"=p_source_publication_id FOR UPDATE;
          IF NOT FOUND THEN RETURN QUERY SELECT 'NotFound'::text,NULL::uuid,NULL::bigint,NULL::text,NULL::bytea,NULL::timestamptz; RETURN; END IF;
          PERFORM 1 FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId" ORDER BY x."CleanupItemId" FOR UPDATE;
          IF p."CleanupDisposition"='Completed' THEN
            IF p_expected_publication_revision=2 THEN
              SELECT pg_catalog.count(*) INTO completed FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId" AND x."CleanupState"='Completed';
              evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-finalization-cleanup-v1',VARIADIC ARRAY[pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),'Completed',completed::text,pg_catalog.to_char(p."FinalizedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
              IF evidence=p."CleanupEvidenceDigest" THEN RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."PublicationRevision",p."CleanupDisposition"::text,p."CleanupEvidenceDigest",p."FinalizedAtUtc"; RETURN; END IF;
            END IF; RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::text,NULL::bytea,NULL::timestamptz; RETURN;
          END IF;
          IF p."CleanupDisposition"='NoObsoleteResidue' AND p."PublicationRevision"=2 AND p_expected_publication_revision=2 THEN
            evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-finalization-cleanup-v1',VARIADIC ARRAY[
              pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),
              'NoObsoleteResidue','0',pg_catalog.to_char(p."FinalizedAtUtc" AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
            IF evidence=p."CleanupEvidenceDigest" THEN RETURN QUERY SELECT 'ExistingMatch'::text,p."SourcePublicationId",p."PublicationRevision",p."CleanupDisposition"::text,p."CleanupEvidenceDigest",p."FinalizedAtUtc"; RETURN; END IF;
            RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::text,NULL::bytea,NULL::timestamptz; RETURN;
          END IF;
          IF p."PublicationState"<>'Available' OR p."CleanupDisposition"<>'Pending' OR p."PublicationRevision"<>p_expected_publication_revision OR p."PublicationRevision"<>2 THEN RETURN QUERY SELECT 'StateConflict'::text,NULL::uuid,NULL::bigint,NULL::text,NULL::bytea,NULL::timestamptz; RETURN; END IF;
          SELECT pg_catalog.count(*) FILTER(WHERE x."CleanupState"='Pending'),pg_catalog.count(*) FILTER(WHERE x."CleanupState"='Completed') INTO pending,completed FROM tagekyc.raw_export_source_cleanup_items x WHERE x."SourcePublicationId"=p."SourcePublicationId";
          IF pending>0 THEN RETURN QUERY SELECT 'CleanupPending'::text,p."SourcePublicationId",p."PublicationRevision",p."CleanupDisposition"::text,NULL::bytea,NULL::timestamptz; RETURN; END IF;
          now_:=pg_catalog.clock_timestamp(); evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-source-finalization-cleanup-v1',VARIADIC ARRAY[
            pg_catalog.replace(pg_catalog.lower(p."SourcePublicationId"::text),'-',''),pg_catalog.replace(pg_catalog.lower(p."SourceArtifactId"::text),'-',''),'Completed',completed::text,pg_catalog.to_char(now_ AT TIME ZONE 'UTC','YYYY-MM-DD"T"HH24:MI:SS.US"Z"')]);
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r6-cleanup-v1',true);
          UPDATE tagekyc.raw_export_source_publications x SET "CleanupDisposition"='Completed',"CleanupEvidenceDigest"=evidence,"FinalizedAtUtc"=now_,"PublicationRevision"=3 WHERE x."SourcePublicationId"=p."SourcePublicationId";
          PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true);
          RETURN QUERY SELECT 'Completed'::text,p."SourcePublicationId",3::bigint,'Completed'::text,evidence,now_;
        EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true); RAISE;
        END $fn$;

        ALTER FUNCTION tagekyc.enforce_raw_export_source_publication_write() OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.enforce_raw_export_source_cleanup_item_write() OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.enforce_raw_export_source_head_write() OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_publish_available_source(uuid,bigint,bigint) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_finalize_source_cleanup(uuid,bigint) OWNER TO tagekyc_raw_export_deployer;
        REVOKE ALL ON TABLE tagekyc.raw_export_source_publications,tagekyc.raw_export_source_cleanup_items FROM PUBLIC;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint),tagekyc.raw_export_publish_available_source(uuid,bigint,bigint),tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint),tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint),tagekyc.raw_export_finalize_source_cleanup(uuid,bigint) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint),tagekyc.raw_export_publish_available_source(uuid,bigint,bigint),tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint) TO tagekyc_raw_export_reconciler;
        GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint),tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint),tagekyc.raw_export_finalize_source_cleanup(uuid,bigint) TO tagekyc_raw_export_lifecycle;
        """;

        private const string DownPreflightSql = """
        DO $down$ BEGIN
          IF EXISTS(SELECT 1 FROM tagekyc.raw_export_source_publications) OR EXISTS(SELECT 1 FROM tagekyc.raw_export_source_cleanup_items) THEN
            RAISE EXCEPTION 'RAW_EXPORT_SOURCE_FINALIZATION_DOWN_OCCUPIED'; END IF;
        END $down$;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint),tagekyc.raw_export_publish_available_source(uuid,bigint,bigint),tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint),tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint),tagekyc.raw_export_finalize_source_cleanup(uuid,bigint) FROM tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle;
        DROP FUNCTION tagekyc.raw_export_finalize_source_cleanup(uuid,bigint);
        DROP FUNCTION tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint);
        DROP FUNCTION tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint);
        DROP FUNCTION tagekyc.raw_export_publish_available_source(uuid,bigint,bigint);
        DROP FUNCTION tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint);
        DROP TRIGGER tr_raw_export_source_cleanup_item_guard ON tagekyc.raw_export_source_cleanup_items;
        DROP TRIGGER tr_raw_export_source_publication_guard ON tagekyc.raw_export_source_publications;
        DROP FUNCTION tagekyc.enforce_raw_export_source_cleanup_item_write();
        DROP FUNCTION tagekyc.enforce_raw_export_source_publication_write();
        """;

        private const string DownRestoreSql = """
        CREATE OR REPLACE FUNCTION tagekyc.enforce_raw_export_source_head_write()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $guard$
        BEGIN
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='complete-r1'
             AND TG_OP<>'DELETE' THEN
            RETURN NEW;
          END IF;
          IF current_user='tagekyc_raw_export_deployer'
             AND pg_catalog.current_setting('tagekyc.raw_export_source_core_write_context',true)='tip88c1-r3-stage-v1'
             AND TG_OP='UPDATE'
             AND OLD."CustodyState"='Reserved' AND NEW."CustodyState"='Staged'
             AND NEW."ReservationRevision"=OLD."ReservationRevision"+1
             AND ROW(NEW."SourceArtifactId",NEW."CurrentEncryptionAttemptId",NEW."Fence")
                 IS NOT DISTINCT FROM ROW(OLD."SourceArtifactId",OLD."CurrentEncryptionAttemptId",OLD."Fence") THEN
            RETURN NEW;
          END IF;
          RAISE EXCEPTION 'RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN';
        END $guard$;
        ALTER FUNCTION tagekyc.enforce_raw_export_source_head_write() OWNER TO tagekyc_raw_export_deployer;
        """;
    }
}
