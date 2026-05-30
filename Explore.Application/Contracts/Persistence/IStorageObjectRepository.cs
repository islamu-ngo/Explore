// ABOUTME: Repository contract for storage object metadata queries.
// ABOUTME: Returns domain entities with explicit detail-loading methods for handler-side DTO mapping.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IStorageObjectRepository : IGenericRepository<StorageObject, Guid>
{
    Task<StorageObject?> GetFileWithDetails(Guid id);
    Task<List<StorageObject>> GetFilesWithDetails();
    Task<(List<StorageObject> Items, int TotalCount)> GetFilesWithDetailsPaged(int pageNumber, int pageSize);
    Task<IReadOnlyList<StorageObject>> GetAllForInstanceStorageReportAsync(CancellationToken cancellationToken);
}
