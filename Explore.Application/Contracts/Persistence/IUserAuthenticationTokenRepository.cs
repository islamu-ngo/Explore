// ABOUTME: Repository contract for persisted external authentication token records.
// ABOUTME: Exposes user-scoped reads so handlers cannot accidentally enumerate credentials.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IUserAuthenticationTokenRepository : IGenericRepository<UserAuthenticationToken, Guid>
{
    Task<UserAuthenticationToken?> GetByUserAndProvider(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<List<UserAuthenticationToken>> GetByUser(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAuthenticationToken?> GetByIdForUser(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<UserAuthenticationToken?> GetUserAuthenticationTokenWithDetailsForUser(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserAuthenticationToken>> GetUserAuthenticationTokensWithDetailsForUser(Guid userId, CancellationToken cancellationToken = default);
}
