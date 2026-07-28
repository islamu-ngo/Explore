// ABOUTME: Write model for a ticket type entitlement selection.
// ABOUTME: Contains only caller-supplied scope references and quantity semantics.
namespace Explore.Application.DTOs.EventTicketing;

public sealed class ManageTicketTypeEntitlementDto
{
    public int EntitlementScopeTypeId { get; init; }
    public Guid? EventDayId { get; init; }
    public Guid? EventSessionId { get; init; }
    public int IncludedQuantity { get; init; }
    public int EntitlementSelectionRuleId { get; init; }
}
