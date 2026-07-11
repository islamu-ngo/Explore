// ABOUTME: Contract for RegistrationScope lookup operations (read-only).
// ABOUTME: Wraps the NSwag-generated IEventApiClient methods for RegistrationScope lookup.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IRegistrationScopeService
{
    Task<ICollection<RegistrationScopeListDto>> GetRegistrationScopesAsync();
}
