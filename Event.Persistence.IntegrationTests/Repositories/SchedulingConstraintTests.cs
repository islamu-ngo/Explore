// ABOUTME: Persistence integration tests for DB-enforced constraints on scheduling entities.
// ABOUTME: Tests unique constraints (EventDay date, LocationRoom name) and check constraints (capacity, end > start).

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
public class SchedulingConstraintTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public SchedulingConstraintTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task EventDay_ShouldRejectDuplicateLocalDate_ForSameEvent()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, @event) = await SetupEventAsync(context);
        var calculator = new EventScheduleProjectionCalculator();

        var day1 = new EventDay
        {
            EventId = @event.Id,
            Event = @event,
            LocalDate = new DateOnly(2026, 7, 1),
            Label = "First Day",
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = tenant
        };
        context.EventDays.Add(day1);
        await context.SaveChangesAsync();

        using var context2 = _fixture.CreateDbContext();
        var day2 = new EventDay
        {
            EventId = @event.Id,
            Event = null!,
            LocalDate = new DateOnly(2026, 7, 1),
            Label = "Duplicate Date",
            SortOrder = 2,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context2.EventDays.Add(day2);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context2.SaveChangesAsync());
    }

    [Test]
    public async Task EventDay_ShouldAllowSameLocalDate_ForDifferentEvents()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, @event) = await SetupEventAsync(context);
        var event2 = new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = "Second Event",
            EventTypeId = 1,
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            ActorId = @event.ActorId,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            EventStatusId = 1,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            TotalViews = 0,
            IsRegistrationRequired = false
        };
        context.Events.Add(event2);
        await context.SaveChangesAsync();

        var repository = new EventDayRepository(context);

        var day1 = new EventDay
        {
            EventId = @event.Id,
            Event = @event,
            LocalDate = new DateOnly(2026, 7, 1),
            Label = "Event 1 Day",
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = tenant
        };
        await repository.Create(day1);

        var day2 = new EventDay
        {
            EventId = event2.Id,
            Event = event2,
            LocalDate = new DateOnly(2026, 7, 1),
            Label = "Event 2 Day",
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = tenant
        };

        var result = await repository.Create(day2);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task LocationRoom_ShouldRejectDuplicateName_ForSameLocation()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, location) = await SetupLocationAsync(context);

        var room1 = new LocationRoom
        {
            LocationId = location.Id,
            Location = null!,
            Name = "Main Hall",
            Capacity = 200,
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context.LocationRooms.Add(room1);
        await context.SaveChangesAsync();

        using var context2 = _fixture.CreateDbContext();
        var room2 = new LocationRoom
        {
            LocationId = location.Id,
            Location = null!,
            Name = "Main Hall",
            Capacity = 100,
            SortOrder = 2,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context2.LocationRooms.Add(room2);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context2.SaveChangesAsync());
    }

    [Test]
    public async Task LocationRoom_ShouldRejectNegativeCapacity()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, location) = await SetupLocationAsync(context);

        var room = new LocationRoom
        {
            LocationId = location.Id,
            Location = null!,
            Name = "Bad Room",
            Capacity = -5,
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context.LocationRooms.Add(room);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task LocationRoom_ShouldAllowNullCapacity()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, location) = await SetupLocationAsync(context);
        var repository = new LocationRoomRepository(context);

        var room = new LocationRoom
        {
            LocationId = location.Id,
            Location = location,
            Name = "Open Space",
            Capacity = null,
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = tenant
        };

        var result = await repository.Create(room);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task EventAgendaItem_ShouldRejectEndTimeBeforeStartTime()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, @event) = await SetupEventAsync(context);

        var item = new EventAgendaItem
        {
            EventId = @event.Id,
            Event = null!,
            Title = "Bad Item",
            StartTime = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context.EventAgendaItems.Add(item);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task EventAgendaItem_ShouldAcceptValidTimeRange()
    {
        using var context = _fixture.CreateDbContext();
        var (tenant, @event) = await SetupEventAsync(context);
        var calculator = new EventScheduleProjectionCalculator();
        var repository = new EventAgendaItemRepository(context);

        var item = new EventAgendaItem
        {
            EventId = @event.Id,
            Event = @event,
            Title = "Valid Item",
            StartTime = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
            SortOrder = 1,
            TenantId = tenant.Id,
            Tenant = tenant
        };
        item.ReprojectLocalTimes("UTC", calculator);

        var result = await repository.Create(item);
        await Assert.That(result).IsNotNull();
    }

    private static async Task<(Tenant tenant, Explore.Domain.Event @event)> SetupEventAsync(ExploreDbContext context)
    {
        var activeStatus = await context.TenantStatuses.FindAsync(2);
        var tenant = new Tenant
        {
            FullName = "Constraint Test Tenant",
            Slug = "constraint-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = activeStatus?.Id ?? 2,
            TenantStatus = activeStatus!
        };
        context.Tenants.Add(tenant);

        var user = new User { Pii = new UserPii { Email = $"constraint-{Guid.NewGuid():N}@example.com", FirstName = "Test", LastName = "User" } };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = "Constraint Actor" },
            ActorTypeId = 1,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = "Constraint Test Event",
            EventTypeId = 1,
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            EventStatusId = 1,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            TotalViews = 0,
            IsRegistrationRequired = false
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return (tenant, @event);
    }

    private static async Task<(Tenant tenant, Location location)> SetupLocationAsync(ExploreDbContext context)
    {
        var (tenant, @event) = await SetupEventAsync(context);

        var location = new Location
        {
            FullName = "Constraint Test Venue",
            Country = "BE",
            City = "Brussels",
            Pii = new LocationPii { Address = "123 Test St", Postcode = "1000" },
            TenantId = tenant.Id,
            Tenant = tenant
        };
        context.Locations.Add(location);
        await context.SaveChangesAsync();

        return (tenant, location);
    }
}
