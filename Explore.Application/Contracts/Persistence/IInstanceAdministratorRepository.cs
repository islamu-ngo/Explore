// ABOUTME: Repository contract for instance administrator user assignments.
// ABOUTME: Supports membership checks used by onboarding and instance governance endpoints.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IInstanceAdministratorRepository : IGenericRepository<InstanceAdministrator, Guid>
{
    Task<bool> IsUserInstanceAdmin(Guid userId);
    Task<InstanceAdministrator?> GetByUserId(Guid userId);
    Task<bool> HasAnyInstanceAdministrator();
}
