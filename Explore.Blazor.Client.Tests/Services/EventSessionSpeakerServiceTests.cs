// ABOUTME: Unit tests for EventSessionSpeakerService temporary fallback behavior.
// ABOUTME: Verifies current stubbed API-regeneration placeholders remain deterministic.

namespace Explore.Blazor.Client.Tests.Services;

public class EventSessionSpeakerServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly EventSessionSpeakerService _service;

    public EventSessionSpeakerServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _service = new EventSessionSpeakerService(_apiClient);
    }

    [Test]
    public async Task GetSpeakersBySessionAsync_ReturnsEmptyCollection()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var result = await _service.GetSpeakersBySessionAsync(sessionId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task AddSpeakerToSessionAsync_ReturnsNull()
    {
        // Arrange
        var speaker = new { Name = "Speaker" };

        // Act
        var result = await _service.AddSpeakerToSessionAsync(speaker);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task RemoveSpeakerFromSessionAsync_ReturnsFalse()
    {
        // Arrange
        var speakerId = Guid.NewGuid();

        // Act
        var result = await _service.RemoveSpeakerFromSessionAsync(speakerId);

        // Assert
        await Assert.That(result).IsFalse();
    }
}
