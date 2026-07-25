using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpirePrivacyErasureProviderLocators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_lifecycle",
                table: "privacy_erasure_provider_work");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_lifecycle",
                table: "privacy_erasure_provider_work",
                sql: "(status = 5 AND protected_locator IS NULL) OR status = 6 OR (status NOT IN (5, 6) AND protected_locator IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_lifecycle",
                table: "privacy_erasure_provider_work");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_provider_work_locator_lifecycle",
                table: "privacy_erasure_provider_work",
                sql: "(status = 5 AND protected_locator IS NULL) OR (status <> 5 AND protected_locator IS NOT NULL)");
        }
    }
}
