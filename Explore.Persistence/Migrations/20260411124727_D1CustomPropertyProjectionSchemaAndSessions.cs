using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class D1CustomPropertyProjectionSchemaAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_sessions_tenant_id",
                table: "event_sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_template_custom_property_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_template_custom_property_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "event_day_id",
                table: "event_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "local_end_date",
                table: "event_sessions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "local_end_minute_of_day",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "local_end_time",
                table: "event_sessions",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<DateOnly>(
                name: "local_start_date",
                table: "event_sessions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "local_start_minute_of_day",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "local_start_time",
                table: "event_sessions",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "room_id",
                table: "event_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_template_custom_property_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_template_custom_property_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_custom_property_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_custom_property_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_custom_property_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_custom_property_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_custom_property_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_custom_property_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "custom_property_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "custom_property_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "custom_property_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "custom_property_projection_dirty_scope",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    projection_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    projection_version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    drained_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_property_projection_dirty_scope", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_property_projection_dirty_scope_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custom_property_projection_status",
                columns: table => new
                {
                    projection_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    projection_version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_rebuild_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_rebuild_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rows_processed = table.Column<long>(type: "bigint", nullable: false),
                    rows_failed = table.Column<long>(type: "bigint", nullable: false),
                    last_checkpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_property_projection_status", x => new { x.projection_name, x.projection_version, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_custom_property_projection_status_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    banner_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    banner_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    allows_day_scope_registration = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_days_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_days_storage_objects_banner_image_id",
                        column: x => x.banner_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_days_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "location_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_rooms", x => x.id);
                    table.CheckConstraint("CK_LocationRoom_NonNegativeCapacity", "capacity IS NULL OR capacity >= 0");
                    table.ForeignKey(
                        name: "fk_location_rooms_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_location_rooms_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schedule_item_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_item_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_agenda_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    local_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    local_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    local_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    local_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    local_start_minute_of_day = table.Column<int>(type: "integer", nullable: false),
                    local_end_minute_of_day = table.Column<int>(type: "integer", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind_id = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_agenda_items", x => x.id);
                    table.CheckConstraint("CK_EventAgendaItem_EndAfterStart", "end_time > start_time");
                    table.ForeignKey(
                        name: "fk_event_agenda_items_event_days_event_day_id",
                        column: x => x.event_day_id,
                        principalTable: "event_days",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_location_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "location_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_schedule_item_kinds_kind_id",
                        column: x => x.kind_id,
                        principalTable: "schedule_item_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_event_day_id",
                table: "event_sessions",
                column: "event_day_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_room_id",
                table: "event_sessions",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_day_sort",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_day_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_event_local_start",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_id", "local_start_date", "local_start_minute_of_day" });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_room_time",
                table: "event_sessions",
                columns: new[] { "tenant_id", "room_id", "start_time", "end_time" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_EndAfterStart",
                table: "event_sessions",
                sql: "end_time > start_time");

            migrationBuilder.CreateIndex(
                name: "ix_custom_property_projection_dirty_scope_tenant_id",
                table: "custom_property_projection_dirty_scope",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_dirty_scope_pending",
                table: "custom_property_projection_dirty_scope",
                columns: new[] { "projection_name", "projection_version", "tenant_id" },
                filter: "drained_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_dirty_scope_unique",
                table: "custom_property_projection_dirty_scope",
                columns: new[] { "projection_name", "projection_version", "tenant_id", "scope_type", "scope_id", "definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_custom_property_projection_status_tenant_id",
                table: "custom_property_projection_status",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_event_day_id",
                table: "event_agenda_items",
                column: "event_day_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_event_id",
                table: "event_agenda_items",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_kind_id",
                table: "event_agenda_items",
                column: "kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_location_id",
                table: "event_agenda_items",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_room_id",
                table: "event_agenda_items",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_tenant_event_local_start",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "local_start_date", "local_start_minute_of_day" });

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_tenant_event_sort",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_days_banner_image_id",
                table: "event_days",
                column: "banner_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_days_event_id",
                table: "event_days",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_days_tenant_event_local_date",
                table: "event_days",
                columns: new[] { "tenant_id", "event_id", "local_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_days_tenant_event_published",
                table: "event_days",
                columns: new[] { "tenant_id", "event_id", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ix_event_days_tenant_event_sort",
                table: "event_days",
                columns: new[] { "tenant_id", "event_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_location_rooms_location_id",
                table: "location_rooms",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_location_rooms_tenant_location_name",
                table: "location_rooms",
                columns: new[] { "tenant_id", "location_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_location_rooms_tenant_location_sort",
                table: "location_rooms",
                columns: new[] { "tenant_id", "location_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_schedule_item_kinds_master_code",
                table: "schedule_item_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_event_sessions_event_days_event_day_id",
                table: "event_sessions",
                column: "event_day_id",
                principalTable: "event_days",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_event_sessions_location_rooms_room_id",
                table: "event_sessions",
                column: "room_id",
                principalTable: "location_rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_event_days_event_day_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_location_rooms_room_id",
                table: "event_sessions");

            migrationBuilder.DropTable(
                name: "custom_property_projection_dirty_scope");

            migrationBuilder.DropTable(
                name: "custom_property_projection_status");

            migrationBuilder.DropTable(
                name: "event_agenda_items");

            migrationBuilder.DropTable(
                name: "event_days");

            migrationBuilder.DropTable(
                name: "location_rooms");

            migrationBuilder.DropTable(
                name: "schedule_item_kinds");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_event_day_id",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_room_id",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_tenant_day_sort",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_tenant_event_local_start",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_tenant_room_time",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_EndAfterStart",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_templates");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_template_custom_property_options");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_template_custom_property_definitions");

            migrationBuilder.DropColumn(
                name: "event_day_id",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "local_end_date",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "local_end_minute_of_day",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "local_end_time",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "local_start_date",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "local_start_minute_of_day",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "local_start_time",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_templates");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_template_custom_property_options");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_template_custom_property_definitions");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_custom_property_values");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_custom_property_options");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_custom_property_definitions");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_custom_property_values");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_custom_property_options");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_custom_property_definitions");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "custom_property_values");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "custom_property_options");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "custom_property_definitions");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_id",
                table: "event_sessions",
                column: "tenant_id");
        }
    }
}
