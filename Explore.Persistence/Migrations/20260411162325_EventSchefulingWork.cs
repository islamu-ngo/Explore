using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventSchefulingWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_tags_tenant_id",
                table: "event_tags");

            migrationBuilder.DropIndex(
                name: "ix_event_session_speakers_tenant_id",
                table: "event_session_speakers");

            migrationBuilder.DropIndex(
                name: "ix_event_categories_tenant_id",
                table: "event_categories");

            migrationBuilder.AddColumn<int>(
                name: "registration_policy_id",
                table: "events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "event_registration_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_registration_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_session_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_categories_event_sessions_event_session_id",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_tags_event_sessions_event_session_id",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_tags_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_registration_policy_id",
                table: "events",
                column: "registration_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_tags_tenant_event_tag",
                table: "event_tags",
                columns: new[] { "tenant_id", "event_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_speakers_tenant_session_actor",
                table: "event_session_speakers",
                columns: new[] { "tenant_id", "event_session_id", "actor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_categories_tenant_event_category",
                table: "event_categories",
                columns: new[] { "tenant_id", "event_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_policies_master_code",
                table: "event_registration_policies",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_categories_category_id",
                table: "event_session_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_categories_event_session_id",
                table: "event_session_categories",
                column: "event_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_categories_tenant_session_category",
                table: "event_session_categories",
                columns: new[] { "tenant_id", "event_session_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_tags_event_session_id",
                table: "event_session_tags",
                column: "event_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_tags_tag_id",
                table: "event_session_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_tags_tenant_session_tag",
                table: "event_session_tags",
                columns: new[] { "tenant_id", "event_session_id", "tag_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_registration_policies_registration_policy_id",
                table: "events",
                column: "registration_policy_id",
                principalTable: "event_registration_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_event_registration_policies_registration_policy_id",
                table: "events");

            migrationBuilder.DropTable(
                name: "event_registration_policies");

            migrationBuilder.DropTable(
                name: "event_session_categories");

            migrationBuilder.DropTable(
                name: "event_session_tags");

            migrationBuilder.DropIndex(
                name: "ix_events_registration_policy_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_event_tags_tenant_event_tag",
                table: "event_tags");

            migrationBuilder.DropIndex(
                name: "ix_event_session_speakers_tenant_session_actor",
                table: "event_session_speakers");

            migrationBuilder.DropIndex(
                name: "ix_event_categories_tenant_event_category",
                table: "event_categories");

            migrationBuilder.DropColumn(
                name: "registration_policy_id",
                table: "events");

            migrationBuilder.CreateIndex(
                name: "ix_event_tags_tenant_id",
                table: "event_tags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_speakers_tenant_id",
                table: "event_session_speakers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_categories_tenant_id",
                table: "event_categories",
                column: "tenant_id");
        }
    }
}
