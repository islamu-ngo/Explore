// ABOUTME: Resolves the active primary authentication provider for new login flows.
// ABOUTME: Returns the normalized Domain lookup enum rather than a persisted provider-name string.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Infrastructure;

public interface IAuthenticationProviderDispatcher
{
    Task<AuthenticationProviderKind> GetActivePrimaryProviderAsync(
        CancellationToken cancellationToken);
}
