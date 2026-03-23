// ABOUTME: Application layer service registration for DI container.
// ABOUTME: Registers MediatR, AutoMapper, pipeline behaviors, and application services.
using System.Reflection;
using Explore.Application.Analytics;
using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Application.Settings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Application;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddMediatR(typeof(ApplicationServicesRegistration).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

        // Onboarding Services
        services.AddScoped<ITenantPolicySettingService, TenantPolicySettingService>();
        services.AddScoped<IInstanceGovernanceSettingService, InstanceGovernanceSettingService>();
        services.AddScoped<IInstanceStorageSettingService, InstanceStorageSettingService>();
        services.AddScoped<IInstanceSmtpSettingService, InstanceSmtpSettingService>();
        services.AddScoped<IAuthProviderConfigurationService, AuthProviderConfigurationService>();
        services.AddScoped<IAnalyticsGovernanceService, AnalyticsGovernanceService>();
        services.AddScoped<IModuleCapabilityService, ModuleCapabilityService>();
        services.AddScoped<SettingUpsertService>();

        // Analytics consent / runtime profile resolution
        services.AddScoped<IAnalyticsRuntimeProfileResolver, AnalyticsRuntimeProfileResolver>();

        // Authorization: dynamic permission infrastructure
        services.AddScoped<ICapabilityCeilingService, CapabilityCeilingService>();
        services.AddScoped<ICustomPropertyGovernancePolicy, CustomPropertyGovernancePolicy>();
        services.AddScoped<IPermissionRegistryService, PermissionRegistryService>();
        services.AddScoped<IContactShareConsentService, ContactShareConsentService>();

        return services;
    }
}
