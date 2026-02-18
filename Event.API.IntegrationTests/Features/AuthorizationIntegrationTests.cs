// ABOUTME: End-to-end integration tests verifying endpoint-level authorization.
// Tests that protected endpoints return 401 for anonymous users and allow authenticated users through.
// Auth state is per-request via X-Test-Auth header — no shared static state, safe for parallel execution.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Organization;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Integration tests verifying that the authentication/authorization pipeline correctly
/// denies anonymous access to protected endpoints and allows authenticated access.
/// Uses per-request X-Test-Auth header for thread-safe parallel test execution.
/// </summary>
[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]
public class AuthorizationIntegrationTests
{
    private readonly AuthenticatedApiTestFixture _fixture;

    public AuthorizationIntegrationTests(AuthenticatedApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Anonymous Deny Tests — Write Endpoints

    [Test]
    public async Task CreateOrganization_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange — no X-Test-Auth header = anonymous
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Org",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "Test Street 1",
            Postcode = 1000
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/organization")
        {
            Content = JsonContent.Create(dto)
        };

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateOrganization_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange — no X-Test-Auth header = anonymous
        var dto = new UpdateOrganizationDto
        {
            FullName = "Updated Org",
            Email = "updated@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "Updated Street",
            Postcode = 1000
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/organization/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(dto)
        };

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMyOrganizations_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange — no X-Test-Auth header = anonymous
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/organization/my");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Anonymous Allow Tests — Read Endpoints

    [Test]
    [Arguments("/api/organization")]
    [Arguments("/api/event")]
    [Arguments("/api/eventsession")]
    [Arguments("/api/actor")]
    [Arguments("/api/location")]
    [Arguments("/api/category")]
    [Arguments("/api/tag")]
    public async Task GetAllPublicEndpoints_WithoutAuth_ShouldReturnOk(string endpoint)
    {
        // Arrange — no X-Test-Auth header = anonymous
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert — public read endpoints should be accessible without auth
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    #endregion

    #region Authenticated Allow Tests — Write Endpoints

    [Test]
    public async Task CreateOrganization_WithAuth_ShouldNotReturnUnauthorized()
    {
        // Arrange — authenticated user via X-Test-Auth header
        var dto = new CreateOrganizationDto
        {
            FullName = "Auth Org",
            Email = "auth@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "Auth Street 1",
            Postcode = 1000
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/organization")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid(), "Auth User"));

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert — should NOT be 401; could be 201, 400, or 403 depending on auth provider
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMyOrganizations_WithAuth_ShouldNotReturnUnauthorized()
    {
        // Arrange — authenticated user via X-Test-Auth header
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/organization/my");
        request.Headers.Add(TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid(), "Auth User"));

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Admin Claim Tests

    [Test]
    public async Task UpdateOrganizationStatus_WithInstanceAdmin_ShouldNotReturnUnauthorized()
    {
        // Arrange — instance admin via X-Test-Auth header
        var dto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 1 };

        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"/api/organization/updatestatustype/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add(TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateInstanceAdminHeaderValue(Guid.NewGuid()));

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert — should NOT be 401; could be 404 (org not found) or other error
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateOrganizationStatus_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange — no X-Test-Auth header = anonymous
        var dto = new UpdateOrganizationApprovalStatusDto { ApprovalStatusId = 1 };

        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"/api/organization/updatestatustype/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(dto)
        };

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion
}
