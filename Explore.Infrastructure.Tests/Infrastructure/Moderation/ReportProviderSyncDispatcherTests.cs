// ABOUTME: Unit tests for idempotent event-report provider sync outbox dispatch.
// ABOUTME: Verifies provider outcomes are persisted as local report links and signals.

using System.Diagnostics.Metrics;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Models;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Infrastructure.Configuration;
using Explore.Infrastructure.Services.Moderation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Moderation;

public sealed class ReportProviderSyncDispatcherTests
{
    [Test]
    public async Task DispatchAsync_WhenProviderSucceeds_PersistsSignalAndExternalLink()
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var (report, reportCase) = CreateReport(createdAt);
        var message = CreateMessage(report, reportCase, createdAt, "request-correlation-1");
        var provider = new RecordingEventReportProvider
        {
            Result = EventReportProviderSyncResult.Success(
                providerSignalId: "signal-1",
                signals:
                [
                    new EventSafetySignalEnvelope(
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        EventReportSignalProvider.Osprey,
                        "policy_match",
                        "event.spam",
                        0.91m,
                        EventReportSignalVerdict.LikelyViolation,
                        EventReportRecommendedAction.LightModerate,
                        "Likely spam promotion.",
                        "signal-1",
                        "provider-correlation-1",
                        createdAt.AddMinutes(1))
                ])
        };
        var repository = CreateRepository(report);
        var dispatcher = CreateDispatcher(provider, repository, new ModerationProviderOptions
        {
            Mode = ModerationProviderOptions.ModeOsprey,
            EvaluateSignals = true
        });

        await dispatcher.DispatchAsync(message);

        var idempotencyKey = BuildIdempotencyKey(message);
        await Assert.That(provider.Calls).IsEqualTo(1);
        await Assert.That(provider.LastEnvelope!.TenantId).IsEqualTo(report.TenantId);
        await Assert.That(provider.LastEnvelope.ReportId).IsEqualTo(report.Id);
        await Assert.That(provider.LastEnvelope.CaseId).IsEqualTo(reportCase.Id);
        await Assert.That(provider.LastEnvelope.IdempotencyKey).IsEqualTo(idempotencyKey);
        await Assert.That(provider.LastEnvelope.CorrelationId).IsEqualTo("request-correlation-1");
        await Assert.That(report.Signals).Count().IsEqualTo(1);
        await Assert.That(report.Signals.Single().TenantId).IsEqualTo(report.TenantId);
        await Assert.That(report.Signals.Single().ReportId).IsEqualTo(report.Id);
        await Assert.That(report.Signals.Single().EventId).IsEqualTo(report.EventId);
        await Assert.That(report.ExternalLinks).Count().IsEqualTo(1);
        await Assert.That(report.ExternalLinks.Single().Provider).IsEqualTo(EventReportExternalProvider.Osprey);
        await Assert.That(report.ExternalLinks.Single().ProviderSignalId).IsEqualTo("signal-1");
        await Assert.That(report.ExternalLinks.Single().SyncState).IsEqualTo(EventReportSyncState.Synced);
        await Assert.That(report.ExternalLinks.Single().CorrelationId).IsEqualTo(idempotencyKey);
        await repository.Received(1).Update(report);
    }

    [Test]
    public async Task DispatchAsync_WhenProviderFailsTransiently_PersistsFailureAndThrowsForOutboxRetry()
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var (report, reportCase) = CreateReport(createdAt);
        var message = CreateMessage(report, reportCase, createdAt, "request-correlation-2");
        var provider = new RecordingEventReportProvider
        {
            Result = EventReportProviderSyncResult.Failure("coop_timeout", isTransient: true)
        };
        var repository = CreateRepository(report);
        var dispatcher = CreateDispatcher(provider, repository, new ModerationProviderOptions
        {
            Mode = ModerationProviderOptions.ModeCoop,
            MirrorReviewQueue = true
        });

        await Assert.That(async () => await dispatcher.DispatchAsync(message)).Throws<InvalidOperationException>();

        await Assert.That(provider.Calls).IsEqualTo(1);
        await Assert.That(report.ExternalLinks).Count().IsEqualTo(1);
        await Assert.That(report.ExternalLinks.Single().Provider).IsEqualTo(EventReportExternalProvider.Coop);
        await Assert.That(report.ExternalLinks.Single().SyncState).IsEqualTo(EventReportSyncState.Failed);
        await Assert.That(report.ExternalLinks.Single().LastErrorCategory).IsEqualTo("coop_timeout");
        await Assert.That(report.ExternalLinks.Single().RetryCount).IsEqualTo(1);
        await repository.Received(1).Update(report);
    }

    [Test]
    public async Task DispatchAsync_WhenProviderDisabled_PersistsDisabledMarkerAndCompletes()
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var (report, reportCase) = CreateReport(createdAt);
        var message = CreateMessage(report, reportCase, createdAt, "request-correlation-3");
        var provider = new RecordingEventReportProvider
        {
            Result = EventReportProviderSyncResult.Disabled()
        };
        var repository = CreateRepository(report);
        var dispatcher = CreateDispatcher(provider, repository, new ModerationProviderOptions
        {
            Mode = ModerationProviderOptions.ModeCoop,
            MirrorReviewQueue = true
        });

        await dispatcher.DispatchAsync(message);

        await Assert.That(provider.Calls).IsEqualTo(1);
        await Assert.That(report.ExternalLinks).Count().IsEqualTo(1);
        await Assert.That(report.ExternalLinks.Single().Provider).IsEqualTo(EventReportExternalProvider.Coop);
        await Assert.That(report.ExternalLinks.Single().SyncState).IsEqualTo(EventReportSyncState.Disabled);
        await repository.Received(1).Update(report);
    }

    [Test]
    public async Task DispatchAsync_WhenCompletedMarkerExists_SkipsProviderAndPersistence()
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var (report, reportCase) = CreateReport(createdAt);
        var message = CreateMessage(report, reportCase, createdAt, "request-correlation-4");
        var idempotencyKey = BuildIdempotencyKey(message);
        var existingLink = EventReportExternalLink.CreatePending(
            report.TenantId,
            report.Id,
            reportCase.Id,
            EventReportExternalProvider.Coop,
            idempotencyKey,
            createdAt);
        existingLink.MarkSynced("case-1", null, "https://coop.example/cases/case-1", createdAt.AddMinutes(1));
        report.ExternalLinks.Add(existingLink);

        var provider = new RecordingEventReportProvider();
        var repository = CreateRepository(report);
        var dispatcher = CreateDispatcher(provider, repository, new ModerationProviderOptions
        {
            Mode = ModerationProviderOptions.ModeCoop,
            MirrorReviewQueue = true
        });

        await dispatcher.DispatchAsync(message);

        await Assert.That(provider.Calls).IsEqualTo(0);
        await repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    private static ReportProviderSyncDispatcher CreateDispatcher(
        IEventReportProvider provider,
        IEventReportRepository repository,
        ModerationProviderOptions options) =>
        new(
            provider,
            repository,
            new StaticOptionsMonitor<ModerationProviderOptions>(options),
            CreateMetrics(),
            NullLogger<ReportProviderSyncDispatcher>.Instance);

    private static IEventReportRepository CreateRepository(EventReport report)
    {
        var repository = Substitute.For<IEventReportRepository>();
        repository.GetByIdForUpdateAsync(report.TenantId, report.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventReport?>(report));
        repository.Update(Arg.Any<EventReport>()).Returns(Task.CompletedTask);
        return repository;
    }

    private static (EventReport Report, EventReportCase ReportCase) CreateReport(DateTime createdAt)
    {
        var report = EventReport.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            EventReporterKind.AuthenticatedUser,
            EventReportSourceKind.UserReport,
            "spam",
            null,
            EventReportPriority.Normal,
            null,
            reporterContactConsent: true,
            "en",
            null,
            null,
            createdAt);
        var reportCase = EventReportCase.Create(
            report.TenantId,
            report.Id,
            "safety",
            EventReportPriority.Normal,
            createdAt.AddHours(48),
            createdAt);

        report.Cases.Add(reportCase);
        return (report, reportCase);
    }

    private static OutboxMessage CreateMessage(
        EventReport report,
        EventReportCase reportCase,
        DateTime createdAt,
        string correlationId)
    {
        var payload = new EventReportProviderSyncRequested
        {
            TenantId = report.TenantId,
            ReportId = report.Id,
            EventId = report.EventId,
            CaseId = reportCase.Id,
            ReasonCode = report.ReasonCode,
            QueueCode = reportCase.QueueCode,
            SubmittedAtUtc = createdAt,
            CorrelationId = correlationId
        };

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = "EventReport",
            AggregateId = report.Id,
            EventType = EventReportOutboxMessageFactory.EventReportProviderSyncRequestedEventType,
            Payload = JsonSerializer.Serialize(payload),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = createdAt,
            MaxRetries = 5
        };
    }

    private static string BuildIdempotencyKey(OutboxMessage message) =>
        $"event-report-provider-sync:{message.Id:N}";

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed class RecordingEventReportProvider : IEventReportProvider
    {
        public int Calls { get; private set; }
        public EventReportProviderEnvelope? LastEnvelope { get; private set; }
        public EventReportProviderSyncResult Result { get; init; } = EventReportProviderSyncResult.Success();

        public Task<EventReportProviderSyncResult> SyncReportAsync(
            EventReportProviderEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastEnvelope = envelope;
            return Task.FromResult(Result);
        }
    }

    private sealed class StaticOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue { get; } = currentValue;

        public TOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
