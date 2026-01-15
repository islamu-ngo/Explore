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
        return await _client.ActorAllAsync();
    }

    public async Task<ActorDto> GetActorByIdAsync(Guid id)
    {
        return await _client.ActorGETAsync(id);
    }
}
