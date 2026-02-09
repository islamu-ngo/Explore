using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services.Contracts;

namespace Explore.Blazor.Client.Services;

public class LanguageService : ILanguageService
{
    private readonly IEventApiClient _client;

    public LanguageService(IEventApiClient client)
    {
        _client = client;
    }

    public async Task<ICollection<LanguageListDto>> GetLanguagesAsync()
    {
        return await _client.LanguageAllAsync();
    }

    public async Task<LanguageDto> GetLanguageByIdAsync(int id)
    {
        return await _client.LanguageAsync(id);
    }
}
