using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "actor_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "analytics_providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analytics_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    config_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    encrypted_value = table.Column<string>(type: "text", nullable: false),
                    key_version = table.Column<int>(type: "integer", nullable: false),
                    encrypted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    encrypted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.config_key);
                    table.CheckConstraint("CK_AppSettings_NoHighValueSecrets", "config_key NOT LIKE 'Database:%' AND config_key NOT LIKE 'Security:MasterKey%' AND config_key NOT LIKE 'ConnectionStrings:%'");
                });

            migrationBuilder.CreateTable(
                name: "approval_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "atproto_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    collection = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    record_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audience_ages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    min_age = table.Column<int>(type: "integer", nullable: true),
                    max_age = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_ages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audience_genders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audience_genders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "did_custody_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_did_custody_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_formats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_formats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_registration_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_registration_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_session_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_api_key_credit_periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_credit_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_api_key_owner_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_owner_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_api_key_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_usable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_positions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    request_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    request_target = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    request_content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    request_body_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    principal_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "indexed_dids",
                columns: table => new
                {
                    did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    handle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    signing_key = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_indexed_dids", x => x.did);
                });

            migrationBuilder.CreateTable(
                name: "instance_bootstrap_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selected_deployment_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instance_bootstrap_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instance_policy_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    modules_enable_islamic_module_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    modules_enable_islamic_module_override_mode = table.Column<int>(type: "integer", nullable: false),
                    modules_enable_tech_module_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    modules_enable_tech_module_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_user_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_user_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_organization_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_organization_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_group_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_group_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_event_card_click_opens_detail_page_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_event_card_click_opens_detail_page_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_require_verification_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_require_verification_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_allow_tenant_to_omit_verification_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_allow_tenant_to_omit_verification_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_allow_self_registration_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_allow_self_registration_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_allow_group_self_registration_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_allow_group_self_registration_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_display_name_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_display_name_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_logo_url_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_logo_url_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_favicon_url_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_favicon_url_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_custom_css_url_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_custom_css_url_override_mode = table.Column<int>(type: "integer", nullable: false),
                    domains_instance_base_domain_local_value = table.Column<string>(type: "text", nullable: true),
                    domains_instance_base_domain_override_mode = table.Column<int>(type: "integer", nullable: false),
                    domains_allow_tenant_custom_domains_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    domains_allow_tenant_custom_domains_override_mode = table.Column<int>(type: "integer", nullable: false),
                    domains_lock_tenant_subdomain_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    domains_lock_tenant_subdomain_override_mode = table.Column<int>(type: "integer", nullable: false),
                    domains_lock_tenant_custom_domain_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    domains_lock_tenant_custom_domain_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_allow_self_service_registration_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_delegation_allow_self_service_registration_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_allow_white_labeling_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_delegation_allow_white_labeling_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_default_public_home_page_local_value = table.Column<string>(type: "text", nullable: true),
                    tenant_delegation_default_public_home_page_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_lock_tenant_smtp_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_delegation_lock_tenant_smtp_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_lock_tenant_storage_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_delegation_lock_tenant_storage_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_lock_tenant_analytics_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_delegation_lock_tenant_analytics_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_decentralization_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_delegation_decentralization_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    tenant_delegation_authorization_provider_local_value = table.Column<string>(type: "text", nullable: true),
                    tenant_delegation_authorization_provider_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_version_local_value = table.Column<int>(type: "integer", nullable: false),
                    render_policy_version_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_preset_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_preset_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_enable_advanced_overrides_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_enable_advanced_overrides_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_global_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_global_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_global_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_global_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_public_seo_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_public_seo_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_public_seo_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_public_seo_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_operational_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_operational_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_operational_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_operational_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_admin_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_admin_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_admin_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_admin_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_onboarding_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_onboarding_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_onboarding_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_onboarding_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_disallow_interactive_server_on_onboarding_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_disallow_interactive_server_on_onboarding_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_allow_tenant_override_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_allow_tenant_override_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_lock_tenant_public_seo_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_lock_tenant_public_seo_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_lock_tenant_operational_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_lock_tenant_operational_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_lock_tenant_admin_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_lock_tenant_admin_override_mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instance_policy_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "madhabs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_madhabs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "module_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    module_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    wizard_schema_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
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
                name: "notification_entity_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_entity_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_scope_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_scope_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_policy_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    events_allow_user_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_user_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_organization_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_organization_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_group_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_group_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_event_card_click_opens_detail_page_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_event_card_click_opens_detail_page_override_mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_policy_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization_positions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    aggregate_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "owner_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_owner_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pds_sync_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    collection = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    record_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    operation = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    pds_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pds_sync_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "policy_change_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_policy_change_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_scopes", x => x.id);
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
                name: "secret_source_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secret_source_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "secret_validation_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secret_validation_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "setting_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setting_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "setting_value_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setting_value_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false),
                    last_seq_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tag_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_policy_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    events_allow_user_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_user_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_organization_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_organization_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_allow_group_submitted_events_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_allow_group_submitted_events_override_mode = table.Column<int>(type: "integer", nullable: false),
                    events_event_card_click_opens_detail_page_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    events_event_card_click_opens_detail_page_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_require_verification_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_require_verification_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_allow_tenant_to_omit_verification_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_allow_tenant_to_omit_verification_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_allow_self_registration_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_allow_self_registration_override_mode = table.Column<int>(type: "integer", nullable: false),
                    organizations_allow_group_self_registration_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    organizations_allow_group_self_registration_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_display_name_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_display_name_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_logo_url_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_logo_url_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_favicon_url_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_favicon_url_override_mode = table.Column<int>(type: "integer", nullable: false),
                    branding_custom_css_url_local_value = table.Column<string>(type: "text", nullable: true),
                    branding_custom_css_url_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_version_local_value = table.Column<int>(type: "integer", nullable: false),
                    render_policy_version_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_preset_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_preset_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_enable_advanced_overrides_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_enable_advanced_overrides_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_global_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_global_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_global_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_global_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_public_seo_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_public_seo_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_public_seo_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_public_seo_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_operational_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_operational_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_operational_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_operational_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_admin_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_admin_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_admin_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_admin_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_onboarding_render_mode_local_value = table.Column<string>(type: "text", nullable: true),
                    render_policy_onboarding_render_mode_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_onboarding_prerender_enabled_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_onboarding_prerender_enabled_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_disallow_interactive_server_on_onboarding_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_disallow_interactive_server_on_onboarding_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_allow_tenant_override_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_allow_tenant_override_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_lock_tenant_public_seo_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_lock_tenant_public_seo_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_lock_tenant_operational_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_lock_tenant_operational_override_mode = table.Column<int>(type: "integer", nullable: false),
                    render_policy_lock_tenant_admin_local_value = table.Column<bool>(type: "boolean", nullable: false),
                    render_policy_lock_tenant_admin_override_mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_policy_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active_state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_appearance_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    theme_mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    light_snapshot_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_snapshot_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_snapshot_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_snapshot_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_snapshot_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_snapshot_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_preset_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    source_preset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_preset_seed_version = table.Column<int>(type: "integer", nullable: true),
                    is_user_editable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cloned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_appearance_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "visibility_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visibility_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    resource_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    field_scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    master_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role_scope_id = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_filtered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_permissions_role_scopes_role_scope_id",
                        column: x => x.role_scope_id,
                        principalTable: "role_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    role_scope_id = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.UniqueConstraint("ak_roles_id_role_scope_id", x => new { x.id, x.role_scope_id });
                    table.ForeignKey(
                        name: "fk_roles_role_scopes_role_scope_id",
                        column: x => x.role_scope_id,
                        principalTable: "role_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuration_change_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: false),
                    setting_scope_id = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_change_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuration_change_logs_setting_scopes_setting_scope_id",
                        column: x => x.setting_scope_id,
                        principalTable: "setting_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "secret_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    setting_scope_id = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    secret_source_type_id = table.Column<int>(type: "integer", nullable: false),
                    infisical_environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    infisical_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    infisical_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    environment_variable_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    inline_ciphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    inline_ciphertext_version = table.Column<int>(type: "integer", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    secret_validation_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_validation_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_validated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secret_bindings", x => x.id);
                    table.CheckConstraint("ck_secret_bindings_setting_scope_scope_id", "(setting_scope_id = 1 AND scope_id IS NULL) OR (setting_scope_id = 2 AND scope_id IS NOT NULL)");
                    table.CheckConstraint("ck_secret_bindings_source_metadata", "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
                    table.ForeignKey(
                        name: "fk_secret_bindings_secret_source_types_secret_source_type_id",
                        column: x => x.secret_source_type_id,
                        principalTable: "secret_source_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_secret_bindings_secret_validation_statuses_secret_validatio",
                        column: x => x.secret_validation_status_id,
                        principalTable: "secret_validation_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_secret_bindings_setting_scopes_setting_scope_id",
                        column: x => x.setting_scope_id,
                        principalTable: "setting_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    setting_value_type_id = table.Column<int>(type: "integer", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    allowed_values = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_system_settings_setting_value_types_setting_value_type_id",
                        column: x => x.setting_value_type_id,
                        principalTable: "setting_value_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_status_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenants_tenant_statuses_tenant_status_id",
                        column: x => x.tenant_status_id,
                        principalTable: "tenant_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_appearance_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    active_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme_mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "System"),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "auto"),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_appearance_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_appearance_preferences_user_appearance_profiles_active",
                        column: x => x.active_profile_id,
                        principalTable: "user_appearance_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    affected_columns = table.Column<string>(type: "jsonb", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.UniqueConstraint("ak_categories_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_categories_categories_tenant_id_parent_id",
                        columns: x => new { x.tenant_id, x.parent_id },
                        principalTable: "categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "email_dispatch_tenant_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    pause_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    paused_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paused_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_tenant_controls", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_tenant_controls_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_types_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    key_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    secret_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    external_api_key_owner_type_id = table.Column<int>(type: "integer", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_api_key_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    external_api_key_credit_period_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    credit_limit = table.Column<int>(type: "integer", nullable: true),
                    max_rollover_credits = table.Column<int>(type: "integer", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_api_keys_external_api_key_credit_periods_external_",
                        column: x => x.external_api_key_credit_period_id,
                        principalTable: "external_api_key_credit_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_api_keys_external_api_key_owner_types_external_api",
                        column: x => x.external_api_key_owner_type_id,
                        principalTable: "external_api_key_owner_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_api_keys_external_api_key_statuses_external_api_ke",
                        column: x => x.external_api_key_status_id,
                        principalTable: "external_api_key_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_api_keys_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_system = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    internal_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    internal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_binding_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_bindings", x => x.id);
                    table.CheckConstraint("ck_external_bindings_registered_pair_scope", "(external_type = 'provider-customer' AND internal_type = 'Tenant' AND scope_tenant_id IS NULL) OR (external_type = 'external-admin-user' AND internal_type = 'User' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-tenant-user' AND internal_type = 'TenantUser' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-tenant-user-profile' AND internal_type = 'TenantUserProfile' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-user-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'external-admin-user-login' AND internal_type = 'UserExternalLogin' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-organization' AND internal_type = 'Organization' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-organization-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-group' AND internal_type = 'Group' AND scope_tenant_id IS NOT NULL) OR (external_type = 'customer-group-actor' AND internal_type = 'Actor' AND scope_tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_external_bindings_status", "external_binding_status_id IN (1, 2, 3)");
                    table.CheckConstraint("ck_external_bindings_text_not_blank", "length(btrim(provider_key)) > 0 AND length(btrim(external_system)) > 0 AND length(btrim(external_type)) > 0 AND length(btrim(external_id)) > 0 AND length(btrim(internal_type)) > 0");
                    table.ForeignKey(
                        name: "fk_external_bindings_tenants_scope_tenant_id",
                        column: x => x.scope_tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    country = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    city = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timezone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locations", x => x.id);
                    table.UniqueConstraint("ak_locations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_locations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                    table.UniqueConstraint("ak_tags_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_tags_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_capabilities_module_definitions_module_id",
                        column: x => x.module_id,
                        principalTable: "module_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_footer_link_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_footer_link_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_footer_link_groups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_invitations_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_invitations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_lifecycle_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status_id = table.Column<int>(type: "integer", nullable: true),
                    new_status_id = table.Column<int>(type: "integer", nullable: false),
                    transitioned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    transitioned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_lifecycle_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_lifecycle_logs_tenant_statuses_new_status_id",
                        column: x => x.new_status_id,
                        principalTable: "tenant_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_lifecycle_logs_tenant_statuses_old_status_id",
                        column: x => x.old_status_id,
                        principalTable: "tenant_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_lifecycle_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_navigation_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    open_in_new_tab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("pk_tenant_navigation_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_navigation_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_onboarding_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    current_step = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_steps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_steps_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_onboarding_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_onboarding_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_setting_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                name: "tenant_settings_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    defaults_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_settings_documents", x => x.id);
                    table.CheckConstraint("ck_tenant_settings_documents_document_key_not_blank", "length(trim(document_key)) > 0");
                    table.CheckConstraint("ck_tenant_settings_documents_payload_object", "jsonb_typeof(payload_json) = 'object'");
                    table.CheckConstraint("ck_tenant_settings_documents_schema_version_positive", "schema_version > 0");
                    table.ForeignKey(
                        name: "fk_tenant_settings_documents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ui_theme_presets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    theme_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    light_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_editable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    seed_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ui_theme_presets", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_theme_presets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ui_themes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    theme_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    light_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ui_themes", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_themes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_preferences_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_type_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_type_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_type_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_type_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_category_type_categories_category_types_category_type_id",
                        column: x => x.category_type_id,
                        principalTable: "category_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_category_type_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    event_type_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_event_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_templates_event_types_event_type_id",
                        column: x => x.event_type_id,
                        principalTable: "event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_templates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_api_key_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_api_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    credit_limit = table.Column<int>(type: "integer", nullable: false),
                    credits_used = table.Column<int>(type: "integer", nullable: false),
                    rollover_credits = table.Column<int>(type: "integer", nullable: false),
                    request_count = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_quotas", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_api_key_quotas_external_api_keys_external_api_key_",
                        column: x => x.external_api_key_id,
                        principalTable: "external_api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "location_pii",
                columns: table => new
                {
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    postcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_pii", x => x.location_id);
                    table.ForeignKey(
                        name: "fk_location_pii_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.UniqueConstraint("ak_location_rooms_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.UniqueConstraint("ak_location_rooms_tenant_id_location_id_id", x => new { x.tenant_id, x.location_id, x.id });
                    table.CheckConstraint("CK_LocationRoom_NonNegativeCapacity", "capacity IS NULL OR capacity >= 0");
                    table.ForeignKey(
                        name: "fk_location_rooms_locations_tenant_id_location_id",
                        columns: x => new { x.tenant_id, x.location_id },
                        principalTable: "locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_location_rooms_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_type_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_type_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_type_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_tag_type_tags_tag_types_tag_type_id",
                        column: x => x.tag_type_id,
                        principalTable: "tag_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tag_type_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tag_type_tags_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_footer_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    footer_link_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    open_in_new_tab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_footer_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_footer_links_tenant_footer_link_groups_footer_link_g",
                        column: x => x.footer_link_group_id,
                        principalTable: "tenant_footer_link_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_session_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "actor_key_stores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    private_key_encrypted = table.Column<string>(type: "text", nullable: false),
                    public_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_key_stores", x => x.id);
                    table.ForeignKey(
                        name: "fk_actor_key_stores_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actor_pii",
                columns: table => new
                {
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    did = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    handle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    profile_picture_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_pii", x => x.actor_id);
                });

            migrationBuilder.CreateTable(
                name: "actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_type_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    banner_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    background_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    background_effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    banner_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    background_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    did_custody_type_id = table.Column<int>(type: "integer", nullable: true),
                    pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    profile_picture_cid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_actors", x => x.id);
                    table.UniqueConstraint("ak_actors_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_Actor_UserOrOrganization", "(user_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL) OR (user_id IS NULL AND organization_id IS NOT NULL AND group_id IS NULL) OR (user_id IS NULL AND organization_id IS NULL AND group_id IS NOT NULL) OR (user_id IS NULL AND organization_id IS NULL AND group_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_actors_actor_types_actor_type_id",
                        column: x => x.actor_type_id,
                        principalTable: "actor_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actors_did_custody_types_did_custody_type_id",
                        column: x => x.did_custody_type_id,
                        principalTable: "did_custody_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actors_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    website_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    approval_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_notes = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                    table.UniqueConstraint("ak_organizations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_organizations_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_organizations_approval_statuses_approval_status_id",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "storage_objects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    file_type_id = table.Column<int>(type: "integer", nullable: false),
                    uri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    extension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_objects", x => x.id);
                    table.ForeignKey(
                        name: "fk_storage_objects_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_storage_objects_file_types_file_type_id",
                        column: x => x.file_type_id,
                        principalTable: "file_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_storage_objects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auth_provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    auth_provider_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_pii",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    country = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    postcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_pii", x => x.organization_id);
                    table.ForeignKey(
                        name: "fk_organization_pii_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_setting_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_setting_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_setting_overrides_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_setting_overrides_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_series",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    featured_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    total_views = table.Column<int>(type: "integer", nullable: false),
                    visibility_type_id = table.Column<int>(type: "integer", nullable: false),
                    start_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_event_series", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_series_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_series_storage_objects_featured_image_id",
                        column: x => x.featured_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_event_series_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_series_visibility_types_visibility_type_id",
                        column: x => x.visibility_type_id,
                        principalTable: "visibility_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    profile_picture_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
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
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.UniqueConstraint("ak_groups_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_groups_no_self_parent", "parent_group_id IS NULL OR parent_group_id <> id");
                    table.CheckConstraint("ck_groups_parent_exclusive", "parent_organization_id IS NULL OR parent_group_id IS NULL");
                    table.ForeignKey(
                        name: "fk_groups_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_groups_approval_statuses_approval_status_id",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_groups_tenant_id_parent_group_id",
                        columns: x => new { x.tenant_id, x.parent_group_id },
                        principalTable: "groups",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_organizations_tenant_id_parent_organization_id",
                        columns: x => new { x.tenant_id, x.parent_organization_id },
                        principalTable: "organizations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_storage_objects_profile_picture_id",
                        column: x => x.profile_picture_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_groups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notification_entity_type_id = table.Column<int>(type: "integer", nullable: true),
                    entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notification_scope_id = table.Column<int>(type: "integer", nullable: false),
                    source_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_context_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_reason_id = table.Column<int>(type: "integer", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    snoozed_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.CheckConstraint("ck_notifications_entity_reference_shape", "(notification_entity_type_id IS NULL AND entity_id IS NULL) OR (notification_entity_type_id IS NOT NULL AND entity_id ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')");
                    table.ForeignKey(
                        name: "fk_notifications_actors_recipient_context_actor_id",
                        column: x => x.recipient_context_actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_actors_source_actor_id",
                        column: x => x.source_actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_notification_entity_types_notification_entity",
                        column: x => x.notification_entity_type_id,
                        principalTable: "notification_entity_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_notification_reasons_notification_reason_id",
                        column: x => x.notification_reason_id,
                        principalTable: "notification_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_notification_scope_types_notification_scope_id",
                        column: x => x.notification_scope_id,
                        principalTable: "notification_scope_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_notification_types_notification_type_id",
                        column: x => x.notification_type_id,
                        principalTable: "notification_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    organization_position_id = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_organization_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_members_organization_positions_organization_po",
                        column: x => x.organization_position_id,
                        principalTable: "organization_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_members_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_members_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "platform_user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    ban_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    removed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_users", x => x.id);
                    table.UniqueConstraint("ak_tenant_users_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_tenant_users_status", "status_id IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_tenant_users_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_authentication_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    access_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pds_host = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dpop_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_authentication_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_authentication_tokens_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_authentication_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_external_logins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    provider_display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_external_logins_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_external_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notification_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_notification_preferences_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_pii",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    first_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    last_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_pii", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_pii_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    audience_gender_id = table.Column<int>(type: "integer", nullable: true),
                    audience_age_id = table.Column<int>(type: "integer", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    featured_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_views = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_registration_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_user_reported = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    event_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    madhab_id = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    visibility_type_id = table.Column<int>(type: "integer", nullable: false),
                    session_count = table.Column<int>(type: "integer", nullable: true),
                    event_status_id = table.Column<int>(type: "integer", nullable: false),
                    external_registration_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    first_session_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_session_date = table.Column<DateOnly>(type: "date", nullable: true),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_template_key = table.Column<string>(type: "text", nullable: true),
                    source_template_version = table.Column<int>(type: "integer", nullable: true),
                    instantiated_from_template_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_from_template_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_session_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_session_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    event_time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    event_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    series_order = table.Column<int>(type: "integer", nullable: true),
                    event_format_id = table.Column<int>(type: "integer", nullable: false),
                    registration_policy_id = table.Column<int>(type: "integer", nullable: true),
                    atproto_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    background_color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    background_effect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    background_image_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.UniqueConstraint("ak_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_Event_NonNegativePrice", "price IS NULL OR price >= 0");
                    table.CheckConstraint("CK_Event_SessionDateRange", "first_session_date IS NULL OR last_session_date IS NULL OR first_session_date <= last_session_date");
                    table.CheckConstraint("CK_Event_SessionStartUtcRange", "first_session_start_utc IS NULL OR last_session_start_utc IS NULL OR first_session_start_utc <= last_session_start_utc");
                    table.CheckConstraint("CK_Event_TimeZoneIdNotBlank", "event_time_zone_id IS NULL OR length(btrim(event_time_zone_id)) > 0");
                    table.ForeignKey(
                        name: "fk_events_actors_tenant_id_actor_id",
                        columns: x => new { x.tenant_id, x.actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_atproto_records_atproto_record_id",
                        column: x => x.atproto_record_id,
                        principalTable: "atproto_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_events_audience_ages_audience_age_id",
                        column: x => x.audience_age_id,
                        principalTable: "audience_ages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_audience_genders_audience_gender_id",
                        column: x => x.audience_gender_id,
                        principalTable: "audience_genders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_event_formats_event_format_id",
                        column: x => x.event_format_id,
                        principalTable: "event_formats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_event_registration_policies_registration_policy_id",
                        column: x => x.registration_policy_id,
                        principalTable: "event_registration_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_event_series_event_series_id",
                        column: x => x.event_series_id,
                        principalTable: "event_series",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_events_event_statuses_event_status_id",
                        column: x => x.event_status_id,
                        principalTable: "event_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_event_types_event_type_id",
                        column: x => x.event_type_id,
                        principalTable: "event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_madhabs_madhab_id",
                        column: x => x.madhab_id,
                        principalTable: "madhabs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_storage_objects_background_image_id",
                        column: x => x.background_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_events_storage_objects_featured_image_id",
                        column: x => x.featured_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_visibility_types_visibility_type_id",
                        column: x => x.visibility_type_id,
                        principalTable: "visibility_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    group_position_id = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_members_group_positions_group_position_id",
                        column: x => x.group_position_id,
                        principalTable: "group_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_members_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_setting_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_setting_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_setting_overrides_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_setting_overrides_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name_override = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    contact_email_override = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    locale = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    preferences_json = table.Column<string>(type: "jsonb", nullable: true),
                    consent_json = table.Column<string>(type: "jsonb", nullable: true),
                    admin_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_user_profiles_tenant_users_tenant_user_id",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_user_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_role_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    role_scope_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_user_role_grants", x => x.id);
                    table.CheckConstraint("ck_tenant_user_role_grants_role_scope", "role_scope_id = 1");
                    table.ForeignKey(
                        name: "fk_tenant_user_role_grants_roles_role_id_role_scope_id",
                        columns: x => new { x.role_id, x.role_scope_id },
                        principalTable: "roles",
                        principalColumns: new[] { "id", "role_scope_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_user_role_grants_tenant_users_tenant_id_tenant_user_",
                        columns: x => new { x.tenant_id, x.tenant_user_id },
                        principalTable: "tenant_users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_user_role_grants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_categories_categories_tenant_id_category_id",
                        columns: x => new { x.tenant_id, x.category_id },
                        principalTable: "categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_categories_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_contact_share_exports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exported_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_contact_share_exports", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_contact_share_exports_actors_tenant_id_recipient_acto",
                        columns: x => new { x.tenant_id, x.recipient_actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_exports_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_exports_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_exports_users_exported_by_user_id",
                        column: x => x.exported_by_user_id,
                        principalTable: "users",
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
                    table.UniqueConstraint("ak_event_days_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_days_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_days_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
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
                name: "event_role_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_role_assignments", x => x.id);
                    table.CheckConstraint("ck_event_role_assignments_validity_window", "expires_at_utc IS NULL OR expires_at_utc > starts_at_utc");
                    table.ForeignKey(
                        name: "fk_event_role_assignments_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_role_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_role_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_role_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_event_session_groups", x => x.id);
                    table.UniqueConstraint("ak_event_session_groups_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_session_groups_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_EventSessionGroup_RoomRequiresLocation", "room_id IS NULL OR location_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_event_session_groups_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_groups_location_rooms_tenant_id_location_id_r",
                        columns: x => new { x.tenant_id, x.location_id, x.room_id },
                        principalTable: "location_rooms",
                        principalColumns: new[] { "tenant_id", "location_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_session_groups_locations_tenant_id_location_id",
                        columns: x => new { x.tenant_id, x.location_id },
                        principalTable: "locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_session_groups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_tags_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_tags_tags_tenant_id_tag_id",
                        columns: x => new { x.tenant_id, x.tag_id },
                        principalTable: "tags",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_tags_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_tech_aspects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_repo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    hackathon_track = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    skill_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tech_stack_tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    requires_laptop = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_coding_competition = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_team_size = table.Column<int>(type: "integer", nullable: true),
                    prize_pool = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
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
                name: "organization_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_reviews_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_reviews_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_reviews_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.CheckConstraint("CK_EventAgendaItem_LocalEndMinuteMatchesTime", "local_end_minute_of_day = ((EXTRACT(HOUR FROM local_end_time)::int * 60) + EXTRACT(MINUTE FROM local_end_time)::int)");
                    table.CheckConstraint("CK_EventAgendaItem_LocalEndMinuteRange", "local_end_minute_of_day BETWEEN 0 AND 1439");
                    table.CheckConstraint("CK_EventAgendaItem_LocalStartMinuteMatchesTime", "local_start_minute_of_day = ((EXTRACT(HOUR FROM local_start_time)::int * 60) + EXTRACT(MINUTE FROM local_start_time)::int)");
                    table.CheckConstraint("CK_EventAgendaItem_LocalStartMinuteRange", "local_start_minute_of_day BETWEEN 0 AND 1439");
                    table.CheckConstraint("CK_EventAgendaItem_RoomRequiresLocation", "room_id IS NULL OR location_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_event_agenda_items_event_days_tenant_id_event_id_event_day_",
                        columns: x => new { x.tenant_id, x.event_id, x.event_day_id },
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_location_rooms_tenant_id_location_id_roo",
                        columns: x => new { x.tenant_id, x.location_id, x.room_id },
                        principalTable: "location_rooms",
                        principalColumns: new[] { "tenant_id", "location_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_agenda_items_locations_tenant_id_location_id",
                        columns: x => new { x.tenant_id, x.location_id },
                        principalTable: "locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "event_registration_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_scope_id = table.Column<int>(type: "integer", nullable: false),
                    selected_event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_policy_snapshot_id = table.Column<int>(type: "integer", nullable: true),
                    approval_status_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_event_registration_intents", x => x.id);
                    table.UniqueConstraint("ak_event_registration_intents_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_registration_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_registration_intents_approval_statuses_approval_statu",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_event_days_tenant_id_event_id_se",
                        columns: x => new { x.tenant_id, x.event_id, x.selected_event_day_id },
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_event_registration_policies_regi",
                        column: x => x.registration_policy_snapshot_id,
                        principalTable: "event_registration_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_registration_scopes_registration",
                        column: x => x.registration_scope_id,
                        principalTable: "registration_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    local_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    local_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    local_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    local_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    local_start_minute_of_day = table.Column<int>(type: "integer", nullable: false),
                    local_end_minute_of_day = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    event_session_kind_id = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    max_audience_attendees = table.Column<int>(type: "integer", nullable: true),
                    current_audience_attendees = table.Column<int>(type: "integer", nullable: true),
                    registration_mode_id = table.Column<int>(type: "integer", nullable: true),
                    featured_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_template_key = table.Column<string>(type: "text", nullable: true),
                    source_template_version = table.Column<int>(type: "integer", nullable: true),
                    instantiated_from_template_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_from_template_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_event_sessions", x => x.id);
                    table.UniqueConstraint("ak_event_sessions_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_sessions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_EventSession_EndAfterStart", "end_time > start_time");
                    table.CheckConstraint("CK_EventSession_LocalEndMinuteMatchesTime", "local_end_minute_of_day = ((EXTRACT(HOUR FROM local_end_time)::int * 60) + EXTRACT(MINUTE FROM local_end_time)::int)");
                    table.CheckConstraint("CK_EventSession_LocalEndMinuteRange", "local_end_minute_of_day BETWEEN 0 AND 1439");
                    table.CheckConstraint("CK_EventSession_LocalStartMinuteMatchesTime", "local_start_minute_of_day = ((EXTRACT(HOUR FROM local_start_time)::int * 60) + EXTRACT(MINUTE FROM local_start_time)::int)");
                    table.CheckConstraint("CK_EventSession_LocalStartMinuteRange", "local_start_minute_of_day BETWEEN 0 AND 1439");
                    table.CheckConstraint("CK_EventSession_NonNegativePrice", "price IS NULL OR price >= 0");
                    table.CheckConstraint("CK_EventSession_RoomRequiresLocation", "room_id IS NULL OR location_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_event_sessions_event_days_tenant_id_event_id_event_day_id",
                        columns: x => new { x.tenant_id, x.event_id, x.event_day_id },
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_sessions_event_session_kinds_event_session_kind_id",
                        column: x => x.event_session_kind_id,
                        principalTable: "event_session_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_sessions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_sessions_location_rooms_tenant_id_location_id_room_id",
                        columns: x => new { x.tenant_id, x.location_id, x.room_id },
                        principalTable: "location_rooms",
                        principalColumns: new[] { "tenant_id", "location_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_sessions_locations_tenant_id_location_id",
                        columns: x => new { x.tenant_id, x.location_id },
                        principalTable: "locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_sessions_registration_modes_registration_mode_id",
                        column: x => x.registration_mode_id,
                        principalTable: "registration_modes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_event_sessions_storage_objects_featured_image_id",
                        column: x => x.featured_image_id,
                        principalTable: "storage_objects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_event_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_dispatch_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publish_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    source_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    plain_text_body = table.Column<string>(type: "text", nullable: true),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    reply_to = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    parked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("pk_email_dispatch_outbox", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_event_registration_intents_registrati",
                        column: x => x.registration_intent_id,
                        principalTable: "event_registration_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_contact_share_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_event_registration_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    email_normalized_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    consent_text_snapshot = table.Column<string>(type: "text", nullable: false),
                    consent_ui_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    withdrawn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_contact_share_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consents_actors_tenant_id_recipient_act",
                        columns: x => new { x.tenant_id, x.recipient_actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consents_event_registration_intents_ten",
                        columns: x => new { x.tenant_id, x.source_event_registration_intent_id },
                        principalTable: "event_registration_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consents_events_tenant_id_source_event_",
                        columns: x => new { x.tenant_id, x.source_event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_registration_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_status_id = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atproto_record_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_event_registrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_registrations_approval_statuses_approval_status_id",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registrations_atproto_records_atproto_record_id",
                        column: x => x.atproto_record_id,
                        principalTable: "atproto_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_registrations_event_registration_intents_tenant_id_ev",
                        columns: x => new { x.tenant_id, x.event_id, x.event_registration_intent_id },
                        principalTable: "event_registration_intents",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_registrations_event_sessions_tenant_id_event_id_event",
                        columns: x => new { x.tenant_id, x.event_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_registrations_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_registrations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registrations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_agenda_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_agenda_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_agenda_items_event_sessions_tenant_id_event_s",
                        columns: x => new { x.tenant_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_agenda_items_locations_tenant_id_location_id",
                        columns: x => new { x.tenant_id, x.location_id },
                        principalTable: "locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_session_agenda_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_categories_categories_tenant_id_category_id",
                        columns: x => new { x.tenant_id, x.category_id },
                        principalTable: "categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_categories_event_sessions_tenant_id_event_ses",
                        columns: x => new { x.tenant_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_group_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_group_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_event_session_groups_tenant_id",
                        columns: x => new { x.tenant_id, x.event_id, x.event_session_group_id },
                        principalTable: "event_session_groups",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_event_sessions_tenant_id_event",
                        columns: x => new { x.tenant_id, x.event_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_group_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_islamic_aspects",
                columns: table => new
                {
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time_type = table.Column<int>(type: "integer", nullable: false),
                    reference_prayer = table.Column<int>(type: "integer", nullable: true),
                    offset_minutes = table.Column<int>(type: "integer", nullable: true),
                    requires_wudu = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ritual_requirements_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_islamic_aspects", x => x.event_session_id);
                    table.CheckConstraint("CK_EventSessionIslamicAspect_OffsetRange", "offset_minutes IS NULL OR offset_minutes BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_EventSessionIslamicAspect_ReferencePrayerRange", "reference_prayer IS NULL OR reference_prayer BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_EventSessionIslamicAspect_StartTimeState", "((start_time_type = 0 AND reference_prayer IS NULL AND offset_minutes IS NULL) OR (start_time_type = 1 AND reference_prayer IS NOT NULL AND offset_minutes IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_event_session_islamic_aspects_event_sessions_event_session_",
                        column: x => x.event_session_id,
                        principalTable: "event_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_session_languages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_languages", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_languages_event_sessions_tenant_id_event_sess",
                        columns: x => new { x.tenant_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_languages_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_session_languages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_speakers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_speakers", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_speakers_actors_tenant_id_actor_id",
                        columns: x => new { x.tenant_id, x.actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_speakers_event_sessions_tenant_id_event_sessi",
                        columns: x => new { x.tenant_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_speakers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_session_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_session_tags_event_sessions_tenant_id_event_session_id",
                        columns: x => new { x.tenant_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_tags_tags_tenant_id_tag_id",
                        columns: x => new { x.tenant_id, x.tag_id },
                        principalTable: "tags",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_session_tags_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_dispatch_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_dispatch_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    transport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sanitized_error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_attempts_email_dispatch_outbox_email_dispatc",
                        column: x => x.email_dispatch_outbox_id,
                        principalTable: "email_dispatch_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_email_dispatch_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_dispatch_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publish_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_dispatch_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    consumer_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_receipts_email_dispatch_outbox_email_dispatc",
                        column: x => x.email_dispatch_outbox_id,
                        principalTable: "email_dispatch_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_email_dispatch_receipts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_contact_share_export_items",
                columns: table => new
                {
                    export_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_contact_share_export_items", x => new { x.export_id, x.consent_id });
                    table.ForeignKey(
                        name: "fk_event_contact_share_export_items_event_contact_share_consen",
                        column: x => x.consent_id,
                        principalTable: "event_contact_share_consents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_export_items_event_contact_share_export",
                        column: x => x.export_id,
                        principalTable: "event_contact_share_exports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom_property_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("pk_custom_property_definitions", x => x.id);
                    table.CheckConstraint("ck_custom_property_definitions_shared_entity_type", "entity_type_name IN ('Organization', 'Group')");
                    table.ForeignKey(
                        name: "fk_custom_property_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custom_property_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_custom_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_property_options_custom_property_definitions_custom_",
                        column: x => x.custom_property_definition_id,
                        principalTable: "custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_custom_property_options_custom_property_options_parent_opti",
                        column: x => x.parent_option_id,
                        principalTable: "custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "custom_property_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    text_value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    number_value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    date_time_value = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_custom_property_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_property_values_custom_property_definitions_custom_p",
                        column: x => x.custom_property_definition_id,
                        principalTable: "custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_custom_property_values_custom_property_options_option_id",
                        column: x => x.option_id,
                        principalTable: "custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_custom_property_values_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_custom_property_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_custom_property_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_custom_property_definitions_event_templates_source_te",
                        column: x => x.source_template_id,
                        principalTable: "event_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_custom_property_definitions_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_custom_property_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_custom_property_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    event_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_custom_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_ecpo_definition",
                        column: x => x.event_custom_property_definition_id,
                        principalTable: "event_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ecpo_parent_option",
                        column: x => x.parent_option_id,
                        principalTable: "event_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_custom_property_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    event_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_custom_property_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_custom_property_values_event_custom_property_definiti",
                        column: x => x.event_custom_property_definition_id,
                        principalTable: "event_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_custom_property_values_event_custom_property_options_",
                        column: x => x.option_id,
                        principalTable: "event_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_custom_property_values_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_custom_property_values_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_custom_property_projections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_custom_property_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_custom_property_projections", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_custom_property_projections_event_custom_property_def",
                        column: x => x.event_custom_property_definition_id,
                        principalTable: "event_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_custom_property_projections_event_custom_property_opt",
                        column: x => x.option_id,
                        principalTable: "event_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_event_custom_property_projections_event_custom_property_val",
                        column: x => x.event_custom_property_value_id,
                        principalTable: "event_custom_property_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_custom_property_projections_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_custom_property_projections_tenants_tenant_id",
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
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
                        onDelete: ReferentialAction.Restrict);
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
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
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "event_template_custom_property_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    event_template_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_template_custom_property_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_template_custom_property_definitions_event_templates_",
                        column: x => x.event_template_id,
                        principalTable: "event_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_template_custom_property_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_template_custom_property_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    event_template_custom_property_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_event_template_custom_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_etcpo_definition",
                        column: x => x.event_template_custom_property_definition_id,
                        principalTable: "event_template_custom_property_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_etcpo_parent_option",
                        column: x => x.parent_option_id,
                        principalTable: "event_template_custom_property_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_actor_key_stores_actor_id",
                table: "actor_key_stores",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_key_stores_tenant_id",
                table: "actor_key_stores",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_pii_did",
                table: "actor_pii",
                column: "did");

            migrationBuilder.CreateIndex(
                name: "ix_actor_pii_handle",
                table: "actor_pii",
                column: "handle");

            migrationBuilder.CreateIndex(
                name: "ix_actors_actor_type_id",
                table: "actors",
                column: "actor_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_background_image_id",
                table: "actors",
                column: "background_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_banner_picture_id",
                table: "actors",
                column: "banner_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_did_custody_type_id",
                table: "actors",
                column: "did_custody_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_group_id",
                table: "actors",
                column: "group_id",
                unique: true,
                filter: "group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_actors_organization_id",
                table: "actors",
                column: "organization_id",
                unique: true,
                filter: "organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_actors_profile_picture_id",
                table: "actors",
                column: "profile_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_actors_user_id_tenant_id",
                table: "actors",
                columns: new[] { "user_id", "tenant_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_category",
                table: "app_settings",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_is_sensitive",
                table: "app_settings",
                column: "is_sensitive");

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_key_version",
                table: "app_settings",
                column: "key_version");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_records_did_collection_record_key",
                table: "atproto_records",
                columns: new[] { "did", "collection", "record_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auditlogs_tenant_actor_time",
                table: "audit_logs",
                columns: new[] { "tenant_id", "actor_id", "timestamp" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_auditlogs_tenant_entity",
                table: "audit_logs",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditlogs_tenant_time",
                table: "audit_logs",
                columns: new[] { "tenant_id", "timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_categories_tenant_id_parent_id",
                table: "categories",
                columns: new[] { "tenant_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_tenant_master_code",
                table: "categories",
                columns: new[] { "tenant_id", "master_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_category_id",
                table: "category_type_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_category_type_id",
                table: "category_type_categories",
                column: "category_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_tenant_id",
                table: "category_type_categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_change_logs_setting_key",
                table: "configuration_change_logs",
                column: "setting_key");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_change_logs_setting_scope_id_scope_id",
                table: "configuration_change_logs",
                columns: new[] { "setting_scope_id", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_change_logs_timestamp",
                table: "configuration_change_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_change_logs_user_id",
                table: "configuration_change_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_cpd_tenant_entity_active",
                table: "custom_property_definitions",
                columns: new[] { "tenant_id", "entity_type_name", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_cpd_tenant_entity_namespace_key",
                table: "custom_property_definitions",
                columns: new[] { "tenant_id", "entity_type_name", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cpd_tenant_entity_search_filter",
                table: "custom_property_definitions",
                columns: new[] { "tenant_id", "entity_type_name", "is_searchable", "is_filterable" });

            migrationBuilder.CreateIndex(
                name: "ix_custom_property_definitions_default_option_id",
                table: "custom_property_definitions",
                column: "default_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_cpo_definition_namespace_key",
                table: "custom_property_options",
                columns: new[] { "custom_property_definition_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cpo_definition_sort",
                table: "custom_property_options",
                columns: new[] { "custom_property_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_custom_property_options_parent_option_id",
                table: "custom_property_options",
                column: "parent_option_id");

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
                name: "ix_cpv_definition_entity_ordinal",
                table: "custom_property_values",
                columns: new[] { "custom_property_definition_id", "entity_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cpv_tenant_definition",
                table: "custom_property_values",
                columns: new[] { "tenant_id", "custom_property_definition_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cpv_tenant_entity",
                table: "custom_property_values",
                columns: new[] { "tenant_id", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_custom_property_values_option_id",
                table: "custom_property_values",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_attempts_tenant_started",
                table: "email_dispatch_attempts",
                columns: new[] { "tenant_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_attempts_outbox_attempt",
                table: "email_dispatch_attempts",
                columns: new[] { "email_dispatch_outbox_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_event_id",
                table: "email_dispatch_outbox",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_registration_intent_id",
                table: "email_dispatch_outbox",
                column: "registration_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_tenant_status",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "status", "last_failure_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_user_id",
                table: "email_dispatch_outbox",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_worker_poll",
                table: "email_dispatch_outbox",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_outbox_tenant_publish_event",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "publish_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_outbox_tenant_source_kind",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "source_type", "source_id", "kind" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_receipts_outbox_status",
                table: "email_dispatch_receipts",
                columns: new[] { "email_dispatch_outbox_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_receipts_tenant_publish_event",
                table: "email_dispatch_receipts",
                columns: new[] { "tenant_id", "publish_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_tenant_controls_pause_state",
                table: "email_dispatch_tenant_controls",
                columns: new[] { "is_paused", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_tenant_controls_tenant",
                table: "email_dispatch_tenant_controls",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_kind_id",
                table: "event_agenda_items",
                column: "kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_tenant_event_local_start",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "local_start_date", "local_start_minute_of_day" });

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_tenant_event_sort",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_tenant_id_event_id_event_day_id",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_tenant_id_location_id_room_id",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "location_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_categories_tenant_event_category",
                table: "event_categories",
                columns: new[] { "tenant_id", "event_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_categories_tenant_id_category_id",
                table: "event_categories",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_event_id",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_event_registr",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_event_registration_intent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_user_id",
                table: "event_contact_share_consents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventcontactshareconsents_recipient_status",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "recipient_actor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_eventcontactshareconsents_scope_unique",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "user_id", "recipient_actor_id", "purpose_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventcontactshareconsents_user_status",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_export_items_consent_id",
                table: "event_contact_share_export_items",
                column: "consent_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_exports_exported_by_user_id",
                table: "event_contact_share_exports",
                column: "exported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_exports_tenant_id_event_id",
                table: "event_contact_share_exports",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_eventcontactshareexports_recipient_date",
                table: "event_contact_share_exports",
                columns: new[] { "tenant_id", "recipient_actor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ecpd_event_namespace_key",
                table: "event_custom_property_definitions",
                columns: new[] { "event_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ecpd_tenant_event_search_filter",
                table: "event_custom_property_definitions",
                columns: new[] { "tenant_id", "event_id", "is_searchable", "is_filterable" });

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_definitions_default_option_id",
                table: "event_custom_property_definitions",
                column: "default_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_definitions_source_template_id",
                table: "event_custom_property_definitions",
                column: "source_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_ecpo_definition_namespace_key",
                table: "event_custom_property_options",
                columns: new[] { "event_custom_property_definition_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ecpo_definition_sort",
                table: "event_custom_property_options",
                columns: new[] { "event_custom_property_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_options_parent_option_id",
                table: "event_custom_property_options",
                column: "parent_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_ecpp_tenant_event_namespace_key_ordinal",
                table: "event_custom_property_projections",
                columns: new[] { "tenant_id", "event_id", "namespace", "key", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "ix_ecpp_tenant_exposure",
                table: "event_custom_property_projections",
                columns: new[] { "tenant_id", "exposure_level" });

            migrationBuilder.CreateIndex(
                name: "ix_ecpp_tenant_namespace_key_normalized",
                table: "event_custom_property_projections",
                columns: new[] { "tenant_id", "namespace", "key", "normalized_value" });

            migrationBuilder.CreateIndex(
                name: "ix_ecpp_value",
                table: "event_custom_property_projections",
                column: "event_custom_property_value_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_projections_event_custom_property_def",
                table: "event_custom_property_projections",
                column: "event_custom_property_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_projections_event_id",
                table: "event_custom_property_projections",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_projections_option_id",
                table: "event_custom_property_projections",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_ecpv_definition_event_ordinal",
                table: "event_custom_property_values",
                columns: new[] { "event_custom_property_definition_id", "event_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ecpv_tenant_event",
                table: "event_custom_property_values",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_values_event_id",
                table: "event_custom_property_values",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_custom_property_values_option_id",
                table: "event_custom_property_values",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_days_banner_image_id",
                table: "event_days",
                column: "banner_image_id");

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
                name: "ix_event_islamic_aspects_madhab_id",
                table: "event_islamic_aspects",
                column: "madhab_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_islamic_aspects_primary_language_id",
                table: "event_islamic_aspects",
                column: "primary_language_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_approval_status_id",
                table: "event_registration_intents",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_registration_policy_snapshot_id",
                table: "event_registration_intents",
                column: "registration_policy_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_registration_scope_id",
                table: "event_registration_intents",
                column: "registration_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_event_day",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "selected_event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_event_user_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id", "registration_scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_user",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_day_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id", "selected_event_day_id" },
                unique: true,
                filter: "registration_scope_id = 2 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_session_selection_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id" },
                unique: true,
                filter: "registration_scope_id = 3 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_user_id",
                table: "event_registration_intents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_policies_master_code",
                table: "event_registration_policies",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_approval_status_id",
                table: "event_registrations",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_atproto_record_id",
                table: "event_registrations",
                column: "atproto_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_intent",
                table: "event_registrations",
                columns: new[] { "tenant_id", "event_id", "event_registration_intent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations",
                columns: new[] { "tenant_id", "event_id", "event_session_id", "user_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_user",
                table: "event_registrations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_role_id",
                table: "event_role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_tenant_event_role_status",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "event_id", "role_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_tenant_event_user_status",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "event_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_tenant_user_event_status",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "user_id", "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_unique_pending_active",
                table: "event_role_assignments",
                columns: new[] { "tenant_id", "event_id", "user_id", "role_id" },
                unique: true,
                filter: "status IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "ix_event_role_assignments_user_id",
                table: "event_role_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_actor_id",
                table: "event_series",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_featured_image_id",
                table: "event_series",
                column: "featured_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_series_tenant_id_is_published",
                table: "event_series",
                columns: new[] { "tenant_id", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ix_event_series_tenant_id_slug",
                table: "event_series",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_series_tenant_id_total_views",
                table: "event_series",
                columns: new[] { "tenant_id", "total_views" });

            migrationBuilder.CreateIndex(
                name: "ix_event_series_visibility_type_id",
                table: "event_series",
                column: "visibility_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_agenda_items_tenant_id_event_session_id",
                table: "event_session_agenda_items",
                columns: new[] { "tenant_id", "event_session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_agenda_items_tenant_id_location_id",
                table: "event_session_agenda_items",
                columns: new[] { "tenant_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_categories_tenant_id_category_id",
                table: "event_session_categories",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_categories_tenant_session_category",
                table: "event_session_categories",
                columns: new[] { "tenant_id", "event_session_id", "category_id" },
                unique: true);

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
                name: "ix_event_session_group_sessions_tenant_event_group_session",
                table: "event_session_group_sessions",
                columns: new[] { "tenant_id", "event_id", "event_session_group_id", "event_session_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_tenant_event_session_primary",
                table: "event_session_group_sessions",
                columns: new[] { "tenant_id", "event_id", "event_session_id", "is_primary" },
                unique: true,
                filter: "is_primary = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_group_sessions_tenant_group_sort",
                table: "event_session_group_sessions",
                columns: new[] { "tenant_id", "event_session_group_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_tenant_event_slug",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "event_id", "slug" },
                unique: true,
                filter: "is_deleted = false AND slug IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_tenant_event_sort",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "event_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_tenant_id_location_id_room_id",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "location_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_kinds_master_code",
                table: "event_session_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_languages_language_id",
                table: "event_session_languages",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventsessionlanguages_session_language",
                table: "event_session_languages",
                columns: new[] { "tenant_id", "event_session_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_speakers_tenant_id_actor_id",
                table: "event_session_speakers",
                columns: new[] { "tenant_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_speakers_tenant_session_actor",
                table: "event_session_speakers",
                columns: new[] { "tenant_id", "event_session_id", "actor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_session_tags_tenant_id_tag_id",
                table: "event_session_tags",
                columns: new[] { "tenant_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_tags_tenant_session_tag",
                table: "event_session_tags",
                columns: new[] { "tenant_id", "event_session_id", "tag_id" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_event_session_kind_id",
                table: "event_sessions",
                column: "event_session_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_featured_image_id",
                table: "event_sessions",
                column: "featured_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_registration_mode_id",
                table: "event_sessions",
                column: "registration_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_day_sort",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_day_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_event_local_start",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_id", "local_start_date", "local_start_minute_of_day" });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_id_event_id_event_day_id",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_id", "event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_tenant_location_room_time",
                table: "event_sessions",
                columns: new[] { "tenant_id", "location_id", "room_id", "start_time", "end_time" });

            migrationBuilder.CreateIndex(
                name: "ix_event_tags_tenant_event_tag",
                table: "event_tags",
                columns: new[] { "tenant_id", "event_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_tags_tenant_id_tag_id",
                table: "event_tags",
                columns: new[] { "tenant_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_etcpd_template_namespace_key",
                table: "event_template_custom_property_definitions",
                columns: new[] { "event_template_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_etcpd_tenant_search_filter",
                table: "event_template_custom_property_definitions",
                columns: new[] { "tenant_id", "is_searchable", "is_filterable" });

            migrationBuilder.CreateIndex(
                name: "ix_event_template_custom_property_definitions_default_option_id",
                table: "event_template_custom_property_definitions",
                column: "default_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_etcpo_definition_namespace_key",
                table: "event_template_custom_property_options",
                columns: new[] { "event_template_custom_property_definition_id", "namespace", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_etcpo_definition_sort",
                table: "event_template_custom_property_options",
                columns: new[] { "event_template_custom_property_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_template_custom_property_options_parent_option_id",
                table: "event_template_custom_property_options",
                column: "parent_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_templates_event_type_id",
                table: "event_templates",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_templates_tenant_key_version",
                table: "event_templates",
                columns: new[] { "tenant_id", "template_key", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_templates_tenant_published_active",
                table: "event_templates",
                columns: new[] { "tenant_id", "is_published", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_event_types_global_master_code",
                table: "event_types",
                column: "master_code",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_types_tenant_master_code",
                table: "event_types",
                columns: new[] { "tenant_id", "master_code" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_events_atproto_record_id",
                table: "events",
                column: "atproto_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_audience_age_id",
                table: "events",
                column: "audience_age_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_audience_gender_id",
                table: "events",
                column: "audience_gender_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_background_image_id",
                table: "events",
                column: "background_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_format_id",
                table: "events",
                column: "event_format_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_series_id",
                table: "events",
                column: "event_series_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_status_id",
                table: "events",
                column: "event_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_event_type_id",
                table: "events",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_featured_image_id",
                table: "events",
                column: "featured_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_madhab_id",
                table: "events",
                column: "madhab_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_registration_policy_id",
                table: "events",
                column: "registration_policy_id");

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
                name: "ix_events_visibility_type_id",
                table: "events",
                column: "visibility_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_key_owner_types_master_code",
                table: "external_api_key_owner_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_api_key_quotas_external_api_key_id_period_start",
                table: "external_api_key_quotas",
                columns: new[] { "external_api_key_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_external_api_key_credit_period_id",
                table: "external_api_keys",
                column: "external_api_key_credit_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_external_api_key_owner_type_id",
                table: "external_api_keys",
                column: "external_api_key_owner_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_external_api_key_status_id",
                table: "external_api_keys",
                column: "external_api_key_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_key_id",
                table: "external_api_keys",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_tenant_id_external_api_key_owner_type_id_",
                table: "external_api_keys",
                columns: new[] { "tenant_id", "external_api_key_owner_type_id", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_tenant_id_external_api_key_status_id",
                table: "external_api_keys",
                columns: new[] { "tenant_id", "external_api_key_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_external_global_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "external_type", "external_id" },
                unique: true,
                filter: "scope_tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_external_tenant_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "external_type", "external_id", "scope_tenant_id" },
                unique: true,
                filter: "scope_tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_internal_global_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "internal_type", "internal_id" },
                unique: true,
                filter: "scope_tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_internal_tenant_unique",
                table: "external_bindings",
                columns: new[] { "provider_key", "external_system", "internal_type", "internal_id", "scope_tenant_id" },
                unique: true,
                filter: "scope_tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_external_bindings_scope_tenant_id",
                table: "external_bindings",
                column: "scope_tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_group_position_id",
                table: "group_members",
                column: "group_position_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_group_user",
                table: "group_members",
                columns: new[] { "group_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_members_role_id",
                table: "group_members",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_tenant_user",
                table: "group_members",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_members_user_id",
                table: "group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_setting_overrides_group_id_setting_key",
                table: "group_setting_overrides",
                columns: new[] { "group_id", "setting_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_setting_overrides_tenant_id",
                table: "group_setting_overrides",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_actor_id",
                table: "groups",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_approval_status_id",
                table: "groups",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_profile_picture_id",
                table: "groups",
                column: "profile_picture_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_active_status",
                table: "groups",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_name",
                table: "groups",
                columns: new[] { "tenant_id", "full_name" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_parent_group",
                table: "groups",
                columns: new[] { "tenant_id", "parent_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_parent_organization",
                table: "groups",
                columns: new[] { "tenant_id", "parent_organization_id" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresAt",
                table: "idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key_TenantId",
                table: "idempotency_records",
                columns: new[] { "key", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_state_completed_unique",
                table: "instance_bootstrap_states",
                column: "is_completed",
                unique: true,
                filter: "\"is_completed\" = true");

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
                name: "ix_locations_tenant_city",
                table: "locations",
                columns: new[] { "tenant_id", "city" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_country",
                table: "locations",
                columns: new[] { "tenant_id", "country" });

            migrationBuilder.CreateIndex(
                name: "ix_module_definitions_display_order",
                table: "module_definitions",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "ix_module_definitions_module_key",
                table: "module_definitions",
                column: "module_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_scope_types_master_code",
                table: "notification_scope_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_notification_entity_type_id",
                table: "notifications",
                column: "notification_entity_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_notification_reason_id",
                table: "notifications",
                column: "notification_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_notification_scope_id",
                table: "notifications",
                column: "notification_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_notification_type_id",
                table: "notifications",
                column: "notification_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_context_actor_id",
                table: "notifications",
                column: "recipient_context_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_source_actor_id",
                table: "notifications",
                column: "source_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_tenant_type",
                table: "notifications",
                columns: new[] { "tenant_id", "notification_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_tenant_user_unread",
                table: "notifications",
                columns: new[] { "tenant_id", "user_id", "is_read", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_unread_by_user",
                table: "notifications",
                columns: new[] { "tenant_id", "user_id", "created_at" },
                descending: new[] { false, false, true },
                filter: "is_read = false AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_archived",
                table: "notifications",
                columns: new[] { "user_id", "is_archived", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_scope",
                table: "notifications",
                columns: new[] { "user_id", "notification_scope_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_position_id",
                table: "organization_members",
                column: "organization_position_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_role_id",
                table: "organization_members",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_tenant_id",
                table: "organization_members",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_orgmembers_org_user",
                table: "organization_members",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orgmembers_user",
                table: "organization_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_pii_name",
                table: "organization_pii",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_organization_policy_sets_organization_id",
                table: "organization_policy_sets",
                column: "organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_reviews_event_id",
                table: "organization_reviews",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_reviews_organization_id",
                table: "organization_reviews",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_reviews_tenant_id",
                table: "organization_reviews",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_reviews_user_id",
                table: "organization_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_setting_overrides_organization_id_setting_key",
                table: "organization_setting_overrides",
                columns: new[] { "organization_id", "setting_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_setting_overrides_tenant_id",
                table: "organization_setting_overrides",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_actor_id",
                table: "organizations",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_approval_status_id",
                table: "organizations",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant",
                table: "organizations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant_active_status",
                table: "organizations",
                columns: new[] { "tenant_id", "is_deleted", "approval_status_id" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Aggregate",
                table: "outbox_messages",
                columns: new[] { "aggregate_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Dedup",
                table: "outbox_messages",
                columns: new[] { "aggregate_type", "aggregate_id", "event_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_WorkerPoll",
                table: "outbox_messages",
                columns: new[] { "status", "next_retry_at", "created_at" });

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
                name: "ix_permissions_mastercode",
                table: "permissions",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_resource_action",
                table: "permissions",
                columns: new[] { "resource_kind", "action" });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_role_scope_id",
                table: "permissions",
                column: "role_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_user_roles_role_id",
                table: "platform_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_user_roles_user_id",
                table: "platform_user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_user_roles_user_id_role_id",
                table: "platform_user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_policy_change_outbox_status_retry",
                table: "policy_change_outbox",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_scopes_master_code",
                table: "registration_scopes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rolepermissions_permission",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_rolepermissions_role",
                table: "role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_scopes_master_code",
                table: "role_scopes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_mastercode",
                table: "roles",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_role_scope_id",
                table: "roles",
                column: "role_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_item_kinds_master_code",
                table: "schedule_item_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_secret_source_type_id",
                table: "secret_bindings",
                column: "secret_source_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_secret_validation_status_id",
                table: "secret_bindings",
                column: "secret_validation_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_setting_key_instance_unique",
                table: "secret_bindings",
                column: "setting_key",
                unique: true,
                filter: "scope_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_setting_key_scope_id_tenant_unique",
                table: "secret_bindings",
                columns: new[] { "setting_key", "scope_id" },
                unique: true,
                filter: "scope_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_setting_scope_id_scope_id",
                table: "secret_bindings",
                columns: new[] { "setting_scope_id", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_secret_source_types_master_code",
                table: "secret_source_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_secret_validation_statuses_master_code",
                table: "secret_validation_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_setting_scopes_master_code",
                table: "setting_scopes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_setting_value_types_master_code",
                table: "setting_value_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_actor_id",
                table: "storage_objects",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_file_type_id",
                table: "storage_objects",
                column: "file_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_tenant_id",
                table: "storage_objects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_states_service",
                table: "sync_states",
                column: "service",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_setting_key",
                table: "system_settings",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_setting_value_type_id",
                table: "system_settings",
                column: "setting_value_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tag_id",
                table: "tag_type_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tag_type_id",
                table: "tag_type_tags",
                column: "tag_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tenant_id",
                table: "tag_type_tags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_tenant_master_code",
                table: "tags",
                columns: new[] { "tenant_id", "master_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_module_id",
                table: "tenant_capabilities",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_tenant_id_module_id",
                table: "tenant_capabilities",
                columns: new[] { "tenant_id", "module_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_footer_link_groups_tenant_id",
                table: "tenant_footer_link_groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_footer_link_groups_tenant_id_order",
                table: "tenant_footer_link_groups",
                columns: new[] { "tenant_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_footer_links_footer_link_group_id_order",
                table: "tenant_footer_links",
                columns: new[] { "footer_link_group_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_invitations_role_id",
                table: "tenant_invitations",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_invitations_tenant_id_email",
                table: "tenant_invitations",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_invitations_token",
                table: "tenant_invitations",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_lifecycle_logs_new_status_id",
                table: "tenant_lifecycle_logs",
                column: "new_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_lifecycle_logs_old_status_id",
                table: "tenant_lifecycle_logs",
                column: "old_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_lifecycle_logs_tenant_id_transitioned_at",
                table: "tenant_lifecycle_logs",
                columns: new[] { "tenant_id", "transitioned_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_lifecycle_logs_transitioned_by_user_id",
                table: "tenant_lifecycle_logs",
                column: "transitioned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_navigation_links_tenant_id",
                table: "tenant_navigation_links",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_navigation_links_tenant_id_order",
                table: "tenant_navigation_links",
                columns: new[] { "tenant_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_onboarding_states_tenant_id",
                table: "tenant_onboarding_states",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_policy_sets_tenant_id",
                table: "tenant_policy_sets",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_setting_overrides_tenant_id_setting_key",
                table: "tenant_setting_overrides",
                columns: new[] { "tenant_id", "setting_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_documents_document_key",
                table: "tenant_settings_documents",
                column: "document_key");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_documents_tenant_id_document_key",
                table: "tenant_settings_documents",
                columns: new[] { "tenant_id", "document_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenantuserprofiles_tenant_contact_email",
                table: "tenant_user_profiles",
                columns: new[] { "tenant_id", "contact_email_override" },
                filter: "contact_email_override IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenantuserprofiles_tenant_user",
                table: "tenant_user_profiles",
                column: "tenant_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_user_role_grants_active_tenant_user_role",
                table: "tenant_user_role_grants",
                columns: new[] { "tenant_id", "tenant_user_id", "role_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_user_role_grants_role_id_role_scope_id",
                table: "tenant_user_role_grants",
                columns: new[] { "role_id", "role_scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_user_role_grants_tenant_role",
                table: "tenant_user_role_grants",
                columns: new[] { "tenant_id", "role_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_users_actor_id",
                table: "tenant_users",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_users_user_id",
                table: "tenant_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenantusers_tenant_actor",
                table: "tenant_users",
                columns: new[] { "tenant_id", "actor_id" },
                unique: true,
                filter: "actor_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenantusers_tenant_user",
                table: "tenant_users",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_tenant_status_id",
                table: "tenants",
                column: "tenant_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ui_theme_presets_tenant_id_is_active",
                table: "ui_theme_presets",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_ui_theme_presets_tenant_id_theme_key",
                table: "ui_theme_presets",
                columns: new[] { "tenant_id", "theme_key" },
                unique: true,
                filter: "tenant_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_theme_presets_theme_key",
                table: "ui_theme_presets",
                column: "theme_key",
                unique: true,
                filter: "tenant_id IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_is_default",
                table: "ui_themes",
                column: "is_default",
                unique: true,
                filter: "tenant_id IS NULL AND is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_tenant_id_is_default",
                table: "ui_themes",
                columns: new[] { "tenant_id", "is_default" },
                unique: true,
                filter: "tenant_id IS NOT NULL AND is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_tenant_id_theme_key",
                table: "ui_themes",
                columns: new[] { "tenant_id", "theme_key" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_theme_key",
                table: "ui_themes",
                column: "theme_key",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_preferences_active_profile_id",
                table: "user_appearance_preferences",
                column: "active_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_preferences_user_id_tenant_id",
                table: "user_appearance_preferences",
                columns: new[] { "user_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_source_preset_id",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "source_preset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_tenant_id_is_archived",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "tenant_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_tenant_id_is_default",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "tenant_id", "is_default" },
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_tenant_id_name",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_user_authentication_tokens_tenant_id",
                table: "user_authentication_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_authentication_tokens_user_id",
                table: "user_authentication_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_external_logins_tenant_id",
                table: "user_external_logins",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_external_logins_user_id",
                table: "user_external_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_notification_preferences_tenant_id_user_id_category",
                table: "user_notification_preferences",
                columns: new[] { "tenant_id", "user_id", "category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_notification_preferences_user_id",
                table: "user_notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_pii_email",
                table: "user_pii",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_preferences_tenant_id_user_id_setting_key",
                table: "user_preferences",
                columns: new[] { "tenant_id", "user_id", "setting_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_actor_id",
                table: "users",
                column: "actor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_actor_key_stores_actors_actor_id",
                table: "actor_key_stores",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_actor_pii_actors_actor_id",
                table: "actor_pii",
                column: "actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_groups_group_id",
                table: "actors",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_organizations_organization_id",
                table: "actors",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_background_image_id",
                table: "actors",
                column: "background_image_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_banner_picture_id",
                table: "actors",
                column: "banner_picture_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_profile_picture_id",
                table: "actors",
                column: "profile_picture_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_actors_users_user_id",
                table: "actors",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_custom_property_definitions_custom_property_options_default",
                table: "custom_property_definitions",
                column: "default_option_id",
                principalTable: "custom_property_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_custom_property_definitions_event_custom_property_opt",
                table: "event_custom_property_definitions",
                column: "default_option_id",
                principalTable: "event_custom_property_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

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

            migrationBuilder.AddForeignKey(
                name: "fk_event_template_custom_property_definitions_event_template_c",
                table: "event_template_custom_property_definitions",
                column: "default_option_id",
                principalTable: "event_template_custom_property_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_series_actors_actor_id",
                table: "event_series");

            migrationBuilder.DropForeignKey(
                name: "fk_events_actors_tenant_id_actor_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_actors_actor_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_actors_actor_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_actors_actor_id",
                table: "storage_objects");

            migrationBuilder.DropForeignKey(
                name: "fk_users_actors_actor_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_custom_property_definitions_tenants_tenant_id",
                table: "custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_custom_property_definitions_tenants_tenant_id",
                table: "event_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_days_tenants_tenant_id",
                table: "event_days");

            migrationBuilder.DropForeignKey(
                name: "fk_event_series_tenants_tenant_id",
                table: "event_series");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_custom_property_definitions_tenants_tenant_id",
                table: "event_session_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_template_custom_property_definitions_tenants_",
                table: "event_session_template_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_templates_tenants_tenant_id",
                table: "event_session_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_tenants_tenant_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_template_custom_property_definitions_tenants_tenant_id",
                table: "event_template_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_templates_tenants_tenant_id",
                table: "event_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_event_types_tenants_tenant_id",
                table: "event_types");

            migrationBuilder.DropForeignKey(
                name: "fk_events_tenants_tenant_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_location_rooms_tenants_tenant_id",
                table: "location_rooms");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_tenants_tenant_id",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_objects_tenants_tenant_id",
                table: "storage_objects");

            migrationBuilder.DropForeignKey(
                name: "fk_event_days_storage_objects_banner_image_id",
                table: "event_days");

            migrationBuilder.DropForeignKey(
                name: "fk_event_series_storage_objects_featured_image_id",
                table: "event_series");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_storage_objects_featured_image_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_events_storage_objects_background_image_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_storage_objects_featured_image_id",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_custom_property_definitions_custom_property_options_default",
                table: "custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_custom_property_definitions_events_event_id",
                table: "event_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_days_events_tenant_id_event_id",
                table: "event_days");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_events_tenant_id_event_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_event_days_tenant_id_event_id_event_day_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_location_rooms_tenant_id_location_id_room_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_locations_tenant_id_location_id",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_custom_property_definitions_event_custom_property_opt",
                table: "event_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_templates_event_templates_event_template_id",
                table: "event_session_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_event_template_custom_property_definitions_event_templates_",
                table: "event_template_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_custom_property_definitions_event_sessions_ev",
                table: "event_session_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_custom_property_definitions_event_session_cus",
                table: "event_session_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_template_custom_property_definitions_event_se1",
                table: "event_session_template_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_template_custom_property_definitions_event_se",
                table: "event_session_template_custom_property_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_event_template_custom_property_definitions_event_template_c",
                table: "event_template_custom_property_definitions");

            migrationBuilder.DropTable(
                name: "actor_key_stores");

            migrationBuilder.DropTable(
                name: "actor_pii");

            migrationBuilder.DropTable(
                name: "analytics_providers");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "category_type_categories");

            migrationBuilder.DropTable(
                name: "configuration_change_logs");

            migrationBuilder.DropTable(
                name: "custom_property_projection_dirty_scope");

            migrationBuilder.DropTable(
                name: "custom_property_projection_status");

            migrationBuilder.DropTable(
                name: "custom_property_values");

            migrationBuilder.DropTable(
                name: "email_dispatch_attempts");

            migrationBuilder.DropTable(
                name: "email_dispatch_receipts");

            migrationBuilder.DropTable(
                name: "email_dispatch_tenant_controls");

            migrationBuilder.DropTable(
                name: "event_agenda_items");

            migrationBuilder.DropTable(
                name: "event_categories");

            migrationBuilder.DropTable(
                name: "event_contact_share_export_items");

            migrationBuilder.DropTable(
                name: "event_custom_property_projections");

            migrationBuilder.DropTable(
                name: "event_islamic_aspects");

            migrationBuilder.DropTable(
                name: "event_registrations");

            migrationBuilder.DropTable(
                name: "event_role_assignments");

            migrationBuilder.DropTable(
                name: "event_session_agenda_items");

            migrationBuilder.DropTable(
                name: "event_session_categories");

            migrationBuilder.DropTable(
                name: "event_session_custom_property_projections");

            migrationBuilder.DropTable(
                name: "event_session_group_sessions");

            migrationBuilder.DropTable(
                name: "event_session_islamic_aspects");

            migrationBuilder.DropTable(
                name: "event_session_languages");

            migrationBuilder.DropTable(
                name: "event_session_speakers");

            migrationBuilder.DropTable(
                name: "event_session_tags");

            migrationBuilder.DropTable(
                name: "event_tags");

            migrationBuilder.DropTable(
                name: "event_tech_aspects");

            migrationBuilder.DropTable(
                name: "external_api_key_quotas");

            migrationBuilder.DropTable(
                name: "external_bindings");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "group_setting_overrides");

            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "indexed_dids");

            migrationBuilder.DropTable(
                name: "instance_bootstrap_states");

            migrationBuilder.DropTable(
                name: "instance_policy_sets");

            migrationBuilder.DropTable(
                name: "location_pii");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "organization_members");

            migrationBuilder.DropTable(
                name: "organization_pii");

            migrationBuilder.DropTable(
                name: "organization_policy_sets");

            migrationBuilder.DropTable(
                name: "organization_reviews");

            migrationBuilder.DropTable(
                name: "organization_setting_overrides");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "owner_types");

            migrationBuilder.DropTable(
                name: "pds_sync_outbox");

            migrationBuilder.DropTable(
                name: "platform_user_roles");

            migrationBuilder.DropTable(
                name: "policy_change_outbox");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "secret_bindings");

            migrationBuilder.DropTable(
                name: "sync_states");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "tag_type_tags");

            migrationBuilder.DropTable(
                name: "tenant_capabilities");

            migrationBuilder.DropTable(
                name: "tenant_footer_links");

            migrationBuilder.DropTable(
                name: "tenant_invitations");

            migrationBuilder.DropTable(
                name: "tenant_lifecycle_logs");

            migrationBuilder.DropTable(
                name: "tenant_navigation_links");

            migrationBuilder.DropTable(
                name: "tenant_onboarding_states");

            migrationBuilder.DropTable(
                name: "tenant_policy_sets");

            migrationBuilder.DropTable(
                name: "tenant_setting_overrides");

            migrationBuilder.DropTable(
                name: "tenant_settings_documents");

            migrationBuilder.DropTable(
                name: "tenant_user_profiles");

            migrationBuilder.DropTable(
                name: "tenant_user_role_grants");

            migrationBuilder.DropTable(
                name: "ui_theme_presets");

            migrationBuilder.DropTable(
                name: "ui_themes");

            migrationBuilder.DropTable(
                name: "user_appearance_preferences");

            migrationBuilder.DropTable(
                name: "user_authentication_tokens");

            migrationBuilder.DropTable(
                name: "user_external_logins");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "user_pii");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "category_types");

            migrationBuilder.DropTable(
                name: "email_dispatch_outbox");

            migrationBuilder.DropTable(
                name: "schedule_item_kinds");

            migrationBuilder.DropTable(
                name: "event_contact_share_consents");

            migrationBuilder.DropTable(
                name: "event_contact_share_exports");

            migrationBuilder.DropTable(
                name: "event_custom_property_values");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "event_session_custom_property_values");

            migrationBuilder.DropTable(
                name: "event_session_groups");

            migrationBuilder.DropTable(
                name: "languages");

            migrationBuilder.DropTable(
                name: "external_api_keys");

            migrationBuilder.DropTable(
                name: "group_positions");

            migrationBuilder.DropTable(
                name: "notification_entity_types");

            migrationBuilder.DropTable(
                name: "notification_reasons");

            migrationBuilder.DropTable(
                name: "notification_scope_types");

            migrationBuilder.DropTable(
                name: "notification_types");

            migrationBuilder.DropTable(
                name: "organization_positions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "secret_source_types");

            migrationBuilder.DropTable(
                name: "secret_validation_statuses");

            migrationBuilder.DropTable(
                name: "setting_scopes");

            migrationBuilder.DropTable(
                name: "setting_value_types");

            migrationBuilder.DropTable(
                name: "tag_types");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "module_definitions");

            migrationBuilder.DropTable(
                name: "tenant_footer_link_groups");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "tenant_users");

            migrationBuilder.DropTable(
                name: "user_appearance_profiles");

            migrationBuilder.DropTable(
                name: "event_registration_intents");

            migrationBuilder.DropTable(
                name: "external_api_key_credit_periods");

            migrationBuilder.DropTable(
                name: "external_api_key_owner_types");

            migrationBuilder.DropTable(
                name: "external_api_key_statuses");

            migrationBuilder.DropTable(
                name: "role_scopes");

            migrationBuilder.DropTable(
                name: "registration_scopes");

            migrationBuilder.DropTable(
                name: "actors");

            migrationBuilder.DropTable(
                name: "actor_types");

            migrationBuilder.DropTable(
                name: "did_custody_types");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "approval_statuses");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "tenant_statuses");

            migrationBuilder.DropTable(
                name: "storage_objects");

            migrationBuilder.DropTable(
                name: "file_types");

            migrationBuilder.DropTable(
                name: "custom_property_options");

            migrationBuilder.DropTable(
                name: "custom_property_definitions");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "atproto_records");

            migrationBuilder.DropTable(
                name: "audience_ages");

            migrationBuilder.DropTable(
                name: "audience_genders");

            migrationBuilder.DropTable(
                name: "event_formats");

            migrationBuilder.DropTable(
                name: "event_registration_policies");

            migrationBuilder.DropTable(
                name: "event_series");

            migrationBuilder.DropTable(
                name: "event_statuses");

            migrationBuilder.DropTable(
                name: "madhabs");

            migrationBuilder.DropTable(
                name: "visibility_types");

            migrationBuilder.DropTable(
                name: "event_days");

            migrationBuilder.DropTable(
                name: "location_rooms");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "event_custom_property_options");

            migrationBuilder.DropTable(
                name: "event_custom_property_definitions");

            migrationBuilder.DropTable(
                name: "event_templates");

            migrationBuilder.DropTable(
                name: "event_types");

            migrationBuilder.DropTable(
                name: "event_sessions");

            migrationBuilder.DropTable(
                name: "event_session_kinds");

            migrationBuilder.DropTable(
                name: "registration_modes");

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

            migrationBuilder.DropTable(
                name: "event_template_custom_property_options");

            migrationBuilder.DropTable(
                name: "event_template_custom_property_definitions");
        }
    }
}
