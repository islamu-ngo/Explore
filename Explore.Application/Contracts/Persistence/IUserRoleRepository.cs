using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IUserRoleRepository : IGenericRepository<UserRole, int>
    {
        Task<UserRole?> GetByMasterCode(string masterCode);
    }
}
