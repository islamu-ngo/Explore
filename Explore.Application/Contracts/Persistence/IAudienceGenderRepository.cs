using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IAudienceGenderRepository : IGenericRepository<AudienceGender, int>
    {
        Task<AudienceGender> GetAudienceGenderWithDetails(int id);
        Task<List<AudienceGender>> GetAudienceGendersWithDetails();
    }
}
