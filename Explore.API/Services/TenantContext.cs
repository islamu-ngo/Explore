// ABOUTME: Legacy API tenant context implementation kept for comparison during the tenant-resolution migration.
// ABOUTME: Standard runtime wiring now uses shared infrastructure tenant context plus API-authoritative middleware.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Infrastructure;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Explore.API.Services;

/// <summary>
/// Legacy API tenant context implementation retained outside the active DI path.
/// It still documents the old direct-resolution model but is no longer the standard runtime authority.
/// </summary>
public class TenantContext : ITenantContext
{
    private const string TenantIdHeaderName = "X-Tenant-Id";
    private const string ResolvedTenantContextItemKey = "__resolved_tenant_id";

    /// <summary>
    /// Fallback default tenant ID matching the seeded tenant in the database.
    /// This MUST match SeedIds.DefaultTenantId in Explore.Persistence.
    /// Used when no configuration is provided.
    /// </summary>
    private static readonly Guid FallbackDefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DeploymentSettings _deploymentSettings;
    private readonly IDbContextFactory<ExploreDbContext>? _dbContextFactory;

    public TenantContext(
        IHttpContextAccessor httpContextAccessor,
        IOptions<DeploymentSettings> deploymentSettings,
        IDbContextFactory<ExploreDbContext>? dbContextFactory = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _deploymentSettings = deploymentSettings.Value;
        _dbContextFactory = dbContextFactory;
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

        if (httpContext.Items.TryGetValue(ResolvedTenantContextItemKey, out var cached) &&
            cached is Guid cachedTenantId &&
            cachedTenantId != Guid.Empty)
        {
            return cachedTenantId;
        }

        // Priority 1: X-Tenant-Id header (explicit tenant selection)
        if (httpContext.Request.Headers.TryGetValue(TenantIdHeaderName, out var tenantIdHeader) &&
            Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var tenantId))
        {
            httpContext.Items[ResolvedTenantContextItemKey] = tenantId;
            return tenantId;
        }

        // If no DB factory is available (e.g., some test setups), use config fallback behavior.
        if (_dbContextFactory == null)
        {
            var fallbackMode = _deploymentSettings.IsSingleTenant ? "SingleTenant" : "MultiTenant";
            if (fallbackMode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase))
            {
                var defaultTenantId = GetDefaultTenantId();
                httpContext.Items[ResolvedTenantContextItemKey] = defaultTenantId;
                return defaultTenantId;
            }

            var hostFromRequest = GetRequestHost(httpContext);
            var fallbackSubdomain = ExtractFallbackSubdomain(hostFromRequest);
            if (!string.IsNullOrWhiteSpace(fallbackSubdomain))
            {
                // No persistence lookup available in this mode; keep deterministic fallback.
                var defaultTenantId = GetDefaultTenantId();
                httpContext.Items[ResolvedTenantContextItemKey] = defaultTenantId;
                return defaultTenantId;
            }

            var fallbackTenant = GetDefaultTenantId();
            httpContext.Items[ResolvedTenantContextItemKey] = fallbackTenant;
            return fallbackTenant;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var runtimeMode = ResolveRuntimeDeploymentMode(dbContext);
        if (runtimeMode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase))
        {
            var defaultTenantId = GetDefaultTenantId();
            httpContext.Items[ResolvedTenantContextItemKey] = defaultTenantId;
            return defaultTenantId;
        }

        var host = GetRequestHost(httpContext);

        // Priority 2: Custom domain
        var customDomainTenantId = TryResolveTenantByCustomDomain(dbContext, host);
        if (customDomainTenantId.HasValue)
        {
            httpContext.Items[ResolvedTenantContextItemKey] = customDomainTenantId.Value;
            return customDomainTenantId.Value;
        }

        // Priority 3: Subdomain
        var subdomainTenantId = TryResolveTenantBySubdomain(dbContext, host);
        if (subdomainTenantId.HasValue)
        {
            httpContext.Items[ResolvedTenantContextItemKey] = subdomainTenantId.Value;
            return subdomainTenantId.Value;
        }

        // Priority 4: Default tenant
        var fallbackTenantId = GetDefaultTenantId();
        httpContext.Items[ResolvedTenantContextItemKey] = fallbackTenantId;
        return fallbackTenantId;
    }

    private string ResolveRuntimeDeploymentMode(ExploreDbContext dbContext)
    {
        try
        {
            var rawSetting = dbContext.SystemSettings
                .AsNoTracking()
                .Where(s => s.SettingKey == GovernanceSettingKeys.Deployment.Mode)
                .Select(s => s.Value)
                .FirstOrDefault();

            var modeFromSettings = DeserializeString(rawSetting, string.Empty);
            if (modeFromSettings.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase))
            {
                return "SingleTenant";
            }

            if (modeFromSettings.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase))
            {
                return "MultiTenant";
            }
        }
        catch
        {
            // Fall back to static configuration if runtime settings cannot be read.
        }

        return _deploymentSettings.IsSingleTenant ? "SingleTenant" : "MultiTenant";
    }

    private static string GetRequestHost(HttpContext httpContext)
    {
        var forwardedHost = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var candidate = string.IsNullOrWhiteSpace(forwardedHost)
            ? httpContext.Request.Host.Value
            : forwardedHost.Split(',')[0].Trim();

        return NormalizeHost(candidate);
    }

    private Guid? TryResolveTenantByCustomDomain(ExploreDbContext dbContext, string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var allowCustomDomainsRaw = dbContext.SystemSettings
            .AsNoTracking()
            .Where(s => s.SettingKey == GovernanceSettingKeys.Domains.AllowTenantCustomDomain)
            .Select(s => s.Value)
            .FirstOrDefault();

        if (!DeserializeBoolean(allowCustomDomainsRaw, true))
        {
            return null;
        }

        var serializedHost = JsonSerializer.Serialize(host);
        var tenantId = dbContext.TenantSettingOverrides
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.SettingKey == GovernanceSettingKeys.Domains.TenantCustomDomain && s.Value == serializedHost)
            .Select(s => s.TenantId)
            .FirstOrDefault();

        if (tenantId == Guid.Empty)
        {
            return null;
        }

        return dbContext.Tenants.AsNoTracking().Any(t => t.Id == tenantId && t.TenantStatusId == (int)TenantStatusEnum.Active)
            ? tenantId
            : null;
    }

    private Guid? TryResolveTenantBySubdomain(ExploreDbContext dbContext, string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var baseDomainRaw = dbContext.SystemSettings
            .AsNoTracking()
            .Where(s => s.SettingKey == GovernanceSettingKeys.Domains.InstanceBaseDomain)
            .Select(s => s.Value)
            .FirstOrDefault();

        var baseDomain = NormalizeHost(DeserializeString(baseDomainRaw, string.Empty));
        var candidateSubdomain = ExtractSubdomainFromBaseDomain(host, baseDomain) ?? ExtractFallbackSubdomain(host);
        if (string.IsNullOrWhiteSpace(candidateSubdomain))
        {
            return null;
        }

        var serializedSubdomain = JsonSerializer.Serialize(candidateSubdomain);
        var tenantId = dbContext.TenantSettingOverrides
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.SettingKey == GovernanceSettingKeys.Domains.TenantSubdomain && s.Value == serializedSubdomain)
            .Select(s => s.TenantId)
            .FirstOrDefault();

        if (tenantId != Guid.Empty && dbContext.Tenants.AsNoTracking().Any(t => t.Id == tenantId && t.TenantStatusId == (int)TenantStatusEnum.Active))
        {
            return tenantId;
        }

        var slugTenant = dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefault(t => t.TenantStatusId == (int)TenantStatusEnum.Active && t.Slug.ToLower() == candidateSubdomain);

        return slugTenant?.Id;
    }

    private static string? ExtractSubdomainFromBaseDomain(string host, string baseDomain)
    {
        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            return null;
        }

        if (host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase))
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

        var firstLabel = prefix.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return NormalizeSubdomain(firstLabel);
    }

    /// <summary>
    /// Fallback subdomain extraction for hosts where no explicit base domain is configured.
    /// Returns null if no subdomain or if it's a common prefix (www, api).
    /// </summary>
    private static string? ExtractFallbackSubdomain(string host)
    {
        var hostWithoutPort = NormalizeHost(host);
        var parts = hostWithoutPort.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return null;
        }

        var subdomain = NormalizeSubdomain(parts[0]);
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return null;
        }

        var ignoredSubdomains = new[] { "www", "api", "app", "admin", "localhost" };
        if (ignoredSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return subdomain;
    }

    private static string NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        var normalized = host.Trim().ToLowerInvariant();
        normalized = normalized.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);

        var withoutPort = normalized.Split(':')[0].Trim();
        return withoutPort.Trim('/');
    }

    private static string? NormalizeSubdomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool DeserializeBoolean(string? rawValue, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<bool>(rawValue);
        }
        catch
        {
            return bool.TryParse(rawValue, out var parsed) ? parsed : fallback;
        }
    }

    private static string DeserializeString(string? rawValue, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return deserialized ?? fallback;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }
}
