// ABOUTME: Repository contract for tenant-scoped custom-property projection rebuild status rows.
// ABOUTME: Used by the projection rebuild worker and operator admin endpoints to coordinate and observe rebuilds.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public interface ICustomPropertyProjectionStatusRepository
{
    Task<CustomPropertyProjectionStatus?> GetAsync(
        string projectionName,
        int projectionVersion,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomPropertyProjectionStatus>> GetAllForProjectionAsync(
        string projectionName,
        int projectionVersion,
        CancellationToken cancellationToken);

    Task<CustomPropertyProjectionStatus> UpsertAsync(
        CustomPropertyProjectionStatus row,
        CancellationToken cancellationToken);

    Task MarkStateAsync(
        string projectionName,
        int projectionVersion,
        Guid tenantId,
        CustomPropertyProjectionState state,
        string? errorMessage,
        CancellationToken cancellationToken);
}
