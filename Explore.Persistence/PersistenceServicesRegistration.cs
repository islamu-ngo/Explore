using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence
{
    public static class PersistenceServicesRegistration
    {
        // didn't implement aspire integration trough passing the builder cause it will require to install aspnetcore nuget package in persistence project and i want to keep it clean so let this dependency in API project only
        //public static IServiceCollection ConfigurePersistenceServices(this IServiceCollection services,
        //    WebApplicationBuilder builder) // Pass the builder instead of just configuration
        //{
        //    // Use Aspire's integration
        //    builder.AddNpgsqlDbContext<ExploreDbContext>("ExploreDB");

        public static IServiceCollection CongfigurePersistenceServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:DefaultConnection"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in configuration.");
            }
            services.AddDbContext<ExploreDbContext>(options =>
            {
                options.UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention()
                    .EnableSensitiveDataLogging() //temporarly to resolve bug! TODO remove in prod
                    .EnableDetailedErrors();
            });

            // Generic Repository
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

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
            services.AddScoped<IOrganizationRoleRepository, OrganizationRoleRepository>();
            services.AddScoped<IOrganizationPositionRepository, OrganizationPositionRepository>();
            services.AddScoped<IActorTypeRepository, ActorTypeRepository>();
            services.AddScoped<IDidCustodyTypeRepository, DidCustodyTypeRepository>();
            services.AddScoped<IFileTypeRepository, FileTypeRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();

            // Multi-tenancy Repositories
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<ITenantUserRepository, TenantUserRepository>();
            services.AddScoped<ITenantSettingsRepository, TenantSettingsRepository>();

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

            // Event Repositories
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IEventSessionRepository, EventSessionRepository>();
            services.AddScoped<IEventRegistrationRepository, EventRegistrationRepository>();
            services.AddScoped<IEventSessionAgendaItemRepository, EventSessionAgendaItemRepository>();
            services.AddScoped<IEventSessionLanguageRepository, EventSessionLanguageRepository>();
            services.AddScoped<IEventSessionSpeakerRepository, EventSessionSpeakerRepository>();

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

            return services;
        }
    }
}
