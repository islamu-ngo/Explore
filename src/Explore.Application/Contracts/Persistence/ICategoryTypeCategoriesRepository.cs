using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ICategoryTypeCategoriesRepository : IGenericRepository<CategoryTypeCategories, Guid>
{
    Task<List<Category>> GetCategoriesByCategoryType(int categoryTypeId);
    Task<List<CategoryType>> GetCategoryTypesForCategory(Guid categoryId);
    Task<bool> Exists(Guid categoryId, int categoryTypeId);
    Task<List<(CategoryType CategoryType, List<Category> Categories)>> GetAllCategoriesGroupedByCategoryType();
}
