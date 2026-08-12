// ABOUTME: Single-source-per-secret resolver. Walks Tenant->Instance hierarchy for binding,
// ABOUTME: then dispatches to EXACTLY ONE ISecretSource based on binding.SourceType. No fallback.

namespace Explore.Secrets.Services;

using System.Collections.Frozen;
using System.Diagnostics;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Secrets.Observability;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolves a <c>settingKey</c> to its current <see cref="ResolvedSecret"/> value.
/// </summary>
/// <remarks>
/// <para><b>Core algorithm (no-fallback):</b></para>
/// <list type="number">
///   <item>Look up the winning <see cref="SecretBinding"/> in the hierarchy:
///         Tenant (if tenantId supplied) -> Instance.</item>
///   <item>Dispatch to the <see cref="ISecretSource"/> matching
///         <see cref="SecretBinding.SourceType"/>.</item>
///   <item>If the source returns <c>null</c>, return <c>null</c>. Do NOT fall back
///         to another source type. This is the architectural invariant that kills
///         the old Infisical->DB->AppSetting precedence chain.</item>
/// </list>
/// <para>Results are cached in-process for <see cref="CacheTtl"/>. Writes to the
/// binding invalidate the cache via <see cref="InvalidateAsync"/>.</para>
/// </remarks>
public sealed class SecretResolver : ISecretResolver
{
    /// <summary>In-memory cache TTL (resolved values live here, not the binding itself).</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ISecretBindingRepository _bindings;
    private readonly FrozenDictionary<SecretSourceType, ISecretSource> _sources;
    private readonly IMemoryCache _cache;
    private readonly SecretResolverMetrics _metrics;
    private readonly ILogger<SecretResolver> _logger;

    public SecretResolver(
        ISecretBindingRepository bindings,
        IEnumerable<ISecretSource> sources,
        IMemoryCache cache,
        SecretResolverMetrics metrics,
        ILogger<SecretResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);

        _bindings = bindings;
        _cache = cache;
        _metrics = metrics;
        _logger = logger;

        // Index sources by SourceType. Duplicate types = bug => throw at startup.
        _sources = sources.ToFrozenDictionary(s => s.SourceType);
    }

    /// <inheritdoc />
    public async Task<ResolvedSecret?> ResolveAsync(
        string settingKey,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingKey);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 1) Find the winning binding in the hierarchy.
            var binding = await ResolveBindingAsync(settingKey, tenantId, cancellationToken)
                .ConfigureAwait(false);

            if (binding is null)
            {
                _metrics.RecordMiss(settingKey, source: null);
                return null;
            }

            return await ResolveBoundBindingAsync(binding, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordDuration(settingKey, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public async Task<ResolvedSecret?> ResolveTenantBindingAsync(
        Guid tenantId,
        Guid bindingId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || bindingId == Guid.Empty)
        {
            throw new ArgumentException("Tenant and binding identifiers are required.");
        }

        SecretBinding? binding = await _bindings.GetByTenantAndIdAsync(tenantId, bindingId, cancellationToken).ConfigureAwait(false);
        if (binding is null)
        {
            _metrics.RecordMiss($"binding:{bindingId:N}", source: null);
            return null;
        }

        return await ResolveBoundBindingAsync(binding, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task InvalidateAsync(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingKey);
        _cache.Remove(BuildCacheKey(settingKey, scope, scopeId, qualifier: string.Empty));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Walks Tenant -> Instance to find the winning binding. A null return means
    /// the secret is unbound (not configured) - caller treats as "feature disabled".
    /// </summary>
    private async Task<SecretBinding?> ResolveBindingAsync(
        string settingKey,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            var tenantBinding = await _bindings.GetByKeyAndScopeAsync(
                settingKey, SecretScope.Tenant, tenantId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (tenantBinding is not null)
            {
                return tenantBinding;
            }
        }

        return await _bindings.GetByKeyAndScopeAsync(
            settingKey, SecretScope.Instance, scopeId: null, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ResolvedSecret?> ResolveBoundBindingAsync(SecretBinding binding, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(binding.SettingKey, binding.Scope, binding.ScopeId, binding.Qualifier);
        if (_cache.TryGetValue<ResolvedSecret>(cacheKey, out var cached) && cached is not null)
        {
            _metrics.RecordCacheHit(binding.SettingKey);
            return cached;
        }

        _metrics.RecordCacheMiss(binding.SettingKey);
        if (!_sources.TryGetValue(binding.SourceType, out var source))
        {
            _logger.LogError(
                "No ISecretSource registered for source type {SourceType} (settingKey={SettingKey}). This indicates a missing DI registration.",
                binding.SourceType, binding.SettingKey);
            _metrics.RecordError(binding.SettingKey, binding.SourceType);
            return null;
        }

        string? value;
        try
        {
            value = await source.GetSecretAsync(binding, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // Do not catch general exception types - source boundary
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(
                ex,
                "Secret source {SourceType} threw resolving {SettingKey} at scope {Scope}/{ScopeId}. Returning null (no fallback).",
                binding.SourceType, binding.SettingKey, binding.Scope, binding.ScopeId);
            _metrics.RecordError(binding.SettingKey, binding.SourceType);
            return null;
        }

        if (value is null)
        {
            _metrics.RecordMiss(binding.SettingKey, binding.SourceType);
            return null;
        }

        var resolved = new ResolvedSecret(
            SettingKey: binding.SettingKey,
            Value: value,
            Source: binding.SourceType,
            Scope: binding.Scope,
            ScopeId: binding.ScopeId,
            ResolvedAt: DateTime.UtcNow);

        _cache.Set(cacheKey, resolved, CacheTtl);
        _metrics.RecordSuccess(binding.SettingKey, binding.SourceType);
        return resolved;
    }

    internal static string BuildCacheKey(string settingKey, SecretScope scope, Guid? scopeId, string qualifier = "") =>
        scopeId.HasValue
            ? $"secret::{settingKey}::{scope}::{scopeId.Value:N}::{qualifier}"
            : $"secret::{settingKey}::{scope}::-::{qualifier}";
}
