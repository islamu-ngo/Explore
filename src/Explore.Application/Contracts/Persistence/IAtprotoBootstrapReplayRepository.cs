// ABOUTME: Reserves ATProto bootstrap assertion identifiers atomically across API instances.
// ABOUTME: Prevents a valid BFF assertion from authorizing more than one bridge request.

namespace Explore.Application.Contracts.Persistence;

public interface IAtprotoBootstrapReplayRepository
{
    Task<bool> TryConsumeAsync(
        string jti,
        Guid tenantId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
