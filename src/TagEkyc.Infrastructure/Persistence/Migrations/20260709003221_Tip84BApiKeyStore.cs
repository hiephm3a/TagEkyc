using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip84BApiKeyStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_keys",
                schema: "tagekyc",
                columns: table => new
                {
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CredentialType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CredentialStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    KeyHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    ScopesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CallerCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AllowedClientApplicationIdsJson = table.Column<string>(type: "jsonb", nullable: true),
                    AllowedCaptureAgentIdsJson = table.Column<string>(type: "jsonb", nullable: true),
                    OAuthClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MtlsSubjectDn = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.ApiKeyId);
                    table.CheckConstraint("CK_api_keys_KeyHash_Length", "octet_length(\"KeyHash\") = 32");
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_KeyPrefix",
                schema: "tagekyc",
                table: "api_keys",
                column: "KeyPrefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys",
                schema: "tagekyc");
        }
    }
}
