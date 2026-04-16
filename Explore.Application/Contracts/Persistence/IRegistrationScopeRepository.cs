// ABOUTME: Repository contract for RegistrationScope lookup table.
// ABOUTME: Provides lookup access for registration scope options (Event, Day, SessionSelection).

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IRegistrationScopeRepository : IGenericRepository<RegistrationScope, int>
{
}
