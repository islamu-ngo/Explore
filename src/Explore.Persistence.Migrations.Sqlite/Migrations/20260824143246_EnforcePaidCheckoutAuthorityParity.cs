using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePaidCheckoutAuthorityParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_paid_order_acceptance_lines_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_paid_order_acceptance_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_attempts_amounts",
                table: "ie_payment_attempts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_attempts_amounts",
                table: "ie_payment_attempts",
                sql: "organizer_amount_minor >= 0 AND platform_fee_minor >= 0 AND platform_contribution_minor >= 0 AND total_minor >= 0 AND platform_fee_minor <= organizer_amount_minor AND total_minor = organizer_amount_minor + platform_contribution_minor");

            migrationBuilder.AddForeignKey(
                name: "fk_ie_paid_order_acceptance_lines_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" },
                principalTable: "ie_paid_order_acceptance_snapshots",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_paid_order_acceptance_lines_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_paid_order_acceptance_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_attempts_amounts",
                table: "ie_payment_attempts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_attempts_amounts",
                table: "ie_payment_attempts",
                sql: "organizer_amount_minor >= 0 AND platform_fee_minor >= 0 AND platform_contribution_minor >= 0 AND total_minor >= 0 AND platform_fee_minor <= organizer_amount_minor");

            migrationBuilder.AddForeignKey(
                name: "fk_ie_paid_order_acceptance_lines_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" },
                principalTable: "ie_paid_order_acceptance_snapshots",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
