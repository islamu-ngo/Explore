// ABOUTME: Application layer service registration for DI container.
// ABOUTME: Registers MediatR, AutoMapper, pipeline behaviors, and application services.
using System.Reflection;
using Explore.Application.Analytics;
using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Application.Features.AiAssistant.Disclosure;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Features.EventReporting;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Settings;
using Explore.Application.Webhooks;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Application;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection ConfigureApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(cfg =>
        {
#if USE_COMMERCIAL_LUCKYPENNY_LIBS
            // AutoMapper 15+ requires a Lucky Penny commercial license key at runtime.
            // Injected from Infisical /api folder: LUCKYPENNY_LICENSE_KEY → Licensing:LuckyPenny:LicenseKey.
            // No throw here: the OpenAPI doc generator runs Program.Main at build time without secrets.
            // Lucky Penny libraries themselves enforce licensing at runtime.
            var licenseKey = configuration["Licensing:LuckyPenny:LicenseKey"];
            if (!string.IsNullOrEmpty(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }
#endif
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        services.AddMediatR(cfg =>
        {
#if USE_COMMERCIAL_LUCKYPENNY_LIBS
            // MediatR 13+ requires a Lucky Penny commercial license key at runtime.
            // Same key as AutoMapper — single LUCKYPENNY_LICENSE_KEY from Infisical.
            var licenseKey = configuration["Licensing:LuckyPenny:LicenseKey"];
            if (!string.IsNullOrEmpty(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }
#endif
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServicesRegistration).Assembly);
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.Configure<EventReportSubmissionOptions>(configuration.GetSection(EventReportSubmissionOptions.SectionName));
        services.Configure<NotificationRoutingOptions>(configuration.GetSection(NotificationRoutingOptions.SectionName));
        services.Configure<AccountAuthorityLifecycleEmailOptions>(configuration.GetSection(AccountAuthorityLifecycleEmailOptions.SectionName));
        services.AddSingleton<IAiToolContractRegistry>(_ => AiToolContractRegistry.CreateDefault());
        services.AddScoped<IAiAssistantActorContextService, AiAssistantActorContextService>();
        services.AddScoped<IAiContextGateway, AiContextGateway>();
        services.AddScoped<IAiProviderTrustResolver, DefaultAiProviderTrustResolver>();
        services.AddScoped<IAiContextRedactor, AiContextRedactor>();
        services.AddScoped<IAiContextHygieneService, AiContextHygieneService>();

        // Onboarding Services
        services.AddScoped<ITenantPolicySettingService, TenantPolicySettingService>();
        services.AddScoped<ITenantStorageSettingService, TenantStorageSettingService>();
        services.AddScoped<ITenantBrandingSettingsDocumentProvisioningService, TenantBrandingSettingsDocumentProvisioningService>();
        services.AddScoped<ITenantBrandingSettingsDocumentLockService, TenantBrandingSettingsDocumentLockService>();
        services.AddScoped<IInstanceGovernanceSettingService, InstanceGovernanceSettingService>();
        services.AddScoped<IInstanceStorageSettingService, InstanceStorageSettingService>();
        services.AddScoped<IInstanceSmtpSettingService, InstanceSmtpSettingService>();
        services.AddScoped<IInstanceBootstrapAuditLogger, InstanceBootstrapAuditLogger>();
        services.AddScoped<IAuthProviderConfigurationService, AuthProviderConfigurationService>();
        services.AddScoped<IKeycloakIdentityContractContributor, EventKeycloakIdentityContractContributor>();
        services.AddScoped<IAccountAuthorityLifecycleEmailService, DefaultAccountAuthorityLifecycleEmailService>();
        services.AddScoped<IKeycloakRealmDesiredStateBuilder, KeycloakRealmDesiredStateBuilder>();
        services.AddScoped<IAnalyticsGovernanceService, AnalyticsGovernanceService>();
        services.AddScoped<IModuleCapabilityService, ModuleCapabilityService>();
        services.AddScoped<SettingUpsertService>();

        // Analytics consent / runtime profile resolution
        services.AddScoped<IAnalyticsRuntimeProfileResolver, AnalyticsRuntimeProfileResolver>();
        services.AddScoped<IStoragePolicyResolver, StoragePolicyResolver>();
        services.AddScoped<IStorageObjectContentReader, StorageObjectContentReader>();

        // Authorization: dynamic permission infrastructure
        services.AddScoped<ICapabilityCeilingService, CapabilityCeilingService>();
        services.AddScoped<IEventRoleAuthorityCeilingService, EventRoleAuthorityCeilingService>();
        services.AddScoped<ICustomPropertyGovernancePolicy, CustomPropertyGovernancePolicy>();
        services.AddScoped<ICustomPropertyAutomationConditionPolicy, CustomPropertyAutomationConditionPolicy>();
        services.AddScoped<IEventActorResolver, EventActorResolver>();
        services.AddScoped<IEventTemplateInstantiationService, EventTemplateInstantiationService>();
        services.AddScoped<IEventSessionTemplateInstantiationService, EventSessionTemplateInstantiationService>();
        services.AddScoped<IEventTemplateDiffService, EventTemplateDiffService>();
        services.AddScoped<IEventTemplateSyncService, EventTemplateSyncService>();
        services.AddScoped<IEventSessionTemplateDiffService, EventSessionTemplateDiffService>();
        services.AddScoped<IEventSessionTemplateSyncService, EventSessionTemplateSyncService>();
        services.AddScoped<IPermissionRegistryService, PermissionRegistryService>();
        services.AddScoped<IContactShareConsentService, ContactShareConsentService>();
        services.AddScoped<INotificationOwnershipResolver, DefaultNotificationOwnershipResolver>();
        services.AddScoped<INotificationOrchestrator, DefaultNotificationOrchestrator>();
        services.AddScoped<IEventLifecycleEmailOutboxFactory, EventLifecycleEmailOutboxFactory>();
        services.AddScoped<IListmonkRegistrationSyncOutboxFactory, ListmonkRegistrationSyncOutboxFactory>();
        services.AddScoped<IRegistrationNotificationDeliveryService, RegistrationNotificationDeliveryService>();
        services.AddScoped<IEventPublishedNotificationFanoutService, EventPublishedNotificationFanoutService>();
        services.AddScoped<IEventModerationNotificationFanoutService, EventModerationNotificationFanoutService>();
        services.AddScoped<IEventDetailsProjectionService, EventDetailsProjectionService>();
        services.AddScoped<INotificationRefreshStreamService, NotificationRefreshStreamService>();
        services.AddScoped<IEventLifecycleScheduler, EventLifecycleScheduler>();
        services.AddScoped<IEventLifecyclePolicyProvider, EventLifecyclePolicyProvider>();
        services.AddScoped<IEventLifecycleReadinessEvaluator, EventLifecycleReadinessEvaluator>();
        services.AddScoped<IScheduledEmailDispatchTrigger, NoOpScheduledEmailDispatchTrigger>();
        services.AddSingleton<IScheduledJobRegistry, ScheduledJobRegistry>();
        services.AddSingleton<IWebhookEventTypeRegistry, WebhookEventTypeRegistry>();
        services.AddSingleton<IWebhookEventSchemaProvider, WebhookEventSchemaProvider>();
        services.AddSingleton<IWebhookPayloadBuilder, DefaultWebhookPayloadBuilder>();
        services.AddScoped<IWebhookEventTypeCatalogSyncService, WebhookEventTypeCatalogSyncService>();
        services.AddScoped<IWebhookEventPublisher, DefaultWebhookEventPublisher>();

        // Scheduling domain services (stateless, safe as singleton).
        services.AddSingleton<IEventScheduleProjectionCalculator, EventScheduleProjectionCalculator>();

        // Appearance resolution and palette generation
        services.AddScoped<IAppearanceResolutionService, AppearanceResolutionService>();

        return services;
    }
}
