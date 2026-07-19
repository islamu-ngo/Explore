// ABOUTME: Adds the cross-tenant due-occurrence index used by fair notification fanout rounds.
// ABOUTME: Keeps pending work globally searchable while preserving the existing tenant-local index.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260719183139_AddNotificationFanoutGlobalRunnableIndex")]
public partial class AddNotificationFanoutGlobalRunnableIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_notification_fanout_occurrences_global_runnable",
            table: "notification_fanout_occurrences",
            columns: ["not_before", "tenant_id", "priority", "occurred_at", "id"],
            descending: [false, false, true, false, false],
            filter: "state = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_notification_fanout_occurrences_global_runnable",
            table: "notification_fanout_occurrences");
    }
}
