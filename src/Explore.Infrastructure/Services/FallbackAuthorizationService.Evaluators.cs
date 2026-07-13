// ABOUTME: Async resource-family evaluators for FallbackAuthorizationService.
// ABOUTME: Tenant-scoped, org-scoped, and resource-specific access evaluation methods.

using Explore.Application.Authorization;
using Explore.Domain;

namespace Explore.Infrastructure.Services;

public partial class FallbackAuthorizationService
{
    private async Task<bool> EvaluateTenantSettingAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (!IsTenantBrandingDocument(resourceAttributes)
            && resourceAttributes?.TryGetValue("isLockedByInstance", out var lockedObj) == true
            && lockedObj is true)
        {
            LogDecision("deny", "locked_by_instance", "islamuevent_tenant_setting", resourceId, action);
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
            $"is_tenant_admin={isTenantAdmin}",
            "islamuevent_tenant_setting",
            resourceId,
            action);
        return isTenantAdmin;
    }

    private static bool IsTenantBrandingDocument(IDictionary<string, object>? resourceAttributes)
        => resourceAttributes?.TryGetValue("documentKey", out var documentKey) == true
            && documentKey is string documentKeyString
            && string.Equals(documentKeyString, "tenant.branding", StringComparison.Ordinal);

    private async Task<bool> EvaluateOrganizationAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (IsPreCreateCheck(action, resourceAttributes))
        {
            var preCreateTenantId = ResolveTenantId(resourceAttributes);
            var userId = _adminContext.UserId ?? await _adminContext.ResolveUserIdAsync(cancellationToken);
            var allowed = preCreateTenantId == _tenantContext.TenantId && userId.HasValue;

            LogDecision(
                allowed ? "allow" : "deny",
                $"organization_pre_create user_present={userId.HasValue}",
                "islamuevent_organization",
                resourceId,
                action);
            return allowed;
        }

        Guid orgId;

        if (resourceAttributes?.TryGetValue("organizationId", out var orgIdObj) != true)
        {
            if (!Guid.TryParse(resourceId, out var orgIdFromResource))
            {
                LogDecision("deny", "missing_organization_id", "islamuevent_organization", resourceId, action);
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
                LogDecision("deny", "invalid_organization_id", "islamuevent_organization", resourceId, action);
                return false;
            }
        }

        var tenantId = _tenantContext.TenantId;
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            LogDecision("allow", "is_tenant_admin=true", "islamuevent_organization", resourceId, action);
            return true;
        }

        var isOrgAdmin = await _adminContext.IsOrganizationAdminAsync(orgId, cancellationToken);
        LogDecision(
            isOrgAdmin ? "allow" : "deny",
            $"organization_admin={isOrgAdmin}",
            "islamuevent_organization",
            resourceId,
            action);
        return isOrgAdmin;
    }

    private static bool IsPreCreateCheck(string action, IDictionary<string, object>? resourceAttributes)
    {
        return action == AuthorizationActions.Create
            && resourceAttributes?.TryGetValue("authorizationPhase", out var phase) == true
            && string.Equals(phase?.ToString(), AuthorizationPhases.PreCreate, StringComparison.Ordinal);
    }

    private async Task<bool> EvaluateTenantScopedAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        Guid tenantId;
        if (resourceAttributes?.ContainsKey("tenantId") == true)
        {
            if (!TryResolveGuidAttribute(resourceAttributes, "tenantId", out tenantId))
            {
                LogDecision("deny", "invalid_tenant_context", resourceKind, resourceId, action);
                return false;
            }

            if (tenantId != _tenantContext.TenantId)
            {
                LogDecision("deny", "tenant_mismatch", resourceKind, resourceId, action);
                return false;
            }
        }
        else
        {
            tenantId = _tenantContext.TenantId;
        }

        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"is_tenant_admin={isTenantAdmin}", resourceKind, resourceId, action);
        return isTenantAdmin;
    }

    private async Task<bool> EvaluateWebhookAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken))
            return true;

        if (!AllowsOrganizationWebhooks(resourceAttributes))
        {
            LogDecision("deny", "organization_webhooks_disabled", resourceKind, resourceId, action);
            return false;
        }

        if (!IsOrganizationWebhookAction(action))
        {
            LogDecision("deny", "organization_webhook_action_not_allowed", resourceKind, resourceId, action);
            return false;
        }

        var orgId = ResolveOrganizationId(resourceAttributes, resourceId);
        if (orgId.HasValue && await _adminContext.IsOrganizationAdminAsync(orgId.Value, cancellationToken))
        {
            LogDecision("allow", "organization_admin=true", resourceKind, resourceId, action);
            return true;
        }

        LogDecision("deny", "no_webhook_admin_authority", resourceKind, resourceId, action);
        return false;
    }

    private static bool AllowsOrganizationWebhooks(IDictionary<string, object>? resourceAttributes)
    {
        if (resourceAttributes?.TryGetValue("allowOrganizationWebhooks", out var value) != true)
            return false;

        return value switch
        {
            bool enabled => enabled,
            string text => bool.TryParse(text, out var enabled) && enabled,
            _ => false
        };
    }

    private static bool IsOrganizationWebhookAction(string action)
        => action is AuthorizationActions.Webhooks.View
            or AuthorizationActions.Webhooks.Create
            or AuthorizationActions.Webhooks.Update
            or AuthorizationActions.Webhooks.Delete
            or AuthorizationActions.Webhooks.RotateSecret
            or AuthorizationActions.Webhooks.Test
            or AuthorizationActions.Webhooks.Retry
            or AuthorizationActions.Webhooks.Resume
            or AuthorizationActions.Webhooks.ViewDelivery
            or AuthorizationActions.Webhooks.OpenProviderPortal;

    private async Task<bool> EvaluateCustomPropertyProjectionAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is not (AuthorizationActions.CustomPropertyProjections.View or AuthorizationActions.CustomPropertyProjections.Update))
        {
            LogDecision("deny", "invalid_projection_action", ResourceKinds.CustomPropertyProjection, resourceId, action);
            return false;
        }

        if (!TryResolveGuidAttribute(resourceAttributes, "tenantId", out var tenantId))
        {
            LogDecision("deny", "missing_tenant_context", ResourceKinds.CustomPropertyProjection, resourceId, action);
            return false;
        }

        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", ResourceKinds.CustomPropertyProjection, resourceId, action);
            return false;
        }

        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"is_tenant_admin={isTenantAdmin}", ResourceKinds.CustomPropertyProjection, resourceId, action);
        return isTenantAdmin;
    }

    private async Task<bool> EvaluateTenantResourceAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is not (AuthorizationActions.View or AuthorizationActions.Update))
        {
            LogDecision("deny", "tenant_action_requires_instance_admin", ResourceKinds.Tenant, resourceId, action);
            return false;
        }

        var tenantId = ResolveTenantId(resourceAttributes);
        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", ResourceKinds.Tenant, resourceId, action);
            return false;
        }

        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"is_tenant_admin={isTenantAdmin}", ResourceKinds.Tenant, resourceId, action);
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
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken)
            && IsTenantAdminOrgScopedAction(action))
        {
            LogDecision("allow", "is_tenant_admin=true", resourceKind, resourceId, action);
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

        return await EvaluateOrgScopedAccessAsync("islamuevent_organization_review", resourceId, action, resourceAttributes, cancellationToken);
    }

    private static bool IsTenantAdminOrgScopedAction(string action)
        => action is AuthorizationActions.View
            or AuthorizationActions.Create
            or AuthorizationActions.Update
            or AuthorizationActions.Delete
            or AuthorizationActions.ManageMembers;

    private async Task<bool> EvaluateEventRegistrationAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (!TryResolveEventContext("islamuevent_event_registration", resourceId, resourceAttributes, out var tenantId, out var eventId))
        {
            LogDecision("deny", "missing_event_context", "islamuevent_event_registration", resourceId, action);
            return false;
        }

        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", "islamuevent_event_registration", resourceId, action);
            return false;
        }

        if (action is "create" or "view")
            return true;

        if (await EvaluateEventRolePermissionAsync("islamuevent_event_registration", resourceId, action, tenantId, eventId, cancellationToken))
            return true;

        return await EvaluateOrgScopedAccessAsync("islamuevent_event_registration", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateEventScopedAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (resourceKind is "islamuevent_event" && action is "create")
            return await EvaluateEventCreateAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);

        if (!TryResolveEventContext(resourceKind, resourceId, resourceAttributes, out var tenantId, out var eventId))
        {
            LogDecision("deny", "missing_event_context", resourceKind, resourceId, action);
            return false;
        }

        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", resourceKind, resourceId, action);
            return false;
        }

        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken)
            && (resourceKind != ResourceKinds.Event || IsTenantAdminEventAction(action)))
        {
            LogDecision("allow", "is_tenant_admin=true", resourceKind, resourceId, action);
            return true;
        }

        if (resourceKind == ResourceKinds.Event && IsEventModerationAction(action))
        {
            LogDecision("deny", "moderation_requires_platform_or_tenant_admin", resourceKind, resourceId, action);
            return false;
        }

        var orgId = ResolveOrganizationId(resourceAttributes, resourceId);
        if (orgId.HasValue && await _adminContext.IsOrganizationAdminAsync(orgId.Value, cancellationToken))
        {
            LogDecision("allow", "organization_admin=true", resourceKind, resourceId, action);
            return true;
        }

        if (await IsActorUserOwnerAsync(resourceAttributes, cancellationToken))
        {
            LogDecision("allow", "actor_user_owner=true", resourceKind, resourceId, action);
            return true;
        }

        if (await EvaluateEventRolePermissionAsync(resourceKind, resourceId, action, tenantId, eventId, cancellationToken))
            return true;

        LogDecision("deny", "no_event_authority", resourceKind, resourceId, action);
        return false;
    }

    private async Task<bool> EvaluateEventCreateAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(resourceAttributes);
        if (tenantId != _tenantContext.TenantId)
        {
            LogDecision("deny", "tenant_mismatch", resourceKind, resourceId, action);
            return false;
        }

        var userId = _adminContext.UserId ?? await _adminContext.ResolveUserIdAsync(cancellationToken);
        if (!userId.HasValue)
        {
            LogDecision("deny", "missing_user_id", resourceKind, resourceId, action);
            return false;
        }

        LogDecision("allow", "authenticated_pre_create_handler_policy", resourceKind, resourceId, action);
        return true;
    }

    private async Task<bool> IsActorUserOwnerAsync(
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (!TryResolveGuidAttribute(resourceAttributes, "userId", out var ownerUserId))
            return false;

        var currentUserId = _adminContext.UserId ?? await _adminContext.ResolveUserIdAsync(cancellationToken);
        return currentUserId == ownerUserId;
    }

    private async Task<bool> EvaluateEventRolePermissionAsync(
        string resourceKind,
        string resourceId,
        string action,
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var userId = _adminContext.UserId ?? await _adminContext.ResolveUserIdAsync(cancellationToken);
        if (!userId.HasValue)
        {
            LogDecision("deny", "missing_user_id", resourceKind, resourceId, action);
            return false;
        }

        var snapshot = await _eventAuthoritySnapshotService.GetForUserAndEventsAsync(
            tenantId,
            userId.Value,
            [eventId],
            cancellationToken);

        if (snapshot is null)
        {
            LogDecision("deny", "event_role_snapshot_missing", resourceKind, resourceId, action);
            return false;
        }

        var permissionCode = PermissionCodeFor(resourceKind, action);
        var allowed = snapshot.Events.TryGetValue(eventId, out var authority)
            && authority.PermissionCodes.Contains(permissionCode);

        LogDecision(
            allowed ? "allow" : "deny",
            allowed ? "event_role_permission=true" : "event_role_permission_missing",
            resourceKind,
            resourceId,
            action);

        return allowed;
    }

    private async Task<bool> EvaluateStorageObjectAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action == AuthorizationActions.StorageObjects.Create)
            return true;

        if (action is AuthorizationActions.StorageObjects.Download
            or AuthorizationActions.StorageObjects.PresignedDownload)
        {
            if (await EvaluateTenantScopedAccessAsync("islamuevent_storage_object", resourceId, action, resourceAttributes, cancellationToken))
                return true;

            return CanReadStorageObjectContent(resourceId, action, resourceAttributes);
        }

        return await EvaluateTenantScopedAccessAsync("islamuevent_storage_object", resourceId, action, resourceAttributes, cancellationToken);
    }

    private bool CanReadStorageObjectContent(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes)
    {
        var visibility = GetAttribute(resourceAttributes, "visibility");
        var lifecycleState = GetAttribute(resourceAttributes, "lifecycleState");
        if (!string.Equals(lifecycleState, StorageObjectLifecycleStates.Active, StringComparison.Ordinal))
        {
            LogDecision("deny", "storage_object_not_active", "islamuevent_storage_object", resourceId, action);
            return false;
        }

        if (visibility is StorageObjectVisibilities.PublicImage or StorageObjectVisibilities.AuthenticatedTenant)
        {
            LogDecision("allow", $"storage_visibility={visibility}", "islamuevent_storage_object", resourceId, action);
            return true;
        }

        var createdBy = GetAttribute(resourceAttributes, "createdBy");
        var currentUserId = _adminContext.UserId?.ToString("D");
        var isOwner = !string.IsNullOrWhiteSpace(createdBy)
            && !string.IsNullOrWhiteSpace(currentUserId)
            && string.Equals(createdBy, currentUserId, StringComparison.OrdinalIgnoreCase);

        LogDecision(
            isOwner ? "allow" : "deny",
            isOwner ? "storage_private_owner" : "storage_private_owner_mismatch",
            "islamuevent_storage_object",
            resourceId,
            action);
        return isOwner;
    }

    private static string? GetAttribute(IDictionary<string, object>? resourceAttributes, string name) =>
        resourceAttributes is not null && resourceAttributes.TryGetValue(name, out var value)
            ? value?.ToString()
            : null;

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
            LogDecision("allow", "self_service", "islamuevent_user", resourceId, action);
            return true;
        }

        var tenantId = ResolveTenantId(resourceAttributes);
        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"is_tenant_admin={isTenantAdmin}", "islamuevent_user", resourceId, action);
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

        return await EvaluateOrgScopedAccessAsync("islamuevent_group_member", resourceId, action, resourceAttributes, cancellationToken);
    }

    private async Task<bool> EvaluateContactShareConsentAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        if (action is not ("viewsharedcontacts" or "exportsharedcontacts"))
            return false;

        return await EvaluateOrgScopedAccessAsync("islamuevent_event_contact_share_consent", resourceId, action, resourceAttributes, cancellationToken);
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
