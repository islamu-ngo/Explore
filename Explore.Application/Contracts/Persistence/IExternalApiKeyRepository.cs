// ABOUTME: Repository contract for persisted external API key authentication lookups.
// ABOUTME: Exposes an explicit pre-tenant auth path so API-key authentication can resolve tenant authority safely.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public interface IExternalApiKeyRepository : IGenericRepository<ExternalApiKey, Guid>
{
    Task<ExternalApiKey?> GetByKeyIdForAuthentication(string keyId);
    Task<bool> TouchUsageMetadata(Guid id, DateTime usedAtUtc, string? lastUsedIp, TimeSpan minUpdateInterval, CancellationToken cancellationToken = default);
    Task<List<ExternalApiKey>> GetByOwner(ExternalApiKeyOwnerType ownerType, Guid ownerId);
    Task<List<ExternalApiKey>> GetByOwners(ExternalApiKeyOwnerType ownerType, IReadOnlyCollection<Guid> ownerIds);
    Task<bool> ExistsByOwnerAndName(ExternalApiKeyOwnerType ownerType, Guid ownerId, string name);
}
