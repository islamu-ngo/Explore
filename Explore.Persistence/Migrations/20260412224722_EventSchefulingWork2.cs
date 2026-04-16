using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventSchefulingWork2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "event_registration_intent_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "registration_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_registration_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_scope_id = table.Column<int>(type: "integer", nullable: false),
                    selected_event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_policy_snapshot_id = table.Column<int>(type: "integer", nullable: true),
                    approval_status_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_event_registration_intents", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_approval_statuses_approval_statu",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_event_days_selected_event_day_id",
                        column: x => x.selected_event_day_id,
                        principalTable: "event_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_event_registration_policies_regi",
                        column: x => x.registration_policy_snapshot_id,
                        principalTable: "event_registration_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_registration_scopes_registration",
                        column: x => x.registration_scope_id,
                        principalTable: "registration_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_intent",
                table: "event_registrations",
                column: "event_registration_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_approval_status_id",
                table: "event_registration_intents",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_event_id",
                table: "event_registration_intents",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_registration_policy_snapshot_id",
                table: "event_registration_intents",
                column: "registration_policy_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_registration_scope_id",
                table: "event_registration_intents",
                column: "registration_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_selected_event_day_id",
                table: "event_registration_intents",
                column: "selected_event_day_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_event_day",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "selected_event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_event_user_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id", "registration_scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_user",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_day_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id", "selected_event_day_id" },
                unique: true,
                filter: "registration_scope_id = 2 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_session_selection_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id" },
                unique: true,
                filter: "registration_scope_id = 3 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_user_id",
                table: "event_registration_intents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_scopes_master_code",
                table: "registration_scopes",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_event_registrations_event_registration_intents_event_regist",
                table: "event_registrations",
                column: "event_registration_intent_id",
                principalTable: "event_registration_intents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_registrations_event_registration_intents_event_regist",
                table: "event_registrations");

            migrationBuilder.DropTable(
                name: "event_registration_intents");

            migrationBuilder.DropTable(
                name: "registration_scopes");

            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_intent",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "event_registration_intent_id",
                table: "event_registrations");
        }
    }
}
