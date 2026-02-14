// ABOUTME: Authorization provider wrapper that delegates to Cerbos or Local provider based on SystemSetting.
// ABOUTME: Reads "authorization.provider" setting with 1-minute cache; falls back to Local if Cerbos is unreachable.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Authorization provider that delegates to either <see cref="CerbosAuthorizationService"/> or
/// <see cref="FallbackAuthorizationService"/> based on a runtime-switchable SystemSetting.
/// The active provider mode is cached for 1 minute to avoid repeated DB queries.
/// Falls back to the local provider if Cerbos is unreachable.
/// </summary>
public sealed class RuntimeAuthorizationProvider : IAuthorizationProvider
{
    private readonly CerbosAuthorizationService _cerbosProvider;
    private readonly FallbackAuthorizationService _localProvider;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RuntimeAuthorizationProvider> _logger;

    private const string CacheKey = "AuthorizationProvider_Mode";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public RuntimeAuthorizationProvider(
        CerbosAuthorizationService cerbosProvider,
        FallbackAuthorizationService localProvider,
        ISystemSettingRepository systemSettingRepository,
        IMemoryCache cache,
        ILogger<RuntimeAuthorizationProvider> logger)
    {
        _cerbosProvider = cerbosProvider;
        _localProvider = localProvider;
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
        var provider = await ResolveProviderAsync(cancellationToken);

        try
        {
            return await provider.IsAllowedAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);
        }
        catch (Exception ex) when (provider == _cerbosProvider)
        {
            _logger.LogWarning(ex, "Cerbos provider failed; falling back to local provider for {ResourceKind}/{ResourceId}:{Action}",
                resourceKind, resourceId, action);
            return await _localProvider.IsAllowedAsync(resourceKind, resourceId, action, resourceAttributes, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        var provider = await ResolveProviderAsync(cancellationToken);

        try
        {
            return await provider.IsAllowedBatchAsync(checks, cancellationToken);
        }
        catch (Exception ex) when (provider == _cerbosProvider)
        {
            _logger.LogWarning(ex, "Cerbos provider failed for batch ({Count} checks); falling back to local provider", checks.Count);
            return await _localProvider.IsAllowedBatchAsync(checks, cancellationToken);
        }
    }

    public async Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var provider = await ResolveProviderAsync(cancellationToken);

        try
        {
            return await provider.CheckSettingAccessAsync(settingKey, action, tenantId, organizationId, cancellationToken);
        }
        catch (Exception ex) when (provider == _cerbosProvider)
        {
            _logger.LogWarning(ex, "Cerbos provider failed for setting check {SettingKey}:{Action}; falling back to local provider",
                settingKey, action);
            return await _localProvider.CheckSettingAccessAsync(settingKey, action, tenantId, organizationId, cancellationToken);
        }
    }

    private async Task<IAuthorizationProvider> ResolveProviderAsync(CancellationToken cancellationToken)
    {
        var mode = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            try
            {
                var setting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AuthorizationProvider);
                var value = setting?.Value?.Trim().ToLowerInvariant();

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
                _logger.LogWarning(ex, "Failed to read authorization provider setting; defaulting to local");
                return "local";
            }
        });

        return mode == "cerbos" ? _cerbosProvider : _localProvider;
    }
}
