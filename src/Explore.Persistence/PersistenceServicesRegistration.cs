// ABOUTME: Registers EF Core persistence, repositories, caches, and unit-of-work services.
// ABOUTME: Keeps DbContext pooling compatible with property-injected scoped tenant and user dependencies.

using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Persistence.Caching;
using Explore.Persistence.Extensions;
using Explore.Persistence.Repositories;
using Explore.Persistence.Security;
using Explore.Persistence.Services;
using Explore.Secrets.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Explore.Persistence;

public static class PersistenceServicesRegistration
{
    // didn't implement aspire integration trough passing the builder cause it will require to install aspnetcore nuget package in persistence project and i want to keep it clean so let this dependency in API project only
    //public static IServiceCollection ConfigurePersistenceServices(this IServiceCollection services,
    //    WebApplicationBuilder builder) // Pass the builder instead of just configuration
    //{
    //    // Use Aspire's integration
    //    builder.AddNpgsqlDbContext<ExploreDbContext>("ExploreDB");

    public static IServiceCollection ConfigurePersistenceServices(this IServiceCollection services,
        IConfiguration configuration,
        bool skipDbContextRegistration = false,
        bool skipLookupCacheInitializer = false,
        string? environmentName = null)
    {
        // Skip DbContext registration when running integration tests (they register their own)
        if (!skipDbContextRegistration)
        {
            // Precedence: explicit ConnectionStrings:DefaultConnection (tests / overrides)
            // -> BootstrapSecretLoader (Infisical -> POSTGRESQL_* env -> Postgresql:* config). No URL form.
            var connectionString = configuration["ConnectionStrings:DefaultConnection"];
            if (string.IsNullOrEmpty(connectionString))
            {
                using var bootstrapLoggerFactory = LoggerFactory.Create(static builder =>
                {
                    builder.AddSimpleConsole(static options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss.fff ";
                    });
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("Explore.Persistence.Bootstrap");

                var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(configuration, bootstrapLogger);
                connectionString = credentials.ConnectionString;
            }

            // Use pooled DbContext factory for performance (EF Core recommended pattern)
            // The scoped ExploreDbContext registration below handles scoped dependency injection
            services.AddPooledDbContextFactory<ExploreDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                        npgsqlOptions.CommandTimeout(30);
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                    .UseSnakeCaseNamingConvention();

                if (IsEnabled(configuration["Persistence:EnableRlsTenantSession"]))
                {
                    options.AddInterceptors(PostgresTenantSessionInterceptor.Instance);
                }

                var runtimeEnvironmentName = environmentName
                                             ?? configuration["ASPNETCORE_ENVIRONMENT"]
                                             ?? configuration["DOTNET_ENVIRONMENT"];
                if (string.Equals(runtimeEnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    options.EnableDetailedErrors();
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                }
            });

            // Register scoped DbContext that sets scoped dependencies from DI
            // This follows EF Core's recommended pattern for pooled contexts with scoped dependencies
            services.AddScoped(sp =>
            {
                var factory = sp.GetRequiredService<IDbContextFactory<ExploreDbContext>>();
                var context = factory.CreateDbContext();

                // Set scoped dependencies via property injection (null during migrations, populated during API requests)
                context.ClearTenantFilterBypass();
                context.TenantContext = sp.GetService<ITenantContext>();
                context.CurrentUserService = sp.GetService<ICurrentUserService>();

                return context;
            });

        }

        // Unit of Work (wraps EF Core transactions)
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        services.AddScoped<ISettingMutationLock, PostgresSettingMutationLock>();

        services.AddScoped<IGenericRepository<EventReportDecision, Guid>, GenericRepository<EventReportDecision, Guid>>();
        services.AddScoped<IGenericRepository<EventReportTarget, Guid>, GenericRepository<EventReportTarget, Guid>>();
        services.AddScoped<IGenericRepository<EventReportEvidence, Guid>, GenericRepository<EventReportEvidence, Guid>>();
        services.AddScoped<IGenericRepository<EventReportCase, Guid>, GenericRepository<EventReportCase, Guid>>();
        services.AddScoped<IGenericRepository<UserPii, Guid>, GenericRepository<UserPii, Guid>>();
        services.AddScoped<IGenericRepository<ActorPii, Guid>, GenericRepository<ActorPii, Guid>>();

        // Lookup cache
        services.AddSingleton<ILookupDataCache, LookupDataCache>();
        if (!skipLookupCacheInitializer)
        {
            services.AddHostedService<LookupDataCacheInitializer>();
        }

        // Lookup Table Repositories
        services.AddScoped<IApprovalStatusRepository, ApprovalStatusRepository>();
        services.AddScoped<IAudienceAgeRepository, AudienceAgeRepository>();
        services.AddScoped<IAudienceGenderRepository, AudienceGenderRepository>();
        services.AddScoped<IEventTypeRepository, EventTypeRepository>();
        services.AddScoped<IEventStatusRepository, EventStatusRepository>();
        services.AddScoped<IEventSessionStatusRepository, EventSessionStatusRepository>();
        services.AddScoped<IEventFormatRepository, EventFormatRepository>();
        services.AddScoped<IVisibilityTypeRepository, VisibilityTypeRepository>();
        services.AddScoped<IRegistrationModeRepository, RegistrationModeRepository>();
        services.AddScoped<IEventRegistrationPolicyRepository, EventRegistrationPolicyRepository>();
        services.AddScoped<IRegistrationScopeRepository, RegistrationScopeRepository>();
        services.AddScoped<IEventSessionKindRepository, EventSessionKindRepository>();
        services.AddScoped<IScheduleItemKindRepository, ScheduleItemKindRepository>();
        services.AddScoped<IMadhabRepository, MadhabRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IOrganizationPositionRepository, OrganizationPositionRepository>();
        services.AddScoped<IGroupPositionRepository, GroupPositionRepository>();
        services.AddScoped<IActorTypeRepository, ActorTypeRepository>();
        services.AddScoped<IDidCustodyTypeRepository, DidCustodyTypeRepository>();
        services.AddScoped<IFileTypeRepository, FileTypeRepository>();
        services.AddScoped<INotificationTypeRepository, NotificationTypeRepository>();
        services.AddScoped<INotificationEntityTypeRepository, NotificationEntityTypeRepository>();

        // Multi-tenancy Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantLookupSource, TenantLookupSource>();
        services.AddScoped<IInstanceBootstrapStateRepository, InstanceBootstrapStateRepository>();
        services.AddScoped<IPlatformUserRoleRepository, PlatformUserRoleRepository>();
        services.AddScoped<ITenantUserRepository, TenantUserRepository>();
        services.AddScoped<ITenantUserProfileRepository, TenantUserProfileRepository>();
        services.AddScoped<ITenantUserRoleGrantRepository, TenantUserRoleGrantRepository>();
        services.AddScoped<ITenantOnboardingStateRepository, TenantOnboardingStateRepository>();
        services.AddScoped<ITenantNavigationLinkRepository, TenantNavigationLinkRepository>();
        services.AddScoped<IFooterLinkGroupRepository, FooterLinkGroupRepository>();
        services.AddScoped<IFooterLinkRepository, FooterLinkRepository>();
        services.AddScoped<ITenantInvitationRepository, TenantInvitationRepository>();
        services.AddScoped<ITenantLifecycleLogRepository, TenantLifecycleLogRepository>();
        services.AddScoped<ITenantPlanRepository, TenantPlanRepository>();

        // User & Authentication Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExternalApiKeyRepository, ExternalApiKeyRepository>();
        services.AddScoped<IManagedControlPlaneRegistrationRepository, ManagedControlPlaneRegistrationRepository>();
        services.AddScoped<IManagedTenantProvisioningOperationRepository, ManagedTenantProvisioningOperationRepository>();
        services.AddScoped<IExternalApiKeyQuotaRepository, ExternalApiKeyQuotaRepository>();
        services.AddScoped<IUserNotificationPreferenceRepository, UserNotificationPreferenceRepository>();
        services.AddScoped<IUserAuthenticationTokenRepository, UserAuthenticationTokenRepository>();
        services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
        services.AddScoped<IExternalBindingRepository, ExternalBindingRepository>();

        // Actor Repositories
        services.AddScoped<IActorRepository, ActorRepository>();
        services.AddScoped<IActorKeyStoreRepository, ActorKeyStoreRepository>();
        services.AddScoped<IActorSubscriptionRepository, ActorSubscriptionRepository>();
        services.AddScoped<INotificationFanoutRunRepository, NotificationFanoutRunRepository>();
        services.AddScoped<INotificationFanoutOccurrenceRepository, NotificationFanoutOccurrenceRepository>();

        // Organization Repositories
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationReviewRepository, OrganizationReviewRepository>();

        // Group Repositories
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
        services.AddScoped<ICustomPropertyDefinitionRepository, CustomPropertyDefinitionRepository>();

        // Event Repositories
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventHeavyRedactionRepository, EventHeavyRedactionRepository>();
        services.AddScoped<IEventModerationRecordRepository, EventModerationRecordRepository>();
        services.AddScoped<IEventReportRepository, EventReportRepository>();
        services.AddScoped<IEventSessionRepository, EventSessionRepository>();
        services.AddScoped<IEventSessionGroupRepository, EventSessionGroupRepository>();
        services.AddScoped<IEventSessionGroupSessionRepository, EventSessionGroupSessionRepository>();
        services.AddScoped<IEventSessionIslamicAspectRepository, EventSessionIslamicAspectRepository>();
        services.AddScoped<IEventRegistrationRepository, EventRegistrationRepository>();
        services.AddScoped<IEventRegistrationIntentRepository, EventRegistrationIntentRepository>();
        services.AddScoped<IEventDayRepository, EventDayRepository>();
        services.AddScoped<IEventAgendaItemRepository, EventAgendaItemRepository>();
        services.AddScoped<IEventSessionAgendaItemRepository, EventSessionAgendaItemRepository>();
        services.AddScoped<IEventSessionLanguageRepository, EventSessionLanguageRepository>();
        services.AddScoped<IEventSessionSpeakerRepository, EventSessionSpeakerRepository>();

        // Event Custom Property Repositories
        services.AddScoped<IEventTemplateRepository, EventTemplateRepository>();
        services.AddScoped<IEventCustomPropertyRepository, EventCustomPropertyRepository>();

        // Event Session Custom Property Repositories
        services.AddScoped<IEventSessionTemplateRepository, EventSessionTemplateRepository>();
        services.AddScoped<IEventSessionCustomPropertyRepository, EventSessionCustomPropertyRepository>();

        // Custom Property Projection Coordination
        services.AddScoped<ICustomPropertyProjectionStatusRepository, CustomPropertyProjectionStatusRepository>();
        services.AddScoped<ICustomPropertyProjectionDirtyScopeRepository, CustomPropertyProjectionDirtyScopeRepository>();
        services.AddScoped<ICustomPropertyQuotaResolver, Services.CustomPropertyQuotaResolver>();
        services.AddScoped<IEventCustomPropertyProjectionUpdater, Projections.EventCustomPropertyProjectionUpdater>();
        services.AddScoped<IEventSessionCustomPropertyProjectionUpdater, Projections.EventSessionCustomPropertyProjectionUpdater>();

        // Custom Property Projection Query Repositories
        services.AddScoped<IEventCustomPropertyProjectionRepository, EventCustomPropertyProjectionRepository>();
        services.AddScoped<IEventSessionCustomPropertyProjectionRepository, EventSessionCustomPropertyProjectionRepository>();
        services.AddScoped<IEventAggregateViewRepository, EventAggregateViewRepository>();
        services.AddScoped<ICustomPropertyGovernanceRepository, CustomPropertyGovernanceRepository>();

        // Event Aspect Repositories
        services.AddScoped<IEventIslamicAspectRepository, EventIslamicAspectRepository>();
        services.AddScoped<IEventTechAspectRepository, EventTechAspectRepository>();
        services.AddScoped<IEventSeriesRepository, EventSeriesRepository>();
        services.AddScoped<IEventContactShareConsentRepository, EventContactShareConsentRepository>();
        services.AddScoped<IEventContactShareExportRepository, EventContactShareExportRepository>();

        // Location Repository
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ILocationRoomRepository, LocationRoomRepository>();
        services.AddScoped<IEventLocationRepository, EventLocationRepository>();
        services.AddScoped<IEventLocationDisclosureAuditRepository, EventLocationDisclosureAuditRepository>();
        services.AddScoped<IEventLocationExactReadAuditRepository, EventLocationExactReadAuditRepository>();
        services.AddScoped<ILocationPrivacyErasureReplayCheckpointRepository, LocationPrivacyErasureReplayCheckpointRepository>();

        // Storage Repository
        services.AddScoped<IStorageObjectRepository, StorageObjectRepository>();
        services.AddScoped<IStorageUploadSessionRepository, StorageUploadSessionRepository>();
        services.AddScoped<IStorageUsageCounterRepository, StorageUsageCounterRepository>();

        // Tag & Category Repositories
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITagTypeRepository, TagTypeRepository>();
        services.AddScoped<ITagTypeTagsRepository, TagTypeTagsRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryTypeRepository, CategoryTypeRepository>();
        services.AddScoped<ICategoryTypeCategoriesRepository, CategoryTypeCategoriesRepository>();
        services.AddScoped<IEventTagsRepository, EventTagsRepository>();
        services.AddScoped<IEventCategoriesRepository, EventCategoriesRepository>();

        // ATProto/Federation Repositories
        services.AddScoped<IAtprotoRecordRepository, AtprotoRecordRepository>();
        services.AddScoped<IAtprotoJetstreamRepository, AtprotoJetstreamRepository>();
        services.AddScoped<IIndexedDidRepository, IndexedDidRepository>();
        services.AddScoped<ISyncStateRepository, SyncStateRepository>();

        // Settings Repositories
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<ITenantSettingRepository, TenantSettingRepository>();
        services.AddScoped<ITenantSettingsDocumentRepository, TenantSettingsDocumentRepository>();
        services.AddScoped<IOrganizationSettingRepository, OrganizationSettingRepository>();
        services.AddScoped<IGroupSettingRepository, GroupSettingRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<INotificationChannelPreferenceRepository, NotificationChannelPreferenceRepository>();
        services.AddScoped<INotificationPreferenceProfileRepository, NotificationPreferenceProfileRepository>();
        services.AddScoped<IWebPushSubscriptionRepository, WebPushSubscriptionRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<ISecretBindingRepository, SecretBindingRepository>();
        services.AddScoped<IUiThemeRepository, UiThemeRepository>();
        services.AddScoped<IUiThemePresetRepository, UiThemePresetRepository>();
        services.AddScoped<IUserAppearanceProfileRepository, UserAppearanceProfileRepository>();
        services.AddScoped<IUserAppearancePreferenceRepository, UserAppearancePreferenceRepository>();

        // Module Governance Repositories
        services.AddScoped<IModuleDefinitionRepository, ModuleDefinitionRepository>();
        services.AddScoped<ITenantCapabilityRepository, TenantCapabilityRepository>();

        // PDS Synchronization Repositories
        services.AddScoped<IPdsSyncOutboxRepository, PdsSyncOutboxRepository>();

        // Generic Outbox Repositories
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IEmailDispatchOutboxRepository, EmailDispatchOutboxRepository>();
        services.AddScoped<IEmailDispatchEligibilityEvaluator, EmailDispatchEligibilityEvaluator>();
        services.AddScoped<IWebPushDispatchOutboxRepository, WebPushDispatchOutboxRepository>();
        services.AddScoped<IIntegrationSyncOutboxRepository, IntegrationSyncOutboxRepository>();

        // Webhook Repositories
        services.AddScoped<IWebhookConsumerRepository, WebhookConsumerRepository>();
        services.AddScoped<IWebhookConsumerProviderBindingRepository, WebhookConsumerProviderBindingRepository>();
        services.AddScoped<IWebhookEventTypeRepository, WebhookEventTypeRepository>();
        services.AddScoped<IWebhookEndpointRepository, WebhookEndpointRepository>();
        services.AddScoped<IWebhookMessageRepository, WebhookMessageRepository>();
        services.AddScoped<IWebhookDeliveryAttemptRepository, WebhookDeliveryAttemptRepository>();
        services.AddScoped<IWebhookLocalTargetRepository, WebhookLocalTargetRepository>();
        services.AddScoped<IWebhookBulkReplayRepository, WebhookBulkReplayRepository>();
        services.AddScoped<IWebhookProviderPublicationRepository, WebhookProviderPublicationRepository>();
        services.AddScoped<IWebhookDeliveryPlanMaterializer, WebhookDeliveryPlanMaterializer>();
        services.AddScoped<IIncomingWebhookMessageRepository, IncomingWebhookMessageRepository>();
        services.AddScoped<IIncomingWebhookEffectOutboxRepository, IncomingWebhookEffectOutboxRepository>();
        services.AddScoped<IIncomingWebhookEffectReceiptRepository, IncomingWebhookEffectReceiptRepository>();
        services.AddScoped<IWebhookRetentionCleanupRepository, WebhookRetentionCleanupRepository>();

        // Authorization (RBAC) Repositories
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IEventRoleAssignmentRepository, EventRoleAssignmentRepository>();
        services.AddScoped<IEventAuthoritySnapshotService, Services.EventAuthoritySnapshotService>();

        // Configuration Audit Repositories
        services.AddScoped<IConfigurationChangeLogRepository, ConfigurationChangeLogRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IWebhookAuditEventRepository, WebhookAuditEventRepository>();
        services.AddScoped<ISupportAccessSessionRepository, SupportAccessSessionRepository>();
        services.AddScoped<ISupportAccessAuditEventRepository, SupportAccessAuditEventRepository>();

        // Governance Policy Resolver (deterministic hierarchy walk: Instance → Tenant → Organization)
        services.AddScoped<IPolicyResolver, Services.PolicyResolver>();
        services.AddScoped<INotificationPreferenceResolver, Services.NotificationPreferenceResolver>();

        // Notification Repository
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationIntentRepository, NotificationIntentRepository>();
        services.AddScoped<IRecipientNotificationGraphRepository, NotificationIntentRepository>();

        // Idempotency Repository
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

        // AI Assistant Repositories
        services.AddScoped<IAiConversationRepository, AiConversationRepository>();
        services.AddScoped<IAiConsentGrantRepository, AiConsentGrantRepository>();

        return services;
    }

    private static bool IsEnabled(string? value)
    {
        return bool.TryParse(value, out var enabled) && enabled;
    }
}
