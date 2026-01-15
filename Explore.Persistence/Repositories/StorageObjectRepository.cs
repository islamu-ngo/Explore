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
            return await _dbContext.StorageObjects
                .Include(f => f.FileType)
                .Include(f => f.Tenant)
                .ToListAsync();
        }

        public async Task<StorageObject?> GetFileWithDetails(Guid id)
        {
            return await _dbContext.StorageObjects
                .Include(f => f.FileType)
                .Include(f => f.Tenant)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<(List<StorageObject> Items, int TotalCount)> GetFilesWithDetailsPaged(int pageNumber, int pageSize)
        {
            var query = _dbContext.StorageObjects
                .AsNoTracking()
                .Include(f => f.FileType)
                .OrderByDescending(f => f.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
