// ABOUTME: Handles adding a ticket type to an event ticket catalog draft.
// ABOUTME: Resolves scoped authoring inputs before mutating the ticket catalog aggregate.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.DTOs.EventTicketing.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventTicketing.Handlers.Commands;

public sealed class CreateEventTicketTypeCommandHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    TicketTypeEntitlementResolver entitlementResolver,
    ITenantContext tenant,
    IUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<CreateEventTicketTypeCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateEventTicketTypeCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await new ManageEventTicketTypeDtoValidator()
            .ValidateAsync(request.TicketType, cancellationToken);
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
            Guid stableTicketTypeId = Guid.CreateVersion7();
            Guid? ticketTypeId = await unitOfWork.ExecuteInTransactionAsync<Guid?>(async token =>
            {
                EventTicketCatalogVersion? catalog = await catalogs.GetDraftCatalogForUpdateAsync(
                    request.EventId,
                    tenant.TenantId,
                    token);
                if (catalog is null)
                {
                    return null;
                }

                if (catalog.TicketTypes.Any(ticketType => ticketType.Id == stableTicketTypeId))
                {
                    return stableTicketTypeId;
                }

                EventCapacityPool? pool = request.TicketType.CapacityPoolId.HasValue
                    ? await catalogs.GetActiveCapacityPoolForUpdateAsync(
                        request.TicketType.CapacityPoolId.Value,
                        request.EventId,
                        tenant.TenantId,
                        token)
                    : null;
                if (request.TicketType.CapacityPoolId.HasValue && pool is null)
                {
                    return null;
                }

                EventTicketType ticketType = CreateTicketType(stableTicketTypeId, catalog, request.TicketType);
                IReadOnlyList<TicketTypeEntitlement> entitlements = await entitlementResolver.ResolveAsync(
                    ticketType.Id,
                    request.TicketType.Entitlements,
                    request.EventId,
                    token);

                catalog.AddTicketType(ticketType, pool);
                foreach (TicketTypeEntitlement entitlement in entitlements)
                {
                    catalog.AddEntitlement(ticketType, entitlement);
                }

                await catalogs.UpdateAsync(catalog, token);
                return ticketType.Id;
            }, cancellationToken);

            if (ticketTypeId is null)
            {
                return Missing(request.EventId);
            }

            await cache.RemoveAsync($"event:detail:{request.EventId}", cancellationToken);
            return Ok(ticketTypeId.Value, "Ticket type created.");
        }
        catch (TicketingNotFoundException)
        {
            return Missing(request.EventId);
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

    private static EventTicketType CreateTicketType(
        Guid ticketTypeId,
        EventTicketCatalogVersion catalog,
        ManageEventTicketTypeDto dto) =>
        EventTicketType.Create(
            ticketTypeId,
            catalog.TenantId,
            catalog.Id,
            dto.Name,
            catalog.CurrencyCode,
            (TicketPricingModeEnum)dto.TicketPricingModeId,
            dto.FixedPriceMinor,
            dto.MinimumPriceMinor,
            dto.SuggestedPriceMinor,
            (ParticipantDataCollectionModeEnum)dto.ParticipantDataCollectionModeId,
            dto.CapacityPoolId,
            dto.MinimumAge,
            dto.MaximumAge,
            dto.RequiresGuardian,
            dto.RequiresApproval,
            dto.PerOrderLimit,
            dto.PerAccountLimit,
            dto.PerVerifiedContactLimit,
            dto.PerBookingPartyLimit);

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
