// ABOUTME: Batch evaluation optimization for FallbackAuthorizationService.
// ABOUTME: Pre-resolves admin authority once per batch to eliminate repeated async overhead.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;

namespace Explore.Infrastructure.Services;

public partial class FallbackAuthorizationService
{
    public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
            return [];

        if (checks.Count <= 2)
        {
            var smallResults = new bool[checks.Count];
            for (var i = 0; i < checks.Count; i++)
            {
                var check = checks[i];
                smallResults[i] = await IsAllowedAsync(
                    check.ResourceKind,
                    check.ResourceId,
                    check.Action,
                    check.ResourceAttributes is null ? null : new Dictionary<string, object>(check.ResourceAttributes),
                    cancellationToken);
            }

            return smallResults;
        }

        var profile = await ResolveAuthorityProfileAsync(cancellationToken);
        var eventAuthority = await ResolveBatchEventAuthorityAsync(profile, checks, cancellationToken);

        var results = new bool[checks.Count];
        for (var i = 0; i < checks.Count; i++)
        {
            var check = checks[i];
            var attributes = check.ResourceAttributes is null
                ? null
                : new Dictionary<string, object>(check.ResourceAttributes);
            results[i] = EvaluateWithProfile(profile, eventAuthority, check.ResourceKind, check.ResourceId, check.Action, attributes);
        }

        return results;
    }

    private sealed record AuthorityProfile(
        bool IsInstanceAdmin,
        bool IsTenantAdmin,
        Guid TenantId,
        IReadOnlySet<Guid> AdminOrgIds,
        Guid? UserId);

    private async Task<AuthorityProfile> ResolveAuthorityProfileAsync(CancellationToken cancellationToken)
    {
        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        var tenantId = _tenantContext.TenantId;
        var isTenantAdmin = !isInstanceAdmin && await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        var adminOrgIds = isInstanceAdmin || isTenantAdmin
            ? (IReadOnlySet<Guid>)new HashSet<Guid>()
            : (await _adminContext.GetAdminOrganizationIdsAsync(cancellationToken)).ToHashSet();

        return new AuthorityProfile(isInstanceAdmin, isTenantAdmin, tenantId, adminOrgIds, _adminContext.UserId);
    }

    private bool EvaluateWithProfile(
        AuthorityProfile profile,
        EventAuthoritySnapshot? eventAuthority,
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes)
    {
        if (profile.IsInstanceAdmin)
        {
            LogDecision("allow", "instance_admin", resourceKind, resourceId, action);
            return true;
        }

        if (SafeMode)
        {
            LogDecision("deny", "safe_mode_active", resourceKind, resourceId, action);
            return false;
        }

        var decision = resourceKind switch
        {
            "instance_setting" => false,
            "tenant_setting" => EvaluateTenantSettingWithProfile(profile, resourceId, action, resourceAttributes),
            "tenant" => false,
            "tenant_member" or "category" or "tag" or "location" or "location_room"
                => profile.IsTenantAdmin,
            "custom_property_definition" or "custom_property_template" or "actor"
                => action is "view" || profile.IsTenantAdmin,
            "custom_property_value"
                => action is "view" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "custom_property_projection" or "custom_property_governance"
                => profile.IsTenantAdmin,
            "platform_namespace" => action is "view",
            "organization" => profile.IsTenantAdmin || IsOrgAdminFromProfile(profile, resourceAttributes, resourceId),
            "organization_member" => IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "organization_review" => action is "create" or "view" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "group" => action is "view" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "group_member" => action is "view" or "create" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "event" or "event_session" or "event_session_group" or "event_session_agenda_item" or "event_day" or "event_agenda_item"
                => HasEventContextForProfile(profile, resourceKind, resourceId, resourceAttributes)
                    && (IsTenantAdminForResourceTenant(profile, resourceKind, resourceId, resourceAttributes)
                        || IsOrgAdminFromProfile(profile, resourceAttributes, resourceId)
                        || HasEventRolePermission(eventAuthority, resourceKind, resourceId, action, resourceAttributes)),
            "event_registration" => HasEventContextForProfile(profile, resourceKind, resourceId, resourceAttributes)
                && (action is "create" or "view"
                    || IsAdminForOrgScope(profile, resourceAttributes, resourceId)
                    || HasEventRolePermission(eventAuthority, resourceKind, resourceId, action, resourceAttributes)),
            "event_contact_share_consent" => action is "viewsharedcontacts" or "exportsharedcontacts"
                && IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "storage_object" => action is "create" or "view" || profile.IsTenantAdmin,
            "user" => EvaluateUserWithProfile(profile, resourceId, action),
            "notification" => true,
            "atproto_record" or "indexed_did" => false,
            _ => false
        };

        LogDecision(decision ? "allow" : "deny", "fallback_batch_policy", resourceKind, resourceId, action);
        return decision;
    }

    private static bool EvaluateTenantSettingWithProfile(
        AuthorityProfile profile,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes)
    {
        if (resourceAttributes?.TryGetValue("isLockedByInstance", out var lockedObj) == true && lockedObj is true)
            return false;

        return profile.IsTenantAdmin;
    }

    private static bool EvaluateUserWithProfile(AuthorityProfile profile, string resourceId, string action)
    {
        if (action is "view" or "update" && profile.UserId.HasValue
            && Guid.TryParse(resourceId, out var targetUserId)
            && targetUserId == profile.UserId.Value)
            return true;

        return profile.IsTenantAdmin;
    }

    private static bool IsOrgAdminFromProfile(
        AuthorityProfile profile,
        IDictionary<string, object>? resourceAttributes,
        string resourceId)
    {
        var orgId = ResolveOrganizationId(resourceAttributes, resourceId);
        return orgId.HasValue && profile.AdminOrgIds.Contains(orgId.Value);
    }

    private static bool IsAdminForOrgScope(
        AuthorityProfile profile,
        IDictionary<string, object>? resourceAttributes,
        string resourceId)
    {
        if (profile.IsTenantAdmin)
            return true;

        return IsOrgAdminFromProfile(profile, resourceAttributes, resourceId);
    }

    private static bool HasEventContextForProfile(
        AuthorityProfile profile,
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes)
    {
        return TryResolveEventContext(resourceKind, resourceId, resourceAttributes, out var tenantId, out _)
            && tenantId == profile.TenantId;
    }

    private static bool IsTenantAdminForResourceTenant(
        AuthorityProfile profile,
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes)
    {
        return profile.IsTenantAdmin
            && TryResolveEventContext(resourceKind, resourceId, resourceAttributes, out var tenantId, out _)
            && tenantId == profile.TenantId;
    }

    private async Task<EventAuthoritySnapshot?> ResolveBatchEventAuthorityAsync(
        AuthorityProfile profile,
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken)
    {
        if (!profile.UserId.HasValue)
            return null;

        var eventIds = checks
            .Where(check => IsEventScopedResourceKind(check.ResourceKind))
            .Select(check => TryResolveEventContext(
                check.ResourceKind,
                check.ResourceId,
                check.ResourceAttributes is null ? null : new Dictionary<string, object>(check.ResourceAttributes),
                out var tenantId,
                out var eventId)
                && tenantId == profile.TenantId
                    ? eventId
                    : Guid.Empty)
            .Where(eventId => eventId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (eventIds.Length == 0)
            return null;

        return await _eventAuthoritySnapshotService.GetForUserAndEventsAsync(
            profile.TenantId,
            profile.UserId.Value,
            eventIds,
            cancellationToken);
    }

    private static bool HasEventRolePermission(
        EventAuthoritySnapshot? eventAuthority,
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes)
    {
        if (eventAuthority is null)
            return false;

        return TryResolveEventContext(resourceKind, resourceId, resourceAttributes, out _, out var eventId)
            && eventAuthority.Events.TryGetValue(eventId, out var authority)
            && authority.PermissionCodes.Contains(PermissionCodeFor(resourceKind, action));
    }
}
