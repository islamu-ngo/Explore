// ABOUTME: Database-driven authorization service used when Cerbos PDP is unavailable.
// Evaluates access control using IAdminContext and IHierarchicalSettingsResolver for lock semantics.

using System.Diagnostics;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Settings;
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
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FallbackAuthorizationService> _logger;

    /// <summary>
    /// When true, only instance admin emergency access is allowed — all other requests are denied.
    /// Activated when a BYO-Cerbos tenant's PDP is unreachable and failure_mode is "closed".
    /// This prevents bypassing the tenant's potentially stricter policies via a more permissive fallback.
    /// Once activated, safe mode persists until the instance is restarted.
    /// </summary>
    public bool SafeMode { get; private set; }

    private bool _safeModeLogged;

    /// <summary>
    /// Transitions the service to safe mode. Only instance admins will be allowed.
    /// This is a one-way latch — safe mode cannot be deactivated programmatically.
    /// Logs at Critical level on first activation.
    /// </summary>
    public void ActivateSafeMode()
    {
        SafeMode = true;

        if (!_safeModeLogged)
        {
            _safeModeLogged = true;
            _logger.LogCritical(
                "Safe mode ACTIVATED. Only instance admin access is permitted. " +
                "Cause: BYO Cerbos PDP unreachable with failure_mode=closed. " +
                "Restart the instance to deactivate safe mode.");
        }
    }

    public FallbackAuthorizationService(
        IAdminContext adminContext,
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        ILogger<FallbackAuthorizationService> logger)
    {
        _adminContext = adminContext;
        _resolver = resolver;
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

        // Safe-Mode: only instance admin allowed (bypassed above) — deny everything else.
        // Activated when a BYO-Cerbos tenant's PDP is unreachable with failure_mode=closed.
        if (SafeMode)
        {
            LogDecision("deny", "safe_mode_active", resourceKind, resourceId, action);
            return false;
        }

        var decision = resourceKind switch
        {
            // Instance-scoped: only instance admins (bypassed above)
            "instance_setting" => false,

            // Tenant-scoped: tenant admin with lock semantics
            "tenant_setting" => await EvaluateTenantSettingAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Tenant-scoped: tenant admin only
            "tenant" => false, // Only instance admins can create/update/delete tenants
            "tenant_member" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "category" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "tag" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "location" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "custom_property_definition" => await EvaluateViewableTenantResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),

            // Org-scoped: tenant admin or org admin
            "organization" => await EvaluateOrganizationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
            "organization_member" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "organization_review" => await EvaluateOrgReviewAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Group resources: org-scoped (tenant admin or org admin), view open
            "group" => await EvaluateViewableOrgResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "group_member" => await EvaluateGroupMemberAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Event resources: org-scoped (tenant admin or org admin)
            "event" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "event_session" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "event_session_agenda_item" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),

            // Event registration: all authenticated can create, org/tenant admin can manage
            "event_registration" => await EvaluateEventRegistrationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Contact share consent: tenant/org admin can view and export shared contacts
            "event_contact_share_consent" => await EvaluateContactShareConsentAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Storage: all authenticated can create, tenant admin for full management
            "storage_object" => await EvaluateStorageObjectAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // User management: instance admin or tenant admin, or self-update
            "user" => await EvaluateUserAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Notification: personal data, all authenticated can manage own notifications
            "notification" => true,

            // Actor: read-only for all authenticated; writes require tenant admin
            "actor" => await EvaluateActorAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),

            // ATProto federation: instance admin only for writes (bypassed above)
            "atproto_record" => false,
            "indexed_did" => false,

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

        // For small batches, the overhead of pre-resolution is not worth it.
        // Delegate directly to the single-check path.
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

        // Pre-resolve admin authority once for the entire batch.
        // IAdminContext caches internally, but this eliminates repeated async overhead per check.
        var profile = await ResolveAuthorityProfileAsync(cancellationToken);

        var results = new bool[checks.Count];
        for (var i = 0; i < checks.Count; i++)
        {
            var check = checks[i];
            var attributes = check.ResourceAttributes is null
                ? null
                : new Dictionary<string, object>(check.ResourceAttributes);
            results[i] = EvaluateWithProfile(profile, check.ResourceKind, check.ResourceId, check.Action, attributes);
        }

        return results;
    }

    /// <summary>
    /// Pre-resolved authority profile for batch evaluation.
    /// Eliminates repeated async calls to IAdminContext per check.
    /// </summary>
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

    /// <summary>
    /// Synchronous evaluation using a pre-resolved authority profile.
    /// Mirrors the logic of IsAllowedAsync but without any async DB calls.
    /// </summary>
    private bool EvaluateWithProfile(
        AuthorityProfile profile,
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
            "tenant_member" or "category" or "tag" or "location"
                => profile.IsTenantAdmin,
            "custom_property_definition" or "actor"
                => action is "view" || profile.IsTenantAdmin,
            "organization" => profile.IsTenantAdmin || IsOrgAdminFromProfile(profile, resourceAttributes, resourceId),
            "organization_member" => IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "organization_review" => action is "create" or "view" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "group" => action is "view" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "group_member" => action is "view" or "create" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "event" or "event_session" or "event_session_agenda_item"
                => IsAdminForOrgScope(profile, resourceAttributes, resourceId),
            "event_registration" => action is "create" or "view" || IsAdminForOrgScope(profile, resourceAttributes, resourceId),
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

    private bool EvaluateTenantSettingWithProfile(
        AuthorityProfile profile,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes)
    {
        if (resourceAttributes?.TryGetValue("isLockedByInstance", out var lockedObj) == true && lockedObj is true)
            return false;

        return profile.IsTenantAdmin;
    }

    private bool EvaluateUserWithProfile(AuthorityProfile profile, string resourceId, string action)
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

        // Safe-Mode: only instance admin allowed (bypassed above)
        if (SafeMode)
            return false;

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
            var metadata = await _resolver.ResolveWithMetadataAsync(settingKey, new SettingContext(), cancellationToken);
            attributes["isLockedByInstance"] = metadata?.IsLocked == true;
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

    /// <summary>
    /// Evaluates access for tenant-scoped resources (category, tag, location, tenant_member).
    /// Tenant admins can perform all CRUD operations within their tenant.
    /// </summary>
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

    /// <summary>
    /// Evaluates access for org-scoped resources (event, event_session, event_session_agenda_item, organization_member).
    /// Tenant admins can manage all resources within their tenant; org admins can manage resources in their org.
    /// </summary>
    private async Task<bool> EvaluateOrgScopedAccessAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // Tenant admin can manage all org-scoped resources within their tenant
        var tenantId = ResolveTenantId(resourceAttributes);
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            LogDecision("allow", "tenant_admin=true", resourceKind, resourceId, action);
            return true;
        }

        // Org admin can manage resources within their organization
        var orgId = ResolveOrganizationId(resourceAttributes, resourceId);
        if (orgId.HasValue && await _adminContext.IsOrganizationAdminAsync(orgId.Value, cancellationToken))
        {
            LogDecision("allow", "organization_admin=true", resourceKind, resourceId, action);
            return true;
        }

        LogDecision("deny", "no_admin_authority", resourceKind, resourceId, action);
        return false;
    }

    /// <summary>
    /// Evaluates access for organization reviews.
    /// All authenticated users can create reviews; tenant/org admins can manage them.
    /// </summary>
    private async Task<bool> EvaluateOrgReviewAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // All authenticated users can create and view reviews
        if (action is "create" or "view")
            return true;

        // Update/delete requires tenant or org admin
        return await EvaluateOrgScopedAccessAsync("organization_review", resourceId, action, resourceAttributes, cancellationToken);
    }

    /// <summary>
    /// Evaluates access for event registrations.
    /// All authenticated users can create registrations; org/tenant admin can manage them.
    /// </summary>
    private async Task<bool> EvaluateEventRegistrationAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // All authenticated users can create and view registrations
        if (action is "create" or "view")
            return true;

        // Update/delete requires tenant or org admin
        return await EvaluateOrgScopedAccessAsync("event_registration", resourceId, action, resourceAttributes, cancellationToken);
    }

    /// <summary>
    /// Evaluates access for storage objects.
    /// All authenticated users can create and view; tenant admin for full management.
    /// </summary>
    private async Task<bool> EvaluateStorageObjectAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // All authenticated users can create and view storage objects
        if (action is "create" or "view")
            return true;

        // Update/delete requires tenant admin
        return await EvaluateTenantScopedAccessAsync("storage_object", resourceId, action, resourceAttributes, cancellationToken);
    }

    /// <summary>
    /// Evaluates access for user management.
    /// Instance admins are bypassed above. Tenant admins can manage users within their tenant.
    /// Users can update their own profile (self-service).
    /// </summary>
    private async Task<bool> EvaluateUserAccessAsync(
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes,
        CancellationToken cancellationToken)
    {
        // Self-service: users can view/update their own profile
        if (action is "view" or "update" && _adminContext.UserId.HasValue
            && Guid.TryParse(resourceId, out var targetUserId)
            && targetUserId == _adminContext.UserId.Value)
        {
            LogDecision("allow", "self_service", "user", resourceId, action);
            return true;
        }

        // Tenant admin can manage users within their tenant
        var tenantId = ResolveTenantId(resourceAttributes);
        var isTenantAdmin = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        LogDecision(isTenantAdmin ? "allow" : "deny", $"tenant_admin={isTenantAdmin}", "user", resourceId, action);
        return isTenantAdmin;
    }

    /// <summary>
    /// Evaluates access for tenant-scoped resources that are viewable by all authenticated users.
    /// All authenticated can view; tenant admin required for mutations.
    /// Used for: custom_property_definition.
    /// </summary>
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

    /// <summary>
    /// Evaluates access for org-scoped resources that are viewable by all authenticated users.
    /// All authenticated can view; tenant admin or org admin required for mutations.
    /// Used for: group.
    /// </summary>
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

    /// <summary>
    /// Evaluates access for group members.
    /// All authenticated users can view and create (join); tenant/org admin can manage.
    /// </summary>
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

    /// <summary>
    /// Evaluates access for contact share consent operations.
    /// Tenant admin or org admin can view and export shared contacts.
    /// Matches Cerbos policy: event_contact_share_consent.yaml.
    /// </summary>
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

    /// <summary>
    /// Evaluates access for actor resources.
    /// All authenticated can view; tenant admin required for mutations (actors are system-managed).
    /// </summary>
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

    /// <summary>
    /// Resolves the tenant ID from resource attributes, falling back to the current tenant context.
    /// </summary>
    private Guid ResolveTenantId(IDictionary<string, object>? resourceAttributes)
    {
        if (resourceAttributes?.TryGetValue("tenantId", out var tenantIdObj) == true)
        {
            if (tenantIdObj is Guid tid) return tid;
            if (tenantIdObj is string s && Guid.TryParse(s, out var parsed)) return parsed;
        }

        return _tenantContext.TenantId;
    }

    /// <summary>
    /// Resolves the organization ID from resource attributes or resource ID.
    /// Returns null if no organization ID can be determined.
    /// </summary>
    private static Guid? ResolveOrganizationId(IDictionary<string, object>? resourceAttributes, string resourceId)
    {
        if (resourceAttributes?.TryGetValue("organizationId", out var orgIdObj) == true)
        {
            if (orgIdObj is Guid oid) return oid;
            if (orgIdObj is string s && Guid.TryParse(s, out var parsed)) return parsed;
        }

        return Guid.TryParse(resourceId, out var fromId) ? fromId : null;
    }

    private void LogDecision(
        string decision,
        string reason,
        string resourceKind,
        string resourceId,
        string action)
    {
        var correlationId = Activity.Current?.Id ?? string.Empty;

        // Log level depends on decision: allow→Debug (high volume), deny→Warning (actionable)
        if (decision == "allow")
        {
            _logger.LogDebug(
                "Fallback authorization decision: {Decision} reason={Reason} resource={ResourceKind}/{ResourceId} action={Action} correlationId={CorrelationId}",
                decision, reason, resourceKind, resourceId, action, correlationId);
        }
        else
        {
            _logger.LogWarning(
                "Fallback authorization decision: {Decision} reason={Reason} resource={ResourceKind}/{ResourceId} action={Action} correlationId={CorrelationId}",
                decision, reason, resourceKind, resourceId, action, correlationId);
        }
    }
}
