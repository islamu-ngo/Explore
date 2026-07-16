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

        List<EventSession> sessions = await eventSessionRepository.GetPublicSessionsByEventAsync(
            request.EventId,
            cancellationToken);
        EventSession? primarySession = sessions
            .OrderBy(session => session.StartTime)
            .FirstOrDefault();

        if (primarySession is null)
        {
            return null;
        }

        if (primarySession.StartTime is null || primarySession.EndTime is null)
        {
            return null;
        }

        return new EventCalendarExportDto(
            entity.Id,
            entity.Title,
            entity.Content ?? entity.Description,
            entity.Slug,
            primarySession.StartTime.Value.ToUniversalTime(),
            primarySession.EndTime.Value.ToUniversalTime(),
            null);
    }

    private static bool IsPublicCalendarEligible(Event entity)
    {
        return entity.EventStatusId == (int)EventStatusEnum.Published &&
            entity.VisibilityTypeId == (int)VisibilityTypeEnum.Public;
    }

}
