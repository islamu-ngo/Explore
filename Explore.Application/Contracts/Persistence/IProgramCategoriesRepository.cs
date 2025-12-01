using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IProgramCategoriesRepository : IGenericRepository<ProgramCategories, Guid>
    {
        Task<List<Program>> GetProgramsByCategory(Guid categoryId);
        Task<List<Category>> GetCategoriesByProgram(Guid programId);
        Task<bool> Exists(Guid programId, Guid categoryId);
    }
}
