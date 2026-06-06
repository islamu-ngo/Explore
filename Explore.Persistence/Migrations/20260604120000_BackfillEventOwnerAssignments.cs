// ABOUTME: Idempotent data migration that backfills EventOwner assignments for pre-existing events.
// ABOUTME: Uses Event.CreatedBy as the initial owner; safe to re-run on already-migrated databases.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillEventOwnerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO event_role_assignments
                    (id, tenant_id, event_id, user_id, role_id, status,
                     starts_at_utc, expires_at_utc, revoked_at_utc, revoked_by_user_id,
                     version, created_at, created_by, updated_at, updated_by)
                SELECT
                    gen_random_uuid(),
                    e.tenant_id,
                    e.id,
                    e.created_by,
                    41,
                    2,
                    NOW() AT TIME ZONE 'UTC',
                    NULL,
                    NULL,
                    NULL,
                    1,
                    NOW() AT TIME ZONE 'UTC',
                    e.created_by,
                    NULL,
                    NULL
                FROM events e
                WHERE e.created_by IS NOT NULL
                  AND e.is_deleted = false
                  AND NOT EXISTS (
                      SELECT 1
                      FROM event_role_assignments era
                      WHERE era.tenant_id = e.tenant_id
                        AND era.event_id = e.id
                        AND era.user_id = e.created_by
                        AND era.role_id = 41
                        AND era.status IN (1, 2)
                  )
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
