// ABOUTME: Publishes a validated draft ticket catalog for a platform-managed event.
// ABOUTME: Maps ticketing failures and invalidates the event detail cache after successful publication.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Responses;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTicketing.Handlers.Commands;

public sealed class PublishEventTicketCatalogCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant,
    IUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<PublishEventTicketCatalogCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        PublishEventTicketCatalogCommand request,
        CancellationToken cancellationToken)
    {
        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (!IsPlatformManaged(eventTarget, tenant.TenantId))
        {
            return Missing(request.EventId);
        }

        Guid failureId = request.EventId;
        Guid? catalogId;
        try
        {
            catalogId = await unitOfWork.ExecuteInTransactionAsync<Guid?>(async token =>
            {
                EventTicketCatalogVersion? draft = await catalogs.GetDraftForUpdateAsync(
                    request.EventId,
                    tenant.TenantId,
                    token);
                if (draft is null)
                {
                    return null;
                }

                failureId = draft.Id;
                EventTicketCatalogVersion? currentPublication = await catalogs.GetPublishedForUpdateAsync(
                    request.EventId,
                    tenant.TenantId,
                    token);

                draft.ValidateForPublication();
                currentPublication?.Retire();
                await catalogs.SaveChangesAsync(token);

                draft.Publish();
                await catalogs.SaveChangesAsync(token);
                return draft.Id;
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Bad(failureId, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Bad(failureId, exception.Message);
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(failureId, exception.Message);
        }

        if (catalogId is null)
        {
            return Missing(request.EventId);
        }

        await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
        return Ok(catalogId.Value, "Ticket catalog published.");
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
