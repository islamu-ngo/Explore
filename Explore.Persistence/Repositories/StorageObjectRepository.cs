using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class StorageObjectRepository : GenericRepository<StorageObject, Guid>, IStorageObjectRepository
    {
        private readonly ExploreDbContext _dbContext;
        public StorageObjectRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<StorageObject>> GetFilesWithDetails()
        {
            var files = await _dbContext.Files
                .ToListAsync();
            return files;
        }

        public async Task<StorageObject> GetFileWithDetails(Guid id)
        {
            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id);
            return file;
        }
    }
}
