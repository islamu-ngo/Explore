// ABOUTME: Production-faithful integration tests for EventController against real PostgreSQL.
// ABOUTME: Uses RealRuntimeApiFixture with Respawn reset and scenario seeds for deterministic testing.

using System.Net;
using System.Text;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// First vertical slice: EventController tests running against real PostgreSQL via Testcontainers.
/// Each test resets the database via Respawn and seeds its own scenario data for full isolation.
/// Tests are sequential to avoid shared-database interference.
/// </summary>
[ClassDataSource<RealRuntimeApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public class EventControllerRealRuntimeTests(RealRuntimeApiFixture fixture)
{
    private readonly RealRuntimeApiFixture _fixture = fixture;

    [Test]
    public async Task GetAll_WithEmptyDatabase_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/event");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetAll_WithSeededEvent_ReturnsOkContainingEventTitle()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);
        var eventResult = await EventScenarioSeed.SeedPublishedEventAsync(
            context, tenantResult.ActorId, tenantResult.TenantId);

        var response = await _fixture.Client.GetAsync("/api/event");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).Contains(eventResult.Title);
    }

    [Test]
    public async Task GetById_WithSeededEvent_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);
        var eventResult = await EventScenarioSeed.SeedPublishedEventAsync(
            context, tenantResult.ActorId, tenantResult.TenantId);

        var response = await _fixture.Client.GetAsync($"/api/event/{eventResult.EventId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync($"/api/event/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        await _fixture.ResetDatabaseAsync();

        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _fixture.Client.PostAsync("/api/event", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.DeleteAsync($"/api/event/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAll_AfterReset_DoesNotReturnPreviouslySeededData()
    {
        await _fixture.ResetDatabaseAsync();

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);
            await EventScenarioSeed.SeedPublishedEventAsync(
                context, tenantResult.ActorId, tenantResult.TenantId, "Ephemeral Event");
        }

        // Reset wipes all non-lookup data
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/event");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).DoesNotContain("Ephemeral Event");
    }
}
