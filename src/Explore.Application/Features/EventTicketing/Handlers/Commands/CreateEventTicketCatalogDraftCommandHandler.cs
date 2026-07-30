// ABOUTME: Handles creation of an event ticket catalog draft.
// ABOUTME: Validates event authority and catalog uniqueness before persisting the draft.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventTicketing.Handlers.Commands;

public sealed class CreateEventTicketCatalogDraftCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant) : IRequestHandler<CreateEventTicketCatalogDraftCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateEventTicketCatalogDraftCommand request,
        CancellationToken cancellationToken)
    {
        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (!IsPlatformManaged(eventTarget, tenant.TenantId)
            || await catalogs.GetManagementCatalogAsync(request.EventId, tenant.TenantId, cancellationToken) is not null)
        {
            return Missing(request.EventId);
        }

        try
        {
            EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(
                tenant.TenantId,
                request.EventId,
                request.CurrencyCode,
                1);
            await catalogs.AddAsync(catalog, cancellationToken);
            return Ok(catalog.Id, "Ticket catalog draft created.");
        }
        catch (ArgumentException exception)
        {
            return Bad(request.EventId, exception.Message);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(request.EventId, exception.Message);
        }
    }

    private static bool IsPlatformManaged(Event? eventTarget, Guid tenantId) =>
        eventTarget?.TenantId == tenantId
        && eventTarget.ParticipationConfiguration?.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.PlatformManaged;

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

    private static BaseCommandResponse<Guid> Conflict(Guid id, string error) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "event_ticketing_concurrency_conflict",
        Message = "Ticketing configuration was updated by another request.",
        Errors = [error]
    };
}
