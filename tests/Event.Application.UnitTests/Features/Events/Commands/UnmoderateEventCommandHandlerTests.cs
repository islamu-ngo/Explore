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
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public sealed class UnmoderateEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventModerationRecordRepository _moderationRecordRepository = Substitute.For<IEventModerationRecordRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UnmoderateEventCommandHandler _handler;

    public UnmoderateEventCommandHandlerTests()
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

        _handler = new UnmoderateEventCommandHandler(
            _eventRepository,
            _moderationRecordRepository,
            _unitOfWork,
            _currentUserService,
            _cache,
            CreateMetrics(),
            NullLogger<UnmoderateEventCommandHandler>.Instance,
            AtprotoPublicationPlannerTestFactory.Disabled());
    }

    [Test]
    public async Task Handle_WhenLatestModerationIsReversibleLight_RestoresPublishedAndWritesAudit()
    {
        var moderatorUserId = Guid.NewGuid();
        var @event = CreateEvent(EventStatusEnum.Moderated);
        const string reasonCode = "appeal_approved";
        const string correlationId = "case-restore-123";
        var sourceRecord = EventModerationRecord.CreateLightModeration(
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

        await Assert.That(result.Success).IsTrue();
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

        await _eventRepository.Received(1).Update(@event);
        await _cache.Received(1).RemoveAsync($"event:detail:{@event.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenLatestModerationIsIrreversibleHeavy_ReturnsNotReversibleFailure()
    {
        var @event = CreateEvent(EventStatusEnum.Moderated);
        var sourceRecord = EventModerationRecord.CreateHeavyRedaction(
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_unmoderation_not_reversible");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotModerated_ReturnsInvalidStatusFailure()
    {
        var @event = CreateEvent(EventStatusEnum.Published);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new UnmoderateEventCommand { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_unmoderation_invalid_status");
        await _moderationRecordRepository.DidNotReceive().Create(Arg.Any<EventModerationRecord>());
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

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }
}
