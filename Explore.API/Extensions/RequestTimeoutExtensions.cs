// ABOUTME: Registers named request timeout policies for different endpoint categories.
// ABOUTME: Provides default (30s), lookup (10s), and complex (60s) timeout tiers.

using Microsoft.AspNetCore.Http.Timeouts;

namespace Explore.API.Extensions;

/// <summary>
/// Configures per-endpoint request timeout policies.
/// - Default: 30 seconds for standard CRUD operations
/// - Lookup: 10 seconds for simple key-value lookups (categories, tags, types)
/// - Complex: 60 seconds for search, file upload, and report generation
///
/// All timeouts are configurable via appsettings.json under "RequestTimeouts".
/// The middleware triggers <see cref="OperationCanceledException"/> on expiry,
/// which the global exception handler converts to 504 Gateway Timeout.
/// </summary>
public static class RequestTimeoutExtensions
{
    public const string DefaultPolicy = "Default";
    public const string LookupPolicy = "Lookup";
    public const string ComplexPolicy = "Complex";

    public static IServiceCollection AddApiRequestTimeouts(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RequestTimeouts");

        var defaultSeconds = section.GetValue("DefaultSeconds", 30);
        var lookupSeconds = section.GetValue("LookupSeconds", 10);
        var complexSeconds = section.GetValue("ComplexSeconds", 60);

        services.AddRequestTimeouts(options =>
        {
            var defaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(defaultSeconds),
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
            };

            options.DefaultPolicy = defaultPolicy;
            options.AddPolicy(DefaultPolicy, defaultPolicy);

            options.AddPolicy(LookupPolicy, new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(lookupSeconds),
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
            });

            options.AddPolicy(ComplexPolicy, new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(complexSeconds),
                TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
            });
        });

        return services;
    }
}
