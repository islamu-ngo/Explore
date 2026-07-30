// ABOUTME: Handles updating an event-scoped capacity pool for ticket authoring.
// ABOUTME: Resolves the pool within the platform-managed event before its domain transition.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing.Validators;
using Explore.Application.Exceptions;
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
    IUnitOfWork unitOfWork,
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

        try
        {
            BaseCommandResponse<Guid> response = await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                EventCapacityPool? pool = await catalogs.GetActiveCapacityPoolForUpdateAsync(
                    request.CapacityPoolId,
                    request.EventId,
                    tenant.TenantId,
                    token);
                if (pool is null)
                {
                    return Missing(request.CapacityPoolId);
                }

                pool.Update(
                    request.CapacityPool.Name,
                    request.CapacityPool.MaximumQuantity,
                    request.CapacityPool.HoldDurationSeconds,
                    (CapacityHoldPolicyEnum)request.CapacityPool.CapacityHoldPolicyId,
                    (CapacityOversellPolicyEnum)request.CapacityPool.CapacityOversellPolicyId,
                    request.CapacityPool.IsActive);
                await catalogs.UpdateCapacityPoolAsync(pool, token);
                return Ok(pool.Id, "Capacity pool updated.");
            }, cancellationToken);

            if (!response.Success)
            {
                return response;
            }

            await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
            return response;
        }
        catch (ConcurrencyConflictException exception)
        {
            return Conflict(request.CapacityPoolId, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Bad(request.CapacityPoolId, exception.Message);
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

    private static BaseCommandResponse<Guid> Conflict(Guid id, string error) => new()
    {
        Id = id,
        Success = false,
        FailureCode = "event_ticketing_concurrency_conflict",
        Message = "Ticketing configuration was updated by another request.",
        Errors = [error]
    };
}
