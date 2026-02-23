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

        public static class RenderPolicy
        {
            private const string Base = "routing.render_policy";

            public const string Version = Base + ".version";
            public const string Preset = Base + ".preset";
            public const string AdvancedEnabled = Base + ".advanced_enabled";
            public const string DisallowInteractiveServerOnOnboarding = Base + ".onboarding.disallow_interactive_server";

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
        public const string RequireApproval = "events.require_approval";
    }

    public static class Organizations
    {
        public const string VerificationRequired = "organizations.verification_required";
        public const string TenantCanOmitVerification = "organizations.tenant_can_omit_verification";
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

    public static class Analytics
    {
        public const string Provider = "analytics.provider";
        public const string Enabled = "analytics.enabled";
        public const string ApiKey = "analytics.api_key";
        public const string EndpointUrl = "analytics.endpoint_url";
        public const string PersonalApiKey = "analytics.personal_api_key";
    }

    public const string DeploymentMode = Deployment.Mode;
    public const string TenantSelfServiceRegistration = Tenants.SelfServiceRegistration;
    public const string TenantWhiteLabelingEnabled = Tenants.WhiteLabelingEnabled;
    public const string RoutingDefaultPublicHomePage = Routing.DefaultPublicHomePage;
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
    public const string EventsUserSubmissionEnabled = Events.UserSubmissionEnabled;
    public const string EventsRequireApproval = Events.RequireApproval;
    public const string OrganizationsVerificationRequired = Organizations.VerificationRequired;
    public const string OrganizationsTenantCanOmitVerification = Organizations.TenantCanOmitVerification;
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

    public const string AnalyticsProvider = Analytics.Provider;
    public const string AnalyticsEnabled = Analytics.Enabled;
    public const string AnalyticsApiKey = Analytics.ApiKey;
    public const string AnalyticsEndpointUrl = Analytics.EndpointUrl;
    public const string AnalyticsPersonalApiKey = Analytics.PersonalApiKey;
}
