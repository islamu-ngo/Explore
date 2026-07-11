// ABOUTME: Scope discriminator for custom-property projection dirty-scope backlog rows.
// ABOUTME: Identifies whether a pending drain request targets an Event or EventSession parent aggregate.

namespace Explore.Domain.Enums;

public enum CustomPropertyProjectionScopeType
{
    Event = 0,
    EventSession = 1,
}
