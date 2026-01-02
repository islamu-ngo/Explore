using Amazon;
using Amazon.S3;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
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
                    RegionEndpoint = RegionEndpoint.GetBySystemName(s3Options.Region),
                    ServiceURL = s3Options.Endpoint,
                    ForcePathStyle = true // needed for some S3 compatible services
                };

                return new AmazonS3Client(s3Options.AccessKeyId, s3Options.SecretAccessKey, config);
            });

            services.AddTransient<IObjectStorageService, ObjectStorageService>();

            return services;
        }
    }
}
