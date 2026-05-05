using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_session_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_groups_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_groups_location_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "location_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_session_groups_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_session_groups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_group_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_session_group_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_event_session_groups_event_ses",
                        column: x => x.event_session_group_id,
                        principalTable: "event_session_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_event_sessions_event_session_id",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_event_id",
                table: "event_session_group_sessions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_event_session_group_id",
                table: "event_session_group_sessions",
                column: "event_session_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_event_session_id",
                table: "event_session_group_sessions",
                column: "event_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_tenant_event_group_session",
                table: "event_session_group_sessions",
                columns: new[] { "tenant_id", "event_id", "event_session_group_id", "event_session_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_tenant_event_session_primary",
                table: "event_session_group_sessions",
                columns: new[] { "tenant_id", "event_id", "event_session_id", "is_primary" },
                unique: true,
                filter: "is_primary = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_tenant_group_sort",
                table: "event_session_group_sessions",
                columns: new[] { "tenant_id", "event_session_group_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_event_id",
                table: "event_session_groups",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_location_id",
                table: "event_session_groups",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_room_id",
                table: "event_session_groups",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_tenant_event_slug",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "event_id", "slug" },
                unique: true,
                filter: "is_deleted = false AND slug IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_tenant_event_sort",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "event_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_session_group_sessions");

            migrationBuilder.DropTable(
                name: "event_session_groups");
        }
    }
}
