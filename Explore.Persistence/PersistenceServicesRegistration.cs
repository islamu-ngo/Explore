using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
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
            services.AddDbContext<ExploreDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ExploreDB");
                options.UseNpgsql(connectionString);
            });

            //services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            //services.AddScoped<IBlogRepository, BlogRepository>();
            //services.AddScoped<IBlogLikeRepository, BlogLikeRepository>();
            //services.AddScoped<IBlogCategoryMappingRepository, BlogCategoryMappingRepository>();
            //services.AddScoped<IBlogTagMappingRepository, BlogTagMappingRepository>();
            //services.AddScoped<ITagRepository, TagRepository>();
            //services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
            //services.AddScoped<ICommentRepository, CommentRepository>();
            //services.AddScoped<ICategoryRepository, CategoryRepository>();
            //services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            //services.AddScoped<IUserSalahActivityRepository, UserSalahActivityRepository>();
            //services.AddScoped<IUserSalahOverviewRepository, UserSalahOverviewRepository>();
            //services.AddScoped<IUserDhikrActivityRepository, UserDhikrActivityRepository>();
            //services.AddScoped<IUserDhikrOverviewRepository, UserDhikrOverviewRepository>();
            //services.AddScoped<ISalahTypeRepository, SalahTypeRepository>();
            //services.AddScoped<IDhikrTypeRepository, DhikrTypeRepository>();
            //services.AddScoped<IProfilePictureTypeRepository, ProfilePictureTypeRepository>();
            //services.AddScoped<IPermissionTypeRepository, PermissionTypeRepository>();
            //services.AddScoped<IRoleTypeRepository, RoleTypeRepository>();
            //services.AddScoped<IBlobFileRepository, BlobFileRepository>();
            //services.AddScoped<IUserAccountRoleTypeMappingRepository, UserAccountRoleTypeMappingRepository>();
            //services.AddScoped<IRoleTypePermissionTypeMappingRepository, RoleTypePermissionTypeMappingRepository>();

            return services;
        }
    }
}