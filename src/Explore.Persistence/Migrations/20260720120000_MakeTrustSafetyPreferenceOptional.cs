// ABOUTME: Makes the exposed trust-safety preference category user-controllable for optional delivery policies.
// ABOUTME: Keeps required moderation delivery authoritative through its policy instead of global category metadata.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260720120000_MakeTrustSafetyPreferenceOptional")]
public partial class MakeTrustSafetyPreferenceOptional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE notification_preference_categories
            SET is_required = FALSE
            WHERE id = 7
              AND master_code = 'trust-safety';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE notification_preference_categories
            SET is_required = TRUE
            WHERE id = 7
              AND master_code = 'trust-safety';
            """);
    }
}
