// ABOUTME: Deterministic registration journey seed for Playwright E2E coverage.
// ABOUTME: Creates one published registration-ready event with an open future session.

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class RegistrationScenarioSeed
{
    private const string TimezoneId = "Europe/Brussels";

    public sealed record Result(
        Guid TenantId,
        string TenantSlug,
        Guid EventId,
        Guid SessionId,
        string EventTitle,
        string SessionTitle);

    public static async Task<Result> SeedAsync(ExploreDbContext context)
    {
        SeedMultiTenantBootstrapState(context);

        var tenant = CreateTenant();
        context.Tenants.Add(tenant);

        var organizer = CreateUser("registration-organizer-e2e@example.test", "Registration", "Organizer");
        context.Users.Add(organizer);

        await context.SaveChangesAsync();

        var actor = CreateActor(tenant.Id, organizer.Id);
        context.Actors.Add(actor);

        await context.SaveChangesAsync();

        var startUtc = DateTimeOffset.UtcNow.AddDays(14).AddHours(2);
        var endUtc = startUtc.AddHours(2);
        var eventTitle = "Registration E2E Published Event";
        var sessionTitle = "Registration E2E Session";

        var eventEntity = CreatePublishedRegistrationEvent(tenant.Id, actor.Id, eventTitle, startUtc);
        context.Events.Add(eventEntity);

        var session = CreateOpenSession(tenant.Id, eventEntity.Id, sessionTitle, startUtc, endUtc);
        context.EventSessions.Add(session);

        await context.SaveChangesAsync();

        return new Result(
            tenant.Id,
            tenant.Slug,
            eventEntity.Id,
            session.Id,
            eventEntity.Title,
            sessionTitle);
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

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Registration E2E Tenant",
        Slug = "registration-e2e",
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

    private static Actor CreateActor(Guid tenantId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        Pii = new ActorPii { DisplayName = "Registration E2E Organizer" },
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        UserId = userId,
        TenantId = tenantId,
        Tenant = null!
    };

    private static Explore.Domain.Event CreatePublishedRegistrationEvent(
        Guid tenantId,
        Guid actorId,
        string title,
        DateTimeOffset sessionStartUtc)
    {
        var sessionDate = DateOnly.FromDateTime(sessionStartUtc.UtcDateTime);

        return new Explore.Domain.Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = "registration-e2e-published-event",
            Description = "Registration E2E event with one open future session.",
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
            Timezone = TimezoneId,
            EventTimeZoneId = TimezoneId,
            TotalViews = 0,
            IsRegistrationRequired = true,
            RegistrationPolicyId = (int)EventRegistrationPolicyEnum.SessionSelectionOnly,
            ConcurrencyStamp = Guid.NewGuid()
        };
    }

    private static EventSession CreateOpenSession(
        Guid tenantId,
        Guid eventId,
        string title,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            Title = title,
            Slug = "registration-e2e-session",
            RegistrationModeId = (int)RegistrationModeEnum.Open,
            RegistrationMode = null,
            MaxAudienceAttendees = 50,
            CurrentAudienceAttendees = 0,
            SortOrder = 1,
            ConcurrencyStamp = Guid.NewGuid()
        };

        session.Reschedule(startUtc, endUtc, TimezoneId, new EventScheduleProjectionCalculator());

        return session;
    }
}
