// ABOUTME: Business-readable scenario seed for events in integration tests.
// ABOUTME: Creates events within an established tenant context for test scenarios.

using Event.Api.IntegrationTests.Builders;
using Explore.Domain.Enums;
using Explore.Persistence;

namespace Event.Api.IntegrationTests.Seeds;

/// <summary>
/// Seeds event entities within an established tenant context.
/// Requires a prior <see cref="TenantScenarioSeed"/> call to provide ActorId and TenantId.
/// </summary>
public static class EventScenarioSeed
{
    /// <summary>Carries the ID and title of a seeded event.</summary>
    public sealed record EventScenarioResult(Guid EventId, string Title);

    /// <summary>
    /// Seeds a single published event visible to public API queries.
    /// </summary>
    public static async Task<EventScenarioResult> SeedPublishedEventAsync(
        ExploreDbContext context,
        Guid actorId,
        Guid tenantId,
        string title = "Published Test Event")
    {
        var @event = new EventBuilder()
            .WithTitle(title)
            .WithActorId(actorId)
            .WithTenantId(tenantId)
            .WithStatus(EventStatusEnum.Published)
            .WithVisibility(VisibilityTypeEnum.Public)
            .WithSessionDates(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .Build();

        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return new EventScenarioResult(@event.Id, @event.Title);
    }

    /// <summary>
    /// Seeds a draft event that is not publicly visible.
    /// </summary>
    public static async Task<EventScenarioResult> SeedDraftEventAsync(
        ExploreDbContext context,
        Guid actorId,
        Guid tenantId,
        string title = "Draft Test Event")
    {
        var @event = new EventBuilder()
            .WithTitle(title)
            .WithActorId(actorId)
            .WithTenantId(tenantId)
            .WithStatus(EventStatusEnum.Draft)
            .Build();

        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return new EventScenarioResult(@event.Id, @event.Title);
    }

    /// <summary>
    /// Seeds multiple published events for pagination and list testing.
    /// </summary>
    public static async Task<IReadOnlyList<EventScenarioResult>> SeedMultiplePublishedEventsAsync(
        ExploreDbContext context,
        Guid actorId,
        Guid tenantId,
        int count = 5)
    {
        var results = new List<EventScenarioResult>();

        for (var i = 0; i < count; i++)
        {
            var @event = new EventBuilder()
                .WithTitle($"Event {i + 1}")
                .WithActorId(actorId)
                .WithTenantId(tenantId)
                .WithStatus(EventStatusEnum.Published)
                .WithVisibility(VisibilityTypeEnum.Public)
                .Build();

            context.Events.Add(@event);
            results.Add(new EventScenarioResult(@event.Id, @event.Title));
        }

        await context.SaveChangesAsync();
        return results;
    }
}
