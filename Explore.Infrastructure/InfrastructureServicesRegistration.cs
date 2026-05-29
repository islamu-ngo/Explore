// ABOUTME: Registers Infrastructure services, providers, options, and validators for the platform.
// ABOUTME: Keeps application contracts wired to concrete infrastructure implementations at composition time.

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
using Explore.Infrastructure.Ai;
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
        services.AddSingleton<IEmailDispatchDrainService, EmailDispatchDrainService>();
        services.AddScoped<IEmailUnsubscribeTokenService, EmailUnsubscribeTokenService>();

        // Legacy S3-compatible object storage service. New local-first flows use IFileStorageProvider.
        services.AddScoped<IS3ConfigResolver, S3ConfigResolver>();
        services.AddSingleton<IS3ClientFactory, S3ClientFactory>();
        services.AddScoped<IObjectStorageService, ObjectStorageService>();
        services.AddOptions<LocalFileStorageOptions>()
            .Bind(configuration.GetSection(LocalFileStorageOptions.SectionName));
        services.AddSingleton<IValidateOptions<LocalFileStorageOptions>, LocalFileStorageOptionsValidator>();
        services.AddSingleton<IFileStorageProvider, LocalFileStorageProvider>();
        services.AddScoped<IFileStorageProvider, S3FileStorageProvider>();
        services.AddScoped<IFileStorageProviderResolver, FileStorageProviderResolver>();

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
        services.AddScoped<ITypedSettingsDocumentResolver, TypedSettingsDocumentResolver>();
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

        // Claims transformation: enriches the server ClaimsPrincipal with DB-resolved admin authority.
        // Admin authority is not serialized to Blazor WASM; browser affordances use BFF/API/HAL/status endpoints.
        services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

        // Configuration audit logging
        services.AddScoped<IConfigurationChangeLogService, ConfigurationChangeLogService>();
        services.AddScoped<IAuthorizationProviderConfigurationService, AuthorizationProviderConfigurationService>();

        // Authorization providers (runtime-switchable via SystemSetting "authorization.provider")
        // Both concrete providers are always registered; RuntimeAuthorizationProvider delegates at runtime.
        services.Configure<CerbosSettings>(configuration.GetSection(CerbosSettings.SectionName));
        services.Configure<CerbosAdminApiSettings>(configuration.GetSection(CerbosAdminApiSettings.SectionName));
        services.Configure<CerbosPolicyPackageOptions>(configuration.GetSection(CerbosPolicyPackageOptions.SectionName));
        services.PostConfigure<CerbosPolicyPackageOptions>(options =>
        {
            var policyPackagePath = configuration["Cerbos:PolicyPackagePath"];
            if (!string.IsNullOrWhiteSpace(policyPackagePath))
            {
                options.PoliciesPath = policyPackagePath;
            }
        });
        services.AddSingleton<CerbosAdminEndpointValidator>();

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
        services.AddSingleton<CerbosConfigCacheRegistry>();

        // Admin API client for policy package publishing (HTTP-based, separate from gRPC runtime)
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("CerbosAdminClient");

        services.AddScoped<CerbosPrincipalBuilder>();
        services.AddScoped<CerbosAuthorizationService>();
        services.AddScoped<FallbackAuthorizationService>();
        services.AddScoped<ICerbosConfigResolver, CerbosConfigResolver>();
        services.AddScoped<RuntimeAuthorizationProvider>();
        services.AddScoped<IAuthorizationProvider>(sp => sp.GetRequiredService<RuntimeAuthorizationProvider>());
        services.AddScoped<IAuthorizationProviderModeCacheInvalidator>(sp => sp.GetRequiredService<RuntimeAuthorizationProvider>());
        services.AddScoped<IPolicyPackageService, CerbosPolicyPackageService>();
        services.AddScoped<IPolicySyncService, PolicySyncService>();

        // Event Strategies
        services.AddScoped<IEventStrategy, IslamicEventStrategy>();
        services.AddScoped<IEventStrategy, TechEventStrategy>();
        services.AddScoped<IStrategyResolver, StrategyResolver>();

        // Analytics providers (runtime-switchable via SystemSetting "analytics.provider")
        // All concrete providers are always registered; RuntimeAnalyticsProvider delegates at runtime.
        // Config resolved per-tenant from cascading settings engine (SystemSetting -> TenantSetting)
        services.AddHttpClient("PostHogClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<PostHogAnalyticsProvider>();
        
        services.AddHttpClient("PlausibleClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<PlausibleAnalyticsProvider>();
        
        services.AddHttpClient("RybbitClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<RybbitAnalyticsProvider>();
        
        services.AddHttpClient("RudderStackClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<RudderStackAnalyticsProvider>();
        services.AddScoped<NullAnalyticsProvider>();
        services.AddScoped<IAnalyticsConfigResolver, AnalyticsConfigResolver>();
        services.AddScoped<RuntimeAnalyticsProvider>();
        services.AddScoped<IAnalyticsProvider>(sp => sp.GetRequiredService<RuntimeAnalyticsProvider>());
        services.AddScoped<IAnalyticsFeatureFlagProvider>(sp => sp.GetRequiredService<RuntimeAnalyticsProvider>());

        // AI provider foundation. Runtime provider selection is added later; keep fake concrete-only for now.
        services.AddOptions<AiProviderSettings>()
            .Bind(configuration.GetSection(AiProviderSettings.SectionName));
        services.AddSingleton<AiProviderSettingsValidator>();
        services.AddSingleton<IValidateOptions<AiProviderSettings>>(sp => sp.GetRequiredService<AiProviderSettingsValidator>());
        services.AddSingleton<AiProviderHealthReporter>();
        services.AddScoped<FakeAiChatProvider>();
        services.AddHttpClient(OpenAiCompatibleChatProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<OpenAiCompatibleChatProvider>();

        // Translation Management System providers (runtime-switchable via GovernanceSettings "localization.tms_provider")
        // All concrete providers are always registered; RuntimeTranslationProvider delegates at runtime.
        // None → OfflineTranslationProvider (bundled .json files), Tolgee → TolgeeTranslationProvider, Weblate → WeblateTranslationProvider
        services.AddHttpClient("TolgeeClient", client =>
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
        services.AddScoped<TolgeeTranslationProvider>();
        
        services.AddHttpClient("WeblateClient", client =>
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
        services.AddScoped<WeblateTranslationProvider>();
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
        services.AddSingleton<IEmailDispatchTransport, RabbitMqEmailDispatchTransport>();

        // Generic Outbox Processor settings and dispatcher
        services.Configure<OutboxProcessorSettings>(configuration.GetSection(OutboxProcessorSettings.SectionName));
        services.AddScoped<MqContractOutboxMessageDispatcher>();
        services.AddScoped<IOutboxMessageDispatcher, CompositeOutboxMessageDispatcher>();
        services.AddOptions<EmailDispatchProcessorSettings>()
            .Bind(configuration.GetSection(EmailDispatchProcessorSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailDispatchProcessorSettings>, EmailDispatchProcessorSettingsValidator>();
        services.AddOptions<EmailDispatchRabbitMqSettings>()
            .Bind(configuration.GetSection(EmailDispatchRabbitMqSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailDispatchRabbitMqSettings>, EmailDispatchRabbitMqSettingsValidator>();
        services.AddOptions<IdempotencyCleanupSettings>()
            .Bind(configuration.GetSection(IdempotencyCleanupSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdempotencyCleanupSettings>, IdempotencyCleanupSettingsValidator>();
        services.AddScoped<IIdempotencyCleanupService, IdempotencyCleanupService>();

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
