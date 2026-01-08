using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventRepository : IGenericRepository<Event, Guid>
    {
        Task<Event?> GetEventWithDetails(Guid id);
        Task<List<Event>> GetEventsWithDetails();
        Task<List<Event>> GetMyEventsWithDetails(string userId);
    }
}
