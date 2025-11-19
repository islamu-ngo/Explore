using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.DTOs.Event;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventRepository : IGenericRepository<Event, Guid>
    {
        Task<EventDto> GetEventWithDetails(Guid id);
        Task<List<EventListDto>> GetEventsWithDetails();
        Task<List<EventListDto>> GetMyEventsWithDetails(string userId);
    }
}
