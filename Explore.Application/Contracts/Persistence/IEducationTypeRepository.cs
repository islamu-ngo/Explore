using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEducationTypeRepository : IGenericRepository<EducationType, int>
    {
        Task<EducationType> GetEducationTypeWithDetails(int id);
        Task<List<EducationType>> GetEducationTypesWithDetails();
    }
}
