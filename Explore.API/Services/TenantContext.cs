// ABOUTME: Provides tenant context for multi-tenant data isolation.
// ABOUTME: Respects deployment mode - single-tenant skips resolution, multi-tenant uses header/subdomain.

using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.Services;

/// <summary>
/// Provides tenant context by reading the X-Tenant-Id header from HTTP requests.
/// In SingleTenant mode, always returns the configured default tenant ID.
/// In MultiTenant mode, resolves from header or subdomain with fallback to default.
/// </summary>
public class TenantContext : ITenantContext
{
    private const string TenantIdHeaderName = "X-Tenant-Id";

    /// <summary>
    /// Fallback default tenant ID matching the seeded tenant in the database.
    /// This MUST match SeedIds.DefaultTenantId in Explore.Persistence.
    /// Used when no configuration is provided.
    /// </summary>
    private static readonly Guid FallbackDefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DeploymentSettings _deploymentSettings;

    public TenantContext(
        IHttpContextAccessor httpContextAccessor,
        IOptions<DeploymentSettings> deploymentSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _deploymentSettings = deploymentSettings.Value;
    }

    /// <summary>
    /// Gets the current tenant ID.
    /// In SingleTenant mode: Always returns DefaultTenantId from configuration.
    /// In MultiTenant mode: Resolves from X-Tenant-Id header, subdomain, or falls back to default.
    /// </summary>
    public Guid TenantId
    {
        get
        {
            // SingleTenant mode: Always use configured default tenant
            if (_deploymentSettings.IsSingleTenant)
            {
                return GetDefaultTenantId();
            }

            // MultiTenant mode: Resolve from request
            return ResolveTenantFromRequest();
        }
    }

    /// <summary>
    /// Gets the default tenant ID from configuration or fallback.
    /// </summary>
    private Guid GetDefaultTenantId()
    {
        return _deploymentSettings.DefaultTenantId != Guid.Empty
            ? _deploymentSettings.DefaultTenantId
            : FallbackDefaultTenantId;
    }

    /// <summary>
    /// Resolves tenant ID from the HTTP request in multi-tenant mode.
    /// Priority: X-Tenant-Id header > Subdomain > Default
    /// </summary>
    private Guid ResolveTenantFromRequest()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return GetDefaultTenantId();
        }

        // Priority 1: X-Tenant-Id header (explicit tenant selection)
        if (httpContext.Request.Headers.TryGetValue(TenantIdHeaderName, out var tenantIdHeader) &&
            Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var tenantId))
        {
            return tenantId;
        }

        // Priority 2: Subdomain resolution (e.g., tech-hub.islamu.app)
        var host = httpContext.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);
        if (!string.IsNullOrEmpty(subdomain))
        {
            // TODO: Look up tenant by subdomain from database/cache
            // For now, fall through to default
            // var tenant = _tenantRepository.GetBySubdomain(subdomain);
            // if (tenant != null) return tenant.Id;
        }

        // Priority 3: Default tenant
        return GetDefaultTenantId();
    }

    /// <summary>
    /// Extracts the subdomain from a host string.
    /// Returns null if no subdomain or if it's a common prefix (www, api).
    /// </summary>
    private static string? ExtractSubdomain(string host)
    {
        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];

        // Split by dots
        var parts = hostWithoutPort.Split('.');

        // Need at least 3 parts for a subdomain (subdomain.domain.tld)
        if (parts.Length < 3)
            return null;

        var subdomain = parts[0];

        // Ignore common non-tenant subdomains
        var ignoredSubdomains = new[] { "www", "api", "app", "admin", "localhost" };
        if (ignoredSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
            return null;

        return subdomain;
    }
}
