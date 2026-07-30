// ABOUTME: Handler for soft-deleting an EventDay by Id.
// ABOUTME: Follows the pattern where delete returns BaseCommandResponse<Guid>.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventDays.Handlers.Commands;

public class DeleteEventDayCommandHandler : IRequestHandler<DeleteEventDayCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventTicketCatalogRepository _catalogs;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEventDayCommandHandler(
        IEventDayRepository eventDayRepository,
        IEventTicketCatalogRepository catalogs,
        IUnitOfWork unitOfWork)
    {
        _eventDayRepository = eventDayRepository;
        _catalogs = catalogs;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventDayCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var eventDay = await _eventDayRepository.GetById(request.Id);
        if (eventDay == null)
        {
            response.Success = false;
            response.FailureCode = "event_day_not_found";
            response.Message = "Event day not found.";
            return response;
        }

        DeleteResult deleteResult = await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _catalogs.GetDraftCatalogForUpdateAsync(eventDay.EventId, eventDay.TenantId, token);
            EventTicketCatalogVersion? published = await _catalogs.GetPublishedForUpdateAsync(
                eventDay.EventId,
                eventDay.TenantId,
                token);
            EventDay? lockedDay = await _eventDayRepository.GetByIdForEventForUpdateAsync(
                eventDay.Id,
                eventDay.EventId,
                eventDay.TenantId,
                token);
            if (lockedDay is null)
            {
                return DeleteResult.NotFound;
            }

            if (ReferencesEventDay(published, lockedDay.Id))
            {
                return DeleteResult.Referenced;
            }

            await _eventDayRepository.Delete(lockedDay);
            return DeleteResult.Deleted;
        }, cancellationToken);

        if (deleteResult != DeleteResult.Deleted)
        {
            response.Success = false;
            response.FailureCode = deleteResult == DeleteResult.Referenced
                ? "event_day_ticket_entitlement_conflict"
                : "event_day_not_found";
            response.Message = deleteResult == DeleteResult.Referenced
                ? "Event day is referenced by a published ticket catalog."
                : "Event day not found.";
            return response;
        }

        response.Success = true;
        response.Id = eventDay.Id;
        response.Message = "Event day deleted successfully.";

        return response;
    }

    private static bool ReferencesEventDay(EventTicketCatalogVersion? catalog, Guid eventDayId) =>
        catalog?.TicketTypes
            .Where(ticketType => !ticketType.IsDeleted)
            .SelectMany(ticketType => ticketType.Entitlements)
            .Any(entitlement => entitlement.EventDayId == eventDayId) == true;

    private enum DeleteResult
    {
        Deleted,
        NotFound,
        Referenced
    }
}
