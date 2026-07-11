// ABOUTME: DI registration for the secret resolution pipeline (resolver + sources + decorator + health).
// ABOUTME: Composition root for Phase 3 - called by Explore.API and Explore.Blazor Program.cs.

namespace Explore.Secrets.Extensions;

using Explore.Application.Contracts.Secrets;
using Explore.Secrets.HealthChecks;
using Explore.Secrets.Infrastructure;
using Explore.Secrets.Observability;
using Explore.Secrets.Services;
using Explore.Secrets.Sources;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Registers the secret resolution pipeline.
/// </summary>
public static class SecretResolutionServiceCollectionExtensions
{
    /// <summary>
    /// Name used for the secret-resolver health check registration.
    /// </summary>
    public const string HealthCheckName = "secret-resolver";

    /// <summary>
    /// Wires the full Phase 3 pipeline: sources, resolver, auditing decorator,
    /// metrics meter, and health check. Safe to call multiple times (uses TryAdd).
    /// </summary>
    /// <remarks>
    /// Ordering matters: <see cref="SecretResolver"/> is registered as the concrete
    /// implementation, then <see cref="AuditingSecretResolverDecorator"/> is bound
    /// as the public <see cref="ISecretResolver"/>. This is a hand-rolled decorator
    /// registration to avoid pulling in Scrutor just for one call site.
    /// </remarks>
    public static IServiceCollection AddSecretResolution(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ---- Infrastructure ---------------------------------------------------
        services.AddMemoryCache();
        services.TryAddSingleton<IInfisicalClientFactory, InfisicalClientFactory>();
        services.TryAddSingleton<IInlineSecretProtector, InlineSecretProtector>();

        // ---- Metrics ---------------------------------------------------------
        // IMeterFactory is registered by AddMetrics() (part of the default host).
        services.AddMetrics();
        services.TryAddSingleton<SecretResolverMetrics>();

        // ---- Sources (one per SourceType) ------------------------------------
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretSource, EnvironmentSecretSource>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretSource, InlineSecretSource>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretSource, InfisicalSecretSource>());

        // ---- Resolver + Decorator --------------------------------------------
        // SecretResolver is the concrete inner implementation; the decorator owns
        // the public ISecretResolver contract so every consumer gets audit logging.
        services.TryAddScoped<SecretResolver>();
        services.TryAddScoped<ISecretResolver>(sp => new AuditingSecretResolverDecorator(
            inner: sp.GetRequiredService<SecretResolver>(),
            logger: sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AuditingSecretResolverDecorator>>()));

        // ---- Health check ----------------------------------------------------
        services.AddHealthChecks()
            .AddCheck<SecretResolverHealthCheck>(
                name: HealthCheckName,
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "secrets", "infrastructure"]);

        return services;
    }
}
