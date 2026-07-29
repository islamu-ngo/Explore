// ABOUTME: Handles removal of a ticket type from an event ticket catalog draft.
// ABOUTME: Applies the aggregate deletion transition with audited actor and time data.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTicketing.Handlers.Commands;

public sealed class DeleteEventTicketTypeCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<DeleteEventTicketTypeCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        DeleteEventTicketTypeCommand request,
        CancellationToken cancellationToken)
    {
        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (!IsPlatformManaged(eventTarget, tenant.TenantId))
        {
            return Missing(request.TicketTypeId);
        }

        if (currentUser.UserId is not Guid userId)
        {
            return Bad(request.TicketTypeId, "An authenticated user is required.");
        }

        try
        {
            Guid? ticketTypeId = await unitOfWork.ExecuteInTransactionAsync<Guid?>(async token =>
            {
                EventTicketCatalogVersion? catalog = await catalogs.GetDraftCatalogForUpdateAsync(
                    request.EventId,
                    tenant.TenantId,
                    token);
                EventTicketType? ticketType = catalog?.TicketTypes.SingleOrDefault(
                    candidate => candidate.Id == request.TicketTypeId && !candidate.IsDeleted);
                if (ticketType is null)
                {
                    return null;
                }

                catalog!.DeleteTicketType(ticketType, timeProvider.GetUtcNow().UtcDateTime, userId);
                await catalogs.UpdateAsync(catalog, token);
                return ticketType.Id;
            }, cancellationToken);

            if (ticketTypeId is null)
            {
                return Missing(request.TicketTypeId);
            }

            await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
            return Ok(ticketTypeId.Value, "Ticket type deleted.");
        }
        catch (ArgumentException exception)
        {
            return Bad(request.TicketTypeId, exception.Message);
        }
    }

    private static bool IsPlatformManaged(Event? eventTarget, Guid tenantId) =>
        eventTarget?.TenantId == tenantId
        && eventTarget.ParticipationConfiguration?.ParticipationHandlingModeId
            == (int)ParticipationHandlingModeEnum.PlatformManaged;

    private static BaseCommandResponse<Guid> Ok(Guid id, string message) => new()
    {
        Id = id,
        Success = true,
        Message = message
    };

    private static BaseCommandResponse<Guid> Missing(Guid id) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "event_ticketing_not_found",
        Message = "Ticketing configuration was not found.",
        Errors = ["Ticketing configuration was not found."]
    };

    private static BaseCommandResponse<Guid> Bad(Guid id, string error) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "event_ticketing_validation_failed",
        Message = "Ticketing configuration is invalid.",
        Errors = [error]
    };
}
