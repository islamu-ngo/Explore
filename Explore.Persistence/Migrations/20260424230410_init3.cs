using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class init3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_actors_recipient_actor_id",
                table: "event_contact_share_export");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_events_event_id",
                table: "event_contact_share_export");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_tenants_tenant_id",
                table: "event_contact_share_export");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_users_exported_by_user_id",
                table: "event_contact_share_export");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_item_event_contact_share_consent",
                table: "event_contact_share_export_item");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_item_event_contact_share_export_",
                table: "event_contact_share_export_item");

            migrationBuilder.DropPrimaryKey(
                name: "pk_event_contact_share_export_item",
                table: "event_contact_share_export_item");

            migrationBuilder.DropPrimaryKey(
                name: "pk_event_contact_share_export",
                table: "event_contact_share_export");

            migrationBuilder.RenameTable(
                name: "event_contact_share_export_item",
                newName: "event_contact_share_export_items");

            migrationBuilder.RenameTable(
                name: "event_contact_share_export",
                newName: "event_contact_share_exports");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_export_item_consent_id",
                table: "event_contact_share_export_items",
                newName: "ix_event_contact_share_export_items_consent_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_export_recipient_actor_id",
                table: "event_contact_share_exports",
                newName: "ix_event_contact_share_exports_recipient_actor_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_export_exported_by_user_id",
                table: "event_contact_share_exports",
                newName: "ix_event_contact_share_exports_exported_by_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_export_event_id",
                table: "event_contact_share_exports",
                newName: "ix_event_contact_share_exports_event_id");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "instantiated_from_template_at",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_synced_from_template_at",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_template_id",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_template_key",
                table: "events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_template_version",
                table: "events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "instantiated_from_template_at",
                table: "event_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_synced_from_template_at",
                table: "event_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_template_id",
                table: "event_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_template_key",
                table: "event_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_template_version",
                table: "event_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_event_contact_share_export_items",
                table: "event_contact_share_export_items",
                columns: new[] { "export_id", "consent_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_event_contact_share_exports",
                table: "event_contact_share_exports",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_items_event_contact_share_consen",
                table: "event_contact_share_export_items",
                column: "consent_id",
                principalTable: "event_contact_share_consents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_items_event_contact_share_export",
                table: "event_contact_share_export_items",
                column: "export_id",
                principalTable: "event_contact_share_exports",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_exports_actors_recipient_actor_id",
                table: "event_contact_share_exports",
                column: "recipient_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_exports_events_event_id",
                table: "event_contact_share_exports",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_exports_tenants_tenant_id",
                table: "event_contact_share_exports",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_exports_users_exported_by_user_id",
                table: "event_contact_share_exports",
                column: "exported_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_items_event_contact_share_consen",
                table: "event_contact_share_export_items");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_export_items_event_contact_share_export",
                table: "event_contact_share_export_items");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_exports_actors_recipient_actor_id",
                table: "event_contact_share_exports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_exports_events_event_id",
                table: "event_contact_share_exports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_exports_tenants_tenant_id",
                table: "event_contact_share_exports");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_exports_users_exported_by_user_id",
                table: "event_contact_share_exports");

            migrationBuilder.DropPrimaryKey(
                name: "pk_event_contact_share_exports",
                table: "event_contact_share_exports");

            migrationBuilder.DropPrimaryKey(
                name: "pk_event_contact_share_export_items",
                table: "event_contact_share_export_items");

            migrationBuilder.DropColumn(
                name: "instantiated_from_template_at",
                table: "events");

            migrationBuilder.DropColumn(
                name: "last_synced_from_template_at",
                table: "events");

            migrationBuilder.DropColumn(
                name: "source_template_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "source_template_key",
                table: "events");

            migrationBuilder.DropColumn(
                name: "source_template_version",
                table: "events");

            migrationBuilder.DropColumn(
                name: "instantiated_from_template_at",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "last_synced_from_template_at",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "source_template_id",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "source_template_key",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "source_template_version",
                table: "event_sessions");

            migrationBuilder.RenameTable(
                name: "event_contact_share_exports",
                newName: "event_contact_share_export");

            migrationBuilder.RenameTable(
                name: "event_contact_share_export_items",
                newName: "event_contact_share_export_item");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_exports_recipient_actor_id",
                table: "event_contact_share_export",
                newName: "ix_event_contact_share_export_recipient_actor_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_exports_exported_by_user_id",
                table: "event_contact_share_export",
                newName: "ix_event_contact_share_export_exported_by_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_exports_event_id",
                table: "event_contact_share_export",
                newName: "ix_event_contact_share_export_event_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_export_items_consent_id",
                table: "event_contact_share_export_item",
                newName: "ix_event_contact_share_export_item_consent_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_event_contact_share_export",
                table: "event_contact_share_export",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_event_contact_share_export_item",
                table: "event_contact_share_export_item",
                columns: new[] { "export_id", "consent_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_actors_recipient_actor_id",
                table: "event_contact_share_export",
                column: "recipient_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_events_event_id",
                table: "event_contact_share_export",
                column: "event_id",
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_tenants_tenant_id",
                table: "event_contact_share_export",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_users_exported_by_user_id",
                table: "event_contact_share_export",
                column: "exported_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_item_event_contact_share_consent",
                table: "event_contact_share_export_item",
                column: "consent_id",
                principalTable: "event_contact_share_consents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_export_item_event_contact_share_export_",
                table: "event_contact_share_export_item",
                column: "export_id",
                principalTable: "event_contact_share_export",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
