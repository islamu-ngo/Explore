using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Program;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IProgramRepository : IGenericRepository<Program, Guid>
    {
        Task<ProgramDto> GetProgramWithDetails(Guid id);
        Task<List<ProgramListDto>> GetProgramsWithDetails();
        Task<List<ProgramListDto>> GetByOrganization(Guid organizationId);
    }
}
