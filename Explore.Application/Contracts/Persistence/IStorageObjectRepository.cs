using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IStorageObjectRepository : IGenericRepository<StorageObject, Guid>
    {
        Task<StorageObject?> GetFileWithDetails(Guid id);
        Task<List<StorageObject>> GetFilesWithDetails();
        Task<(List<StorageObject> Items, int TotalCount)> GetFilesWithDetailsPaged(int pageNumber, int pageSize);
    }
}
