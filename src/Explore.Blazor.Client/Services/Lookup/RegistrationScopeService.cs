// ABOUTME: Lookup service for RegistrationScope read-only data.
// ABOUTME: Thin wrapper around NSwag-generated client method.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class RegistrationScopeService : IRegistrationScopeService
{
    private readonly IEventApiClient _client;

    public RegistrationScopeService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<RegistrationScopeListDto>> GetRegistrationScopesAsync()
    {
        return await _client.GetRegistrationScopesAsync();
    }
}
