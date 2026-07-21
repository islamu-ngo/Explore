// ABOUTME: Application layer service registration for DI container.
// ABOUTME: Registers MediatR, AutoMapper, pipeline behaviors, and application services.
using System.Reflection;
using AutoMapper.Internal;
using Explore.Application.Analytics;
using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Configuration;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.AiAssistant.Actors;
using Explore.Application.Features.AiAssistant.Disclosure;
using Explore.Application.Features.AiAssistant.Tools;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Features.ManagedProviderProvisioning;
using Explore.Application.Features.ManagedProviderProvisioning.Handlers.Commands;
using Explore.Application.Features.Management;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Services.Federation;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Services.Webhooks;
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
        PrivacyErasureDurabilityOptions erasureDurability =
            PrivacyErasureDurabilityOptions.FromConfiguration(configuration);
        services.AddOptions<PrivacyErasureDurabilityOptions>()
            .Configure(options =>
            {
                options.Mode = erasureDurability.Mode;
            });

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
            // Bound every map traversal in the FOSS line to mitigate CVE-2026-32933.
            // The same ceiling is defense in depth for commercial vendor-patched builds.
            cfg.Internal().ForAllMaps((_, mapping) => mapping.MaxDepth(64));
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
        services.AddOptions<EventReportSubmissionOptions>()
            .Bind(configuration.GetSection(EventReportSubmissionOptions.SectionName))
            .Validate(
                options => EventReportSubmissionOptions.IsValidCaseSlaHours(options.CaseSlaHours),
                $"Reporting:CaseSlaHours must be between {EventReportSubmissionOptions.MinCaseSlaHours} and {EventReportSubmissionOptions.MaxCaseSlaHours} hours.")
            .ValidateOnStart();
        services.AddOptions<EventReminderOptions>()
            .Bind(configuration.GetSection(EventReminderOptions.SectionName))
            .Validate(
                options => EventReminderOptions.IsValidLeadTimeHours(options.EventReminderLeadTimeHours),
                $"EmailDispatch:EventReminderLeadTimeHours must be between {EventReminderOptions.MinLeadTimeHours} and {EventReminderOptions.MaxLeadTimeHours} hours.")
            .ValidateOnStart();
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
        services.AddScoped<IWebhookAuditEventWriter, WebhookAuditEventWriter>();
        services.AddScoped<IWebhookOwnershipScopeResolver, WebhookOwnershipScopeResolver>();
        services.AddScoped<IAuthProviderConfigurationService, AuthProviderConfigurationService>();
        services.AddScoped<IKeycloakIdentityContractContributor, EventKeycloakIdentityContractContributor>();
        services.AddScoped<IAccountAuthorityLifecycleEmailService, DefaultAccountAuthorityLifecycleEmailService>();
        services.AddScoped<IKeycloakRealmDesiredStateBuilder, KeycloakRealmDesiredStateBuilder>();
        services.AddScoped<IAnalyticsGovernanceService, AnalyticsGovernanceService>();
        services.AddScoped<ILocationPrivacyGovernanceService, LocationPrivacyGovernanceService>();
        services.AddScoped<ILocationPrivacyGovernanceMutationService, LocationPrivacyGovernanceMutationService>();
        services.AddScoped<EventLocationAttachmentService>();
        services.AddSingleton<IEventLocationRegistrationAccessService, EventLocationRegistrationAccessService>();
        services.AddScoped<IEventLocationExactReadAuditService, EventLocationExactReadAuditService>();
        services.AddScoped<IEventLocationManagementAuthorizationService, EventLocationManagementAuthorizationService>();
        services.AddScoped<IEventLocationDisclosureService, EventLocationDisclosureService>();
        services.AddScoped<IFanoutAttendeeLocationAuthorizationService, FanoutAttendeeLocationAuthorizationService>();
        services.AddScoped<PrivacyErasureApplier>();
        if (erasureDurability.Mode == PrivacyErasureDurabilityMode.RetainedAuthority)
        {
            services.AddScoped<IPrivacyErasureService, RetainedAuthorityPrivacyErasureWorkflow>();
        }
        else
        {
            services.AddScoped<IPrivacyErasureService, ApplicationDatabasePrivacyErasureWorkflow>();
        }
        services.AddSingleton<EventLocationDisclosureEvaluator>();
        services.AddScoped<PublicEventLocationDisclosureEvaluator>();
        services.AddScoped<AtprotoEventPublicationSnapshotFactory>();
        services.AddScoped<AtprotoEventGovernanceResolver>();
        services.AddScoped<AtprotoEventPublicationPlanner>();
        services.AddScoped<IAtprotoDeliveryGate>(provider =>
            provider.GetRequiredService<AtprotoEventPublicationPlanner>());
        services.AddScoped<IAtprotoLocationPrivacyCorrectionPlanner>(provider =>
            provider.GetRequiredService<AtprotoEventPublicationPlanner>());
        services.AddScoped<AtprotoPdsDeliveryProcessor>();
        services.AddScoped<IModuleCapabilityService, ModuleCapabilityService>();
        services.AddScoped<SettingUpsertService>();
        services.AddScoped<ManagedTenantProvisioningPreflight>();
        services.AddScoped<ManagedTenantProvisioningCapacityReader>();
        services.AddScoped<TenantActivationCapacityPolicy>();
        services.AddScoped<TenantPlanStorageQuotaCeilingPolicy>();
        services.AddScoped<IManagedProviderClientProvisioner, EnsureManagedProviderClientProvisionedCommandHandler>();

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
        services.AddScoped<IRecipientNotificationMaterializer, RecipientNotificationMaterializer>();
        services.AddSingleton<NotificationDeliveryPolicyResolver>();
        services.AddSingleton<ReportReceiptNotificationFactory>();
        services.AddSingleton<ReportOutcomeNotificationFactory>();
        services.AddSingleton<ReportNeedsMoreInformationNotificationFactory>();
        services.AddSingleton<EventOrganizerWarningNotificationFactory>();
        services.AddSingleton<NotificationFanoutRecipientTemplateFactory>();
        services.AddScoped<NotificationFanoutRecipientMaterializationService>();
        services.AddScoped<INotificationFanoutRecipientMaterializationService>(provider =>
            provider.GetRequiredService<NotificationFanoutRecipientMaterializationService>());
        services.AddScoped<NotificationFanoutPageProcessor>();
        services.AddScoped<NotificationFanoutOccurrenceCoordinator>();
        services.AddScoped<IEventLifecycleEmailOutboxFactory, EventLifecycleEmailOutboxFactory>();
        services.AddScoped<IListmonkRegistrationSyncOutboxFactory, ListmonkRegistrationSyncOutboxFactory>();
        services.AddScoped<IRegistrationNotificationDeliveryService, RegistrationNotificationDeliveryService>();
        services.AddScoped<NotificationFanoutOccurrenceHandoffService>();
        services.AddScoped<IEventPublishedNotificationFanoutService, EventPublishedNotificationFanoutService>();
        services.AddScoped<IEventModerationNotificationFanoutService, EventModerationNotificationFanoutService>();
        services.AddScoped<IEventDetailsProjectionService, EventDetailsProjectionService>();
        services.AddScoped<INotificationRefreshStreamService, NotificationRefreshStreamService>();
        services.AddScoped<IEventLifecycleScheduler, EventLifecycleScheduler>();
        services.AddScoped<AtprotoEventGovernanceResolver>();
        services.AddScoped<AtprotoJetstreamTenantPresentationResolver>();
        services.AddScoped<AtprotoPdsRecoveryPolicyResolver>();
        services.AddScoped<IEventLifecyclePolicyProvider, EventLifecyclePolicyProvider>();
        services.AddScoped<IEventLifecycleReadinessEvaluator, EventLifecycleReadinessEvaluator>();
        services.AddScoped<IScheduledEmailDispatchTrigger, NoOpScheduledEmailDispatchTrigger>();
        services.AddSingleton<IScheduledJobRegistry, ScheduledJobRegistry>();
        services.AddSingleton<IWebhookEventTypeRegistry, WebhookEventTypeRegistry>();
        services.AddSingleton<IWebhookEventSchemaProvider, WebhookEventSchemaProvider>();
        services.AddSingleton<IWebhookPayloadBuilder, DefaultWebhookPayloadBuilder>();
        services.AddSingleton<IWebhookDeliveryPlanResolver, FailClosedWebhookDeliveryPlanResolver>();
        services.AddScoped<IWebhookEventTypeCatalogSyncService, WebhookEventTypeCatalogSyncService>();
        services.AddScoped<IWebhookEventPublisher, DefaultWebhookEventPublisher>();

        // Scheduling domain services (stateless, safe as singleton).
        services.AddSingleton<IEventScheduleProjectionCalculator, EventScheduleProjectionCalculator>();

        // Appearance resolution and palette generation
        services.AddScoped<IAppearanceResolutionService, AppearanceResolutionService>();

        return services;
    }
}
