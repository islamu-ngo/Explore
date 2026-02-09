using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Contracts;

public interface ILanguageService
{
    Task<ICollection<LanguageListDto>> GetLanguagesAsync();
    Task<LanguageDto> GetLanguageByIdAsync(int id);
}
