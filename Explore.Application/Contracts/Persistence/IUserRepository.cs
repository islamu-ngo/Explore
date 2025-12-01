using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IUserRepository : IGenericRepository<User, Guid>
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
