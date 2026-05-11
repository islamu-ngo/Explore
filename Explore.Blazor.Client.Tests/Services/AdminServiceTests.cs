// ABOUTME: Unit tests for AdminService covering organization management, approval operations,
// representative lookup tables, and CRUD operations for categories, tags, and locations.

using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Tests AdminService methods across five areas:
/// 1. Organization management (list, details, null/error handling)
/// 2. Approval status operations (approve, reject, revert)
/// 3. Lookup tables (representative selection: event types, madhabs, languages, approval statuses, actor types)
/// 4. Category CRUD (list, get by id, create, update with null id, delete, error)
/// 5. Tag CRUD (list, create, update, delete)
/// 6. Location CRUD (list, create, update, delete)
/// </summary>
public class AdminServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<AdminService> _logger;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<AdminService>>();
        _service = new AdminService(_apiClient, _logger);
    }

    // ========== Organization Management ==========

    #region GetOrganizationRequestsAsync Tests

    [Test]
    public async Task GetOrganizationRequestsAsync_ReturnsOrganizations_WhenApiSucceeds()
    {
        // Arrange
        var orgs = ComponentDataBuilder.OrganizationListDto.Generate(3);
        var halResponse = CreateOrgCollectionResponse(orgs);

        _apiClient.GetOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetOrganizationRequestsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.First().FullName).IsEqualTo(orgs.First().FullName);
    }

    [Test]
    public async Task GetOrganizationRequestsAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetOrganizationRequestsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetOrganizationRequestsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var halResponse = CreateOrgCollectionResponse(new List<OrganizationListDto>());
        _apiClient.GetOrganizationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetOrganizationRequestsAsync();

        // Assert
        await _apiClient.Received(1).GetOrganizationsAsync(
            ApiConstants.FirstPage,
            ApiConstants.DefaultPageSize,
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetOrganizationDetailsAsync_ReturnsOrganization_WhenFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var expected = ComponentDataBuilder.OrganizationDto.Generate();
        expected.Id = orgId;
        var halResponse = CreateOrgResourceResponse(expected);

        _apiClient.GetOrganizationByIdAsync(orgId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetOrganizationDetailsAsync(orgId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(orgId);
        await Assert.That(result.FullName).IsEqualTo(expected.FullName);
    }

    [Test]
    public async Task GetOrganizationDetailsAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.GetOrganizationByIdAsync(orgId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.GetOrganizationDetailsAsync(orgId);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== Approval Operations ==========

    #region Approval Status Tests

    [Test]
    public async Task ApproveOrganizationAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.UpdateOrganizationApprovalStatusAsync(orgId, Arg.Any<UpdateOrganizationApprovalStatusDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApproveOrganizationAsync(orgId);

        // Assert
        await Assert.That(result).IsTrue();
        await _apiClient.Received(1).UpdateOrganizationApprovalStatusAsync(
            orgId,
            Arg.Is<UpdateOrganizationApprovalStatusDto>(d => d.ApprovalStatusId == ApprovalStatusId.Approved),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApproveOrganizationAsync_ReturnsTrue_WhenApiThrowsStatus200()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.UpdateOrganizationApprovalStatusAsync(orgId, Arg.Any<UpdateOrganizationApprovalStatusDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("No content body", 200));

        // Act
        var result = await _service.ApproveOrganizationAsync(orgId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task RejectOrganizationAsync_ReturnsTrue_WhenApiThrowsStatus204()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.UpdateOrganizationApprovalStatusAsync(orgId, Arg.Any<UpdateOrganizationApprovalStatusDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("No content body", 204));

        // Act
        var result = await _service.RejectOrganizationAsync(orgId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task RejectOrganizationAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.UpdateOrganizationApprovalStatusAsync(orgId, Arg.Any<UpdateOrganizationApprovalStatusDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RejectOrganizationAsync(orgId);

        // Assert
        await Assert.That(result).IsTrue();
        await _apiClient.Received(1).UpdateOrganizationApprovalStatusAsync(
            orgId,
            Arg.Is<UpdateOrganizationApprovalStatusDto>(d => d.ApprovalStatusId == ApprovalStatusId.Rejected),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RevertToPendingAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        _apiClient.UpdateOrganizationApprovalStatusAsync(orgId, Arg.Any<UpdateOrganizationApprovalStatusDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Forbidden", 403));

        // Act
        var result = await _service.RevertToPendingAsync(orgId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    // ========== Lookup Tables (Representative Selection) ==========

    #region Lookup Table Tests

    [Test]
    public async Task GetEventTypesAsync_ReturnsTypes_WhenApiSucceeds()
    {
        // Arrange
        var types = new List<EventTypeListDto>
        {
            new() { Id = 1, FullName = "Conference", MasterCode = "CONF" },
            new() { Id = 2, FullName = "Workshop", MasterCode = "WKSP" }
        };
        _apiClient.GetEventTypesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(types);

        // Act
        var result = await _service.GetEventTypesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo("Conference");
    }

    [Test]
    public async Task GetEventTypesAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetEventTypesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.GetEventTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMadhabsAsync_ReturnsData_WhenApiSucceeds()
    {
        // Arrange
        var madhabs = new List<MadhabListDto>
        {
            new() { Id = 1, FullName = "Hanafi", MasterCode = "HAN" },
            new() { Id = 2, FullName = "Maliki", MasterCode = "MAL" }
        };
        _apiClient.GetMadhabsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(madhabs);

        // Act
        var result = await _service.GetMadhabsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo("Hanafi");
    }

    [Test]
    public async Task GetLanguagesAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetLanguagesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.GetLanguagesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetApprovalStatusesAsync_ReturnsStatuses_WhenApiSucceeds()
    {
        // Arrange
        var statuses = new List<StatusTypeListDto>
        {
            new() { Id = 1, FullName = "Pending", MasterCode = "PEND" },
            new() { Id = 2, FullName = "Approved", MasterCode = "APPR" }
        };
        _apiClient.GetApprovalStatusOptionsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(statuses);

        // Act
        var result = await _service.GetApprovalStatusesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo("Pending");
    }

    [Test]
    public async Task GetActorTypesAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetActorTypesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.GetActorTypesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== Category CRUD ==========

    #region Category CRUD Tests

    [Test]
    public async Task GetCategoriesAsync_ReturnsCategories_WhenApiSucceeds()
    {
        // Arrange
        var categories = ComponentDataBuilder.CategoryListDto.Generate(3);
        var halResponse = CreateCategoryCollectionResponse(categories);

        _apiClient.GetCategoriesAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.First().FullName).IsEqualTo(categories.First().FullName);
    }

    [Test]
    public async Task GetCategoriesAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetCategoriesAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetCategoryByIdAsync_ReturnsCategory_WhenFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var halResponse = new HalResourceOfCategoryDto { Id = categoryId, FullName = "Education", MasterCode = "EDU" };

        _apiClient.GetCategoryByIdAsync(categoryId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);
        _apiClient.GetCategoryByIdAsync(categoryId)
            .Returns(halResponse);

        // Act
        var result = await _service.GetCategoryByIdAsync(categoryId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FullName).IsEqualTo("Education");
        await Assert.That(result.MasterCode).IsEqualTo("EDU");
    }

    [Test]
    public async Task GetCategoryByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _apiClient.GetCategoryByIdAsync(categoryId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));
        _apiClient.GetCategoryByIdAsync(categoryId)
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.GetCategoryByIdAsync(categoryId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task CreateCategoryAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var dto = new CreateCategoryDto { FullName = "New Category" };
        _apiClient.CreateCategoryAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse());

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        await Assert.That(result).IsTrue();
        await _apiClient.Received(1).CreateCategoryAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateCategoryAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        var dto = new CreateCategoryDto { FullName = "New Category" };
        _apiClient.CreateCategoryAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Bad Request", 400));

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UpdateCategoryAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var dto = new UpdateCategoryDto { Id = categoryId, FullName = "Updated Category" };
        _apiClient.UpdateCategoryAsync(categoryId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse(categoryId));

        // Act
        var result = await _service.UpdateCategoryAsync(dto);

        // Assert
        await Assert.That(result).IsTrue();
        await _apiClient.Received(1).UpdateCategoryAsync(categoryId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateCategoryAsync_ReturnsFalse_WhenIdIsNull()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Id = null, FullName = "No ID Category" };

        // Act
        var result = await _service.UpdateCategoryAsync(dto);

        // Assert
        await Assert.That(result).IsFalse();
        await _apiClient.DidNotReceive().UpdateCategoryAsync(
            Arg.Any<Guid>(), Arg.Any<UpdateCategoryDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteCategoryAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _apiClient.DeleteCategoryAsync(categoryId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteCategoryAsync(categoryId);

        // Assert
        await Assert.That(result).IsTrue();
        await _apiClient.Received(1).DeleteCategoryAsync(categoryId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _apiClient.DeleteCategoryAsync(categoryId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Forbidden", 403));

        // Act
        var result = await _service.DeleteCategoryAsync(categoryId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    // ========== Tag CRUD ==========

    #region Tag CRUD Tests

    [Test]
    public async Task GetTagsAsync_ReturnsTags_WhenApiSucceeds()
    {
        // Arrange
        var tags = ComponentDataBuilder.TagListDto.Generate(2);
        var halResponse = CreateTagCollectionResponse(tags);

        _apiClient.GetTagsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetTagsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo(tags.First().FullName);
    }

    [Test]
    public async Task CreateTagAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var dto = new CreateTagDto { FullName = "New Tag" };
        _apiClient.CreateTagAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse());

        // Act
        var result = await _service.CreateTagAsync(dto);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UpdateTagAsync_ReturnsFalse_WhenIdIsNull()
    {
        // Arrange
        var dto = new UpdateTagDto { Id = null, FullName = "No ID Tag" };

        // Act
        var result = await _service.UpdateTagAsync(dto);

        // Assert
        await Assert.That(result).IsFalse();
        await _apiClient.DidNotReceive().UpdateTagAsync(
            Arg.Any<Guid>(), Arg.Any<UpdateTagDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteTagAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        _apiClient.DeleteTagAsync(tagId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteTagAsync(tagId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    #endregion

    // ========== Location CRUD ==========

    #region Location CRUD Tests

    [Test]
    public async Task GetLocationsAsync_ReturnsLocations_WhenApiSucceeds()
    {
        // Arrange
        var locations = ComponentDataBuilder.LocationListDto.Generate(2);
        var halResponse = CreateLocationCollectionResponse(locations);

        _apiClient.GetLocationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetLocationsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.First().FullName).IsEqualTo(locations.First().FullName);
    }

    [Test]
    public async Task CreateLocationAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var dto = new CreateLocationDto { FullName = "New Location" };
        _apiClient.CreateLocationAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse());

        // Act
        var result = await _service.CreateLocationAsync(dto);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UpdateLocationAsync_ReturnsFalse_WhenIdIsNull()
    {
        // Arrange
        var dto = new UpdateLocationDto { Id = null, FullName = "No ID Location" };

        // Act
        var result = await _service.UpdateLocationAsync(dto);

        // Assert
        await Assert.That(result).IsFalse();
        await _apiClient.DidNotReceive().UpdateLocationAsync(
            Arg.Any<Guid>(), Arg.Any<UpdateLocationDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteLocationAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        _apiClient.DeleteLocationAsync(locationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.DeleteLocationAsync(locationId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    // ========== Helper Methods ==========

    #region HAL Response Helpers

    private static HalCollectionResourceOfOrganizationListDto CreateOrgCollectionResponse(
        IList<OrganizationListDto> items)
    {
        return new HalCollectionResourceOfOrganizationListDto
        {
            _embedded = new HalCollectionEmbeddedOfOrganizationListDto
            {
                Items = items.Select(ToHalResource).ToList()
            }
        };
    }

    private static HalResourceOfOrganizationListDto ToHalResource(OrganizationListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfOrganizationListDto>(json)
               ?? new HalResourceOfOrganizationListDto();
    }

    private static HalResourceOfOrganizationDto CreateOrgResourceResponse(OrganizationDto dto)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfOrganizationDto>(json)
               ?? new HalResourceOfOrganizationDto();
    }

    private static HalCollectionResourceOfCategoryListDto CreateCategoryCollectionResponse(
        IList<CategoryListDto> items)
    {
        return new HalCollectionResourceOfCategoryListDto
        {
            _embedded = new HalCollectionEmbeddedOfCategoryListDto
            {
                Items = items.Select(ToHalResource).ToList()
            }
        };
    }

    private static HalResourceOfCategoryListDto ToHalResource(CategoryListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfCategoryListDto>(json)
               ?? new HalResourceOfCategoryListDto();
    }

    private static HalCollectionResourceOfTagListDto CreateTagCollectionResponse(
        IList<TagListDto> items)
    {
        return new HalCollectionResourceOfTagListDto
        {
            _embedded = new HalCollectionEmbeddedOfTagListDto
            {
                Items = items.Select(ToHalResource).ToList()
            }
        };
    }

    private static HalResourceOfTagListDto ToHalResource(TagListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfTagListDto>(json)
               ?? new HalResourceOfTagListDto();
    }

    private static HalCollectionResourceOfLocationListDto CreateLocationCollectionResponse(
        IList<LocationListDto> items)
    {
        return new HalCollectionResourceOfLocationListDto
        {
            _embedded = new HalCollectionEmbeddedOfLocationListDto
            {
                Items = items.Select(ToHalResource).ToList()
            }
        };
    }

    private static HalResourceOfLocationListDto ToHalResource(LocationListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfLocationListDto>(json)
               ?? new HalResourceOfLocationListDto();
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
