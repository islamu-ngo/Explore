using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IUserAuthenticationTokenRepository : IGenericRepository<UserAuthenticationToken, Guid>
{
    Task<UserAuthenticationToken?> GetByUserAndProvider(Guid userId, string provider);
    Task<List<UserAuthenticationToken>> GetByUser(Guid userId);
    Task<UserAuthenticationToken?> GetUserAuthenticationTokenWithDetails(Guid id);
    Task<List<UserAuthenticationToken>> GetUserAuthenticationTokensWithDetails();
}
