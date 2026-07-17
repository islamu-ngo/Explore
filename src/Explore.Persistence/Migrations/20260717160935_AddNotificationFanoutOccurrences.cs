using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationFanoutOccurrences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "fanout_occurrence_id",
                table: "notification_intents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_fanout_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    audience_cutoff_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aggregate_version = table.Column<Guid>(type: "uuid", nullable: false),
                    change_set_json = table.Column<string>(type: "jsonb", nullable: false),
                    safe_before_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    safe_after_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    template_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    template_version = table.Column<int>(type: "integer", nullable: false),
                    delivery_policy_id = table.Column<int>(type: "integer", nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    not_before = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coalescing_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    coalescing_window_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    superseded_by_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    suppression_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    superseded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_fanout_occurrences", x => x.id);
                    table.UniqueConstraint("ak_notification_fanout_occurrences_tenant_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_notification_fanout_occurrences_state", "state IN (1, 2)");
                    table.CheckConstraint("ck_notification_fanout_occurrences_supersession", "(state = 1 AND superseded_by_occurrence_id IS NULL AND suppression_reason IS NULL AND superseded_at IS NULL) OR (state = 2 AND superseded_by_occurrence_id IS NOT NULL AND suppression_reason IS NOT NULL AND superseded_at IS NOT NULL)");
                    table.CheckConstraint("ck_notification_fanout_occurrences_versions", "template_version > 0 AND policy_version > 0");
                    table.ForeignKey(
                        name: "fk_fanout_occurrences_session_tenant",
                        columns: x => new { x.tenant_id, x.session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fanout_occurrences_event_tenant",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fanout_occurrences_delivery_policy",
                        column: x => x.delivery_policy_id,
                        principalTable: "notification_delivery_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fanout_occurrences_superseded_tenant",
                        columns: x => new { x.tenant_id, x.superseded_by_occurrence_id },
                        principalTable: "notification_fanout_occurrences",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fanout_occurrences_tenant",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_notification_intents_tenant_occurrence_recipient",
                table: "notification_intents",
                columns: new[] { "tenant_id", "fanout_occurrence_id", "recipient_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_coalescing",
                table: "notification_fanout_occurrences",
                columns: new[] { "tenant_id", "coalescing_key", "state", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_delivery_policy_id",
                table: "notification_fanout_occurrences",
                column: "delivery_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_runnable",
                table: "notification_fanout_occurrences",
                columns: new[] { "tenant_id", "state", "not_before", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_source",
                table: "notification_fanout_occurrences",
                columns: new[] { "tenant_id", "source_type", "source_id", "aggregate_version" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_tenant_id_event_id",
                table: "notification_fanout_occurrences",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_tenant_id_session_id",
                table: "notification_fanout_occurrences",
                columns: new[] { "tenant_id", "session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_occurrences_tenant_id_superseded_by_occ",
                table: "notification_fanout_occurrences",
                columns: new[] { "tenant_id", "superseded_by_occurrence_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_notification_intents_fanout_occurrence_tenant",
                table: "notification_intents",
                columns: new[] { "tenant_id", "fanout_occurrence_id" },
                principalTable: "notification_fanout_occurrences",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notification_intents_fanout_occurrence_tenant",
                table: "notification_intents");

            migrationBuilder.DropTable(
                name: "notification_fanout_occurrences");

            migrationBuilder.DropIndex(
                name: "ux_notification_intents_tenant_occurrence_recipient",
                table: "notification_intents");

            migrationBuilder.DropColumn(
                name: "fanout_occurrence_id",
                table: "notification_intents");
        }
    }
}
