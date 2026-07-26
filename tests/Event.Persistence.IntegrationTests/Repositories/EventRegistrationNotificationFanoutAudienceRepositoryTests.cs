// ABOUTME: PostgreSQL coverage for deterministic event and session attendee fanout cohorts.
// ABOUTME: Proves cutoff, live-status, immutable coverage, deduplication, and compound cursor rules.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventRegistrationNotificationFanoutAudienceRepositoryTests(
    PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task EventAudience_UsesParentCutoffAndCurrentLiveStatusMatrix()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        AudienceScenario scenario = await SeedScenarioAsync(context, "event-audience");
        DateTime cutoff = Utc(2026, 8, 1, 12);

        Guid pending = AddMember(context, scenario, "pending", ApprovalStatusEnum.Pending, cutoff.AddMinutes(-3));
        Guid approved = AddMember(context, scenario, "approved", ApprovalStatusEnum.Approved, cutoff.AddMinutes(-2));
        Guid waitlisted = AddMember(context, scenario, "waitlisted", ApprovalStatusEnum.Waitlisted, cutoff);
        AddMember(context, scenario, "rejected", ApprovalStatusEnum.Rejected, cutoff.AddMinutes(-5));
        AddMember(context, scenario, "cancelled", ApprovalStatusEnum.Cancelled, cutoff.AddMinutes(-5));
        AddMember(context, scenario, "revoked", ApprovalStatusEnum.Revoked, cutoff.AddMinutes(-5));
        AddMember(context, scenario, "deleted", ApprovalStatusEnum.Approved, cutoff.AddMinutes(-5), parentDeleted: true);
        Guid late = AddMember(context, scenario, "late", ApprovalStatusEnum.Approved, cutoff.AddMilliseconds(1));
        AddMember(
            context,
            scenario,
            "inactive-membership",
            ApprovalStatusEnum.Approved,
            cutoff.AddMinutes(-5),
            membershipStatus: TenantUserStatusEnum.Suspended,
            membershipDeleted: true);
        AddMember(
            context,
            scenario,
            "deleted-user",
            ApprovalStatusEnum.Approved,
            cutoff.AddMinutes(-5),
            userDeleted: true);
        await context.SaveChangesAsync();
        await SetIntentCreatedAtAsync(context, late, cutoff.AddMilliseconds(1));

        var repository = new EventRegistrationIntentRepository(context);
        IReadOnlyList<NotificationFanoutAudienceMember> audience =
            await repository.GetNotificationFanoutAudienceBatchAsync(
                scenario.TenantId,
                scenario.EventId,
                sessionId: null,
                cutoff,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
                after: null,
                pageSize: 50,
                CancellationToken.None);
        IReadOnlyList<NotificationFanoutAudienceMember> requiredModerationAudience =
            await repository.GetNotificationFanoutAudienceBatchAsync(
                scenario.TenantId,
                scenario.EventId,
                sessionId: null,
                cutoff,
                (int)NotificationDeliveryPolicyEnum.ModerationAvailabilityRequired,
                after: null,
                pageSize: 50,
                CancellationToken.None);

        await Assert.That(ReadSortedAudienceLabels(context, audience))
            .IsEqualTo("approved,pending,waitlisted");
        await Assert.That(audience.Select(member => member.UserId).ToHashSet())
            .IsEquivalentTo(new HashSet<Guid> { pending, approved, waitlisted });
        await Assert.That(requiredModerationAudience.Select(member => member.UserId).ToHashSet())
            .IsEquivalentTo(new HashSet<Guid> { pending, approved, waitlisted });
        await Assert.That(audience.All(member => member.FirstEligibleRegistrationCreatedAt <= cutoff)).IsTrue();
    }

    [Test]
    public async Task SessionAudience_RequiresLiveParentAndTargetChildEstablishedBeforeCutoff()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        AudienceScenario scenario = await SeedScenarioAsync(context, "session-audience");
        DateTime cutoff = Utc(2026, 8, 1, 12);

        Guid eventScope = AddMemberWithChild(
            context, scenario, "event", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Pending, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-2), cutoff.AddMinutes(-30));
        Guid dayScope = AddMemberWithChild(
            context, scenario, "day", RegistrationScopeEnum.Day,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Waitlisted,
            cutoff.AddHours(-2), cutoff.AddMinutes(-20));
        Guid sessionScope = AddMemberWithChild(
            context, scenario, "session", RegistrationScopeEnum.SessionSelection,
            ApprovalStatusEnum.Waitlisted, ApprovalStatusEnum.Pending,
            cutoff.AddHours(-2), cutoff.AddMinutes(-10));
        Guid duplicateHistory = AddMemberWithChild(
            context, scenario, "duplicate", RegistrationScopeEnum.SessionSelection,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-2), cutoff.AddMinutes(-5), addDeletedDuplicate: true);

        Guid lateParent = AddMemberWithChild(
            context, scenario, "late-parent", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddMilliseconds(1), cutoff.AddHours(-1));
        AddMemberWithChild(
            context, scenario, "late-child", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMilliseconds(1));
        AddMemberWithChild(
            context, scenario, "rejected-parent", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Rejected, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMinutes(-10));
        AddMemberWithChild(
            context, scenario, "rejected-child", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Rejected,
            cutoff.AddHours(-1), cutoff.AddMinutes(-10));
        AddMemberWithChild(
            context, scenario, "deleted-parent", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMinutes(-10), parentDeleted: true);
        AddMemberWithChild(
            context, scenario, "deleted-child", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMinutes(-10), childDeleted: true);
        AddMemberWithChild(
            context, scenario, "wrong-session", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMinutes(-10), targetSession: false);
        AddPartiallyCancelledMember(context, scenario, cutoff);
        await context.SaveChangesAsync();
        await SetIntentCreatedAtAsync(context, lateParent, cutoff.AddMilliseconds(1));

        var repository = new EventRegistrationIntentRepository(context);
        IReadOnlyList<NotificationFanoutAudienceMember> audience =
            await repository.GetNotificationFanoutAudienceBatchAsync(
                scenario.TenantId,
                scenario.EventId,
                scenario.TargetSessionId,
                cutoff,
                (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
                after: null,
                pageSize: 100,
                CancellationToken.None);

        await Assert.That(ReadSortedAudienceLabels(context, audience))
            .IsEqualTo("day,duplicate,event,session");
        await Assert.That(audience.Select(member => member.UserId).ToHashSet())
            .IsEquivalentTo(new HashSet<Guid> { eventScope, dayScope, sessionScope, duplicateHistory });
        await Assert.That(audience.Count(member => member.UserId == duplicateHistory)).IsEqualTo(1);
        await Assert.That(audience.Single(member => member.UserId == duplicateHistory)
            .FirstEligibleRegistrationCreatedAt).IsEqualTo(cutoff.AddMinutes(-5));
    }

    [Test]
    public async Task ReminderAudience_RequiresApprovedParentAndApprovedTargetChild()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        AudienceScenario scenario = await SeedScenarioAsync(context, "reminder-audience");
        DateTime cutoff = Utc(2026, 8, 1, 12);

        Guid approved = AddMemberWithChild(
            context, scenario, "approved", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMinutes(-30));
        AddMemberWithChild(
            context, scenario, "pending-parent", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Pending, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), cutoff.AddMinutes(-30));
        AddMemberWithChild(
            context, scenario, "pending-child", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Pending,
            cutoff.AddHours(-1), cutoff.AddMinutes(-30));
        AddMemberWithChild(
            context, scenario, "waitlisted-child", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Waitlisted,
            cutoff.AddHours(-1), cutoff.AddMinutes(-30));
        await context.SaveChangesAsync();

        var repository = new EventRegistrationIntentRepository(context);
        IReadOnlyList<NotificationFanoutAudienceMember> audience =
            await repository.GetNotificationFanoutAudienceBatchAsync(
                scenario.TenantId,
                scenario.EventId,
                scenario.TargetSessionId,
                cutoff,
                (int)NotificationDeliveryPolicyEnum.ReminderOptional,
                after: null,
                pageSize: 50,
                CancellationToken.None);

        await Assert.That(audience.Select(member => member.UserId)).IsEquivalentTo([approved]);
    }

    [Test]
    public async Task AudiencePaging_UsesTimestampThenUserCompoundCursorWithoutDuplicatesOrSkips()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        AudienceScenario scenario = await SeedScenarioAsync(context, "audience-cursor");
        DateTime cutoff = Utc(2026, 8, 1, 12);
        DateTime first = cutoff.AddMinutes(-30);
        DateTime tied = cutoff.AddMinutes(-20);
        DateTime last = cutoff.AddMinutes(-10);

        Guid firstUser = AddMemberWithChild(
            context, scenario, "first", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), first);
        Guid tiedUserA = AddMemberWithChild(
            context, scenario, "tie-a", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), tied);
        Guid tiedUserB = AddMemberWithChild(
            context, scenario, "tie-b", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), tied);
        Guid lastUser = AddMemberWithChild(
            context, scenario, "last", RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved, ApprovalStatusEnum.Approved,
            cutoff.AddHours(-1), last);
        await context.SaveChangesAsync();

        var repository = new EventRegistrationIntentRepository(context);
        IReadOnlyList<NotificationFanoutAudienceMember> page1 = await ReadPageAsync(repository, scenario, cutoff, null, 2);
        NotificationFanoutAudienceMember page1Last = page1[^1];
        var cursor1 = new NotificationFanoutAudienceCursor(
            page1Last.FirstEligibleRegistrationCreatedAt,
            page1Last.UserId);
        IReadOnlyList<NotificationFanoutAudienceMember> page2 = await ReadPageAsync(repository, scenario, cutoff, cursor1, 2);
        NotificationFanoutAudienceMember page2Last = page2[^1];
        var cursor2 = new NotificationFanoutAudienceCursor(
            page2Last.FirstEligibleRegistrationCreatedAt,
            page2Last.UserId);
        IReadOnlyList<NotificationFanoutAudienceMember> page3 = await ReadPageAsync(repository, scenario, cutoff, cursor2, 2);
        IReadOnlyList<NotificationFanoutAudienceMember> repeatedPage1 = await ReadPageAsync(repository, scenario, cutoff, null, 2);

        Guid[] allUsers = page1.Concat(page2).Select(member => member.UserId).ToArray();
        await Assert.That(page1.Count).IsEqualTo(2);
        await Assert.That(page2.Count).IsEqualTo(2);
        await Assert.That(page3).IsEmpty();
        await Assert.That(allUsers.Distinct().Count()).IsEqualTo(4);
        await Assert.That(allUsers.ToHashSet()).IsEquivalentTo(
            new HashSet<Guid> { firstUser, tiedUserA, tiedUserB, lastUser });
        await Assert.That(repeatedPage1).IsEquivalentTo(page1);
        await Assert.That(page1[0].FirstEligibleRegistrationCreatedAt).IsEqualTo(first);
        await Assert.That(page2[^1].FirstEligibleRegistrationCreatedAt).IsEqualTo(last);
    }

    private static Task<IReadOnlyList<NotificationFanoutAudienceMember>> ReadPageAsync(
        EventRegistrationIntentRepository repository,
        AudienceScenario scenario,
        DateTime cutoff,
        NotificationFanoutAudienceCursor? cursor,
        int pageSize) =>
        repository.GetNotificationFanoutAudienceBatchAsync(
            scenario.TenantId,
            scenario.EventId,
            scenario.TargetSessionId,
            cutoff,
            (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
            cursor,
            pageSize,
            CancellationToken.None);

    private static Guid AddMember(
        Explore.Persistence.ExploreDbContext context,
        AudienceScenario scenario,
        string label,
        ApprovalStatusEnum parentStatus,
        DateTime parentCreatedAt,
        bool parentDeleted = false,
        TenantUserStatusEnum membershipStatus = TenantUserStatusEnum.Active,
        bool membershipDeleted = false,
        bool userDeleted = false)
    {
        Guid userId = AddUser(
            context,
            scenario.TenantId,
            label,
            membershipStatus,
            membershipDeleted,
            userDeleted);
        context.EventRegistrationIntents.Add(NewIntent(
            scenario,
            userId,
            RegistrationScopeEnum.Event,
            parentStatus,
            parentCreatedAt,
            parentDeleted));
        return userId;
    }

    private static Guid AddMemberWithChild(
        Explore.Persistence.ExploreDbContext context,
        AudienceScenario scenario,
        string label,
        RegistrationScopeEnum scope,
        ApprovalStatusEnum parentStatus,
        ApprovalStatusEnum childStatus,
        DateTime parentCreatedAt,
        DateTime coverageEstablishedAt,
        bool parentDeleted = false,
        bool childDeleted = false,
        bool targetSession = true,
        bool addDeletedDuplicate = false)
    {
        Guid userId = AddUser(context, scenario.TenantId, label);
        EventRegistrationIntent intent = NewIntent(
            scenario,
            userId,
            scope,
            parentStatus,
            parentCreatedAt,
            parentDeleted);
        context.EventRegistrationIntents.Add(intent);

        Guid sessionId = targetSession ? scenario.TargetSessionId : scenario.OtherSessionId;
        context.EventRegistrations.Add(NewChild(
            scenario, intent, userId, sessionId, childStatus, coverageEstablishedAt, childDeleted));

        if (addDeletedDuplicate)
        {
            context.EventRegistrations.Add(NewChild(
                scenario,
                intent,
                userId,
                sessionId,
                ApprovalStatusEnum.Approved,
                coverageEstablishedAt.AddHours(-1),
                isDeleted: true));
        }

        return userId;
    }

    private static void AddPartiallyCancelledMember(
        Explore.Persistence.ExploreDbContext context,
        AudienceScenario scenario,
        DateTime cutoff)
    {
        Guid userId = AddUser(context, scenario.TenantId, "partial-cancel");
        EventRegistrationIntent intent = NewIntent(
            scenario,
            userId,
            RegistrationScopeEnum.Event,
            ApprovalStatusEnum.Approved,
            cutoff.AddHours(-2));
        context.EventRegistrationIntents.Add(intent);
        context.EventRegistrations.AddRange(
            NewChild(
                scenario, intent, userId, scenario.TargetSessionId,
                ApprovalStatusEnum.Cancelled, cutoff.AddHours(-1)),
            NewChild(
                scenario, intent, userId, scenario.OtherSessionId,
                ApprovalStatusEnum.Approved, cutoff.AddHours(-1)));
    }

    private static Guid AddUser(
        Explore.Persistence.ExploreDbContext context,
        Guid tenantId,
        string label,
        TenantUserStatusEnum membershipStatus = TenantUserStatusEnum.Active,
        bool membershipDeleted = false,
        bool userDeleted = false)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{label}-{Guid.NewGuid():N}@example.test",
                FirstName = "Fanout",
                LastName = label,
            },
            EmailVerified = true,
            CreatedAt = Utc(2026, 1, 1, 0),
            IsDeleted = userDeleted,
            DeletedAt = userDeleted ? Utc(2026, 1, 2, 0) : null,
        };
        context.TenantUsers.Add(new TenantUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = user.Id,
            User = user,
            StatusId = (int)membershipStatus,
            JoinedAt = Utc(2026, 1, 1, 0),
            CreatedAt = Utc(2026, 1, 1, 0),
            IsDeleted = membershipDeleted,
            DeletedAt = membershipDeleted ? Utc(2026, 1, 2, 0) : null,
        });
        return user.Id;
    }

    private static EventRegistrationIntent NewIntent(
        AudienceScenario scenario,
        Guid userId,
        RegistrationScopeEnum scope,
        ApprovalStatusEnum status,
        DateTime createdAt,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EventId = scenario.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScopeId = (int)scope,
            RegistrationScope = null!,
            SelectedEventDayId = scope == RegistrationScopeEnum.Day ? scenario.EventDayId : null,
            SelectedEventDay = null,
            ApprovalStatusId = (int)status,
            ApprovalStatus = null,
            TenantId = scenario.TenantId,
            Tenant = null!,
            CreatedAt = createdAt,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? createdAt.AddMinutes(1) : null,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };

    private static EventRegistration NewChild(
        AudienceScenario scenario,
        EventRegistrationIntent intent,
        Guid userId,
        Guid sessionId,
        ApprovalStatusEnum status,
        DateTime coverageEstablishedAt,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EventId = scenario.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            EventSessionId = sessionId,
            EventSession = null!,
            EventRegistrationIntentId = intent.Id,
            EventRegistrationIntent = null,
            ApprovalStatusId = (int)status,
            ApprovalStatus = null,
            TenantId = scenario.TenantId,
            Tenant = null!,
            CoverageEstablishedAt = coverageEstablishedAt,
            CreatedAt = coverageEstablishedAt,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? coverageEstablishedAt.AddMinutes(1) : null,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };

    private static async Task<AudienceScenario> SeedScenarioAsync(
        Explore.Persistence.ExploreDbContext context,
        string slugPrefix)
    {
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = $"Fanout {slugPrefix}",
            Slug = $"{slugPrefix}-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = (int)ActorTypeEnum.Bot,
            ActorType = null!,
            Pii = new ActorPii { DisplayName = "Fanout source" },
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Fanout audience event",
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
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var eventDay = new EventDay
        {
            Id = Guid.CreateVersion7(),
            EventId = @event.Id,
            Event = null!,
            LocalDate = new DateOnly(2026, 8, 1),
            Label = "Fanout day",
            IsPublished = true,
            SortOrder = 0,
            AllowsDayScopeRegistration = true,
            TenantId = tenant.Id,
            Tenant = null!,
        };
        context.EventDays.Add(eventDay);
        await context.SaveChangesAsync();

        EventSession target = NewSession(tenant.Id, @event.Id, eventDay.Id, "Target session", 9);
        EventSession other = NewSession(tenant.Id, @event.Id, eventDay.Id, "Other session", 11);
        context.EventSessions.AddRange(target, other);
        await context.SaveChangesAsync();

        return new AudienceScenario(tenant.Id, @event.Id, eventDay.Id, target.Id, other.Id);
    }

    private static EventSession NewSession(
        Guid tenantId,
        Guid eventId,
        Guid eventDayId,
        string title,
        int startHour)
    {
        var session = new EventSession
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            Event = null!,
            EventDayId = eventDayId,
            EventDay = null,
            Title = title,
            TenantId = tenantId,
            Tenant = null!,
            EventSessionStatusId = (int)EventSessionStatusEnum.Published,
            EventSessionStatus = null!,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        session.Reschedule(
            new DateTimeOffset(2026, 8, 1, startHour, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, startHour + 1, 0, 0, TimeSpan.Zero),
            "UTC",
            new EventScheduleProjectionCalculator());
        return session;
    }

    private static string ReadSortedAudienceLabels(
        Explore.Persistence.ExploreDbContext context,
        IReadOnlyList<NotificationFanoutAudienceMember> audience)
    {
        Dictionary<Guid, string?> labels = context.Users.Local
            .ToDictionary(user => user.Id, user => user.Pii.LastName);
        return string.Join(",", audience.Select(member => labels[member.UserId]!).Order());
    }

    private static Task SetIntentCreatedAtAsync(
        Explore.Persistence.ExploreDbContext context,
        Guid userId,
        DateTime createdAt) =>
        context.EventRegistrationIntents
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Where(intent => intent.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(intent => intent.CreatedAt, createdAt));

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private sealed record AudienceScenario(
        Guid TenantId,
        Guid EventId,
        Guid EventDayId,
        Guid TargetSessionId,
        Guid OtherSessionId);
}
