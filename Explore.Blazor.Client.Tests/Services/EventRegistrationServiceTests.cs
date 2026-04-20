// ABOUTME: Unit tests for EventRegistrationService covering all registration operations.
// Tests GetAll, GetById, Register, Update, Cancel, BySession, ByUser, and IsUserRegistered.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Tests EventRegistrationService across eight areas:
/// 1. GetAllRegistrations (success, empty on error, empty on null items)
/// 2. GetRegistrationById (success, null on 404, null on error)
/// 3. RegisterForSession (success, returns failure response on error, calls api with dto)
/// 4. UpdateRegistration (success, returns failure response on error)
/// 5. CancelRegistration (success, false on error)
/// 6. GetRegistrationsBySession (success, empty on error)
/// 7. GetRegistrationsByUser (success, empty on error)
/// 8. IsUserRegisteredForSession (true when registered, false when not, false on error)
/// </summary>
public class EventRegistrationServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventRegistrationService> _logger;
    private readonly EventRegistrationService _service;

    public EventRegistrationServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<EventRegistrationService>>();
        _service = new EventRegistrationService(_apiClient, _logger);
    }

    // ========== GetAllRegistrationsAsync ==========

    #region GetAllRegistrationsAsync Tests

    [Test]
    public async Task GetAllRegistrationsAsync_ReturnsRegistrations_WhenApiSucceeds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrations = new List<EventRegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, EventSessionId = sessionId },
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), EventSessionId = sessionId }
        };
        var response = new PaginatedResultOfEventRegistrationListDto
        {
            Items = registrations,
            TotalCount = 2
        };

        _apiClient.GetEventRegistrationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _service.GetAllRegistrationsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetAllRegistrationsAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetEventRegistrationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetAllRegistrationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllRegistrationsAsync_ReturnsEmptyList_WhenItemsAreNull()
    {
        // Arrange
        var response = new PaginatedResultOfEventRegistrationListDto
        {
            Items = null,
            TotalCount = 0
        };
        _apiClient.GetEventRegistrationsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _service.GetAllRegistrationsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== GetRegistrationByIdAsync ==========

    #region GetRegistrationByIdAsync Tests

    [Test]
    public async Task GetRegistrationByIdAsync_ReturnsRegistration_WhenFound()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var expected = new EventRegistrationDto
        {
            Id = registrationId,
            UserId = Guid.NewGuid(),
            EventSessionId = Guid.NewGuid()
        };

        _apiClient.GetEventRegistrationByIdAsync(registrationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _service.GetRegistrationByIdAsync(registrationId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(registrationId);
    }

    [Test]
    public async Task GetRegistrationByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _apiClient.GetEventRegistrationByIdAsync(registrationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.GetRegistrationByIdAsync(registrationId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetRegistrationByIdAsync_ReturnsNull_WhenApiThrows()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _apiClient.GetEventRegistrationByIdAsync(registrationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetRegistrationByIdAsync(registrationId);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== GetRegistrationsPagedAsync ==========

    #region GetRegistrationsPagedAsync Tests

    [Test]
    public async Task GetRegistrationsPagedAsync_ReturnsMappedPaginatedResult_WhenApiSucceeds()
    {
        // Arrange
        var registrations = new List<EventRegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), EventSessionId = Guid.NewGuid() }
        };

        var response = new PaginatedResultOfEventRegistrationListDto
        {
            Items = registrations,
            PageNumber = 2,
            PageSize = 15,
            TotalCount = 47
        };

        _apiClient.GetEventRegistrationsAsync(2, 15, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _service.GetRegistrationsPagedAsync(2, 15);

        // Assert
        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.PageSize).IsEqualTo(15);
        await Assert.That(result.TotalCount).IsEqualTo(47);
    }

    [Test]
    public async Task GetRegistrationsPagedAsync_ReturnsEmptyResult_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetEventRegistrationsAsync(3, 20, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetRegistrationsPagedAsync(3, 20);

        // Assert
        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.PageNumber).IsEqualTo(3);
        await Assert.That(result.PageSize).IsEqualTo(20);
        await Assert.That(result.TotalCount).IsEqualTo(0);
    }

    #endregion

    // ========== RegisterForSessionAsync ==========

    #region RegisterForSessionAsync Tests

    [Test]
    public async Task RegisterForSessionAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var dto = new CreateEventRegistrationDto
        {
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RegistrationScopeId = 3, // SessionSelection
            SelectedSessionIds = new List<Guid> { sessionId },
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse();

        _apiClient.CreateEventRegistrationAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.RegisterForSessionAsync(dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task RegisterForSessionAsync_ReturnsFailureResponse_WhenApiThrows()
    {
        // Arrange
        var dto = new CreateEventRegistrationDto
        {
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RegistrationScopeId = 3,
            SelectedSessionIds = new List<Guid> { Guid.NewGuid() },
        };
        _apiClient.CreateEventRegistrationAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Bad Request", 400));

        // Act
        var result = await _service.RegisterForSessionAsync(dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    [Test]
    public async Task RegisterForSessionAsync_CallsApiWithCorrectDto()
    {
        // Arrange
        var dto = new CreateEventRegistrationDto
        {
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RegistrationScopeId = 3,
            SelectedSessionIds = new List<Guid> { Guid.NewGuid() },
        };
        _apiClient.CreateEventRegistrationAsync(Arg.Any<CreateEventRegistrationDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ComponentDataBuilder.SuccessResponse());

        // Act
        await _service.RegisterForSessionAsync(dto);

        // Assert
        await _apiClient.Received(1).CreateEventRegistrationAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    // ========== UpdateRegistrationAsync ==========

    #region UpdateRegistrationAsync Tests

    [Test]
    public async Task UpdateRegistrationAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var dto = new UpdateEventRegistrationDto { Id = registrationId };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(registrationId);

        _apiClient.UpdateEventRegistrationAsync(registrationId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.UpdateRegistrationAsync(registrationId, dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task UpdateRegistrationAsync_ReturnsFailureResponse_WhenApiThrows()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var dto = new UpdateEventRegistrationDto { Id = registrationId };
        _apiClient.UpdateEventRegistrationAsync(registrationId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Bad Request", 400));

        // Act
        var result = await _service.UpdateRegistrationAsync(registrationId, dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    #endregion

    // ========== CancelRegistrationAsync ==========

    #region CancelRegistrationAsync Tests

    [Test]
    public async Task CancelRegistrationAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _apiClient.DeleteEventRegistrationAsync(registrationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelRegistrationAsync(registrationId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CancelRegistrationAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _apiClient.DeleteEventRegistrationAsync(registrationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.CancelRegistrationAsync(registrationId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CancelRegistrationAsync_ReturnsFalse_WhenApiThrowsUnauthorized()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _apiClient.DeleteEventRegistrationAsync(registrationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.CancelRegistrationAsync(registrationId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    // ========== GetRegistrationsBySessionAsync ==========

    #region GetRegistrationsBySessionAsync Tests

    [Test]
    public async Task GetRegistrationsBySessionAsync_ReturnsRegistrations_WhenApiSucceeds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var registrations = new List<EventRegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), EventSessionId = sessionId },
            new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), EventSessionId = sessionId }
        };

        _apiClient.GetRegistrationsBySessionAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(registrations);

        // Act
        var result = await _service.GetRegistrationsBySessionAsync(sessionId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetRegistrationsBySessionAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _apiClient.GetRegistrationsBySessionAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.GetRegistrationsBySessionAsync(sessionId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetRegistrationsBySessionAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _apiClient.GetRegistrationsBySessionAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((ICollection<EventRegistrationListDto>?)null);

        // Act
        var result = await _service.GetRegistrationsBySessionAsync(sessionId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== GetRegistrationsByUserAsync ==========

    #region GetRegistrationsByUserAsync Tests

    [Test]
    public async Task GetRegistrationsByUserAsync_ReturnsRegistrations_WhenApiSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var registrations = new List<EventRegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, EventSessionId = Guid.NewGuid() }
        };

        _apiClient.GetRegistrationsByUserAsync(userId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(registrations);

        // Act
        var result = await _service.GetRegistrationsByUserAsync(userId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetRegistrationsByUserAsync_ReturnsEmptyList_WhenApiThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _apiClient.GetRegistrationsByUserAsync(userId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.GetRegistrationsByUserAsync(userId);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== IsUserRegisteredForSessionAsync ==========

    #region IsUserRegisteredForSessionAsync Tests

    [Test]
    public async Task IsUserRegisteredForSessionAsync_ReturnsTrue_WhenUserIsRegistered()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var registrations = new List<EventRegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, EventSessionId = sessionId }
        };

        _apiClient.GetRegistrationsBySessionAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(registrations);

        // Act
        var result = await _service.IsUserRegisteredForSessionAsync(sessionId, userId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsUserRegisteredForSessionAsync_ReturnsFalse_WhenUserIsNotRegistered()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var registrations = new List<EventRegistrationListDto>
        {
            new() { Id = Guid.NewGuid(), UserId = otherUserId, EventSessionId = sessionId }
        };

        _apiClient.GetRegistrationsBySessionAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(registrations);

        // Act
        var result = await _service.IsUserRegisteredForSessionAsync(sessionId, userId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsUserRegisteredForSessionAsync_ReturnsFalse_WhenApiThrows()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _apiClient.GetRegistrationsBySessionAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Error", 500));

        // Act
        var result = await _service.IsUserRegisteredForSessionAsync(sessionId, userId);

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
