using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerPaymentProviderAccountOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_organizer_payment_provider_account_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    organizer_actor_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    connect_platform_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_idempotency_key = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    active_scope_key = table.Column<string>(type: "varchar(232)", maxLength: 232, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_uniqueness_slot = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    external_account_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    connection_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    failure_code = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_request_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolution_reason = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    manual_reconciliation_required_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    bound_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    no_provider_account_confirmed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    provider_rejected_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_organizer_payment_provider_account_operations", x => x.id);
                    table.UniqueConstraint("ak_organizer_payment_provider_account_operations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_organizer_payment_provider_account_operations_status", "status_id BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_account_operations_ie__4164CD46",
                        columns: x => new { x.tenant_id, x.connection_id },
                        principalTable: "ie_organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_account_operations_ie__C0111C4C",
                        column: x => x.organizer_actor_id,
                        principalTable: "ie_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_account_operations_ie__FE88438F",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_account_operations_act_DBE94CEF",
                table: "ie_organizer_payment_provider_account_operations",
                columns: new[] { "active_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_account_operations_org_431FA825",
                table: "ie_organizer_payment_provider_account_operations",
                column: "organizer_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_account_operations_pro_17248AA8",
                table: "ie_organizer_payment_provider_account_operations",
                column: "provider_idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_account_operations_ten_9A43E3F1",
                table: "ie_organizer_payment_provider_account_operations",
                columns: new[] { "tenant_id", "connection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_account_operations_ten_BC1E553B",
                table: "ie_organizer_payment_provider_account_operations",
                columns: new[] { "tenant_id", "organizer_actor_id", "provider_code", "connect_platform_id", "status_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_organizer_payment_provider_account_operations");
        }
    }
}
