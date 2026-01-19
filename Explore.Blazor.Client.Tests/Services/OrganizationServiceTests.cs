namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for OrganizationService.
/// Tests organization CRUD operations and membership management.
/// </summary>
/// <remarks>
/// These tests verify:
/// - Proper API client calls with correct method signatures
/// - Error handling and fallback behavior
/// - Response transformation
/// - Edge cases (null responses, exceptions)
///
/// IMPORTANT: The API client has two overloads for each method:
/// - Without CancellationToken (used by the service)
/// - With CancellationToken
/// We must mock the correct overload (without CancellationToken) for tests to work.
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
        var response = new PaginatedResultOfOrganizationListDto
        {
            Items = expectedOrgs,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 100
        };
        // Service calls My2Async(pageNumber: 1, pageSize: 100) without CancellationToken
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

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
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
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
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns((PaginatedResultOfOrganizationListDto?)null);

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyOrganizationsAsync_ReturnsEmptyList_WhenItemsIsNull()
    {
        // Arrange
        var response = new PaginatedResultOfOrganizationListDto
        {
            Items = null,
            TotalCount = 0
        };
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        var result = await _service.GetMyOrganizationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyOrganizationsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var response = new PaginatedResultOfOrganizationListDto
        {
            Items = new List<OrganizationListDto>(),
            TotalCount = 0
        };
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        await _service.GetMyOrganizationsAsync();

        // Assert - Service should request page 1 with size 100
        await _apiClient.Received(1).My2Async(1, 100);
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
        // Service calls OrganizationGET2Async(id) without CancellationToken
        _apiClient.OrganizationGET2Async(orgId)
            .Returns(expectedOrg);

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
        _apiClient.OrganizationGET2Async(orgId)
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
        _apiClient.OrganizationGET2Async(orgId)
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
        _apiClient.OrganizationGET2Async(Arg.Any<Guid>())
            .Returns(expectedOrg);

        // Act
        await _service.GetOrganizationByIdAsync(orgId);

        // Assert
        await _apiClient.Received(1).OrganizationGET2Async(orgId);
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
        // Service calls OrganizationPOSTAsync(organization) without CancellationToken
        _apiClient.OrganizationPOSTAsync(Arg.Any<CreateOrganizationDto>())
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
        _apiClient.OrganizationPOSTAsync(Arg.Any<CreateOrganizationDto>())
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
        _apiClient.OrganizationPOSTAsync(Arg.Any<CreateOrganizationDto>())
            .Returns(expectedResponse);

        // Act
        await _service.CreateOrganizationAsync(createDto);

        // Assert
        await _apiClient.Received(1).OrganizationPOSTAsync(createDto);
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
        _apiClient.OrganizationPOSTAsync(Arg.Any<CreateOrganizationDto>())
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
        // UpdateOrganizationDto doesn't have Id - the orgId is passed separately to the API
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
        // Service calls OrganizationPUTAsync(id, organization) without CancellationToken
        _apiClient.OrganizationPUTAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>())
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
        _apiClient.OrganizationPUTAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>())
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
        _apiClient.OrganizationPUTAsync(Arg.Any<Guid>(), Arg.Any<UpdateOrganizationDto>())
            .Returns(expectedResponse);

        // Act
        await _service.UpdateOrganizationAsync(orgId, updateDto);

        // Assert
        await _apiClient.Received(1).OrganizationPUTAsync(orgId, updateDto);
    }

    #endregion

    #region GetOrganizationsByUserAsync Tests

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsOrganizations_WhenApiSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedOrgs = ComponentDataBuilder.OrganizationListDto.Generate(3);
        // Service calls OrganizationsAsync(userId) without CancellationToken
        _apiClient.OrganizationsAsync(userId)
            .Returns(expectedOrgs);

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_FallsBackToMy2Async_WhenOrganizationsAsyncFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedOrgs = ComponentDataBuilder.OrganizationListDto.Generate(2);
        var fallbackResponse = new PaginatedResultOfOrganizationListDto
        {
            Items = expectedOrgs,
            TotalCount = 2
        };

        _apiClient.OrganizationsAsync(userId)
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(fallbackResponse);

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_ReturnsEmptyList_WhenBothEndpointsFail()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _apiClient.OrganizationsAsync(userId)
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));
        _apiClient.My2Async(Arg.Any<int?>(), Arg.Any<int?>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act
        var result = await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetOrganizationsByUserAsync_CallsApiWithCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedOrgs = ComponentDataBuilder.OrganizationListDto.Generate(1);
        _apiClient.OrganizationsAsync(Arg.Any<Guid>())
            .Returns(expectedOrgs);

        // Act
        await _service.GetOrganizationsByUserAsync(userId);

        // Assert
        await _apiClient.Received(1).OrganizationsAsync(userId);
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
        // Service calls ApprovalStatusAllAsync() without CancellationToken
        _apiClient.ApprovalStatusAllAsync()
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
        _apiClient.ApprovalStatusAllAsync()
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
        _apiClient.ApprovalStatusAllAsync()
            .Returns((ICollection<StatusTypeListDto>?)null);

        // Act
        var result = await _service.GetStatusTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion
}
