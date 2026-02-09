// ABOUTME: Unit tests for EventFormatService covering list and by-id pass-through behavior.
// Verifies collection/DTO returns and propagated API exceptions with no internal catch.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for EventFormatService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetEventFormatsAsync returns API collection on success
/// - GetEventFormatsAsync throws when API fails
/// - GetEventFormatByIdAsync returns DTO on success
/// - GetEventFormatByIdAsync throws when API fails
/// </remarks>
public class EventFormatServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly EventFormatService _service;

    public EventFormatServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new EventFormatService(_client);
    }

    // ========== GetEventFormatsAsync ==========

    #region GetEventFormatsAsync Tests

    [Test]
    public async Task GetEventFormatsAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<EventFormatListDto> { new() { FullName = "Online", MasterCode = "ONLINE" } };
        _client.EventformatAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.EventformatAllAsync().Returns(list);

        // Act
        var result = await _service.GetEventFormatsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetEventFormatsAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.EventformatAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.EventformatAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetEventFormatsAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetEventFormatByIdAsync ==========

    #region GetEventFormatByIdAsync Tests

    [Test]
    public async Task GetEventFormatByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        const int id = 10;
        var dto = new EventFormatDto { FullName = "Hybrid", MasterCode = "HYB" };
        _client.EventformatAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(dto);
        _client.EventformatAsync(Arg.Any<int>()).Returns(dto);

        // Act
        var result = await _service.GetEventFormatByIdAsync(id);

        // Assert
        await Assert.That(result.MasterCode).IsEqualTo("HYB");
    }

    [Test]
    public async Task GetEventFormatByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 10;
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.EventformatAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.EventformatAsync(Arg.Any<int>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetEventFormatByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
