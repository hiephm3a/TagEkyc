using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations;

public partial class OpenBaoProductionKekProvider : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PreflightSql);
        DropChangedConstraints(migrationBuilder);

        migrationBuilder.AddColumn<string>(name: "MaterialRepresentationId", table: "raw_export_attempt_key_reservations", schema: "tagekyc", type: "character varying(512)", maxLength: 512, nullable: true);
        migrationBuilder.AddColumn<int>(name: "MaterialRepresentationVersion", table: "raw_export_attempt_key_reservations", schema: "tagekyc", type: "integer", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "OpaqueWrappedDekPayload", table: "raw_export_attempt_key_reservations", schema: "tagekyc", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<string>(name: "MaterialRepresentationId", table: "raw_export_key_provider_operations", schema: "tagekyc", type: "character varying(512)", maxLength: 512, nullable: true);
        migrationBuilder.AddColumn<int>(name: "MaterialRepresentationVersion", table: "raw_export_key_provider_operations", schema: "tagekyc", type: "integer", nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "OpaqueWrappedDekPayload", table: "raw_export_key_provider_operations", schema: "tagekyc", type: "bytea", nullable: true);

        migrationBuilder.CreateTable(
            name: "raw_export_openbao_kek_operation_journal", schema: "tagekyc",
            columns: table => new
            {
                ProviderOperationId = table.Column<Guid>("uuid", nullable: false),
                ProviderOperationTokenDigest = table.Column<byte[]>("bytea", nullable: false),
                AttemptKeyContextFingerprint = table.Column<byte[]>("bytea", nullable: false),
                PreparationId = table.Column<Guid>("uuid", nullable: false),
                PreparationFence = table.Column<long>("bigint", nullable: false),
                KeyProviderId = table.Column<string>("character varying(512)", maxLength: 512, nullable: false),
                KekId = table.Column<string>("character varying(512)", maxLength: 512, nullable: false),
                KekVersion = table.Column<int>("integer", nullable: false),
                KekFingerprint = table.Column<string>("character varying(512)", maxLength: 512, nullable: false),
                JournalState = table.Column<string>("text", nullable: false),
                MaterialRepresentationId = table.Column<string>("character varying(512)", maxLength: 512, nullable: false),
                MaterialRepresentationVersion = table.Column<int>("integer", nullable: false),
                WrappingSchemeId = table.Column<string>("character varying(512)", maxLength: 512, nullable: false),
                WrappingSchemeVersion = table.Column<int>("integer", nullable: false),
                OpaqueWrappedDekPayload = table.Column<byte[]>("bytea", nullable: true),
                ProviderResourceReference = table.Column<string>("character varying(512)", maxLength: 512, nullable: true),
                ProviderOperationReceipt = table.Column<string>("character varying(512)", maxLength: 512, nullable: true),
                ProviderAbsenceProofReceipt = table.Column<string>("character varying(512)", maxLength: 512, nullable: true),
                ProviderCleanupReference = table.Column<string>("character varying(512)", maxLength: 512, nullable: true),
                ProviderCleanupReceipt = table.Column<string>("character varying(512)", maxLength: 512, nullable: true),
                RowRevision = table.Column<long>("bigint", nullable: false),
                IssuedAtUtc = table.Column<DateTimeOffset>("timestamp with time zone", nullable: false),
                ResultObservedAtUtc = table.Column<DateTimeOffset>("timestamp with time zone", nullable: true),
                CleanedAtUtc = table.Column<DateTimeOffset>("timestamp with time zone", nullable: true),
                UpdatedAtUtc = table.Column<DateTimeOffset>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_raw_export_openbao_kek_operation_journal", x => x.ProviderOperationId);
                table.ForeignKey(
                    name: "fk_raw_export_openbao_kek_journal_provider_operation",
                    column: x => x.ProviderOperationId,
                    principalSchema: "tagekyc",
                    principalTable: "raw_export_key_provider_operations",
                    principalColumn: "ProviderOperationId",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_raw_export_openbao_kek_journal_values", OpenBaoJournalConstraint);
            });

        migrationBuilder.Sql("""
            ALTER TABLE tagekyc.raw_export_openbao_kek_operation_journal
              OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON TABLE tagekyc.raw_export_openbao_kek_operation_journal FROM PUBLIC;
            """);

        migrationBuilder.Sql("""
            SET LOCAL ROLE tagekyc_raw_export_deployer;
            SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
            UPDATE tagekyc.raw_export_attempt_key_reservations
            SET "MaterialRepresentationId"='LEGACY_AES_GCM_SPLIT', "MaterialRepresentationVersion"=1;
            UPDATE tagekyc.raw_export_key_provider_operations
            SET "MaterialRepresentationId"='LEGACY_AES_GCM_SPLIT', "MaterialRepresentationVersion"=1
            WHERE "ProviderOperationState" IN ('ResultObserved','CleanupRequired','CleanedUp');
            SET CONSTRAINTS ALL IMMEDIATE;
            RESET ROLE;
            SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','',true);
            """);
        migrationBuilder.AlterColumn<string>(name: "MaterialRepresentationId", table: "raw_export_attempt_key_reservations", schema: "tagekyc",
            type: "character varying(512)", maxLength: 512, nullable: false, oldClrType: typeof(string), oldType: "character varying(512)", oldMaxLength: 512, oldNullable: true);
        migrationBuilder.AlterColumn<int>(name: "MaterialRepresentationVersion", table: "raw_export_attempt_key_reservations", schema: "tagekyc",
            type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);

        AddCurrentConstraints(migrationBuilder);
        migrationBuilder.Sql(UpProviderSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DownProviderSql);
        DropChangedConstraints(migrationBuilder);
        migrationBuilder.DropTable("raw_export_openbao_kek_operation_journal", "tagekyc");
        migrationBuilder.DropColumn("MaterialRepresentationId", "raw_export_key_provider_operations", "tagekyc");
        migrationBuilder.DropColumn("MaterialRepresentationVersion", "raw_export_key_provider_operations", "tagekyc");
        migrationBuilder.DropColumn("OpaqueWrappedDekPayload", "raw_export_key_provider_operations", "tagekyc");
        migrationBuilder.DropColumn("MaterialRepresentationId", "raw_export_attempt_key_reservations", "tagekyc");
        migrationBuilder.DropColumn("MaterialRepresentationVersion", "raw_export_attempt_key_reservations", "tagekyc");
        migrationBuilder.DropColumn("OpaqueWrappedDekPayload", "raw_export_attempt_key_reservations", "tagekyc");
        AddLegacyConstraints(migrationBuilder);
    }

    private static void DropChangedConstraints(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("ck_raw_export_attempt_key_event_values", "raw_export_attempt_key_preparation_events", "tagekyc");
        migrationBuilder.DropCheckConstraint("ck_raw_export_attempt_key_reservation_sparse", "raw_export_attempt_key_reservations", "tagekyc");
        migrationBuilder.DropCheckConstraint("ck_raw_export_attempt_key_reservation_text", "raw_export_attempt_key_reservations", "tagekyc");
        migrationBuilder.DropCheckConstraint("ck_raw_export_attempt_key_reservation_values", "raw_export_attempt_key_reservations", "tagekyc");
        migrationBuilder.DropCheckConstraint("ck_raw_export_key_provider_operation_sparse", "raw_export_key_provider_operations", "tagekyc");
        migrationBuilder.DropCheckConstraint("ck_raw_export_key_provider_operation_values", "raw_export_key_provider_operations", "tagekyc");
    }

    private static void AddCurrentConstraints(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_event_values", "raw_export_attempt_key_preparation_events", EventValuesConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_reservation_sparse", "raw_export_attempt_key_reservations", ReservationSparseConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_reservation_text", "raw_export_attempt_key_reservations", ReservationTextConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_reservation_values", "raw_export_attempt_key_reservations", ReservationValuesConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_key_provider_operation_sparse", "raw_export_key_provider_operations", ProviderSparseConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_key_provider_operation_values", "raw_export_key_provider_operations", ProviderValuesConstraint, "tagekyc");
    }

    private static void AddLegacyConstraints(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_event_values", "raw_export_attempt_key_preparation_events", LegacyEventValuesConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_reservation_sparse", "raw_export_attempt_key_reservations", LegacyReservationSparseConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_reservation_text", "raw_export_attempt_key_reservations", LegacyReservationTextConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_attempt_key_reservation_values", "raw_export_attempt_key_reservations", LegacyReservationValuesConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_key_provider_operation_sparse", "raw_export_key_provider_operations", LegacyProviderSparseConstraint, "tagekyc");
        migrationBuilder.AddCheckConstraint("ck_raw_export_key_provider_operation_values", "raw_export_key_provider_operations", LegacyProviderValuesConstraint, "tagekyc");
    }
}
