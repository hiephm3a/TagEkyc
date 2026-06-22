using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip65EvidenceHashMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalizationScheme",
                schema: "tagekyc",
                table: "evidence_packages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "web-json-deterministic-v1");

            migrationBuilder.AddColumn<string>(
                name: "HashAlgorithm",
                schema: "tagekyc",
                table: "evidence_packages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "sha256");

            migrationBuilder.AddColumn<string>(
                name: "CanonicalizationScheme",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "web-json-deterministic-v1");

            migrationBuilder.AddColumn<string>(
                name: "HashAlgorithm",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "sha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanonicalizationScheme",
                schema: "tagekyc",
                table: "evidence_packages");

            migrationBuilder.DropColumn(
                name: "HashAlgorithm",
                schema: "tagekyc",
                table: "evidence_packages");

            migrationBuilder.DropColumn(
                name: "CanonicalizationScheme",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "HashAlgorithm",
                schema: "tagekyc",
                table: "evidence_manifests");
        }
    }
}
