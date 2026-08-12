using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationFormTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registration_form_templates",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pack_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_registration_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_registration_form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_form_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_registration_form_templates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_templates_source_registration_form_id_sou",
                schema: "islamu_event",
                table: "registration_form_templates",
                columns: new[] { "source_registration_form_id", "source_registration_form_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_templates_tenant_id_category_name",
                schema: "islamu_event",
                table: "registration_form_templates",
                columns: new[] { "tenant_id", "category", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_form_templates",
                schema: "islamu_event");
        }
    }
}
