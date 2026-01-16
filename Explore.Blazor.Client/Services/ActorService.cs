using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class ActorService : IActorService
{
    private readonly IEventApiClient _client;

    public ActorService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<ActorListDto>> GetActorsAsync()
    {
        var response = await _client.ActorGETAsync(pageNumber: 1, pageSize: 100);
        return response?.Items ?? new List<ActorListDto>();
    }

    public async Task<ActorDto> GetActorByIdAsync(Guid id)
    {
        return await _client.ActorGET2Async(id);
    }
}
