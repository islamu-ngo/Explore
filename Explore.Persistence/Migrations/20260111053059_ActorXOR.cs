using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

/// <inheritdoc />
public partial class ActorXOR : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_actors_actor_id",
            table: "users");

        migrationBuilder.DeleteData(
            table: "actors",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000020"));

        migrationBuilder.AlterColumn<Guid>(
            name: "actor_id",
            table: "users",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "organization_id",
            table: "actors",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "user_id",
            table: "actors",
            type: "uuid",
            nullable: true);

        migrationBuilder.UpdateData(
            table: "actors",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000021"),
            columns: new[] { "organization_id", "user_id" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000040"), null });

        migrationBuilder.InsertData(
            table: "actors",
            columns: new[] { "id", "actor_type_id", "description", "did", "did_custody_type_id", "display_name", "handle", "indexed_at", "organization_id", "pds_host", "profile_picture_cid", "profile_picture_id", "profile_picture_uri", "tenant_id", "user_id" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000022"), 1, "System user account", null, null, "System Account", "system", null, null, null, null, null, null, new Guid("018e4e5c-7f00-7000-8000-000000000001"), new Guid("018e4e5c-7f00-7000-8000-000000000030") });

        migrationBuilder.UpdateData(
            table: "storage_objects",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000050"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000022"));

        migrationBuilder.UpdateData(
            table: "storage_objects",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000051"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000022"));

        migrationBuilder.UpdateData(
            table: "storage_objects",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000052"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000022"));

        migrationBuilder.UpdateData(
            table: "users",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000030"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000022"));

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_actors_organization_id",
            table: "actors",
            column: "organization_id",
            unique: true,
            filter: "organization_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_actors_user_id",
            table: "actors",
            column: "user_id",
            unique: true,
            filter: "user_id IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Actor_UserOrOrganization",
            table: "actors",
            sql: "(user_id IS NOT NULL AND organization_id IS NULL) OR (user_id IS NULL AND organization_id IS NOT NULL)");

        migrationBuilder.AddForeignKey(
            name: "fk_actors_organizations_organization_id",
            table: "actors",
            column: "organization_id",
            principalTable: "organizations",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_actors_users_user_id",
            table: "actors",
            column: "user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_users_actors_actor_id",
            table: "users",
            column: "actor_id",
            principalTable: "actors",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_actors_organizations_organization_id",
            table: "actors");

        migrationBuilder.DropForeignKey(
            name: "fk_actors_users_user_id",
            table: "actors");

        migrationBuilder.DropForeignKey(
            name: "fk_users_actors_actor_id",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_email",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_actors_organization_id",
            table: "actors");

        migrationBuilder.DropIndex(
            name: "ix_actors_user_id",
            table: "actors");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Actor_UserOrOrganization",
            table: "actors");

        migrationBuilder.DeleteData(
            table: "actors",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000022"));

        migrationBuilder.DropColumn(
            name: "organization_id",
            table: "actors");

        migrationBuilder.DropColumn(
            name: "user_id",
            table: "actors");

        migrationBuilder.AlterColumn<Guid>(
            name: "actor_id",
            table: "users",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.InsertData(
            table: "actors",
            columns: new[] { "id", "actor_type_id", "description", "did", "did_custody_type_id", "display_name", "handle", "indexed_at", "pds_host", "profile_picture_cid", "profile_picture_id", "profile_picture_uri", "tenant_id" },
            values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000020"), 3, "System actor for automated operations", null, null, "System", "system", null, null, null, null, null, new Guid("018e4e5c-7f00-7000-8000-000000000001") });

        migrationBuilder.UpdateData(
            table: "storage_objects",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000050"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000020"));

        migrationBuilder.UpdateData(
            table: "storage_objects",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000051"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000020"));

        migrationBuilder.UpdateData(
            table: "storage_objects",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000052"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000020"));

        migrationBuilder.UpdateData(
            table: "users",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000030"),
            column: "actor_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000020"));

        migrationBuilder.AddForeignKey(
            name: "fk_users_actors_actor_id",
            table: "users",
            column: "actor_id",
            principalTable: "actors",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }
}
