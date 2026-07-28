// ABOUTME: Persistence contract for immutable instance-scoped platform fee policy history.
// ABOUTME: Returns Domain entities so future handlers retain version and concurrency ownership.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IPlatformFeePolicyRepository
{
    Task<PlatformFeePolicy?> GetActiveAsync(CancellationToken cancellationToken);

    Task AddAsync(PlatformFeePolicy policy, CancellationToken cancellationToken);

    Task UpdateAsync(PlatformFeePolicy policy, CancellationToken cancellationToken);
}
