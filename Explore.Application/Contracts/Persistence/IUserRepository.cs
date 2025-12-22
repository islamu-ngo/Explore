using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.User;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IUserRepository : IGenericRepository<User, Guid>
    {
        Task<UserDto> GetByIdDto(Guid id);
        Task<bool> ExistsByEmail(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<List<User>> GetUsersByIdsAsync(List<Guid> ids);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
