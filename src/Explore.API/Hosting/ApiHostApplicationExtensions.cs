// ABOUTME: Applies the reusable ordered API middleware pipeline and maps API-owned endpoints.
// ABOUTME: Preserves OpenAPI, MCP, controllers, health routes, caching, tenancy, and authorization order.

using Explore.API.Configuration;
using Explore.API.Extensions;
using Explore.API.Mcp;
using Explore.API.Middleware;
using Explore.ServiceDefaults.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using Scalar.AspNetCore;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Explore.API.Hosting;

public static class ApiHostApplicationExtensions
{
    public static WebApplication UseApiHostMiddleware(
        this WebApplication app,
        ApiHostCompositionState state)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(state);

        if (app.Environment.IsDevelopment() ||
            app.Environment.IsEnvironment("Testing") ||
            state.IsOpenApiGeneration)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
                options.SwaggerEndpoint("/swagger/v0.1/swagger.json", "Explore API v0.1"));
        }

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            app.UseCors("DevPolicy");
        }
        else
        {
            app.UseCors("InternalAppPolicy");
            app.UseHsts();
        }

        app.UseApiExceptionHandling();
        app.UseForwardedHeaders();
        app.UseSecurityHeaders();
        app.UseCorrelationId();
        app.UseRequestLogging();
        app.UseResponseCompression();
        if (state.HttpsRedirectionEnabled)
        {
            app.UseHttpsRedirection();
        }

        app.UseHateoas();
        app.UseRouting();
        app.UseMiddleware<ApiTenantResolutionMiddleware>();
        app.UseRequestTimeouts();
        app.UseMiddleware<ApiAuthenticationConflictMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<ApiTenantPostAuthenticationMiddleware>();
        app.UseMiddleware<McpRuntimeGateMiddleware>();
        app.UseRequestLocalization();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.UseMiddleware<SupportAccessAuditMiddleware>();
        if (!state.IsOpenApiGeneration &&
            state.UseTickerQEmailDispatch &&
            TickerQSchedulerExtensions.IsTickerQSchedulerEnabled(app.Configuration, app.Environment))
        {
            app.UseApiTickerQScheduler();
        }

        app.UseOutputCache();
        app.UseETag();
        return app;
    }

    public static WebApplication MapApiHostEndpoints(
        this WebApplication app,
        ApiHostCompositionState state)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(state);

        if (app.Environment.IsDevelopment() ||
            app.Environment.IsEnvironment("Testing") ||
            state.IsOpenApiGeneration)
        {
            app.MapOpenApi().DisableRequestTimeout();
            app.MapScalarApiReference();
        }

        app.MapControllers();
        var mcpAdapterSettings = app.Services.GetRequiredService<IOptions<McpAdapterSettings>>().Value;
        if (mcpAdapterSettings.Enabled && !state.IsOpenApiGeneration)
        {
            app.MapMcp(mcpAdapterSettings.EndpointPath).AllowAnonymous();
        }

        app.MapDefaultEndpoints();
        app.MapHealthChecks(
            "/health/webhooks/local",
            CreateWebhookReadinessOptions("webhook-local-readiness"));
        app.MapHealthChecks(
            "/health/webhooks/svix",
            CreateWebhookReadinessOptions("webhook-svix-readiness"));
        app.MapHealthChecks(
            "/health/webhooks/coop-effects",
            CreateWebhookReadinessOptions("webhook-coop-effect-readiness"));
        return app;
    }

    private static HealthCheckOptions CreateWebhookReadinessOptions(string tag) => new()
    {
        Predicate = registration => registration.Tags.Contains(tag),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = Status200OK,
            [HealthStatus.Degraded] = Status200OK,
            [HealthStatus.Unhealthy] = Status503ServiceUnavailable
        }
    };
}
