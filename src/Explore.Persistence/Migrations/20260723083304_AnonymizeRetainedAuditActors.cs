using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnonymizeRetainedAuditActors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_reviews_users_user_id",
                table: "organization_reviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "assigned_by_user_id",
                table: "tenant_plan_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "applied_by_user_id",
                table: "tenant_plan_application_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "transitioned_by_user_id",
                table: "tenant_lifecycle_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "invited_by_user_id",
                table: "tenant_invitations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "actor_user_id",
                table: "support_access_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "actor_user_id",
                table: "support_access_audit_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "organization_reviews",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "requester_user_id",
                table: "event_location_exact_read_audits",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "actor_user_id",
                table: "event_location_disclosure_audits",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "exported_by_user_id",
                table: "event_contact_share_exports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "configuration_change_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_reviews_users_user_id",
                table: "organization_reviews",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM tenant_plan_assignments WHERE assigned_by_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM tenant_plan_application_logs WHERE applied_by_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM tenant_lifecycle_logs WHERE transitioned_by_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM tenant_invitations WHERE invited_by_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM support_access_sessions WHERE actor_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM support_access_audit_events WHERE actor_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM organization_reviews WHERE user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM event_location_exact_read_audits WHERE requester_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM event_location_disclosure_audits WHERE actor_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM event_contact_share_exports WHERE exported_by_user_id IS NULL)
                        OR EXISTS (SELECT 1 FROM configuration_change_logs WHERE user_id IS NULL)
                    THEN
                        RAISE EXCEPTION 'Cannot downgrade after retained audit actor identities have been anonymized.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_organization_reviews_users_user_id",
                table: "organization_reviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "assigned_by_user_id",
                table: "tenant_plan_assignments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "applied_by_user_id",
                table: "tenant_plan_application_logs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "transitioned_by_user_id",
                table: "tenant_lifecycle_logs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "invited_by_user_id",
                table: "tenant_invitations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "actor_user_id",
                table: "support_access_sessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "actor_user_id",
                table: "support_access_audit_events",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "organization_reviews",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "requester_user_id",
                table: "event_location_exact_read_audits",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "actor_user_id",
                table: "event_location_disclosure_audits",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "exported_by_user_id",
                table: "event_contact_share_exports",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "configuration_change_logs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_reviews_users_user_id",
                table: "organization_reviews",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
