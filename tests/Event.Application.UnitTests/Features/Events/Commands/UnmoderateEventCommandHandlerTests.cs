// ABOUTME: Unit tests for reversible unmoderation command handling.
// ABOUTME: Verifies light-only restoration, irreversible rejection, audit writes, and cache invalidation.

using System.Diagnostics.Metrics;
using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

[NotInParallel("BusinessMetricsMeter")]
public sealed class UnmoderateEventCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 20, 30, 0, TimeSpan.Zero);
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly IUnitOfWork _unitOfWork = new ImmediateUnitOfWork();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UnmoderateEventCommandHandler _handler;

    public UnmoderateEventCommandHandlerTests()
    {
        _moderationRecordRepository.Create(Arg.Any<EventModerationRecord>())
            .Returns(call => call.Arg<EventModerationRecord>());

        _handler = new UnmoderateEventCommandHandler(
            _eventRepository,
            _moderationRecordRepository,
            _unitOfWork,
            _currentUserService,
            _cache,
            CreateMetrics(),
            NullLogger<UnmoderateEventCommandHandler>.Instance,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));
    }

    [Test]
    public async Task Handle_WhenLatestModerationIsReversibleLight_RestoresPublishedAndWritesAudit()
    {
        var moderatorUserId = Guid.NewGuid();
        var @event = CreateEvent(EventStatusEnum.Moderated);
        const string reasonCode = "appeal_approved";
        const string correlationId = "case-restore-123";
        var sourceRecord = EventModerationRecord.CreateLightModeration(
            Guid.CreateVersion7(),
            @event.TenantId,
            @event.Id,
            Guid.NewGuid(),
            "policy_review",
            (int)EventStatusEnum.Published,
            "source-correlation",
            DateTimeOffset.UtcNow.AddMinutes(-10));
        var createdRecords = new List<EventModerationRecord>();

        _currentUserService.UserId.Returns(moderatorUserId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _moderationRecordRepository.GetLatestByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns(sourceRecord);
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(record => createdRecords.Add(record)))
            .Returns(call => call.Arg<EventModerationRecord>());

        var result = await _handler.Handle(new UnmoderateEventCommand
        {
            Id = @event.Id,
            ReasonCode = reasonCode,
            CorrelationId = correlationId
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(createdRecords).Count().IsEqualTo(1);

        var record = createdRecords.Single();
        await Assert.That(record.TenantId).IsEqualTo(@event.TenantId);
        await Assert.That(record.EventId).IsEqualTo(@event.Id);
        await Assert.That(record.ModeratorUserId).IsEqualTo(moderatorUserId);
        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.Unmoderated);
        await Assert.That(record.PreviousStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(record.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(record.IsIrreversible).IsFalse();
        await Assert.That(record.SourceModerationRecordId).IsEqualTo(sourceRecord.Id);
        await Assert.That(record.ReasonCode).IsEqualTo(reasonCode);
        await Assert.That(record.CorrelationId).IsEqualTo(correlationId);
        await Assert.That(record.CreatedAt).IsEqualTo(Now);

        await _eventRepository.Received(1).Update(@event);
        await _cache.Received(1).RemoveAsync($"event:detail:{@event.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenLatestModerationIsIrreversibleHeavy_ReturnsNotReversibleFailure()
    {
        var @event = CreateEvent(EventStatusEnum.Moderated);
        var sourceRecord = EventModerationRecord.CreateHeavyRedaction(
            Guid.CreateVersion7(),
            @event.TenantId,
            @event.Id,
            Guid.NewGuid(),
            "illegal_content",
            (int)EventStatusEnum.Published,
            null,
            DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);
        _moderationRecordRepository.GetLatestByEventAsync(@event.TenantId, @event.Id, Arg.Any<CancellationToken>())
            .Returns(sourceRecord);

        var result = await _handler.Handle(new UnmoderateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_unmoderation_not_reversible");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotModerated_ReturnsInvalidStatusFailure()
    {
        var @event = CreateEvent(EventStatusEnum.Draft);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new UnmoderateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_unmoderation_invalid_status");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenRetryFindsEventAlreadyPublished_ReturnsSuccessWithoutDurableWork()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var @event = CreateEvent(EventStatusEnum.Published);
        var unitOfWork = new RetryingUnitOfWork();
        bool loggerObservedCommit = false;
        var logger = new TestLogger<UnmoderateEventCommandHandler>(() => loggerObservedCommit = unitOfWork.Completed);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);
        var handler = new UnmoderateEventCommandHandler(
            _eventRepository,
            _moderationRecordRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new UnmoderateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event is already published.");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("unmoderated")).IsEqualTo(0);
        await Assert.That(loggerObservedCommit).IsTrue();
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(logger.Entries[0].Message).Contains("already published");
    }

    [Test]
    public async Task Handle_WhenTransactionDelegateRetries_AppliesCacheAndMetricOnceAfterCommit()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Moderated);
        var retryEvent = CreateEvent(EventStatusEnum.Moderated);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        var sourceRecord = EventModerationRecord.CreateLightModeration(
            Guid.CreateVersion7(),
            firstAttemptEvent.TenantId,
            firstAttemptEvent.Id,
            Guid.NewGuid(),
            "policy_review",
            (int)EventStatusEnum.Published,
            null,
            Now.AddMinutes(-10));
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        _moderationRecordRepository.GetLatestByEventAsync(
                firstAttemptEvent.TenantId,
                firstAttemptEvent.Id,
                Arg.Any<CancellationToken>())
            .Returns(sourceRecord);
        var createdRecords = new List<EventModerationRecord>();
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(createdRecords.Add))
            .Returns(call => call.Arg<EventModerationRecord>());
        var unitOfWork = new RetryingUnitOfWork();
        var logger = new TestLogger<UnmoderateEventCommandHandler>();
        bool cacheObservedCommit = false;
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = unitOfWork.Completed;
                return ValueTask.CompletedTask;
            });
        var handler = new UnmoderateEventCommandHandler(
            _eventRepository,
            _moderationRecordRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new UnmoderateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _moderationRecordRepository.Received(2).Create(Arg.Any<EventModerationRecord>());
        await Assert.That(createdRecords.Select(record => record.Id).Distinct()).Count().IsEqualTo(1);
        await _eventRepository.Received(2).Update(Arg.Any<Explore.Domain.Event>());
        await _cache.Received(1).RemoveAsync($"event:detail:{firstAttemptEvent.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(firstAttemptEvent.TenantId), Arg.Any<CancellationToken>());
        await Assert.That(cacheObservedCommit).IsTrue();
        await Assert.That(metricsCapture.Count("unmoderated")).IsEqualTo(1);
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(logger.Entries[0].Message).Contains("Event unmoderation succeeded");
    }

    [Test]
    public async Task Handle_WhenCommitAmbiguityRetryFindsEventPublished_PreservesPostCommitEffects()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Moderated);
        var retryEvent = CreateEvent(EventStatusEnum.Published);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        var sourceRecord = EventModerationRecord.CreateLightModeration(
            Guid.CreateVersion7(),
            firstAttemptEvent.TenantId,
            firstAttemptEvent.Id,
            Guid.NewGuid(),
            "policy_review",
            (int)EventStatusEnum.Published,
            null,
            Now.AddMinutes(-10));
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        _moderationRecordRepository.GetLatestByEventAsync(
                firstAttemptEvent.TenantId,
                firstAttemptEvent.Id,
                Arg.Any<CancellationToken>())
            .Returns(sourceRecord);
        var unitOfWork = new RetryingUnitOfWork();
        var logger = new TestLogger<UnmoderateEventCommandHandler>();
        var handler = new UnmoderateEventCommandHandler(
            _eventRepository,
            _moderationRecordRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new UnmoderateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _moderationRecordRepository.Received(1).Create(Arg.Any<EventModerationRecord>());
        await _eventRepository.Received(1).Update(firstAttemptEvent);
        await _cache.Received(1).RemoveAsync($"event:detail:{firstAttemptEvent.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(firstAttemptEvent.TenantId), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("unmoderated")).IsEqualTo(1);
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Message).Contains("Event unmoderation succeeded");
    }

    [Test]
    public async Task Handle_WhenRolledBackAttemptIsFollowedByInvalidStatus_ReturnsFailureWithoutPostCommitEffects()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Moderated);
        var retryEvent = CreateEvent(EventStatusEnum.Draft);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        var sourceRecord = EventModerationRecord.CreateLightModeration(
            Guid.CreateVersion7(),
            firstAttemptEvent.TenantId,
            firstAttemptEvent.Id,
            Guid.NewGuid(),
            "policy_review",
            (int)EventStatusEnum.Published,
            null,
            Now.AddMinutes(-10));
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        _moderationRecordRepository.GetLatestByEventAsync(
                firstAttemptEvent.TenantId,
                firstAttemptEvent.Id,
                Arg.Any<CancellationToken>())
            .Returns(sourceRecord);
        var logger = new TestLogger<UnmoderateEventCommandHandler>();
        var handler = new UnmoderateEventCommandHandler(
            _eventRepository,
            _moderationRecordRepository,
            new RetryingUnitOfWork(),
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new UnmoderateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_unmoderation_invalid_status");
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("unmoderated")).IsEqualTo(0);
        await Assert.That(logger.Entries.Count(entry => entry.Message.Contains("succeeded", StringComparison.Ordinal))).IsEqualTo(0);
    }

    private static Explore.Domain.Event CreateEvent(EventStatusEnum status) => new(status)
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        ActorId = Guid.CreateVersion7(),
        Actor = null!,
        Title = "Community Iftar",
        EventStatus = null!,
        VisibilityTypeId = 1,
        VisibilityType = null!,
        EventFormatId = 1,
        EventFormat = null!,
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            operation(ct);
    }

    private sealed class RetryingUnitOfWork : IUnitOfWork
    {
        public bool Completed { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
        {
            await operation(ct);
            await operation(ct);
            Completed = true;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            await operation(ct);
            T result = await operation(ct);
            Completed = true;
            return result;
        }

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class MetricsCapture : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<KeyValuePair<string, object?>[]> _measurements = [];

        public MetricsCapture()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BusinessMetrics.MeterName
                    && instrument.Name == "explore.events.moderation_actions")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, _, tags, _) => _measurements.Add(tags.ToArray()));
            _listener.Start();
        }

        public int Count(string actionKind) => _measurements.Count(tags =>
            tags.Any(tag => tag.Key == "action_kind" && Equals(tag.Value, actionKind)));

        public void Dispose() => _listener.Dispose();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly Action? _onLog;

        public TestLogger(Action? onLog = null) => _onLog = onLog;

        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _onLog?.Invoke();
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
