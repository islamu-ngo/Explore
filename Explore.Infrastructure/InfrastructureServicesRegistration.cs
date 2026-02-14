using Amazon;
using Amazon.S3;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Strategies;
using Explore.Application.Models;
using Explore.Infrastructure.Identity;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Services;
using Explore.Infrastructure.Services.Federation;
using Explore.Infrastructure.Storage;
using Explore.Infrastructure.Strategies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Explore.Infrastructure;

public static class InfrastructureServicesRegistration
{
    public static IServiceCollection ConfigureInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Email service: provider-agnostic SMTP via MailKit
        // Config resolved per-tenant from cascading settings engine (SystemSetting → TenantSetting)
        // Instance admin can lock settings to enforce SaaS-wide SMTP or let tenants override
        services.AddScoped<ISmtpConfigResolver, SmtpConfigResolver>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Object storage: provider-agnostic S3-compatible via AWS SDK
        // Config resolved per-tenant from cascading settings engine (SystemSetting → TenantSetting)
        // Instance admin can lock settings to enforce SaaS-wide storage or let tenants override
        services.AddScoped<IS3ConfigResolver, S3ConfigResolver>();
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        // Identity services
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Memory cache for settings and module governance
        services.AddMemoryCache();

        // Settings and Module Governance services
        services.AddScoped<ISettingsResolver, SettingsResolver>();
        services.AddScoped<IModuleService, ModuleService>();

        // Admin context (hybrid JWT + database identity resolution)
        services.AddScoped<AdminContext>();
        services.AddScoped<IAdminContext>(sp => sp.GetRequiredService<AdminContext>());
        services.AddScoped<IAdminCacheInvalidator>(sp => sp.GetRequiredService<AdminContext>());

        // Claims transformation: enriches ClaimsPrincipal with DB-resolved admin authority.
        // Claims are serialized to Blazor WASM via AddAuthenticationStateSerialization.
        services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

        // Configuration audit logging
        services.AddScoped<IConfigurationChangeLogService, ConfigurationChangeLogService>();

        // Authorization providers (runtime-switchable via SystemSetting "authorization.provider")
        // Both concrete providers are always registered; RuntimeAuthorizationProvider delegates at runtime.
        services.Configure<CerbosSettings>(configuration.GetSection(CerbosSettings.SectionName));
        services.Configure<CerbosAdminApiSettings>(configuration.GetSection(CerbosAdminApiSettings.SectionName));

        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("CerbosClient", client =>
        {
            var endpoint = configuration["Cerbos:Endpoint"] ?? "http://localhost:3592";
            client.BaseAddress = new Uri(endpoint);
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddResilienceHandler("cerbos-resilience", pipeline =>
        {
            // Timeout: 2s hard limit for authorization checks
            pipeline.AddTimeout(TimeSpan.FromSeconds(2));

            // Circuit breaker: trip after 50% failure rate, break for 15s
            // No retry — fail-fast to LocalAuthorizationProvider is safer than retrying auth checks
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(15)
            });
        });
        services.AddHttpClient("CerbosAdminClient");
        services.AddScoped<CerbosPrincipalBuilder>();
        services.AddScoped<CerbosAuthorizationService>();
        services.AddScoped<FallbackAuthorizationService>();
        services.AddScoped<IAuthorizationProvider, RuntimeAuthorizationProvider>();
        services.AddScoped<IPolicySyncService, PolicySyncService>();

        // Event Strategies
        services.AddScoped<IEventStrategy, IslamicEventStrategy>();
        services.AddScoped<IEventStrategy, TechEventStrategy>();
        services.AddScoped<IStrategyResolver, StrategyResolver>();

        // PDS Synchronization services
        services.Configure<PdsSyncSettings>(configuration.GetSection(PdsSyncSettings.SectionName));
        services.AddHttpClient("PdsService");
        services.AddScoped<IPdsService, PdsService>();

        // Deployment mode configuration (single-tenant vs multi-tenant)
        services.Configure<DeploymentSettings>(configuration.GetSection(DeploymentSettings.SectionName));

        // Setup secret provider: singleton that manages the bootstrap setup secret lifecycle.
        // Must be singleton because the secret is resolved once at startup and locked after onboarding completion.
        services.AddSingleton<ISetupSecretProvider, SetupSecretProvider>();

        return services;
    }
}
