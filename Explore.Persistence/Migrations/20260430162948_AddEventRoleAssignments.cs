using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_role_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_role_assignments", x => x.id);
                    table.CheckConstraint("ck_event_role_assignments_validity_window", "expires_at_utc IS NULL OR expires_at_utc > starts_at_utc");
                    table.ForeignKey(
                        name: "fk_event_role_assignments_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_role_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_role_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_role_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_event_id",
                table: "event_role_assignments",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_role_id",
                table: "event_role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_tenant_event_role_status",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "event_id", "role_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_tenant_event_user_status",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "event_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_tenant_user_event_status",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "user_id", "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_unique_pending_active",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "event_id", "user_id", "role_id" },
                unique: true,
                filter: "status IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_user_id",
                table: "event_role_assignments",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_role_assignments");
        }
    }
}
