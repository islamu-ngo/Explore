using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class MadhabService : IMadhabService
{
    private readonly IMadhabClient _client;

    public MadhabService(IMadhabClient client)
    {
        _client = client;
    }

    public async Task<ICollection<MadhabListDto>> GetMadhabsAsync()
    {
        return await _client.GetMadhabsAsync();
    }

    public async Task<MadhabDto> GetMadhabByIdAsync(int id)
    {
        return await _client.GetMadhabByIdAsync(id);
    }
}
