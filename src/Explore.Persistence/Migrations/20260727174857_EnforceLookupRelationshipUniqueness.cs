using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceLookupRelationshipUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tag_type_tags_tenant_id",
                table: "tag_type_tags");

            migrationBuilder.DropIndex(
                name: "ix_category_type_categories_tenant_id",
                table: "category_type_categories");

            migrationBuilder.DropColumn(
                name: "external_registration_url",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_registration_required",
                table: "events");

            migrationBuilder.CreateTable(
                name: "advance_registration_obligations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_advance_registration_obligations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_access_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participation_handling_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participation_handling_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_participation_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participation_handling_mode_id = table.Column<int>(type: "integer", nullable: false),
                    advance_registration_obligation_id = table.Column<int>(type: "integer", nullable: false),
                    identity_access_mode_id = table.Column<int>(type: "integer", nullable: true),
                    guest_recovery_policy = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_participation_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_advance_registration_obl",
                        column: x => x.advance_registration_obligation_id,
                        principalTable: "advance_registration_obligations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_events_tenant_id_id",
                        columns: x => new { x.tenant_id, x.id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_identity_access_modes_id",
                        column: x => x.identity_access_mode_id,
                        principalTable: "identity_access_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_participation_handling_m",
                        column: x => x.participation_handling_mode_id,
                        principalTable: "participation_handling_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tenant_id_tag_id_tag_type_id",
                table: "tag_type_tags",
                columns: new[] { "tenant_id", "tag_id", "tag_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_tenant_id_category_id_category_typ",
                table: "category_type_categories",
                columns: new[] { "tenant_id", "category_id", "category_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_advance_registration_obligations_master_code",
                table: "advance_registration_obligations",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_advance_registration_obl",
                table: "event_participation_configurations",
                column: "advance_registration_obligation_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_identity_access_mode_id",
                table: "event_participation_configurations",
                column: "identity_access_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_participation_handling_m",
                table: "event_participation_configurations",
                column: "participation_handling_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_tenant_id_id",
                table: "event_participation_configurations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_modes_master_code",
                table: "identity_access_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participation_handling_modes_master_code",
                table: "participation_handling_modes",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_participation_configurations");

            migrationBuilder.DropTable(
                name: "advance_registration_obligations");

            migrationBuilder.DropTable(
                name: "identity_access_modes");

            migrationBuilder.DropTable(
                name: "participation_handling_modes");

            migrationBuilder.DropIndex(
                name: "ix_tag_type_tags_tenant_id_tag_id_tag_type_id",
                table: "tag_type_tags");

            migrationBuilder.DropIndex(
                name: "ix_category_type_categories_tenant_id_category_id_category_typ",
                table: "category_type_categories");

            migrationBuilder.AddColumn<string>(
                name: "external_registration_url",
                table: "events",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_registration_required",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tenant_id",
                table: "tag_type_tags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_tenant_id",
                table: "category_type_categories",
                column: "tenant_id");
        }
    }
}
