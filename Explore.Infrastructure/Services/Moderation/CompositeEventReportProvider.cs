// ABOUTME: Composite moderation provider that layers optional signals and review mirroring on local reporting.
// ABOUTME: Uses configuration switches so external provider failures never affect report intake persistence.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting.Models;
using Explore.Domain.Enums;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Moderation;

public sealed class CompositeEventReportProvider : IEventReportProvider
{
    private readonly LocalEventReportProvider _localProvider;
    private readonly IModerationSignalProvider _signalProvider;
    private readonly IReviewQueueProvider _reviewQueueProvider;
    private readonly IOptionsMonitor<ModerationProviderOptions> _options;

    public CompositeEventReportProvider(
        LocalEventReportProvider localProvider,
        IModerationSignalProvider signalProvider,
        IReviewQueueProvider reviewQueueProvider,
        IOptionsMonitor<ModerationProviderOptions> options)
    {
        _localProvider = localProvider;
        _signalProvider = signalProvider;
        _reviewQueueProvider = reviewQueueProvider;
        _options = options;
    }

    public async Task<EventReportProviderSyncResult> SyncReportAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var localResult = await _localProvider.SyncReportAsync(envelope, cancellationToken);
        if (!localResult.Succeeded)
        {
            return localResult;
        }

        var options = _options.CurrentValue;
        IReadOnlyList<EventSafetySignalEnvelope> signals = [];
        if (options.ShouldEvaluateSignals)
        {
            var signalResult = await _signalProvider.EvaluateAsync(envelope, cancellationToken);
            if (!signalResult.Succeeded)
            {
                return EventReportProviderSyncResult.Failure(
                    signalResult.Error?.Category ?? "signal_provider_failed",
                    signalResult.Error?.IsTransient ?? true,
                    signalResult.Error?.SafeDetail);
            }

            signals = signalResult.Signals;
        }

        string? providerCaseId = null;
        string? providerUrl = null;
        if (options.ShouldMirrorReviewQueue)
        {
            var reviewResult = await _reviewQueueProvider.MirrorCaseAsync(CreateReviewCaseEnvelope(envelope, options), cancellationToken);
            if (!reviewResult.Succeeded && !reviewResult.ProviderDisabled)
            {
                return EventReportProviderSyncResult.Failure(
                    reviewResult.Error?.Category ?? "review_queue_provider_failed",
                    reviewResult.Error?.IsTransient ?? true,
                    reviewResult.Error?.SafeDetail);
            }

            providerCaseId = reviewResult.ProviderCaseId;
            providerUrl = reviewResult.ProviderUrl;
        }

        return EventReportProviderSyncResult.Success(
            providerCaseId,
            providerSignalId: signals.FirstOrDefault()?.ExternalSignalId,
            providerUrl,
            signals);
    }

    private static ReviewCaseEnvelope CreateReviewCaseEnvelope(
        EventReportProviderEnvelope envelope,
        ModerationProviderOptions options) =>
        new(
            envelope.TenantId,
            envelope.ReportId,
            envelope.EventId,
            envelope.CaseId,
            ResolveReviewProvider(options),
            envelope.QueueCode,
            envelope.CaseStatusCode,
            envelope.PriorityCode,
            envelope.ReasonCode,
            envelope.SubmittedAtUtc,
            null,
            envelope.IdempotencyKey,
            envelope.CorrelationId,
            options.EvidenceMode);

    private static EventReportExternalProvider ResolveReviewProvider(ModerationProviderOptions options) =>
        options.IsMode(ModerationProviderOptions.ModeOsprey)
            ? EventReportExternalProvider.Osprey
            : EventReportExternalProvider.Coop;
}
