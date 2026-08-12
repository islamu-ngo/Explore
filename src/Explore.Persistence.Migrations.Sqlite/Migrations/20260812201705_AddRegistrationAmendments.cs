using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_registration_amendments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    change_kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    lineage_key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    before_participant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    before_assignment_status_id = table.Column<int>(type: "INTEGER", nullable: true),
                    after_participant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    after_assignment_status_id = table.Column<int>(type: "INTEGER", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_amendments", x => x.id);
                    table.UniqueConstraint("ak_registration_amendments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_ie_registration_amendments_ie_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_amendments_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_amendments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_amendments_tenant_id_event_id_registration_order_id_source_lineage_key_registration_order_line_id_ordinal",
                table: "ie_registration_amendments",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "source", "lineage_key", "registration_order_line_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_amendments_tenant_id_registration_order_id_source_lineage_key",
                table: "ie_registration_amendments",
                columns: new[] { "tenant_id", "registration_order_id", "source", "lineage_key" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_amendments_tenant_id_registration_order_line_id_ordinal",
                table: "ie_registration_amendments",
                columns: new[] { "tenant_id", "registration_order_line_id", "ordinal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_registration_amendments");
        }
    }
}
