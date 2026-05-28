// ABOUTME: Tests Event aggregate schedule projection rebuilding across timezone changes.
// ABOUTME: Verifies sessions, agenda items, day links, and event rollups stay derived from UTC instants.

namespace Event.Domain.UnitTests.Entities;

using Explore.Domain.Services.Scheduling;

public class EventScheduleProjectionTests
{
    private readonly EventScheduleProjectionCalculator _calculator = new();

    [Test]
    public async Task ApplyScheduleTimeZone_ReprojectsChildrenAndRelinksMatchingEventDays()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant();
        var day = new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            LocalDate = new DateOnly(2026, 6, 15)
        };
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            StartTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var agendaItem = new EventAgendaItem
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            Title = "Dhuhr break",
            StartTime = new DateTimeOffset(2026, 6, 15, 9, 30, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero)
        };
        var @event = CreateEvent(eventId, tenant);
        @event.Days.Add(day);
        @event.Sessions.Add(session);
        @event.AgendaItems.Add(agendaItem);

        @event.ApplyScheduleTimeZone("Europe/Brussels", _calculator);

        await Assert.That(@event.EventTimeZoneId).IsEqualTo("Europe/Brussels");
        await Assert.That(@event.Timezone).IsEqualTo("Europe/Brussels");
        await Assert.That(session.LocalStartTime).IsEqualTo(new TimeOnly(12, 0));
        await Assert.That(agendaItem.LocalStartTime).IsEqualTo(new TimeOnly(11, 30));
        await Assert.That(session.EventDayId).IsEqualTo(day.Id);
        await Assert.That(agendaItem.EventDayId).IsEqualTo(day.Id);
        await Assert.That(@event.FirstSessionStartUtc).IsEqualTo(session.StartTime);
        await Assert.That(@event.LastSessionStartUtc).IsEqualTo(session.StartTime);
        await Assert.That(@event.FirstSessionDate).IsEqualTo(day.LocalDate);
        await Assert.That(@event.SessionCount).IsEqualTo(1);
    }

    [Test]
    public async Task ApplyScheduleTimeZone_WhenTimezoneShiftsLocalDate_DropsStaleDayLink()
    {
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant();
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            EventDayId = Guid.NewGuid(),
            StartTime = new DateTimeOffset(2026, 6, 15, 22, 30, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 6, 15, 23, 30, 0, TimeSpan.Zero)
        };
        var @event = CreateEvent(eventId, tenant);
        @event.Days.Add(new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            LocalDate = new DateOnly(2026, 6, 15)
        });
        @event.Sessions.Add(session);

        @event.ApplyScheduleTimeZone("Europe/Brussels", _calculator);

        await Assert.That(session.LocalStartDate).IsEqualTo(new DateOnly(2026, 6, 16));
        await Assert.That(session.EventDayId).IsNull();
        await Assert.That(@event.FirstSessionDate).IsEqualTo(new DateOnly(2026, 6, 16));
    }

    private static Explore.Domain.Event CreateEvent(Guid eventId, Tenant tenant) => new()
    {
        Id = eventId,
        Title = "Event",
        Actor = null!,
        Tenant = tenant,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!
    };

    private static Tenant CreateTenant() => new()
    {
        FullName = "Tenant",
        Slug = "tenant",
        TenantStatusId = 2,
        TenantStatus = new TenantStatus { Id = 2, MasterCode = "ACTIVE", FullName = "Active", IsActiveState = true }
    };
}
