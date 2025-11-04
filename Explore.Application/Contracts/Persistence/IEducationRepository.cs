using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEducationRepository : IGenericRepository<Education, Guid>
    {
        Task<Education> GetEducationWithDetails(Guid id);
        Task<List<Education>> GetEducationsWithDetails();
    }
}
