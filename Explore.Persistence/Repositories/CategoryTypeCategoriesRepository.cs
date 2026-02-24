using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class CategoryTypeCategoriesRepository : GenericRepository<CategoryTypeCategories, Guid>, ICategoryTypeCategoriesRepository
{
    private readonly ExploreDbContext _dbContext;

    public CategoryTypeCategoriesRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Category>> GetCategoriesByCategoryType(int categoryTypeId)
    {
        return await _dbContext.CategoryTypeCategories
            .AsNoTracking()
            .Include(ct => ct.Category)
            .Where(ct => ct.CategoryTypeId == categoryTypeId)
            .Select(ct => ct.Category)
            .ToListAsync();
    }

    public async Task<List<CategoryType>> GetCategoryTypesForCategory(Guid categoryId)
    {
        return await _dbContext.CategoryTypeCategories
            .AsNoTracking()
            .Include(ct => ct.CategoryType)
            .Where(ct => ct.CategoryId == categoryId)
            .Select(ct => ct.CategoryType)
            .ToListAsync();
    }

    public async Task<bool> Exists(Guid categoryId, int categoryTypeId)
    {
        return await _dbContext.CategoryTypeCategories
            .AsNoTracking()
            .AnyAsync(ct => ct.CategoryId == categoryId && ct.CategoryTypeId == categoryTypeId);
    }

    public async Task<List<(CategoryType CategoryType, List<Category> Categories)>> GetAllCategoriesGroupedByCategoryType()
    {
        var allEntries = await _dbContext.CategoryTypeCategories
            .AsNoTracking()
            .Include(ct => ct.CategoryType)
            .Include(ct => ct.Category)
            .OrderBy(ct => ct.CategoryType.FullName)
            .ThenBy(ct => ct.Category.FullName)
            .ToListAsync();

        return allEntries
            .GroupBy(ct => ct.CategoryTypeId)
            .Select(g =>
            {
                var first = g.First();
                return (first.CategoryType, g.Select(ct => ct.Category).ToList());
            })
            .ToList();
    }
}
