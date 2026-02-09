// ABOUTME: Canonical keys for system governance settings used by onboarding and admin flows.
// ABOUTME: Prevents string duplication across API, Application, Infrastructure, and UI layers.

namespace Explore.Domain.Constants;

public static class GovernanceSettingKeys
{
    public const string DeploymentMode = "deployment.mode";
    public const string TenantSelfServiceRegistration = "tenants.self_service_registration";
    public const string RoutingDefaultPublicHomePage = "routing.default_public_home_page";
    public const string EventsUserSubmissionEnabled = "events.user_submission_enabled";
    public const string EventsRequireApproval = "events.require_approval";
    public const string OrganizationsVerificationRequired = "organizations.verification_required";
    public const string OrganizationsTenantCanOmitVerification = "organizations.tenant_can_omit_verification";
    public const string ModulesIslamicEnabled = "modules.islamic_enabled";
    public const string ModulesTechEnabled = "modules.tech_enabled";
    public const string BrandingDisplayName = "branding.display_name";
    public const string BrandingLogoUrl = "branding.logo_url";
    public const string BrandingFaviconUrl = "branding.favicon_url";
    public const string BrandingCustomCssUrl = "branding.custom_css_url";
    public const string DomainsInstanceBaseDomain = "domains.instance_base_domain";
    public const string DomainsAllowTenantCustomDomain = "domains.allow_tenant_custom_domain";
    public const string DomainsTenantSubdomain = "domains.tenant_subdomain";
    public const string DomainsTenantCustomDomain = "domains.tenant_custom_domain";
}
