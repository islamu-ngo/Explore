// ABOUTME: Shared service registrations used by both Blazor Server (BFF) and WASM host.
// ABOUTME: Eliminates duplication between server Program.cs and client Program.cs (DRY).

using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application services shared between Blazor Server and WebAssembly hosts.
    /// Services that need different implementations per host (e.g., IAnalyticsInterop)
    /// must be registered separately by each host.
    /// </summary>
    public static IServiceCollection AddSharedApplicationServices(this IServiceCollection services)
    {
        // Domain services (NSwag IEventApiClient consumers)
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationReviewService, OrganizationReviewService>();
        services.AddScoped<IMapsService, MapsService>();
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
        services.AddScoped<ILookupCacheService, LookupCacheService>();

        // Event-specific services
        services.AddScoped<IEventRegistrationService, EventRegistrationService>();
        services.AddScoped<IEventSessionSpeakerService, EventSessionSpeakerService>();
        services.AddScoped<IActorService, ActorService>();
        services.AddScoped<IEventCreationEligibilityService, EventCreationEligibilityService>();

        // BFF / onboarding services (use named HttpClient "BffClient")
        services.AddScoped<IInstanceOnboardingService, InstanceOnboardingService>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IPublicExperienceService, PublicExperienceService>();

        // Runtime render policy and routing
        services.AddScoped<IRuntimeRenderPolicyService, RuntimeRenderPolicyService>();
        services.AddScoped<IStartupRoutingService, StartupRoutingService>();

        // Auth state
        services.AddScoped<IAuthStateService, AuthStateService>();

        return services;
    }
}
