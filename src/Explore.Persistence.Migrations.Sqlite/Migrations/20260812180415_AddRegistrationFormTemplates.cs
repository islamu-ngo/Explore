using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationFormTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_registration_form_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    pack_key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    source_event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    source_registration_form_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    source_registration_form_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_form_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_ie_registration_form_templates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_form_templates_source_registration_form_id_source_registration_form_version_id",
                table: "ie_registration_form_templates",
                columns: new[] { "source_registration_form_id", "source_registration_form_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_form_templates_tenant_id_category_name",
                table: "ie_registration_form_templates",
                columns: new[] { "tenant_id", "category", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_registration_form_templates");
        }
    }
}
