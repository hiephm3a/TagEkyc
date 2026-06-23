using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TagEkycDbContext))]
    [Migration("20260623120000_Tip67BNeutralProofPublicKeyMaterial")]
    public partial class Tip67BNeutralProofPublicKeyMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKeyFingerprint",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicKeyJwk",
                schema: "tagekyc",
                table: "evidence_manifests",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicKeyFingerprint",
                schema: "tagekyc",
                table: "evidence_manifests");

            migrationBuilder.DropColumn(
                name: "PublicKeyJwk",
                schema: "tagekyc",
                table: "evidence_manifests");
        }
    }
}
