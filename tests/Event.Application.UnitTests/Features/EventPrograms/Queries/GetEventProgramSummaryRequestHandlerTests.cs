// ABOUTME: Unit tests for server-backed event program summary assembly.
// ABOUTME: Protects local-day grouping, section assignment, and readiness warnings.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventPrograms.Handlers.Queries;
using Explore.Application.Features.EventPrograms.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventPrograms.Queries;

public sealed class GetEventProgramSummaryRequestHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository = Substitute.For<IEventSessionGroupRepository>();
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
    private readonly IEventLocationDisclosureService _disclosureService = Substitute.For<IEventLocationDisclosureService>();

    [Test]
    public async Task Handle_WhenEventHasGroupedSessions_ReturnsSectionsDaysItemsAndMetadata()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant(Guid.NewGuid());
        var eventEntity = CreateEvent(eventId, tenant);
        var location = CreateLocation(Guid.NewGuid(), tenant, "Main Hall");
        var room = CreateRoom(Guid.NewGuid(), location, "Auditorium");
        var group = CreateGroup(Guid.NewGuid(), eventEntity, tenant, location, room);
        var session = CreateSession(
            Guid.NewGuid(),
            eventEntity,
            tenant,
            location,
            room,
            new DateTimeOffset(2026, 6, 1, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 8, 15, 0, TimeSpan.Zero));
        session.SessionGroups.Add(CreateAssignment(group, session, eventEntity, tenant, isPrimary: true, sortOrder: 4));

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([session]);
        _eventSessionGroupRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([group]);
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetEventProgramSummaryRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(eventId);
        await Assert.That(result.TimeZoneId).IsEqualTo("Europe/Brussels");
        await Assert.That(result.Sections.Count).IsEqualTo(1);
        await Assert.That(result.Sections[0].Title).IsEqualTo("Main track");
        await Assert.That(result.Sections[0].SessionGroups.Single().Days.Single().DisplayLabel).IsEqualTo("Mon 1 Jun");

        var item = result.Sections[0].SessionGroups.Single().Days.Single().Items.Single();
        await Assert.That(item.Title).IsEqualTo("Opening talk");
        await Assert.That(item.LocalStartTime).IsEqualTo(new TimeOnly(9, 0));
        await Assert.That(item.LocalEndTime).IsEqualTo(new TimeOnly(10, 15));
        await Assert.That(item.RoomName).IsNull();
        await Assert.That(item.Capacity).IsEqualTo(120);
        await Assert.That(item.RegistrationModeName).IsEqualTo("Open");
        await Assert.That(result.ReadinessWarnings).IsEmpty();
        await _eventSessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task HandlePublicProgramDoesNotExposePhysicalVenueOrRoomNames()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant(Guid.NewGuid());
        var eventEntity = CreateEvent(eventId, tenant);
        var location = CreateLocation(Guid.NewGuid(), tenant, "Private Home Venue");
        var room = CreateRoom(Guid.NewGuid(), location, "Family Living Room");
        var group = CreateGroup(Guid.NewGuid(), eventEntity, tenant, location, room);
        var session = CreateSession(
            Guid.NewGuid(),
            eventEntity,
            tenant,
            location,
            room,
            new DateTimeOffset(2026, 6, 1, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        session.SessionGroups.Add(CreateAssignment(group, session, eventEntity, tenant, isPrimary: true, sortOrder: 1));

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([session]);
        _eventSessionGroupRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([group]);
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetEventProgramSummaryRequest { EventId = eventId }, CancellationToken.None);
        var section = result!.Sections.Single().SessionGroups.Single();
        var item = section.Days.Single().Items.Single();
        var leakedFields = new[]
        {
            section.LocationName,
            section.RoomName,
            item.LocationName,
            item.RoomName
        }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

        await Assert.That(leakedFields).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenEventIsMissing_ReturnsNull()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetEventWithDetails(eventId).Returns((Explore.Domain.Event?)null);

        var result = await CreateHandler().Handle(new GetEventProgramSummaryRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventSessionRepository.DidNotReceive().GetSessionsByEvent(Arg.Any<Guid>());
        await _eventSessionRepository.DidNotReceive().GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventAgendaItemRepository.DidNotReceive().GetPublicByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotPublicPublished_ReturnsNullWithoutLoadingProgram()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant(Guid.NewGuid());
        var eventEntity = CreateEvent(eventId, tenant);
        eventEntity.EventStatusId = (int)EventStatusEnum.Draft;

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);

        var result = await CreateHandler().Handle(new GetEventProgramSummaryRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventSessionRepository.DidNotReceive().GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventSessionGroupRepository.DidNotReceive().GetPublicByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventAgendaItemRepository.DidNotReceive().GetPublicByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleManaged_WhenDraftSessionIsUnscheduled_ReturnsItemAndReadinessWarnings()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant(Guid.NewGuid());
        var eventEntity = CreateEvent(eventId, tenant);
        eventEntity.EventStatusId = (int)EventStatusEnum.Draft;
        eventEntity.VisibilityTypeId = (int)VisibilityTypeEnum.Private;
        var session = CreateSession(
            Guid.NewGuid(),
            eventEntity,
            tenant,
            location: null,
            room: null,
            new DateTimeOffset(2026, 6, 1, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        session.StartTime = null;
        session.EndTime = null;
        session.ReprojectLocalTimes("Europe/Brussels", new Explore.Domain.Services.Scheduling.EventScheduleProjectionCalculator());

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([session]);
        _eventSessionGroupRepository.GetActiveByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);
        _eventAgendaItemRepository.GetByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(
            new GetManagedEventProgramSummaryRequest { EventId = eventId },
            CancellationToken.None);

        var item = result!.Sections.Single().SessionGroups.Single().Days.Single().Items.Single();
        await Assert.That(item.SessionId).IsEqualTo(session.Id);
        await Assert.That(item.StartsAtUtc).IsNull();
        await Assert.That(item.LocalDate).IsNull();
        await Assert.That(item.ReadinessWarnings.Any(warning => warning.Path.EndsWith("startTime", StringComparison.Ordinal))).IsTrue();
        await Assert.That(item.ReadinessWarnings.Any(warning => warning.Path.EndsWith("endTime", StringComparison.Ordinal))).IsTrue();
        await _eventSessionRepository.DidNotReceive().GetPublicSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenProgramSetupIsIncomplete_ReturnsUnassignedSectionAndWarnings()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant(Guid.NewGuid());
        var eventEntity = CreateEvent(eventId, tenant);
        eventEntity.EventTimeZoneId = null;
        eventEntity.Timezone = null;
        eventEntity.FirstSessionDate = null;
        eventEntity.LastSessionDate = null;
        var session = CreateSession(
            Guid.NewGuid(),
            eventEntity,
            tenant,
            location: null,
            room: null,
            new DateTimeOffset(2026, 6, 1, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        session.Title = null;
        session.MaxAudienceAttendees = null;
        session.RegistrationModeId = null;
        session.RegistrationMode = null;

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([session]);
        _eventSessionGroupRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetEventProgramSummaryRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Sections.Single().SectionKey).IsEqualTo("unassigned");
        await Assert.That(result.Sections.Single().Title).IsEqualTo("Unassigned program items");
        await Assert.That(result.ReadinessWarnings.Any(warning => warning.Path == "event.timeZoneId")).IsTrue();
        await Assert.That(result.ReadinessWarnings.Any(warning => warning.Path == "program.groups")).IsTrue();
        await Assert.That(result.ReadinessWarnings.Any(warning => warning.Path == "program.sessions[0].title")).IsTrue();
        await Assert.That(result.ReadinessWarnings.Any(warning =>
            warning.Path.Contains("locationId", StringComparison.OrdinalIgnoreCase)
            || warning.Message.Contains("no location or room assigned", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task Handle_WhenSessionAndAgendaAreOutsideEventWindow_ReturnsProgramReadinessPaths()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant(Guid.NewGuid());
        var eventEntity = CreateEvent(eventId, tenant);
        var session = CreateSession(
            Guid.NewGuid(),
            eventEntity,
            tenant,
            location: null,
            room: null,
            new DateTimeOffset(2026, 6, 5, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 5, 8, 0, 0, TimeSpan.Zero));
        var agendaItem = CreateAgendaItem(
            Guid.NewGuid(),
            eventEntity,
            tenant,
            new DateTimeOffset(2026, 5, 30, 7, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 30, 8, 0, 0, TimeSpan.Zero));

        _eventRepository.GetEventWithDetails(eventId).Returns(eventEntity);
        _eventSessionRepository.GetPublicSessionsByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([session]);
        _eventSessionGroupRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([]);
        _eventAgendaItemRepository.GetPublicByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([agendaItem]);

        var result = await CreateHandler().Handle(new GetEventProgramSummaryRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ReadinessWarnings.Any(warning => warning.Path == "program.sessions[0].startTime")).IsTrue();
        await Assert.That(result.ReadinessWarnings.Any(warning => warning.Path == "program.agenda[0].startTime")).IsTrue();
        await Assert.That(result.Sections.Single().SessionGroups.Single().Days.Single().Items.Single().ReadinessWarnings
            .Any(warning => warning.Path == "program.sessions[0].startTime")).IsTrue();
    }

    private GetEventProgramSummaryRequestHandler CreateHandler()
    {
        return new GetEventProgramSummaryRequestHandler(
            _eventRepository,
            _eventSessionRepository,
            _eventSessionGroupRepository,
            _eventAgendaItemRepository,
            _disclosureService);
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
            EventStatusId = (int)EventStatusEnum.Published,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            FirstSessionDate = new DateOnly(2026, 6, 1),
            LastSessionDate = new DateOnly(2026, 6, 2),
            Timezone = "Europe/Brussels",
            EventTimeZoneId = "Europe/Brussels"
        };
    }

    private static Location CreateLocation(Guid locationId, Tenant tenant, string fullName)
    {
        return new Location
        {
            Id = locationId,
            FullName = fullName,
            City = "Brussels",
            Country = "Belgium",
            TenantId = tenant.Id,
            Tenant = tenant,
            Pii = new LocationPii
            {
                LocationId = locationId,
                Address = "Rue Test 1",
                Postcode = "1000"
            }
        };
    }

    private static LocationRoom CreateRoom(Guid roomId, Location location, string name)
    {
        return new LocationRoom
        {
            Id = roomId,
            LocationId = location.Id,
            Location = location,
            Name = name,
            Capacity = 120,
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
            IsPublished = true,
            TenantId = tenant.Id,
            Tenant = tenant
        };
    }

    private static EventSession CreateSession(
        Guid sessionId,
        Explore.Domain.Event eventEntity,
        Tenant tenant,
        Location? location,
        LocationRoom? room,
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        return new EventSession
        {
            Id = sessionId,
            EventId = eventEntity.Id,
            Event = eventEntity,
            TenantId = tenant.Id,
            Tenant = tenant,
            Title = "Opening talk",
            StartTime = startTime,
            EndTime = endTime,
            SortOrder = 2,
            LocationId = location?.Id,
            Location = location,
            RoomId = room?.Id,
            Room = room,
            MaxAudienceAttendees = 120,
            RegistrationModeId = 1,
            RegistrationMode = new RegistrationMode
            {
                Id = 1,
                FullName = "Open",
                MasterCode = "OPEN"
            }
        };
    }

    private static EventAgendaItem CreateAgendaItem(
        Guid agendaItemId,
        Explore.Domain.Event eventEntity,
        Tenant tenant,
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        return new EventAgendaItem
        {
            Id = agendaItemId,
            EventId = eventEntity.Id,
            Event = eventEntity,
            TenantId = tenant.Id,
            Tenant = tenant,
            Title = "Lunch break",
            StartTime = startTime,
            EndTime = endTime,
            SortOrder = 1
        };
    }

    private static EventSessionGroupSession CreateAssignment(
        EventSessionGroup group,
        EventSession session,
        Explore.Domain.Event eventEntity,
        Tenant tenant,
        bool isPrimary,
        int sortOrder)
    {
        return new EventSessionGroupSession
        {
            Id = Guid.NewGuid(),
            EventSessionGroupId = group.Id,
            EventSessionGroup = group,
            EventSessionId = session.Id,
            EventSession = session,
            EventId = eventEntity.Id,
            Event = eventEntity,
            TenantId = tenant.Id,
            Tenant = tenant,
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
    }
}
