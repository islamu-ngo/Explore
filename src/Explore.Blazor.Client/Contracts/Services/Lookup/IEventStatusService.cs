using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IEventStatusService
{
    Task<ICollection<EventStatusListDto>> GetEventStatusesAsync();
    Task<EventStatusDto> GetEventStatusByIdAsync(int id);
}
