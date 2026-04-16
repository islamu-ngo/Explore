// ABOUTME: Database-driven authorization service used when Cerbos PDP is unavailable.
// ABOUTME: Main dispatch class — delegates to evaluators (partial) and batch optimization (partial).

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
public partial class FallbackAuthorizationService : IAuthorizationProvider
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
            "location_room" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "custom_property_definition" => await EvaluateViewableTenantResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "custom_property_template" => await EvaluateViewableTenantResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "custom_property_value" => await EvaluateViewableOrgResourceAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "custom_property_projection" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "custom_property_governance" => await EvaluateTenantScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "platform_namespace" => action is "view",

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
            "event_day" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),
            "event_agenda_item" => await EvaluateOrgScopedAccessAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken),

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
