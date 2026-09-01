// ABOUTME: Repository contract for instance bootstrap state used by first-run onboarding flow.
// ABOUTME: Exposes current bootstrap state lookup for startup gating and completion writes.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IInstanceBootstrapStateRepository : IGenericRepository<InstanceBootstrapState, Guid>
{
    Task<InstanceBootstrapState?> GetCurrent(CancellationToken cancellationToken = default);
    Task<InstanceBootstrapState?> GetCurrentForUpdate(CancellationToken cancellationToken = default);
}
