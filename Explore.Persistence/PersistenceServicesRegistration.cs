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

            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

            services.AddScoped<IAudienceAgeRepository, AudienceAgeRepository>();
            services.AddScoped<IAudienceGenderRepository, AudienceGenderRepository>();
            services.AddScoped<IEducationRepository, EducationRepository>();
            services.AddScoped<IEducationTypeRepository, EducationTypeRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IEventTypeRepository, EventTypeRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
            services.AddScoped<IProgramRepository, ProgramRepository>();
            services.AddScoped<IProgramRegistrationRepository, ProgramRegistrationRepository>();
            services.AddScoped<IProgramTypeRepository, ProgramTypeRepository>();
            services.AddScoped<IStatusTypeRepository, StatusTypeRepository>();
            services.AddScoped<IStorageObjectRepository, StorageObjectRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}