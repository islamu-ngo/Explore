// ABOUTME: Endpoint-level coverage for event program section write routes.
// ABOUTME: Verifies session-group mutations remain authenticated API operations.

using System.Net;
using System.Net.Http.Json;

using Event.Api.IntegrationTests.Fixtures;

using Explore.Application.DTOs.EventSessionGroup;

using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventSessionGroupControllerTests
{
    private const string BaseUrl = "/api/eventsessiongroup";

    private readonly ApiTestFixture _fixture;

    public EventSessionGroupControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var createDto = new CreateEventSessionGroupRequestDto
        {
            EventId = Guid.NewGuid(),
            Name = "Main stage",
            Slug = "main-stage",
            SortOrder = 10,
            IsPublished = true
        };

        var response = await _fixture.Client.PostAsJsonAsync(BaseUrl, createDto);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Update_WithoutAuth_ShouldReturnUnauthorized()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateEventSessionGroupRequestDto
        {
            Id = id,
            EventId = Guid.NewGuid(),
            Name = "Updated main stage",
            Slug = "updated-main-stage",
            SortOrder = 20,
            IsPublished = true
        };

        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var id = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/{id}?eventId={eventId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AssignSession_WithoutAuth_ShouldReturnUnauthorized()
    {
        var groupId = Guid.NewGuid();
        var assignment = new AssignSessionToGroupRequestDto
        {
            EventId = Guid.NewGuid(),
            EventSessionGroupId = groupId,
            EventSessionId = Guid.NewGuid(),
            IsPrimary = true,
            SortOrder = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync($"{BaseUrl}/{groupId}/sessions", assignment);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UnassignSession_WithoutAuth_ShouldReturnUnauthorized()
    {
        var groupId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/{groupId}/sessions/{sessionId}?eventId={eventId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
