// ABOUTME: PostgreSQL integration tests for EventRegistrationIntentRepository capacity and duplicate safety.
// ABOUTME: Verifies serializable registration creation keeps session capacity counters correct under concurrency and rollback.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services;
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
public sealed class EventRegistrationIntentRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_ConcurrentCapacityOneRegistrations_CreatesSingleApprovedRow()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 6, sessionCapacity: 1);

        var results = await Task.WhenAll(scenario.UserIds.Select(userId => CreateRegistrationAsync(scenario, userId)));

        await using var verifyContext = fixture.CreateDbContext();
        var registrations = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .Where(registration => registration.EventSessionId == scenario.SessionId)
            .ToListAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var approvedCount = registrations.Count(registration => registration.ApprovalStatusId == (int)ApprovalStatusEnum.Approved);
        var waitlistedCount = registrations.Count(registration => registration.ApprovalStatusId == (int)ApprovalStatusEnum.Waitlisted);

        await Assert.That(results.Count(result => !result.HasWaitlistedSessions)).IsEqualTo(1);
        await Assert.That(results.Count(result => result.HasWaitlistedSessions)).IsEqualTo(scenario.UserIds.Count - 1);
        await Assert.That(registrations.Count).IsEqualTo(scenario.UserIds.Count);
        await Assert.That(approvedCount).IsEqualTo(1);
        await Assert.That(waitlistedCount).IsEqualTo(scenario.UserIds.Count - 1);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)RegistrationScopeEnum.Event)]
    [Arguments((int)RegistrationScopeEnum.Day)]
    [Arguments((int)RegistrationScopeEnum.SessionSelection)]
    public async Task CreateWithChildrenAndCapacityAsyncAppliesCapacityForEveryRegistrationScope(int scopeId)
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 2, sessionCapacity: 1);
        var scope = (RegistrationScopeEnum)scopeId;

        var first = await CreateRegistrationAsync(scenario, scenario.UserIds[0], scope);
        var second = await CreateRegistrationAsync(scenario, scenario.UserIds[1], scope);

        await using var verifyContext = fixture.CreateDbContext();
        var intentStates = await verifyContext.EventRegistrationIntents
            .Where(intent => intent.EventId == scenario.EventId)
            .OrderBy(intent => intent.UserId)
            .Select(intent => intent.ApprovalStatusId)
            .ToArrayAsync();
        var childStates = await verifyContext.EventRegistrations
            .Where(registration => registration.EventSessionId == scenario.SessionId)
            .OrderBy(registration => registration.UserId)
            .Select(registration => registration.ApprovalStatusId)
            .ToArrayAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(first.HasWaitlistedSessions).IsFalse();
        await Assert.That(second.HasWaitlistedSessions).IsTrue();
        await Assert.That(intentStates.Count(status => status == (int)ApprovalStatusEnum.Approved)).IsEqualTo(1);
        await Assert.That(intentStates.Count(status => status == (int)ApprovalStatusEnum.Waitlisted)).IsEqualTo(1);
        await Assert.That(childStates.Count(status => status == (int)ApprovalStatusEnum.Approved)).IsEqualTo(1);
        await Assert.That(childStates.Count(status => status == (int)ApprovalStatusEnum.Waitlisted)).IsEqualTo(1);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateSessionSelectionIntent_ReturnsExistingIntent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationAsync(scenario, userId);
        await using var duplicateContext = CreateRetryingDbContext();
        var duplicateRepository = new EventRegistrationIntentRepository(duplicateContext);

        var duplicate = await duplicateRepository.CreateWithChildrenAndCapacityAsync(
            NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection),
            [NewRegistrationChild(scenario, userId)],
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventSessionId == scenario.SessionId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(currentAttendees).IsEqualTo(1);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(intentCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateSessionSelectionIntent_DoesNotCreateDuplicateEmailDispatchRow()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationWithEmailDispatchAsync(scenario, userId);
        await using var duplicateContext = CreateRetryingDbContext();
        var duplicateRepository = new EventRegistrationIntentRepository(duplicateContext);
        var duplicateIntent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        var duplicateOutbox = NewRegistrationConfirmationDispatch(scenario, userId, duplicateIntent.Id);

        var duplicate = await duplicateRepository.CreateWithChildrenAndCapacityAsync(
            duplicateIntent,
            [NewRegistrationChild(scenario, userId)],
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            CancellationToken.None,
            duplicateOutbox);

        await using var verifyContext = fixture.CreateDbContext();
        var outboxRows = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .Where(dispatch => dispatch.TenantId == scenario.TenantId
                && dispatch.EventId == scenario.EventId
                && dispatch.UserId == userId
                && dispatch.Kind == EmailDispatchKind.RegistrationConfirmation)
            .Select(dispatch => new
            {
                dispatch.SourceType,
                dispatch.SourceId,
                dispatch.RegistrationIntentId,
                dispatch.Status
            })
            .ToListAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventSessionId == scenario.SessionId);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(outboxRows.Count).IsEqualTo(1);
        var outboxRow = outboxRows.Single();
        await Assert.That(outboxRow.SourceType).IsEqualTo(EventLifecycleEmailOutboxFactory.RegistrationIntentSourceType);
        await Assert.That(outboxRow.SourceId).IsEqualTo(first.Intent.Id);
        await Assert.That(outboxRow.RegistrationIntentId).IsEqualTo(first.Intent.Id);
        await Assert.That(outboxRow.Status).IsEqualTo(EmailDispatchStatus.Pending);
        await Assert.That(outboxRow.SourceId).IsNotEqualTo(duplicateIntent.Id);
        await Assert.That(currentAttendees).IsEqualTo(1);
        await Assert.That(registrationCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateEventScopeIntent_ReturnsExistingIntent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationAsync(scenario, userId, RegistrationScopeEnum.Event);
        await using var duplicateContext = CreateRetryingDbContext();
        var duplicateRepository = new EventRegistrationIntentRepository(duplicateContext);

        var duplicate = await duplicateRepository.CreateWithChildrenAndCapacityAsync(
            NewIntent(scenario, userId, RegistrationScopeEnum.Event),
            [NewRegistrationChild(scenario, userId, scenario.SecondarySessionId)],
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var attendeeCounts = await GetSessionAttendeeCountsAsync(verifyContext, scenario);
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId && registration.UserId == userId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId
                && registrationIntent.UserId == userId
                && registrationIntent.RegistrationScopeId == (int)RegistrationScopeEnum.Event);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(attendeeCounts[scenario.SessionId]).IsEqualTo(1);
        await Assert.That(attendeeCounts[scenario.SecondarySessionId]).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(intentCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateDayScopeIntent_ReturnsExistingIntent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationAsync(scenario, userId, RegistrationScopeEnum.Day);
        await using var duplicateContext = CreateRetryingDbContext();
        var duplicateRepository = new EventRegistrationIntentRepository(duplicateContext);

        var duplicate = await duplicateRepository.CreateWithChildrenAndCapacityAsync(
            NewIntent(scenario, userId, RegistrationScopeEnum.Day),
            [NewRegistrationChild(scenario, userId, scenario.SecondarySessionId)],
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var attendeeCounts = await GetSessionAttendeeCountsAsync(verifyContext, scenario);
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId && registration.UserId == userId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId
                && registrationIntent.UserId == userId
                && registrationIntent.RegistrationScopeId == (int)RegistrationScopeEnum.Day
                && registrationIntent.SelectedEventDayId == scenario.EventDayId);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(attendeeCounts[scenario.SessionId]).IsEqualTo(1);
        await Assert.That(attendeeCounts[scenario.SecondarySessionId]).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(intentCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_WhenChildUniqueConstraintFails_RollsBackCapacityCounter()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        await using var registrationContext = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(registrationContext);
        var userId = scenario.UserIds.Single();
        var intent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        var duplicateChildren = new[]
        {
            NewRegistrationChild(scenario, userId),
            NewRegistrationChild(scenario, userId)
        };

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await repository.CreateWithChildrenAndCapacityAsync(
                intent,
                duplicateChildren,
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                CancellationToken.None);
        });

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventSessionId == scenario.SessionId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId);

        await Assert.That(currentAttendees).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(0);
        await Assert.That(intentCount).IsEqualTo(0);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_WhenChildSessionBelongsToDifferentEvent_RejectsBeforeCapacityCounter()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var otherScenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        await using var registrationContext = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(registrationContext);
        var userId = scenario.UserIds.Single();
        var intent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        var crossEventChild = NewRegistrationChild(scenario, userId, otherScenario.SessionId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await repository.CreateWithChildrenAndCapacityAsync(
                intent,
                [crossEventChild],
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                CancellationToken.None);
        });

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var otherCurrentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == otherScenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId
                || registration.EventSessionId == otherScenario.SessionId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId);

        await Assert.That(exception.Message).Contains("does not belong to the registration intent tenant and event");
        await Assert.That(currentAttendees).IsEqualTo(0);
        await Assert.That(otherCurrentAttendees).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(0);
        await Assert.That(intentCount).IsEqualTo(0);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task GetRegisteredUserFanoutBatchAsyncReturnsOnlyLiveApprovalStates()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 7, sessionCapacity: 10);
        var statusCases = new (int StatusId, bool IsDeleted)[]
        {
            ((int)ApprovalStatusEnum.Pending, false),
            ((int)ApprovalStatusEnum.Approved, false),
            ((int)ApprovalStatusEnum.Waitlisted, false),
            ((int)ApprovalStatusEnum.Rejected, false),
            ((int)ApprovalStatusEnum.Cancelled, false),
            ((int)ApprovalStatusEnum.Revoked, false),
            ((int)ApprovalStatusEnum.Approved, true)
        };

        var intents = scenario.UserIds
            .Zip(statusCases)
            .Select(item =>
            {
                var intent = NewIntent(scenario, item.First, RegistrationScopeEnum.Event);
                intent.ApprovalStatusId = item.Second.StatusId;
                intent.IsDeleted = item.Second.IsDeleted;
                return intent;
            })
            .ToArray();
        seedContext.EventRegistrationIntents.AddRange(intents);
        await seedContext.SaveChangesAsync();

        await using var queryContext = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(queryContext);
        var userIds = await repository.GetRegisteredUserFanoutBatchAsync(
            scenario.TenantId,
            scenario.EventId,
            afterUserId: null,
            pageSize: 20,
            CancellationToken.None);

        await Assert.That(userIds.ToHashSet().SetEquals(scenario.UserIds.Take(3))).IsTrue();
    }

    private async Task<EventRegistrationIntentCreationResult> CreateRegistrationAsync(
        RegistrationScenario scenario,
        Guid userId,
        RegistrationScopeEnum scope = RegistrationScopeEnum.SessionSelection)
    {
        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(context);

        return await repository.CreateWithChildrenAndCapacityAsync(
            NewIntent(scenario, userId, scope),
            [NewRegistrationChild(scenario, userId)],
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            CancellationToken.None);
    }

    private async Task<EventRegistrationIntentCreationResult> CreateRegistrationWithEmailDispatchAsync(
        RegistrationScenario scenario,
        Guid userId)
    {
        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(context);
        var intent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        var outbox = NewRegistrationConfirmationDispatch(scenario, userId, intent.Id);

        return await repository.CreateWithChildrenAndCapacityAsync(
            intent,
            [NewRegistrationChild(scenario, userId)],
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            CancellationToken.None,
            outbox);
    }

    private ExploreDbContext CreateRetryingDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Persistence integration test concurrent registration context.");
        return context;
    }

    private static async Task<RegistrationScenario> SeedRegistrationScenarioAsync(
        ExploreDbContext context,
        int userCount,
        int sessionCapacity)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = "Registration Intent Tenant",
            Slug = "registration-intent-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var users = Enumerable.Range(0, userCount + 1)
            .Select(index => NewUser($"registration-intent-{index}"))
            .ToList();

        context.Tenants.Add(tenant);
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Pii = new ActorPii { DisplayName = "Registration Intent Actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            UserId = users[0].Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventId = Guid.NewGuid();
        var @event = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Registration Intent Event",
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

        var eventDay = new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            LocalDate = new DateOnly(2026, 8, 1),
            Label = "Registration Intent Day",
            IsPublished = true,
            SortOrder = 0,
            AllowsDayScopeRegistration = true,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context.EventDays.Add(eventDay);
        await context.SaveChangesAsync();

        var session = NewSession(
            tenant.Id,
            eventId,
            eventDay.Id,
            "Capacity Limited Session",
            sessionCapacity,
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
        var secondarySession = NewSession(
            tenant.Id,
            eventId,
            eventDay.Id,
            "Alternate Capacity Limited Session",
            sessionCapacity,
            new DateTimeOffset(2026, 8, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));
        context.EventSessions.AddRange(session, secondarySession);
        await context.SaveChangesAsync();

        return new RegistrationScenario(
            tenant.Id,
            eventId,
            eventDay.Id,
            session.Id,
            secondarySession.Id,
            users.Skip(1).Select(user => user.Id).ToList());
    }

    private static EventSession NewSession(
        Guid tenantId,
        Guid eventId,
        Guid eventDayId,
        string title,
        int sessionCapacity,
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            EventDayId = eventDayId,
            EventDay = null,
            Title = title,
            TenantId = tenantId,
            Tenant = null!,
            MaxAudienceAttendees = sessionCapacity,
            CurrentAudienceAttendees = 0
        };
        session.Reschedule(startTime, endTime, "UTC", new EventScheduleProjectionCalculator());

        return session;
    }

    private static User NewUser(string prefix)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Pii = new UserPii
            {
                Email = $"{prefix}-{Guid.NewGuid():N}@example.com",
                FirstName = "Registration",
                LastName = "Intent"
            }
        };
    }

    private static EventRegistrationIntent NewIntent(
        RegistrationScenario scenario,
        Guid userId,
        RegistrationScopeEnum scope)
    {
        return new EventRegistrationIntent
        {
            Id = Guid.NewGuid(),
            EventId = scenario.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScopeId = (int)scope,
            RegistrationScope = null!,
            SelectedEventDayId = scope == RegistrationScopeEnum.Day ? scenario.EventDayId : null,
            SelectedEventDay = null,
            TenantId = scenario.TenantId,
            Tenant = null!
        };
    }

    private static EventRegistration NewRegistrationChild(
        RegistrationScenario scenario,
        Guid userId,
        Guid? sessionId = null)
    {
        return new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = scenario.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            EventSessionId = sessionId ?? scenario.SessionId,
            EventSession = null!,
            TenantId = scenario.TenantId,
            Tenant = null!
        };
    }

    private static EmailDispatchOutbox NewRegistrationConfirmationDispatch(
        RegistrationScenario scenario,
        Guid userId,
        Guid registrationIntentId)
    {
        return new EmailDispatchOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = scenario.TenantId,
            Tenant = null!,
            Kind = EmailDispatchKind.RegistrationConfirmation,
            SourceType = EventLifecycleEmailOutboxFactory.RegistrationIntentSourceType,
            SourceId = registrationIntentId,
            EventId = scenario.EventId,
            Event = null,
            RegistrationIntentId = registrationIntentId,
            RegistrationIntent = null,
            UserId = userId,
            User = null,
            RecipientEmail = $"registration-intent-{userId:N}@example.com",
            Subject = "Registration received",
            PlainTextBody = "Registration received.",
            HtmlBody = "<p>Registration received.</p>",
            CorrelationId = registrationIntentId.ToString()
        };
    }

    private static async Task<Dictionary<Guid, int?>> GetSessionAttendeeCountsAsync(
        ExploreDbContext context,
        RegistrationScenario scenario)
    {
        return await context.EventSessions
            .Where(session => session.Id == scenario.SessionId || session.Id == scenario.SecondarySessionId)
            .Select(session => new { session.Id, session.CurrentAudienceAttendees })
            .ToDictionaryAsync(session => session.Id, session => session.CurrentAudienceAttendees);
    }

    private sealed record RegistrationScenario(
        Guid TenantId,
        Guid EventId,
        Guid EventDayId,
        Guid SessionId,
        Guid SecondarySessionId,
        IReadOnlyList<Guid> UserIds);
}
