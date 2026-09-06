using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddAtprotoTransientAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_atproto_transient_assertion_replays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    assertion_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at_unix_milliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_atproto_transient_assertion_replays", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_atproto_transient_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    purpose = table.Column<int>(type: "int", nullable: false),
                    token_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    protected_payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at_unix_milliseconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_atproto_transient_records", x => x.id);
                    table.CheckConstraint("ck_atproto_transients_tenant_purpose", "(purpose = 3 AND tenant_id IS NULL) OR (purpose IN (1, 2) AND tenant_id IS NOT NULL)");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_assertion_replays_assertion_digest",
                table: "ie_atproto_transient_assertion_replays",
                column: "assertion_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_atproto_transient_assertion_replays_expires_at_un_b4a32cd9",
                table: "ie_atproto_transient_assertion_replays",
                column: "expires_at_unix_milliseconds");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_records_expires_at_unix_milliseconds",
                table: "ie_atproto_transient_records",
                column: "expires_at_unix_milliseconds");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_transient_records_purpose_token_digest",
                table: "ie_atproto_transient_records",
                columns: new[] { "purpose", "token_digest" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_atproto_transient_assertion_replays");

            migrationBuilder.DropTable(
                name: "ie_atproto_transient_records");
        }
    }
}
