// ABOUTME: Unit tests for OrganizationService.
// Tests organization CRUD operations and membership management.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for OrganizationService.
/// Tests organization CRUD operations and membership management.
/// </summary>
/// <remarks>
/// These tests verify:
/// - Proper API client calls with correct method signatures
/// - Error handling and fallback behavior
/// - Response transformation from HAL to DTO
/// - Edge cases (null responses, exceptions)
///
/// IMPORTANT: The API client uses HAL resource types:
/// - GetMyOrganizationsAsync returns HalCollectionResourceOfOrganizationListDto
/// - GetOrganizationByIdAsync returns HalResourceOfOrganizationDto
/// The service converts these to plain DTOs using extension methods.
///
/// OrganizationService behavior:
/// - CreateOrganizationAsync: THROWS on exception
/// - UpdateOrganizationAsync: THROWS on exception
/// - GetMyOrganizationsAsync: Returns empty list on exception (doesn't throw)
/// - GetOrganizationByIdAsync: Returns null on exception (doesn't throw)
/// </remarks>
public class OrganizationServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<OrganizationService> _logger;
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<OrganizationService>>();
        _service = new OrganizationService(_apiClient, _logger);
    }

    #region GetMyOrganizationsAsync Tests

    [Test]
    public async Task GetMyOrganizationsAsync_ReturnsOrganizations_WhenApiSucceeds()
    {
        // Arrange
        var expectedOrgs = ComponentDataBuilder.OrganizationListDto.Generate(2);
        var halResponse = CreateHalCollectionResponse(expectedOrgs);

        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo(expectedOrgs.First().FullName);
    }

    [Test]
    public async Task GetMyOrganizationsAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Unauthorized", 401, null, null, null));

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
    public async Task GetMyOrganizationsAsync_ReturnsEmptyList_WhenEmbeddedIsNull()
    {
        // Arrange
        var halResponse = new HalCollectionResourceOfOrganizationListDto
        {
            _embedded = null
        };
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyOrganizationsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var halResponse = CreateHalCollectionResponse(new List<OrganizationListDto>());
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetMyOrganizationsAsync();

        // Assert - Service should request page 1 with size 100
        await _apiClient.Received(1).GetMyOrganizationsAsync(1, 100, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetOrganizationByIdAsync Tests

    [Test]
    public async Task GetOrganizationByIdAsync_ReturnsOrganization_WhenFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var expectedOrg = ComponentDataBuilder.OrganizationDto.Generate();
        expectedOrg.Id = orgId;
        var halResponse = CreateHalResourceResponse(expectedOrg);

        _apiClient.GetOrganizationByIdAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetOrganizationByIdAsync(orgId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(orgId);
        await Assert.That(result.FullName).IsEqualTo(expectedOrg.FullName);
    }

    [Test]
    public async Task GetOrganizationByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.GetOrganizationByIdAsync(orgId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act
        var result = await _service.GetOrganizationByIdAsync(orgId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOrganizationByIdAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.GetOrganizationByIdAsync(orgId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act
        var result = await _service.GetOrganizationByIdAsync(orgId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetOrganizationByIdAsync_CallsApiWithCorrectId()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var expectedOrg = ComponentDataBuilder.OrganizationDto.Generate();
        var halResponse = CreateHalResourceResponse(expectedOrg);

        _apiClient.GetOrganizationByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetOrganizationByIdAsync(orgId);

        // Assert
        await _apiClient.Received(1).GetOrganizationByIdAsync(orgId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateOrganizationAsync Tests

    [Test]
    public async Task CreateOrganizationAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateOrganizationDto.Generate();
        var expectedId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(expectedId);

        _apiClient.CreateOrganizationAsync(Arg.Any<CreateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.CreateOrganizationAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task CreateOrganizationAsync_ThrowsException_WhenApiThrowsException()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateOrganizationDto.Generate();
        _apiClient.CreateOrganizationAsync(Arg.Any<CreateOrganizationDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Bad Request", 400, "Validation failed", null, null));

        // Act & Assert - OrganizationService re-throws the exception
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _service.CreateOrganizationAsync(createDto));
    }

    [Test]
    public async Task CreateOrganizationAsync_CallsApiWithCorrectDto()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateOrganizationDto.Generate();
        var expectedResponse = ComponentDataBuilder.SuccessResponse();
        _apiClient.CreateOrganizationAsync(Arg.Any<CreateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.CreateOrganizationAsync(createDto);

        // Assert
        await _apiClient.Received(1).CreateOrganizationAsync(createDto, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateOrganizationAsync_ReturnsResponse_WithCorrectMessage()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateOrganizationDto.Generate();
        var expectedResponse = new BaseCommandResponseOfGuid
        {
            Success = true,
            Id = Guid.NewGuid(),
            Message = "Organization created successfully."
        };
        _apiClient.CreateOrganizationAsync(Arg.Any<CreateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.CreateOrganizationAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).IsEqualTo("Organization created successfully.");
    }

    #endregion

    #region UpdateOrganizationAsync Tests

    [Test]
    public async Task UpdateOrganizationAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var updateDto = new UpdateOrganizationDto
        {
            FullName = "Updated Name",
            Email = "updated@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Main Street",
            Postcode = 12345
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(orgId);

        _apiClient.UpdateOrganizationAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.UpdateOrganizationAsync(orgId, updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(orgId);
    }

    [Test]
    public async Task UpdateOrganizationAsync_ThrowsException_WhenApiThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var updateDto = new UpdateOrganizationDto
        {
            FullName = "Test Name",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Main Street",
            Postcode = 12345
        };
        _apiClient.UpdateOrganizationAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act & Assert - OrganizationService re-throws the exception
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _service.UpdateOrganizationAsync(orgId, updateDto));
    }

    [Test]
    public async Task UpdateOrganizationAsync_CallsApiWithCorrectParameters()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var updateDto = new UpdateOrganizationDto
        {
            FullName = "Test Name",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Main Street",
            Postcode = 12345
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(orgId);
        _apiClient.UpdateOrganizationAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.UpdateOrganizationAsync(orgId, updateDto);

        // Assert
        await _apiClient.Received(1).UpdateOrganizationAsync(orgId, updateDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetOrganizationsByUserAsync Tests

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsOrganizations_WhenApiSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedOrgs = ComponentDataBuilder.OrganizationListDto.Generate(3);
        var halResponse = CreateHalCollectionResponse(expectedOrgs);

        // Note: Current implementation falls back to GetMyOrganizationsAsync
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsEmptyList_WhenApiFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _apiClient.GetMyOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    #region GetStatusTypesAsync Tests

    [Test]
    public async Task GetStatusTypesAsync_ReturnsStatusTypes_WhenApiSucceeds()
    {
        // Arrange
        var expectedStatuses = new List<StatusTypeListDto>
        {
            new() { Id = 1, FullName = "Pending" },
            new() { Id = 2, FullName = "Approved" }
        };
        _apiClient.ApprovalStatusAllAsync(Arg.Any<CancellationToken>())
            .Returns(expectedStatuses);

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetStatusTypesAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.ApprovalStatusAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetStatusTypesAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.ApprovalStatusAllAsync(Arg.Any<CancellationToken>())
            .Returns((ICollection<StatusTypeListDto>?)null);

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a HAL collection response with the provided organization list items.
    /// </summary>
    private static HalCollectionResourceOfOrganizationListDto CreateHalCollectionResponse(
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

    /// <summary>
    /// Creates a HAL resource response from an organization DTO.
    /// Uses JSON serialization to properly populate all properties.
    /// </summary>
    private static HalResourceOfOrganizationDto CreateHalResourceResponse(OrganizationDto dto)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<HalResourceOfOrganizationDto>(json)
               ?? new HalResourceOfOrganizationDto();
    }

    #endregion
}
