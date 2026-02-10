// ABOUTME: Database-driven authorization service used when Cerbos PDP is unavailable.
// Evaluates access control using IAdminContext and ISettingsResolver for lock semantics.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Fallback authorization service that evaluates access decisions using database-driven admin checks
/// and settings lock semantics. Used when Cerbos PDP is not configured (e.g., development, ATProto/PDS-only).
/// Implements the same ICerbosAuthorizationService contract for seamless DI swapping.
/// </summary>
public class FallbackAuthorizationService : ICerbosAuthorizationService
{
    private readonly IAdminContext _adminContext;
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FallbackAuthorizationService> _logger;

    public FallbackAuthorizationService(
        IAdminContext adminContext,
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        ILogger<FallbackAuthorizationService> logger)
    {
        _adminContext = adminContext;
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default)
    {
        // Instance admins can do everything
        if (await _adminContext.IsInstanceAdminAsync(cancellationToken))
        {
            _logger.LogDebug("Fallback auth: ALLOW (instance admin) {Resource}/{Action}", resourceKind, action);
            return true;
        }

        return resourceKind switch
        {
            "instance_setting" => false, // Only instance admins can modify instance settings
            "tenant_setting" => await EvaluateTenantSettingAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
            "organization" => await EvaluateOrganizationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
            _ => await EvaluateDefaultAccessAsync(resourceKind, action, resourceAttributes, cancellationToken)
        };
    }

    public async Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        // Instance admins bypass all lock checks
        if (await _adminContext.IsInstanceAdminAsync(cancellationToken))
            return true;

        // Determine resource kind from scope
        string resourceKind;
        var attributes = new Dictionary<string, object> { ["settingKey"] = settingKey };

        if (organizationId.HasValue)
        {
            resourceKind = "organization";
            attributes["organizationId"] = organizationId.Value;
        }
        else if (tenantId.HasValue)
        {
            resourceKind = "tenant_setting";
            attributes["tenantId"] = tenantId.Value;

            // Check if the setting is locked by instance
            var canOverride = await _settingsResolver.CanOverrideAsync(settingKey, cancellationToken);
            attributes["isLockedByInstance"] = !canOverride;
        }
        else
        {
            resourceKind = "instance_setting";
        }

        return await IsAllowedAsync(resourceKind, settingKey, action, attributes, cancellationToken);
    }

    private async Task<bool> EvaluateTenantSettingAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // Check if setting is locked by instance admin
        if (resourceAttributes?.TryGetValue("isLockedByInstance", out var lockedObj) == true
            && lockedObj is true)
        {
            _logger.LogDebug("Fallback auth: DENY (locked by instance) {Resource}/{Action}", resourceId, action);
            return false;
        }

        // Get tenantId from attributes or current context
        Guid tenantId;
        if (resourceAttributes?.TryGetValue("tenantId", out var tenantIdObj) == true
            && tenantIdObj is Guid tid)
        {
            tenantId = tid;
        }
        else
        {
            tenantId = _tenantContext.TenantId;
        }

        // Check if user is a tenant admin for this specific tenant
        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        _logger.LogDebug("Fallback auth: {Result} (tenant admin={IsTenantAdmin}) {Resource}/{Action}",
            isTenantAdmin ? "ALLOW" : "DENY", isTenantAdmin, resourceId, action);
        return isTenantAdmin;
    }

    private async Task<bool> EvaluateOrganizationAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // Get organizationId from attributes
        if (resourceAttributes?.TryGetValue("organizationId", out var orgIdObj) != true
            || orgIdObj is not Guid orgId)
        {
            // Try parsing resourceId as orgId
            if (!Guid.TryParse(resourceId, out orgId))
            {
                _logger.LogWarning("Fallback auth: DENY (no organizationId) {Resource}/{Action}", resourceId, action);
                return false;
            }
        }

        // Check tenant admin (tenant admins can manage orgs within their tenant)
        var tenantId = _tenantContext.TenantId;
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
            return true;

        // Check organization admin
        var isOrgAdmin = await _adminContext.IsOrganizationAdminAsync(orgId, cancellationToken);
        _logger.LogDebug("Fallback auth: {Result} (org admin={IsOrgAdmin}) {Resource}/{Action}",
            isOrgAdmin ? "ALLOW" : "DENY", isOrgAdmin, resourceId, action);
        return isOrgAdmin;
    }

    private Task<bool> EvaluateDefaultAccessAsync(
        string resourceKind,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // For unknown resource kinds, deny by default (secure by default)
        _logger.LogWarning("Fallback auth: DENY (unknown resource kind) {Resource}/{Action}", resourceKind, action);
        return Task.FromResult(false);
    }
}
