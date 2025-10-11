using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Client.Services;

public interface IEventService
{
    List<Event> GetUserEvents(string? searchText = null, string? country = null, string? category = null, DateTime? date = null);
    Event? GetEventById(int eventId);
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
                Category = "lecture",
                ImageUrl = "dummy-image-1",
                Description = "Learn about Islamic finance principles and banking systems in this comprehensive workshop.",
                Location = "Amsterdam Conference Center, Zuidplein 36",
                Address = "Zuidplein 36",
                City = "Amsterdam",
                LocationCountry = "Netherlands",
                IsOnline = false
            },
            new Event
            {
                Id = 2,
                Title = "DeepDives Seminar",
                Date = DateTime.Now.AddDays(14),
                Country = "Belgium",
                Category = "workshop",
                ImageUrl = "dummy-image-2",
                Description = "Deep dive into advanced topics with industry experts and thought leaders.",
                Location = "Brussels Convention Hall, Rue Ducale 29",
                Address = "Rue Ducale 29",
                City = "Brussels",
                LocationCountry = "Belgium",
                IsOnline = false
            },
            new Event
            {
                Id = 3,
                Title = "Online Tech Conference",
                Date = DateTime.Now.AddDays(21),
                Country = "Netherlands",
                Category = "social",
                ImageUrl = "dummy-image-3",
                Description = "Virtual conference exploring the latest in technology and innovation.",
                Url = "https://techconf.example.com",
                IsOnline = true
            },
            new Event
            {
                Id = 4,
                Title = "Business Networking Event",
                Date = DateTime.Now.AddDays(28),
                Country = "Germany",
                Category = "charity",
                ImageUrl = "dummy-image-4",
                Description = "Connect with business professionals and expand your network in this exciting event.",
                Location = "Berlin Business Center, Potsdamer Platz 1",
                IsOnline = false
            },
            new Event
            {
                Id = 5,
                Title = "Cultural Heritage Workshop",
                Date = DateTime.Now.AddDays(35),
                Country = "France",
                Category = "workshop",
                ImageUrl = "dummy-image-5",
                Description = "Explore cultural heritage preservation and its importance in modern society.",
                Location = "Paris Cultural Institute, Champs-Élysées 25",
                IsOnline = false
            },
            new Event
            {
                Id = 6,
                Title = "Virtual Leadership Summit",
                Date = DateTime.Now.AddDays(42),
                Country = "Netherlands",
                Category = "lecture",
                ImageUrl = "dummy-image-6",
                Description = "Online summit featuring leadership strategies and management techniques.",
                Url = "https://leadershipsummit.example.com",
                IsOnline = true
            },
            new Event
            {
                Id = 7,
                Title = "Innovation Lab",
                Date = DateTime.Now.AddDays(49),
                Country = "Belgium",
                Category = "social",
                ImageUrl = "dummy-image-7",
                Description = "Hands-on innovation laboratory exploring cutting-edge technologies and solutions.",
                Location = "Antwerp Innovation Hub, Meir 15",
                IsOnline = false
            },
            new Event
            {
                Id = 8,
                Title = "Digital Marketing Masterclass",
                Date = DateTime.Now.AddDays(56),
                Country = "Germany",
                Category = "workshop",
                ImageUrl = "dummy-image-8",
                Description = "Master the art of digital marketing with expert-led sessions and practical exercises.",
                Location = "Munich Digital Campus, Maximilianstrasse 42",
                IsOnline = false
            },
            new Event
            {
                Id = 9,
                Title = "Online Community Meetup",
                Date = DateTime.Now.AddDays(63),
                Country = "Netherlands",
                Category = "social",
                ImageUrl = "dummy-image-9",
                Description = "Virtual community meetup to connect with like-minded individuals and share experiences.",
                Url = "https://communitymeetup.example.com",
                IsOnline = true
            },
            new Event
            {
                Id = 10,
                Title = "Sustainable Development Forum",
                Date = DateTime.Now.AddDays(70),
                Country = "France",
                Category = "charity",
                ImageUrl = "dummy-image-10",
                Description = "Forum discussing sustainable development practices and environmental conservation.",
                Location = "Lyon Convention Center, Place Bellecour 12",
                IsOnline = false
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

    public Event? GetEventById(int eventId)
    {
        return _events.FirstOrDefault(e => e.Id == eventId);
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
