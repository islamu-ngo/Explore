// ABOUTME: Unit tests for location query cancellation propagation.
// ABOUTME: Proves handlers pass MediatR cancellation tokens into repository reads.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.EventSessions.Handlers.Queries;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Features.Locations.Handlers.Queries;
using Explore.Application.Features.Locations.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Locations.Queries;

public sealed class LocationQueryHandlerCancellationTests
{
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task GetLocationList_ForwardsCancellationToken()
    {
        var locations = new List<Location>();
        var handler = new GetLocationListRequestHandler(_locationRepository, _mapper);
        var request = new GetLocationListRequest { PageNumber = 2, PageSize = 10 };
        using var cancellation = new CancellationTokenSource();

        _locationRepository.GetLocationsWithDetailsPaged(2, 10, cancellation.Token).Returns((locations, 0));
        _mapper.Map<List<LocationListDto>>(locations).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _locationRepository.Received(1).GetLocationsWithDetailsPaged(2, 10, cancellation.Token);
    }

    [Test]
    public async Task GetEventSessionCreateContext_ForwardsCancellationTokenToEventScopedLookups()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = "Tenant",
            Slug = "tenant",
            TenantStatus = null!
        };
        var eventEntity = new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = "Program",
            TenantId = tenant.Id,
            Tenant = tenant,
            Actor = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
        var eventRepository = Substitute.For<IEventRepository>();
        var roomRepository = Substitute.For<ILocationRoomRepository>();
        var groupRepository = Substitute.For<IEventSessionGroupRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var agendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        var handler = new GetEventSessionCreateContextRequestHandler(
            eventRepository,
            _locationRepository,
            roomRepository,
            groupRepository,
            sessionRepository,
            agendaItemRepository);
        using var cancellation = new CancellationTokenSource();

        eventRepository.GetEventWithDetails(eventEntity.Id).Returns(eventEntity);
        groupRepository.GetActiveByEventAsync(eventEntity.Id, cancellation.Token).Returns([]);
        sessionRepository.GetSessionsByEvent(eventEntity.Id).Returns([]);
        agendaItemRepository.GetByEventAsync(eventEntity.Id, cancellation.Token).Returns([]);

        await handler.Handle(new GetEventSessionCreateContextRequest { EventId = eventEntity.Id }, cancellation.Token);

        await groupRepository.Received(1).GetActiveByEventAsync(eventEntity.Id, cancellation.Token);
        await agendaItemRepository.Received(1).GetByEventAsync(eventEntity.Id, cancellation.Token);
    }
}
