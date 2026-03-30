using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class customevents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_session_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_templates_event_templates_event_template_id",
                        column: x => x.event_template_id,
                        principalTable: "event_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_templates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_custom_property_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    property_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_multi = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    exposure_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_searchable = table.Column<bool>(type: "boolean", nullable: false),
                    is_filterable = table.Column<bool>(type: "boolean", nullable: false),
                    is_exportable = table.Column<bool>(type: "boolean", nullable: false),
                    is_moderation_relevant = table.Column<bool>(type: "boolean", nullable: false),
                    is_analytics_relevant = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_owned = table.Column<bool>(type: "boolean", nullable: false),
                    default_text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_number_value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    default_boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    default_date_time_value = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    default_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    min_length = table.Column<int>(type: "integer", nullable: true),
                    max_length = table.Column<int>(type: "integer", nullable: true),
                    regex_pattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    min_number = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    max_number = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    min_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allowed_url_schemes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_template_version = table.Column<int>(type: "integer", nullable: true),
                    source_template_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instantiated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_synced_from_template_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_custom_property_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_definitions_event_session_tem",
                        column: x => x.source_template_id,
                        principalTable: "event_session_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_definitions_event_sessions_ev",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_custom_property_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    parent_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_template_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_template_version = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_custom_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_escpo_definition",
                        column: x => x.event_session_custom_property_definition_id,
                        principalTable: "event_session_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_escpo_parent_option",
                        column: x => x.parent_option_id,
                        principalTable: "event_session_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_session_custom_property_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    text_value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    number_value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    date_time_value = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_custom_property_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_values_event_session_custom_p",
                        column: x => x.event_session_custom_property_definition_id,
                        principalTable: "event_session_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_values_event_session_custom_p1",
                        column: x => x.option_id,
                        principalTable: "event_session_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_values_event_sessions_event_s",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_values_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_custom_property_projections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_custom_property_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    property_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    exposure_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_searchable = table.Column<bool>(type: "boolean", nullable: false),
                    is_filterable = table.Column<bool>(type: "boolean", nullable: false),
                    is_exportable = table.Column<bool>(type: "boolean", nullable: false),
                    is_moderation_relevant = table.Column<bool>(type: "boolean", nullable: false),
                    is_analytics_relevant = table.Column<bool>(type: "boolean", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text_value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    number_value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    date_time_value = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    normalized_value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_custom_property_projections", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_projections_event_session_cus",
                        column: x => x.event_session_custom_property_definition_id,
                        principalTable: "event_session_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_projections_event_session_cus1",
                        column: x => x.event_session_custom_property_value_id,
                        principalTable: "event_session_custom_property_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_projections_event_session_cus2",
                        column: x => x.option_id,
                        principalTable: "event_session_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_projections_event_sessions_ev",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_custom_property_projections_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_template_custom_property_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    property_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_multi = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    exposure_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_searchable = table.Column<bool>(type: "boolean", nullable: false),
                    is_filterable = table.Column<bool>(type: "boolean", nullable: false),
                    is_exportable = table.Column<bool>(type: "boolean", nullable: false),
                    is_moderation_relevant = table.Column<bool>(type: "boolean", nullable: false),
                    is_analytics_relevant = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_owned = table.Column<bool>(type: "boolean", nullable: false),
                    default_text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    default_number_value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    default_boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    default_date_time_value = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    default_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    min_length = table.Column<int>(type: "integer", nullable: true),
                    max_length = table.Column<int>(type: "integer", nullable: true),
                    regex_pattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    min_number = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    max_number = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    min_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allowed_url_schemes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_template_custom_property_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_template_custom_property_definitions_event_se1",
                        column: x => x.event_session_template_id,
                        principalTable: "event_session_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_template_custom_property_definitions_tenants_",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_template_custom_property_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_template_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    parent_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_template_custom_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_estcpo_definition",
                        column: x => x.event_session_template_custom_property_definition_id,
                        principalTable: "event_session_template_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_estcpo_parent_option",
                        column: x => x.parent_option_id,
                        principalTable: "event_session_template_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_escpd_session_namespace_key",
                table: "event_session_custom_property_definitions",
                columns: new[] { "event_session_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_escpd_tenant_session_search_filter",
                table: "event_session_custom_property_definitions",
                columns: new[] { "tenant_id", "event_session_id", "is_searchable", "is_filterable" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_definitions_default_option_id",
                table: "event_session_custom_property_definitions",
                column: "default_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_definitions_source_template_id",
                table: "event_session_custom_property_definitions",
                column: "source_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_escpo_definition_namespace_key",
                table: "event_session_custom_property_options",
                columns: new[] { "event_session_custom_property_definition_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_escpo_definition_sort",
                table: "event_session_custom_property_options",
                columns: new[] { "event_session_custom_property_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_options_parent_option_id",
                table: "event_session_custom_property_options",
                column: "parent_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_escpp_tenant_exposure",
                table: "event_session_custom_property_projections",
                columns: new[] { "tenant_id", "exposure_level" });

            migrationBuilder.CreateIndex(
                name: "ix_escpp_tenant_namespace_key_normalized",
                table: "event_session_custom_property_projections",
                columns: new[] { "tenant_id", "namespace", "key", "normalized_value" });

            migrationBuilder.CreateIndex(
                name: "ix_escpp_tenant_session_namespace_key_ordinal",
                table: "event_session_custom_property_projections",
                columns: new[] { "tenant_id", "event_session_id", "namespace", "key", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "ix_escpp_value",
                table: "event_session_custom_property_projections",
                column: "event_session_custom_property_value_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_projections_event_session_cus",
                table: "event_session_custom_property_projections",
                column: "event_session_custom_property_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_projections_event_session_id",
                table: "event_session_custom_property_projections",
                column: "event_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_projections_option_id",
                table: "event_session_custom_property_projections",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_escpv_definition_session_ordinal",
                table: "event_session_custom_property_values",
                columns: new[] { "event_session_custom_property_definition_id", "event_session_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_escpv_tenant_session",
                table: "event_session_custom_property_values",
                columns: new[] { "tenant_id", "event_session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_values_event_session_id",
                table: "event_session_custom_property_values",
                column: "event_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_custom_property_values_option_id",
                table: "event_session_custom_property_values",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_estcpd_template_namespace_key",
                table: "event_session_template_custom_property_definitions",
                columns: new[] { "event_session_template_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estcpd_tenant_search_filter",
                table: "event_session_template_custom_property_definitions",
                columns: new[] { "tenant_id", "is_searchable", "is_filterable" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_template_custom_property_definitions_default_",
                table: "event_session_template_custom_property_definitions",
                column: "default_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_estcpo_definition_namespace_key",
                table: "event_session_template_custom_property_options",
                columns: new[] { "event_session_template_custom_property_definition_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estcpo_definition_sort",
                table: "event_session_template_custom_property_options",
                columns: new[] { "event_session_template_custom_property_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_template_custom_property_options_parent_optio",
                table: "event_session_template_custom_property_options",
                column: "parent_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_est_template_key_version",
                table: "event_session_templates",
                columns: new[] { "event_template_id", "session_template_key", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_est_tenant_published_active",
                table: "event_session_templates",
                columns: new[] { "tenant_id", "is_published", "is_active" });

            migrationBuilder.AddForeignKey(
                name: "fk_event_session_custom_property_definitions_event_session_cus",
                table: "event_session_custom_property_definitions",
                column: "default_option_id",
                principalTable: "event_session_custom_property_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_session_template_custom_property_definitions_event_se",
                table: "event_session_template_custom_property_definitions",
                column: "default_option_id",
                principalTable: "event_session_template_custom_property_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_session_custom_property_definitions_event_session_cus",
                table: "event_session_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_template_custom_property_definitions_event_se1",
                table: "event_session_template_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_template_custom_property_definitions_event_se",
                table: "event_session_template_custom_property_definitions");

            migrationBuilder.DropTable(
                name: "event_session_custom_property_projections");

            migrationBuilder.DropTable(
                name: "event_session_custom_property_values");

            migrationBuilder.DropTable(
                name: "event_session_custom_property_options");

            migrationBuilder.DropTable(
                name: "event_session_custom_property_definitions");

            migrationBuilder.DropTable(
                name: "event_session_templates");

            migrationBuilder.DropTable(
                name: "event_session_template_custom_property_options");

            migrationBuilder.DropTable(
                name: "event_session_template_custom_property_definitions");
        }
    }
}
