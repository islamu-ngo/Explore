using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class AudienceGenderService : IAudienceGenderService
{
    private readonly IAudienceGenderClient _client;

    public AudienceGenderService(IAudienceGenderClient client)
    {
        _client = client;
    }

    public async Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync()
    {
        return await _client.GetAudienceGenderOptionsAsync();
    }

    public async Task<AudienceGenderDto> GetAudienceGenderByIdAsync(int id)
    {
        return await _client.GetAudienceGenderOptionByIdAsync(id);
    }
}

