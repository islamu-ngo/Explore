// ABOUTME: PostgreSQL integration tests for EventRegistrationRepository cancellation behavior.
// ABOUTME: Verifies cancellation remains atomic when Npgsql retry execution strategies are enabled.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventRegistrationRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CancelAndReleaseCapacityAsync_WithRetryingExecutionStrategy_CancelsRegistrationAndReleasesCapacity()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedApprovedRegistrationAsync(seedContext);
        await using var cancelContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(cancelContext);

        var cancelled = await repository.CancelAndReleaseCapacityAsync(scenario.RegistrationId, CancellationToken.None);

        await Assert.That(cancelled).IsTrue();

        await using var verifyContext = fixture.CreateDbContext();
        var registration = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .SingleAsync(r => r.Id == scenario.RegistrationId);
        var currentAttendees = await verifyContext.EventSessions
            .Where(s => s.Id == scenario.SessionId)
            .Select(s => s.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(registration.IsDeleted).IsTrue();
        await Assert.That(currentAttendees).IsEqualTo(0);
    }

    private ExploreDbContext CreateRetryingDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Persistence integration test retrying cancellation context.");
        return context;
    }

    private static async Task<RegistrationScenario> SeedApprovedRegistrationAsync(ExploreDbContext context)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = "Registration Cancellation Tenant",
            Slug = "registration-cancel-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Pii = new UserPii
            {
                Email = $"registration-cancel-{Guid.NewGuid():N}@example.com",
                FirstName = "Registration",
                LastName = "Cancel"
            }
        };
        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Pii = new ActorPii { DisplayName = "Registration Cancellation Actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventId = Guid.NewGuid();
        var @event = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Registration Cancellation Event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            TotalViews = 0,
            IsRegistrationRequired = true
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Title = "Registration Cancellation Session",
            TenantId = tenant.Id,
            Tenant = null!,
            MaxAudienceAttendees = 10,
            CurrentAudienceAttendees = 1
        };
        session.Reschedule(
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
            "UTC",
            new EventScheduleProjectionCalculator());
        context.EventSessions.Add(session);

        var registration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            UserId = user.Id,
            User = null!,
            EventSessionId = session.Id,
            EventSession = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved
        };
        context.EventRegistrations.Add(registration);
        await context.SaveChangesAsync();

        return new RegistrationScenario(registration.Id, session.Id);
    }

    private sealed record RegistrationScenario(Guid RegistrationId, Guid SessionId);
}
