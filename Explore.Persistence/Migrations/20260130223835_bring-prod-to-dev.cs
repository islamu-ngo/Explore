using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class bringprodtodev : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "event_islamic_aspects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    madhab_id = table.Column<int>(type: "integer", nullable: true),
                    reference_prayer = table.Column<int>(type: "integer", nullable: true),
                    prayer_time_offset = table.Column<int>(type: "integer", nullable: true),
                    gender_mode = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    includes_quran_recitation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    primary_language_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_islamic_aspects", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_islamic_aspects_events_id",
                        column: x => x.id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_islamic_aspects_languages_primary_language_id",
                        column: x => x.primary_language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_islamic_aspects_madhabs_madhab_id",
                        column: x => x.madhab_id,
                        principalTable: "madhabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_tech_aspects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_repo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hackathon_track = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    skill_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tech_stack_tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    requires_laptop = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_coding_competition = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_team_size = table.Column<int>(type: "integer", nullable: true),
                    prize_pool = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    prize_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_tech_aspects", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_tech_aspects_events_id",
                        column: x => x.id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleDefinitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    wizard_schema_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_module_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pds_sync_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    collection = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    record_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    operation = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pds_sync_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    allowed_values = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_setting_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_setting_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_setting_overrides_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantCapabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_capabilities_module_definitions_module_id",
                        column: x => x.module_id,
                        principalTable: "ModuleDefinitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ModuleDefinitions",
                columns: new[] { "id", "category", "created_at", "description", "display_order", "icon_name", "is_active", "key", "name", "updated_at", "wizard_schema_url" },
                values: new object[,]
                {
                    { new Guid("018e4e5c-7f00-7000-8000-000000000600"), "Core", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Basic event functionality - title, description, sessions, locations", 0, "Event", true, "Mod_Core", "Core Events", null, null },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000601"), "Domain", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Islamic-specific features: Madhab selection, prayer time scheduling, gender segregation", 1, "Mosque", true, "Mod_Islamic", "Islamic Events", null, null },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000602"), "Domain", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Developer event features: GitHub repositories, skill levels, live coding sessions", 2, "Code", true, "Mod_Tech", "Tech Events", null, null }
                });

            migrationBuilder.UpdateData(
                table: "events",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000060"),
                column: "metadata_json",
                value: null);

            migrationBuilder.InsertData(
                table: "system_settings",
                columns: new[] { "id", "allowed_values", "category", "created_at", "description", "display_order", "is_locked", "key", "updated_at", "value", "value_type" },
                values: new object[] { new Guid("018e4e5c-7f00-7000-8000-000000000500"), "[\"SingleTenant\", \"MultiTenant\"]", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deployment mode of the application", 1, true, "deployment.mode", null, "\"MultiTenant\"", 0 });

            migrationBuilder.InsertData(
                table: "system_settings",
                columns: new[] { "id", "allowed_values", "category", "created_at", "description", "display_order", "key", "updated_at", "value", "value_type" },
                values: new object[,]
                {
                    { new Guid("018e4e5c-7f00-7000-8000-000000000501"), null, "Events", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Maximum number of sessions allowed per event", 1, "events.max_sessions_per_event", null, "100", 1 },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000502"), null, "Events", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Whether events require admin approval before publishing", 2, "events.require_approval", null, "false", 2 },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000503"), null, "Modules", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Enable Islamic event module", 1, "modules.islamic_enabled", null, "true", 2 },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000504"), null, "Modules", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Enable Tech event module", 2, "modules.tech_enabled", null, "true", 2 }
                });

            migrationBuilder.InsertData(
                table: "TenantCapabilities",
                columns: new[] { "id", "configuration_json", "enabled_at", "enabled_by", "is_enabled", "module_id", "tenant_id" },
                values: new object[,]
                {
                    { new Guid("018e4e5c-7f00-7000-8000-000000000610"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new Guid("018e4e5c-7f00-7000-8000-000000000600"), new Guid("018e4e5c-7f00-7000-8000-000000000001") },
                    { new Guid("018e4e5c-7f00-7000-8000-000000000611"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new Guid("018e4e5c-7f00-7000-8000-000000000601"), new Guid("018e4e5c-7f00-7000-8000-000000000001") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_islamic_aspects_madhab_id",
                table: "event_islamic_aspects",
                column: "madhab_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_islamic_aspects_primary_language_id",
                table: "event_islamic_aspects",
                column: "primary_language_id");

            migrationBuilder.CreateIndex(
                name: "ix_module_definitions_display_order",
                table: "ModuleDefinitions",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "ix_module_definitions_key",
                table: "ModuleDefinitions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_Did",
                table: "pds_sync_outbox",
                column: "did");

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_SourceEntity",
                table: "pds_sync_outbox",
                columns: new[] { "source_entity_type", "source_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_Unique",
                table: "pds_sync_outbox",
                columns: new[] { "did", "collection", "record_key", "operation", "created_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PdsSyncOutbox_WorkerPoll",
                table: "pds_sync_outbox",
                columns: new[] { "status", "next_retry_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_setting_overrides_tenant_id_key",
                table: "tenant_setting_overrides",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_module_id",
                table: "TenantCapabilities",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_tenant_id_module_id",
                table: "TenantCapabilities",
                columns: new[] { "tenant_id", "module_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_islamic_aspects");

            migrationBuilder.DropTable(
                name: "event_tech_aspects");

            migrationBuilder.DropTable(
                name: "pds_sync_outbox");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "tenant_setting_overrides");

            migrationBuilder.DropTable(
                name: "TenantCapabilities");

            migrationBuilder.DropTable(
                name: "ModuleDefinitions");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "events");
        }
    }
}
