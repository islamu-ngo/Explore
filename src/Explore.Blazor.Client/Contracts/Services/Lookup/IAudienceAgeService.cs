using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IAudienceAgeService
{
    Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync();
    Task<AudienceAgeDto> GetAudienceAgeByIdAsync(int id);
}
