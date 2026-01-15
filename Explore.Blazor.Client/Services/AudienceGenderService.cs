using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class AudienceGenderService : IAudienceGenderService
{
    private readonly IEventApiClient _client;

    public AudienceGenderService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync()
    {
        return await _client.AudienceGenderAllAsync();
    }

    public async Task<AudienceGenderDto> GetAudienceGenderByIdAsync(int id)
    {
        return await _client.AudienceGenderAsync(id);
    }
}
