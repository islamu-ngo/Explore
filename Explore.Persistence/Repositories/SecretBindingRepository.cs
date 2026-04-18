// ABOUTME: Repository implementation for SecretBinding - reads use AsNoTracking for
// resolver hot path; inherits Create/Update/Delete/Exists semantics from GenericRepository.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Microsoft.EntityFrameworkCore;

public class SecretBindingRepository : GenericRepository<SecretBinding, Guid>, ISecretBindingRepository
{
    private readonly ExploreDbContext _dbContext;

    public SecretBindingRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SecretBinding?> GetByKeyAndScopeAsync(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SecretBindings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.SettingKey == settingKey
                     && b.Scope == scope
                     && b.ScopeId == scopeId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SecretBinding>> GetByScopeAsync(
        SecretScope scope,
        Guid? scopeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SecretBindings
            .AsNoTracking()
            .Where(b => b.Scope == scope && b.ScopeId == scopeId)
            .OrderBy(b => b.SettingKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecretBinding>> GetAllForKeyAsync(
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.SecretBindings
            .AsNoTracking()
            .Where(b => b.SettingKey == settingKey)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForScopeAsync(
        string settingKey,
        SecretScope scope,
        Guid? scopeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SecretBindings
            .AsNoTracking()
            .AnyAsync(
                b => b.SettingKey == settingKey
                     && b.Scope == scope
                     && b.ScopeId == scopeId,
                cancellationToken);
    }
}
