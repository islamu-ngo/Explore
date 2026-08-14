using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerPaymentProviderAccountOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizer_payment_provider_account_operations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizer_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    connect_platform_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    provider_idempotency_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    active_scope_key = table.Column<string>(type: "character varying(232)", maxLength: 232, nullable: false),
                    active_uniqueness_slot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    external_account_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    provider_request_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    resolution_reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    manual_reconciliation_required_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bound_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    no_provider_account_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizer_payment_provider_account_operations", x => x.id);
                    table.UniqueConstraint("ak_organizer_payment_provider_account_operations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_organizer_payment_provider_account_operations_status", "status_id BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_organizer_payment_account_operations_connection",
                        columns: x => new { x.tenant_id, x.connection_id },
                        principalSchema: "islamu_event",
                        principalTable: "organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizer_payment_provider_account_operations_actors_organi",
                        column: x => x.organizer_actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizer_payment_provider_account_operations_tenants_tenan",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_account_operations_active_scope_",
                schema: "islamu_event",
                table: "organizer_payment_provider_account_operations",
                columns: new[] { "active_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_account_operations_organizer_act",
                schema: "islamu_event",
                table: "organizer_payment_provider_account_operations",
                column: "organizer_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_account_operations_provider_idem",
                schema: "islamu_event",
                table: "organizer_payment_provider_account_operations",
                column: "provider_idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_account_operations_tenant_id_con",
                schema: "islamu_event",
                table: "organizer_payment_provider_account_operations",
                columns: new[] { "tenant_id", "connection_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_account_operations_tenant_id_org",
                schema: "islamu_event",
                table: "organizer_payment_provider_account_operations",
                columns: new[] { "tenant_id", "organizer_actor_id", "provider_code", "connect_platform_id", "status_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizer_payment_provider_account_operations",
                schema: "islamu_event");
        }
    }
}
