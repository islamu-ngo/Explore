using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

/// <inheritdoc />
public partial class secretmanagementfix : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings",
            sql: "key NOT LIKE 'Database:%' AND key NOT LIKE 'Security:MasterKey%' AND key NOT LIKE 'ConnectionStrings:%'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings",
            sql: "\"Key\" NOT LIKE 'Database:%' AND \"Key\" NOT LIKE 'Security:MasterKey%' AND \"Key\" NOT LIKE 'ConnectionStrings:%'");
    }
}
