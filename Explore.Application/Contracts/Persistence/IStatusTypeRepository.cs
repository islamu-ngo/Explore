using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IStatusTypeRepository : IGenericRepository<ApprovalStatus, int>
    {
        Task<ApprovalStatus> GetStatusTypeWithDetails(int id);
        Task<List<ApprovalStatus>> GetStatusTypesWithDetails();
    }
}
