// ABOUTME: PostgreSQL coverage for immutable event-registration replacement rows.
// ABOUTME: Proves authoritative IDs, coverage continuity, session moves, and EF immutability enforcement.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventRegistrationCoverageReplacementTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task StatusReplacement_PreservesCoverageAndReturnsAuthoritativeReplacementId()
    {
        await fixture.ResetAsync();
        ReplacementScenario scenario = await SeedScenarioAsync();
        Guid occurrenceId = Guid.CreateVersion7();
        DateTimeOffset occurredAt = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

        EventRegistrationTransitionResult transition = await UpdateAsync(
            scenario,
            occurrenceId,
            occurredAt,
            registration =>
            {
                registration.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
                registration.CoverageEstablishedAt = new DateTime(
                    2020,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);
            });

        await using var verifyContext = fixture.CreateDbContext();
        EventRegistration[] rows = await verifyContext.EventRegistrations
            .IgnoreAllFilters("Replacement history verification requires active and soft-deleted rows.")
            .Where(registration => registration.EventRegistrationIntentId == scenario.IntentId)
            .OrderBy(registration => registration.IsDeleted)
            .ToArrayAsync();
        EventRegistration replacement = rows.Single(registration => !registration.IsDeleted);
        EventRegistration historical = rows.Single(registration => registration.IsDeleted);

        await Assert.That(transition.Changed).IsTrue();
        await Assert.That(transition.ChildTransitions).HasSingleItem();
        await Assert.That(transition.ChildTransitions[0].RegistrationId).IsEqualTo(occurrenceId);
        await Assert.That(replacement.Id).IsEqualTo(occurrenceId);
        await Assert.That(historical.Id).IsEqualTo(scenario.RegistrationId);
        await Assert.That(replacement.EventSessionId).IsEqualTo(scenario.SourceSessionId);
        await Assert.That(replacement.CoverageEstablishedAt).IsEqualTo(scenario.CoverageEstablishedAt);
        await Assert.That(replacement.CreatedAt).IsNotEqualTo(historical.CreatedAt);
    }

    [Test]
    public async Task SessionMove_StartsNewCoverageAtOccurrenceAndReturnsAuthoritativeReplacementId()
    {
        await fixture.ResetAsync();
        ReplacementScenario scenario = await SeedScenarioAsync();
        Guid occurrenceId = Guid.CreateVersion7();
        DateTimeOffset occurredAt = new(2026, 8, 5, 13, 15, 0, TimeSpan.Zero);

        EventRegistrationTransitionResult transition = await UpdateAsync(
            scenario,
            occurrenceId,
            occurredAt,
            registration => registration.EventSessionId = scenario.TargetSessionId);

        await using var verifyContext = fixture.CreateDbContext();
        EventRegistration[] rows = await verifyContext.EventRegistrations
            .IgnoreAllFilters("Replacement history verification requires active and soft-deleted rows.")
            .Where(registration => registration.EventRegistrationIntentId == scenario.IntentId)
            .OrderBy(registration => registration.IsDeleted)
            .ToArrayAsync();
        EventRegistration replacement = rows.Single(registration => !registration.IsDeleted);
        EventRegistration historical = rows.Single(registration => registration.IsDeleted);
        var capacities = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SourceSessionId || session.Id == scenario.TargetSessionId)
            .ToDictionaryAsync(session => session.Id, session => session.CurrentAudienceAttendees);

        await Assert.That(transition.Changed).IsTrue();
        await Assert.That(transition.ChildTransitions).HasSingleItem();
        await Assert.That(transition.ChildTransitions[0].RegistrationId).IsEqualTo(occurrenceId);
        await Assert.That(transition.ChildTransitions[0].EventSessionId).IsEqualTo(scenario.TargetSessionId);
        await Assert.That(replacement.Id).IsEqualTo(occurrenceId);
        await Assert.That(historical.Id).IsEqualTo(scenario.RegistrationId);
        await Assert.That(replacement.EventSessionId).IsEqualTo(scenario.TargetSessionId);
        await Assert.That(replacement.CoverageEstablishedAt).IsEqualTo(occurredAt.UtcDateTime);
        await Assert.That(replacement.CoverageEstablishedAt).IsNotEqualTo(scenario.CoverageEstablishedAt);
        await Assert.That(capacities[scenario.SourceSessionId]).IsEqualTo(0);
        await Assert.That(capacities[scenario.TargetSessionId]).IsEqualTo(1);
    }

    [Test]
    public async Task PersistedCoverageEstablishedAt_CannotBeMutatedInPlace()
    {
        await fixture.ResetAsync();
        ReplacementScenario scenario = await SeedScenarioAsync();

        await using var context = fixture.CreateDbContext();
        EventRegistration registration = await context.EventRegistrations
            .SingleAsync(item => item.Id == scenario.RegistrationId);
        registration.CoverageEstablishedAt = registration.CoverageEstablishedAt.AddMinutes(1);

        await Assert.That(async () => await context.SaveChangesAsync())
            .Throws<InvalidOperationException>();
    }

    private async Task<EventRegistrationTransitionResult> UpdateAsync(
        ReplacementScenario scenario,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        Action<EventRegistration> mutate)
    {
        await using var context = fixture.CreateDbContext();
        EventRegistration registration = await context.EventRegistrations
            .SingleAsync(item => item.Id == scenario.RegistrationId);
        mutate(registration);
        var repository = new EventRegistrationRepository(context);
        return await new EfCoreUnitOfWork(context).ExecuteSerializableAsync(
            cancellationToken => repository.UpdateAndAdjustCapacityAsync(
                registration,
                occurrenceId,
                occurredAt,
                EventRegistrationActorProvenance.Organizer,
                actorUserId: null,
                cancellationToken));
    }

    private async Task<ReplacementScenario> SeedScenarioAsync()
    {
        await using var context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = "Registration replacement tenant",
            Slug = $"registration-replacement-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"registration-replacement-{Guid.NewGuid():N}@example.test",
                FirstName = "Registration",
                LastName = "Replacement",
            },
            EmailVerified = true,
        };
        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = user.Id,
            Pii = new ActorPii { DisplayName = "Registration replacement actor" },
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Registration replacement event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            IsRegistrationRequired = true,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        EventSession source = NewSession(tenant.Id, @event.Id, "Source session", 9, currentAttendees: 1);
        EventSession target = NewSession(tenant.Id, @event.Id, "Target session", 11, currentAttendees: 0);
        context.EventSessions.AddRange(source, target);

        var intent = new EventRegistrationIntent
        {
            Id = Guid.CreateVersion7(),
            EventId = @event.Id,
            Event = null!,
            UserId = user.Id,
            User = null!,
            RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
            RegistrationScope = null!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Pending,
            ApprovalStatus = null,
            TenantId = tenant.Id,
            Tenant = null!,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.EventRegistrationIntents.Add(intent);

        DateTime coverageEstablishedAt = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var registration = new EventRegistration
        {
            Id = Guid.CreateVersion7(),
            EventId = @event.Id,
            Event = null!,
            UserId = user.Id,
            User = null!,
            EventSessionId = source.Id,
            EventSession = null!,
            EventRegistrationIntentId = intent.Id,
            EventRegistrationIntent = null,
            ApprovalStatusId = (int)ApprovalStatusEnum.Pending,
            ApprovalStatus = null,
            TenantId = tenant.Id,
            Tenant = null!,
            CoverageEstablishedAt = coverageEstablishedAt,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.EventRegistrations.Add(registration);
        await context.SaveChangesAsync();

        DateTime historicalCreatedAt = new(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        await context.EventRegistrations
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(item => item.Id == registration.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedAt, historicalCreatedAt));

        return new ReplacementScenario(
            intent.Id,
            registration.Id,
            source.Id,
            target.Id,
            coverageEstablishedAt);
    }

    private static EventSession NewSession(
        Guid tenantId,
        Guid eventId,
        string title,
        int startHour,
        int currentAttendees)
    {
        var session = new EventSession
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            Event = null!,
            Title = title,
            TenantId = tenantId,
            Tenant = null!,
            EventSessionStatusId = (int)EventSessionStatusEnum.Published,
            EventSessionStatus = null!,
            MaxAudienceAttendees = 10,
            CurrentAudienceAttendees = currentAttendees,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        session.Reschedule(
            new DateTimeOffset(2026, 8, 1, startHour, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, startHour + 1, 0, 0, TimeSpan.Zero),
            "UTC",
            new EventScheduleProjectionCalculator());
        return session;
    }

    private sealed record ReplacementScenario(
        Guid IntentId,
        Guid RegistrationId,
        Guid SourceSessionId,
        Guid TargetSessionId,
        DateTime CoverageEstablishedAt);
}
