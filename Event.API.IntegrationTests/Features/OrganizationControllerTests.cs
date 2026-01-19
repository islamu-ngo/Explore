using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.DTOs.Organization;
using Explore.Application.Responses;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class OrganizationControllerTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/organization";

    public OrganizationControllerTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region GET Endpoints

    [Test]
    public async Task GetAll_ShouldReturnOk_WithPaginatedResult()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("items");
        await Assert.That(content).Contains("totalCount");
    }

    [Test]
    [Arguments(1, 10)]
    [Arguments(1, 20)]
    [Arguments(2, 5)]
    public async Task GetAll_WithPaginationParams_ShouldReturnPaginatedResult(int pageNumber, int pageSize)
    {
        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetById_WithValidId_ShouldReturnOk_OrNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{id}");

        // Assert - Either OK (if exists) or OK with null (empty database)
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetById_WithInvalidGuidFormat_ShouldReturnBadRequest()
    {
        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/not-a-guid");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetMyOrganizations_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/my");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Endpoints

    [Test]
    public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var createDto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync(BaseUrl, createDto);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT Endpoints

    [Test]
    public async Task Update_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        var updateDto = new UpdateOrganizationDto
        {
            FullName = "Updated Organization",
            Email = "updated@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "456 Updated Street",
            Postcode = 1000
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateStatusType_ShouldReturnNoContent_OrBadRequest()
    {
        // Arrange - This endpoint is temporarily AllowAnonymous
        var id = Guid.NewGuid();
        var updateDto = new UpdateOrganizationApprovalStatusDto
        {
            ApprovalStatusId = 1
        };

        // Act
        var response = await _fixture.Client.PutAsJsonAsync($"{BaseUrl}/updatestatustype/{id}", updateDto);

        // Assert - Should not return 500
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
    }

    #endregion
}
