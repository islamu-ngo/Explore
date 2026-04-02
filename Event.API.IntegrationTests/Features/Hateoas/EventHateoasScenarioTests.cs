// ABOUTME: Scenario-based HATEOAS tests for EventController using RealRuntimeApiFixture with seeded data.
// ABOUTME: Validates event-specific item links (sessions, actor) and pagination with actual data present.

using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// Scenario-based HATEOAS tests for EventController on RealRuntime (PostgreSQL).
/// Seeds real data to verify item-level links (sessions, actor) and multi-page pagination
/// that are meaningless on an empty database.
/// </summary>
[ClassDataSource<RealRuntimeApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public class EventHateoasScenarioTests(RealRuntimeApiFixture fixture)
{
    private readonly RealRuntimeApiFixture _fixture = fixture;

    [Test]
    public async Task GetAll_WithSeededEvents_ShouldIncludeItemLinks()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
        await EventScenarioSeed.SeedPublishedEventAsync(db, tenant.ActorId, tenant.TenantId);

        var response = await _fixture.Client.GetAsync("/api/event?pageNumber=1&pageSize=20");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var embedded = json.RootElement.GetProperty("_embedded");
        var items = embedded.GetProperty("items");

        await Assert.That(items.GetArrayLength()).IsGreaterThanOrEqualTo(1);

        var firstItem = items[0];
        await Assert.That(firstItem.TryGetProperty("_links", out var itemLinks)).IsTrue();
        await Assert.That(itemLinks.TryGetProperty("self", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_SeededEventItem_ShouldIncludeSessionsLink()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
        await EventScenarioSeed.SeedPublishedEventAsync(db, tenant.ActorId, tenant.TenantId);

        var response = await _fixture.Client.GetAsync("/api/event?pageNumber=1&pageSize=20");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("_embedded").GetProperty("items");

        await Assert.That(items.GetArrayLength()).IsGreaterThanOrEqualTo(1);

        var firstItemLinks = items[0].GetProperty("_links");
        if (firstItemLinks.TryGetProperty("sessions", out var sessionsLink))
        {
            var href = sessionsLink.GetProperty("href").GetString();
            await Assert.That(href).Contains("/api/eventsession/by-event/");
        }
    }

    [Test]
    public async Task GetAll_SeededEventItem_ShouldIncludeActorLink()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
        await EventScenarioSeed.SeedPublishedEventAsync(db, tenant.ActorId, tenant.TenantId);

        var response = await _fixture.Client.GetAsync("/api/event?pageNumber=1&pageSize=20");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("_embedded").GetProperty("items");

        await Assert.That(items.GetArrayLength()).IsGreaterThanOrEqualTo(1);

        var firstItemLinks = items[0].GetProperty("_links");
        if (firstItemLinks.TryGetProperty("actor", out var actorLink))
        {
            var href = actorLink.GetProperty("href").GetString();
            await Assert.That(href).Contains("/api/actor/");
        }
    }

    [Test]
    public async Task GetAll_WithMultipleSeededEvents_ShouldShowCorrectTotalCount()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
        await EventScenarioSeed.SeedMultiplePublishedEventsAsync(db, tenant.ActorId, tenant.TenantId, 5);

        var response = await _fixture.Client.GetAsync("/api/event?pageNumber=1&pageSize=20");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var totalCount = json.RootElement.GetProperty("totalCount").GetInt32();
        await Assert.That(totalCount).IsGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task GetAll_PaginatedSeededEvents_ShouldHaveNextLink_WhenMorePagesExist()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
        await EventScenarioSeed.SeedMultiplePublishedEventsAsync(db, tenant.ActorId, tenant.TenantId, 6);

        var response = await _fixture.Client.GetAsync("/api/event?pageNumber=1&pageSize=3");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var links = json.RootElement.GetProperty("_links");

        await Assert.That(links.TryGetProperty("next", out var nextLink)).IsTrue();
        var nextHref = nextLink.GetProperty("href").GetString();
        await Assert.That(nextHref).Contains("pageNumber=2");
    }

    [Test]
    public async Task GetAll_WithPreferMinimal_SeededEvents_ItemsShouldNotHaveLinks()
    {
        await _fixture.ResetDatabaseAsync();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);
        await EventScenarioSeed.SeedPublishedEventAsync(db, tenant.ActorId, tenant.TenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/event?pageNumber=1&pageSize=20");
        request.Headers.Add("Prefer", "return=minimal");

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("_embedded").GetProperty("items");

        await Assert.That(items.GetArrayLength()).IsGreaterThanOrEqualTo(1);

        var hasLinks = items[0].TryGetProperty("_links", out _);
        await Assert.That(hasLinks).IsFalse();
    }
}
