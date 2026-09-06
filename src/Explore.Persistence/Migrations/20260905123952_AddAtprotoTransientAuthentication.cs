using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtprotoTransientAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atproto_transient_assertion_replays",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assertion_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    expires_at_unix_milliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_transient_assertion_replays", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "atproto_transient_records",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    token_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    protected_payload = table.Column<string>(type: "text", nullable: false),
                    expires_at_unix_milliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_transient_records", x => x.id);
                    table.CheckConstraint("ck_atproto_transients_tenant_purpose", "(purpose = 3 AND tenant_id IS NULL) OR (purpose IN (1, 2) AND tenant_id IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_assertion_replays_assertion_digest",
                schema: "islamu_event",
                table: "atproto_transient_assertion_replays",
                column: "assertion_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_assertion_replays_expires_at__17191c448719",
                schema: "islamu_event",
                table: "atproto_transient_assertion_replays",
                column: "expires_at_unix_milliseconds");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_records_expires_at_unix_milliseconds",
                schema: "islamu_event",
                table: "atproto_transient_records",
                column: "expires_at_unix_milliseconds");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_records_purpose_token_digest",
                schema: "islamu_event",
                table: "atproto_transient_records",
                columns: new[] { "purpose", "token_digest" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atproto_transient_assertion_replays",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "atproto_transient_records",
                schema: "islamu_event");
        }
    }
}
