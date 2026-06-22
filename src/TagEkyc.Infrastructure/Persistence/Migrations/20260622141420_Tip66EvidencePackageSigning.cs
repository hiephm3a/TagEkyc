using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip66EvidencePackageSigning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeyId",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureAlgorithm",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureFormat",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureScheme",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureValue",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedAt",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeyId",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "SignatureAlgorithm",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "SignatureFormat",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "SignatureScheme",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "SignatureValue",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                schema: "tagekyc",
                table: "evidence_manifests");
        }
    }
}
