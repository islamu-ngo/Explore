// ABOUTME: Unit tests for server-owned program item create context assembly.
// ABOUTME: Protects event-scoped defaults, selector options, and missing-event behavior.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Handlers.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessions.Queries;

public sealed class GetEventSessionCreateContextRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly ILocationRoomRepository _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository = Substitute.For<IEventSessionGroupRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();

    [Test]
    public async Task Handle_WhenEventExists_ReturnsEventDefaultsAndSelectorOptions()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        var eventEntity = CreateEvent(eventId, tenant);
        var location = CreateLocation(Guid.NewGuid(), tenant, "Main Hall", "Brussels", "Belgium");
        var room = CreateRoom(Guid.NewGuid(), location, "Auditorium", capacity: 120, sortOrder: 2);
        var group = CreateGroup(Guid.NewGuid(), eventEntity, tenant, location, room);

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _locationRepository.GetById(location.Id).Returns(location);
        _locationRoomRepository.GetById(room.Id).Returns(room);
        _eventSessionGroupRepository.GetActiveByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([group]);
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([]);
        _eventAgendaItemRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(
            new GetEventSessionCreateContextRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(eventId);
        await Assert.That(result.EventTitle).IsEqualTo("Program launch");
        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.TimeZoneId).IsEqualTo("Europe/Brussels");
        await Assert.That(result.Defaults.SessionDate).IsEqualTo(new DateOnly(2026, 6, 1));
        await Assert.That(result.Defaults.RegistrationModeId).IsEqualTo((int)RegistrationModeEnum.Open);
        await Assert.That(result.Locations.Single().FullName).IsEqualTo("Main Hall");
        await Assert.That(result.Rooms.Single().Capacity).IsEqualTo(120);
        await Assert.That(result.SessionGroups.Single().RoomName).IsEqualTo("Auditorium");
        await Assert.That(result.Notices).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenEventIsMissing_ReturnsNull()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetEventWithDetails(eventId).Returns((Explore.Domain.Event?)null);

        var result = await CreateHandler().Handle(
            new GetEventSessionCreateContextRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await _locationRepository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenEventHasIncompleteProgramSetup_ReturnsGuidanceNotices()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId);
        var eventEntity = CreateEvent(eventId, tenant);
        eventEntity.EventTimeZoneId = null;
        eventEntity.Timezone = null;
        eventEntity.FirstSessionDate = null;
        eventEntity.LastSessionDate = null;

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _eventSessionGroupRepository.GetActiveByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([]);
        _eventAgendaItemRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(
            new GetEventSessionCreateContextRequest { EventId = eventId },
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Notices.Count).IsEqualTo(4);
        await Assert.That(result.Notices[0]).Contains("No event timezone");
        await Assert.That(result.Notices[1]).Contains("No event date window");
        await Assert.That(result.Notices[2]).Contains("No program sections");
        await Assert.That(result.Notices[3]).Contains("No event-associated locations");
    }

    private GetEventSessionCreateContextRequestHandler CreateHandler()
    {
        return new GetEventSessionCreateContextRequestHandler(
            _eventRepository,
            _locationRepository,
            _locationRoomRepository,
            _eventSessionGroupRepository,
            _eventSessionRepository,
            _eventAgendaItemRepository);
    }

    private static Tenant CreateTenant(Guid tenantId)
    {
        return new Tenant
        {
            Id = tenantId,
            FullName = "Tenant",
            Slug = "tenant",
            TenantStatus = null!
        };
    }

    private static Explore.Domain.Event CreateEvent(Guid eventId, Tenant tenant)
    {
        return new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Program launch",
            TenantId = tenant.Id,
            Tenant = tenant,
            Actor = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            FirstSessionDate = new DateOnly(2026, 6, 1),
            LastSessionDate = new DateOnly(2026, 6, 2),
            Timezone = "Europe/Brussels",
            EventTimeZoneId = "Europe/Brussels"
        };
    }

    private static Location CreateLocation(Guid locationId, Tenant tenant, string fullName, string city, string country)
    {
        var location = new Location
        {
            Id = locationId,
            FullName = fullName,
            City = city,
            Country = country,
            Timezone = "Europe/Brussels",
            TenantId = tenant.Id,
            Tenant = tenant
        };
        location.SetManualAddress("Rue Test 1", "1000");
        return location;
    }

    private static LocationRoom CreateRoom(Guid roomId, Location location, string name, int capacity, int sortOrder)
    {
        return new LocationRoom
        {
            Id = roomId,
            LocationId = location.Id,
            Location = location,
            Name = name,
            Capacity = capacity,
            SortOrder = sortOrder,
            TenantId = location.TenantId,
            Tenant = location.Tenant
        };
    }

    private static EventSessionGroup CreateGroup(Guid groupId, Explore.Domain.Event eventEntity, Tenant tenant, Location location, LocationRoom room)
    {
        return new EventSessionGroup
        {
            Id = groupId,
            EventId = eventEntity.Id,
            Event = eventEntity,
            Name = "Main track",
            LocationId = location.Id,
            Location = location,
            RoomId = room.Id,
            Room = room,
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = tenant
        };
    }
}
