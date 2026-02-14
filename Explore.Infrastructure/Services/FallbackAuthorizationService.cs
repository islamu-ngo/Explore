// ABOUTME: Database-driven authorization service used when Cerbos PDP is unavailable.
// Evaluates access control using IAdminContext and ISettingsResolver for lock semantics.

using System.Diagnostics;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Fallback authorization service that evaluates access decisions using database-driven admin checks
/// and settings lock semantics. Used when Cerbos PDP is not configured (e.g., development, ATProto/PDS-only).
/// Implements the same IAuthorizationProvider contract for seamless DI swapping.
/// </summary>
public class FallbackAuthorizationService : IAuthorizationProvider
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
            LogDecision("allow", "instance_admin", resourceKind, resourceId, action);
            return true;
        }

        var decision = resourceKind switch
        {
            "instance_setting" => false, // Only instance admins can modify instance settings
            "tenant_setting" => await EvaluateTenantSettingAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
            "organization" => await EvaluateOrganizationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
            _ => await EvaluateDefaultAccessAsync(resourceKind, action, resourceAttributes, cancellationToken)
        };

        LogDecision(decision ? "allow" : "deny", "fallback_policy", resourceKind, resourceId, action);
        return decision;
    }

    public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
            return [];

        var results = new bool[checks.Count];
        for (var i = 0; i < checks.Count; i++)
        {
            var check = checks[i];
            results[i] = await IsAllowedAsync(
                check.ResourceKind,
                check.ResourceId,
                check.Action,
                check.ResourceAttributes is null ? null : new Dictionary<string, object>(check.ResourceAttributes),
                cancellationToken);
        }

        return results;
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
            attributes["organizationId"] = organizationId.Value.ToString();
        }
        else if (tenantId.HasValue)
        {
            resourceKind = "tenant_setting";
            attributes["tenantId"] = tenantId.Value.ToString();

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
            LogDecision("deny", "locked_by_instance", "tenant_setting", resourceId, action);
            return false;
        }

        // Get tenantId from attributes or current context
        Guid tenantId;
        if (resourceAttributes?.TryGetValue("tenantId", out var tenantIdObj) == true)
        {
            if (tenantIdObj is Guid tid)
            {
                tenantId = tid;
            }
            else if (tenantIdObj is string tenantIdString && Guid.TryParse(tenantIdString, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }
            else
            {
                tenantId = _tenantContext.TenantId;
            }
        }
        else
        {
            tenantId = _tenantContext.TenantId;
        }

        // Check if user is a tenant admin for this specific tenant
        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(
            isTenantAdmin ? "allow" : "deny",
            $"tenant_admin={isTenantAdmin}",
            "tenant_setting",
            resourceId,
            action);
        return isTenantAdmin;
    }

    private async Task<bool> EvaluateOrganizationAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        Guid orgId;

        // Get organizationId from attributes
        if (resourceAttributes?.TryGetValue("organizationId", out var orgIdObj) != true)
        {
            // Try parsing resourceId as orgId
            if (!Guid.TryParse(resourceId, out var orgIdFromResource))
            {
                LogDecision("deny", "missing_organization_id", "organization", resourceId, action);
                return false;
            }

            orgId = orgIdFromResource;
        }
        else
        {
            if (orgIdObj is Guid parsedOrgId)
            {
                orgId = parsedOrgId;
            }
            else if (orgIdObj is string orgIdString && Guid.TryParse(orgIdString, out var parsedOrgIdFromString))
            {
                orgId = parsedOrgIdFromString;
            }
            else if (!Guid.TryParse(resourceId, out orgId))
            {
                LogDecision("deny", "invalid_organization_id", "organization", resourceId, action);
                return false;
            }
        }

        // Check tenant admin (tenant admins can manage orgs within their tenant)
        var tenantId = _tenantContext.TenantId;
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            LogDecision("allow", "tenant_admin=true", "organization", resourceId, action);
            return true;
        }

        // Check organization admin
        var isOrgAdmin = await _adminContext.IsOrganizationAdminAsync(orgId, cancellationToken);
        LogDecision(
            isOrgAdmin ? "allow" : "deny",
            $"organization_admin={isOrgAdmin}",
            "organization",
            resourceId,
            action);
        return isOrgAdmin;
    }

    private Task<bool> EvaluateDefaultAccessAsync(
        string resourceKind,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // For unknown resource kinds, deny by default (secure by default)
        LogDecision("deny", "unknown_resource_kind", resourceKind, resourceKind, action);
        return Task.FromResult(false);
    }

    private void LogDecision(
        string decision,
        string reason,
        string resourceKind,
        string resourceId,
        string action)
    {
        var correlationId = Activity.Current?.Id ?? string.Empty;
        _logger.LogInformation(
            "Fallback authorization decision: {Decision} reason={Reason} resource={ResourceKind}/{ResourceId} action={Action} correlationId={CorrelationId}",
            decision,
            reason,
            resourceKind,
            resourceId,
            action,
            correlationId);
    }
}
