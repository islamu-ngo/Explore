// ABOUTME: Enforces collection-level invariants for typed public event actions.
// ABOUTME: Allows actionless events while rejecting ambiguous multiple-primary call-to-action state.

namespace Explore.Domain.Services.Registration;

public static class EventPublicActionRules
{
    public static void EnsureValid(IEnumerable<EventPublicAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        if (actions.Count(action => action.IsPrimary && !action.IsDeleted) > 1)
        {
            throw new InvalidOperationException("An event cannot have more than one primary public action.");
        }
    }
}
