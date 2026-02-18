// ABOUTME: Unit tests for OrganizationService covering read and write operations with HAL conversion.
// Validates pagination constants, error handling behavior, and API call contracts with NSubstitute.

using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for OrganizationService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - HAL collection/resource conversion to DTOs
/// - Read-operation fallback behavior (empty/null)
/// - Write-operation re-throw behavior for API failures
/// - Pagination usage with ApiConstants.FirstPage and ApiConstants.DefaultPageSize
/// </remarks>
public class OrganizationServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly Microsoft.Extensions.Logging.ILogger<OrganizationService> _logger;
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<OrganizationService>>();
        _service = new OrganizationService(_apiClient, _logger);
    }

    // ========== GetMyOrganizationsAsync ==========

    #region GetMyOrganizationsAsync Tests

    [Test]
    public async Task GetMyOrganizationsAsync_ReturnsOrganizations_WhenApiSucceeds()
    {
        // Arrange
        var organizations = ComponentDataBuilder.OrganizationListDto.Generate(2);
        var halResponse = CreateOrgCollectionResponse(organizations);

        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo(organizations.First().FullName);
    }

    [Test]
    public async Task GetMyOrganizationsAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("API Error", 500));

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyOrganizationsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfOrganizationListDto?)null);

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyOrganizationsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var halResponse = CreateOrgCollectionResponse(new List<OrganizationListDto>());
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetMyOrganizationsAsync();

        // Assert
        await _apiClient.Received(1).GetMyOrganizationsAsync(
            ApiConstants.FirstPage,
            ApiConstants.DefaultPageSize,
            Arg.Any<CancellationToken>());
    }

    #endregion

    // ========== GetOrganizationsByUserAsync ==========

    #region GetOrganizationsByUserAsync Tests

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsOrganizations_WhenApiSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizations = ComponentDataBuilder.OrganizationListDto.Generate(3);
        var halResponse = CreateOrgCollectionResponse(organizations);

        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.First().Id).IsEqualTo(organizations.First().Id);
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Forbidden", 403));

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var halResponse = CreateOrgCollectionResponse(new List<OrganizationListDto>());

        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await _apiClient.Received(1).GetMyOrganizationsAsync(
            ApiConstants.FirstPage,
            ApiConstants.DefaultPageSize,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfOrganizationListDto?)null);

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsEmptyList_WhenApiThrowsServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== GetOrganizationByIdAsync ==========

    #region GetOrganizationByIdAsync Tests

    [Test]
    public async Task GetOrganizationByIdAsync_ReturnsOrganization_WhenFound()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = ComponentDataBuilder.OrganizationDto.Generate();
        organization.Id = organizationId;
        var halResponse = CreateOrgResourceResponse(organization);

        _apiClient.GetOrganizationByIdAsync(organizationId, Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetOrganizationByIdAsync(organizationId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(organizationId);
        await Assert.That(result.FullName).IsEqualTo(organization.FullName);
    }

    [Test]
    public async Task GetOrganizationByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _apiClient.GetOrganizationByIdAsync(organizationId, Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.GetOrganizationByIdAsync(organizationId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOrganizationByIdAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _apiClient.GetOrganizationByIdAsync(organizationId, Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetOrganizationByIdAsync(organizationId);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== CreateOrganizationAsync ==========

    #region CreateOrganizationAsync Tests

    [Test]
    public async Task CreateOrganizationAsync_ReturnsResponse_WhenSuccess()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateOrganizationDto.Generate();
        var expectedResponse = ComponentDataBuilder.SuccessResponse();

        _apiClient.CreateOrganizationAsync(Arg.Any<CreateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.CreateOrganizationAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task CreateOrganizationAsync_Throws_WhenApiThrowsApiException()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateOrganizationDto.Generate();
        _apiClient.CreateOrganizationAsync(Arg.Any<CreateOrganizationDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server error", 500));

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(async () => await _service.CreateOrganizationAsync(createDto));
    }

    #endregion

    // ========== UpdateOrganizationAsync ==========

    #region UpdateOrganizationAsync Tests

    [Test]
    public async Task UpdateOrganizationAsync_ReturnsResponse_WhenSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateDto = new UpdateOrganizationDto
        {
            FullName = "Updated Organization",
            Email = "updated@example.com"
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(organizationId);

        _apiClient.UpdateOrganizationAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.UpdateOrganizationAsync(organizationId, updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(organizationId);
    }

    [Test]
    public async Task UpdateOrganizationAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var updateDto = new UpdateOrganizationDto
        {
            FullName = "Updated Organization",
            Email = "updated@example.com"
        };

        _apiClient.UpdateOrganizationAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Bad Request", 400));

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(async () => await _service.UpdateOrganizationAsync(organizationId, updateDto));
    }

    #endregion

    // ========== GetStatusTypesAsync ==========

    #region GetStatusTypesAsync Tests

    [Test]
    public async Task GetStatusTypesAsync_ReturnsStatusTypes_WhenSuccess()
    {
        // Arrange
        var statuses = new List<StatusTypeListDto>
        {
            new() { Id = 1, FullName = "Pending", MasterCode = "PEND" },
            new() { Id = 2, FullName = "Approved", MasterCode = "APPR" }
        };
        _apiClient.ApprovalstatusAllAsync(Arg.Any<CancellationToken>())
            .Returns(statuses);

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo("Pending");
    }

    [Test]
    public async Task GetStatusTypesAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.ApprovalstatusAllAsync(Arg.Any<CancellationToken>())
            .Returns((ICollection<StatusTypeListDto>?)null);

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetStatusTypesAsync_ReturnsEmptyList_WhenApiThrows_Exception()
    {
        // Arrange
        _apiClient.ApprovalstatusAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Internal Server Error", 500));

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetStatusTypesAsync_ReturnsEmptyList_WhenApiReturnsUnauthorized()
    {
        // Arrange
        _apiClient.ApprovalstatusAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== HAL Response Helpers ==========

    #region HAL Response Helpers

    private static HalCollectionResourceOfOrganizationListDto CreateOrgCollectionResponse(
        IList<OrganizationListDto> items)
    {
        return new HalCollectionResourceOfOrganizationListDto
        {
            _embedded = new HalCollectionEmbeddedOfOrganizationListDto
            {
                Items = items.Cast<object>().ToList()
            }
        };
    }

    private static HalResourceOfOrganizationDto CreateOrgResourceResponse(OrganizationDto dto)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfOrganizationDto>(json)
               ?? new HalResourceOfOrganizationDto();
    }

    private static ApiException CreateApiException(string message, int statusCode, string response = "")
    {
        return new ApiException(
            message,
            statusCode,
            response,
            new Dictionary<string, IEnumerable<string>>(),
            new InvalidOperationException(message));
    }

    #endregion
}
