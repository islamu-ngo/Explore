// ABOUTME: Registers Infrastructure services, providers, options, and validators for the platform.
// ABOUTME: Keeps application contracts wired to concrete infrastructure implementations at composition time.

using System.Net.Http;
using System.Net.Sockets;
using Amazon;
using Amazon.S3;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Cerbos.Sdk;
using Cerbos.Sdk.Builder;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Strategies;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Management;
using Explore.Application.Models;
using Explore.Application.Utilities;
using Explore.Infrastructure.Ai;
using Explore.Infrastructure.Analytics;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Identity;
using Explore.Infrastructure.Integrations.Listmonk;
using Explore.Infrastructure.Localization;
using Explore.Infrastructure.Localization.Resilience;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Mail.Unsubscribe;
using Explore.Infrastructure.Management;
using Explore.Infrastructure.Messaging;
using Explore.Infrastructure.Services;
using Explore.Infrastructure.Services.Federation;
using Explore.Infrastructure.Services.Keycloak;
using Explore.Infrastructure.Services.Moderation;
using Explore.Infrastructure.Services.Moderation.Coop;
using Explore.Infrastructure.Storage;
using Explore.Infrastructure.Strategies;
using Explore.Infrastructure.SupportAccess;
using Explore.Infrastructure.Webhooks;
using Explore.Infrastructure.WebPush;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.AI;
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
        services.AddOptions<ManagedControlPlaneOptions>()
            .Bind(configuration.GetSection(ManagedControlPlaneOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ManagedControlPlaneOptions>, ManagedControlPlaneOptionsValidator>();
        services.AddHttpClient<IManagedControlPlaneRegistrationClient, ManagedControlPlaneRegistrationClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                ConnectTimeout = TimeSpan.FromSeconds(10)
            });

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
        services.AddScoped<IStorageObjectDeletionService, StorageObjectDeletionService>();

        // Identity services
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISupportAccessSessionService, SupportAccessSessionService>();
        services.AddScoped<IPublicUrlBuilder, PublicUrlBuilder>();
        services.AddScoped<IEventReportEvidenceProtector, EventReportEvidenceProtector>();
        services.AddOptions<ModerationProviderOptions>()
            .Bind(configuration.GetSection(ModerationProviderOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ModerationProviderOptions>, ModerationProviderOptionsValidator>();
        services.AddOptions<OspreyProviderOptions>()
            .Bind(configuration.GetSection(OspreyProviderOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OspreyProviderOptions>, OspreyProviderOptionsValidator>();
        services.AddOptions<CoopProviderOptions>()
            .Bind(configuration.GetSection(CoopProviderOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CoopProviderOptions>, CoopProviderOptionsValidator>();
        services.AddScoped<LocalEventReportProvider>();
        services.AddScoped<NoopModerationSignalProvider>();
        services.AddHttpClient(OspreyModerationSignalProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<OspreyModerationSignalProvider>();
        services.AddHttpClient(CoopReviewQueueProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<CoopReviewQueueProvider>();
        services.AddScoped<NoopReviewQueueProvider>();
        services.AddScoped<IReportingRoutingPolicyResolver, ReportingRoutingPolicyResolver>();
        services.AddScoped<CompositeEventReportProvider>(sp => new CompositeEventReportProvider(
            sp.GetRequiredService<LocalEventReportProvider>(),
            sp.GetRequiredService<OspreyModerationSignalProvider>(),
            sp.GetRequiredService<CoopReviewQueueProvider>(),
            sp.GetRequiredService<IReportingRoutingPolicyResolver>()));
        services.AddScoped<RuntimeModerationProviderResolver>();
        services.AddScoped<IReportProviderSyncDispatcher, ReportProviderSyncDispatcher>();
        services.AddScoped<IEventReportProvider>(sp => sp.GetRequiredService<RuntimeModerationProviderResolver>());
        services.AddScoped<IModerationSignalProvider>(sp => sp.GetRequiredService<RuntimeModerationProviderResolver>());
        services.AddScoped<IReviewQueueProvider>(sp => sp.GetRequiredService<RuntimeModerationProviderResolver>());
        services.AddScoped<IReportDecisionExecutor>(sp => sp.GetRequiredService<RuntimeModerationProviderResolver>());

        // Webhook providers: Local is the self-hostable default; Runtime provider selects configured mode.
        services.AddOptions<WebhookOptions>()
            .Bind(configuration.GetSection(WebhookOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<WebhookDeliveryProcessorSettings>()
            .Bind(configuration.GetSection(WebhookDeliveryProcessorSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<WebhookOptions>, WebhookOptionsValidator>();
        services.AddSingleton<IValidateOptions<WebhookDeliveryProcessorSettings>, WebhookDeliveryProcessorSettingsValidator>();
        services.AddSingleton<IWebhookSignatureService, WebhookSignatureService>();
        services.AddSingleton<WebhookRetryScheduler>();
        services.AddSingleton<WebhookEndpointSafetyPolicy>();
        services.AddSingleton<WebhookEndpointSecretResolver>();
        services.AddSingleton<IWebhookDeliveryDrainService, WebhookDeliveryDrainService>();
        services.AddHttpClient(WebhookDeliveryDrainService.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<WebhookOptions>>().CurrentValue;
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectTimeout = TimeSpan.FromSeconds(options.Local.ConnectTimeoutSeconds),
                UseCookies = false
            };
        });
        services.AddScoped<DisabledWebhookDeliveryProvider>();
        services.AddScoped<DryRunWebhookDeliveryProvider>();
        services.AddScoped<LocalWebhookDeliveryProvider>();
        services.AddScoped<ISvixWebhookClient, SvixWebhookClient>();
        services.AddScoped<SvixWebhookDeliveryProvider>();
        services.AddScoped<IWebhookProviderPortalService, SvixAppPortalService>();
        services.AddScoped<IWebhookProviderEventTypeSyncService, SvixEventTypeSyncService>();
        services.AddScoped<RuntimeWebhookDeliveryProvider>();
        services.AddScoped<IWebhookDeliveryProvider>(sp => sp.GetRequiredService<RuntimeWebhookDeliveryProvider>());

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
        services.Configure<KeycloakBootstrapOptions>(configuration.GetSection(KeycloakBootstrapOptions.SectionName));
        services.AddHttpClient(KeycloakBootstrapService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
        })
            .ConfigurePrimaryHttpMessageHandler(CreateKeycloakBootstrapHttpHandler);
        services.AddScoped<IKeycloakBootstrapService, KeycloakBootstrapService>();
        services.Configure<KeycloakLifecycleEmailOptions>(configuration.GetSection(KeycloakLifecycleEmailOptions.SectionName));
        services.AddHttpClient(KeycloakAccountAuthorityLifecycleEmailService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
        })
        .ConfigurePrimaryHttpMessageHandler(CreateKeycloakBootstrapHttpHandler);
        services.AddScoped<IAccountAuthorityLifecycleEmailService, KeycloakAccountAuthorityLifecycleEmailService>();

        services.AddOptions<AuthorizationProviderDeploymentOptions>()
            .Bind(configuration.GetSection(AuthorizationProviderDeploymentOptions.SectionName))
            .Validate(AuthorizationProviderDeploymentOptions.IsValid,
                "Authorization:Provider must be blank, 'local', or 'cerbos'.")
            .ValidateOnStart();
        services.AddSingleton<AuthorizationProviderBootstrapState>();

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

        // AI provider foundation. Strategy pattern dispatches to concrete adapters without if/else bloat.
        services.AddOptions<AiProviderSettings>()
            .Bind(configuration.GetSection(AiProviderSettings.SectionName));
        services.AddSingleton<AiProviderSettingsValidator>();
        services.AddSingleton<IValidateOptions<AiProviderSettings>>(sp => sp.GetRequiredService<AiProviderSettingsValidator>());
        services.AddScoped<AiProviderHealthReporter>();
        services.AddScoped<FakeAiChatProvider>();
        services.AddHttpClient(OpenAiResponsesChatProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<OpenAiResponsesChatProvider>();
        services.AddHttpClient(OpenAiCompatibleChatProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<OpenAiCompatibleChatProvider>();
        services.AddHttpClient(AnthropicCompatibleChatProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<AnthropicCompatibleChatProvider>();
        services.AddHttpClient(AnthropicChatProvider.HttpClientName, client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddScoped<AnthropicChatProvider>();
        if (IsSdkBackedAiProvider(configuration))
        {
            services.AddScoped<IChatClient>(sp => CreateSdkBackedChatClient(
                sp.GetRequiredService<IOptions<AiProviderSettings>>().Value));
            services.AddScoped<MicrosoftExtensionsAiChatProvider>();
        }

        services.AddScoped<IAiProviderStrategy, FakeAiProviderStrategy>();
        services.AddScoped<IAiProviderStrategy, OpenAiResponsesProviderStrategy>();
        services.AddScoped<IAiProviderStrategy, OpenAiCompatibleProviderStrategy>();
        services.AddScoped<IAiProviderStrategy, AnthropicProviderStrategy>();
        services.AddScoped<IAiProviderStrategy, AnthropicCompatibleProviderStrategy>();
        if (IsSdkBackedAiProvider(configuration))
        {
            services.AddScoped<IAiProviderStrategy, MicrosoftExtensionsProviderStrategy>();
        }
        services.AddScoped<IAiProviderStrategyResolver, AiProviderStrategyResolver>();

        services.AddScoped<RuntimeAiChatProvider>();
        services.AddScoped<IAiChatProvider>(sp => sp.GetRequiredService<RuntimeAiChatProvider>());
        services.AddScoped<IAiModelCatalog>(sp => sp.GetRequiredService<RuntimeAiChatProvider>());
        services.AddOptions<AiRetentionCleanupSettings>()
            .Bind(configuration.GetSection(AiRetentionCleanupSettings.SectionName));
        services.AddSingleton<IValidateOptions<AiRetentionCleanupSettings>, AiRetentionCleanupSettingsValidator>();
        services.AddSingleton<IAiRetentionCleanupService, AiRetentionCleanupService>();

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
        services.AddSingleton<IStaticTranslationBundleReader>(sp => sp.GetRequiredService<OfflineTranslationProvider>());
        services.AddScoped<NullTranslationProvider>();
        services.AddScoped<ITranslationConfigResolver, TranslationConfigResolver>();
        services.AddScoped<RuntimeTranslationProvider>();
        services.AddScoped<ITranslationManagementProvider>(sp => sp.GetRequiredService<RuntimeTranslationProvider>());
        services.AddScoped<TranslationResolver>();
        services.AddScoped<ITranslationResolver>(sp => sp.GetRequiredService<TranslationResolver>());
        services.AddScoped<IBundleFileWriter, BundleFileWriter>();

        services.AddSingleton<IEmailDispatchTransport, RabbitMqEmailDispatchTransport>();
        services.AddScoped<EmailDispatchRabbitMqPointerPublisher>();

        services.AddOptions<WebPushSettings>()
            .Bind(configuration.GetSection(WebPushSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<WebPushSettings>, WebPushSettingsValidator>();
        services.AddSingleton<IWebPushConfigurationProvider, WebPushConfigurationProvider>();
        services.AddSingleton<WebPushEndpointSafetyPolicy>();
        services.AddSingleton<WebPushDispatchDrainService>();
        services.AddHostedService<WebPushDispatchProcessor>();
        services.AddHttpClient<IWebPushNotificationSender, WebPushNotificationSender>((sp, client) =>
        {
            var webPushSettings = sp.GetRequiredService<IOptions<WebPushSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(webPushSettings.RequestTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var safetyPolicy = sp.GetRequiredService<WebPushEndpointSafetyPolicy>();
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                ConnectCallback = (context, cancellationToken) =>
                    WebPushSafeConnector.ConnectAsync(safetyPolicy, context.DnsEndPoint, cancellationToken)
            };
        });

        // Generic Outbox Processor settings and dispatcher
        services.Configure<OutboxProcessorSettings>(configuration.GetSection(OutboxProcessorSettings.SectionName));
        services.AddScoped<IOutboxMessageDispatcher, CompositeOutboxMessageDispatcher>();
        services.AddOptions<EmailDispatchProcessorSettings>()
            .Bind(configuration.GetSection(EmailDispatchProcessorSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailDispatchProcessorSettings>, EmailDispatchProcessorSettingsValidator>();
        services.AddOptions<EmailDispatchRabbitMqSettings>()
            .Bind(configuration.GetSection(EmailDispatchRabbitMqSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailDispatchRabbitMqSettings>, EmailDispatchRabbitMqSettingsValidator>();
        services.AddOptions<IntegrationSyncProcessorSettings>()
            .Bind(configuration.GetSection(IntegrationSyncProcessorSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IIntegrationSyncDrainService, IntegrationSyncDrainService>();
        services.AddHttpClient(ListmonkSyncService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<ListmonkSyncService>();
        services.AddScoped<IListmonkConnectionTester, ListmonkConnectionTester>();
        services.AddOptions<IdempotencyCleanupSettings>()
            .Bind(configuration.GetSection(IdempotencyCleanupSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdempotencyCleanupSettings>, IdempotencyCleanupSettingsValidator>();
        services.AddScoped<IIdempotencyCleanupService, IdempotencyCleanupService>();
        services.AddOptions<StorageReconciliationSettings>()
            .Bind(configuration.GetSection(StorageReconciliationSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageReconciliationSettings>, StorageReconciliationSettingsValidator>();
        services.AddScoped<IStorageReconciliationService, StorageReconciliationService>();

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

    private static bool IsSdkBackedAiProvider(IConfiguration configuration)
    {
        var providerValue = configuration[$"{AiProviderSettings.SectionName}:Provider"];
        if (!int.TryParse(providerValue, out var providerId))
            return false;
        return providerId == AiProviderSettings.ProviderAzureOpenAi;
    }

    private static SocketsHttpHandler CreateKeycloakBootstrapHttpHandler()
    {
        // Keep bootstrap aligned with runtime OIDC backchannels: this deployment's Keycloak
        // host publishes AAAA records, but IPv6 is unreachable from some developer machines.
        // Forcing IPv4 avoids a 60s API request timeout while requesting the admin token.
        return new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }

    private static IChatClient CreateSdkBackedChatClient(AiProviderSettings settings)
    {
        if (settings.Provider == AiProviderSettings.ProviderAzureOpenAi)
        {
            var endpoint = new Uri(settings.EndpointUrl.Trim(), UriKind.Absolute);
            var client = settings.AzureCredentialMode.Equals(
                AiProviderSettings.AzureCredentialModeDefaultAzureCredential,
                StringComparison.OrdinalIgnoreCase)
                    ? new AzureOpenAIClient(endpoint, CreateDefaultAzureCredential(settings))
                    : new AzureOpenAIClient(endpoint, new AzureKeyCredential(settings.ApiKey.Trim()));

            return client
                .GetChatClient(settings.ModelId.Trim())
                .AsIChatClient();
        }

        throw new InvalidOperationException("Configured AI provider is not SDK-backed.");
    }

    private static DefaultAzureCredential CreateDefaultAzureCredential(AiProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AzureTenantId))
        {
            return new DefaultAzureCredential();
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = settings.AzureTenantId.Trim()
        });
    }
}
