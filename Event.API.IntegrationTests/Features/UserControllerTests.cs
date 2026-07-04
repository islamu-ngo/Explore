// ABOUTME: API contract tests for authenticated user profile endpoints.
// ABOUTME: Covers auth requirements, PATCH route shape, and If-Match precondition validation.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Event.Api.IntegrationTests.Helpers;
using Explore.Application.DTOs.User;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class UserControllerTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/user";

    public UserControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region GET Endpoints

    [Test]
    public async Task GetCurrentUser_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUserOrganizations_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{userId}/organizations");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUserOrganizations_WhenRequestedUserDiffersFromCurrentUser_ShouldReturnForbiddenProblemDetails()
    {
        await using var factory = new AuthenticatedWebApplicationFactory();
        using var client = factory.CreateClient();
        var currentUserId = Guid.NewGuid();
        var requestedUserId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{requestedUserId}/organizations");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(currentUserId));

        var response = await client.SendAsync(request);

        await ProblemDetailsAssertions.AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    #endregion

    #region POST Endpoints

    [Test]
    public async Task SyncUser_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _fixture.Client.PostAsync($"{BaseUrl}/sync", null);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PATCH Endpoints

    [Test]
    public async Task UpdateUserPatch_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            Names = new UpdateUserNamesDto
            {
                FirstName = "Updated",
                LastName = "User"
            }
        };

        // Act
        var response = await _fixture.Client.PatchAsJsonAsync($"{BaseUrl}/{userId}", updateDto);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateUserPut_WhenUsingOldBodyIdRoute_ShouldReturnMethodNotAllowed()
    {
        // Arrange
        var oldBody = new
        {
            id = Guid.NewGuid(),
            names = new
            {
                firstName = "Updated",
                lastName = "User"
            }
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync(BaseUrl, oldBody);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task UpdateUserPatch_WhenAuthenticatedWithoutIfMatch_ShouldReturnBadRequest()
    {
        // Arrange
        await using var factory = new AuthenticatedWebApplicationFactory();
        using var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            Names = new UpdateUserNamesDto
            {
                FirstName = "Updated",
                LastName = "User"
            }
        };
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/{userId}")
        {
            Content = JsonContent.Create(updateDto)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE Endpoints

    [Test]
    public async Task DeleteUser_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _fixture.Client.DeleteAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion
}
