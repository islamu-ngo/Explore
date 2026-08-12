using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAnswerAnalyticsGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_analytics_relevant",
                table: "ie_registration_form_fields",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_operationally_filterable",
                table: "ie_registration_form_fields",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_analytics_relevant",
                table: "ie_registration_form_fields");

            migrationBuilder.DropColumn(
                name: "is_operationally_filterable",
                table: "ie_registration_form_fields");
        }
    }
}
