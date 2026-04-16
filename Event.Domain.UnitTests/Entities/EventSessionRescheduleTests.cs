// ABOUTME: Tests the EventSession aggregate methods Reschedule and ReprojectLocalTimes for UTC/local projection consistency.
// ABOUTME: Covers happy path, validation (end <= start), null calculator guard, and cached local field synchronization.

namespace Event.Domain.UnitTests.Entities;

using Explore.Domain.Services.Scheduling;

public class EventSessionRescheduleTests
{
    private readonly EventScheduleProjectionCalculator _calculator = new();

    [Test]
    public async Task Reschedule_WithValidTimes_SetsStartAndEndTime()
    {
        var session = CreateEventSession();
        var start = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        session.Reschedule(start, end, "Europe/Brussels", _calculator);

        await Assert.That(session.StartTime).IsEqualTo(start);
        await Assert.That(session.EndTime).IsEqualTo(end);
    }

    [Test]
    public async Task Reschedule_WithValidTimes_ProjectsLocalFields()
    {
        var session = CreateEventSession();
        var start = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        session.Reschedule(start, end, "Europe/Brussels", _calculator);

        // 10:00 UTC = 12:00 CEST, 12:00 UTC = 14:00 CEST
        await Assert.That(session.LocalStartDate).IsEqualTo(new DateOnly(2026, 6, 15));
        await Assert.That(session.LocalEndDate).IsEqualTo(new DateOnly(2026, 6, 15));
        await Assert.That(session.LocalStartTime).IsEqualTo(new TimeOnly(12, 0));
        await Assert.That(session.LocalEndTime).IsEqualTo(new TimeOnly(14, 0));
        await Assert.That(session.LocalStartMinuteOfDay).IsEqualTo(720);
        await Assert.That(session.LocalEndMinuteOfDay).IsEqualTo(840);
    }

    [Test]
    public async Task Reschedule_EndEqualsStart_ThrowsArgumentException()
    {
        var session = CreateEventSession();
        var time = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        await Assert.That(() => session.Reschedule(time, time, "Europe/Brussels", _calculator))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Reschedule_EndBeforeStart_ThrowsArgumentException()
    {
        var session = CreateEventSession();
        var start = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        await Assert.That(() => session.Reschedule(start, end, "Europe/Brussels", _calculator))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Reschedule_CrossMidnightInTimezone_ProjectsDifferentDates()
    {
        var session = CreateEventSession();
        // 21:00 UTC = 23:00 CEST, 01:00 UTC next day = 03:00 CEST next day
        var start = new DateTimeOffset(2026, 6, 15, 21, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 16, 1, 0, 0, TimeSpan.Zero);

        session.Reschedule(start, end, "Europe/Brussels", _calculator);

        await Assert.That(session.LocalStartDate).IsEqualTo(new DateOnly(2026, 6, 15));
        await Assert.That(session.LocalEndDate).IsEqualTo(new DateOnly(2026, 6, 16));
    }

    [Test]
    public async Task ReprojectLocalTimes_NullCalculator_ThrowsArgumentNullException()
    {
        var session = CreateEventSession();

        await Assert.That(() => session.ReprojectLocalTimes("Europe/Brussels", null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ReprojectLocalTimes_UpdatesCachedFields()
    {
        var session = CreateEventSession();
        session.StartTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        session.EndTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        session.ReprojectLocalTimes("Asia/Tokyo", _calculator);

        // 10:00 UTC = 19:00 JST, 12:00 UTC = 21:00 JST
        await Assert.That(session.LocalStartTime).IsEqualTo(new TimeOnly(19, 0));
        await Assert.That(session.LocalEndTime).IsEqualTo(new TimeOnly(21, 0));
        await Assert.That(session.LocalStartMinuteOfDay).IsEqualTo(1140);
        await Assert.That(session.LocalEndMinuteOfDay).IsEqualTo(1260);
    }

    [Test]
    public async Task ReprojectLocalTimes_WithDifferentTimezone_OverwritesPreviousProjection()
    {
        var session = CreateEventSession();
        session.StartTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        session.EndTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        session.ReprojectLocalTimes("Europe/Brussels", _calculator);
        await Assert.That(session.LocalStartTime).IsEqualTo(new TimeOnly(12, 0));

        session.ReprojectLocalTimes("America/New_York", _calculator);
        // 10:00 UTC = 06:00 EDT
        await Assert.That(session.LocalStartTime).IsEqualTo(new TimeOnly(6, 0));
    }

    [Test]
    public async Task Reschedule_PreservesOtherProperties()
    {
        var session = CreateEventSession();
        session.Title = "Test Session";
        session.SortOrder = 5;
        session.MaxAudienceAttendees = 100;

        var start = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        session.Reschedule(start, end, "Europe/Brussels", _calculator);

        await Assert.That(session.Title).IsEqualTo("Test Session");
        await Assert.That(session.SortOrder).IsEqualTo(5);
        await Assert.That(session.MaxAudienceAttendees).IsEqualTo(100);
    }

    private static EventSession CreateEventSession()
    {
        return new EventSession
        {
            Event = CreateEvent(),
            Tenant = CreateTenant()
        };
    }

    private static global::Explore.Domain.Event CreateEvent()
    {
        return new global::Explore.Domain.Event
        {
            Title = "Event",
            Actor = new Actor
            {
                Pii = new ActorPii { DisplayName = "Actor" },
                ActorType = new ActorType { FullName = "User", MasterCode = "USER" },
                Tenant = CreateTenant()
            },
            Tenant = CreateTenant(),
            VisibilityType = new VisibilityType { MasterCode = "PUBLIC", FullName = "Public" },
            EventStatus = new EventStatus { MasterCode = "DRAFT", FullName = "Draft" },
            EventFormat = new EventFormat { MasterCode = "ONLINE", FullName = "Online" }
        };
    }

    private static Tenant CreateTenant()
    {
        return new Tenant
        {
            FullName = "Tenant",
            Slug = "tenant",
            TenantStatusId = 2,
            TenantStatus = new TenantStatus { Id = 2, MasterCode = "ACTIVE", FullName = "Active", IsActiveState = true }
        };
    }
}
