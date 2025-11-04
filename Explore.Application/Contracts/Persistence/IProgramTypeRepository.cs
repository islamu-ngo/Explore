using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IProgramTypeRepository : IGenericRepository<ProgramType, int>
    {
        Task<ProgramType> GetProgramTypeWithDetails(int id);
        Task<List<ProgramType>> GetProgramTypesWithDetails();
    }
}
