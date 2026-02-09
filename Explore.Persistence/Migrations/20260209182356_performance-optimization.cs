using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class performanceoptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_tenant_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_organization_id",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_id",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_events_tenant_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_event_registrations_event_session_id",
                table: "event_registrations");

            migrationBuilder.RenameIndex(
                name: "ix_organization_members_user_id",
                table: "organization_members",
                newName: "ix_orgmembers_user");

            migrationBuilder.RenameIndex(
                name: "ix_event_registrations_user_id",
                table: "event_registrations",
                newName: "ix_eventregistrations_user");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant_active_status",
                table: "organizations",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant_name",
                table: "organizations",
                columns: new[] { "tenant_id", "full_name" });

            migrationBuilder.CreateIndex(
                name: "ix_orgmembers_org_user",
                table: "organization_members",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_city",
                table: "locations",
                columns: new[] { "tenant_id", "city" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_country",
                table: "locations",
                columns: new[] { "tenant_id", "country" });

            migrationBuilder.CreateIndex(
                name: "ix_events_tenant_active_status",
                table: "events",
                columns: new[] { "tenant_id", "is_deleted", "event_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_tenant_actor_created",
                table: "events",
                columns: new[] { "tenant_id", "actor_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_events_tenant_daterange",
                table: "events",
                columns: new[] { "tenant_id", "first_session_date", "last_session_date" });

            migrationBuilder.CreateIndex(
                name: "ix_events_tenant_eventtype",
                table: "events",
                columns: new[] { "tenant_id", "event_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_events_tenant_slug",
                table: "events",
                columns: new[] { "tenant_id", "slug" });

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations",
                columns: new[] { "event_session_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_tenant_active_status",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_organizations_tenant_name",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_orgmembers_org_user",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_city",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_country",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_events_tenant_active_status",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_tenant_actor_created",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_tenant_daterange",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_tenant_eventtype",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_tenant_slug",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations");

            migrationBuilder.RenameIndex(
                name: "ix_orgmembers_user",
                table: "organization_members",
                newName: "ix_organization_members_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_eventregistrations_user",
                table: "event_registrations",
                newName: "ix_event_registrations_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant_id",
                table: "organizations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id",
                table: "organization_members",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_id",
                table: "locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_tenant_id",
                table: "events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_event_session_id",
                table: "event_registrations",
                column: "event_session_id");
        }
    }
}
