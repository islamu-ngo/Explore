// ABOUTME: Defines route-owned request bodies for registration-order checkout selections.
// ABOUTME: Keeps event identity authoritative in the API route rather than caller-controlled JSON.

using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Domain.Enums;

namespace Explore.API.Models;

public sealed class StartRegistrationOrderRequest
{
    public Guid TicketCatalogVersionId { get; init; }

    public BookingPartyTypeEnum BookingPartyType { get; init; }

    public IReadOnlyList<RegistrationOrderLineSelection> Lines { get; init; } = [];

    public int? PlatformContributionBasisPoints { get; init; }
}

public sealed class ContinueRegistrationOrderRequest
{
    public int? PlatformContributionBasisPoints { get; init; }
}
