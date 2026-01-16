using Explore.Blazor.Client.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventStatusService
{
    Task<ICollection<EventStatusListDto>> GetEventStatusesAsync();
    Task<EventStatusDto> GetEventStatusByIdAsync(int id);
}
