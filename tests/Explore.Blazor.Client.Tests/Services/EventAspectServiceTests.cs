// ABOUTME: Unit tests for EventAspectService Islamic and Tech aspect operations.
// Verifies Get/Upsert/Delete success and expected error-handling contracts.

namespace Explore.Blazor.Client.Tests.Services;

public class EventAspectServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventAspectService> _logger;
    private readonly EventAspectService _service;

    public EventAspectServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<EventAspectService>>();
        _service = (EventAspectService)Activator.CreateInstance(typeof(EventAspectService), _apiClient, _logger)!;
    }

    #region Get Aspect Tests

    [Test]
    public async Task GetIslamicAspectAsync_ReturnsAspect_WhenApiSucceeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var expected = new EventIslamicAspectDto
        {
            MadhabId = 1,
            MadhabName = "Hanafi",
            IncludesQuranRecitation = true,
            PrimaryLanguageId = 1,
            PrimaryLanguageName = "Arabic"
        };

        _apiClient.GetEventIslamicAspectAsync(eventId).Returns(expected);

        // Act
        var result = await _service.GetIslamicAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MadhabName).IsEqualTo("Hanafi");
    }

    [Test]
    public async Task GetIslamicAspectAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.GetEventIslamicAspectAsync(eventId).ThrowsAsync(new ApiException("Not found", 404, null, null, null));

        // Act
        var result = await _service.GetIslamicAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetTechAspectAsync_ReturnsNull_WhenApiErrorOccurs()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.GetEventTechAspectAsync(eventId).ThrowsAsync(new ApiException("Error", 500, null, null, null));

        // Act
        var result = await _service.GetTechAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetTechAspectAsync_ReturnsNull_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.GetEventTechAspectAsync(eventId).ThrowsAsync(new InvalidOperationException("Unexpected"));

        // Act
        var result = await _service.GetTechAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region Upsert Aspect Tests

    [Test]
    public async Task UpsertIslamicAspectAsync_ReturnsResponse_WhenApiSucceeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dto = new CreateUpdateIslamicAspectDto
        {
            MadhabId = 1,
            IncludesQuranRecitation = true,
            PrimaryLanguageId = 1
        };
        var expected = new BaseCommandResponseOfGuid { Success = true, Id = eventId, Message = "Saved" };

        _apiClient.UpsertEventIslamicAspectAsync(eventId, dto).Returns(expected);

        // Act
        var result = await _service.UpsertIslamicAspectAsync(eventId, dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task UpsertIslamicAspectAsync_ReturnsNull_WhenApiErrorOccurs()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dto = new CreateUpdateIslamicAspectDto { MadhabId = 1 };

        _apiClient.UpsertEventIslamicAspectAsync(eventId, dto).ThrowsAsync(new ApiException("Error", 500, null, null, null));

        // Act
        var result = await _service.UpsertIslamicAspectAsync(eventId, dto);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpsertTechAspectAsync_ReturnsResponse_WhenApiSucceeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dto = new CreateUpdateTechAspectDto
        {
            GithubRepoUrl = "https://github.com/example/repo",
            RequiresLaptop = true,
            IsCodingCompetition = true,
            MaxTeamSize = 4
        };
        var expected = new BaseCommandResponseOfGuid { Success = true, Id = Guid.NewGuid(), Message = "Saved" };

        _apiClient.UpsertEventTechAspectAsync(eventId, dto).Returns(expected);

        // Act
        var result = await _service.UpsertTechAspectAsync(eventId, dto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task UpsertTechAspectAsync_ReturnsNull_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dto = new CreateUpdateTechAspectDto { RequiresLaptop = false };

        _apiClient.UpsertEventTechAspectAsync(eventId, dto).ThrowsAsync(new InvalidOperationException("Unexpected"));

        // Act
        var result = await _service.UpsertTechAspectAsync(eventId, dto);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region Delete Aspect Tests

    [Test]
    public async Task DeleteIslamicAspectAsync_ReturnsTrue_WhenApiSucceeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventIslamicAspectAsync(eventId).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteIslamicAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DeleteIslamicAspectAsync_ReturnsFalse_WhenApiErrorOccurs()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventIslamicAspectAsync(eventId).ThrowsAsync(new ApiException("Error", 500, null, null, null));

        // Act
        var result = await _service.DeleteIslamicAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DeleteTechAspectAsync_ReturnsTrue_WhenNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventTechAspectAsync(eventId).ThrowsAsync(new ApiException("Not found", 404, null, null, null));

        // Act
        var result = await _service.DeleteTechAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DeleteTechAspectAsync_ReturnsFalse_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventTechAspectAsync(eventId).ThrowsAsync(new InvalidOperationException("Unexpected"));

        // Act
        var result = await _service.DeleteTechAspectAsync(eventId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion
}
