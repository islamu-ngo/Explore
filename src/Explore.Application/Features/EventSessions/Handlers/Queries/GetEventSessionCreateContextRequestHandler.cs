// ABOUTME: Query handler for event-scoped program item creation context.
// ABOUTME: Returns only locations and rooms already referenced by the authorized event boundary.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries;

public class GetEventSessionCreateContextRequestHandler : IRequestHandler<GetEventSessionCreateContextRequest, EventSessionCreateContextDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;

    public GetEventSessionCreateContextRequestHandler(
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        ILocationRoomRepository locationRoomRepository,
        IEventSessionGroupRepository eventSessionGroupRepository,
        IEventSessionRepository eventSessionRepository,
        IEventAgendaItemRepository eventAgendaItemRepository)
    {
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _locationRoomRepository = locationRoomRepository;
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _eventSessionRepository = eventSessionRepository;
        _eventAgendaItemRepository = eventAgendaItemRepository;
    }

    public async Task<EventSessionCreateContextDto?> Handle(GetEventSessionCreateContextRequest request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetEventWithDetails(request.EventId);
        if (eventEntity is null)
            return null;

        var sessions = await _eventSessionRepository.GetSessionsByEvent(eventEntity.Id);
        var groups = await _eventSessionGroupRepository.GetActiveByEventAsync(eventEntity.Id, cancellationToken);
        var agendaItems = await _eventAgendaItemRepository.GetByEventAsync(eventEntity.Id, cancellationToken);

        var referencedLocationIds = sessions
            .Select(session => session.LocationId)
            .Concat(groups.Select(group => group.LocationId))
            .Concat(agendaItems.Select(item => item.LocationId))
            .OfType<Guid>()
            .ToHashSet();
        var referencedRoomIds = sessions
            .Select(session => session.RoomId)
            .Concat(groups.Select(group => group.RoomId))
            .Concat(agendaItems.Select(item => item.RoomId))
            .OfType<Guid>()
            .ToHashSet();

        var referencedRooms = new List<Explore.Domain.LocationRoom>();
        foreach (Guid roomId in referencedRoomIds)
        {
            var room = await _locationRoomRepository.GetById(roomId);
            if (room?.TenantId != eventEntity.TenantId)
                continue;

            referencedRooms.Add(room);
            referencedLocationIds.Add(room.LocationId);
        }

        var referencedLocations = new List<Explore.Domain.Location>();
        foreach (Guid locationId in referencedLocationIds)
        {
            var location = await _locationRepository.GetById(locationId);
            if (location?.TenantId == eventEntity.TenantId)
                referencedLocations.Add(location);
        }

        var context = new EventSessionCreateContextDto
        {
            EventId = eventEntity.Id,
            EventTitle = eventEntity.Title,
            TenantId = eventEntity.TenantId,
            TimeZoneId = eventEntity.EventTimeZoneId ?? eventEntity.Timezone,
            EventStartDate = eventEntity.FirstSessionDate,
            EventEndDate = eventEntity.LastSessionDate,
            Defaults = new EventSessionCreateDefaultsDto
            {
                SessionDate = eventEntity.FirstSessionDate,
                RegistrationModeId = (int)RegistrationModeEnum.Open
            },
            Locations = referencedLocations
                .OrderBy(location => location.FullName)
                .ThenBy(location => location.City)
                .Select(location => new EventSessionCreateLocationOptionDto
                {
                    Id = location.Id,
                    FullName = location.FullName,
                    City = location.City,
                    Country = location.Country,
                    TimeZoneId = location.Timezone
                })
                .ToList(),
            Rooms = referencedRooms
                .OrderBy(room => room.SortOrder)
                .ThenBy(room => room.Name)
                .Select(room => new EventSessionCreateRoomOptionDto
                {
                    Id = room.Id,
                    LocationId = room.LocationId,
                    Name = room.Name,
                    Capacity = room.Capacity,
                    SortOrder = room.SortOrder
                })
                .ToList(),
            SessionGroups = groups
                .OrderBy(group => group.SortOrder)
                .ThenBy(group => group.Name)
                .Select(group => new EventSessionCreateGroupOptionDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    LocationId = group.LocationId,
                    LocationName = group.Location?.FullName,
                    RoomId = group.RoomId,
                    RoomName = group.Room?.Name,
                    Color = group.Color,
                    SortOrder = group.SortOrder
                })
                .ToList()
        };

        AddNotices(context);
        return context;
    }

    private static void AddNotices(EventSessionCreateContextDto context)
    {
        if (string.IsNullOrWhiteSpace(context.TimeZoneId))
        {
            context.Notices.Add("No event timezone is configured yet. Program item times use your local timezone until the event timezone is set.");
        }

        if (!context.EventStartDate.HasValue || !context.EventEndDate.HasValue)
        {
            context.Notices.Add("No event date window is configured yet. Confirm the program item date before publishing.");
        }

        if (context.SessionGroups.Count == 0)
        {
            context.Notices.Add("No program sections exist yet. You can save the program item now and assign it to a track or section later.");
        }

        if (context.Locations.Count == 0)
        {
            context.Notices.Add("No event-associated locations are available. Venue selection remains unavailable until a location is linked through an authorized event management flow.");
        }
    }
}
