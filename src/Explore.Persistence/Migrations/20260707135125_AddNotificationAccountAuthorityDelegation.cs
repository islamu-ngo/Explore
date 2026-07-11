using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationAccountAuthorityDelegation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "account_authority_kind_id",
                table: "notification_external_delegations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "account_authority_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_authority_kinds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_account_authority_kind_id",
                table: "notification_external_delegations",
                column: "account_authority_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_tenant_account_authority_status",
                table: "notification_external_delegations",
                columns: new[] { "tenant_id", "account_authority_kind_id", "status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_account_authority_kinds_master_code",
                table: "account_authority_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_external_delegations_account_authority_kinds_a",
                table: "notification_external_delegations",
                column: "account_authority_kind_id",
                principalTable: "account_authority_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notification_external_delegations_account_authority_kinds_a",
                table: "notification_external_delegations");

            migrationBuilder.DropTable(
                name: "account_authority_kinds");

            migrationBuilder.DropIndex(
                name: "ix_notification_external_delegations_account_authority_kind_id",
                table: "notification_external_delegations");

            migrationBuilder.DropIndex(
                name: "ix_notification_external_delegations_tenant_account_authority_status",
                table: "notification_external_delegations");

            migrationBuilder.DropColumn(
                name: "account_authority_kind_id",
                table: "notification_external_delegations");
        }
    }
}
