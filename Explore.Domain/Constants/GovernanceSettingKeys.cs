// ABOUTME: Canonical governance setting keys used across onboarding, runtime policy resolution, and admin configuration.
// ABOUTME: Organized as nested static classes for discoverable, compile-time key references.

namespace Explore.Domain.Constants;

public static class GovernanceSettingKeys
{
    public static class Deployment
    {
        public const string Mode = "deployment.mode";
    }

    public static class Tenants
    {
        public const string SelfServiceRegistration = "tenants.self_service_registration";
        public const string WhiteLabelingEnabled = "tenants.white_labeling_enabled";
    }

    public static class Routing
    {
        public const string DefaultPublicHomePage = "routing.default_public_home_page";
        public const string ResolverHeaderEnabled = "routing.resolver_header_enabled";
        public const string ResolverSubdomainEnabled = "routing.resolver_subdomain_enabled";
        public const string ResolverCustomDomainEnabled = "routing.resolver_custom_domain_enabled";
        public const string ResolverPathEnabled = "routing.resolver_path_enabled";
        public const string PathPrefix = "routing.path_prefix";

        public static class RenderPolicy
        {
            private const string Base = "routing.render_policy";

            public const string Version = Base + ".version";
            public const string Preset = Base + ".preset";
            public const string AdvancedEnabled = Base + ".advanced_enabled";
            public const string DisallowInteractiveServerOnOnboarding = Base + ".onboarding.disallow_interactive_server";
            public const string AllowTenantOverride = Base + ".allow_tenant_override";
            public const string LockTenantPublicSeo = Base + ".lock_tenant_public_seo";
            public const string LockTenantOperational = Base + ".lock_tenant_operational";
            public const string LockTenantAdmin = Base + ".lock_tenant_admin";

            public static class Fallback
            {
                private const string SubBase = Base + ".global";

                public const string RenderMode = SubBase + ".render_mode";
                public const string PrerenderEnabled = SubBase + ".prerender_enabled";
            }

            public static class PublicSeo
            {
                private const string SubBase = Base + ".public_seo";

                public const string RenderMode = SubBase + ".render_mode";
                public const string PrerenderEnabled = SubBase + ".prerender_enabled";
            }

            public static class Operational
            {
                private const string SubBase = Base + ".operational";

                public const string RenderMode = SubBase + ".render_mode";
                public const string PrerenderEnabled = SubBase + ".prerender_enabled";
            }

            public static class Admin
            {
                private const string SubBase = Base + ".admin";

                public const string RenderMode = SubBase + ".render_mode";
                public const string PrerenderEnabled = SubBase + ".prerender_enabled";
            }

            public static class Onboarding
            {
                private const string SubBase = Base + ".onboarding";

                public const string RenderMode = SubBase + ".render_mode";
                public const string PrerenderEnabled = SubBase + ".prerender_enabled";
            }
        }
    }

    public static class Events
    {
        public const string UserSubmissionEnabled = "events.user_submission_enabled";
        public const string OrganizationSubmissionEnabled = "events.organization_submission_enabled";
        public const string GroupSubmissionEnabled = "events.group_submission_enabled";
        public const string RequireApproval = "events.require_approval";
        public const string CardClickOpensDetailPage = "events.card_click_opens_detail_page";
    }

    public static class Organizations
    {
        public const string VerificationRequired = "organizations.verification_required";
        public const string TenantCanOmitVerification = "organizations.tenant_can_omit_verification";
        public const string SelfRegistrationEnabled = "organizations.self_registration_enabled";
    }

    public static class Groups
    {
        public const string SelfRegistrationEnabled = "groups.self_registration_enabled";
    }

    public static class Modules
    {
        public const string IslamicEnabled = "modules.islamic_enabled";
        public const string TechEnabled = "modules.tech_enabled";
    }

    public static class Branding
    {
        public const string DisplayName = "branding.display_name";
        public const string LogoUrl = "branding.logo_url";
        public const string FaviconUrl = "branding.favicon_url";
        public const string CustomCssUrl = "branding.custom_css_url";
    }

    public static class Appearance
    {
        public const string DefaultThemeId = "appearance.default_theme_id";
        public const string ThemeMode = "appearance.theme_mode";
    }

    public static class Domains
    {
        public const string InstanceBaseDomain = "domains.instance_base_domain";
        public const string AllowTenantCustomDomain = "domains.allow_tenant_custom_domain";
        public const string TenantSubdomain = "domains.tenant_subdomain";
        public const string TenantCustomDomain = "domains.tenant_custom_domain";
    }

    public static class Email
    {
        public const string SmtpHost = "email.smtp_host";
        public const string SmtpPort = "email.smtp_port";
        public const string SmtpSecurity = "email.smtp_security";
        public const string FromAddress = "email.from_address";
        public const string FromName = "email.from_name";
        public const string SmtpTimeoutSeconds = "email.smtp_timeout_seconds";
        public const string SmtpSkipCertValidation = "email.smtp_skip_cert_validation";
    }

    public static class Storage
    {
        public const string Endpoint = "s3.endpoint";
        public const string PublicEndpoint = "s3.public_endpoint";
        public const string BucketName = "s3.bucket_name";
        public const string Region = "s3.region";
        public const string ForcePathStyle = "s3.force_path_style";
        public const string UploadUrlExpirationMinutes = "s3.upload_url_expiration_minutes";
    }

    public static class Security
    {
        public const string AuthorizationProvider = "authorization.provider";
    }

    public static class Cerbos
    {
        public const string TenantCustomizationEnabled = "cerbos.tenant_customization_enabled";
        public const string Mode = "cerbos.mode";
        public const string CustomEndpoint = "cerbos.custom_endpoint";
        public const string FailureMode = "cerbos.failure_mode";
        public const string CustomAdminEndpoint = "cerbos.custom_admin_endpoint";
    }

    public static class Authentication
    {
        public const string KeycloakEnabled = "auth.keycloak_enabled";
        public const string KeycloakAuthority = "auth.keycloak_authority";
        public const string KeycloakClientId = "auth.keycloak_client_id";
        public const string AtprotoLoginEnabled = "auth.atproto_login_enabled";
        public const string AtprotoPublicUrl = "auth.atproto_public_url";
        public const string GoogleSsoEnabled = "auth.google_sso_enabled";
        public const string GoogleClientId = "auth.google_client_id";
    }

    public static class Federation
    {
        public const string DecentralizationEnabled = "federation.decentralization_enabled";
    }

    public static class Analytics
    {
        public const string Provider = "analytics.provider";
        public const string Enabled = "analytics.enabled";
        public const string ConsentMode = "analytics.consent_mode";
        public const string TransportMode = "analytics.transport_mode";
        public const string ApiKey = "analytics.api_key";
        public const string EndpointUrl = "analytics.endpoint_url";
        public const string PersonalApiKey = "analytics.personal_api_key";

        // Cookie consent & storage governance
        public const string CookieConsentEnabled = "analytics.cookie_consent_enabled";
        public const string DeclineBehavior = "analytics.decline_behavior";
        public const string ConsentCookieLifetimeDays = "analytics.consent_cookie_lifetime_days";
        public const string GlobalDisableClientTracking = "analytics.global_disable_client_tracking";

        // PostHog privacy & feature controls
        public const string PosthogCookielessMode = "analytics.posthog_cookieless_mode";
        public const string PosthogPersonProfiles = "analytics.posthog_person_profiles";
        public const string PosthogSessionReplay = "analytics.posthog_session_replay";
        public const string PosthogAutocapture = "analytics.posthog_autocapture";
        public const string PosthogHeatmaps = "analytics.posthog_heatmaps";
        public const string PosthogToolbar = "analytics.posthog_toolbar";
    }

    public static class TenantDelegation
    {
        public const string LockSmtp = "governance.lock_tenant_smtp";
        public const string LockStorage = "governance.lock_tenant_storage";
        public const string LockAnalytics = "governance.lock_tenant_analytics";
    }

    public static class Policies
    {
        public const string CommunityGuidelinesContent = "policies.community_guidelines_content";
    }

    public static class Localization
    {
        public const string DefaultLanguage = "localization.default_language";
        public const string TmsProvider = "localization.tms_provider";
        public const string TmsApiUrl = "localization.tms_api_url";
        public const string TmsProjectId = "localization.tms_project_id";
        public const string TmsComponent = "localization.tms_component";
    }

    public static class Footer
    {
        public const string Enabled = "footer.enabled";
        public const string Template = "footer.template";
        public const string ShowDescription = "footer.show_description";
        public const string DescriptionText = "footer.description_text";
        public const string ShowSocialLinks = "footer.show_social_links";
        public const string SocialLinks = "footer.social_links";
        public const string CopyrightText = "footer.copyright_text";
        public const string ShowCookieSettingsLink = "footer.show_cookie_settings_link";

        // Instance-level lock flags (prevent tenant override)
        public const string LockTenantTemplate = "footer.lock_tenant_template";
        public const string LockTenantLinkGroups = "footer.lock_tenant_link_groups";
        public const string LockTenantSocialLinks = "footer.lock_tenant_social_links";
        public const string LockTenantDescription = "footer.lock_tenant_description";
        public const string LockTenantCopyright = "footer.lock_tenant_copyright";
    }
}
