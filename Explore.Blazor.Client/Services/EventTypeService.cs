using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class EventTypeService : IEventTypeService
{
    private readonly IEventApiClient _client;

    public EventTypeService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventTypeListDto>> GetEventTypesAsync()
    {
        return await _client.EventTypeAllAsync();
    }
}
