// ABOUTME: Runs the one globally leased reconnecting Jetstream consumer for community event and RSVP records.
// ABOUTME: Rechecks tenant capability before each atomic record, tombstone, presentation, or quarantine commit.

using System.Diagnostics.Metrics;
using CarpaNet.Jetstream;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Federation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoJetstreamSubscriber(
    IAtprotoJetstreamRuntimeStore store,
    IAtprotoJetstreamEventSource eventSource,
    IOptions<AtprotoJetstreamOptions> options,
    TimeProvider timeProvider,
    ILogger<AtprotoJetstreamSubscriber> logger) : BackgroundService
{
    private static readonly Meter Meter = new("Explore.Atproto.Jetstream", "1.0.0");
    private static readonly Counter<long> EnvelopeCounter = Meter.CreateCounter<long>("atproto.jetstream.envelopes");
    private readonly AtprotoJetstreamOptions _options = options.Value;
    private readonly string _owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.CreateVersion7():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int retryMilliseconds = _options.RetryMinimumMilliseconds;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool consumed = await RunSingleLeaseAsync(stoppingToken);
                retryMilliseconds = consumed
                    ? _options.RetryMinimumMilliseconds
                    : Math.Min(_options.RetryMaximumMilliseconds, retryMilliseconds * 2);
                int delay = consumed ? _options.RetryMinimumMilliseconds : Math.Max(_options.CapabilityPollMilliseconds, retryMilliseconds);
                await Task.Delay(TimeSpan.FromMilliseconds(delay), timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "ATProto Jetstream subscription failed with {FailureType}; reconnecting after bounded backoff.",
                    exception.GetType().Name);
                EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "connection_failure"));
                await Task.Delay(TimeSpan.FromMilliseconds(retryMilliseconds), timeProvider, stoppingToken);
                retryMilliseconds = Math.Min(_options.RetryMaximumMilliseconds, retryMilliseconds * 2);
            }
        }
    }

    internal async Task<bool> RunSingleLeaseAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> enabledTenants = await store.ResolveEnabledTenantIdsAsync(cancellationToken);
        if (enabledTenants.Count == 0)
        {
            return false;
        }

        if (_options.AllowedDids is not { Length: > 0 })
        {
            logger.LogError("ATProto Jetstream is enabled but its curated DID allowlist is empty; no stream will be opened.");
            EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "configuration_rejected"));
            return false;
        }

        var endpoint = new Uri(_options.Endpoint, UriKind.Absolute);
        string service = endpoint.GetLeftPart(UriPartial.Authority);
        DateTime claimedAt = timeProvider.GetUtcNow().UtcDateTime;
        AtprotoJetstreamClaim? claim = await store.TryClaimAsync(
            service,
            _owner,
            claimedAt,
            TimeSpan.FromSeconds(_options.LeaseDurationSeconds),
            cancellationToken);
        if (claim is null)
        {
            return false;
        }

        using var leaseCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task renewal = RenewLeaseAsync(claim, leaseCancellation);
        bool appliedAny = false;
        long cursor = claim.Cursor;
        try
        {
            var subscription = new AtprotoJetstreamSubscription(
                endpoint,
                AtprotoJetstreamConstants.Collections,
                _options.AllowedDids,
                cursor == 0 ? null : cursor,
                _options.MaxMessageSizeBytes);
            await foreach (JetstreamEvent envelope in eventSource.SubscribeAsync(subscription, leaseCancellation.Token))
            {
                if (envelope.TimeUs <= cursor)
                {
                    EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "replay"));
                    continue;
                }

                enabledTenants = await store.ResolveEnabledTenantIdsAsync(leaseCancellation.Token);
                if (enabledTenants.Count == 0)
                {
                    return appliedAny;
                }

                DateTime observedAt = timeProvider.GetUtcNow().UtcDateTime;
                AtprotoJetstreamParsedEnvelope parsed = AtprotoJetstreamEnvelopeParser.Parse(
                    envelope,
                    cursor,
                    _options.AllowedDids,
                    observedAt);
                IReadOnlyList<AtprotoRecordTenantPresentation> presentations =
                    parsed.Record is { TombstonedAt: null }
                        ? enabledTenants.Select(tenantId => new AtprotoRecordTenantPresentation
                        {
                            TenantId = tenantId,
                            IsVisible = true
                        }).ToArray()
                        : [];
                var request = new AtprotoJetstreamApplyRequest(
                    claim,
                    cursor,
                    parsed.Cursor,
                    parsed.Record,
                    presentations,
                    parsed.Quarantine,
                    observedAt,
                    parsed.AdvanceCursor,
                    parsed.EventProjection,
                    parsed.EventProjectionInvalidation);
                if (!await store.TryApplyAndAdvanceAsync(request, leaseCancellation.Token))
                {
                    EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "fence_rejected"));
                    return false;
                }

                if (parsed.AdvanceCursor)
                {
                    cursor = parsed.Cursor;
                }
                appliedAny = true;
                EnvelopeCounter.Add(
                    1,
                    new KeyValuePair<string, object?>("outcome", parsed.Quarantine is null ? "materialized" : "quarantined"),
                    new KeyValuePair<string, object?>("collection", CollectionTag(envelope.Commit?.Collection)));
            }

            return appliedAny;
        }
        finally
        {
            leaseCancellation.Cancel();
            try
            {
                await renewal;
            }
            catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested)
            {
            }
        }
    }

    private async Task RenewLeaseAsync(
        AtprotoJetstreamClaim claim,
        CancellationTokenSource leaseCancellation)
    {
        try
        {
            while (!leaseCancellation.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.LeaseRenewalSeconds),
                    timeProvider,
                    leaseCancellation.Token);
                DateTime observedAt = timeProvider.GetUtcNow().UtcDateTime;
                bool renewed = await store.TryRenewAsync(
                    claim,
                    observedAt,
                    observedAt.AddSeconds(_options.LeaseDurationSeconds),
                    leaseCancellation.Token);
                if (!renewed)
                {
                    logger.LogWarning("ATProto Jetstream lease renewal was fenced; the active stream is being cancelled.");
                    EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "lease_lost"));
                    leaseCancellation.Cancel();
                    return;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !leaseCancellation.IsCancellationRequested)
        {
            logger.LogWarning(
                "ATProto Jetstream lease renewal failed with {FailureType}; the active stream is being cancelled.",
                exception.GetType().Name);
            EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "lease_renewal_failure"));
            leaseCancellation.Cancel();
            throw;
        }
    }

    private static string CollectionTag(string? collection) => collection switch
    {
        AtprotoJetstreamConstants.EventCollection => "event",
        AtprotoJetstreamConstants.RsvpCollection => "rsvp",
        _ => "unsupported"
    };
}
