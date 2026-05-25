// ABOUTME: Shared service registrations used by both Blazor Server (BFF) and WASM host.
// ABOUTME: Eliminates duplication between server Program.cs and client Program.cs (DRY).

using Explore.Blazor.Client.Contracts.Providers;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.CustomProperties;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Lookup;
using Explore.Blazor.Client.Contracts.Services.Notifications;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Accessibility;
using Explore.Blazor.Client.Services.Docking;
using Explore.Blazor.Client.Services.Http;
using Explore.Blazor.Client.Services.Lookup;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application services shared between Blazor Server and WebAssembly hosts.
    /// Services that need different implementations per host (e.g., IAnalyticsInterop)
    /// must be registered separately by each host.
    /// </summary>
    public static IServiceCollection AddSharedApplicationServices(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureBffRefitClient = null,
        Action<IHttpClientBuilder>? configureBffRefitClientBuilder = null)
    {
        // Domain services (NSwag IEventApiClient consumers)
        services.AddScoped<IApiClientExecutor, ApiClientExecutor>();
        services.AddScoped<IExternalApiKeyService, ExternalApiKeyService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ICustomPropertyAdminService, CustomPropertyAdminService>();
        services.AddScoped<ICustomPropertyDefinitionService, CustomPropertyDefinitionService>();
        services.AddScoped<ICustomPropertyValueService, CustomPropertyValueService>();
        services.AddScoped<Explore.Blazor.Client.Contracts.Services.EventTemplates.IEventTemplateService, EventTemplateService>();
        services.AddScoped<Explore.Blazor.Client.Contracts.Services.EventSessionTemplates.IEventSessionTemplateService, EventSessionTemplateService>();
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationReviewService, OrganizationReviewService>();
        services.AddBffRefitClient<IMapsApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddScoped<IMapsService, MapsService>();
        services.AddScoped<IImageContentClassifier, ImageContentClassifier>();
        services.AddScoped<IImageFileReaderService, ImageFileReaderService>();
        services.AddScoped<IImagePreviewService, ImagePreviewService>();
        services.AddScoped<IImageUploadClient, ImageUploadClient>();
        services.AddScoped<IImageStorageRecordClient, ImageStorageRecordClient>();
        services.AddScoped<IImageStorageService, ImageStorageService>();

        // Lookup / reference data services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IAudienceAgeService, AudienceAgeService>();
        services.AddScoped<IAudienceGenderService, AudienceGenderService>();
        services.AddScoped<IEventFormatService, EventFormatService>();
        services.AddScoped<IEventStatusService, EventStatusService>();
        services.AddScoped<IEventTypeService, EventTypeService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IMadhabService, MadhabService>();
        services.AddScoped<IEventAspectService, EventAspectService>();
        services.AddScoped<IScheduleItemKindService, ScheduleItemKindService>();
        services.AddScoped<IRegistrationScopeService, RegistrationScopeService>();
        services.AddScoped<IEventRegistrationPolicyService, EventRegistrationPolicyService>();
        services.AddScoped<ILookupCacheService, LookupCacheService>();

        // Event-specific services
        services.AddScoped<IEventRegistrationService, EventRegistrationService>();
        services.AddScoped<IEventSessionLanguageService, EventSessionLanguageService>();
        services.AddScoped<IEventSessionSpeakerService, EventSessionSpeakerService>();
        services.AddScoped<IEventSessionAgendaItemService, EventSessionAgendaItemService>();
        services.AddScoped<IEventDayService, EventDayService>();
        services.AddScoped<IEventAgendaItemService, EventAgendaItemService>();
        services.AddScoped<ILocationRoomService, LocationRoomService>();
        services.AddScoped<IActorService, ActorService>();
        services.AddScoped<IEventCreationEligibilityService, EventCreationEligibilityService>();
        services.AddScoped<IContactShareConsentService, ContactShareConsentService>();

        // Notification services
        services.AddScoped<INotificationService, NotificationService>();

        // BFF / onboarding services (use named HttpClient "BffClient")
        services.AddBffRefitClient<IInstanceOnboardingApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddBffRefitClient<ITenantOnboardingApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddBffRefitClient<IPublicExperienceApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddScoped<IInstanceOnboardingService, InstanceOnboardingService>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IPublicExperienceService, PublicExperienceService>();
        services.AddScoped<ITenantPublicExperienceAdminService, TenantPublicExperienceAdminService>();
        services.AddBffRefitClient<ITenantBrandingSettingsApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddScoped<ITenantBrandingSettingsAdminService, TenantBrandingSettingsAdminService>();
        services.AddScoped<IAppearanceThemeService, AppearanceThemeService>();
        services.AddScoped<IUserAppearancePreferencesService, UserAppearancePreferencesService>();

        // Runtime render policy and routing
        services.AddScoped<IRuntimeRenderPolicyService, RuntimeRenderPolicyService>();
        services.AddScoped<IStartupRoutingService, StartupRoutingService>();

        // Auth state
        services.AddScoped<IAuthStateService, AuthStateService>();

        // Localization
        services.AddScoped<ITranslationService, TranslationService>();
        services.AddBffRefitClient<ILanguagePreferenceApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddScoped<ILanguagePreferenceService, LanguagePreferenceService>();
        services.AddTransient<MudBlazor.MudLocalizer, MudBlazorLocalizer>();

        // UI state
        services.AddScoped<SidebarState>();
        services.AddScoped<MainContentAppearanceState>();
        services.AddScoped<AiAssistantState>();
        services.AddScoped<TenantNavLinksState>();
        services.AddScoped<DockLayoutState>();
        services.AddScoped<IDockPanelRegistry>(provider => provider.GetRequiredService<DockLayoutState>());
        services.AddScoped<IDockLayoutPersistence, Explore.Blazor.Client.Services.Interop.LocalStorageDockLayoutPersistence>();

        // User-scoped settings (auth-branching: API for authenticated, localStorage for anonymous)
        services.AddScoped<IUserSettingsService, UserSettingsService>();

        // Accessibility services (ARIA announcements + focus management)
        services.AddScoped<IAccessibilityAnnouncerService, AccessibilityAnnouncerService>();
        services.AddScoped<IAccessibilityFocusService, AccessibilityFocusService>();

        // Feature flags (hydrated from API, no OpenFeature SDK dependency)
        services.AddScoped<FeatureStateContainer>();
        services.AddBffRefitClient<IFeatureFlagApi>(configureBffRefitClient)
            .ConfigureBffRefitClient(configureBffRefitClientBuilder);
        services.AddScoped<IFeatureFlagClientService, FeatureFlagClientService>();

        return services;
    }


    private static IHttpClientBuilder ConfigureBffRefitClient(
        this IHttpClientBuilder builder,
        Action<IHttpClientBuilder>? configure)
    {
        configure?.Invoke(builder);
        return builder;
    }
}
