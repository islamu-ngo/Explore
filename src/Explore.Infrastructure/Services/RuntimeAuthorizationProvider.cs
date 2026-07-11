// ABOUTME: Authorization provider wrapper that delegates to Cerbos or Local provider based on SystemSetting.
// ABOUTME: Supports BYO (Bring Your Own) Cerbos per tenant with configurable failure modes.

using System.Diagnostics;
using System.Text.Json;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Application.Models;
using Explore.Application.Telemetry;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Authorization provider that routes decisions based on per-tenant and instance-level configuration.
/// <para><b>Decision flow (evaluated in order):</b></para>
/// <list type="number">
/// <item><b>BYO Cerbos</b>: If tenant has a custom Cerbos endpoint configured via
/// <see cref="ICerbosConfigResolver"/>, route ALL resource checks there (regardless of instance mode).
/// This allows tenants to enforce stricter or custom policies.</item>
/// <item><b>Instance Cerbos</b>: If the <c>AuthorizationProvider</c> system setting is <c>"cerbos"</c>,
/// route to the shared instance PDP. <see cref="Application.Authorization.AuthorizationScope"/> on each
/// check provides tenant context for scoped policy resolution.</item>
/// <item><b>Fallback RBAC</b>: Otherwise, use <see cref="FallbackAuthorizationService"/>
/// (database-driven role/permission checks).</item>
/// </list>
/// <para><b>Failure handling:</b></para>
/// <list type="bullet">
/// <item>Instance Cerbos failure → deny all checks. The operator chose Cerbos; falling back
/// to a potentially more permissive local RBAC would silently bypass intended policies.</item>
/// <item>BYO Cerbos failure with <c>FailureMode.Closed</c> → Safe-Mode activated (one-way latch):
/// deny all except instance admin. Prevents bypassing stricter tenant policies.</item>
/// <item>BYO Cerbos failure with <c>FailureMode.Open</c> → Standard fallback RBAC
/// (tenant accepts permissive fallback risk).</item>
/// </list>
/// <para><b>Setting access</b>: Always uses instance-level provider (never BYO).
/// Settings are platform governance, not tenant-customizable.</para>
/// </summary>
public sealed class RuntimeAuthorizationProvider : IAuthorizationProvider, IAuthorizationProviderModeCacheInvalidator
{
    private readonly CerbosAuthorizationService _cerbosProvider;
    private readonly FallbackAuthorizationService _localProvider;
    private readonly ICerbosConfigResolver _cerbosConfigResolver;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ISupportAccessSessionService? _supportAccessSessionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RuntimeAuthorizationProvider> _logger;
    private readonly BusinessMetrics? _metrics;

    private const string InstanceModeCacheKey = "AuthorizationProvider_Mode";
    private static readonly TimeSpan InstanceModeCacheDuration = TimeSpan.FromMinutes(1);

    public RuntimeAuthorizationProvider(
        CerbosAuthorizationService cerbosProvider,
        FallbackAuthorizationService localProvider,
        ICerbosConfigResolver cerbosConfigResolver,
        ISystemSettingRepository systemSettingRepository,
        IMemoryCache cache,
        ILogger<RuntimeAuthorizationProvider> logger,
        ISupportAccessSessionService? supportAccessSessionService = null,
        BusinessMetrics? metrics = null)
    {
        _cerbosProvider = cerbosProvider;
        _localProvider = localProvider;
        _cerbosConfigResolver = cerbosConfigResolver;
        _systemSettingRepository = systemSettingRepository;
        _supportAccessSessionService = supportAccessSessionService;
        _cache = cache;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> IsAllowedAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default)
    {
        var checks = new[]
        {
            new AuthorizationCheck(
                resourceKind,
                resourceId,
                action,
                resourceAttributes is null ? null : new Dictionary<string, object>(resourceAttributes))
        };

        var results = await IsAllowedBatchAsync(checks, cancellationToken);
        return results.Count > 0 && results[0];
    }

    public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
            return [];

        var supportBoundary = await ApplySupportAccessBoundaryAsync(checks, cancellationToken);
        if (supportBoundary.EffectiveChecks.Count == 0)
            return supportBoundary.Results;

        var effectiveChecks = supportBoundary.EffectiveChecks;
        IReadOnlyList<bool> evaluatedResults;

        if (UsesSettingAuthorization(effectiveChecks))
        {
            evaluatedResults = await ExecuteInstanceProviderAsync(effectiveChecks, cancellationToken);
            return supportBoundary.Complete(evaluatedResults);
        }

        // Step 1: Check if the tenant has a BYO Cerbos configuration (works regardless of instance mode)
        CerbosConfiguration? byoConfig;
        try
        {
            byoConfig = await ResolveTenantByoConfigAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Failed to resolve tenant BYO Cerbos config for batch ({Count} checks). Activating safe mode to avoid local RBAC bypass. FailureType={FailureType}",
                effectiveChecks.Count,
                ex.GetType().Name);
            _localProvider.ActivateSafeMode();
            evaluatedResults = await _localProvider.IsAllowedBatchAsync(effectiveChecks, cancellationToken);
            return supportBoundary.Complete(evaluatedResults);
        }

        if (byoConfig is not null)
        {
            evaluatedResults = await ExecuteByoAsync(byoConfig, effectiveChecks, cancellationToken);
            return supportBoundary.Complete(evaluatedResults);
        }

        // These checks have canonical local parity because handlers or pre-create flows enforce
        // the resource-specific policy after the coarse authorization gate. Keep them local in
        // instance-Cerbos mode so stale PDP policy packages cannot block canonical handlers.
        var localCheckIndexes = GetHandlerOwnedLocalCheckIndexes(effectiveChecks);
        if (localCheckIndexes.Count == effectiveChecks.Count)
        {
            evaluatedResults = await _localProvider.IsAllowedBatchAsync(effectiveChecks, cancellationToken);
            return supportBoundary.Complete(evaluatedResults);
        }

        if (localCheckIndexes.Count > 0)
        {
            evaluatedResults = await ExecuteMixedLocalAndInstanceAsync(effectiveChecks, localCheckIndexes, cancellationToken);
            return supportBoundary.Complete(evaluatedResults);
        }

        // Step 2: Fall back to instance-level provider resolution (Cerbos or Local)
        evaluatedResults = await ExecuteInstanceProviderAsync(effectiveChecks, cancellationToken);
        return supportBoundary.Complete(evaluatedResults);
    }

    private async Task<IReadOnlyList<bool>> ExecuteMixedLocalAndInstanceAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        IReadOnlyList<int> localCheckIndexes,
        CancellationToken cancellationToken)
    {
        var results = new bool[checks.Count];

        var localChecks = localCheckIndexes
            .Select(index => checks[index])
            .ToArray();
        var localResults = await _localProvider.IsAllowedBatchAsync(localChecks, cancellationToken);
        for (var i = 0; i < localCheckIndexes.Count; i++)
        {
            results[localCheckIndexes[i]] = i < localResults.Count && localResults[i];
        }

        var localIndexSet = localCheckIndexes.ToHashSet();
        var instanceCheckIndexes = Enumerable.Range(0, checks.Count)
            .Where(index => !localIndexSet.Contains(index))
            .ToArray();

        if (instanceCheckIndexes.Length == 0)
            return results;

        var instanceChecks = instanceCheckIndexes
            .Select(index => checks[index])
            .ToArray();
        var instanceResults = await ExecuteInstanceProviderAsync(instanceChecks, cancellationToken);
        for (var i = 0; i < instanceCheckIndexes.Length; i++)
        {
            results[instanceCheckIndexes[i]] = i < instanceResults.Count && instanceResults[i];
        }

        return results;
    }

    private async Task<IReadOnlyList<bool>> ExecuteInstanceProviderAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken)
    {
        var provider = await ResolveInstanceProviderAsync(cancellationToken);

        try
        {
            return provider == _cerbosProvider
                ? await _cerbosProvider.IsAllowedBatchWithUnavailableSignalAsync(checks, cancellationToken)
                : await provider.IsAllowedBatchAsync(checks, cancellationToken);
        }
        catch (Exception ex) when (provider == _cerbosProvider)
        {
            if (UsesSettingAuthorization(checks))
            {
                _logger.LogWarning(
                    "Instance Cerbos provider unavailable for setting authorization batch ({Count} checks). " +
                    "Using local setting-governance parity so administrator affordances match setting command authorization. FailureType={FailureType}",
                    checks.Count,
                    ex.GetType().Name);
                return await _localProvider.IsAllowedBatchAsync(checks, cancellationToken);
            }

            // When Cerbos is the configured instance authorization provider and is unavailable,
            // deny all checks. Falling back to a potentially more permissive local RBAC
            // would silently bypass the policies the operator explicitly chose to enforce.
            _logger.LogError(
                "Instance Cerbos provider unavailable for batch ({Count} checks). " +
                "Denying all — Cerbos is the configured authorization provider. " +
                "Restore Cerbos connectivity or switch authorization.provider setting to resolve. FailureType={FailureType}",
                checks.Count,
                ex.GetType().Name);
            return checks.Select(_ => false).ToArray();
        }
    }

    public async Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var supportBoundary = await ApplySupportAccessBoundaryAsync(
            [CreateSettingAuthorizationCheck(settingKey, action, tenantId, organizationId)],
            cancellationToken);
        if (supportBoundary.EffectiveChecks.Count == 0)
            return supportBoundary.Results[0];

        // BYO Cerbos only applies to resource checks, not setting access.
        // Settings are governed by the instance-level provider.
        var provider = await ResolveInstanceProviderAsync(cancellationToken);

        try
        {
            return await provider.CheckSettingAccessAsync(settingKey, action, tenantId, organizationId, cancellationToken);
        }
        catch (Exception ex) when (provider == _cerbosProvider)
        {
            _logger.LogError(
                "Instance Cerbos provider unavailable for setting check {SettingKey}:{Action}. " +
                "Denying — Cerbos is the configured authorization provider. FailureType={FailureType}",
                settingKey,
                action,
                ex.GetType().Name);
            return false;
        }
    }

    private async Task<SupportAccessBoundaryResult> ApplySupportAccessBoundaryAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken)
    {
        if (_supportAccessSessionService is null)
            return SupportAccessBoundaryResult.PassThrough(checks);

        var supportContext = await _supportAccessSessionService.GetCurrentAsync(cancellationToken);
        if (!supportContext.WasForwarded && !supportContext.IsActive)
            return SupportAccessBoundaryResult.PassThrough(checks);

        AddSupportAccessTraceTags(supportContext);

        var results = new bool[checks.Count];
        var effectiveChecks = new List<AuthorizationCheck>(checks.Count);
        var originalIndexes = new List<int>(checks.Count);

        for (var i = 0; i < checks.Count; i++)
        {
            var enrichedCheck = EnrichWithSupportAccessContext(checks[i], supportContext);
            if (!IsSupportAccessBoundedResource(enrichedCheck))
            {
                effectiveChecks.Add(enrichedCheck);
                originalIndexes.Add(i);
                continue;
            }

            var denialReason = GetSupportAccessBoundaryDenialReason(supportContext, enrichedCheck);
            if (denialReason is null)
            {
                effectiveChecks.Add(enrichedCheck);
                originalIndexes.Add(i);
                continue;
            }

            _metrics?.RecordSupportAccessBoundaryDenial(
                denialReason,
                enrichedCheck.Action,
                supportContext.Mode?.ToString());
            AddSupportAccessBoundaryDeniedTraceEvent(enrichedCheck, denialReason);
            _logger.LogWarning(
                "Support-access authorization boundary denied resource={ResourceKind}/{ResourceId} action={Action} reason={Reason} sessionId={SupportAccessSessionId}",
                enrichedCheck.ResourceKind,
                enrichedCheck.ResourceId,
                enrichedCheck.Action,
                denialReason,
                supportContext.SessionId?.ToString("D") ?? "none");
        }

        return new SupportAccessBoundaryResult(effectiveChecks, originalIndexes, results);
    }

    private static AuthorizationCheck EnrichWithSupportAccessContext(
        AuthorizationCheck check,
        ISupportAccessContext supportContext)
    {
        var attributes = check.ResourceAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(check.ResourceAttributes);

        attributes["supportAccessActive"] = supportContext.IsActive;
        attributes["supportAccessWasForwarded"] = supportContext.WasForwarded;
        attributes["supportAccessAllowsWrites"] = supportContext.AllowsWrites;

        AddIfPresent(attributes, "supportAccessSessionId", supportContext.SessionId);
        AddIfPresent(attributes, "supportAccessActorUserId", supportContext.ActorUserId);
        AddIfPresent(attributes, "supportAccessTargetTenantId", supportContext.TargetTenantId);
        AddIfPresent(attributes, "supportAccessTargetTenantUserId", supportContext.TargetTenantUserId);

        if (supportContext.Mode.HasValue)
            attributes["supportAccessMode"] = supportContext.Mode.Value.ToString();

        return check with { ResourceAttributes = attributes };
    }

    private static void AddSupportAccessTraceTags(ISupportAccessContext supportContext)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("support_access.active", supportContext.IsActive);
        activity.SetTag("support_access.was_forwarded", supportContext.WasForwarded);
        activity.SetTag("support_access.allows_writes", supportContext.AllowsWrites);
        activity.SetTag("support_access.mode", supportContext.Mode?.ToString() ?? "unknown");
    }

    private static void AddSupportAccessBoundaryDeniedTraceEvent(AuthorizationCheck check, string reason)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        var tags = new ActivityTagsCollection
        {
            ["support_access.denial.reason"] = reason,
            ["resource.kind"] = check.ResourceKind,
            ["resource.action"] = check.Action
        };
        activity.AddEvent(new ActivityEvent("support_access.authorization_boundary_denied", tags: tags));
    }

    private static string? GetSupportAccessBoundaryDenialReason(
        ISupportAccessContext supportContext,
        AuthorizationCheck check)
    {
        if (supportContext.WasForwarded && !supportContext.IsActive)
            return "support_access_inactive";

        if (!supportContext.IsActive)
            return null;

        if (!supportContext.AllowsWrites && !IsReadOnlyCompatibleAction(check.Action))
            return "support_access_read_only";

        if (!supportContext.TargetTenantId.HasValue || supportContext.TargetTenantId.Value == Guid.Empty)
            return "support_access_missing_target_tenant";

        if (!TryResolveGuidAttribute(check.ResourceAttributes, "tenantId", out var resourceTenantId))
            return "support_access_missing_tenant_context";

        return resourceTenantId == supportContext.TargetTenantId.Value
            ? null
            : "support_access_target_tenant_mismatch";
    }

    private static bool IsSupportAccessBoundedResource(AuthorizationCheck check)
    {
        if (HasTenantAttribute(check.ResourceAttributes))
            return check.ResourceKind is not ResourceKinds.SupportAccessSession;

        return check.ResourceKind is
            ResourceKinds.Tenant or
            ResourceKinds.TenantSetting or
            ResourceKinds.TenantUserRoleGrant or
            ResourceKinds.Category or
            ResourceKinds.Tag or
            ResourceKinds.Location or
            ResourceKinds.LocationRoom or
            ResourceKinds.CustomPropertyDefinition or
            ResourceKinds.CustomPropertyTemplate or
            ResourceKinds.CustomPropertyValue or
            ResourceKinds.CustomPropertyProjection or
            ResourceKinds.CustomPropertyGovernance or
            ResourceKinds.EmailDispatch or
            ResourceKinds.Webhook or
            ResourceKinds.Organization or
            ResourceKinds.OrganizationMember or
            ResourceKinds.OrganizationReview or
            ResourceKinds.Group or
            ResourceKinds.GroupMember or
            ResourceKinds.Event or
            ResourceKinds.EventSession or
            ResourceKinds.EventSessionGroup or
            ResourceKinds.EventSessionAgendaItem or
            ResourceKinds.EventDay or
            ResourceKinds.EventAgendaItem or
            ResourceKinds.EventRegistration or
            ResourceKinds.EventContactShareConsent or
            ResourceKinds.StorageObject or
            ResourceKinds.Actor;
    }

    private static bool IsReadOnlyCompatibleAction(string action)
    {
        return action is
                AuthorizationActions.View or
                AuthorizationActions.SupportAccessSessions.List or
                AuthorizationActions.SupportAccessSessions.ViewAudit or
                AuthorizationActions.Events.ViewManagement or
                AuthorizationActions.StorageObjects.Download or
                AuthorizationActions.StorageObjects.PresignedDownload or
                AuthorizationActions.ViewSharedContacts or
                AuthorizationActions.ExportSharedContacts
            || action.EndsWith(":view", StringComparison.Ordinal)
            || action.EndsWith(":view-delivery", StringComparison.Ordinal);
    }

    private static AuthorizationCheck CreateSettingAuthorizationCheck(
        string settingKey,
        string action,
        Guid? tenantId,
        Guid? organizationId)
    {
        var attributes = new Dictionary<string, object> { ["settingKey"] = settingKey };
        if (organizationId.HasValue)
        {
            attributes["organizationId"] = organizationId.Value.ToString("D");
            return new AuthorizationCheck(ResourceKinds.Organization, settingKey, action, attributes);
        }

        if (tenantId.HasValue)
        {
            attributes["tenantId"] = tenantId.Value.ToString("D");
            return new AuthorizationCheck(ResourceKinds.TenantSetting, settingKey, action, attributes);
        }

        return new AuthorizationCheck(ResourceKinds.InstanceSetting, settingKey, action, attributes);
    }

    private static bool HasTenantAttribute(IReadOnlyDictionary<string, object>? resourceAttributes) =>
        resourceAttributes?.ContainsKey("tenantId") == true;

    private static bool TryResolveGuidAttribute(
        IReadOnlyDictionary<string, object>? resourceAttributes,
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

    private static void AddIfPresent(IDictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue && value.Value != Guid.Empty)
            attributes[key] = value.Value.ToString("D");
    }

    public void InvalidateInstanceMode()
    {
        _cache.Remove(InstanceModeCacheKey);
        _logger.LogInformation("Authorization provider mode cache invalidated");
    }

    /// <summary>
    /// Resolves BYO Cerbos config for the current tenant. Returns null if tenant uses instance PDP.
    /// </summary>
    private async Task<CerbosConfiguration?> ResolveTenantByoConfigAsync(CancellationToken cancellationToken)
    {
        var config = await _cerbosConfigResolver.ResolveAsync(cancellationToken);

        if (config is null || config.IsInstanceDefault || config.Mode == CerbosMode.Instance)
            return null;

        return config;
    }

    /// <summary>
    /// Executes authorization checks against a BYO Cerbos endpoint.
    /// On failure, applies the tenant's configured failure mode (closed=safe-mode, open=fallback RBAC).
    /// </summary>
    private async Task<IReadOnlyList<bool>> ExecuteByoAsync(
        CerbosConfiguration config,
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Routing {Count} auth checks to BYO Cerbos endpoint", checks.Count);
            return await _cerbosProvider.IsAllowedBatchWithEndpointAsync(config.Endpoint, checks, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "BYO Cerbos PDP unreachable. Applying failure_mode={FailureMode}. FailureType={FailureType}",
                config.FailureMode,
                ex.GetType().Name);

            if (config.FailureMode == CerbosFailureMode.Closed)
            {
                // Safe-Mode: only instance admin allowed, deny everything else.
                // Never fall back to instance PDP — tenant policies might be stricter.
                // Safe mode is a one-way latch for this fallback provider instance.
                _localProvider.ActivateSafeMode();
                return await _localProvider.IsAllowedBatchAsync(checks, cancellationToken);
            }

            // Open mode: standard RBAC fallback — tenant accepts the risk
            _logger.LogInformation("BYO Cerbos failure_mode=open; using standard FallbackAuthorizationService");
            return await _localProvider.IsAllowedBatchAsync(checks, cancellationToken);
        }
    }

    /// <summary>
    /// Resolves the instance-level provider (Cerbos or Local) based on SystemSetting.
    /// </summary>
    private async Task<IAuthorizationProvider> ResolveInstanceProviderAsync(CancellationToken cancellationToken)
    {
        var mode = await _cache.GetOrCreateAsync(InstanceModeCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = InstanceModeCacheDuration;

            try
            {
                var setting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Security.AuthorizationProvider);
                var value = NormalizeProviderMode(setting?.Value);

                if (value is "cerbos")
                {
                    _logger.LogDebug("Authorization provider resolved to: Cerbos (from SystemSetting)");
                    return "cerbos";
                }

                _logger.LogDebug("Authorization provider resolved to: Local (setting={Value})", value ?? "null");
                return "local";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Failed to read authorization provider setting. Using Cerbos fail-closed path. FailureType={FailureType}",
                    ex.GetType().Name);
                return "cerbos";
            }
        });

        return mode == "cerbos" ? _cerbosProvider : _localProvider;
    }

    private static IReadOnlyList<int> GetHandlerOwnedLocalCheckIndexes(IReadOnlyList<AuthorizationCheck> checks)
    {
        var indexes = new List<int>();
        for (var i = 0; i < checks.Count; i++)
        {
            if (IsHandlerOwnedLocalCheck(checks[i]))
                indexes.Add(i);
        }

        return indexes;
    }

    private static bool IsHandlerOwnedLocalCheck(AuthorizationCheck check)
    {
        return check.ResourceKind == ResourceKinds.AiConversation
            || IsHandlerOwnedUserProfileUpdateCheck(check)
            || IsHandlerOwnedEventCreateCheck(check)
            || IsHandlerOwnedOrganizationCreateCheck(check)
            || IsHandlerOwnedEventSessionPreCreateCheck(check)
            || IsHandlerOwnedStorageUploadSessionCheck(check);
    }

    private static bool IsHandlerOwnedUserProfileUpdateCheck(AuthorizationCheck check)
    {
        return check.ResourceKind == ResourceKinds.User
            && check.Action == AuthorizationActions.Update
            && Guid.TryParse(check.ResourceId, out _);
    }

    private static bool IsHandlerOwnedEventCreateCheck(AuthorizationCheck check)
    {
        return check.ResourceKind == ResourceKinds.Event
            && check.Action == AuthorizationActions.Create
            && string.Equals(check.ResourceId, "create", StringComparison.Ordinal);
    }

    private static bool IsHandlerOwnedOrganizationCreateCheck(AuthorizationCheck check)
    {
        return check.ResourceKind == ResourceKinds.Organization
            && check.Action == AuthorizationActions.Create
            && string.Equals(check.ResourceId, CreateOrganizationCommand.PreCreateResourceId, StringComparison.Ordinal)
            && HasAuthorizationPhase(check, CreateOrganizationCommand.PreCreateAuthorizationPhase);
    }

    private static bool IsHandlerOwnedEventSessionPreCreateCheck(AuthorizationCheck check)
    {
        return check.ResourceKind == ResourceKinds.EventSession
            && check.Action == AuthorizationActions.Create
            && HasAuthorizationPhase(check, AuthorizationPhases.PreCreate);
    }

    private static bool IsHandlerOwnedStorageUploadSessionCheck(AuthorizationCheck check)
    {
        return check.ResourceKind == ResourceKinds.StorageObject
            && check.Action == AuthorizationActions.Create
            && (string.Equals(check.ResourceId, nameof(CreateStorageUploadSessionCommand), StringComparison.Ordinal)
                || Guid.TryParse(check.ResourceId, out _));
    }

    private static bool HasAuthorizationPhase(AuthorizationCheck check, string phase)
    {
        return check.ResourceAttributes?.TryGetValue("authorizationPhase", out var value) == true
            && string.Equals(value?.ToString(), phase, StringComparison.Ordinal);
    }

    private static bool UsesSettingAuthorization(IReadOnlyList<AuthorizationCheck> checks)
    {
        return checks.Count > 0
            && checks.All(check => check.ResourceKind is ResourceKinds.InstanceSetting or ResourceKinds.TenantSetting);
    }

    private sealed record SupportAccessBoundaryResult(
        IReadOnlyList<AuthorizationCheck> EffectiveChecks,
        IReadOnlyList<int> OriginalIndexes,
        bool[] Results)
    {
        public static SupportAccessBoundaryResult PassThrough(IReadOnlyList<AuthorizationCheck> checks)
        {
            return new SupportAccessBoundaryResult(
                checks,
                Enumerable.Range(0, checks.Count).ToArray(),
                new bool[checks.Count]);
        }

        public IReadOnlyList<bool> Complete(IReadOnlyList<bool> evaluatedResults)
        {
            for (var i = 0; i < OriginalIndexes.Count; i++)
            {
                Results[OriginalIndexes[i]] = i < evaluatedResults.Count && evaluatedResults[i];
            }

            return Results;
        }
    }

    private static string NormalizeProviderMode(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return "local";

        var trimmedValue = rawValue.Trim();

        if (TryDeserializeString(trimmedValue, out var deserializedValue)
            && !string.IsNullOrWhiteSpace(deserializedValue))
        {
            return deserializedValue.Trim().ToLowerInvariant();
        }

        return trimmedValue.Trim('"').Trim().ToLowerInvariant();
    }

    private static bool TryDeserializeString(string rawValue, out string? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<string>(rawValue);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
        catch (NotSupportedException)
        {
            value = null;
            return false;
        }
    }
}
