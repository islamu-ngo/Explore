using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IAudienceGenderService
{
    Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync();
    Task<AudienceGenderDto> GetAudienceGenderByIdAsync(int id);
}
