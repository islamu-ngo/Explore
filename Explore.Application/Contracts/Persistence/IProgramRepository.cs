using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IProgramRepository : IGenericRepository<Program, Guid>
    {
        Task<Program> GetProgramWithDetails(Guid id);
        Task<List<Program>> GetProgramsWithDetails();
        Task<List<Program>> GetByOrganization(Guid organizationId);
    }
}
