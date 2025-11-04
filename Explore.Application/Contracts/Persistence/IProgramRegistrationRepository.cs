using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IProgramRegistrationRepository : IGenericRepository<ProgramRegistartion, Guid>
    {
        Task<ProgramRegistartion> GetProgramRegistrationWithDetails(Guid id);
        Task<List<ProgramRegistartion>> GetProgramRegistrationsWithDetails();
    }
}
