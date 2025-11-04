using System;
using System.Collections.Generic;
using System.Text;
using StorageObject = Explore.Domain.StorageObject;

namespace Explore.Application.Contracts.Persistence
{
    public interface IStorageObjectRepository : IGenericRepository<StorageObject, Guid>
    {
        Task<StorageObject> GetFileWithDetails(Guid id);
        Task<List<StorageObject>> GetFilesWithDetails();
    }
}
