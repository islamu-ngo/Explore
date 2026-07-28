// ABOUTME: Enum mirror for stable ParticipantDataCollectionMode lookup identities.
// ABOUTME: Expresses ticket-level participant detail collection without workflow coupling.

namespace Explore.Domain.Enums;

public enum ParticipantDataCollectionModeEnum
{
    None = 1,
    LeadBookerOnly = 2,
    PerTicketOptional = 3,
    PerTicketRequired = 4,
    DeferredAssignment = 5
}
