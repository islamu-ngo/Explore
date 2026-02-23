// ABOUTME: Centralizes all scoped service registrations shared between Server and WASM.
// ABOUTME: Server-only services (CircuitAccessTokenService, etc.) are registered separately.

using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Contracts;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;

namespace Explore.Blazor.Extensions;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers all application-level scoped services that are shared between Server and WASM modes.
    /// These services use the NSwag-generated IEventApiClient or named HttpClient "BffClient".
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationReviewService, OrganizationReviewService>();
        services.AddScoped<IMapsService, MapsService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IEventRegistrationService, EventRegistrationService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IEventAspectService, EventAspectService>();
        services.AddScoped<IAudienceAgeService, AudienceAgeService>();
        services.AddScoped<IAudienceGenderService, AudienceGenderService>();
        services.AddScoped<IEventFormatService, EventFormatService>();
        services.AddScoped<IEventStatusService, EventStatusService>();
        services.AddScoped<IEventTypeService, EventTypeService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IMadhabService, MadhabService>();
        services.AddScoped<IEventSessionSpeakerService, EventSessionSpeakerService>();
        services.AddScoped<IActorService, ActorService>();
        services.AddScoped<ILookupCacheService, LookupCacheService>();
        services.AddScoped<IInstanceOnboardingService, InstanceOnboardingService>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IPublicExperienceService, PublicExperienceService>();
        services.AddScoped<IRuntimeRenderPolicyService, RuntimeRenderPolicyService>();
        services.AddScoped<IStartupRoutingService, StartupRoutingService>();
        services.AddScoped<IEventCreationEligibilityService, EventCreationEligibilityService>();

        return services;
    }

    /// <summary>
    /// Registers services that are only needed on the Blazor Server (BFF) side.
    /// These include token management, analytics no-ops, and admin claims enrichment.
    /// </summary>
    public static IServiceCollection AddServerOnlyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAnalyticsInterop, ServerAnalyticsInterop>();
        services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
        services.AddSingleton<ISetupSecretSessionService, SetupSecretSessionService>();
        services.AddScoped<IAuthStateService, AuthStateService>();

        // BFF admin claims transformation — calls the API to resolve admin authority
        services.AddSingleton<BffAdminClaimsTransformation>();
        services.AddSingleton<IClaimsTransformation>(
            sp => sp.GetRequiredService<BffAdminClaimsTransformation>());

        // Multi-tenancy configuration
        services.Configure<TenantConfiguration>(
            configuration.GetSection("Explore:MultiTenancy"));

        return services;
    }
}
