using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class EventStatusService : IEventStatusService
{
    private readonly IEventApiClient _client;

    public EventStatusService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventStatusListDto>> GetEventStatusesAsync()
    {
        return await _client.EventStatusAllAsync();
    }

    public async Task<EventStatusDto> GetEventStatusByIdAsync(int id)
    {
        return await _client.EventStatusAsync(id);
    }
}
