// ABOUTME: Public ticket-catalog-derived event price summary for discovery and event detail responses.
// ABOUTME: Keeps display pricing derived from published selectable ticket types rather than Event fields.

namespace Explore.Application.DTOs.Event;

public sealed record EventTicketPriceSummaryDto
{
    public required string SummaryCode { get; init; }
    public string? CurrencyCode { get; init; }
    public int CurrencyMinorUnitDigits { get; init; }
    public long FromAmountMinor { get; init; }
}
