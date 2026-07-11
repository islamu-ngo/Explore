// ABOUTME: Persistence contract for tenant reads, counts, and atomic lifecycle transitions.
// ABOUTME: Keeps tenant lifecycle compare-and-swap operations entity-first and cancellation-aware.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantRepository : IGenericRepository<Tenant, Guid>
{
    Task<Tenant?> GetTenantBySlug(string slug);
    Task<int> GetActiveTenantCountAsync();
    Task<Tenant?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TryTransitionStatusAsync(
        Guid id,
        int expectedStatusId,
        int newStatusId,
        DateTime updatedAt,
        Guid updatedBy,
        CancellationToken cancellationToken = default);
}
