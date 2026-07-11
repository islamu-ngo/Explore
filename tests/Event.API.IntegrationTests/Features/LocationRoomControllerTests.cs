// ABOUTME: Integration tests for LocationRoom API routing and authorization behavior.
// ABOUTME: Verifies public reads plus PATCH route, If-Match precondition, and old PUT rejection.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.LocationRoom;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class LocationRoomControllerTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/locationroom";

    public LocationRoomControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetById_WithRandomId_ShouldReturnNotFound()
    {
        var id = Guid.NewGuid();

        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{id}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var createDto = new CreateLocationRoomDto
        {
            LocationId = Guid.NewGuid(),
            Name = "Main Hall"
        };

        var response = await _fixture.Client.PostAsJsonAsync(BaseUrl, createDto);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Update_WithoutAuth_ShouldReturnUnauthorized()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateLocationRoomDto
        {
            Name = new UpdateLocationRoomNameDto { Value = "Updated Room" }
        };

        var response = await _fixture.Client.PatchAsJsonAsync($"{BaseUrl}/{id}", updateDto);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdatePut_WhenUsingOldRoute_ShouldReturnMethodNotAllowed()
    {
        var id = Guid.NewGuid();
        var updateDto = new UpdateLocationRoomDto
        {
            Name = new UpdateLocationRoomNameDto { Value = "Updated Room" }
        };

        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task UpdatePatch_WhenAuthenticatedWithoutIfMatch_ShouldReturnBadRequest()
    {
        await using var factory = new AuthenticatedWebApplicationFactory();
        using var client = factory.CreateClient();
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateDto = new UpdateLocationRoomDto
        {
            Name = new UpdateLocationRoomNameDto { Value = "Updated Room" }
        };
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/{roomId}")
        {
            Content = JsonContent.Create(updateDto)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var id = Guid.NewGuid();

        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/{id}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
