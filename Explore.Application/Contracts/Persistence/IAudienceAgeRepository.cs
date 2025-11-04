using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IAudienceAgeRepository : IGenericRepository<AudienceAge, int>
    {
        Task<AudienceAge> GetAudienceAgeWithDetails(int id);
        Task<List<AudienceAge>> GetAudienceAgesWithDetails();
    }
}
