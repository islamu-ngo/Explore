// ABOUTME: Unit tests for grouped EventDay PATCH command handling.
// ABOUTME: Covers validation, concurrency, tenant safety, field clears, one-save updates, and cache invalidation.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventDays.Handlers.Commands;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventDays.Commands;

public class UpdateEventDayCommandHandlerTests
{
    private readonly IEventDayRepository _eventDayRepository = Substitute.For<IEventDayRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventDayCommandHandler _handler;

    public UpdateEventDayCommandHandlerTests()
    {
        _handler = new UpdateEventDayCommandHandler(
            _eventDayRepository,
            _eventRepository,
            Substitute.For<IStorageObjectRepository>(),
            _cache);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventDayCommand
        {
            EventDayId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventDayDto = new UpdateEventDayDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event day update group must be provided.");
        await _eventDayRepository.DidNotReceive().Update(Arg.Any<EventDay>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var eventDayId = Guid.NewGuid();
        var eventDay = CreateEventDay(eventDayId, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 7, 16));
        eventDay.ConcurrencyStamp = Guid.NewGuid();
        _eventDayRepository.GetById(eventDayId).Returns(eventDay);

        var command = new UpdateEventDayCommand
        {
            EventDayId = eventDayId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventDayDto = new UpdateEventDayDto
            {
                Label = new UpdateEventDayLabelDto { Value = OptionalUpdate<string?>.Set("New label") }
            }
        };

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _eventDayRepository.DidNotReceive().Update(Arg.Any<EventDay>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithLabelClear_SavesOnceAndInvalidatesParentEvent()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var eventDay = CreateEventDay(eventDayId, eventId, tenantId, new DateOnly(2026, 7, 16));
        eventDay.Label = "Opening day";
        eventDay.ConcurrencyStamp = stamp;
        _eventDayRepository.GetById(eventDayId).Returns(eventDay);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));

        var command = new UpdateEventDayCommand
        {
            EventDayId = eventDayId,
            ExpectedConcurrencyStamp = stamp,
            EventDayDto = new UpdateEventDayDto
            {
                Label = new UpdateEventDayLabelDto { Value = OptionalUpdate<string?>.Set(null) }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(eventDay.Label).IsNull();
        await _eventDayRepository.Received(1).Update(eventDay);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDuplicateLocalDate_ReturnsFailedResponseWithoutSaving()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var otherEventDayId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var localDate = new DateOnly(2026, 7, 17);
        var eventDay = CreateEventDay(eventDayId, eventId, tenantId, new DateOnly(2026, 7, 16));
        eventDay.ConcurrencyStamp = stamp;
        _eventDayRepository.GetById(eventDayId).Returns(eventDay);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _eventDayRepository.FindByEventAndLocalDateAsync(eventId, localDate, Arg.Any<CancellationToken>())
            .Returns(CreateEventDay(otherEventDayId, eventId, tenantId, localDate));

        var command = new UpdateEventDayCommand
        {
            EventDayId = eventDayId,
            ExpectedConcurrencyStamp = stamp,
            EventDayDto = new UpdateEventDayDto
            {
                LocalDate = new UpdateEventDayLocalDateDto { Value = localDate }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Another EventDay already exists for this event on the specified date.");
        await _eventDayRepository.DidNotReceive().Update(Arg.Any<EventDay>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithCrossTenantParentEvent_ReturnsFailedResponseWithoutSaving()
    {
        var eventDayId = Guid.NewGuid();
        var currentEventId = Guid.NewGuid();
        var newEventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var eventDay = CreateEventDay(eventDayId, currentEventId, tenantId, new DateOnly(2026, 7, 16));
        eventDay.ConcurrencyStamp = stamp;
        _eventDayRepository.GetById(eventDayId).Returns(eventDay);
        _eventRepository.Exists(newEventId).Returns(true);
        _eventRepository.GetById(newEventId).Returns(CreateEvent(newEventId, Guid.NewGuid()));

        var command = new UpdateEventDayCommand
        {
            EventDayId = eventDayId,
            ExpectedConcurrencyStamp = stamp,
            EventDayDto = new UpdateEventDayDto
            {
                Event = new UpdateEventDayEventDto { EventId = newEventId }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _eventDayRepository.DidNotReceive().Update(Arg.Any<EventDay>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenParentEventChanges_InvalidatesPreviousAndNewEventDetails()
    {
        var oldEventId = Guid.NewGuid();
        var newEventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var eventDayId = Guid.NewGuid();
        var stamp = Guid.NewGuid();
        var eventDay = CreateEventDay(eventDayId, oldEventId, tenantId, new DateOnly(2026, 7, 16));
        eventDay.ConcurrencyStamp = stamp;
        _eventDayRepository.GetById(eventDayId).Returns(eventDay);
        _eventRepository.Exists(newEventId).Returns(true);
        _eventRepository.GetById(newEventId).Returns(CreateEvent(newEventId, tenantId));

        var command = new UpdateEventDayCommand
        {
            EventDayId = eventDayId,
            ExpectedConcurrencyStamp = stamp,
            EventDayDto = new UpdateEventDayDto
            {
                Event = new UpdateEventDayEventDto { EventId = newEventId }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(eventDay.EventId).IsEqualTo(newEventId);
        await _eventDayRepository.Received(1).Update(eventDay);
        await _cache.Received(1).RemoveAsync($"event:detail:{oldEventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"event:detail:{newEventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    private static EventDay CreateEventDay(Guid id, Guid eventId, Guid tenantId, DateOnly localDate)
    {
        var eventDay = DataBuilder.EventDay.Generate();
        eventDay.Id = id;
        eventDay.EventId = eventId;
        eventDay.TenantId = tenantId;
        eventDay.LocalDate = localDate;
        return eventDay;
    }

    private static Explore.Domain.Event CreateEvent(Guid id, Guid tenantId)
    {
        var eventEntity = DataBuilder.Event.Generate();
        eventEntity.Id = id;
        eventEntity.TenantId = tenantId;
        return eventEntity;
    }
}
