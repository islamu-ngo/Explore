using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJsonbFromPolicySets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branding_policy",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_policy",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_policy",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_policy",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_policy",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_policy",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_policy",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "modules_policy",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_policy",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_policy",
                table: "instance_policy_sets");

            migrationBuilder.AddColumn<string>(
                name: "branding_custom_css_url_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_custom_css_url_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_display_name_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_display_name_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_favicon_url_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_favicon_url_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_logo_url_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_logo_url_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_group_submitted_events_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_group_submitted_events_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_organization_submitted_events_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_organization_submitted_events_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_user_submitted_events_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_user_submitted_events_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_event_card_click_opens_detail_page_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_event_card_click_opens_detail_page_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_allow_group_self_registration_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_allow_group_self_registration_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_allow_self_registration_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_allow_self_registration_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_allow_tenant_to_omit_verification_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_allow_tenant_to_omit_verification_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_require_verification_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_require_verification_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_admin_prerender_enabled_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_admin_prerender_enabled_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_admin_render_mode_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_admin_render_mode_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_allow_tenant_override_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_allow_tenant_override_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_disallow_interactive_server_on_onboarding_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_disallow_interactive_server_on_onboarding_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_enable_advanced_overrides_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_enable_advanced_overrides_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_global_prerender_enabled_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_global_prerender_enabled_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_global_render_mode_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_global_render_mode_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_lock_tenant_admin_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_lock_tenant_admin_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_lock_tenant_operational_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_lock_tenant_operational_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_lock_tenant_public_seo_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_lock_tenant_public_seo_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_onboarding_prerender_enabled_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_onboarding_prerender_enabled_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_onboarding_render_mode_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_onboarding_render_mode_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_operational_prerender_enabled_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_operational_prerender_enabled_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_operational_render_mode_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_operational_render_mode_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_preset_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_preset_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_public_seo_prerender_enabled_local_value",
                table: "tenant_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_public_seo_prerender_enabled_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_public_seo_render_mode_local_value",
                table: "tenant_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_public_seo_render_mode_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_version_local_value",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_version_override_mode",
                table: "tenant_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_group_submitted_events_local_value",
                table: "organization_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_group_submitted_events_override_mode",
                table: "organization_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_organization_submitted_events_local_value",
                table: "organization_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_organization_submitted_events_override_mode",
                table: "organization_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_user_submitted_events_local_value",
                table: "organization_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_user_submitted_events_override_mode",
                table: "organization_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_event_card_click_opens_detail_page_local_value",
                table: "organization_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_event_card_click_opens_detail_page_override_mode",
                table: "organization_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_custom_css_url_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_custom_css_url_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_display_name_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_display_name_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_favicon_url_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_favicon_url_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "branding_logo_url_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branding_logo_url_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "domains_allow_tenant_custom_domains_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "domains_allow_tenant_custom_domains_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "domains_instance_base_domain_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "domains_instance_base_domain_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "domains_lock_tenant_custom_domain_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "domains_lock_tenant_custom_domain_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "domains_lock_tenant_subdomain_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "domains_lock_tenant_subdomain_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_group_submitted_events_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_group_submitted_events_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_organization_submitted_events_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_organization_submitted_events_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_allow_user_submitted_events_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_allow_user_submitted_events_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "events_event_card_click_opens_detail_page_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "events_event_card_click_opens_detail_page_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "modules_enable_islamic_module_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "modules_enable_islamic_module_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "modules_enable_tech_module_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "modules_enable_tech_module_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_allow_group_self_registration_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_allow_group_self_registration_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_allow_self_registration_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_allow_self_registration_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_allow_tenant_to_omit_verification_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_allow_tenant_to_omit_verification_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "organizations_require_verification_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "organizations_require_verification_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_admin_prerender_enabled_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_admin_prerender_enabled_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_admin_render_mode_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_admin_render_mode_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_allow_tenant_override_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_allow_tenant_override_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_disallow_interactive_server_on_onboarding_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_disallow_interactive_server_on_onboarding_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_enable_advanced_overrides_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_enable_advanced_overrides_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_global_prerender_enabled_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_global_prerender_enabled_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_global_render_mode_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_global_render_mode_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_lock_tenant_admin_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_lock_tenant_admin_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_lock_tenant_operational_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_lock_tenant_operational_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_lock_tenant_public_seo_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_lock_tenant_public_seo_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_onboarding_prerender_enabled_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_onboarding_prerender_enabled_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_onboarding_render_mode_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_onboarding_render_mode_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_operational_prerender_enabled_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_operational_prerender_enabled_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_operational_render_mode_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_operational_render_mode_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_preset_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_preset_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "render_policy_public_seo_prerender_enabled_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_public_seo_prerender_enabled_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "render_policy_public_seo_render_mode_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_public_seo_render_mode_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_version_local_value",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "render_policy_version_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "tenant_delegation_allow_self_service_registration_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_allow_self_service_registration_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "tenant_delegation_allow_white_labeling_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_allow_white_labeling_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "tenant_delegation_authorization_provider_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_authorization_provider_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "tenant_delegation_decentralization_enabled_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_decentralization_enabled_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "tenant_delegation_default_public_home_page_local_value",
                table: "instance_policy_sets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_default_public_home_page_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "tenant_delegation_lock_tenant_analytics_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_lock_tenant_analytics_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "tenant_delegation_lock_tenant_smtp_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_lock_tenant_smtp_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "tenant_delegation_lock_tenant_storage_local_value",
                table: "instance_policy_sets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "tenant_delegation_lock_tenant_storage_override_mode",
                table: "instance_policy_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "branding_custom_css_url_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_custom_css_url_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_display_name_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_display_name_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_favicon_url_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_favicon_url_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_logo_url_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_logo_url_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_group_submitted_events_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_group_submitted_events_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_organization_submitted_events_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_organization_submitted_events_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_user_submitted_events_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_user_submitted_events_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_event_card_click_opens_detail_page_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_event_card_click_opens_detail_page_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_group_self_registration_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_group_self_registration_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_self_registration_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_self_registration_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_tenant_to_omit_verification_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_tenant_to_omit_verification_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_require_verification_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_require_verification_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_prerender_enabled_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_prerender_enabled_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_render_mode_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_render_mode_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_allow_tenant_override_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_allow_tenant_override_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_disallow_interactive_server_on_onboarding_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_disallow_interactive_server_on_onboarding_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_enable_advanced_overrides_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_enable_advanced_overrides_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_prerender_enabled_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_prerender_enabled_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_render_mode_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_render_mode_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_admin_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_admin_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_operational_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_operational_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_public_seo_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_public_seo_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_prerender_enabled_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_prerender_enabled_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_render_mode_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_render_mode_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_prerender_enabled_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_prerender_enabled_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_render_mode_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_render_mode_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_preset_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_preset_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_prerender_enabled_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_prerender_enabled_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_render_mode_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_render_mode_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_version_local_value",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_version_override_mode",
                table: "tenant_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_group_submitted_events_local_value",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_group_submitted_events_override_mode",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_organization_submitted_events_local_value",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_organization_submitted_events_override_mode",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_user_submitted_events_local_value",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_user_submitted_events_override_mode",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_event_card_click_opens_detail_page_local_value",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_event_card_click_opens_detail_page_override_mode",
                table: "organization_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_custom_css_url_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_custom_css_url_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_display_name_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_display_name_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_favicon_url_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_favicon_url_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_logo_url_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "branding_logo_url_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_allow_tenant_custom_domains_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_allow_tenant_custom_domains_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_instance_base_domain_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_instance_base_domain_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_lock_tenant_custom_domain_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_lock_tenant_custom_domain_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_lock_tenant_subdomain_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "domains_lock_tenant_subdomain_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_group_submitted_events_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_group_submitted_events_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_organization_submitted_events_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_organization_submitted_events_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_user_submitted_events_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_allow_user_submitted_events_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_event_card_click_opens_detail_page_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "events_event_card_click_opens_detail_page_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "modules_enable_islamic_module_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "modules_enable_islamic_module_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "modules_enable_tech_module_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "modules_enable_tech_module_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_group_self_registration_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_group_self_registration_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_self_registration_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_self_registration_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_tenant_to_omit_verification_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_allow_tenant_to_omit_verification_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_require_verification_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "organizations_require_verification_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_prerender_enabled_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_prerender_enabled_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_render_mode_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_admin_render_mode_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_allow_tenant_override_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_allow_tenant_override_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_disallow_interactive_server_on_onboarding_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_disallow_interactive_server_on_onboarding_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_enable_advanced_overrides_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_enable_advanced_overrides_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_prerender_enabled_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_prerender_enabled_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_render_mode_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_global_render_mode_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_admin_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_admin_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_operational_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_operational_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_public_seo_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_lock_tenant_public_seo_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_prerender_enabled_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_prerender_enabled_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_render_mode_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_onboarding_render_mode_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_prerender_enabled_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_prerender_enabled_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_render_mode_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_operational_render_mode_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_preset_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_preset_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_prerender_enabled_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_prerender_enabled_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_render_mode_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_public_seo_render_mode_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_version_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "render_policy_version_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_allow_self_service_registration_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_allow_self_service_registration_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_allow_white_labeling_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_allow_white_labeling_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_authorization_provider_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_authorization_provider_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_decentralization_enabled_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_decentralization_enabled_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_default_public_home_page_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_default_public_home_page_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_lock_tenant_analytics_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_lock_tenant_analytics_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_lock_tenant_smtp_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_lock_tenant_smtp_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_lock_tenant_storage_local_value",
                table: "instance_policy_sets");

            migrationBuilder.DropColumn(
                name: "tenant_delegation_lock_tenant_storage_override_mode",
                table: "instance_policy_sets");

            migrationBuilder.AddColumn<string>(
                name: "branding_policy",
                table: "tenant_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "events_policy",
                table: "tenant_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "organizations_policy",
                table: "tenant_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "render_policy",
                table: "tenant_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "events_policy",
                table: "organization_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "branding_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "domains_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "events_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "modules_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "organizations_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "render_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "tenant_delegation_policy",
                table: "instance_policy_sets",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
