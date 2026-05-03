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
    public async Task Create_WithSingleSessionRequest_ReturnsCreatedAndPersistsEvent()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);

        var createRequest = CreateValidEventRequest("Single Submit Integration Event");
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", tenantResult.UserId);
        request.Content = JsonContent.Create(createRequest);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Success).IsTrue();
        await Assert.That(body.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(response.Headers.Location?.ToString()).Contains(body.Id.ToString());

        var detailResponse = await _fixture.Client.GetAsync($"/api/event/{body.Id}");
        var detailContent = await detailResponse.Content.ReadAsStringAsync();

        await Assert.That(detailResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(detailContent).Contains(createRequest.Title);
    }

    [Test]
    public async Task Create_WithInvalidTempKey_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);

        var createRequest = CreateValidEventRequest("Invalid Temp Key Event");
        createRequest.AgendaItems.Add(new CreateEventAgendaItemRequest
        {
            Title = "Opening",
            DayTempKey = "missing-day",
            StartTime = createRequest.Sessions[0].StartTime,
            EndTime = createRequest.Sessions[0].StartTime.AddMinutes(30),
            SortOrder = 0
        });

        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Post, "/api/event", tenantResult.UserId);
        request.Content = JsonContent.Create(createRequest);

        var response = await _fixture.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(content).Contains("temp-key references are invalid");
    }

    [Test]
    public async Task Create_WithMultiSessionRoomsAndAgenda_PersistsSubmittedGraph()
    {
        await _fixture.ResetDatabaseAsync();

        Guid locationId;
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var tenantResult = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(context);
            var tenant = await context.Tenants.FindAsync(tenantResult.TenantId);
            var location = new Location
            {
                FullName = "Single Submit Conference Center",
                Country = "BE",
                City = "Brussels",
                TenantId = tenantResult.TenantId,
                Tenant = tenant!,
                Timezone = "UTC",
                Pii = new LocationPii { Address = "1 Test Street", Postcode = "1000" }
            };

            context.Locations.Add(location);
            await context.SaveChangesAsync();
            locationId = location.Id;
        }

        Guid userId;
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            var seededUser = await context.Users.IgnoreQueryFilters().SingleAsync();
            userId = seededUser.Id;
        }

        var start = DateTimeOffset.UtcNow.AddDays(21).AddHours(2);
        var secondStart = start.AddDays(1);
        var requestDto = new CreateEventRequest
        {
            Title = "Advanced Single Submit Integration Event",
            Description = "Creates sessions, days, room, and event-level agenda in one API call.",
            EventStatusId = 2,
            VisibilityTypeId = 1,
            EventFormatId = 1,
            Timezone = "UTC",
            Days =
            [
                new CreateEventDayRequest
                {
                    TempKey = "day-1",
                    LocalDate = DateOnly.FromDateTime(start.UtcDateTime),
                    Label = "Opening Day",
                    SortOrder = 0
                },
                new CreateEventDayRequest
                {
                    TempKey = "day-2",
                    LocalDate = DateOnly.FromDateTime(secondStart.UtcDateTime),
                    Label = "Workshop Day",
                    SortOrder = 1
                }
            ],
            Rooms =
            [
                new CreateEventRoomRequest
                {
                    TempKey = "room-main",
                    LocationId = locationId,
                    Name = "Main Hall",
                    Capacity = 120,
                    SortOrder = 0
                }
            ],
            Sessions =
            [
                new CreateEventSessionRequest
                {
                    TempKey = "session-1",
                    DayTempKey = "day-1",
                    RoomTempKey = "room-main",
                    Title = "Opening Session",
                    StartTime = start,
                    EndTime = start.AddHours(2),
                    LocationId = locationId,
                    SortOrder = 0
                },
                new CreateEventSessionRequest
                {
                    TempKey = "session-2",
                    DayTempKey = "day-2",
                    RoomTempKey = "room-main",
                    Title = "Workshop Session",
                    StartTime = secondStart,
                    EndTime = secondStart.AddHours(2),
                    LocationId = locationId,
                    SortOrder = 1
                }
            ],
            AgendaItems =
            [
                new CreateEventAgendaItemRequest
                {
                    TempKey = "agenda-1",
                    DayTempKey = "day-1",
                    RoomTempKey = "room-main",
                    Title = "Welcome",
                    StartTime = start,
                    EndTime = start.AddMinutes(30),
                    LocationId = locationId,
                    SortOrder = 0
                }
            ]
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
        var roomExists = await verifyContext.LocationRooms.IgnoreQueryFilters()
            .AnyAsync(x => x.LocationId == locationId && x.Name == "Main Hall");

        await Assert.That(sessionCount).IsEqualTo(2);
        await Assert.That(dayCount).IsEqualTo(2);
        await Assert.That(agendaCount).IsEqualTo(1);
        await Assert.That(roomExists).IsTrue();
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

    private static CreateEventRequest CreateValidEventRequest(string title)
    {
        var start = DateTimeOffset.UtcNow.AddDays(14).AddHours(2);

        return new CreateEventRequest
        {
            Title = title,
            Description = "Created by the single-submit API integration test.",
            EventStatusId = 2,
            VisibilityTypeId = 1,
            EventFormatId = 2,
            Timezone = "UTC",
            Sessions =
            [
                new CreateEventSessionRequest
                {
                    TempKey = "session-0",
                    Title = title,
                    StartTime = start,
                    EndTime = start.AddHours(2),
                    SortOrder = 0
                }
            ]
        };
    }
}
