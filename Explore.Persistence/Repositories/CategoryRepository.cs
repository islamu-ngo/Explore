using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class CategoryRepository : GenericRepository<Category, Guid>, ICategoryRepository
    {
        private readonly ExploreDbContext _dbContext;

        public CategoryRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Category> GetCategoryWithDetails(Guid id)
        {
            return await _dbContext.Categories
                .Include(c => c.Parent)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Category>> GetCategoriesWithDetails()
        {
            return await _dbContext.Categories
                .Include(c => c.Parent)
                .Include(c => c.Tenant)
                .ToListAsync();
        }
    }
}
