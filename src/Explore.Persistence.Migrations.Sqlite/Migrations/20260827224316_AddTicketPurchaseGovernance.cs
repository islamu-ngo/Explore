using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPurchaseGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_ticket_purchase_authority_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    access_mode = table.Column<int>(type: "INTEGER", nullable: false),
                    enforcement_key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    acting_account_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    purchaser_actor_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    order_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    supports_hard_cross_order_ceiling = table.Column<bool>(type: "INTEGER", nullable: false),
                    consumed_quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticket_purchase_authority_usages", x => x.id);
                    table.UniqueConstraint("ak_ticket_purchase_authority_usages_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.CheckConstraint("ck_ticket_purchase_authority_usages_mode", "access_mode IN (1, 2, 3)");
                    table.CheckConstraint("ck_ticket_purchase_authority_usages_quantity", "consumed_quantity >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ie_ticket_purchase_policy_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    instance_policy_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_policy_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_policy_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    instance_ceiling = table.Column<int>(type: "INTEGER", nullable: false),
                    tenant_ceiling = table.Column<int>(type: "INTEGER", nullable: false),
                    event_ceiling = table.Column<int>(type: "INTEGER", nullable: false),
                    effective_ceiling = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticket_purchase_policy_versions", x => x.id);
                    table.UniqueConstraint("ak_ticket_purchase_policy_versions_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.CheckConstraint("ck_ticket_purchase_policy_versions_ceilings", "instance_ceiling > 0 AND tenant_ceiling > 0 AND event_ceiling > 0 AND effective_ceiling > 0");
                    table.CheckConstraint("ck_ticket_purchase_policy_versions_effective", "effective_ceiling <= instance_ceiling AND effective_ceiling <= tenant_ceiling AND effective_ceiling <= event_ceiling");
                });

            migrationBuilder.CreateTable(
                name: "ie_ticket_purchase_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    policy_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    authority_usage_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    key_hash = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    fingerprint_hash = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    disposition = table.Column<int>(type: "INTEGER", nullable: false),
                    requested_quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    effective_ceiling = table.Column<int>(type: "INTEGER", nullable: false),
                    consumed_quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticket_purchase_operations", x => x.id);
                    table.CheckConstraint("ck_ticket_purchase_operations_disposition", "disposition IN (1, 3)");
                    table.CheckConstraint("ck_ticket_purchase_operations_quantities", "requested_quantity > 0 AND effective_ceiling > 0 AND consumed_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_ie_ticket_purchase_operations_ie_ticket_purchase_authority_usages_tenant_id_event_id_authority_usage_id",
                        columns: x => new { x.tenant_id, x.event_id, x.authority_usage_id },
                        principalTable: "ie_ticket_purchase_authority_usages",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_ticket_purchase_operations_ticket_purchase_policy_versions_tenant_id_event_id_policy_version_id",
                        columns: x => new { x.tenant_id, x.event_id, x.policy_version_id },
                        principalTable: "ie_ticket_purchase_policy_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_authority_usages_tenant_id_event_id_enforcement_key",
                table: "ie_ticket_purchase_authority_usages",
                columns: new[] { "tenant_id", "event_id", "enforcement_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_operations_tenant_id_event_id_authority_usage_id",
                table: "ie_ticket_purchase_operations",
                columns: new[] { "tenant_id", "event_id", "authority_usage_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_operations_tenant_id_event_id_order_id",
                table: "ie_ticket_purchase_operations",
                columns: new[] { "tenant_id", "event_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_operations_tenant_id_event_id_policy_version_id",
                table: "ie_ticket_purchase_operations",
                columns: new[] { "tenant_id", "event_id", "policy_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_operations_tenant_id_key_hash",
                table: "ie_ticket_purchase_operations",
                columns: new[] { "tenant_id", "key_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_policy_versions_tenant_id_event_id_id",
                table: "ie_ticket_purchase_policy_versions",
                columns: new[] { "tenant_id", "event_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_purchase_policy_versions_tenant_id_event_id_instance_policy_version_id_tenant_policy_version_id_event_policy_version_id",
                table: "ie_ticket_purchase_policy_versions",
                columns: new[] { "tenant_id", "event_id", "instance_policy_version_id", "tenant_policy_version_id", "event_policy_version_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_ticket_purchase_operations");

            migrationBuilder.DropTable(
                name: "ie_ticket_purchase_authority_usages");

            migrationBuilder.DropTable(
                name: "ie_ticket_purchase_policy_versions");
        }
    }
}
