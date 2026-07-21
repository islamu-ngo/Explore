// ABOUTME: Runs the one globally leased reconnecting Jetstream consumer for community event and RSVP records.
// ABOUTME: Coalesces DID updates and invokes governed PDS recovery under the active global lease fence.

using System.Diagnostics.Metrics;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using CarpaNet.Jetstream;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Requests.Commands;
using Explore.Domain.Federation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoJetstreamSubscriber : BackgroundService
{
    private static readonly Meter Meter = new("Explore.Atproto.Jetstream", "1.0.0");
    private static readonly Counter<long> EnvelopeCounter = Meter.CreateCounter<long>("atproto.jetstream.envelopes");
    private static readonly Counter<long> RecoveryCounter = Meter.CreateCounter<long>("atproto.pds.recovery");
    private readonly IAtprotoJetstreamRuntimeStore _store;
    private readonly IAtprotoJetstreamEventSource _eventSource;
    private readonly AtprotoJetstreamOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AtprotoJetstreamSubscriber> _logger;
    private readonly string _owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.CreateVersion7():N}";
    private readonly Channel<bool> _filterChanges = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly object _filterLock = new();
    private readonly IDisposable? _optionsChangeRegistration;
    private string[] _desiredAllowedDids;
    private string? _lastCompletedRecoveryFingerprint;

    public AtprotoJetstreamSubscriber(
        IAtprotoJetstreamRuntimeStore store,
        IAtprotoJetstreamEventSource eventSource,
        IOptionsMonitor<AtprotoJetstreamOptions> options,
        TimeProvider timeProvider,
        ILogger<AtprotoJetstreamSubscriber> logger)
    {
        _store = store;
        _eventSource = eventSource;
        _timeProvider = timeProvider;
        _logger = logger;
        AtprotoJetstreamOptions configured = options.CurrentValue;
        _options = new AtprotoJetstreamOptions
        {
            Endpoint = configured.Endpoint,
            MaxMessageSizeBytes = configured.MaxMessageSizeBytes,
            LeaseDurationSeconds = configured.LeaseDurationSeconds,
            LeaseRenewalSeconds = configured.LeaseRenewalSeconds,
            CapabilityPollMilliseconds = configured.CapabilityPollMilliseconds,
            RetryMinimumMilliseconds = configured.RetryMinimumMilliseconds,
            RetryMaximumMilliseconds = configured.RetryMaximumMilliseconds
        };
        _desiredAllowedDids = NormalizeAllowedDids(configured.AllowedDids);
        _optionsChangeRegistration = options.OnChange(HandleOptionsChanged);
    }

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
                await Task.Delay(TimeSpan.FromMilliseconds(delay), _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    "ATProto Jetstream subscription failed with {FailureType}; reconnecting after bounded backoff.",
                    exception.GetType().Name);
                EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "connection_failure"));
                await Task.Delay(TimeSpan.FromMilliseconds(retryMilliseconds), _timeProvider, stoppingToken);
                retryMilliseconds = Math.Min(_options.RetryMaximumMilliseconds, retryMilliseconds * 2);
            }
        }
    }

    internal async Task<bool> RunSingleLeaseAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> enabledTenants = await _store.ResolveEnabledTenantIdsAsync(cancellationToken);
        if (enabledTenants.Count == 0)
        {
            return false;
        }

        var endpoint = new Uri(_options.Endpoint, UriKind.Absolute);
        string service = endpoint.GetLeftPart(UriPartial.Authority);
        DateTime claimedAt = _timeProvider.GetUtcNow().UtcDateTime;
        AtprotoJetstreamClaim? claim = await _store.TryClaimAsync(
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
        string[] initialAllowedDids = ReadDesiredAllowedDids();
        var subscription = new AtprotoJetstreamSubscription(
            endpoint,
            AtprotoJetstreamConstants.Collections,
            initialAllowedDids,
            cursor == 0 ? null : cursor,
            _options.MaxMessageSizeBytes);
        var sentFilter = new SentFilterState(initialAllowedDids);
        Task? filterUpdates = null;
        Task<RecoveryPumpExit>? recovery = null;
        IAtprotoJetstreamSession? session = null;
        IAsyncEnumerator<JetstreamEvent>? events = null;
        Task<bool>? nextEvent = null;
        try
        {
            session = await _eventSource.OpenSessionAsync(
                subscription,
                TimeSpan.FromMilliseconds(_options.CapabilityPollMilliseconds),
                leaseCancellation.Token);
            filterUpdates = ProcessFilterUpdatesAsync(session, sentFilter, leaseCancellation.Token);
            recovery = ProcessRecoveryAsync(claim, leaseCancellation.Token);
            events = session
                .ReadEventsAsync(leaseCancellation.Token)
                .GetAsyncEnumerator(leaseCancellation.Token);
            nextEvent = events.MoveNextAsync().AsTask();
            while (true)
            {
                Task completed = await Task.WhenAny(nextEvent, filterUpdates, recovery);
                if (completed == filterUpdates)
                {
                    await filterUpdates;
                    throw new InvalidOperationException("The Jetstream filter update pump stopped unexpectedly.");
                }

                if (completed == recovery)
                {
                    if (await recovery == RecoveryPumpExit.FenceRejected)
                    {
                        return false;
                    }

                    throw new InvalidOperationException("The ATProto recovery pump stopped unexpectedly.");
                }

                if (!await nextEvent)
                {
                    return appliedAny;
                }

                JetstreamEvent envelope = events.Current;
                if (envelope.TimeUs <= cursor)
                {
                    EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "replay"));
                    nextEvent = events.MoveNextAsync().AsTask();
                    continue;
                }

                enabledTenants = await _store.ResolveEnabledTenantIdsAsync(leaseCancellation.Token);
                if (enabledTenants.Count == 0)
                {
                    return appliedAny;
                }

                DateTime observedAt = _timeProvider.GetUtcNow().UtcDateTime;
                AtprotoJetstreamParsedEnvelope parsed = AtprotoJetstreamEnvelopeParser.Parse(
                    envelope,
                    cursor,
                    sentFilter.Read(),
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
                if (!await _store.TryApplyAndAdvanceAsync(request, leaseCancellation.Token))
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
                nextEvent = events.MoveNextAsync().AsTask();
            }
        }
        finally
        {
            leaseCancellation.Cancel();
            Exception? backgroundFailure = null;
            if (nextEvent is not null)
            {
                try
                {
                    await nextEvent;
                }
                catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    backgroundFailure = exception;
                }
            }

            if (filterUpdates is not null)
            {
                try
                {
                    await filterUpdates;
                }
                catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    backgroundFailure ??= exception;
                }
            }

            if (recovery is not null)
            {
                try
                {
                    await recovery;
                }
                catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    backgroundFailure ??= exception;
                }
            }

            if (events is not null)
            {
                try
                {
                    await events.DisposeAsync();
                }
                catch (Exception exception)
                {
                    backgroundFailure ??= exception;
                }
            }

            if (session is not null)
            {
                try
                {
                    await session.DisposeAsync();
                }
                catch (Exception exception)
                {
                    backgroundFailure ??= exception;
                }
            }

            try
            {
                await renewal;
            }
            catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                backgroundFailure ??= exception;
            }

            if (backgroundFailure is not null)
            {
                ExceptionDispatchInfo.Capture(backgroundFailure).Throw();
            }
        }
    }

    public override void Dispose()
    {
        _optionsChangeRegistration?.Dispose();
        base.Dispose();
    }

    private async Task ProcessFilterUpdatesAsync(
        IAtprotoJetstreamSession session,
        SentFilterState sentFilter,
        CancellationToken cancellationToken)
    {
        while (await _filterChanges.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_filterChanges.Reader.TryRead(out _))
            {
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(_options.CapabilityPollMilliseconds),
                _timeProvider,
                cancellationToken);
            while (_filterChanges.Reader.TryRead(out _))
            {
            }

            string[] desired = ReadDesiredAllowedDids();
            if (desired.SequenceEqual(sentFilter.Read(), StringComparer.Ordinal))
            {
                continue;
            }

            try
            {
                await session.SendOptionsUpdateAsync(
                    CarpaNetJetstreamEventSource.CreateOptionsUpdate(new AtprotoJetstreamSubscription(
                        new Uri(_options.Endpoint, UriKind.Absolute),
                        AtprotoJetstreamConstants.Collections,
                        desired,
                        null,
                        _options.MaxMessageSizeBytes)),
                    cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "filter_update_failure"));
                throw;
            }

            sentFilter.MarkSent(desired);
            EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "filter_update_success"));
        }
    }

    private async Task<RecoveryPumpExit> ProcessRecoveryAsync(
        AtprotoJetstreamClaim claim,
        CancellationToken cancellationToken)
    {
        int initialRetryMilliseconds = Math.Max(
            _options.CapabilityPollMilliseconds,
            _options.RetryMinimumMilliseconds);
        int maximumRetryMilliseconds = Math.Max(
            _options.CapabilityPollMilliseconds,
            _options.RetryMaximumMilliseconds);
        int retryMilliseconds = initialRetryMilliseconds;
        while (true)
        {
            int delayMilliseconds;
            try
            {
                var command = new ReconcileAtprotoPdsSnapshotsCommand(
                    claim,
                    ReadDesiredAllowedDids(),
                    _timeProvider.GetUtcNow().UtcDateTime,
                    Volatile.Read(ref _lastCompletedRecoveryFingerprint));
                AtprotoPdsRecoveryResult result = await _store.ReconcilePdsSnapshotsAsync(
                    command,
                    cancellationToken);
                if (IsCompletedRecoveryOutcome(result.Outcome))
                {
                    Volatile.Write(ref _lastCompletedRecoveryFingerprint, result.Fingerprint);
                }

                RecoveryCounter.Add(
                    1,
                    new KeyValuePair<string, object?>("outcome", RecoveryOutcomeTag(result.Outcome)));
                if (result.Outcome == AtprotoPdsRecoveryOutcome.FenceRejected)
                {
                    return RecoveryPumpExit.FenceRejected;
                }

                if (IsCompletedRecoveryOutcome(result.Outcome))
                {
                    retryMilliseconds = initialRetryMilliseconds;
                    delayMilliseconds = _options.CapabilityPollMilliseconds;
                }
                else
                {
                    delayMilliseconds = retryMilliseconds;
                    retryMilliseconds = DoubleBounded(retryMilliseconds, maximumRetryMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    "ATProto PDS recovery failed with {FailureType}; retrying with bounded backoff under the current consumer lease.",
                    exception.GetType().Name);
                RecoveryCounter.Add(1, new KeyValuePair<string, object?>("outcome", "recovery_failure"));
                delayMilliseconds = retryMilliseconds;
                retryMilliseconds = DoubleBounded(retryMilliseconds, maximumRetryMilliseconds);
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(delayMilliseconds),
                _timeProvider,
                cancellationToken);
        }
    }

    private void HandleOptionsChanged(AtprotoJetstreamOptions options, string? _)
    {
        string[] desired = NormalizeAllowedDids(options.AllowedDids);
        lock (_filterLock)
        {
            if (desired.SequenceEqual(_desiredAllowedDids, StringComparer.Ordinal))
            {
                return;
            }

            _desiredAllowedDids = desired;
        }

        _filterChanges.Writer.TryWrite(true);
    }

    private string[] ReadDesiredAllowedDids()
    {
        lock (_filterLock)
        {
            return _desiredAllowedDids;
        }
    }

    internal static string[] NormalizeAllowedDids(IEnumerable<string> dids) =>
        dids.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

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
                    _timeProvider,
                    leaseCancellation.Token);
                DateTime observedAt = _timeProvider.GetUtcNow().UtcDateTime;
                bool renewed = await _store.TryRenewAsync(
                    claim,
                    observedAt,
                    observedAt.AddSeconds(_options.LeaseDurationSeconds),
                    leaseCancellation.Token);
                if (!renewed)
                {
                    _logger.LogWarning("ATProto Jetstream lease renewal was fenced; the active stream is being cancelled.");
                    EnvelopeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "lease_lost"));
                    leaseCancellation.Cancel();
                    return;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !leaseCancellation.IsCancellationRequested)
        {
            _logger.LogWarning(
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

    private static bool IsCompletedRecoveryOutcome(AtprotoPdsRecoveryOutcome outcome) => outcome is
        AtprotoPdsRecoveryOutcome.Disabled
        or AtprotoPdsRecoveryOutcome.DowntimeOnly
        or AtprotoPdsRecoveryOutcome.Unchanged
        or AtprotoPdsRecoveryOutcome.Completed;

    private static int DoubleBounded(int value, int maximum) =>
        (int)Math.Min(maximum, (long)value * 2);

    private static string RecoveryOutcomeTag(AtprotoPdsRecoveryOutcome outcome) => outcome switch
    {
        AtprotoPdsRecoveryOutcome.Disabled => "recovery_disabled",
        AtprotoPdsRecoveryOutcome.DowntimeOnly => "recovery_cursor_only",
        AtprotoPdsRecoveryOutcome.ScopeRejected => "recovery_scope_rejected",
        AtprotoPdsRecoveryOutcome.Unchanged => "recovery_unchanged",
        AtprotoPdsRecoveryOutcome.Completed => "recovery_completed",
        AtprotoPdsRecoveryOutcome.PartialFailure => "recovery_partial_failure",
        AtprotoPdsRecoveryOutcome.FenceRejected => "recovery_fence_rejected",
        _ => "recovery_unknown"
    };

    private sealed class SentFilterState(string[] allowedDids)
    {
        private string[] _allowedDids = allowedDids;

        public string[] Read() => Volatile.Read(ref _allowedDids);

        public void MarkSent(string[] value) => Volatile.Write(ref _allowedDids, value);
    }

    private enum RecoveryPumpExit
    {
        FenceRejected
    }
}
