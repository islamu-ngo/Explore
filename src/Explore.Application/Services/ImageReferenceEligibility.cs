// ABOUTME: Resolves image storage references and applies the shared safe-raster eligibility policy.
// ABOUTME: Rejects missing, cross-tenant, inactive, unsafe, or non-public image references before mutation.

using Explore.Application.Contracts.Persistence;

namespace Explore.Application.Services;

public static class ImageReferenceEligibility
{
    public static async Task<bool> AreEligibleAsync(
        IStorageObjectRepository repository,
        Guid tenantId,
        params Guid?[] storageObjectIds)
    {
        foreach (Guid storageObjectId in storageObjectIds
                     .Where(id => id.HasValue)
                     .Select(id => id!.Value)
                     .Distinct())
        {
            if (!SafeRasterContentPolicy.IsEligibleImageReference(
                    await repository.GetById(storageObjectId),
                    tenantId))
            {
                return false;
            }
        }

        return true;
    }
}
