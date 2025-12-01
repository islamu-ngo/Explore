using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ICategoryRepository : IGenericRepository<Category, Guid>
    {
        Task<Category> GetCategoryWithDetails(Guid id);
        Task<List<Category>> GetCategoriesWithDetails();
    }
}
