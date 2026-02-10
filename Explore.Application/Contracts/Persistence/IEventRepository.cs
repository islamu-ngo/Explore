using Explore.Application.Specifications.Events;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<Event?> GetEventWithDetails(Guid id);
    Task<List<Event>> GetEventsWithDetails();
    Task<List<Event>> GetMyEventsWithDetails(string userId);

    /// <summary>
    /// Gets a paginated list of events with details.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing the items and total count.</returns>
    Task<(List<Event> Items, int TotalCount)> GetEventsWithDetailsPaged(int pageNumber, int pageSize);

    /// <summary>
    /// Gets a paginated and filtered list of events with details.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="specification">The query specification containing filters and sort criteria.</param>
    /// <returns>A tuple containing the items and total count.</returns>
    Task<(List<Event> Items, int TotalCount)> GetEventsWithDetailsPaged(
        int pageNumber, int pageSize, EventQuerySpecification specification);

    /// <summary>
    /// Gets a paginated list of events for the current user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing the items and total count.</returns>
    Task<(List<Event> Items, int TotalCount)> GetMyEventsWithDetailsPaged(string userId, int pageNumber, int pageSize);
}
