// ABOUTME: Query handler for event-scoped program item creation context.
// ABOUTME: Aggregates event defaults, locations, rooms, and program sections through repository boundaries.

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

    public GetEventSessionCreateContextRequestHandler(
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        ILocationRoomRepository locationRoomRepository,
        IEventSessionGroupRepository eventSessionGroupRepository)
    {
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _locationRoomRepository = locationRoomRepository;
        _eventSessionGroupRepository = eventSessionGroupRepository;
    }

    public async Task<EventSessionCreateContextDto?> Handle(GetEventSessionCreateContextRequest request, CancellationToken cancellationToken)
    {
        var eventEntity = await _eventRepository.GetEventWithDetails(request.EventId);
        if (eventEntity is null)
            return null;

        var locations = await _locationRepository.GetLocationsByTenant(eventEntity.TenantId, cancellationToken);
        var orderedLocations = locations
            .OrderBy(location => location.FullName)
            .ThenBy(location => location.City)
            .ToList();

        var rooms = new List<EventSessionCreateRoomOptionDto>();
        foreach (var location in orderedLocations)
        {
            var locationRooms = await _locationRoomRepository.GetByLocationAsync(location.Id, cancellationToken);
            rooms.AddRange(locationRooms
                .OrderBy(room => room.SortOrder)
                .ThenBy(room => room.Name)
                .Select(room => new EventSessionCreateRoomOptionDto
                {
                    Id = room.Id,
                    LocationId = room.LocationId,
                    Name = room.Name,
                    Capacity = room.Capacity,
                    SortOrder = room.SortOrder
                }));
        }

        var groups = await _eventSessionGroupRepository.GetByEventAsync(eventEntity.Id, cancellationToken);

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
            Locations = orderedLocations
                .Select(location => new EventSessionCreateLocationOptionDto
                {
                    Id = location.Id,
                    FullName = location.FullName,
                    City = location.City,
                    Country = location.Country,
                    TimeZoneId = location.Timezone
                })
                .ToList(),
            Rooms = rooms,
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
    }
}
