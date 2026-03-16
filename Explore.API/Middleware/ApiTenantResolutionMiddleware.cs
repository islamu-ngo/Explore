// ABOUTME: Resolves tenant identity authoritatively inside the API host from trusted forwarded context.
// ABOUTME: Uses slug and host hints to set the shared tenant accessor before application code touches tenant-scoped data.

using Explore.Application.Constants;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Explore.API.Middleware;

public sealed class ApiTenantResolutionMiddleware
{
    private static readonly Guid FallbackDefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    internal const string RequestedTenantIdItemKey = "__requested_tenant_id";

    /// <summary>
    /// Cached deployment mode from InstanceBootstrapState. Once onboarding completes
    /// and sets a deployment mode, the middleware uses this instead of re-querying the DB.
    /// Null = not yet checked; empty = checked but no completed onboarding found.
    /// </summary>
    private static volatile string? _cachedBootstrapDeploymentMode;

    private readonly RequestDelegate _next;
    private readonly DeploymentSettings _deploymentSettings;

    public ApiTenantResolutionMiddleware(RequestDelegate next, IOptions<DeploymentSettings> deploymentSettings)
    {
        _next = next;
        _deploymentSettings = deploymentSettings.Value;
    }

    /// <summary>
    /// Invalidates the cached bootstrap deployment mode so the next request re-reads from DB.
    /// Called by CompleteInstanceOnboardingCommandHandler after saving the deployment mode.
    /// </summary>
    public static void InvalidateBootstrapCache()
    {
        _cachedBootstrapDeploymentMode = null;
    }

    public async Task InvokeAsync(HttpContext context, IResolverConfigService resolverConfigService, ITenantSlugCache tenantSlugCache, ITenantContextAccessor tenantContextAccessor, IProblemDetailsService problemDetailsService, IInstanceBootstrapStateRepository bootstrapStateRepository)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (tenantContextAccessor.IsResolved)
        {
            await _next(context);
            return;
        }

        if (IsTenantExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (IsSingleTenantMode(bootstrapStateRepository))
        {
            var defaultTenantId = _deploymentSettings.DefaultTenantId != Guid.Empty
                ? _deploymentSettings.DefaultTenantId
                : FallbackDefaultTenantId;

            tenantContextAccessor.SetTenant(defaultTenantId);
            await _next(context);
            return;
        }

        var configuration = await resolverConfigService.GetConfigurationAsync(context.RequestAborted);

        var resolvedTenantId = await ResolveFromSlugHeaderAsync(context, tenantSlugCache);
        resolvedTenantId ??= await ResolveFromHostAsync(context, configuration, tenantSlugCache);

        if (context.Request.Headers.ContainsKey(ApiAuthenticationHeaderNames.ApiKey))
        {
            if (resolvedTenantId is Guid requestedTenantId && requestedTenantId != Guid.Empty)
            {
                context.Items[RequestedTenantIdItemKey] = requestedTenantId;
            }

            await _next(context);
            return;
        }

        if (resolvedTenantId is Guid tenantId && tenantId != Guid.Empty)
        {
            tenantContextAccessor.SetTenant(tenantId);
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant not resolved",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Detail = "The tenant could not be resolved for this request.",
                Instance = context.Request.Path
            }
        });
    }

    private static async Task<Guid?> ResolveFromSlugHeaderAsync(HttpContext context, ITenantSlugCache tenantSlugCache)
    {
        var tenantSlug = context.Request.Headers[TenantHeaderNames.TenantSlug].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return null;
        }

        return await tenantSlugCache.GetTenantIdBySlugAsync(tenantSlug, context.RequestAborted);
    }

    private static async Task<Guid?> ResolveFromHostAsync(HttpContext context, ResolverConfigurationDto configuration, ITenantSlugCache tenantSlugCache)
    {
        var host = GetRequestHost(context);
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        if (configuration.CustomDomainEnabled && configuration.AllowTenantCustomDomains)
        {
            var customDomainTenantId = await tenantSlugCache.GetTenantIdByDomainAsync(host, context.RequestAborted);
            if (customDomainTenantId is Guid resolvedCustomDomainTenantId && resolvedCustomDomainTenantId != Guid.Empty)
            {
                return resolvedCustomDomainTenantId;
            }
        }

        if (!configuration.SubdomainEnabled || string.IsNullOrWhiteSpace(configuration.InstanceBaseDomain))
        {
            return null;
        }

        var baseDomain = NormalizeHost(configuration.InstanceBaseDomain);
        var subdomain = ExtractSubdomain(host, baseDomain);
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return null;
        }

        return await tenantSlugCache.GetTenantIdByDomainAsync(subdomain, context.RequestAborted);
    }

    private static string GetRequestHost(HttpContext context)
    {
        return NormalizeHost(context.Request.Host.Host) ?? string.Empty;
    }

    private static bool IsTenantExemptPath(PathString path)
    {
        return path.StartsWithSegments("/api/InstanceOnboarding", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api/PublicExperience/settings", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeHost(string? host)
    {
        return string.IsNullOrWhiteSpace(host)
            ? null
            : host.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static string? ExtractSubdomain(string host, string? baseDomain)
    {
        if (string.IsNullOrWhiteSpace(baseDomain) || string.Equals(host, baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = "." + baseDomain;
        if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var prefix = host[..^suffix.Length];
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        return prefix.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks if the deployment is in single-tenant mode by first checking config,
    /// then falling back to the persisted InstanceBootstrapState from the database.
    /// The DB result is cached statically to avoid repeated queries.
    /// </summary>
    private bool IsSingleTenantMode(IInstanceBootstrapStateRepository bootstrapStateRepository)
    {
        if (_deploymentSettings.IsSingleTenant)
        {
            return true;
        }

        var cached = _cachedBootstrapDeploymentMode;
        if (cached != null)
        {
            return cached.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase);
        }

        // Synchronous DB check — runs once per app lifetime (until cache is invalidated).
        // Using GetAwaiter().GetResult() because middleware InvokeAsync is already async
        // but this method is called in a sync context for the branching logic.
        var bootstrap = bootstrapStateRepository.GetCurrent().GetAwaiter().GetResult();
        var mode = bootstrap?.IsCompleted == true ? bootstrap.SelectedDeploymentMode : string.Empty;
        _cachedBootstrapDeploymentMode = mode ?? string.Empty;

        return (mode ?? string.Empty).Equals("SingleTenant", StringComparison.OrdinalIgnoreCase);
    }
}
