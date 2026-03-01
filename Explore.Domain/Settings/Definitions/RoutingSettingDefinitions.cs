// ABOUTME: Setting definitions for routing behavior and render policy configuration.
// ABOUTME: Controls public home page, render modes, and prerendering across page categories.

namespace Explore.Domain.Settings.Definitions;

public static class RoutingSettingDefinitions
{
    public static readonly SettingDefinition DefaultPublicHomePage = new(
        Key: "routing.default_public_home_page",
        ValueType: SettingValueType.String,
        DefaultValue: "\"EventList\"",
        Category: "Routing",
        Description: "Default public home page for tenants",
        MaxScope: SettingScope.Tenant,
        AllowedValues: ["EventList", "LandingPage"]);

    // Render Policy
    public static readonly SettingDefinition RenderPolicyVersion = new(
        Key: "routing.render_policy.version",
        ValueType: SettingValueType.String,
        DefaultValue: "\"1\"",
        Category: "Routing",
        Description: "Render policy schema version");

    public static readonly SettingDefinition RenderPolicyPreset = new(
        Key: "routing.render_policy.preset",
        ValueType: SettingValueType.String,
        DefaultValue: "\"balanced\"",
        Category: "Routing",
        Description: "Render policy preset (balanced, performance, compatibility)");

    public static readonly SettingDefinition RenderPolicyAdvancedEnabled = new(
        Key: "routing.render_policy.advanced_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Routing",
        Description: "Whether advanced render policy configuration is enabled");

    public static readonly SettingDefinition RenderPolicyDisallowInteractiveServerOnOnboarding = new(
        Key: "routing.render_policy.onboarding.disallow_interactive_server",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Routing",
        Description: "Disallow InteractiveServer render mode on onboarding pages");

    // Fallback (Global)
    public static readonly SettingDefinition FallbackRenderMode = new(
        Key: "routing.render_policy.global.render_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"InteractiveAuto\"",
        Category: "Routing",
        Description: "Global fallback render mode");

    public static readonly SettingDefinition FallbackPrerenderEnabled = new(
        Key: "routing.render_policy.global.prerender_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Routing",
        Description: "Global fallback prerender enabled");

    // PublicSeo
    public static readonly SettingDefinition PublicSeoRenderMode = new(
        Key: "routing.render_policy.public_seo.render_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"InteractiveAuto\"",
        Category: "Routing",
        Description: "Public SEO pages render mode");

    public static readonly SettingDefinition PublicSeoPrerenderEnabled = new(
        Key: "routing.render_policy.public_seo.prerender_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Routing",
        Description: "Public SEO pages prerender enabled");

    // Operational
    public static readonly SettingDefinition OperationalRenderMode = new(
        Key: "routing.render_policy.operational.render_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"InteractiveAuto\"",
        Category: "Routing",
        Description: "Operational pages render mode");

    public static readonly SettingDefinition OperationalPrerenderEnabled = new(
        Key: "routing.render_policy.operational.prerender_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Routing",
        Description: "Operational pages prerender enabled");

    // Admin
    public static readonly SettingDefinition AdminRenderMode = new(
        Key: "routing.render_policy.admin.render_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"InteractiveAuto\"",
        Category: "Routing",
        Description: "Admin pages render mode");

    public static readonly SettingDefinition AdminPrerenderEnabled = new(
        Key: "routing.render_policy.admin.prerender_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Routing",
        Description: "Admin pages prerender enabled");

    // Onboarding
    public static readonly SettingDefinition OnboardingRenderMode = new(
        Key: "routing.render_policy.onboarding.render_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"InteractiveAuto\"",
        Category: "Routing",
        Description: "Onboarding pages render mode");

    public static readonly SettingDefinition OnboardingPrerenderEnabled = new(
        Key: "routing.render_policy.onboarding.prerender_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Routing",
        Description: "Onboarding pages prerender enabled");

    public static IReadOnlyList<SettingDefinition> All =>
    [
        DefaultPublicHomePage,
        RenderPolicyVersion, RenderPolicyPreset, RenderPolicyAdvancedEnabled,
        RenderPolicyDisallowInteractiveServerOnOnboarding,
        FallbackRenderMode, FallbackPrerenderEnabled,
        PublicSeoRenderMode, PublicSeoPrerenderEnabled,
        OperationalRenderMode, OperationalPrerenderEnabled,
        AdminRenderMode, AdminPrerenderEnabled,
        OnboardingRenderMode, OnboardingPrerenderEnabled
    ];
}
