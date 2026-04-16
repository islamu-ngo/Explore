// ABOUTME: Repository contract for EventRegistrationPolicy lookup table.
// ABOUTME: Provides existence check used by event validators when RegistrationPolicyId is supplied.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRegistrationPolicyRepository : IGenericRepository<EventRegistrationPolicy, int>
{
}
