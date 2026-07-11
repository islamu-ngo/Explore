// ABOUTME: Wraps ISecretResolver with audit-logging side effects. NEVER emits secret values.
// ABOUTME: Samples successful reads (every Nth) to keep log volume bounded under load.

namespace Explore.Secrets.Services;

using System.Threading;
using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

/// <summary>
/// Decorator that adds structured audit logging around each resolution.
/// The inner resolver (<see cref="SecretResolver"/>) already records metrics;
/// this decorator focuses on correlation-friendly log records.
/// </summary>
/// <remarks>
/// <para><b>Security invariant:</b> this class NEVER logs <see cref="ResolvedSecret.Value"/>.
/// Only the setting key, source type, scope, and outcome are emitted. Integration
/// tests assert this property with a regex-based log sink.</para>
/// <para><b>Sampling:</b> to prevent log floods when a hot path resolves the same
/// setting thousands of times per second, successful reads are sampled at
/// <see cref="SuccessSampleRate"/>. Misses and errors are always logged.</para>
/// </remarks>
public sealed class AuditingSecretResolverDecorator : ISecretResolver
{
    /// <summary>
    /// Every Nth successful resolution is audit-logged. Failures/misses always log.
    /// </summary>
    public const int SuccessSampleRate = 100;

    private readonly ISecretResolver _inner;
    private readonly ILogger<AuditingSecretResolverDecorator> _logger;
    private long _successCounter;

    public AuditingSecretResolverDecorator(
        ISecretResolver inner,
        ILogger<AuditingSecretResolverDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResolvedSecret?> ResolveAsync(
        string settingKey,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.ResolveAsync(settingKey, tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            // Misses always logged at Debug (unbound secrets are expected in bare-bones deployments).
            _logger.LogDebug(
                "Secret resolution miss: settingKey={SettingKey} tenantId={TenantId}",
                settingKey, tenantId);
            return null;
        }

        var count = Interlocked.Increment(ref _successCounter);
        if (count % SuccessSampleRate == 1)
        {
            // Sampled success log. Value intentionally absent.
            _logger.LogInformation(
                "Secret resolved (sampled 1/{SampleRate}): settingKey={SettingKey} " +
                "source={Source} scope={Scope} scopeId={ScopeId}",
                SuccessSampleRate, result.SettingKey, result.Source, result.Scope, result.ScopeId);
        }

        return result;
    }

    /// <inheritdoc />
    public Task InvalidateAsync(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Secret cache invalidated: settingKey={SettingKey} scope={Scope} scopeId={ScopeId}",
            settingKey, scope, scopeId);
        return _inner.InvalidateAsync(settingKey, scope, scopeId, cancellationToken);
    }
}
