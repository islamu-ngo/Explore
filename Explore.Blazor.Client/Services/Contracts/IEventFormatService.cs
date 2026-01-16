using Explore.Blazor.Client.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventFormatService
{
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<EventFormatDto> GetEventFormatByIdAsync(int id);
}
