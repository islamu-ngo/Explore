using Explore.Blazor.Client.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventTypeService
{
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
}
