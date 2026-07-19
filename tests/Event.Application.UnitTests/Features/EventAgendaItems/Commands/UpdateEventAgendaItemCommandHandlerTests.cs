// ABOUTME: Unit tests for grouped EventAgendaItem update command handling.
// ABOUTME: Covers validation, optimistic concurrency, schedule projection, relationship checks, and cache invalidation.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventAgendaItems.Handlers.Commands;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventAgendaItems.Commands;

public class UpdateEventAgendaItemCommandHandlerTests
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventDayRepository _eventDayRepository = Substitute.For<IEventDayRepository>();
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly ILocationRoomRepository _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository = Substitute.For<IScheduleItemKindRepository>();
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator = new EventScheduleProjectionCalculator();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventAgendaItemCommandHandler _handler;

    public UpdateEventAgendaItemCommandHandlerTests()
    {
        _eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            _eventRepository,
            Guid.NewGuid());
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>()));

        _handler = new UpdateEventAgendaItemCommandHandler(
            _eventAgendaItemRepository,
            _eventRepository,
            _eventDayRepository,
            _locationRepository,
            _locationRoomRepository,
            _scheduleItemKindRepository,
            _scheduleProjectionCalculator,
            _unitOfWork,
            _eventLocationAttachmentService,
            _cache
        );
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            EventAgendaItemDto = new UpdateEventAgendaItemDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event agenda item update failed.");
        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var tenantId = Guid.CreateVersion7();
        var parentEvent = CreateEvent(tenantId);
        var agendaItem = CreateAgendaItem(parentEvent.Id, tenantId);
        _eventAgendaItemRepository.GetById(agendaItem.Id).Returns(agendaItem);
        _eventRepository.GetById(parentEvent.Id).Returns(parentEvent);

        await Assert.That(async () => await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = agendaItem.Id,
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Title = new UpdateEventAgendaItemTitleDto { Value = "Updated Keynote" }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSingleFieldGroupIsPresent_UpdatesOnlyThatField()
    {
        var tenantId = Guid.CreateVersion7();
        var parentEvent = CreateEvent(tenantId);
        var agendaItem = CreateAgendaItem(parentEvent.Id, tenantId);
        var originalDescription = agendaItem.Description;
        var originalSortOrder = agendaItem.SortOrder;
        _eventAgendaItemRepository.GetById(agendaItem.Id).Returns(agendaItem);
        _eventRepository.GetById(parentEvent.Id).Returns(parentEvent);

        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = agendaItem.Id,
            ExpectedConcurrencyStamp = agendaItem.ConcurrencyStamp,
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Title = new UpdateEventAgendaItemTitleDto { Value = "Updated Keynote" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(agendaItem.Title).IsEqualTo("Updated Keynote");
        await Assert.That(agendaItem.Description).IsEqualTo(originalDescription);
        await Assert.That(agendaItem.SortOrder).IsEqualTo(originalSortOrder);
        await _eventAgendaItemRepository.Received(1).Update(agendaItem);
        await _cache.Received(1).RemoveAsync($"event:detail:{parentEvent.Id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDescriptionIsExplicitlyCleared_SetsDescriptionToNull()
    {
        var tenantId = Guid.CreateVersion7();
        var parentEvent = CreateEvent(tenantId);
        var agendaItem = CreateAgendaItem(parentEvent.Id, tenantId);
        _eventAgendaItemRepository.GetById(agendaItem.Id).Returns(agendaItem);
        _eventRepository.GetById(parentEvent.Id).Returns(parentEvent);

        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = agendaItem.Id,
            ExpectedConcurrencyStamp = agendaItem.ConcurrencyStamp,
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Description = new UpdateEventAgendaItemDescriptionDto
                {
                    Value = OptionalUpdate<string?>.Set(null)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(agendaItem.Description).IsNull();
        await _eventAgendaItemRepository.Received(1).Update(agendaItem);
    }

    [Test]
    public async Task Handle_WhenDescriptionGroupHasNoFieldOperation_ReturnsValidationFailure()
    {
        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = Guid.CreateVersion7(),
            ExpectedConcurrencyStamp = Guid.CreateVersion7(),
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Description = new UpdateEventAgendaItemDescriptionDto()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Description must include an explicit field operation.");
        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
    }

    [Test]
    public async Task Handle_WhenRescheduledToMatchingEventDay_LinksAgendaItemToEventDay()
    {
        var tenantId = Guid.CreateVersion7();
        var parentEvent = CreateEvent(tenantId);
        var agendaItem = CreateAgendaItem(parentEvent.Id, tenantId);
        var eventDayId = Guid.CreateVersion7();
        var startUtc = new DateTimeOffset(2026, 7, 20, 7, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        var expectedLocalDate = new DateOnly(2026, 7, 20);
        _eventAgendaItemRepository.GetById(agendaItem.Id).Returns(agendaItem);
        _eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        _eventDayRepository.FindByEventAndLocalDateAsync(parentEvent.Id, expectedLocalDate, Arg.Any<CancellationToken>())
            .Returns(new EventDay
            {
                Id = eventDayId,
                EventId = parentEvent.Id,
                LocalDate = expectedLocalDate,
                Event = null!,
                Tenant = null!
            });

        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = agendaItem.Id,
            ExpectedConcurrencyStamp = agendaItem.ConcurrencyStamp,
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Schedule = new UpdateEventAgendaItemScheduleDto
                {
                    StartTime = startUtc,
                    EndTime = endUtc
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(agendaItem.StartTime).IsEqualTo(startUtc);
        await Assert.That(agendaItem.EndTime).IsEqualTo(endUtc);
        await Assert.That(agendaItem.EventDayId).IsEqualTo(eventDayId);
    }

    [Test]
    public async Task Handle_WhenRoomBelongsToDifferentLocation_ReturnsFailureAndDoesNotSave()
    {
        var tenantId = Guid.CreateVersion7();
        var parentEvent = CreateEvent(tenantId);
        var agendaItem = CreateAgendaItem(parentEvent.Id, tenantId);
        var selectedLocation = CreateLocation(tenantId);
        var selectedRoom = CreateRoom(tenantId, Guid.CreateVersion7());
        _eventAgendaItemRepository.GetById(agendaItem.Id).Returns(agendaItem);
        _eventRepository.GetById(parentEvent.Id).Returns(parentEvent);
        _locationRepository.Exists(selectedLocation.Id).Returns(true);
        _locationRepository.GetById(selectedLocation.Id).Returns(selectedLocation);
        _locationRoomRepository.Exists(selectedRoom.Id).Returns(true);
        _locationRoomRepository.GetById(selectedRoom.Id).Returns(selectedRoom);

        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = agendaItem.Id,
            ExpectedConcurrencyStamp = agendaItem.ConcurrencyStamp,
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Location = new UpdateEventAgendaItemLocationDto
                {
                    Value = OptionalUpdate<Guid?>.Set(selectedLocation.Id)
                },
                Room = new UpdateEventAgendaItemRoomDto
                {
                    Value = OptionalUpdate<Guid?>.Set(selectedRoom.Id)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Room must belong to the selected location.");
        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenMovingToCrossTenantEvent_ReturnsFailureAndDoesNotSave()
    {
        var tenantId = Guid.CreateVersion7();
        var otherTenantId = Guid.CreateVersion7();
        var originalEvent = CreateEvent(tenantId);
        var crossTenantEvent = CreateEvent(otherTenantId);
        var agendaItem = CreateAgendaItem(originalEvent.Id, tenantId);
        _eventAgendaItemRepository.GetById(agendaItem.Id).Returns(agendaItem);
        _eventRepository.GetById(crossTenantEvent.Id).Returns(crossTenantEvent);
        _eventRepository.Exists(crossTenantEvent.Id).Returns(true);

        var result = await _handler.Handle(new UpdateEventAgendaItemCommand
        {
            EventAgendaItemId = agendaItem.Id,
            ExpectedConcurrencyStamp = agendaItem.ConcurrencyStamp,
            EventAgendaItemDto = new UpdateEventAgendaItemDto
            {
                Event = new UpdateEventAgendaItemEventDto { EventId = crossTenantEvent.Id }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Event does not belong to the same tenant as the agenda item.");
        await _eventAgendaItemRepository.DidNotReceive().Update(Arg.Any<EventAgendaItem>());
    }

    private static Explore.Domain.Event CreateEvent(Guid tenantId)
    {
        return new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Title = "Event",
            Timezone = "Europe/Brussels",
            EventTimeZoneId = "Europe/Brussels",
            Actor = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            Tenant = null!
        };
    }

    private static EventAgendaItem CreateAgendaItem(Guid eventId, Guid tenantId)
    {
        var agendaItem = new EventAgendaItem
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            TenantId = tenantId,
            Title = "Existing Agenda Item",
            Description = "Existing description",
            StartTime = new DateTimeOffset(2026, 7, 20, 6, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 20, 7, 0, 0, TimeSpan.Zero),
            SortOrder = 3,
            ConcurrencyStamp = Guid.CreateVersion7(),
            Event = null!,
            Tenant = null!
        };
        agendaItem.ReprojectLocalTimes("Europe/Brussels", new EventScheduleProjectionCalculator());
        return agendaItem;
    }

    private static Location CreateLocation(Guid tenantId)
    {
        return new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            FullName = "Main Venue",
            Country = "Belgium",
            City = "Brussels",
            Pii = new LocationPii
            {
                Address = "Main Street 1",
                Postcode = "1000"
            },
            Tenant = null!
        };
    }

    private static LocationRoom CreateRoom(Guid tenantId, Guid locationId)
    {
        return new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            LocationId = locationId,
            Name = "Room A",
            Location = null!,
            Tenant = null!
        };
    }
}
