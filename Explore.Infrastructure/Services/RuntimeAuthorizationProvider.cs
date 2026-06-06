// ABOUTME: Authorization provider wrapper that delegates to Cerbos or Local provider based on SystemSetting.
// ABOUTME: Supports BYO (Bring Your Own) Cerbos per tenant with configurable failure modes.

using System.Text.Json;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Models;
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
    private readonly IMemoryCache _cache;
    private readonly ILogger<RuntimeAuthorizationProvider> _logger;

    private const string InstanceModeCacheKey = "AuthorizationProvider_Mode";
    private static readonly TimeSpan InstanceModeCacheDuration = TimeSpan.FromMinutes(1);

    public RuntimeAuthorizationProvider(
        CerbosAuthorizationService cerbosProvider,
        FallbackAuthorizationService localProvider,
        ICerbosConfigResolver cerbosConfigResolver,
        ISystemSettingRepository systemSettingRepository,
        IMemoryCache cache,
        ILogger<RuntimeAuthorizationProvider> logger)
    {
        _cerbosProvider = cerbosProvider;
        _localProvider = localProvider;
        _cerbosConfigResolver = cerbosConfigResolver;
        _systemSettingRepository = systemSettingRepository;
        _cache = cache;
        _logger = logger;
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
                checks.Count,
                ex.GetType().Name);
            _localProvider.ActivateSafeMode();
            return await _localProvider.IsAllowedBatchAsync(checks, cancellationToken);
        }

        if (byoConfig is not null)
            return await ExecuteByoAsync(byoConfig, checks, cancellationToken);

        // AI conversations are handler-owned self-service resources: the Application handlers
        // enforce current-user ownership and tenant isolation, matching the bundled Cerbos policy
        // that allows authenticated users to reach these handlers.
        if (UsesHandlerOwnedAiConversationAuthorization(checks))
        {
            return await _localProvider.IsAllowedBatchAsync(checks, cancellationToken);
        }

        // Step 2: Fall back to instance-level provider resolution (Cerbos or Local)
        var provider = await ResolveInstanceProviderAsync(cancellationToken);

        try
        {
            return await provider.IsAllowedBatchAsync(checks, cancellationToken);
        }
        catch (Exception ex) when (provider == _cerbosProvider)
        {
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

    private static bool UsesHandlerOwnedAiConversationAuthorization(IReadOnlyList<AuthorizationCheck> checks)
    {
        return checks.Count > 0
            && checks.All(check => check.ResourceKind == ResourceKinds.AiConversation);
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
