using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class EventStatusService : IEventStatusService
{
    private readonly IEventStatusClient _client;

    public EventStatusService(IEventStatusClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventStatusListDto>> GetEventStatusesAsync()
    {
        return await _client.GetEventStatusesAsync();
    }

    public async Task<EventStatusDto> GetEventStatusByIdAsync(int id)
    {
        return await _client.GetEventStatusByIdAsync(id);
    }
}

