// ABOUTME: Ticket entitlement projection for event, day, and session targets.
// ABOUTME: Carries target IDs and selection semantics used by ticket authoring.
namespace Explore.Application.DTOs.EventTicketing;

public sealed class TicketTypeEntitlementDto
{
    public int EntitlementScopeTypeId { get; init; }
    public Guid? EventDayId { get; init; }
    public Guid? EventSessionId { get; init; }
    public int IncludedQuantity { get; init; }
    public int EntitlementSelectionRuleId { get; init; }
}
