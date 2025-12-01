using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IProgramRegistrationRepository : IGenericRepository<ProgramRegistration, Guid>
    {
        Task<ProgramRegistration> GetProgramRegistrationWithDetails(Guid id);
        Task<List<ProgramRegistration>> GetProgramRegistrationsWithDetails();
    }
}
