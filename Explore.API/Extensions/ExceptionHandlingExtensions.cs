// ABOUTME: Registers and wires API exception handling services and middleware.
// ABOUTME: Configures chained IExceptionHandler implementations with ProblemDetails.

using Explore.API.ExceptionHandling;

namespace Explore.API.Extensions;

public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddApiExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
                context.ProblemDetails.Extensions["correlationId"] =
                    context.HttpContext.Items["CorrelationId"] as string;
            };
        });

        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        return app;
    }
}
