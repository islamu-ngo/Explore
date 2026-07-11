// ABOUTME: Contract for retrieving authenticated user and tenant context.
// ABOUTME: Centralizes auth-state access so pages depend on abstraction, not auth provider details.

namespace Explore.Blazor.Client.Contracts.Providers;

public interface IAuthStateService
{
    Task<string> GetCurrentUserIdAsync();
    Task<Guid> GetCurrentTenantIdAsync();
    Task<bool> IsAuthenticatedAsync();
}
