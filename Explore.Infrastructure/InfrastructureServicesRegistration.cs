using Amazon;
using Amazon.S3;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Infrastructure.Identity;
using Explore.Infrastructure.Mail;
using Explore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure
{
    public static class InfrastructureServicesRegistration
    {
        public static IServiceCollection ConfigureInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddTransient<IEmailSender, EmailSender>();

            services.Configure<S3Settings>(configuration.GetSection("S3Settings"));

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var s3Options = sp.GetRequiredService<IOptions<S3Settings>>().Value;
                var config = new AmazonS3Config
                {
                    ForcePathStyle = true // needed for some S3 compatible services like MinIO
                };

                // Set RegionEndpoint if Region is provided
                if (!string.IsNullOrWhiteSpace(s3Options.Region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3Options.Region);
                }
                
                // Set ServiceURL if Endpoint is provided (for S3-compatible services like MinIO)
                if (!string.IsNullOrWhiteSpace(s3Options.Endpoint))
                {
                    config.ServiceURL = s3Options.Endpoint;
                    // When using custom endpoint, we may not need RegionEndpoint
                    // but AWS SDK requires at least one of them
                    if (config.RegionEndpoint == null)
                    {
                        // Default to us-east-1 for S3-compatible services
                        config.RegionEndpoint = RegionEndpoint.USEast1;
                    }
                }
                else if (config.RegionEndpoint == null)
                {
                    // Fallback: if neither Region nor Endpoint is set, use a sensible default
                    // This prevents the "No RegionEndpoint or ServiceURL configured" error
                    config.RegionEndpoint = RegionEndpoint.EUWest1;
                    Console.WriteLine("[S3] Warning: No Region or Endpoint configured in S3Settings. Using default region eu-west-1.");
                }

                return new AmazonS3Client(s3Options.AccessKeyId, s3Options.SecretAccessKey, config);
            });

            services.AddTransient<IObjectStorageService, ObjectStorageService>();

            // Identity services
            services.AddScoped<IUserContext, UserContext>();

            return services;
        }
    }
}
