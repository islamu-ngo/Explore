// ABOUTME: Handles creating an event-scoped capacity pool for ticket authoring.
// ABOUTME: Validates platform ownership before persisting and invalidating the event cache.

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

public sealed class CreateEventCapacityPoolCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant,
    HybridCache cache) : IRequestHandler<CreateEventCapacityPoolCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateEventCapacityPoolCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new ManageEventCapacityPoolDtoValidator()
            .ValidateAsync(request.CapacityPool, cancellationToken);
        if (!validation.IsValid)
        {
            return Bad(request.EventId, validation.Errors.Select(error => error.ErrorMessage));
        }

        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (!IsPlatformManaged(eventTarget, tenant.TenantId))
        {
            return Missing(request.EventId);
        }

        try
        {
            EventCapacityPool pool = EventCapacityPool.Create(
                tenant.TenantId,
                request.EventId,
                request.CapacityPool.Name,
                request.CapacityPool.MaximumQuantity,
                request.CapacityPool.HoldDurationSeconds,
                (CapacityOversellPolicyEnum)request.CapacityPool.CapacityOversellPolicyId,
                request.CapacityPool.IsActive);
            await catalogs.AddCapacityPoolAsync(pool, cancellationToken);
            await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
            return Ok(pool.Id, "Capacity pool created.");
        }
        catch (ArgumentException exception)
        {
            return Bad(request.EventId, exception.Message);
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
