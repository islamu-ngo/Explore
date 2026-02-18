// ABOUTME: Configures API versioning using media type versioning strategy.
// ABOUTME: Clients specify version via Accept header: application/json;v=0.1 or application/hal+json;v=0.1.

using Asp.Versioning;

namespace Explore.API.Extensions;

/// <summary>
/// Configures API versioning using media type versioning.
/// Clients specify the desired version in the Accept header parameter:
///   Accept: application/json;v=0.1
///   Accept: application/hal+json;v=0.1
///
/// When no version is specified, defaults to the latest (current) version.
/// This approach keeps URLs clean (no /v{n}/ segments) while providing
/// explicit version negotiation through standard HTTP content negotiation.
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiMediaTypeVersioning(this IServiceCollection services)
    {
        var builder = new MediaTypeApiVersionReaderBuilder();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(0, 1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = builder
                .Parameter("v")
                .Include("application/json")
                .Include("application/hal+json")
                .Build();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
