using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Lookup;

namespace Explore.Blazor.Client.Services.Lookup;

public class LanguageService : ILanguageService
{
    private readonly ILanguageClient _client;

    public LanguageService(ILanguageClient client)
    {
        _client = client;
    }

    public async Task<ICollection<LanguageListDto>> GetLanguagesAsync()
    {
        return await _client.GetLanguagesAsync();
    }

    public async Task<LanguageDto> GetLanguageByIdAsync(int id)
    {
        return await _client.GetLanguageByIdAsync(id);
    }
}
