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
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public sealed class ModerateEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly ModerateEventCommandHandler _handler;

    public ModerateEventCommandHandlerTests()
    {
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var operation = call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

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
            AtprotoPublicationPlannerTestFactory.Disabled());
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

        await Assert.That(result.Success).IsTrue();
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
        var @event = CreateEvent(EventStatusEnum.Moderated);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new ModerateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
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
        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_light_moderation_invalid_status");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    private static Explore.Domain.Event CreateEvent(EventStatusEnum status) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        ActorId = Guid.CreateVersion7(),
        Actor = null!,
        Title = "Community Iftar",
        EventStatusId = (int)status,
        EventStatus = null!,
        VisibilityTypeId = 1,
        VisibilityType = null!,
        EventFormatId = 1,
        EventFormat = null!,
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private static EventSession CreateSession(Explore.Domain.Event @event, EventSessionStatusEnum status) => new()
    {
        Id = Guid.NewGuid(),
        EventId = @event.Id,
        Event = @event,
        TenantId = @event.TenantId,
        Tenant = null!,
        Title = "Session",
        EventSessionStatusId = (int)status
    };

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}
