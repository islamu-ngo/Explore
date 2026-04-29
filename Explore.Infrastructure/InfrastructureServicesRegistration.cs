using Amazon;
using Amazon.S3;
using Cerbos.Sdk;
using Cerbos.Sdk.Builder;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Strategies;
using Explore.Application.Models;
using Explore.Application.Utilities;
using Explore.Infrastructure.Analytics;
using Explore.Infrastructure.Identity;
using Explore.Infrastructure.Localization;
using Explore.Infrastructure.Localization.Resilience;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Mail.Unsubscribe;
using Explore.Infrastructure.Messaging;
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
        services.AddScoped<IEmailUnsubscribeTokenService, EmailUnsubscribeTokenService>();

        // Object storage: provider-agnostic S3-compatible via AWS SDK
        // Config resolved per-tenant from cascading settings engine (SystemSetting → TenantSetting)
        // Instance admin can lock settings to enforce SaaS-wide storage or let tenants override
        services.AddScoped<IS3ConfigResolver, S3ConfigResolver>();
        services.AddScoped<IObjectStorageService, ObjectStorageService>();

        // Identity services
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPublicUrlBuilder, PublicUrlBuilder>();

        // Memory cache for settings and module governance
        services.AddMemoryCache();

        // Distributed cache: default in-memory fallback.
        // Production overrides this with Redis via Aspire in Program.cs.
        services.AddDistributedMemoryCache();

        // Settings and Module Governance services
        services.AddScoped<IHierarchicalSettingsResolver, HierarchicalSettingsResolver>();
        services.AddScoped<IResolverConfigService, ResolverConfigService>();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddSingleton<ITenantSlugCache, TenantSlugCache>();
        services.AddScoped<IModuleService, ModuleService>();

        // Admin context (hybrid JWT + database identity resolution)
        services.AddScoped<AdminContext>();
        services.AddScoped<IAdminContext>(sp => sp.GetRequiredService<AdminContext>());
        services.AddScoped<IAdminCacheInvalidator>(sp => sp.GetRequiredService<AdminContext>());

        // Machine principal accessor: reads API-key-derived principal context from the current HttpContext.
        // Required so authorization providers (Cerbos + fallback) treat external API-key callers consistently with human users.
        services.AddScoped<IMachinePrincipalAccessor, MachinePrincipalAccessor>();

        // Claims transformation: enriches ClaimsPrincipal with DB-resolved admin authority.
        // Claims are serialized to Blazor WASM via AddAuthenticationStateSerialization.
        services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

        // Configuration audit logging
        services.AddScoped<IConfigurationChangeLogService, ConfigurationChangeLogService>();
        services.AddScoped<IAuthorizationProviderConfigurationService, AuthorizationProviderConfigurationService>();

        // Authorization providers (runtime-switchable via SystemSetting "authorization.provider")
        // Both concrete providers are always registered; RuntimeAuthorizationProvider delegates at runtime.
        services.Configure<CerbosSettings>(configuration.GetSection(CerbosSettings.SectionName));
        services.Configure<CerbosAdminApiSettings>(configuration.GetSection(CerbosAdminApiSettings.SectionName));

        // Cerbos gRPC SDK client (singleton — gRPC channels are long-lived and thread-safe)
        services.AddSingleton<ICerbosClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CerbosSettings>>().Value;
            var grpcEndpoint = GrpcEndpointNormalizer.Normalize(settings.GrpcEndpoint);
            var builder = CerbosClientBuilder
                .ForTarget(grpcEndpoint)
                .WithGrpcChannelOptions(CerbosGrpcChannelOptionsFactory.Create());

            if (grpcEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                builder = builder.WithPlaintext();

            return builder.Build();
        });

        // Client factory for BYO (Bring Your Own) Cerbos endpoints — each tenant may have its own PDP
        services.AddSingleton<ICerbosClientFactory, CerbosClientFactory>();

        // Admin API client for PolicySyncService (HTTP-based, separate from gRPC runtime)
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("CerbosAdminClient");

        services.AddScoped<CerbosPrincipalBuilder>();
        services.AddScoped<CerbosAuthorizationService>();
        services.AddScoped<FallbackAuthorizationService>();
        services.AddScoped<ICerbosConfigResolver, CerbosConfigResolver>();
        services.AddScoped<IAuthorizationProvider, RuntimeAuthorizationProvider>();
        services.AddScoped<IPolicySyncService, PolicySyncService>();

        // Event Strategies
        services.AddScoped<IEventStrategy, IslamicEventStrategy>();
        services.AddScoped<IEventStrategy, TechEventStrategy>();
        services.AddScoped<IStrategyResolver, StrategyResolver>();

        // Analytics providers (runtime-switchable via SystemSetting "analytics.provider")
        // All concrete providers are always registered; RuntimeAnalyticsProvider delegates at runtime.
        // Config resolved per-tenant from cascading settings engine (SystemSetting -> TenantSetting)
        services.AddHttpClient<PostHogAnalyticsProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient<PlausibleAnalyticsProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient<RybbitAnalyticsProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient<RudderStackAnalyticsProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<NullAnalyticsProvider>();
        services.AddScoped<IAnalyticsConfigResolver, AnalyticsConfigResolver>();
        services.AddScoped<RuntimeAnalyticsProvider>();
        services.AddScoped<IAnalyticsProvider>(sp => sp.GetRequiredService<RuntimeAnalyticsProvider>());
        services.AddScoped<IAnalyticsFeatureFlagProvider>(sp => sp.GetRequiredService<RuntimeAnalyticsProvider>());

        // Translation Management System providers (runtime-switchable via GovernanceSettings "localization.tms_provider")
        // All concrete providers are always registered; RuntimeTranslationProvider delegates at runtime.
        // None → OfflineTranslationProvider (bundled .json files), Tolgee → TolgeeTranslationProvider, Weblate → WeblateTranslationProvider
        services.AddHttpClient<TolgeeTranslationProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddResilienceHandler("tolgee-pipeline", builder => TmsResiliencePipelineConfigurator.Configure(builder,
            async args =>
            {
                if (args.Outcome.Result is { } response)
                {
                    var delay = await TolgeeRetryAfterReader.ReadDelayAsync(response, args.Context.CancellationToken);
                    if (delay is not null) return delay.Value;
                }
                return null;
            }));
        services.AddHttpClient<WeblateTranslationProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddResilienceHandler("weblate-pipeline", builder => TmsResiliencePipelineConfigurator.Configure(builder,
            args =>
            {
                if (args.Outcome.Result is { } response)
                {
                    var delay = WeblateRateLimitReader.ReadDelay(response);
                    if (delay is not null) return ValueTask.FromResult<TimeSpan?>(delay.Value);
                }
                return ValueTask.FromResult<TimeSpan?>(null);
            }));
        services.AddSingleton<OfflineTranslationProvider>();
        services.AddScoped<NullTranslationProvider>();
        services.AddScoped<ITranslationConfigResolver, TranslationConfigResolver>();
        services.AddScoped<RuntimeTranslationProvider>();
        services.AddScoped<ITranslationManagementProvider>(sp => sp.GetRequiredService<RuntimeTranslationProvider>());
        services.AddScoped<TranslationResolver>();
        services.AddScoped<ITranslationResolver>(sp => sp.GetRequiredService<TranslationResolver>());
        services.AddScoped<IBundleFileWriter, BundleFileWriter>();

        // Messaging providers (runtime-switchable via GovernanceSettings "messaging.provider")
        // All concrete providers are always registered; RuntimeMessagingProvider delegates at runtime.
        // Config resolved per-tenant from cascading settings engine (Instance admin → Tenant admin)
        services.AddSingleton<RabbitMqMessagingProvider>();
        services.AddScoped<NullMessagingProvider>();
        services.AddScoped<IMessagingConfigResolver, MessagingConfigResolver>();
        services.AddScoped<RuntimeMessagingProvider>();
        services.AddScoped<IMessagingProvider>(sp => sp.GetRequiredService<RuntimeMessagingProvider>());

        // Generic Outbox Processor settings and dispatcher
        services.Configure<OutboxProcessorSettings>(configuration.GetSection(OutboxProcessorSettings.SectionName));
        services.AddScoped<IOutboxMessageDispatcher, MqContractOutboxMessageDispatcher>();

        // PDS Synchronization services
        services.Configure<PdsSyncSettings>(configuration.GetSection(PdsSyncSettings.SectionName));
        services.AddHttpClient("PdsService");
        services.AddScoped<IPdsService, PdsService>();

        // Feature flag service: wraps OpenFeature IFeatureClient for Application layer consumption
        services.AddScoped<IFeatureFlagService, OpenFeatureFlagService>();

        // Deployment mode configuration (single-tenant vs multi-tenant)
        services.Configure<DeploymentSettings>(configuration.GetSection(DeploymentSettings.SectionName));

        // Deployment mode provider: singleton that resolves mode from config → cache → DB.
        // Uses IOptionsMonitor for live config reload and IServiceScopeFactory for scoped DB access.
        services.AddSingleton<IDeploymentModeProvider, DeploymentModeProvider>();

        // Setup secret provider: singleton that manages the bootstrap setup secret lifecycle.
        // Must be singleton because the secret is resolved once at startup and locked after onboarding completion.
        services.AddSingleton<ISetupSecretProvider, SetupSecretProvider>();

        return services;
    }
}
