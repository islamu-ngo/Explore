// ABOUTME: Unit tests for reversible light moderation command handling.
// ABOUTME: Verifies moderation history, internal outbox fanout, status transition, and cache invalidation.

using System.Diagnostics.Metrics;
using System.Text.Json;
using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

[NotInParallel("BusinessMetricsMeter")]
public sealed class ModerateEventCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 20, 0, 0, TimeSpan.Zero);
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = new ImmediateUnitOfWork();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly ModerateEventCommandHandler _handler;

    public ModerateEventCommandHandlerTests()
    {
        _moderationRecordRepository.Create(Arg.Any<EventModerationRecord>())
            .Returns(call => call.Arg<EventModerationRecord>());
        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(call => call.Arg<OutboxMessage>());
        _eventSessionRepository.GetSessionsByEvent(Arg.Any<Guid>())
            .Returns(new List<EventSession>());

        _handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            _unitOfWork,
            _currentUserService,
            _cache,
            CreateMetrics(),
            NullLogger<ModerateEventCommandHandler>.Instance,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));
    }

    [Test]
    public async Task Handle_WhenPublishedEventIsModerated_WritesAuditOutboxAndInvalidatesCaches()
    {
        var moderatorUserId = Guid.NewGuid();
        var @event = CreateEvent(EventStatusEnum.Published);
        var sourceReportId = Guid.NewGuid();
        var sourceReportDecisionId = Guid.NewGuid();
        const string reasonCode = "community_safety_review";
        const string correlationId = "case-123";
        var createdRecords = new List<EventModerationRecord>();
        var createdMessages = new List<OutboxMessage>();
        var firstSession = CreateSession(@event, EventSessionStatusEnum.Published);
        var secondSession = CreateSession(@event, EventSessionStatusEnum.Draft);

        _currentUserService.UserId.Returns(moderatorUserId);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _eventSessionRepository.GetSessionsByEvent(@event.Id).Returns([firstSession, secondSession]);
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(record => createdRecords.Add(record)))
            .Returns(call => call.Arg<EventModerationRecord>());
        _outboxRepository.Create(Arg.Do<OutboxMessage>(message => createdMessages.Add(message)))
            .Returns(call => call.Arg<OutboxMessage>());

        var result = await _handler.Handle(new ModerateEventCommand
        {
            Id = @event.Id,
            ReasonCode = reasonCode,
            CorrelationId = correlationId,
            SourceReportId = sourceReportId,
            SourceReportDecisionId = sourceReportDecisionId
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(firstSession.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Moderated);
        await Assert.That(secondSession.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Moderated);
        await _eventSessionRepository.Received(1).Update(firstSession);
        await _eventSessionRepository.Received(1).Update(secondSession);
        await Assert.That(createdRecords).Count().IsEqualTo(1);
        var record = createdRecords.Single();
        await Assert.That(record.TenantId).IsEqualTo(@event.TenantId);
        await Assert.That(record.EventId).IsEqualTo(@event.Id);
        await Assert.That(record.ModeratorUserId).IsEqualTo(moderatorUserId);
        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.LightModerated);
        await Assert.That(record.ReasonCode).IsEqualTo(reasonCode);
        await Assert.That(record.CorrelationId).IsEqualTo(correlationId);
        await Assert.That(record.SourceReportId).IsEqualTo(sourceReportId);
        await Assert.That(record.SourceReportDecisionId).IsEqualTo(sourceReportDecisionId);
        await Assert.That(record.PreviousStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await Assert.That(record.ResultingStatusId).IsEqualTo((int)EventStatusEnum.Moderated);
        await Assert.That(record.IsIrreversible).IsFalse();
        await Assert.That(record.CreatedAt).IsEqualTo(Now);

        await _eventRepository.Received(1).Update(@event);
        await Assert.That(createdMessages).Count().IsEqualTo(1);
        var message = createdMessages.Single();
        await Assert.That(message.EventType).IsEqualTo(EventModerationOutboxMessageFactory.EventLightModeratedNotificationFanoutRequestedEventType);
        await Assert.That(message.AggregateId).IsEqualTo(@event.Id);
        await Assert.That(message.Status).IsEqualTo(OutboxMessageStatus.Pending);

        var payload = JsonSerializer.Deserialize<EventLightModeratedNotificationFanoutRequested>(message.Payload!);
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.TenantId).IsEqualTo(@event.TenantId);
        await Assert.That(payload.EventId).IsEqualTo(@event.Id);
        await Assert.That(payload.EventTitle).IsEqualTo(@event.Title);
        await Assert.That(payload.ModerationRecordId).IsEqualTo(record.Id);

        await _cache.Received(1).RemoveAsync($"event:detail:{@event.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventAlreadyModerated_ReturnsSuccessWithoutDuplicateAudit()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var @event = CreateEvent(EventStatusEnum.Moderated);
        var unitOfWork = new RetryingUnitOfWork();
        bool loggerObservedCommit = false;
        var logger = new TestLogger<ModerateEventCommandHandler>(() => loggerObservedCommit = unitOfWork.Completed);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);
        var handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new ModerateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _eventSessionRepository.DidNotReceive().Update(Arg.Any<EventSession>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("light_moderated")).IsEqualTo(0);
        await Assert.That(loggerObservedCommit).IsTrue();
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(logger.Entries[0].Message).Contains("already moderated");
    }

    [Test]
    public async Task Handle_WhenAlreadyModeratedSessionNeedsRepair_InvalidatesAndRecordsOnceAfterCommit()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var @event = CreateEvent(EventStatusEnum.Moderated);
        var session = CreateSession(@event, EventSessionStatusEnum.Published);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);
        _eventSessionRepository.GetSessionsByEvent(@event.Id).Returns([session]);
        var handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            _unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            NullLogger<ModerateEventCommandHandler>.Instance,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ModerateEventCommand { Id = @event.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(session.EventSessionStatusId).IsEqualTo((int)EventSessionStatusEnum.Moderated);
        await Assert.That(session.UpdatedAt).IsEqualTo(Now.UtcDateTime);
        await _eventSessionRepository.Received(1).Update(session);
        await _cache.Received(1).RemoveAsync($"event:detail:{@event.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("light_moderated")).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WhenTransactionDelegateRetries_AppliesCacheAndMetricOnceAfterCommit()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Published);
        var retryEvent = CreateEvent(EventStatusEnum.Published);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        var createdRecords = new List<EventModerationRecord>();
        var createdMessages = new List<OutboxMessage>();
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(createdRecords.Add))
            .Returns(call => call.Arg<EventModerationRecord>());
        _outboxRepository.Create(Arg.Do<OutboxMessage>(createdMessages.Add))
            .Returns(call => call.Arg<OutboxMessage>());
        var unitOfWork = new RetryingUnitOfWork();
        var logger = new TestLogger<ModerateEventCommandHandler>();
        bool cacheObservedCommit = false;
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cacheObservedCommit = unitOfWork.Completed;
                return ValueTask.CompletedTask;
            });
        var handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ModerateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _moderationRecordRepository.Received(2).Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.Received(2).Create(Arg.Any<OutboxMessage>());
        await Assert.That(createdRecords.Select(record => record.Id).Distinct()).Count().IsEqualTo(1);
        await Assert.That(createdMessages.Select(message => message.Id).Distinct()).Count().IsEqualTo(1);
        await _cache.Received(1).RemoveAsync($"event:detail:{firstAttemptEvent.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(firstAttemptEvent.TenantId), Arg.Any<CancellationToken>());
        await Assert.That(cacheObservedCommit).IsTrue();
        await Assert.That(metricsCapture.Count("light_moderated")).IsEqualTo(1);
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(logger.Entries[0].Message).Contains("Light event moderation succeeded");
    }

    [Test]
    public async Task Handle_WhenCommitAmbiguityRetryFindsEventModerated_PreservesPostCommitEffects()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Published);
        var retryEvent = CreateEvent(EventStatusEnum.Moderated);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        var unitOfWork = new RetryingUnitOfWork();
        var logger = new TestLogger<ModerateEventCommandHandler>();
        var handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ModerateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _moderationRecordRepository.Received(1).Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.Received(1).Create(Arg.Any<OutboxMessage>());
        await _eventRepository.Received(1).Update(firstAttemptEvent);
        await _cache.Received(1).RemoveAsync($"event:detail:{firstAttemptEvent.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(firstAttemptEvent.TenantId), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("light_moderated")).IsEqualTo(1);
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Message).Contains("Light event moderation succeeded");
    }

    [Test]
    public async Task Handle_WhenRolledBackAttemptIsFollowedByInvalidStatus_ReturnsFailureWithoutPostCommitEffects()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Published);
        var retryEvent = CreateEvent(EventStatusEnum.Draft);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        var logger = new TestLogger<ModerateEventCommandHandler>();
        var handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            new RetryingUnitOfWork(),
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ModerateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_light_moderation_invalid_status");
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("light_moderated")).IsEqualTo(0);
        await Assert.That(logger.Entries.Count(entry => entry.Message.Contains("succeeded", StringComparison.Ordinal))).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenCommitAmbiguityRetryFindsSessionsRepaired_PreservesPostCommitEffects()
    {
        using var metricsCapture = new MetricsCapture();
        using var metrics = CreateMetrics();
        var firstAttemptEvent = CreateEvent(EventStatusEnum.Moderated);
        var retryEvent = CreateEvent(EventStatusEnum.Moderated);
        retryEvent.Id = firstAttemptEvent.Id;
        retryEvent.TenantId = firstAttemptEvent.TenantId;
        var firstAttemptSession = CreateSession(firstAttemptEvent, EventSessionStatusEnum.Published);
        var retrySession = CreateSession(retryEvent, EventSessionStatusEnum.Moderated);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(firstAttemptEvent.Id).Returns(firstAttemptEvent, retryEvent);
        _eventSessionRepository.GetSessionsByEvent(firstAttemptEvent.Id)
            .Returns([firstAttemptSession], [retrySession]);
        var unitOfWork = new RetryingUnitOfWork();
        var logger = new TestLogger<ModerateEventCommandHandler>();
        var handler = new ModerateEventCommandHandler(
            _eventRepository,
            _eventSessionRepository,
            _moderationRecordRepository,
            _outboxRepository,
            unitOfWork,
            _currentUserService,
            _cache,
            metrics,
            logger,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new FixedTimeProvider(Now));

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ModerateEventCommand { Id = firstAttemptEvent.Id },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _eventSessionRepository.Received(1).Update(firstAttemptSession);
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _cache.Received(1).RemoveAsync($"event:detail:{firstAttemptEvent.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(firstAttemptEvent.TenantId), Arg.Any<CancellationToken>());
        await Assert.That(metricsCapture.Count("light_moderated")).IsEqualTo(1);
        await Assert.That(logger.Entries).Count().IsEqualTo(1);
        await Assert.That(logger.Entries[0].Message).Contains("already moderated");
    }

    [Test]
    public async Task Handle_WithSourceReportDecisionAndNoCurrentUser_WritesProviderAuditRecord()
    {
        var @event = CreateEvent(EventStatusEnum.Published);
        var sourceReportId = Guid.NewGuid();
        var sourceReportDecisionId = Guid.NewGuid();
        var createdRecords = new List<EventModerationRecord>();

        _currentUserService.UserId.Returns((Guid?)null);
        _eventRepository.GetById(@event.Id).Returns(@event);
        _moderationRecordRepository.Create(Arg.Do<EventModerationRecord>(record => createdRecords.Add(record)))
            .Returns(call => call.Arg<EventModerationRecord>());

        var result = await _handler.Handle(new ModerateEventCommand
        {
            Id = @event.Id,
            ReasonCode = "coop_decision",
            CorrelationId = "coop-correlation",
            SourceReportId = sourceReportId,
            SourceReportDecisionId = sourceReportDecisionId
        }, CancellationToken.None);

        var record = createdRecords.Single();
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(record.ModeratorUserId).IsNull();
        await Assert.That(record.SourceReportId).IsEqualTo(sourceReportId);
        await Assert.That(record.SourceReportDecisionId).IsEqualTo(sourceReportDecisionId);
        await Assert.That(record.ActionKind).IsEqualTo(EventModerationActionKind.LightModerated);
        await _eventRepository.Received(1).Update(@event);
    }

    [Test]
    public async Task Handle_WhenEventIsNotPublished_ReturnsInvalidStatusFailure()
    {
        var @event = CreateEvent(EventStatusEnum.Draft);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new ModerateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_light_moderation_invalid_status");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
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

    private static EventSession CreateSession(Explore.Domain.Event @event, EventSessionStatusEnum status) => new(status)
    {
        Id = Guid.NewGuid(),
        EventId = @event.Id,
        Event = @event,
        TenantId = @event.TenantId,
        Tenant = null!,
        Title = "Session"
    };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
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

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
            operation(ct);
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
