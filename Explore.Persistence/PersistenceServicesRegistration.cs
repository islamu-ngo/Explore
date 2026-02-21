using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Persistence.Caching;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Persistence;

public static class PersistenceServicesRegistration
{
    // didn't implement aspire integration trough passing the builder cause it will require to install aspnetcore nuget package in persistence project and i want to keep it clean so let this dependency in API project only
    //public static IServiceCollection ConfigurePersistenceServices(this IServiceCollection services,
    //    WebApplicationBuilder builder) // Pass the builder instead of just configuration
    //{
    //    // Use Aspire's integration
    //    builder.AddNpgsqlDbContext<ExploreDbContext>("ExploreDB");

    public static IServiceCollection CongfigurePersistenceServices(this IServiceCollection services,
        IConfiguration configuration, bool skipDbContextRegistration = false)
    {
        // Skip DbContext registration when running integration tests (they register their own)
        if (!skipDbContextRegistration)
        {
            var connectionString = configuration["ConnectionStrings:DefaultConnection"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in configuration.");
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

                var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"];
                if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    options.EnableDetailedErrors();
                }
            });

            // Register scoped DbContext that sets scoped dependencies from DI
            // This follows EF Core's recommended pattern for pooled contexts with scoped dependencies
            services.AddScoped(sp =>
            {
                var factory = sp.GetRequiredService<IDbContextFactory<ExploreDbContext>>();
                var context = factory.CreateDbContext();

                // Set scoped dependencies via property injection (null during migrations, populated during API requests)
                context.TenantContext = sp.GetService<ITenantContext>();
                context.CurrentUserService = sp.GetService<ICurrentUserService>();

                return context;
            });
        }

        // Generic Repository
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        // Lookup cache
        services.AddSingleton<ILookupDataCache, LookupDataCache>();
        services.AddHostedService<LookupDataCacheInitializer>();

        // Lookup Table Repositories
        services.AddScoped<IApprovalStatusRepository, ApprovalStatusRepository>();
        services.AddScoped<IAudienceAgeRepository, AudienceAgeRepository>();
        services.AddScoped<IAudienceGenderRepository, AudienceGenderRepository>();
        services.AddScoped<IEventTypeRepository, EventTypeRepository>();
        services.AddScoped<IEventStatusRepository, EventStatusRepository>();
        services.AddScoped<IEventFormatRepository, EventFormatRepository>();
        services.AddScoped<IVisibilityTypeRepository, VisibilityTypeRepository>();
        services.AddScoped<IRegistrationModeRepository, RegistrationModeRepository>();
        services.AddScoped<IMadhabRepository, MadhabRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IOrganizationPositionRepository, OrganizationPositionRepository>();
        services.AddScoped<IActorTypeRepository, ActorTypeRepository>();
        services.AddScoped<IDidCustodyTypeRepository, DidCustodyTypeRepository>();
        services.AddScoped<IFileTypeRepository, FileTypeRepository>();

        // Multi-tenancy Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantUserRepository, TenantUserRepository>();
        services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();
        services.AddScoped<IInstanceBootstrapStateRepository, InstanceBootstrapStateRepository>();
        services.AddScoped<IPlatformUserRoleRepository, PlatformUserRoleRepository>();
        services.AddScoped<ITenantMemberRepository, TenantMemberRepository>();
        services.AddScoped<ITenantOnboardingStateRepository, TenantOnboardingStateRepository>();
        services.AddScoped<ITenantNavigationLinkRepository, TenantNavigationLinkRepository>();
        services.AddScoped<ITenantInvitationRepository, TenantInvitationRepository>();
        services.AddScoped<ITenantLifecycleLogRepository, TenantLifecycleLogRepository>();

        // User & Authentication Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserAuthenticationTokenRepository, UserAuthenticationTokenRepository>();
        services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();

        // Actor Repositories
        services.AddScoped<IActorRepository, ActorRepository>();
        services.AddScoped<IActorKeyStoreRepository, ActorKeyStoreRepository>();

        // Organization Repositories
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationReviewRepository, OrganizationReviewRepository>();

        // Group Repositories
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();

        // Event Repositories
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventSessionRepository, EventSessionRepository>();
        services.AddScoped<IEventRegistrationRepository, EventRegistrationRepository>();
        services.AddScoped<IEventSessionAgendaItemRepository, EventSessionAgendaItemRepository>();
        services.AddScoped<IEventSessionLanguageRepository, EventSessionLanguageRepository>();
        services.AddScoped<IEventSessionSpeakerRepository, EventSessionSpeakerRepository>();

        // Event Aspect Repositories
        services.AddScoped<IEventIslamicAspectRepository, EventIslamicAspectRepository>();
        services.AddScoped<IEventTechAspectRepository, EventTechAspectRepository>();

        // Location Repository
        services.AddScoped<ILocationRepository, LocationRepository>();

        // Storage Repository
        services.AddScoped<IStorageObjectRepository, StorageObjectRepository>();

        // Tag & Category Repositories
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITagTypeRepository, TagTypeRepository>();
        services.AddScoped<ITagTypeTagsRepository, TagTypeTagsRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IEventTagsRepository, EventTagsRepository>();
        services.AddScoped<IEventCategoriesRepository, EventCategoriesRepository>();

        // ATProto/Federation Repositories
        services.AddScoped<IAtprotoRecordRepository, AtprotoRecordRepository>();
        services.AddScoped<IIndexedDidRepository, IndexedDidRepository>();
        services.AddScoped<ISyncStateRepository, SyncStateRepository>();

        // Settings Repositories
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<ITenantSettingRepository, TenantSettingRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();

        // Module Governance Repositories
        services.AddScoped<IModuleDefinitionRepository, ModuleDefinitionRepository>();
        services.AddScoped<ITenantCapabilityRepository, TenantCapabilityRepository>();

        // PDS Synchronization Repositories
        services.AddScoped<IPdsSyncOutboxRepository, PdsSyncOutboxRepository>();

        // Authorization (RBAC) Repositories
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        // Configuration Audit Repositories
        services.AddScoped<IConfigurationChangeLogRepository, ConfigurationChangeLogRepository>();

        return services;
    }
}
