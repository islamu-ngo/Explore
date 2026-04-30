// ABOUTME: Business-readable tenant isolation seed for browser E2E scaffolds.
// ABOUTME: Creates two tenant contexts and a public event owned only by tenant A.

using Explore.Domain;
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
        await context.SaveChangesAsync();

        return new Result(
            tenantA.Id,
            tenantA.Slug,
            tenantB.Id,
            tenantB.Slug,
            tenantAEvent.Id,
            tenantAEvent.Title);
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
        var sessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

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
            TotalViews = 0,
            IsRegistrationRequired = false,
            ConcurrencyStamp = Guid.NewGuid()
        };
    }
}
