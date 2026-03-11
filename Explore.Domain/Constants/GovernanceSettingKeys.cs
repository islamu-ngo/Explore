// ABOUTME: Canonical governance setting keys used across onboarding, runtime policy resolution, and admin configuration.
// ABOUTME: Provides grouped key discovery while preserving legacy flat aliases for backward compatibility.

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

    public static class Localization
    {
        public const string DefaultLanguage = "localization.default_language";
        public const string TmsProvider = "localization.tms_provider";
        public const string TmsApiUrl = "localization.tms_api_url";
        public const string TmsProjectId = "localization.tms_project_id";
        public const string TmsComponent = "localization.tms_component";
    }

    public const string DeploymentMode = Deployment.Mode;
    public const string TenantSelfServiceRegistration = Tenants.SelfServiceRegistration;
    public const string TenantWhiteLabelingEnabled = Tenants.WhiteLabelingEnabled;
    public const string RoutingDefaultPublicHomePage = Routing.DefaultPublicHomePage;
    public const string RoutingResolverHeaderEnabled = Routing.ResolverHeaderEnabled;
    public const string RoutingResolverSubdomainEnabled = Routing.ResolverSubdomainEnabled;
    public const string RoutingResolverCustomDomainEnabled = Routing.ResolverCustomDomainEnabled;
    public const string RoutingResolverPathEnabled = Routing.ResolverPathEnabled;
    public const string RoutingPathPrefix = Routing.PathPrefix;
    public const string RoutingRenderPolicyVersion = Routing.RenderPolicy.Version;
    public const string RoutingRenderPolicyPreset = Routing.RenderPolicy.Preset;
    public const string RoutingRenderPolicyAdvancedEnabled = Routing.RenderPolicy.AdvancedEnabled;
    public const string RoutingRenderPolicyGlobalRenderMode = Routing.RenderPolicy.Fallback.RenderMode;
    public const string RoutingRenderPolicyGlobalPrerenderEnabled = Routing.RenderPolicy.Fallback.PrerenderEnabled;
    public const string RoutingRenderPolicyPublicSeoRenderMode = Routing.RenderPolicy.PublicSeo.RenderMode;
    public const string RoutingRenderPolicyPublicSeoPrerenderEnabled = Routing.RenderPolicy.PublicSeo.PrerenderEnabled;
    public const string RoutingRenderPolicyOperationalRenderMode = Routing.RenderPolicy.Operational.RenderMode;
    public const string RoutingRenderPolicyOperationalPrerenderEnabled = Routing.RenderPolicy.Operational.PrerenderEnabled;
    public const string RoutingRenderPolicyAdminRenderMode = Routing.RenderPolicy.Admin.RenderMode;
    public const string RoutingRenderPolicyAdminPrerenderEnabled = Routing.RenderPolicy.Admin.PrerenderEnabled;
    public const string RoutingRenderPolicyOnboardingRenderMode = Routing.RenderPolicy.Onboarding.RenderMode;
    public const string RoutingRenderPolicyOnboardingPrerenderEnabled = Routing.RenderPolicy.Onboarding.PrerenderEnabled;
    public const string RoutingRenderPolicyDisallowInteractiveServerOnOnboarding = Routing.RenderPolicy.DisallowInteractiveServerOnOnboarding;
    public const string RoutingRenderPolicyAllowTenantOverride = Routing.RenderPolicy.AllowTenantOverride;
    public const string RoutingRenderPolicyLockTenantPublicSeo = Routing.RenderPolicy.LockTenantPublicSeo;
    public const string RoutingRenderPolicyLockTenantOperational = Routing.RenderPolicy.LockTenantOperational;
    public const string RoutingRenderPolicyLockTenantAdmin = Routing.RenderPolicy.LockTenantAdmin;
    public const string EventsUserSubmissionEnabled = Events.UserSubmissionEnabled;
    public const string EventsRequireApproval = Events.RequireApproval;
    public const string EventsCardClickOpensDetailPage = Events.CardClickOpensDetailPage;
    public const string EventsOrganizationSubmissionEnabled = Events.OrganizationSubmissionEnabled;
    public const string EventsGroupSubmissionEnabled = Events.GroupSubmissionEnabled;
    public const string OrganizationsVerificationRequired = Organizations.VerificationRequired;
    public const string OrganizationsTenantCanOmitVerification = Organizations.TenantCanOmitVerification;
    public const string OrganizationsSelfRegistrationEnabled = Organizations.SelfRegistrationEnabled;
    public const string GroupsSelfRegistrationEnabled = Groups.SelfRegistrationEnabled;
    public const string ModulesIslamicEnabled = Modules.IslamicEnabled;
    public const string ModulesTechEnabled = Modules.TechEnabled;
    public const string BrandingDisplayName = Branding.DisplayName;
    public const string BrandingLogoUrl = Branding.LogoUrl;
    public const string BrandingFaviconUrl = Branding.FaviconUrl;
    public const string BrandingCustomCssUrl = Branding.CustomCssUrl;
    public const string DomainsInstanceBaseDomain = Domains.InstanceBaseDomain;
    public const string DomainsAllowTenantCustomDomain = Domains.AllowTenantCustomDomain;
    public const string DomainsTenantSubdomain = Domains.TenantSubdomain;
    public const string DomainsTenantCustomDomain = Domains.TenantCustomDomain;

    public const string EmailSmtpHost = Email.SmtpHost;
    public const string EmailSmtpPort = Email.SmtpPort;
    public const string EmailSmtpUsername = InfrastructureSecretSettingKeys.Email.SmtpUsername;
    public const string EmailSmtpPassword = InfrastructureSecretSettingKeys.Email.SmtpPassword;
    public const string EmailSmtpSecurity = Email.SmtpSecurity;
    public const string EmailFromAddress = Email.FromAddress;
    public const string EmailFromName = Email.FromName;
    public const string EmailSmtpTimeoutSeconds = Email.SmtpTimeoutSeconds;
    public const string EmailSmtpSkipCertValidation = Email.SmtpSkipCertValidation;

    public const string S3Endpoint = Storage.Endpoint;
    public const string S3PublicEndpoint = Storage.PublicEndpoint;
    public const string S3BucketName = Storage.BucketName;
    public const string S3AccessKeyId = InfrastructureSecretSettingKeys.Storage.AccessKeyId;
    public const string S3SecretAccessKey = InfrastructureSecretSettingKeys.Storage.SecretAccessKey;
    public const string S3Region = Storage.Region;
    public const string S3ForcePathStyle = Storage.ForcePathStyle;
    public const string S3UploadUrlExpirationMinutes = Storage.UploadUrlExpirationMinutes;

    public const string AuthorizationProvider = Security.AuthorizationProvider;

    public const string CerbosTenantCustomizationEnabled = Cerbos.TenantCustomizationEnabled;
    public const string CerbosMode = Cerbos.Mode;
    public const string CerbosCustomEndpoint = Cerbos.CustomEndpoint;
    public const string CerbosFailureMode = Cerbos.FailureMode;
    public const string CerbosCustomAdminEndpoint = Cerbos.CustomAdminEndpoint;

    public const string AnalyticsProvider = Analytics.Provider;
    public const string AnalyticsEnabled = Analytics.Enabled;
    public const string AnalyticsConsentMode = Analytics.ConsentMode;
    public const string AnalyticsTransportMode = Analytics.TransportMode;
    public const string AnalyticsApiKey = Analytics.ApiKey;
    public const string AnalyticsEndpointUrl = Analytics.EndpointUrl;
    public const string AnalyticsPersonalApiKey = Analytics.PersonalApiKey;

    public const string AuthKeycloakEnabled = Authentication.KeycloakEnabled;
    public const string AuthKeycloakAuthority = Authentication.KeycloakAuthority;
    public const string AuthKeycloakClientId = Authentication.KeycloakClientId;
    public const string AuthKeycloakClientSecret = InfrastructureSecretSettingKeys.Authentication.KeycloakClientSecret;
    public const string AuthAtprotoLoginEnabled = Authentication.AtprotoLoginEnabled;
    public const string AuthAtprotoPublicUrl = Authentication.AtprotoPublicUrl;
    public const string AuthGoogleSsoEnabled = Authentication.GoogleSsoEnabled;
    public const string AuthGoogleClientId = Authentication.GoogleClientId;
    public const string AuthGoogleClientSecret = InfrastructureSecretSettingKeys.Authentication.GoogleClientSecret;
    public const string FederationDecentralizationEnabled = Federation.DecentralizationEnabled;

    public const string LocalizationDefaultLanguage = Localization.DefaultLanguage;
    public const string LocalizationTmsProvider = Localization.TmsProvider;
    public const string LocalizationTmsApiUrl = Localization.TmsApiUrl;
    public const string LocalizationTmsProjectId = Localization.TmsProjectId;
    public const string LocalizationTmsComponent = Localization.TmsComponent;
}
