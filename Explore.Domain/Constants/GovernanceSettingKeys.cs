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
        public const string ActiveProfileId = "appearance.active_profile_id";
        public const string DefaultPresetId = "appearance.default_preset_id";
        public const string DefaultThemeMode = "appearance.default_theme_mode";
        public const string ThemeMode = "appearance.theme_mode";
        public const string Direction = "appearance.direction";
        public const string Language = "appearance.language";

        [System.Obsolete("Use ActiveProfileId for user scope or DefaultPresetId for tenant/instance scope.")]
        public const string LegacyDefaultThemeId = "appearance.default_theme_id";
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
        public const string Provider = "storage.provider";
        public const string DefaultMaxUploadBytes = "storage.default_max_upload_bytes";
        public const string DefaultTenantQuotaBytes = "storage.default_tenant_quota_bytes";
        public const string InstanceMaxUploadBytes = "storage.instance_max_upload_bytes";
        public const string RouteMatrix = "storage.route_matrix";
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
        public const string GrpcEndpoint = "cerbos.grpc_endpoint";
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

    public static class Mcp
    {
        public const string Enabled = "mcp.enabled";
        public const string EnableLegacySse = "mcp.enable_legacy_sse";
    }

    public static class AiAssistant
    {
        public const string Enabled = "ai_assistant.enabled";
        public const string Provider = "ai_assistant.provider";
        public const string EndpointUrl = "ai_assistant.endpoint_url";
        public const string ApiKey = "ai_assistant.api_key";
        public const string ModelId = "ai_assistant.model_id";
        public const string AllowedModelIds = "ai_assistant.allowed_model_ids";
        public const string MaxInputTokens = "ai_assistant.max_input_tokens";
        public const string MaxOutputTokens = "ai_assistant.max_output_tokens";
        public const string Temperature = "ai_assistant.temperature";
        public const string TimeoutSeconds = "ai_assistant.timeout_seconds";
        public const string RetentionDays = "ai_assistant.retention_days";
        public const string DailyMessageLimit = "ai_assistant.daily_message_limit";
        public const string DailyTenantMessageLimit = "ai_assistant.daily_tenant_message_limit";
        public const string ConcurrentRunLimit = "ai_assistant.concurrent_run_limit";
        public const string SelectedReferenceLimit = "ai_assistant.selected_reference_limit";
        public const string ToolProposalsEnabled = "ai_assistant.tool_proposals_enabled";
        public const string StreamingEnabled = "ai_assistant.streaming_enabled";
        public const string AllowAnonymousAccess = "ai_assistant.allow_anonymous_access";
        public const string MaxAiContextSensitivity = "ai_assistant.max_ai_context_sensitivity";
        public const string PiiDisclosureEnabled = "ai_assistant.pii_disclosure_enabled";
    }

    public static class AiAssistantPreferences
    {
        public const string ShowNavbarButton = "ai_assistant_preferences.show_navbar_button";
    }

    public static class TenantDelegation
    {
        public const string LockSmtp = "governance.lock_tenant_smtp";
        public const string LockStorage = "governance.lock_tenant_storage";
        public const string LockAnalytics = "governance.lock_tenant_analytics";
        public const string LockAiAssistant = "governance.lock_tenant_ai_assistant";
        public const string LockMcp = "governance.lock_tenant_mcp";
        public const string LockMcpLegacySse = "governance.lock_tenant_mcp_legacy_sse";
        public const string LockMessaging = "governance.lock_tenant_messaging";
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
        public const string EnabledLanguages = "localization.enabled_languages";
        public const string FallbackLanguage = "localization.fallback_language";
        public const string ClientPickerEnabled = "localization.client_picker_enabled";
        public const string ForceOfflineMode = "localization.force_offline_mode";
    }

    public static class EventList
    {
        public const string BrowseMode = "event_list.browse_mode";
        public const string PageSize = "event_list.page_size";
        public const string DefaultLayout = "event_list.default_layout";

        public static class Card
        {
            public const string ShowDate = "event_list.card.show_date";
            public const string ShowLocation = "event_list.card.show_location";
            public const string ShowOrganizer = "event_list.card.show_organizer";
            public const string ShowDescription = "event_list.card.show_description";
            public const string ShowTags = "event_list.card.show_tags";
            public const string ShowCategories = "event_list.card.show_categories";
            public const string ShowCapacity = "event_list.card.show_capacity";
            public const string ShowPrice = "event_list.card.show_price";
            public const string ShowStatus = "event_list.card.show_status";
        }
    }

    public static class PublicExperience
    {
        public const string Mode = "public_experience.mode";
        public const string EventCatalogLabel = "public_experience.event_catalog_label";
        public const string PrimaryOrganizationId = "public_experience.primary_organization_id";
        public const string HomeBlocks = "public_experience.home_blocks";
        public const string Ctas = "public_experience.ctas";
        public const string EventSectionPresets = "public_experience.event_section_presets";
        public const string AnnouncementBarEnabled = "public_experience.announcement_bar.enabled";
        public const string AnnouncementBarMessage = "public_experience.announcement_bar.message";
        public const string AnnouncementBarLinkText = "public_experience.announcement_bar.link_text";
        public const string AnnouncementBarLinkUrl = "public_experience.announcement_bar.link_url";
        public const string AnnouncementBarRevision = "public_experience.announcement_bar.revision";
    }

    public static class PublicExperiencePreferences
    {
        public const string AnnouncementBarDismissedRevision = "public_experience_preferences.announcement_bar.dismissed_revision";
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

    public static class Messaging
    {
        public const string Provider = "messaging.provider";
        public const string Enabled = "messaging.enabled";
        public const string HostName = "messaging.host_name";
        public const string Port = "messaging.port";
        public const string UserName = "messaging.user_name";
        public const string Password = "messaging.password";
        public const string VirtualHost = "messaging.virtual_host";
        public const string MaxInboundMessageBodySize = "messaging.max_inbound_message_body_size";
        public const string CircuitBreakerFailureThreshold = "messaging.circuit_breaker_failure_threshold";
        public const string CircuitBreakerBreakDurationSeconds = "messaging.circuit_breaker_break_duration_seconds";
        public const string RetryAttempts = "messaging.retry_attempts";
        public const string EnableOpenTelemetry = "messaging.enable_open_telemetry";
        public const string EnableCompression = "messaging.enable_compression";
    }
}
