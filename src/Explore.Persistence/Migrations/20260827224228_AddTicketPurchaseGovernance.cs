using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPurchaseGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_purchase_authority_usages",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_mode = table.Column<int>(type: "integer", nullable: false),
                    enforcement_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    acting_account_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchaser_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supports_hard_cross_order_ceiling = table.Column<bool>(type: "boolean", nullable: false),
                    consumed_quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_purchase_authority_usages", x => x.id);
                    table.UniqueConstraint("ak_ticket_purchase_authority_usages_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.CheckConstraint("ck_ticket_purchase_authority_usages_mode", "access_mode IN (1, 2, 3)");
                    table.CheckConstraint("ck_ticket_purchase_authority_usages_quantity", "consumed_quantity >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ticket_purchase_policy_versions",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_ceiling = table.Column<int>(type: "integer", nullable: false),
                    tenant_ceiling = table.Column<int>(type: "integer", nullable: false),
                    event_ceiling = table.Column<int>(type: "integer", nullable: false),
                    effective_ceiling = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_purchase_policy_versions", x => x.id);
                    table.UniqueConstraint("ak_ticket_purchase_policy_versions_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.CheckConstraint("ck_ticket_purchase_policy_versions_ceilings", "instance_ceiling > 0 AND tenant_ceiling > 0 AND event_ceiling > 0 AND effective_ceiling > 0");
                    table.CheckConstraint("ck_ticket_purchase_policy_versions_effective", "effective_ceiling <= instance_ceiling AND effective_ceiling <= tenant_ceiling AND effective_ceiling <= event_ceiling");
                });

            migrationBuilder.CreateTable(
                name: "ticket_purchase_operations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authority_usage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    fingerprint_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    disposition = table.Column<int>(type: "integer", nullable: false),
                    requested_quantity = table.Column<int>(type: "integer", nullable: false),
                    effective_ceiling = table.Column<int>(type: "integer", nullable: false),
                    consumed_quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_purchase_operations", x => x.id);
                    table.CheckConstraint("ck_ticket_purchase_operations_disposition", "disposition IN (1, 3)");
                    table.CheckConstraint("ck_ticket_purchase_operations_quantities", "requested_quantity > 0 AND effective_ceiling > 0 AND consumed_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_ticket_purchase_operations_ticket_purchase_authority_usages",
                        columns: x => new { x.tenant_id, x.event_id, x.authority_usage_id },
                        principalSchema: "islamu_event",
                        principalTable: "ticket_purchase_authority_usages",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_purchase_operations_ticket_purchase_policy_versions_",
                        columns: x => new { x.tenant_id, x.event_id, x.policy_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "ticket_purchase_policy_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_authority_usages_tenant_id_event_id_enforce",
                schema: "islamu_event",
                table: "ticket_purchase_authority_usages",
                columns: new[] { "tenant_id", "event_id", "enforcement_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_operations_tenant_id_event_id_authority_usa",
                schema: "islamu_event",
                table: "ticket_purchase_operations",
                columns: new[] { "tenant_id", "event_id", "authority_usage_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_operations_tenant_id_event_id_order_id",
                schema: "islamu_event",
                table: "ticket_purchase_operations",
                columns: new[] { "tenant_id", "event_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_operations_tenant_id_event_id_policy_versio",
                schema: "islamu_event",
                table: "ticket_purchase_operations",
                columns: new[] { "tenant_id", "event_id", "policy_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_operations_tenant_id_key_hash",
                schema: "islamu_event",
                table: "ticket_purchase_operations",
                columns: new[] { "tenant_id", "key_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_policy_versions_tenant_id_event_id_id",
                schema: "islamu_event",
                table: "ticket_purchase_policy_versions",
                columns: new[] { "tenant_id", "event_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_purchase_policy_versions_tenant_id_event_id_instance",
                schema: "islamu_event",
                table: "ticket_purchase_policy_versions",
                columns: new[] { "tenant_id", "event_id", "instance_policy_version_id", "tenant_policy_version_id", "event_policy_version_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_purchase_operations",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "ticket_purchase_authority_usages",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "ticket_purchase_policy_versions",
                schema: "islamu_event");
        }
    }
}
