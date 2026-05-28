// ABOUTME: Persistence repository for external API keys with tenant-scoped and platform-scoped query paths.
// ABOUTME: Uses explicit tenant-filter bypasses for authentication and InstanceAdmin platform key management.

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
            .IgnoreTenantFilter(TenantFilterBypassReasons.ExternalApiKeyAuthentication)
            .AsNoTracking()
            .FirstOrDefaultAsync(apiKey => apiKey.KeyId == keyId);
    }

    public async Task<bool> TouchUsageMetadata(Guid id, DateTime usedAtUtc, string? lastUsedIp, TimeSpan minUpdateInterval, CancellationToken cancellationToken = default)
    {
        var effectiveThreshold = usedAtUtc.Subtract(minUpdateInterval);

        if (!_dbContext.Database.IsRelational())
        {
            var apiKey = await _dbContext.ExternalApiKeys
                .IgnoreTenantFilter(TenantFilterBypassReasons.ExternalApiKeyAuthentication)
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
            .IgnoreTenantFilter(TenantFilterBypassReasons.ExternalApiKeyAuthentication)
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
            .Where(apiKey => apiKey.ExternalApiKeyOwnerTypeId == (int)ownerType && apiKey.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<List<ExternalApiKey>> GetByOwners(ExternalApiKeyOwnerType ownerType, IReadOnlyCollection<Guid> ownerIds)
    {
        if (ownerIds.Count == 0)
        {
            return [];
        }

        var results = new List<ExternalApiKey>(ownerIds.Count);
        foreach (var chunk in ownerIds.Chunk(100))
        {
            var chunkResults = await _dbContext.ExternalApiKeys
                .AsNoTracking()
                .Where(apiKey => apiKey.ExternalApiKeyOwnerTypeId == (int)ownerType && chunk.Contains(apiKey.OwnerId))
                .ToListAsync();
            results.AddRange(chunkResults);
        }
        return results;
    }

    public async Task<bool> ExistsByOwnerAndName(ExternalApiKeyOwnerType ownerType, Guid ownerId, string name)
    {
        return await _dbContext.ExternalApiKeys
            .AsNoTracking()
            .AnyAsync(apiKey => apiKey.ExternalApiKeyOwnerTypeId == (int)ownerType && apiKey.OwnerId == ownerId && apiKey.Name == name);
    }

    public async Task<ExternalApiKey?> GetByIdIgnoringTenantFilter(Guid id)
    {
        return await _dbContext.ExternalApiKeys
            .IgnoreTenantFilter(TenantFilterBypassReasons.ExternalApiKeyPlatformManagement)
            .FirstOrDefaultAsync(apiKey => apiKey.Id == id);
    }

    public async Task<List<ExternalApiKey>> GetByOwnerIgnoringTenantFilter(ExternalApiKeyOwnerType ownerType, Guid ownerId)
    {
        return await _dbContext.ExternalApiKeys
            .IgnoreTenantFilter(TenantFilterBypassReasons.ExternalApiKeyPlatformManagement)
            .AsNoTracking()
            .Where(apiKey => apiKey.ExternalApiKeyOwnerTypeId == (int)ownerType && apiKey.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<bool> ExistsByOwnerAndNameIgnoringTenantFilter(ExternalApiKeyOwnerType ownerType, Guid ownerId, string name)
    {
        return await _dbContext.ExternalApiKeys
            .IgnoreTenantFilter(TenantFilterBypassReasons.ExternalApiKeyPlatformManagement)
            .AsNoTracking()
            .AnyAsync(apiKey => apiKey.ExternalApiKeyOwnerTypeId == (int)ownerType && apiKey.OwnerId == ownerId && apiKey.Name == name);
    }
}
