using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IStatusTypeRepository : IGenericRepository<StatusType, int>
    {
        Task<StatusType> GetStatusTypeWithDetails(int id);
        Task<List<StatusType>> GetStatusTypesWithDetails();
    }
}
