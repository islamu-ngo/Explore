// ABOUTME: Unit tests for UserService covering user sync, current-user retrieval, update, and delete operations.
// Validates retry/sync edge cases, API error handling, and return contracts for user workflows.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for UserService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - SyncUserAsync success and special ApiException(200) handling
/// - GetCurrentUserAsync 404 auto-sync and retry behavior
/// - GetCurrentUserAsync 401 handling
/// - Update and delete fallback behavior on failures
/// </remarks>
public class UserServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly Microsoft.Extensions.Logging.ILogger<UserService> _logger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<UserService>>();
        _service = new UserService(_apiClient, _logger);
    }

    // ========== SyncUserAsync ==========

    #region SyncUserAsync Tests

    [Test]
    public async Task SyncUserAsync_ReturnsResponse_WhenApiSucceeds()
    {
        // Arrange
        var expectedResponse = ComponentDataBuilder.SuccessResponse();
        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.SyncUserAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task SyncUserAsync_ReturnsSuccess_WhenApiThrowsStatus200()
    {
        // Arrange
        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Response parsing issue", 200));

        // Act
        var result = await _service.SyncUserAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("User synced successfully");
    }

    [Test]
    public async Task SyncUserAsync_ReturnsFailure_WhenApiThrowsNon200()
    {
        // Arrange
        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401, "unauthorized"));

        // Act
        var result = await _service.SyncUserAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.Message).Contains("API error");
    }

    #endregion

    // ========== GetCurrentUserAsync ==========

    #region GetCurrentUserAsync Tests

    [Test]
    public async Task GetCurrentUserAsync_ReturnsUser_WhenApiSucceeds()
    {
        // Arrange
        var expectedUser = ComponentDataBuilder.UserDto.Generate();
        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedUser);

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(expectedUser.Id);
        await Assert.That(result.Email).IsEqualTo(expectedUser.Email);
    }

    [Test]
    public async Task GetCurrentUserAsync_RetriesAfterSync_WhenInitialCallReturns404()
    {
        // Arrange
        var expectedUser = ComponentDataBuilder.UserDto.Generate();
        var callCount = 0;

        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw CreateApiException("Not Found", 404);
                }

                return expectedUser;
            });

        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse());

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(expectedUser.Id);
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenInitialCallReturns404AndSyncFails()
    {
        // Arrange
        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Sync failed",
                Errors = new List<string> { "sync error" }
            });

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenInitialCallReturns404AndSyncThrows()
    {
        // Arrange
        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Gateway timeout", 504));

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenRetryAfterSyncStillFails()
    {
        // Arrange
        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        _apiClient.SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse());

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNull();
        await _apiClient.Received(2).GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenApiReturns401()
    {
        // Arrange
        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNull();
        await _apiClient.DidNotReceive().SyncUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenApiThrowsNon404Non401()
    {
        // Arrange
        _apiClient.GetCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetCurrentUserAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== GetAdminAuthorityAsync ==========

    #region GetAdminAuthorityAsync Tests

    [Test]
    public async Task GetAdminAuthorityAsync_ReturnsAuthority_WhenApiSucceeds()
    {
        // Arrange
        var authority = new AdminAuthorityDto
        {
            IsInstanceAdmin = true,
            AdminTenantIds = [Guid.NewGuid()],
            AdminOrganizationIds = [],
            HasAnyAuthority = true
        };
        _apiClient.GetCurrentUserAdminAuthorityAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(authority);

        // Act
        var result = await _service.GetAdminAuthorityAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsInstanceAdmin).IsTrue();
        await Assert.That(result.HasAnyAuthority).IsTrue();
    }

    [Test]
    public async Task GetAdminAuthorityAsync_ReturnsNull_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetCurrentUserAdminAuthorityAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.GetAdminAuthorityAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== UpdateUserAsync ==========

    #region UpdateUserAsync Tests

    [Test]
    public async Task UpdateUserAsync_ReturnsResponse_WhenApiSucceeds()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            FirstName = "Updated",
            LastName = "User"
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse();

        _apiClient.UpdateCurrentUserAsync(Arg.Any<UpdateUserDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.UpdateUserAsync(updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task UpdateUserAsync_ReturnsNull_WhenApiThrows()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            FirstName = "Updated",
            LastName = "User"
        };

        _apiClient.UpdateCurrentUserAsync(Arg.Any<UpdateUserDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Bad Request", 400));

        // Act
        var result = await _service.UpdateUserAsync(updateDto);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== DeleteUserAsync ==========

    #region DeleteUserAsync Tests

    [Test]
    public async Task DeleteUserAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        _apiClient.DeleteCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteUserAsync();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DeleteUserAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        _apiClient.DeleteCurrentUserAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Forbidden", 403));

        // Act
        var result = await _service.DeleteUserAsync();

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    private static ApiException CreateApiException(string message, int statusCode, string response = "")
    {
        return new ApiException(
            message,
            statusCode,
            response,
            new Dictionary<string, IEnumerable<string>>(),
            new InvalidOperationException(message));
    }
}
