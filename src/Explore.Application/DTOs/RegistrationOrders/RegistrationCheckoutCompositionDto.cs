// ABOUTME: Public checkout composition for selecting tickets from one published event catalog.
// ABOUTME: Exposes only buyer-facing pricing and limit fields required to create an order.

namespace Explore.Application.DTOs.RegistrationOrders;

public sealed class RegistrationCheckoutCompositionDto
{
    public Guid EventId { get; init; }
    public Guid TicketCatalogVersionId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public IReadOnlyList<RegistrationCheckoutTicketTypeDto> TicketTypes { get; init; } = [];
}

public sealed class RegistrationCheckoutTicketTypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int TicketPricingModeId { get; init; }
    public string? TicketPricingModeCode { get; init; }
    public long? FixedPriceMinor { get; init; }
    public long? MinimumPriceMinor { get; init; }
    public long? SuggestedPriceMinor { get; init; }
    public int? PerOrderLimit { get; init; }
    public IReadOnlyList<RegistrationCheckoutSlidingScaleOptionDto> SlidingScaleOptions { get; init; } = [];
}

public sealed class RegistrationCheckoutSlidingScaleOptionDto
{
    public long BuyerPriceMinor { get; init; }
    public long OrganizerEarningsMinor { get; init; }
}
