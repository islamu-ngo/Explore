// ABOUTME: Pure domain rules mapping EventRegistrationPolicy (organizer choice) to the set of allowed RegistrationScope values.
// ABOUTME: Single source of truth consumed by validators and handlers so policy enforcement never drifts.

using Explore.Domain.Enums;

namespace Explore.Domain.Services.Registration;

public static class RegistrationPolicyRules
{
    /// <summary>
    /// Returns true when the supplied registration scope is permitted by the supplied organizer policy.
    /// A null policy id is treated as <see cref="EventRegistrationPolicyEnum.Flexible"/> so events that were
    /// created before the policy field landed still accept every scope during rollout.
    /// </summary>
    public static bool IsScopeAllowed(int? policyId, int scopeId)
    {
        var policy = policyId.HasValue
            ? (EventRegistrationPolicyEnum)policyId.Value
            : EventRegistrationPolicyEnum.Flexible;

        var scope = (RegistrationScopeEnum)scopeId;

        return policy switch
        {
            EventRegistrationPolicyEnum.WholeEventOnly => scope == RegistrationScopeEnum.Event,
            EventRegistrationPolicyEnum.WholeDayOnly => scope == RegistrationScopeEnum.Day,
            EventRegistrationPolicyEnum.SessionSelectionOnly => scope == RegistrationScopeEnum.SessionSelection,
            EventRegistrationPolicyEnum.WholeEventOrDay => scope is RegistrationScopeEnum.Event or RegistrationScopeEnum.Day,
            EventRegistrationPolicyEnum.WholeEventOrSession => scope is RegistrationScopeEnum.Event or RegistrationScopeEnum.SessionSelection,
            EventRegistrationPolicyEnum.Flexible => true,
            _ => false
        };
    }
}
