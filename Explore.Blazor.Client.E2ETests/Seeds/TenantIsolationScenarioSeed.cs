// ABOUTME: Business-readable tenant isolation seed for browser E2E scaffolds.
// ABOUTME: Creates two tenant contexts and a public event owned only by tenant A.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Persistence;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class TenantIsolationScenarioSeed
{
    public sealed record Result(
        Guid TenantAId,
        string TenantASlug,
        Guid TenantBId,
        string TenantBSlug,
        Guid TenantAEventId,
        string TenantAEventTitle);

    public static async Task<Result> SeedAsync(ExploreDbContext context)
    {
        SeedMultiTenantBootstrapState(context);

        var tenantA = CreateTenant("Tenant A E2E", "tenant-a-e2e");
        var tenantB = CreateTenant("Tenant B E2E", "tenant-b-e2e");

        context.Tenants.AddRange(tenantA, tenantB);

        var userA = CreateUser("tenant-a-e2e@example.test", "Tenant", "A");
        var userB = CreateUser("tenant-b-e2e@example.test", "Tenant", "B");

        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var actorA = CreateActor(tenantA.Id, userA.Id, "Tenant A E2E Actor");
        var actorB = CreateActor(tenantB.Id, userB.Id, "Tenant B E2E Actor");

        context.Actors.AddRange(actorA, actorB);
        await context.SaveChangesAsync();

        var tenantAEvent = CreatePublishedEvent(tenantA.Id, actorA.Id, "Tenant A Published E2E Event");
        context.Events.Add(tenantAEvent);

        var calculator = new Explore.Domain.Services.Scheduling.EventScheduleProjectionCalculator();
        var sessionStartUtc = DateTimeOffset.UtcNow.AddDays(7);
        var tenantAEventSession = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = tenantAEvent.Id,
            Event = tenantAEvent,
            TenantId = tenantA.Id,
            Tenant = tenantA,
            Title = "Tenant A Published E2E Event Session",
            EventSessionStatusId = (int)EventSessionStatusEnum.Published,
            Slug = "tenant-a-published-e2e-event-session",
            CurrentAudienceAttendees = 0,
            SortOrder = 0
        };
        tenantAEventSession.Reschedule(sessionStartUtc, sessionStartUtc.AddHours(2), "Europe/Brussels", calculator);
        context.EventSessions.Add(tenantAEventSession);

        tenantAEvent.Sessions.Add(tenantAEventSession);
        tenantAEvent.RecalculateScheduleSummaryFromSessions();

        await context.SaveChangesAsync();

        return new Result(
            tenantA.Id,
            tenantA.Slug,
            tenantB.Id,
            tenantB.Slug,
            tenantAEvent.Id,
            tenantAEvent.Title);
    }

    private static void SeedMultiTenantBootstrapState(ExploreDbContext context)
    {
        var now = DateTime.UtcNow;

        context.InstanceBootstrapStates.Add(new InstanceBootstrapState
        {
            Id = Guid.NewGuid(),
            IsCompleted = true,
            CreatedAt = now,
            CompletedAt = now,
            SelectedDeploymentMode = DeploymentMode.MultiTenant.ToString()
        });

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Deployment.Mode,
            $"\"{DeploymentMode.MultiTenant}\"",
            SettingValueType.String,
            "System");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Routing.ResolverPathEnabled,
            "true",
            SettingValueType.Boolean,
            "Routing");

        UpsertSystemSetting(
            context,
            GovernanceSettingKeys.Routing.PathPrefix,
            "\"/t\"",
            SettingValueType.String,
            "Routing");
    }

    private static void UpsertSystemSetting(
        ExploreDbContext context,
        string settingKey,
        string value,
        SettingValueType valueType,
        string category)
    {
        var setting = context.SystemSettings.Local.FirstOrDefault(x => x.SettingKey == settingKey)
            ?? context.SystemSettings.FirstOrDefault(x => x.SettingKey == settingKey);

        if (setting is null)
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });

            return;
        }

        setting.Value = value;
        setting.ValueType = valueType;
        setting.Category ??= category;
        setting.UpdatedAt = DateTime.UtcNow;
    }

    private static Tenant CreateTenant(string name, string slug) => new()
    {
        Id = Guid.NewGuid(),
        FullName = name,
        Slug = slug,
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!
    };

    private static User CreateUser(string email, string firstName, string lastName) => new()
    {
        Id = Guid.NewGuid(),
        Pii = new UserPii
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName
        }
    };

    private static Actor CreateActor(Guid tenantId, Guid userId, string displayName) => new()
    {
        Id = Guid.NewGuid(),
        Pii = new ActorPii { DisplayName = displayName },
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        UserId = userId,
        TenantId = tenantId,
        Tenant = null!
    };

    private static Explore.Domain.Event CreatePublishedEvent(Guid tenantId, Guid actorId, string title)
    {
        var sessionStartUtc = DateTimeOffset.UtcNow.AddDays(7);
        var sessionDate = DateOnly.FromDateTime(sessionStartUtc.UtcDateTime);

        return new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Tenant isolation E2E event that must only appear in tenant A.",
            ActorId = actorId,
            Actor = null!,
            TenantId = tenantId,
            Tenant = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            FirstSessionDate = sessionDate,
            LastSessionDate = sessionDate,
            FirstSessionStartUtc = sessionStartUtc,
            LastSessionStartUtc = sessionStartUtc,
            Timezone = "Europe/Brussels",
            EventTimeZoneId = "Europe/Brussels",
            TotalViews = 0,
            IsRegistrationRequired = false,
            ConcurrencyStamp = Guid.NewGuid()
        };
    }
}
