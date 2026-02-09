// ABOUTME: Unit tests for EventTypeService covering direct event-type lookup pass-through behavior.
// Verifies collection return on success and exception propagation on API failures.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for EventTypeService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetEventTypesAsync returns API collection as-is
/// - GetEventTypesAsync propagates ApiException (no service-level catch)
/// </remarks>
public class EventTypeServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly EventTypeService _service;

    public EventTypeServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new EventTypeService(_client);
    }

    // ========== GetEventTypesAsync ==========

    #region GetEventTypesAsync Tests

    [Test]
    public async Task GetEventTypesAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<EventTypeListDto>
        {
            new() { FullName = "Conference", MasterCode = "CONF" },
            new() { FullName = "Workshop", MasterCode = "WORK" }
        };

        _client.EventtypeAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.EventtypeAllAsync().Returns(list);

        // Act
        var result = await _service.GetEventTypesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetEventTypesAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.EventtypeAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.EventtypeAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetEventTypesAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
