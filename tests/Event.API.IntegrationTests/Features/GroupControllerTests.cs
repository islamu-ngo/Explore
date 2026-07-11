// ABOUTME: Integration tests for public Group API routing and authorization behavior.
// ABOUTME: Verifies read endpoints plus authenticated PATCH route and If-Match contracts.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Group;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class GroupControllerTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/group";

    public GroupControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldReturnOk_WithPaginatedResult()
    {
        var response = await _fixture.Client.GetAsync(BaseUrl);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("items");
    }

    [Test]
    public async Task GetById_WithRandomId_ShouldReturnNotFound()
    {
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PostAsJsonAsync(BaseUrl, new CreateGroupDto
        {
            FullName = "Test Group"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Update_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.PatchAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", CreateUpdateDto());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdatePut_WhenUsingOldRoute_ShouldReturnMethodNotAllowed()
    {
        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", CreateUpdateDto());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task UpdatePatch_WhenAuthenticatedWithoutIfMatch_ShouldReturnBadRequest()
    {
        await using var factory = new AuthenticatedWebApplicationFactory();
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(CreateUpdateDto())
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Delete_WithoutAuth_ShouldReturnUnauthorized()
    {
        var response = await _fixture.Client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private static UpdateGroupDto CreateUpdateDto() =>
        new()
        {
            FullName = new UpdateGroupFullNameDto
            {
                Value = "Updated Group"
            }
        };
}
