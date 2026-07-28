// ABOUTME: Handles deletion of an unused event-scoped capacity pool.
// ABOUTME: Guards active ticket references before applying the audited domain transition.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTicketing.Handlers.Commands;

public sealed class DeleteEventCapacityPoolCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    HybridCache cache) : IRequestHandler<DeleteEventCapacityPoolCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        DeleteEventCapacityPoolCommand request,
        CancellationToken cancellationToken)
    {
        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (!IsPlatformManaged(eventTarget, tenant.TenantId))
        {
            return Missing(request.CapacityPoolId);
        }

        EventCapacityPool? pool = await catalogs.GetCapacityPoolByIdEventAndTenantAsync(
            request.CapacityPoolId,
            request.EventId,
            tenant.TenantId,
            cancellationToken);
        if (pool is null)
        {
            return Missing(request.CapacityPoolId);
        }

        EventTicketCatalogVersion? catalog = await catalogs.GetManagementCatalogAsync(
            request.EventId,
            tenant.TenantId,
            cancellationToken);
        if (catalog?.TicketTypes.Any(ticketType => !ticketType.IsDeleted && ticketType.CapacityPoolId == pool.Id) == true)
        {
            return Bad(pool.Id, "Capacity pool is assigned to an active ticket type.");
        }

        if (currentUser.UserId is not Guid userId)
        {
            return Bad(pool.Id, "An authenticated user is required.");
        }

        try
        {
            pool.Delete(timeProvider.GetUtcNow().UtcDateTime, userId);
            await catalogs.UpdateCapacityPoolAsync(pool, cancellationToken);
            await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
            return Ok(pool.Id, "Capacity pool deleted.");
        }
        catch (ArgumentException exception)
        {
            return Bad(pool.Id, exception.Message);
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
