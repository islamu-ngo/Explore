using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventModerationRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_moderation_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moderator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_kind = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    previous_status_id = table.Column<int>(type: "integer", nullable: false),
                    resulting_status_id = table.Column<int>(type: "integer", nullable: false),
                    is_irreversible = table.Column<bool>(type: "boolean", nullable: false),
                    source_moderation_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_moderation_records", x => x.id);
                    table.CheckConstraint("ck_event_moderation_records_correlation_not_blank", "correlation_id IS NULL OR length(btrim(correlation_id)) > 0");
                    table.CheckConstraint("ck_event_moderation_records_reason_code_not_blank", "length(btrim(reason_code)) > 0");
                    table.CheckConstraint("ck_event_moderation_records_status_transition", "previous_status_id <> resulting_status_id");
                    table.ForeignKey(
                        name: "fk_event_moderation_records_event_moderation_records_source_mo",
                        column: x => x.source_moderation_record_id,
                        principalTable: "event_moderation_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_moderation_records_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_moderation_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_source_moderation_record_id",
                table: "event_moderation_records",
                column: "source_moderation_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_action_created",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "action_kind", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_correlation",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "correlation_id" },
                unique: true,
                filter: "correlation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_event_created",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "event_id", "created_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_moderation_records");
        }
    }
}
