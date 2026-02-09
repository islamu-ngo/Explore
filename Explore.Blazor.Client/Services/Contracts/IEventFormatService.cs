using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventFormatService
{
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<EventFormatDto> GetEventFormatByIdAsync(int id);
}
