using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    secret_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    owner_type = table.Column<int>(type: "integer", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_api_keys_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_key_id",
                table: "external_api_keys",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_tenant_id_owner_type_owner_id",
                table: "external_api_keys",
                columns: new[] { "tenant_id", "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_tenant_id_status",
                table: "external_api_keys",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_api_keys");
        }
    }
}
