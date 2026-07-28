// ABOUTME: Management read model for an event ticket catalog.
// ABOUTME: Contains catalog, ticket type, and capacity pool read projections.
namespace Explore.Application.DTOs.EventTicketing;

public sealed class EventTicketCatalogManagementDto
{
    public Guid EventId { get; init; }
    public Guid? CatalogId { get; init; }
    public int? VersionNumber { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int? StatusId { get; init; }
    public IReadOnlyList<EventTicketTypeDto> TicketTypes { get; init; } = [];
    public IReadOnlyList<EventCapacityPoolDto> CapacityPools { get; init; } = [];
}
