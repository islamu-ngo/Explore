using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_attempts_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_r",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra1",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.DropIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_id",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "registration_form_version_id", "id" });

            migrationBuilder.CreateTable(
                name: "registration_amendments",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    change_kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    lineage_key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    before_participant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    before_assignment_status_id = table.Column<int>(type: "int", nullable: true),
                    after_participant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    after_assignment_status_id = table.Column<int>(type: "int", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_amendments", x => x.id);
                    table.UniqueConstraint("ak_registration_amendments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_registration_amendments_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_amendments_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_amendments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_supersed",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "superseded_by_registration_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_amendments_tenant_id_event_id_registration_order_id_source_lineage_key_registration_order_line_id_ordinal",
                schema: "islamu_event",
                table: "registration_amendments",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "source", "lineage_key", "registration_order_line_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_amendments_tenant_id_registration_order_id_source_lineage_key",
                schema: "islamu_event",
                table: "registration_amendments",
                columns: new[] { "tenant_id", "registration_order_id", "source", "lineage_key" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_amendments_tenant_id_registration_order_line_id_ordinal",
                schema: "islamu_event",
                table: "registration_amendments",
                columns: new[] { "tenant_id", "registration_order_line_id", "ordinal" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_attempts_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_r",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "superseded_by_registration_attempt_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_attempts",
                principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_attempts_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_r",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.DropTable(
                name: "registration_amendments",
                schema: "islamu_event");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_id",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.DropIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_supersed",
                schema: "islamu_event",
                table: "registration_attempts");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra1",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "registration_form_version_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_requirement_id_registra",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "superseded_by_registration_attempt_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_attempts_registration_attempts_tenant_id_event_id_registration_order_id_registration_workflow_id_registration_r",
                schema: "islamu_event",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "superseded_by_registration_attempt_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_attempts",
                principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
