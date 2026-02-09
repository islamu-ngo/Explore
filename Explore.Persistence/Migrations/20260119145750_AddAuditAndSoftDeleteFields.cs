using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAuditAndSoftDeleteFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "updated_by",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            table: "organizations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "organizations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            table: "organizations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "organizations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "updated_by",
            table: "organizations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "organization_members",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            table: "organization_members",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "organization_members",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            table: "organization_members",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "organization_members",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "organization_members",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "updated_by",
            table: "organization_members",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "events",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            table: "events",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "events",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            table: "events",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "events",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "events",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "updated_by",
            table: "events",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "event_sessions",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            table: "event_sessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "event_sessions",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            table: "event_sessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "event_sessions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "event_sessions",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "updated_by",
            table: "event_sessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "actors",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            table: "actors",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "actors",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            table: "actors",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "actors",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "actors",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "updated_by",
            table: "actors",
            type: "uuid",
            nullable: true);

        migrationBuilder.UpdateData(
            table: "actors",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000021"),
            columns: new[] { "created_at", "created_by", "deleted_at", "deleted_by", "is_deleted", "updated_at", "updated_by" },
            values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, false, null, null });

        migrationBuilder.UpdateData(
            table: "actors",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000022"),
            columns: new[] { "created_at", "created_by", "deleted_at", "deleted_by", "is_deleted", "updated_at", "updated_by" },
            values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, false, null, null });

        migrationBuilder.UpdateData(
            table: "events",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000060"),
            columns: new[] { "created_at", "created_by", "deleted_at", "deleted_by", "is_deleted", "updated_at", "updated_by" },
            values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, false, null, null });

        migrationBuilder.UpdateData(
            table: "organization_members",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000041"),
            columns: new[] { "created_at", "created_by", "deleted_at", "deleted_by", "is_deleted", "updated_at", "updated_by" },
            values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, false, null, null });

        migrationBuilder.UpdateData(
            table: "organizations",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000040"),
            columns: new[] { "created_by", "deleted_at", "deleted_by", "is_deleted", "updated_by" },
            values: new object[] { null, null, null, false, null });

        migrationBuilder.UpdateData(
            table: "users",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000030"),
            columns: new[] { "created_at", "created_by", "deleted_at", "deleted_by", "is_deleted", "updated_at", "updated_by" },
            values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, false, null, null });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_at",
            table: "users");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "users");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "users");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "users");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "users");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "users");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "users");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "organizations");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "organizations");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "organizations");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "organizations");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "organizations");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "events");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "events");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "events");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "events");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "events");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "events");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "events");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "event_sessions");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "created_by",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "updated_by",
            table: "actors");
    }
}
