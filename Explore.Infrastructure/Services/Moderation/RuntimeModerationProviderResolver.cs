// ABOUTME: Runtime moderation provider resolver for event-report external integration modes.
// ABOUTME: Routes LocalOnly, Disabled, and future composite modes without leaking provider details upward.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting.Models;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Services.Moderation.Coop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Moderation;

public sealed class RuntimeModerationProviderResolver :
    IEventReportProvider,
    IModerationSignalProvider,
    IReviewQueueProvider,
    IReportDecisionExecutor
{
    private readonly LocalEventReportProvider _localProvider;
    private readonly CompositeEventReportProvider _compositeProvider;
    private readonly OspreyModerationSignalProvider _ospreySignalProvider;
    private readonly CoopReviewQueueProvider _coopReviewQueueProvider;
    private readonly NoopModerationSignalProvider _noopSignalProvider;
    private readonly NoopReviewQueueProvider _noopReviewQueueProvider;
    private readonly IOptionsMonitor<ModerationProviderOptions> _options;
    private readonly ILogger<RuntimeModerationProviderResolver> _logger;

    public RuntimeModerationProviderResolver(
        LocalEventReportProvider localProvider,
        CompositeEventReportProvider compositeProvider,
        OspreyModerationSignalProvider ospreySignalProvider,
        CoopReviewQueueProvider coopReviewQueueProvider,
        NoopModerationSignalProvider noopSignalProvider,
        NoopReviewQueueProvider noopReviewQueueProvider,
        IOptionsMonitor<ModerationProviderOptions> options,
        ILogger<RuntimeModerationProviderResolver> logger)
    {
        _localProvider = localProvider;
        _compositeProvider = compositeProvider;
        _ospreySignalProvider = ospreySignalProvider;
        _coopReviewQueueProvider = coopReviewQueueProvider;
        _noopSignalProvider = noopSignalProvider;
        _noopReviewQueueProvider = noopReviewQueueProvider;
        _options = options;
        _logger = logger;
    }

    public Task<EventReportProviderSyncResult> SyncReportAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.CurrentValue;
        if (options.IsDisabled || !options.SyncReports)
        {
            return Task.FromResult(EventReportProviderSyncResult.Disabled("Event report provider synchronization is disabled."));
        }

        if (options.IsLocalOnly)
        {
            return _localProvider.SyncReportAsync(envelope, cancellationToken);
        }

        return ExecuteProviderAsync(
            () => _compositeProvider.SyncReportAsync(ApplyEvidenceMode(envelope, options), cancellationToken),
            ex => EventReportProviderSyncResult.Failure("provider_sync_failed", isTransient: true, ex.GetType().Name));
    }

    public Task<EventSafetySignalProviderResult> EvaluateAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.CurrentValue;
        if (!options.ShouldEvaluateSignals)
        {
            return _noopSignalProvider.EvaluateAsync(envelope, cancellationToken);
        }

        return ExecuteProviderAsync(
            () => _ospreySignalProvider.EvaluateAsync(ApplyEvidenceMode(envelope, options), cancellationToken),
            ex => EventSafetySignalProviderResult.Failure("signal_provider_failed", isTransient: true, ex.GetType().Name));
    }

    public Task<ReviewCaseSyncResult> MirrorCaseAsync(
        ReviewCaseEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.CurrentValue;
        if (!options.ShouldMirrorReviewQueue)
        {
            return _noopReviewQueueProvider.MirrorCaseAsync(envelope, cancellationToken);
        }

        return ExecuteProviderAsync(
            () => _coopReviewQueueProvider.MirrorCaseAsync(ApplyEvidenceMode(envelope, options), cancellationToken),
            ex => ReviewCaseSyncResult.Failure("review_queue_provider_failed", isTransient: true, ex.GetType().Name));
    }

    public Task<ReportDecisionExecutionResult> ExecuteAsync(
        ReportDecisionExecutionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.CurrentValue;
        if (!options.ExecuteDecisions)
        {
            return Task.FromResult(ReportDecisionExecutionResult.Failure(
                "decision_execution_disabled",
                isTransient: false,
                "Report decision execution provider is disabled."));
        }

        return _localProvider.ExecuteAsync(envelope, cancellationToken);
    }

    private async Task<TResult> ExecuteProviderAsync<TResult>(
        Func<Task<TResult>> operation,
        Func<Exception, TResult> failureFactory)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Moderation provider operation failed with {FailureType}", ex.GetType().Name);
            return failureFactory(ex);
        }
    }

    private static EventReportProviderEnvelope ApplyEvidenceMode(
        EventReportProviderEnvelope envelope,
        ModerationProviderOptions options) =>
        envelope with { EvidenceMode = options.EvidenceMode };

    private static ReviewCaseEnvelope ApplyEvidenceMode(
        ReviewCaseEnvelope envelope,
        ModerationProviderOptions options) =>
        envelope with { EvidenceMode = options.EvidenceMode };
}
