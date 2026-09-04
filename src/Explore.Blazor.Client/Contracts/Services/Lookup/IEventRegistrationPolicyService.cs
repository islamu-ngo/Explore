// ABOUTME: Contract for EventRegistrationPolicy lookup operations (read-only).
// ABOUTME: Wraps the NSwag-generated event-registration-policy lookup client.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IEventRegistrationPolicyService
{
    Task<ICollection<EventRegistrationPolicyListDto>> GetEventRegistrationPoliciesAsync();
}
