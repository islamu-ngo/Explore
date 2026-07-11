// ABOUTME: Adds the missing event-scope uniqueness constraint for active registration intents.
// ABOUTME: Guards registration idempotency without deleting existing duplicate production data.

using Explore.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260704120000_AddEventRegistrationEventScopeUniqueIndex")]
public partial class AddEventRegistrationEventScopeUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM event_registration_intents
                    WHERE registration_scope_id = 1
                      AND is_deleted = false
                    GROUP BY tenant_id, event_id, user_id
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot create ix_event_registration_intents_unique_event_scope because duplicate active event-scope registration intents exist. Resolve duplicates before applying this migration.';
                END IF;
            END $$;
            """);

        migrationBuilder.CreateIndex(
            name: "ix_event_registration_intents_unique_event_scope",
            table: "event_registration_intents",
            columns: new[] { "tenant_id", "event_id", "user_id" },
            unique: true,
            filter: "registration_scope_id = 1 AND is_deleted = false");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_event_registration_intents_unique_event_scope",
            table: "event_registration_intents");
    }
}
