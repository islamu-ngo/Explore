// ABOUTME: Defines internal order-start input for atomic ticket selection and inventory reservation.
// ABOUTME: Keeps purchaser PII outside the hold transaction; a normalized verified contact is used only for limit lookup.

using Explore.Application.Responses;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using MediatR;

namespace Explore.Application.Features.RegistrationOrders.Requests.Commands;

public sealed class CreateRegistrationOrderWithHoldCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid EventId { get; init; }

    public Guid TicketCatalogVersionId { get; init; }

    public Guid? AccountUserId { get; init; }

    public Guid? PurchaserActorId { get; init; }

    public string? VerifiedContactNormalizedEmail { get; init; }

    public BookingPartyTypeEnum BookingPartyType { get; init; }

    public CapabilityTokenHash? GuestAccessTokenHash { get; init; }

    public required IReadOnlyList<RegistrationOrderLineSelection> Lines { get; init; }
}

public sealed record RegistrationOrderLineSelection(
    Guid TicketTypeId,
    int Quantity,
    long? ChosenUnitPriceMinor);
