using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class MadhabService : IMadhabService
{
    private readonly IEventApiClient _client;

    public MadhabService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<MadhabListDto>> GetMadhabsAsync()
    {
        return await _client.MadhabAllAsync();
    }

    public async Task<MadhabDto> GetMadhabByIdAsync(int id)
    {
        return await _client.MadhabAsync(id);
    }
}
