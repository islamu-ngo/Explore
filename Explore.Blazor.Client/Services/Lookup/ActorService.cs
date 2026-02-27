// ABOUTME: Service for managing actor-related operations.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Contracts.Services.Lookup;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Services.Lookup;

public class ActorService : IActorService
{
    private readonly IEventApiClient _client;

    public ActorService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<ActorListDto>> GetActorsAsync()
    {
        var result = await _client.GetActorsAsync(pageNumber: ApiConstants.FirstPage, pageSize: ApiConstants.DefaultPageSize);
        return result?.GetItems() ?? new List<ActorListDto>();
    }

    public async Task<ActorDto?> GetActorByIdAsync(Guid id)
    {
        var result = await _client.GetActorByIdAsync(id);
        return result?.ToDto();
    }
}
