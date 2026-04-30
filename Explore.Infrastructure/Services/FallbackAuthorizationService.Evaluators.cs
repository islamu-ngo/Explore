// ABOUTME: Async resource-family evaluators for FallbackAuthorizationService.
// ABOUTME: Tenant-scoped, org-scoped, and resource-specific access evaluation methods.

namespace Explore.Infrastructure.Services;

public partial class FallbackAuthorizationService
{
    private async Task<bool> EvaluateTenantSettingAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (resourceAttributes?.TryGetValue("isLockedByInstance", out var lockedObj) == true
            && lockedObj is true)
        {
            LogDecision("deny", "locked_by_instance", "tenant_setting", resourceId, action);
            return false;
        }

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

        if (resourceAttributes?.TryGetValue("organizationId", out var orgIdObj) != true)
        {
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

        var tenantId = _tenantContext.TenantId;
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            LogDecision("allow", "tenant_admin=true", "organization", resourceId, action);
            return true;
        }

        var isOrgAdmin = await _adminContext.IsOrganizationAdminAsync(orgId, cancellationToken);
        LogDecision(
            isOrgAdmin ? "allow" : "deny",
            $"organization_admin={isOrgAdmin}",
            "organization",
            resourceId,
            action);
        return isOrgAdmin;
    }

    private async Task<bool> EvaluateTenantScopedAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(resourceAttributes);
        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"tenant_admin={isTenantAdmin}", resourceKind, resourceId, action);
        return isTenantAdmin;
    }

    private async Task<bool> EvaluateOrgScopedAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(resourceAttributes);
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            LogDecision("allow", "tenant_admin=true", resourceKind, resourceId, action);
            return true;
        }

        var orgId = ResolveOrganizationId(resourceAttributes, resourceId);
        if (orgId.HasValue && await _adminContext.IsOrganizationAdminAsync(orgId.Value, cancellationToken))
        {
            LogDecision("allow", "organization_admin=true", resourceKind, resourceId, action);
            return true;
        }

        LogDecision("deny", "no_admin_authority", resourceKind, resourceId, action);
        return false;
    }

    private async Task<bool> EvaluateOrgReviewAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "create" or "view")
            return true;

        return await EvaluateOrgScopedAccessAsync("organization_review", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateEventRegistrationAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (!TryResolveEventContext("event_registration", resourceId, resourceAttributes, out var tenantId, out _))
        {
            LogDecision("deny", "missing_event_context", "event_registration", resourceId, action);
            return false;
        }

        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", "event_registration", resourceId, action);
            return false;
        }

        if (action is "create" or "view")
            return true;

        return await EvaluateOrgScopedAccessAsync("event_registration", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateEventScopedAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (!TryResolveEventContext(resourceKind, resourceId, resourceAttributes, out var tenantId, out _))
        {
            LogDecision("deny", "missing_event_context", resourceKind, resourceId, action);
            return false;
        }

        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", resourceKind, resourceId, action);
            return false;
        }

        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            LogDecision("allow", "tenant_admin=true", resourceKind, resourceId, action);
            return true;
        }

        var orgId = ResolveOrganizationId(resourceAttributes, resourceId);
        if (orgId.HasValue && await _adminContext.IsOrganizationAdminAsync(orgId.Value, cancellationToken))
        {
            LogDecision("allow", "organization_admin=true", resourceKind, resourceId, action);
            return true;
        }

        LogDecision("deny", "no_event_authority", resourceKind, resourceId, action);
        return false;
    }

    private async Task<bool> EvaluateStorageObjectAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "create" or "view")
            return true;

        return await EvaluateTenantScopedAccessAsync("storage_object", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateUserAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "view" or "update" && _adminContext.UserId.HasValue
            && Guid.TryParse(resourceId, out var targetUserId)
            && targetUserId == _adminContext.UserId.Value)
        {
            LogDecision("allow", "self_service", "user", resourceId, action);
            return true;
        }

        var tenantId = ResolveTenantId(resourceAttributes);
        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"tenant_admin={isTenantAdmin}", "user", resourceId, action);
        return isTenantAdmin;
    }

    private async Task<bool> EvaluateViewableTenantResourceAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "view")
            return true;

        return await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateViewableOrgResourceAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "view")
            return true;

        return await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateGroupMemberAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "view" or "create")
            return true;

        return await EvaluateOrgScopedAccessAsync("group_member", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateContactShareConsentAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is not ("viewsharedcontacts" or "exportsharedcontacts"))
            return false;

        return await EvaluateOrgScopedAccessAsync("event_contact_share_consent", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateActorAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is "view")
            return true;

        return await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);
    }
}
