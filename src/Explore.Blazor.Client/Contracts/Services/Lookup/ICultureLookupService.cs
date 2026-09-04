// ABOUTME: Contract for cultural and Islamic reference lookups (languages, madhabs).
// ABOUTME: Encapsulates religious school of thought and language taxonomies for events and sessions.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface ICultureLookupService
{
    Task<ICollection<LanguageListDto>> GetLanguagesAsync();
    Task<ICollection<MadhabListDto>> GetMadhabsAsync();
}
