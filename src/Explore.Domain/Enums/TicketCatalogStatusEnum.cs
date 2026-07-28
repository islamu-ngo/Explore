// ABOUTME: Enum mirror for stable TicketCatalogStatus lookup identities.
// ABOUTME: Supports domain lifecycle rules without persisting enum values directly.

namespace Explore.Domain.Enums;

public enum TicketCatalogStatusEnum
{
    Draft = 1,
    Published = 2,
    Retired = 3
}
