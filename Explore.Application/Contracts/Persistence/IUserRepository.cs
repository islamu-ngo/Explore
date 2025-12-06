using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IUserRepository : IGenericRepository<User, Guid>
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<List<User>> GetUsersByIdsAsync(List<Guid> ids);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
