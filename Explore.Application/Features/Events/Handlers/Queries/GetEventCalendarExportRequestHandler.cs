// ABOUTME: Builds the public calendar export read model from event and primary-session data.
// ABOUTME: Enforces published/public visibility before the API serializes the .ics file.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries;

public sealed class GetEventCalendarExportRequestHandler(
    IEventRepository eventRepository,
    IEventSessionRepository eventSessionRepository)
    : IRequestHandler<GetEventCalendarExportRequest, EventCalendarExportDto?>
{
    public async Task<EventCalendarExportDto?> Handle(
        GetEventCalendarExportRequest request,
        CancellationToken cancellationToken)
    {
        Event? entity = await eventRepository.GetEventWithDetails(request.EventId);
        if (entity is null || !IsPublicCalendarEligible(entity))
        {
            return null;
        }

        List<EventSession> sessions = await eventSessionRepository.GetSessionsByEvent(request.EventId);
        EventSession? primarySession = sessions
            .OrderBy(session => session.StartTime)
            .FirstOrDefault();

        if (primarySession is null)
        {
            return null;
        }

        return new EventCalendarExportDto(
            entity.Id,
            entity.Title,
            entity.Content ?? entity.Description,
            entity.Slug,
            primarySession.StartTime.ToUniversalTime(),
            primarySession.EndTime.ToUniversalTime(),
            BuildLocation(primarySession));
    }

    private static bool IsPublicCalendarEligible(Event entity)
    {
        return entity.EventStatusId == (int)EventStatusEnum.Published &&
            entity.VisibilityTypeId == (int)VisibilityTypeEnum.Public;
    }

    private static string? BuildLocation(EventSession session)
    {
        string?[] parts =
        [
            session.Location?.FullName,
            session.Room?.Name,
            session.Location?.City,
            session.Location?.Country
        ];

        string location = string.Join(
            ", ",
            parts.Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(location) ? null : location;
    }
}
