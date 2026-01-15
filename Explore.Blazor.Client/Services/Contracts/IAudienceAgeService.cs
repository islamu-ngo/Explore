using Explore.Blazor.Client.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IAudienceAgeService
{
    Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync();
    Task<AudienceAgeDto> GetAudienceAgeByIdAsync(int id);
}
