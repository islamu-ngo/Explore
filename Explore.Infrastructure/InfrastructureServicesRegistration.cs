using Amazon;
using Amazon.S3;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Strategies;
using Explore.Application.Models;
using Explore.Infrastructure.Identity;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Services;
using Explore.Infrastructure.Services.Federation;
using Explore.Infrastructure.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        services.Configure<S3Settings>(configuration.GetSection("S3Settings"));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var s3Options = sp.GetRequiredService<IOptions<S3Settings>>().Value;

            if (string.IsNullOrWhiteSpace(s3Options.AccessKeyId))
            {
                throw new InvalidOperationException("S3 AccessKeyId is not configured (S3Settings:AccessKeyId).");
            }
            if (string.IsNullOrWhiteSpace(s3Options.SecretAccessKey))
            {
                throw new InvalidOperationException("S3 SecretAccessKey is not configured (S3Settings:SecretAccessKey).");
            }

            var config = new AmazonS3Config
            {
                ForcePathStyle = true
            };

            if (!string.IsNullOrWhiteSpace(s3Options.Endpoint))
            {
                var endpoint = s3Options.Endpoint.Trim();
                if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = $"https://{endpoint}";
                }

                config.ServiceURL = endpoint;
                config.UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                config.AuthenticationRegion = string.IsNullOrWhiteSpace(s3Options.Region) ? "fsn1" : s3Options.Region;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(s3Options.Region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3Options.Region);
                }
                else
                {
                    config.RegionEndpoint = RegionEndpoint.EUWest1;
                    Console.WriteLine("[S3] Warning: No Region or Endpoint configured in S3Settings. Using default region eu-west-1.");
                }
            }

            return new AmazonS3Client(s3Options.AccessKeyId, s3Options.SecretAccessKey, config);
        });

        services.AddTransient<IObjectStorageService, ObjectStorageService>();

        // Identity services
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Memory cache for settings and module governance
        services.AddMemoryCache();

        // Settings and Module Governance services
        services.AddScoped<ISettingsResolver, SettingsResolver>();
        services.AddScoped<IModuleService, ModuleService>();

        // Admin context (hybrid JWT + database identity resolution)
        services.AddScoped<IAdminContext, AdminContext>();

        // Configuration audit logging
        services.AddScoped<IConfigurationChangeLogService, ConfigurationChangeLogService>();

        // Cerbos authorization (conditional: real Cerbos PDP or fallback)
        services.Configure<CerbosSettings>(configuration.GetSection(CerbosSettings.SectionName));

        var cerbosEnabled = configuration.GetValue<bool>("Cerbos:Enabled");
        if (cerbosEnabled)
        {
            services.AddHttpClient("CerbosClient", client =>
            {
                var endpoint = configuration["Cerbos:Endpoint"] ?? "http://localhost:3592";
                client.BaseAddress = new Uri(endpoint);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            services.AddScoped<ICerbosAuthorizationService, CerbosAuthorizationService>();
        }
        else
        {
            services.AddScoped<ICerbosAuthorizationService, FallbackAuthorizationService>();
        }

        // Event Strategies
        services.AddScoped<IEventStrategy, IslamicEventStrategy>();
        services.AddScoped<IEventStrategy, TechEventStrategy>();
        services.AddScoped<IStrategyResolver, StrategyResolver>();

        // PDS Synchronization services
        services.Configure<PdsSyncSettings>(configuration.GetSection(PdsSyncSettings.SectionName));
        services.AddHttpClient("PdsService");
        services.AddScoped<IPdsService, PdsService>();

        // Deployment mode configuration (single-tenant vs multi-tenant)
        services.Configure<DeploymentSettings>(configuration.GetSection(DeploymentSettings.SectionName));

        return services;
    }
}
