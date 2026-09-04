using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class AudienceAgeService : IAudienceAgeService
{
    private readonly IAudienceAgeClient _client;

    public AudienceAgeService(IAudienceAgeClient client)
    {
        _client = client;
    }

    public async Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync()
    {
        return await _client.GetAudienceAgeOptionsAsync();
    }

    public async Task<AudienceAgeDto> GetAudienceAgeByIdAsync(int id)
    {
        return await _client.GetAudienceAgeOptionByIdAsync(id);
    }
}

