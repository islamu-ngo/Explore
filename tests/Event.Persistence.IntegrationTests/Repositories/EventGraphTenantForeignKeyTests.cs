// ABOUTME: PostgreSQL-backed tests for tenant-scoped composite FKs across the event graph.
// ABOUTME: Writes invalid rows directly so database constraints prove cross-tenant/event links are rejected.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventGraphTenantForeignKeyTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task EventSession_ShouldRejectEventFromDifferentTenant()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenantA = await SeedEventGraphAsync(context, "session-a");
        var tenantB = await SeedEventGraphAsync(context, "session-b");

        context.EventSessions.Add(new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = tenantB.EventId,
            Event = null!,
            TenantId = tenantA.TenantId,
            Tenant = null!,
            Title = "Cross Tenant Session",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            ConcurrencyStamp = Guid.NewGuid()
        });

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task EventSession_ShouldRejectDayFromDifferentEvent()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var scope = await SeedTenantActorAsync(context, "session-day");
        var eventA = await SeedEventAsync(context, scope, "Main Event");
        var eventB = await SeedEventAsync(context, scope, "Foreign Day Event");
        var foreignDay = await SeedEventDayAsync(context, eventB, DateOnly.FromDateTime(DateTime.UtcNow.Date));

        context.EventSessions.Add(new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventA.EventId,
            Event = null!,
            EventDayId = foreignDay.Id,
            EventDay = null,
            TenantId = scope.TenantId,
            Tenant = null!,
            Title = "Cross Event Day Session",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            ConcurrencyStamp = Guid.NewGuid()
        });

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task EventSession_ShouldRejectRoomFromDifferentLocation()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var scope = await SeedTenantActorAsync(context, "session-room");
        var @event = await SeedEventAsync(context, scope, "Room Event");
        var locationA = await SeedLocationAsync(context, scope.TenantId, "Room Event Location A");
        var locationB = await SeedLocationAsync(context, scope.TenantId, "Room Event Location B");
        var foreignRoom = await SeedLocationRoomAsync(context, scope.TenantId, locationB.Id, "Foreign Room");

        context.EventSessions.Add(new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = @event.EventId,
            Event = null!,
            LocationId = locationA.Id,
            Location = null,
            RoomId = foreignRoom.Id,
            Room = null,
            TenantId = scope.TenantId,
            Tenant = null!,
            Title = "Cross Location Room Session",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            ConcurrencyStamp = Guid.NewGuid()
        });

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task EventCategories_ShouldRejectCategoryFromDifferentTenant()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var tenantA = await SeedEventGraphAsync(context, "category-a");
        var tenantB = await SeedEventGraphAsync(context, "category-b");
        var foreignCategory = await SeedCategoryAsync(context, tenantB.TenantId, "foreign-category");

        context.EventCategories.Add(new EventCategories
        {
            Id = Guid.NewGuid(),
            EventId = tenantA.EventId,
            Event = null!,
            CategoryId = foreignCategory.Id,
            Category = null!,
            TenantId = tenantA.TenantId,
            Tenant = null!
        });

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task EventRegistration_ShouldRejectSessionFromDifferentEvent()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var scope = await SeedTenantActorAsync(context, "registration");
        var eventA = await SeedEventAsync(context, scope, "Registration Event");
        var eventB = await SeedEventAsync(context, scope, "Other Session Event");
        var foreignSession = await SeedEventSessionAsync(context, eventB, "Other Event Session");
        var intent = await SeedRegistrationIntentAsync(context, eventA, scope.UserId);

        context.EventRegistrations.Add(new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventA.EventId,
            Event = null!,
            UserId = scope.UserId,
            User = null!,
            EventSessionId = foreignSession.Id,
            EventSession = null!,
            EventRegistrationIntentId = intent.Id,
            EventRegistrationIntent = null,
            ApprovalStatusId = 1,
            ApprovalStatus = null,
            TenantId = scope.TenantId,
            Tenant = null!
        });

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task EventSessionGroupSession_ShouldRejectSessionFromDifferentEvent()
    {
        await fixture.ResetAsync();
        using var context = fixture.CreateDbContext();
        var scope = await SeedTenantActorAsync(context, "group-session");
        var eventA = await SeedEventAsync(context, scope, "Group Event");
        var eventB = await SeedEventAsync(context, scope, "Session Event");
        var group = await SeedEventSessionGroupAsync(context, eventA);
        var foreignSession = await SeedEventSessionAsync(context, eventB, "Foreign Group Session");

        context.EventSessionGroupSessions.Add(new EventSessionGroupSession
        {
            Id = Guid.NewGuid(),
            EventId = eventA.EventId,
            Event = null!,
            EventSessionGroupId = group.Id,
            EventSessionGroup = null!,
            EventSessionId = foreignSession.Id,
            EventSession = null!,
            IsPrimary = false,
            SortOrder = 1,
            TenantId = scope.TenantId,
            Tenant = null!
        });

        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    private static async Task<EventGraphScope> SeedEventGraphAsync(ExploreDbContext context, string slugPrefix)
    {
        var scope = await SeedTenantActorAsync(context, slugPrefix);
        return await SeedEventAsync(context, scope, $"{slugPrefix} Event");
    }

    private static async Task<TenantActorScope> SeedTenantActorAsync(ExploreDbContext context, string slugPrefix)
    {
        var tenant = new Tenant
        {
            FullName = $"Event Graph {slugPrefix}",
            Slug = $"event-graph-{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = 2,
            TenantStatus = null!
        };

        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}@example.com",
                FirstName = "Event",
                LastName = "Graph"
            }
        };

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = $"Event Graph Actor {slugPrefix}" },
            ActorTypeId = 1,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };

        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        return new TenantActorScope(tenant.Id, user.Id, actor.Id);
    }

    private static async Task<EventGraphScope> SeedEventAsync(
        ExploreDbContext context,
        TenantActorScope scope,
        string title)
    {
        var @event = new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            ActorId = scope.ActorId,
            Actor = null!,
            TenantId = scope.TenantId,
            Tenant = null!,
            EventStatusId = 1,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            TotalViews = 0,
            IsRegistrationRequired = false,
            ConcurrencyStamp = Guid.NewGuid()
        };

        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return new EventGraphScope(scope.TenantId, scope.UserId, scope.ActorId, @event.Id);
    }

    private static async Task<EventDay> SeedEventDayAsync(
        ExploreDbContext context,
        EventGraphScope scope,
        DateOnly localDate)
    {
        var day = new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = scope.EventId,
            Event = null!,
            LocalDate = localDate,
            Label = "Day 1",
            IsPublished = true,
            SortOrder = 1,
            AllowsDayScopeRegistration = true,
            TenantId = scope.TenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };

        context.EventDays.Add(day);
        await context.SaveChangesAsync();
        return day;
    }

    private static async Task<EventSession> SeedEventSessionAsync(
        ExploreDbContext context,
        EventGraphScope scope,
        string title)
    {
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = scope.EventId,
            Event = null!,
            TenantId = scope.TenantId,
            Tenant = null!,
            Title = title,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            ConcurrencyStamp = Guid.NewGuid()
        };

        context.EventSessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    private static async Task<EventSessionGroup> SeedEventSessionGroupAsync(
        ExploreDbContext context,
        EventGraphScope scope)
    {
        var group = new EventSessionGroup
        {
            Id = Guid.NewGuid(),
            EventId = scope.EventId,
            Event = null!,
            Name = "Main Track",
            SortOrder = 1,
            IsPublished = true,
            TenantId = scope.TenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };

        context.EventSessionGroups.Add(group);
        await context.SaveChangesAsync();
        return group;
    }

    private static async Task<EventRegistrationIntent> SeedRegistrationIntentAsync(
        ExploreDbContext context,
        EventGraphScope scope,
        Guid userId)
    {
        var intent = new EventRegistrationIntent
        {
            Id = Guid.NewGuid(),
            EventId = scope.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScopeId = 3,
            RegistrationScope = null!,
            ApprovalStatusId = 1,
            ApprovalStatus = null,
            TenantId = scope.TenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };

        context.EventRegistrationIntents.Add(intent);
        await context.SaveChangesAsync();
        return intent;
    }

    private static async Task<Category> SeedCategoryAsync(ExploreDbContext context, Guid tenantId, string masterCode)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            MasterCode = masterCode,
            FullName = $"Category {masterCode}",
            TenantId = tenantId,
            Tenant = null!
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task<Location> SeedLocationAsync(ExploreDbContext context, Guid tenantId, string fullName)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Country = "Belgium",
            City = "Brussels",
            Pii = new LocationPii
            {
                Address = "1 Test Street",
                Postcode = "1000"
            },
            TenantId = tenantId,
            Tenant = null!
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync();
        return location;
    }

    private static async Task<LocationRoom> SeedLocationRoomAsync(
        ExploreDbContext context,
        Guid tenantId,
        Guid locationId,
        string name)
    {
        var room = new LocationRoom
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            Location = null!,
            Name = name,
            SortOrder = 1,
            TenantId = tenantId,
            Tenant = null!,
            ConcurrencyStamp = Guid.NewGuid()
        };

        context.LocationRooms.Add(room);
        await context.SaveChangesAsync();
        return room;
    }

    private sealed record TenantActorScope(Guid TenantId, Guid UserId, Guid ActorId);

    private sealed record EventGraphScope(Guid TenantId, Guid UserId, Guid ActorId, Guid EventId);
}
