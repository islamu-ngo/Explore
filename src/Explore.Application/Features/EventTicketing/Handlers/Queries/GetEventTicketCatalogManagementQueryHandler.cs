// ABOUTME: Handles ticket catalog management reads for an event.
// ABOUTME: Returns an explicit empty management resource for valid platform-managed events without a catalog.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Features.EventTicketing.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventTicketing.Handlers.Queries;

public sealed class GetEventTicketCatalogManagementQueryHandler(
    IEventRepository events,
    IEventTicketCatalogRepository catalogs,
    ITenantContext tenant) : IRequestHandler<GetEventTicketCatalogManagementQuery, EventTicketCatalogManagementDto?>
{
    public async Task<EventTicketCatalogManagementDto?> Handle(
        GetEventTicketCatalogManagementQuery request,
        CancellationToken cancellationToken)
    {
        Event? eventTarget = await events.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (!IsPlatformManaged(eventTarget, tenant.TenantId))
        {
            return null;
        }

        EventTicketCatalogVersion? catalog =
            await catalogs.GetManagementCatalogAsync(request.EventId, tenant.TenantId, cancellationToken)
            ?? await catalogs.GetPublishedCatalogAsync(request.EventId, tenant.TenantId, cancellationToken);

        if (catalog is null)
        {
            return new EventTicketCatalogManagementDto { EventId = request.EventId };
        }

        Event? eventWithDetails = await events.GetEventWithDetails(request.EventId);
        EventCapacityPool[] pools = eventWithDetails?.CapacityPools.Where(pool => !pool.IsDeleted).ToArray() ?? [];
        return Map(catalog, pools);
    }

    private static bool IsPlatformManaged(Event? eventTarget, Guid tenantId) =>
        eventTarget?.TenantId == tenantId
        && eventTarget.ParticipationConfiguration?.ParticipationHandlingModeId == (int)ParticipationHandlingModeEnum.PlatformManaged;

    private static EventTicketCatalogManagementDto Map(
        EventTicketCatalogVersion catalog,
        IReadOnlyList<EventCapacityPool> pools) =>
        new()
        {
            EventId = catalog.EventId,
            CatalogId = catalog.Id,
            VersionNumber = catalog.VersionNumber,
            CurrencyCode = catalog.CurrencyCode,
            StatusId = catalog.TicketCatalogStatusId,
            TicketTypes = catalog.TicketTypes
                .Where(ticketType => !ticketType.IsDeleted)
                .Select(Map)
                .ToArray(),
            CapacityPools = pools.Select(Map).ToArray()
        };

    private static EventTicketTypeDto Map(EventTicketType ticketType) => new()
    {
        Id = ticketType.Id,
        Name = ticketType.Name,
        TicketPricingModeId = ticketType.TicketPricingModeId,
        FixedPriceMinor = ticketType.FixedPriceMinor,
        MinimumPriceMinor = ticketType.MinimumPriceMinor,
        SuggestedPriceMinor = ticketType.SuggestedPriceMinor,
        ParticipantDataCollectionModeId = ticketType.ParticipantDataCollectionModeId,
        CapacityPoolId = ticketType.CapacityPoolId,
        MinimumAge = ticketType.MinimumAge,
        MaximumAge = ticketType.MaximumAge,
        RequiresGuardian = ticketType.RequiresGuardian,
        RequiresApproval = ticketType.RequiresApproval,
        PerOrderLimit = ticketType.PerOrderLimit,
        PerAccountLimit = ticketType.PerAccountLimit,
        PerVerifiedContactLimit = ticketType.PerVerifiedContactLimit,
        PerBookingPartyLimit = ticketType.PerBookingPartyLimit,
        Entitlements = ticketType.Entitlements.Select(entitlement => new TicketTypeEntitlementDto
        {
            EntitlementScopeTypeId = entitlement.EntitlementScopeTypeId,
            EventDayId = entitlement.EventDayId,
            EventSessionId = entitlement.EventSessionId,
            IncludedQuantity = entitlement.IncludedQuantity,
            EntitlementSelectionRuleId = entitlement.EntitlementSelectionRuleId
        }).ToArray()
    };

    private static EventCapacityPoolDto Map(EventCapacityPool pool) => new()
    {
        Id = pool.Id,
        Name = pool.Name,
        MaximumQuantity = pool.MaximumQuantity,
        HoldDurationSeconds = pool.HoldDurationSeconds,
        CapacityOversellPolicyId = pool.CapacityOversellPolicyId,
        IsActive = pool.IsActive
    };
}
