using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.User;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class UserControllerTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/user";

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

    #region PUT Endpoints

    [Test]
    public async Task UpdateUser_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            Id = Guid.NewGuid(),
            Email = "updated@example.com",
            FirstName = "Updated",
            LastName = "User"
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync(BaseUrl, updateDto);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
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
