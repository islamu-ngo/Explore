using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services;

public class AudienceAgeService : IAudienceAgeService
{
    private readonly IEventApiClient _client;

    public AudienceAgeService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync()
    {
        return await _client.AudienceAgeAllAsync();
    }

    public async Task<AudienceAgeDto> GetAudienceAgeByIdAsync(int id)
    {
        return await _client.AudienceAgeAsync(id);
    }
}
