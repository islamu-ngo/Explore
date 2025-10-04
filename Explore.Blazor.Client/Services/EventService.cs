using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Client.Services;

public interface IEventService
{
    List<Event> GetUserEvents(string? searchText = null, string? country = null, string? category = null, DateTime? date = null);
    void DeleteEvent(int eventId);
    void UpdateEvent(Event evt);
}

public class EventService : IEventService
{
    private List<Event> _events;

    public EventService()
    {
        // Initialize with mock data
        _events = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Islamic Finance Workshop",
                Date = DateTime.Now.AddDays(7),
                Country = "Netherlands",
                Category = "Education",
                ImageUrl = "dummy-image-1"
            },
            new Event
            {
                Id = 2,
                Title = "DeepDives Seminar",
                Date = DateTime.Now.AddDays(14),
                Country = "Belgium",
                Category = "Workshop",
                ImageUrl = "dummy-image-2"
            }
        };
    }

    public List<Event> GetUserEvents(string? searchText = null, string? country = null, string? category = null, DateTime? date = null)
    {
        var query = _events.AsQueryable();

        if (!string.IsNullOrEmpty(searchText))
        {
            query = query.Where(e => e.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(country))
        {
            query = query.Where(e => e.Country == country);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(e => e.Category == category);
        }

        return query.ToList();
    }

    public void DeleteEvent(int eventId)
    {
        var evt = _events.FirstOrDefault(e => e.Id == eventId);
        if (evt != null)
        {
            _events.Remove(evt);
        }
    }

    public void UpdateEvent(Event evt)
    {
        var index = _events.FindIndex(e => e.Id == evt.Id);
        if (index != -1)
        {
            _events[index] = evt;
        }
    }
}
