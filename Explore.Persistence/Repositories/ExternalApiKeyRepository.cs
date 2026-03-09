// ABOUTME: Persistence repository for external API keys.
// ABOUTME: Uses an explicit tenant-filter bypass only for pre-tenant authentication lookup by public key id.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class ExternalApiKeyRepository : GenericRepository<ExternalApiKey, Guid>, IExternalApiKeyRepository
{
    private readonly ExploreDbContext _dbContext;

    public ExternalApiKeyRepository(ExploreDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExternalApiKey?> GetByKeyIdForAuthentication(string keyId)
    {
        return await _dbContext.ExternalApiKeys
            .IgnoreTenantFilter()
            .AsNoTracking()
            .FirstOrDefaultAsync(apiKey => apiKey.KeyId == keyId);
    }

    public async Task<bool> TouchUsageMetadata(Guid id, DateTime usedAtUtc, string? lastUsedIp, TimeSpan minUpdateInterval, CancellationToken cancellationToken = default)
    {
        var effectiveThreshold = usedAtUtc.Subtract(minUpdateInterval);

        if (!_dbContext.Database.IsRelational())
        {
            var apiKey = await _dbContext.ExternalApiKeys
                .IgnoreTenantFilter()
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

            if (apiKey is null || (apiKey.LastUsedAt is DateTime lastUsedAt && lastUsedAt >= effectiveThreshold))
            {
                return false;
            }

            apiKey.LastUsedAt = usedAtUtc;
            apiKey.LastUsedIp = lastUsedIp;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var rowsAffected = await _dbContext.ExternalApiKeys
            .IgnoreTenantFilter()
            .Where(apiKey => apiKey.Id == id &&
                (apiKey.LastUsedAt == null || apiKey.LastUsedAt < effectiveThreshold))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(apiKey => apiKey.LastUsedAt, usedAtUtc)
                .SetProperty(apiKey => apiKey.LastUsedIp, lastUsedIp), cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<List<ExternalApiKey>> GetByOwner(ExternalApiKeyOwnerType ownerType, Guid ownerId)
    {
        return await _dbContext.ExternalApiKeys
            .AsNoTracking()
            .Where(apiKey => apiKey.OwnerType == ownerType && apiKey.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<List<ExternalApiKey>> GetByOwners(ExternalApiKeyOwnerType ownerType, IReadOnlyCollection<Guid> ownerIds)
    {
        if (ownerIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.ExternalApiKeys
            .AsNoTracking()
            .Where(apiKey => apiKey.OwnerType == ownerType && ownerIds.Contains(apiKey.OwnerId))
            .ToListAsync();
    }

    public async Task<bool> ExistsByOwnerAndName(ExternalApiKeyOwnerType ownerType, Guid ownerId, string name)
    {
        return await _dbContext.ExternalApiKeys
            .AsNoTracking()
            .AnyAsync(apiKey => apiKey.OwnerType == ownerType && apiKey.OwnerId == ownerId && apiKey.Name == name);
    }
}
