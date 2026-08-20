// ABOUTME: SQLite portability regression tests for EventSessionRepository query translation boundaries.
// ABOUTME: Proves nullable DateTimeOffset schedule ordering keeps StartTime semantics without server-side SQLite ordering.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Explore.Persistence.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests.Repositories;

public sealed class EventSessionRepositorySqliteTests
{
    [Test]
    public async Task GetSessionsByEvent_OnSqlite_ReturnsStartTimeAscending()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        Guid tenantId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateContext(connection, tenantId);
        await context.Database.EnsureCreatedAsync();
        await LookupTableSeeder.SeedAsync(context);
        DomainEvent @event = await SeedEventAsync(context, tenantId);
        DateTimeOffset later = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset earlier = new(2026, 8, 15, 9, 0, 0, TimeSpan.Zero);
        context.EventSessions.AddRange(
            CreateSession(@event, "Later", later),
            CreateSession(@event, "Earlier", earlier));
        await context.SaveChangesAsync();

        var repository = new EventSessionRepository(context);
        List<EventSession> sessions = await repository.GetSessionsByEvent(@event.Id);

        await Assert.That(sessions).Count().IsEqualTo(2);
        await Assert.That(sessions[0].StartTime).IsEqualTo(earlier);
        await Assert.That(sessions[1].StartTime).IsEqualTo(later);
    }

    private static ExploreDbContext CreateContext(SqliteConnection connection, Guid tenantId) =>
        new(new DbContextOptionsBuilder<ExploreDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention()
            .Options)
        {
            TenantContext = new FixedTenantContext(tenantId),
            CurrentUserService = new FixedCurrentUser()
        };

    private static async Task<DomainEvent> SeedEventAsync(ExploreDbContext context, Guid tenantId)
    {
        var tenant = new Tenant { Id = tenantId, FullName = "Session SQLite Tenant", Slug = $"session-sqlite-{Guid.NewGuid():N}", TenantStatusId = (int)TenantStatusEnum.Active, TenantStatus = null! };
        var user = new User { Id = Guid.CreateVersion7(), Pii = new UserPii { Email = $"session-sqlite-{Guid.NewGuid():N}@example.test", FirstName = "Session", LastName = "Owner" } };
        var actor = new Actor { Id = Guid.CreateVersion7(), Pii = new ActorPii { DisplayName = "Session SQLite Actor" }, ActorTypeId = (int)ActorTypeEnum.User, ActorType = null!, UserId = user.Id };
        var @event = new DomainEvent(EventStatusEnum.Draft)
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            Title = "Session SQLite Event",
            EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated,
            ActorId = actor.Id,
            Actor = null!,
            OrganizerActorId = actor.Id,
            EventTypeId = 1,
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            TotalViews = 0,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        context.AddRange(tenant, user, actor, @event);
        await context.SaveChangesAsync();
        return @event;
    }

    private static EventSession CreateSession(DomainEvent @event, string title, DateTimeOffset startTime) => new(EventSessionStatusEnum.Published)
    {
        Id = Guid.CreateVersion7(),
        EventId = @event.Id,
        Event = null!,
        TenantId = @event.TenantId,
        Tenant = null!,
        Title = title,
        StartTime = startTime,
        EndTime = startTime.AddHours(1),
        RegistrationModeId = (int)RegistrationModeEnum.Open,
        ConcurrencyStamp = Guid.CreateVersion7()
    };

    private sealed class FixedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class FixedCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }
}
