// ABOUTME: Reports bounded privacy-erasure replay and provider-work readiness diagnostics.
// ABOUTME: Excludes identifiers, endpoints, payloads, connection details, and exception text.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Explore.API.HealthChecks;

public sealed class PrivacyErasureReadinessHealthCheck(
    IOptions<PrivacyErasureDurabilityOptions> durabilityOptions,
    IPrivacyErasureReplayCheckpointRepository checkpointRepository,
    IPrivacyErasureProviderWorkRepository providerWorkRepository,
    IOutboxRepository outboxRepository,
    IPrivacyErasureAuthority authority,
    TimeProvider timeProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            long localSequence = (await checkpointRepository.GetLatestAsync(cancellationToken))
                ?.AuthoritySequence ?? 0;
            bool replayCaughtUp = (await authority.ReadAfterAsync(localSequence, 1, cancellationToken)).Count == 0;
            DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            int due = await providerWorkRepository.CountDueAsync(nowUtc, cancellationToken);
            int unknown = await providerWorkRepository.CountUnknownAsync(cancellationToken);
            int deadLettered = await providerWorkRepository.CountDeadLetteredAsync(cancellationToken);
            int cacheConvergenceIncomplete = await outboxRepository.CountIncompleteByEventTypeAsync(
                PrivacyErasureCacheInvalidationOutboxMessageFactory.EventType,
                cancellationToken);
            int cacheConvergenceDeadLettered = await outboxRepository.CountDeadLetteredByEventTypeAsync(
                PrivacyErasureCacheInvalidationOutboxMessageFactory.EventType,
                cancellationToken);
            var data = new Dictionary<string, object>
            {
                ["topology"] = durabilityOptions.Value.Topology.ToString(),
                ["restoreReplayProtection"] = durabilityOptions.Value.RestoreReplayProtection,
                ["replayCaughtUp"] = replayCaughtUp,
                ["providerDue"] = due,
                ["providerUnknown"] = unknown,
                ["providerDeadLettered"] = deadLettered,
                ["cacheConvergenceIncomplete"] = cacheConvergenceIncomplete,
                ["cacheConvergenceDeadLettered"] = cacheConvergenceDeadLettered
            };

            return replayCaughtUp
                && unknown == 0
                && deadLettered == 0
                && cacheConvergenceIncomplete == 0
                && cacheConvergenceDeadLettered == 0
                ? HealthCheckResult.Healthy("privacy_erasure_ready", data)
                : HealthCheckResult.Degraded("privacy_erasure_attention_required", data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("privacy_erasure_authority_unavailable");
        }
    }
}
