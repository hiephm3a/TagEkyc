using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip80SAppendIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "append_idempotency_records",
                schema: "tagekyc",
                columns: table => new
                {
                    VerificationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EndpointKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubmissionSlot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MintedId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_append_idempotency_records", x => new { x.VerificationSessionId, x.IdempotencyKey });
                    table.ForeignKey(
                        name: "FK_append_idempotency_records_verification_sessions_Verificati~",
                        column: x => x.VerificationSessionId,
                        principalSchema: "tagekyc",
                        principalTable: "verification_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_append_idempotency_records_MintedId",
                schema: "tagekyc",
                table: "append_idempotency_records",
                column: "MintedId");

            migrationBuilder.CreateIndex(
                name: "IX_append_idempotency_records_VerificationSessionId_Idempotenc~",
                schema: "tagekyc",
                table: "append_idempotency_records",
                columns: new[] { "VerificationSessionId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "append_idempotency_records",
                schema: "tagekyc");
        }
    }
}
