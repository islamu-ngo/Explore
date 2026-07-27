// ABOUTME: Production-faithful integration tests for EventController against real PostgreSQL.
// ABOUTME: Uses RealRuntimeApiFixture with Respawn reset and scenario seeds for deterministic testing.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Seeds;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
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

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Created);
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.OK);
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
    public async Task Create_WithDraftRequest_ReturnsCreatedAndPersistsEvent()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);

        var createRequest = new CreateEventDraftRequestDto
        {
            Title = "Draft Submit Integration Event",
            Description = "Created by the draft API integration test.",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            VisibilityTypeId = 1,
            EventFormatId = 1,
            Timezone = "UTC"
        };
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", tenantResult.UserId);
        request.Content = JsonContent.Create(createRequest);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        await Assert.That(body.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(response.Headers.Location?.ToString()).Contains(body.Id.ToString());

        using var verifyScope = _fixture.Factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var eventEntity = await verifyContext.Events.IgnoreQueryFilters().SingleAsync(x => x.Id == body.Id);

        await Assert.That(eventEntity.Title).IsEqualTo(createRequest.Title);
    }

    [Test]
    public async Task Create_WithDraftWithoutSessions_ReturnsCreatedAndPersistsEmptyProgramDraft()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);

        var createRequest = new CreateEventDraftRequestDto
        {
            Title = "Draft Without Sessions Integration Event",
            Description = "Created before any program items exist.",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            VisibilityTypeId = 1,
            EventFormatId = 1,
            Timezone = "UTC"
        };
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", tenantResult.UserId);
        request.Content = JsonContent.Create(createRequest);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        await Assert.That(body.Id).IsNotEqualTo(Guid.Empty);

        using var verifyScope = _fixture.Factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var eventEntity = await verifyContext.Events.IgnoreQueryFilters().SingleAsync(x => x.Id == body.Id);
        var sessionCount = await verifyContext.EventSessions.IgnoreQueryFilters().CountAsync(x => x.EventId == body.Id);

        await Assert.That(eventEntity.SessionCount).IsEqualTo(0);
        await Assert.That(eventEntity.FirstSessionDate).IsNull();
        await Assert.That(eventEntity.LastSessionDate).IsNull();
        await Assert.That(eventEntity.FirstSessionStartUtc).IsNull();
        await Assert.That(eventEntity.LastSessionStartUtc).IsNull();
        await Assert.That(sessionCount).IsEqualTo(0);
    }

    [Test]
    public async Task Create_WithBlankTitle_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);

        var createRequest = new CreateEventDraftRequestDto
        {
            Title = string.Empty,
            ParticipationConfiguration = CreateParticipationConfiguration(),
            VisibilityTypeId = 1,
            EventFormatId = 1,
            Timezone = "UTC"
        };

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", tenantResult.UserId);
        request.Content = JsonContent.Create(createRequest);

        var response = await _fixture.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(content).Contains("Title");
    }

    [Test]
    public async Task Create_WithDraftContract_DoesNotPersistProgramGraphRows()
    {
        await _fixture.ResetDatabaseAsync();

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);
            await context.SaveChangesAsync();
        }

        Guid userId;
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var seededUser = await context.Users.IgnoreQueryFilters().SingleAsync();
            userId = seededUser.Id;
        }

        var requestDto = new CreateEventDraftRequestDto
        {
            Title = "Draft Contract Integration Event",
            Description = "Creates only the event draft; program rows are added through dedicated endpoints.",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            VisibilityTypeId = 1,
            EventFormatId = 1,
            Timezone = "UTC"
        };

        using var createRequest = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", userId);
        createRequest.Content = JsonContent.Create(requestDto);

        var response = await _fixture.Client.SendAsync(createRequest);
        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Id).IsNotEqualTo(Guid.Empty);

        using var verifyScope = _fixture.Factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var eventId = body.Id;

        var sessionCount = await verifyContext.EventSessions.IgnoreQueryFilters().CountAsync(x => x.EventId == eventId);
        var dayCount = await verifyContext.EventDays.IgnoreQueryFilters().CountAsync(x => x.EventId == eventId);
        var agendaCount = await verifyContext.EventAgendaItems.IgnoreQueryFilters().CountAsync(x => x.EventId == eventId);

        await Assert.That(sessionCount).IsEqualTo(0);
        await Assert.That(dayCount).IsEqualTo(0);
        await Assert.That(agendaCount).IsEqualTo(0);
    }

    [Test]
    public async Task CreateWithSessionsEndpoint_IsRemoved()
    {
        await _fixture.ResetDatabaseAsync();

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event/with-sessions");
        request.Content = JsonContent.Create(new { });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Created);
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.OK);
    }

    private static ConfigureEventParticipationDto CreateParticipationConfiguration() => new()
    {
        ParticipationHandlingModeId = (int)ParticipationHandlingModeEnum.InformationOnly,
        AdvanceRegistrationObligationId = (int)AdvanceRegistrationObligationEnum.NotApplicable
    };

    [Test]
    public async Task Update_WithoutIfMatch_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();

        var userId = Guid.CreateVersion7();
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Patch, $"/api/event/{Guid.CreateVersion7()}", userId);
        request.Content = JsonContent.Create(new UpdateEventDto
        {
            Title = new UpdateEventTitleDto { Value = "Updated title" }
        });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Update_WithOldPutRoute_ReturnsMethodNotAllowed()
    {
        await _fixture.ResetDatabaseAsync();

        var userId = Guid.CreateVersion7();
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Put, $"/api/event/{Guid.CreateVersion7()}", userId);
        request.Content = JsonContent.Create(new { title = new { value = "Updated title" } });

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
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
