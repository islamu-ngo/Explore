using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class EventFormatService : IEventFormatService
{
    private readonly IEventApiClient _client;

    public EventFormatService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventFormatListDto>> GetEventFormatsAsync()
    {
        return await _client.EventFormatAllAsync();
    }

    public async Task<EventFormatDto> GetEventFormatByIdAsync(int id)
    {
        return await _client.EventFormatAsync(id);
    }
}
