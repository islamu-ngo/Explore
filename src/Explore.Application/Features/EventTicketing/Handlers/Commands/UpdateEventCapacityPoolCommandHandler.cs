// ABOUTME: Handles updating an event-scoped capacity pool for ticket authoring.
// ABOUTME: Resolves the pool within the platform-managed event before its domain transition.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing.Validators;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTicketing.Handlers.Commands;

public sealed class UpdateEventCapacityPoolCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant,
    HybridCache cache) : IRequestHandler<UpdateEventCapacityPoolCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateEventCapacityPoolCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new ManageEventCapacityPoolDtoValidator()
            .ValidateAsync(request.CapacityPool, cancellationToken);
        if (!validation.IsValid)
        {
            return Bad(request.CapacityPoolId, validation.Errors.Select(error => error.ErrorMessage));
        }

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

        try
        {
            pool.Update(
                request.CapacityPool.Name,
                request.CapacityPool.MaximumQuantity,
                request.CapacityPool.HoldDurationSeconds,
                (CapacityOversellPolicyEnum)request.CapacityPool.CapacityOversellPolicyId,
                request.CapacityPool.IsActive);
            await catalogs.UpdateCapacityPoolAsync(pool, cancellationToken);
            await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
            return Ok(pool.Id, "Capacity pool updated.");
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

    private static BaseCommandResponse<Guid> Bad(Guid id, string error) => Bad(id, [error]);

    private static BaseCommandResponse<Guid> Bad(Guid id, IEnumerable<string> errors) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "event_ticketing_validation_failed",
        Message = "Ticketing configuration is invalid.",
        Errors = errors.ToList()
    };
}
