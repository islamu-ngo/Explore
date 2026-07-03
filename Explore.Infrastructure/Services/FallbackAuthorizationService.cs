// ABOUTME: Database-driven authorization service used when Cerbos PDP is unavailable.
// ABOUTME: Main dispatch class — delegates to evaluators (partial) and batch optimization (partial).

using System.Diagnostics;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Settings;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Fallback authorization service that evaluates access decisions using database-driven admin checks
/// and settings lock semantics. Used when Cerbos PDP is not configured (e.g., development, ATProto/PDS-only).
/// Implements the same IAuthorizationProvider contract for seamless DI swapping.
/// </summary>
public partial class FallbackAuthorizationService : IAuthorizationProvider
{
    private readonly IAdminContext _adminContext;
    private readonly IMachinePrincipalAccessor _machinePrincipalAccessor;
    private readonly IEventAuthoritySnapshotService _eventAuthoritySnapshotService;
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FallbackAuthorizationService> _logger;

    /// <summary>
    /// When true, only instance admin emergency access is allowed — all other requests are denied.
    /// Activated when a BYO-Cerbos tenant's PDP is unreachable and failure_mode is "closed".
    /// This prevents bypassing the tenant's potentially stricter policies via a more permissive fallback.
    /// Once activated, safe mode persists for this provider instance.
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
                "Recreate the provider instance after the PDP recovers to deactivate safe mode.");
        }
    }

    public FallbackAuthorizationService(
        IAdminContext adminContext,
        IMachinePrincipalAccessor machinePrincipalAccessor,
        IEventAuthoritySnapshotService eventAuthoritySnapshotService,
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        ILogger<FallbackAuthorizationService> logger)
    {
        _adminContext = adminContext;
        _machinePrincipalAccessor = machinePrincipalAccessor;
        _eventAuthoritySnapshotService = eventAuthoritySnapshotService;
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
        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        if (isInstanceAdmin && !RequiresDirectEventAuthority(resourceKind, action))
        {
            LogDecision("allow", "is_instance_admin", resourceKind, resourceId, action);
            return true;
        }

        // Safe-Mode: only instance admin allowed (bypassed above) — deny everything else.
        // Activated when a BYO-Cerbos tenant's PDP is unreachable with failure_mode=closed.
        if (SafeMode && !isInstanceAdmin)
        {
            LogDecision("deny", "safe_mode_active", resourceKind, resourceId, action);
            return false;
        }

        if (_machinePrincipalAccessor.IsMachineCaller)
        {
            bool machineDecision = await EvaluateMachineCallerAccessAsync(
                resourceKind, resourceId, action, resourceAttributes, cancellationToken);
            LogDecision(machineDecision ? "allow" : "deny", "machine_caller", resourceKind, resourceId, action);
            return machineDecision;
        }

        var decision = resourceKind switch
        {
            // Instance-scoped: only instance admins (bypassed above)
            "islamuevent_instance_setting" => false,

            // Tenant-scoped: tenant admin with lock semantics
            "islamuevent_tenant_setting" => await EvaluateTenantSettingAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Tenant-scoped: tenant admin only
            "islamuevent_tenant" => false, // Only instance admins can create/update/delete tenants
            "islamuevent_tenant_user_role_grant" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_category" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_tag" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_location" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_location_room" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_custom_property_definition" => await EvaluateViewableTenantResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_custom_property_template" => await EvaluateViewableTenantResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_custom_property_value" => await EvaluateViewableOrgResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_custom_property_projection" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_custom_property_governance" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_email_dispatch" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_webhook" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_platform_namespace" => action is "view",

            // Org-scoped: tenant admin or org admin
            "islamuevent_organization" => await EvaluateOrganizationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_organization_member" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_organization_review" => await EvaluateOrgReviewAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Group resources: org-scoped (tenant admin or org admin), view open
            "islamuevent_group" => await EvaluateViewableOrgResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_group_member" => await EvaluateGroupMemberAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Event resources require explicit tenant/event context before inherited authority checks.
            "islamuevent_event" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_event_session" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_event_session_group" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_event_session_agenda_item" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_event_day" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "islamuevent_event_agenda_item" => await EvaluateEventScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),

            // Event registration: all authenticated can create/view only when the parent event context is present.
            "islamuevent_event_registration" => await EvaluateEventRegistrationAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Contact share consent: tenant/org admin can view and export shared contacts
            "islamuevent_event_contact_share_consent" => await EvaluateContactShareConsentAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Storage: all authenticated can create, tenant admin for full management
            "islamuevent_storage_object" => await EvaluateStorageObjectAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // User management: instance admin or tenant admin, or self-update
            "islamuevent_user" => await EvaluateUserAccessAsync(resourceId, action, resourceAttributes, cancellationToken),

            // Notification: personal data, all authenticated can manage own notifications
            "islamuevent_notification" => true,

            // Actor subscriptions: authenticated users reach handlers; handlers enforce current-user ownership.
            "islamuevent_actor_subscription" => true,

            // AI conversations: authenticated users reach handlers; handlers enforce owner and tenant isolation.
            "islamuevent_ai_conversation" => true,

            // Actor: read-only for all authenticated; writes require tenant admin
            "islamuevent_actor" => await EvaluateActorAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),

            // ATProto federation: instance admin only for writes (bypassed above)
            "islamuevent_atproto_record" => false,
            "islamuevent_indexed_did" => false,

            _ => await EvaluateDefaultAccessAsync(resourceKind, action, resourceAttributes, cancellationToken)
        };

        LogDecision(decision ? "allow" : "deny", "fallback_policy", resourceKind, resourceId, action);
        return decision;
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
            resourceKind = "islamuevent_organization";
            attributes["organizationId"] = organizationId.Value.ToString();
        }
        else if (tenantId.HasValue)
        {
            resourceKind = "islamuevent_tenant_setting";
            attributes["tenantId"] = tenantId.Value.ToString();

            // Check if the setting is locked by instance
            var metadata = await _resolver.ResolveWithMetadataAsync(settingKey, new SettingContext(), cancellationToken);
            attributes["isLockedByInstance"] = metadata?.IsLocked == true;
        }
        else
        {
            resourceKind = "islamuevent_instance_setting";
        }

        return await IsAllowedAsync(resourceKind, settingKey, action, attributes, cancellationToken);
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

    private static bool HasRequiredEventContext(
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes) =>
        TryResolveEventContext(resourceKind, resourceId, resourceAttributes, out _, out _);

    private static bool IsEventScopedResourceKind(string resourceKind) =>
        resourceKind is "islamuevent_event"
            or "islamuevent_event_session"
            or "islamuevent_event_session_group"
            or "islamuevent_event_session_agenda_item"
            or "islamuevent_event_day"
            or "islamuevent_event_agenda_item"
            or "islamuevent_event_registration";

    private static bool RequiresDirectEventAuthority(string resourceKind, string action) =>
        resourceKind == ResourceKinds.Event &&
        action is AuthorizationActions.Update
            or AuthorizationActions.Delete
            or AuthorizationActions.Events.ManageTeam
            or AuthorizationActions.Events.ManageOwner
            or AuthorizationActions.Events.TransferOwnership
            or AuthorizationActions.Events.ManageFinance;

    private static bool IsTenantAdminEventAction(string action) =>
        action is AuthorizationActions.View
            or AuthorizationActions.Events.ViewManagement
            or AuthorizationActions.Events.ModerateLight
            or AuthorizationActions.Events.ModerateHeavy
            or AuthorizationActions.Events.Unmoderate;

    private static bool IsEventModerationAction(string action) =>
        action is AuthorizationActions.Events.ModerateLight
            or AuthorizationActions.Events.ModerateHeavy
            or AuthorizationActions.Events.Unmoderate;

    private static string PermissionCodeFor(string resourceKind, string action)
    {
        const string productNamespacePrefix = "islamuevent_";

        var permissionResourceKind = resourceKind.StartsWith(productNamespacePrefix, StringComparison.Ordinal)
            ? resourceKind[productNamespacePrefix.Length..]
            : resourceKind;
        var permissionAction = resourceKind == ResourceKinds.Event && action == AuthorizationActions.Events.ViewManagement
            ? AuthorizationActions.View
            : action;

        return string.Concat(permissionResourceKind, ":", permissionAction);
    }

    private static bool TryResolveEventContext(
        string resourceKind,
        string resourceId,
        IDictionary<string, object>? resourceAttributes,
        out Guid tenantId,
        out Guid eventId)
    {
        tenantId = Guid.Empty;
        eventId = Guid.Empty;

        if (!TryResolveGuidAttribute(resourceAttributes, "tenantId", out tenantId))
            return false;

        if (TryResolveGuidAttribute(resourceAttributes, "eventId", out eventId))
            return true;

        return resourceKind == "islamuevent_event" && Guid.TryParse(resourceId, out eventId);
    }

    private static bool TryResolveGuidAttribute(
        IDictionary<string, object>? resourceAttributes,
        string attributeName,
        out Guid value)
    {
        value = Guid.Empty;

        if (resourceAttributes?.TryGetValue(attributeName, out var attributeValue) != true)
            return false;

        if (attributeValue is Guid guidValue)
        {
            value = guidValue;
            return true;
        }

        return attributeValue is string stringValue && Guid.TryParse(stringValue, out value);
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
