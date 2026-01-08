using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IUserRepository : IGenericRepository<User, Guid>
    {
        Task<User?> GetUserWithDetails(Guid id);
        Task<bool> ExistsByEmail(string email);
        Task<List<User>> GetUsersByIdsAsync(List<Guid> ids);
    }
}
