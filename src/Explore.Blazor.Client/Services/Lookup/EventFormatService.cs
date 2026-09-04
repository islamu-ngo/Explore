using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class EventFormatService : IEventFormatService
{
    private readonly IEventFormatClient _client;

    public EventFormatService(IEventFormatClient client)
    {
        _client = client;
    }

    public async Task<ICollection<EventFormatListDto>> GetEventFormatsAsync()
    {
        return await _client.GetEventFormatOptionsAsync();
    }

    public async Task<EventFormatDto> GetEventFormatByIdAsync(int id)
    {
        return await _client.GetEventFormatOptionByIdAsync(id);
    }
}

