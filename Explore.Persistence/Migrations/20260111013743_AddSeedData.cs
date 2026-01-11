using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_actors_actor_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_atproto_records_atproto_record_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_audience_ages_audience_age_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_audience_genders_audience_gender_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_formats_event_format_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_statuses_event_status_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_types_event_type_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_madhabs_madhab_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_storage_objects_featured_image_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_tenants_tenant_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_visibility_types_visibility_type_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_actors_actor_id",
                table: "storage_objects");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_file_types_file_type_id",
                table: "storage_objects");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_tenants_tenant_id",
                table: "storage_objects");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "tenant_settings",
                type: "uuid",
                nullable: false,
                defaultValueSql: "uuidv7()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "uri",
                table: "storage_objects",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "storage_objects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "extension",
                table: "storage_objects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id1",
                table: "organization_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "external_registration_url",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_url",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "events",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "currency_code",
                table: "events",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "organization_members",
                columns: new[] { "id", "organization_id", "organization_id1", "organization_position_id", "organization_role_id", "user_id" },
                values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000041"), new Guid("018e4e5c-7f00-7000-8000-000000000040"), null, 1, 1, new Guid("018e4e5c-7f00-7000-8000-000000000030") });

            migrationBuilder.InsertData(
                table: "storage_objects",
                columns: new[] { "id", "actor_id", "extension", "file_type_id", "full_name", "size", "tenant_id", "uri" },
                values: new object[,]
                {
                    { new Guid("018e4e5c-7f00-7000-8000-000000000050"), new Guid("018e4e5c-7f00-7000-8000-000000000020"), ".jpg", 1, "Default Event Image", 0L, new Guid("018e4e5c-7f00-7000-8000-000000000001"), "https://placeholder.islamu.org/event-default.jpg" },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000051"), new Guid("018e4e5c-7f00-7000-8000-000000000020"), ".jpg", 1, "Default Profile Image", 0L, new Guid("018e4e5c-7f00-7000-8000-000000000001"), "https://placeholder.islamu.org/profile-default.jpg" },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000052"), new Guid("018e4e5c-7f00-7000-8000-000000000020"), ".jpg", 1, "Default Organization Logo", 0L, new Guid("018e4e5c-7f00-7000-8000-000000000001"), "https://placeholder.islamu.org/org-default.jpg" }
                });

            migrationBuilder.InsertData(
                table: "tenant_settings",
                columns: new[] { "id", "tenant_id" },
                values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000400"), new Guid("018e4e5c-7f00-7000-8000-000000000001") });

            migrationBuilder.InsertData(
                table: "events",
                columns: new[] { "id", "actor_id", "atproto_record_id", "audience_age_id", "audience_gender_id", "currency_code", "description", "event_format_id", "event_status_id", "event_type_id", "event_url", "external_registration_url", "featured_image_id", "first_session_date", "is_registration_required", "last_session_date", "madhab_id", "price", "session_count", "slug", "tenant_id", "timezone", "title", "visibility_type_id" },
                values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000060"), new Guid("018e4e5c-7f00-7000-8000-000000000021"), null, 1, 3, "EUR", "This is a sample event to demonstrate the ISLAMU Events platform. Feel free to explore and create your own events!", 2, 2, 2, null, null, new Guid("018e4e5c-7f00-7000-8000-000000000050"), null, false, null, null, 0m, null, "welcome-to-islamu-events", new Guid("018e4e5c-7f00-7000-8000-000000000001"), "Europe/Brussels", "Welcome to ISLAMU Events", 1 });

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members",
                column: "organization_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_events_actors_actor_id",
                table: "events",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_atproto_records_atproto_record_id",
                table: "events",
                column: "atproto_record_id",
                principalTable: "atproto_records",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_events_audience_ages_audience_age_id",
                table: "events",
                column: "audience_age_id",
                principalTable: "audience_ages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_audience_genders_audience_gender_id",
                table: "events",
                column: "audience_gender_id",
                principalTable: "audience_genders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_formats_event_format_id",
                table: "events",
                column: "event_format_id",
                principalTable: "event_formats",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_statuses_event_status_id",
                table: "events",
                column: "event_status_id",
                principalTable: "event_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_types_event_type_id",
                table: "events",
                column: "event_type_id",
                principalTable: "event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_madhabs_madhab_id",
                table: "events",
                column: "madhab_id",
                principalTable: "madhabs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_storage_objects_featured_image_id",
                table: "events",
                column: "featured_image_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_tenants_tenant_id",
                table: "events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_events_visibility_types_visibility_type_id",
                table: "events",
                column: "visibility_type_id",
                principalTable: "visibility_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organizations_organization_id1",
                table: "organization_members",
                column: "organization_id1",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_storage_objects_actors_actor_id",
                table: "storage_objects",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_storage_objects_file_types_file_type_id",
                table: "storage_objects",
                column: "file_type_id",
                principalTable: "file_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_storage_objects_tenants_tenant_id",
                table: "storage_objects",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_actors_actor_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_atproto_records_atproto_record_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_audience_ages_audience_age_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_audience_genders_audience_gender_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_formats_event_format_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_statuses_event_status_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_event_types_event_type_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_madhabs_madhab_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_storage_objects_featured_image_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_tenants_tenant_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_visibility_types_visibility_type_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organizations_organization_id1",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_actors_actor_id",
                table: "storage_objects");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_file_types_file_type_id",
                table: "storage_objects");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_tenants_tenant_id",
                table: "storage_objects");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members");

            migrationBuilder.DeleteData(
                table: "events",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000060"));

            migrationBuilder.DeleteData(
                table: "organization_members",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000041"));

            migrationBuilder.DeleteData(
                table: "storage_objects",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000051"));

            migrationBuilder.DeleteData(
                table: "storage_objects",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000052"));

            migrationBuilder.DeleteData(
                table: "tenant_settings",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000400"));

            migrationBuilder.DeleteData(
                table: "storage_objects",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000050"));

            migrationBuilder.DropColumn(
                name: "organization_id1",
                table: "organization_members");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "tenant_settings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "uuidv7()");

            migrationBuilder.AlterColumn<string>(
                name: "uri",
                table: "storage_objects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "storage_objects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "extension",
                table: "storage_objects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                table: "events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "external_registration_url",
                table: "events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_url",
                table: "events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "currency_code",
                table: "events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_events_actors_actor_id",
                table: "events",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_atproto_records_atproto_record_id",
                table: "events",
                column: "atproto_record_id",
                principalTable: "atproto_records",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_events_audience_ages_audience_age_id",
                table: "events",
                column: "audience_age_id",
                principalTable: "audience_ages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_audience_genders_audience_gender_id",
                table: "events",
                column: "audience_gender_id",
                principalTable: "audience_genders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_formats_event_format_id",
                table: "events",
                column: "event_format_id",
                principalTable: "event_formats",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_statuses_event_status_id",
                table: "events",
                column: "event_status_id",
                principalTable: "event_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_event_types_event_type_id",
                table: "events",
                column: "event_type_id",
                principalTable: "event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_madhabs_madhab_id",
                table: "events",
                column: "madhab_id",
                principalTable: "madhabs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_events_storage_objects_featured_image_id",
                table: "events",
                column: "featured_image_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_tenants_tenant_id",
                table: "events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_events_visibility_types_visibility_type_id",
                table: "events",
                column: "visibility_type_id",
                principalTable: "visibility_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_storage_objects_actors_actor_id",
                table: "storage_objects",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_storage_objects_file_types_file_type_id",
                table: "storage_objects",
                column: "file_type_id",
                principalTable: "file_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_storage_objects_tenants_tenant_id",
                table: "storage_objects",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
