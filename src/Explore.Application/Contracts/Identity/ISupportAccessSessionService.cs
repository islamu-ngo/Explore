// ABOUTME: Application service contract for validating and resolving support-access sessions.
// ABOUTME: Keeps persisted session validation behind an Application-owned abstraction.

namespace Explore.Application.Contracts.Identity;

public interface ISupportAccessSessionService
{
    Task<ISupportAccessContext> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<ISupportAccessContext> ValidateForwardedSessionAsync(
        Guid sessionId,
        Guid actorUserId,
        Guid? resolvedTenantId,
        CancellationToken cancellationToken = default);
}
