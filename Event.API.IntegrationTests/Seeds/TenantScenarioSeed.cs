// ABOUTME: Business-readable scenario seed for tenant contexts in integration tests.
// ABOUTME: Creates a complete tenant + user + actor graph for test scenarios.

using Event.Api.IntegrationTests.Builders;
using Explore.Domain.Constants;
using Explore.Persistence;

namespace Event.Api.IntegrationTests.Seeds;

/// <summary>
/// Seeds complete tenant contexts (tenant + user + actor) for integration test scenarios.
/// Uses the two-phase save pattern required by the User → Actor circular dependency.
/// </summary>
public static class TenantScenarioSeed
{
    /// <summary>Carries the IDs of all entities created by a tenant scenario seed.</summary>
    public sealed record TenantScenarioResult(Guid TenantId, Guid UserId, Guid ActorId);

    /// <summary>
    /// Seeds an active tenant with a user and user-type actor.
    /// Uses <see cref="PlatformDefaults.DefaultTenantId"/> for single-tenant API compatibility.
    /// </summary>
    public static async Task<TenantScenarioResult> SeedActiveTenantWithUserAsync(ExploreDbContext context)
    {
        var tenant = new TenantBuilder()
            .WithId(PlatformDefaults.DefaultTenantId)
            .WithFullName("Default Test Tenant")
            .WithSlug("default-test")
            .Build();
        context.Tenants.Add(tenant);

        var user = new UserBuilder().Build();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new ActorBuilder()
            .WithTenantId(tenant.Id)
            .WithUserId(user.Id)
            .Build();
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        return new TenantScenarioResult(tenant.Id, user.Id, actor.Id);
    }

    /// <summary>
    /// Seeds a secondary tenant with its own user and actor.
    /// Uses a random tenant ID for multi-tenant isolation testing.
    /// </summary>
    public static async Task<TenantScenarioResult> SeedSecondaryTenantWithUserAsync(
        ExploreDbContext context,
        string name = "Secondary Tenant")
    {
        var tenant = new TenantBuilder()
            .WithFullName(name)
            .Build();
        context.Tenants.Add(tenant);

        var user = new UserBuilder().Build();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new ActorBuilder()
            .WithTenantId(tenant.Id)
            .WithUserId(user.Id)
            .WithDisplayName($"{name} User")
            .Build();
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        return new TenantScenarioResult(tenant.Id, user.Id, actor.Id);
    }
}
