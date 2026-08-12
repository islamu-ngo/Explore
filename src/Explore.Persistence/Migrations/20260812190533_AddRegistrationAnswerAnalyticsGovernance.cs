using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAnswerAnalyticsGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_analytics_relevant",
                schema: "islamu_event",
                table: "registration_form_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_operationally_filterable",
                schema: "islamu_event",
                table: "registration_form_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_analytics_relevant",
                schema: "islamu_event",
                table: "registration_form_fields");

            migrationBuilder.DropColumn(
                name: "is_operationally_filterable",
                schema: "islamu_event",
                table: "registration_form_fields");
        }
    }
}
