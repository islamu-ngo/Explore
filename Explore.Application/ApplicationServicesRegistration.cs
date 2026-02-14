using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using FluentValidation;
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
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Onboarding Services
        services.AddScoped<ITenantPolicySettingService, TenantPolicySettingService>();
        services.AddScoped<IInstanceGovernanceSettingService, InstanceGovernanceSettingService>();
        services.AddScoped<IInstanceStorageSettingService, InstanceStorageSettingService>();

        // Authorization: dynamic permission infrastructure
        services.AddScoped<ICapabilityCeilingService, CapabilityCeilingService>();
        services.AddScoped<IPermissionRegistryService, PermissionRegistryService>();

        return services;
    }
}
