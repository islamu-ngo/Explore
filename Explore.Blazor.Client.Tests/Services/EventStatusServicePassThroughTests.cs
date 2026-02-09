// ABOUTME: Unit tests for EventStatusService covering list and by-id pass-through behavior.
// Verifies successful DTO returns and direct exception propagation from API calls.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for EventStatusService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetEventStatusesAsync returns API collection on success
/// - GetEventStatusesAsync throws when API fails
/// - GetEventStatusByIdAsync returns DTO on success
/// - GetEventStatusByIdAsync throws when API fails
/// </remarks>
public class EventStatusServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly EventStatusService _service;

    public EventStatusServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new EventStatusService(_client);
    }

    // ========== GetEventStatusesAsync ==========

    #region GetEventStatusesAsync Tests

    [Test]
    public async Task GetEventStatusesAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<EventStatusListDto> { new() { FullName = "Draft", MasterCode = "DRAFT" } };
        _client.EventstatusAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.EventstatusAllAsync().Returns(list);

        // Act
        var result = await _service.GetEventStatusesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetEventStatusesAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.EventstatusAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.EventstatusAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetEventStatusesAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetEventStatusByIdAsync ==========

    #region GetEventStatusByIdAsync Tests

    [Test]
    public async Task GetEventStatusByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        const int id = 2;
        var dto = new EventStatusDto { FullName = "Published", MasterCode = "PUB" };
        _client.EventstatusAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(dto);
        _client.EventstatusAsync(Arg.Any<int>()).Returns(dto);

        // Act
        var result = await _service.GetEventStatusByIdAsync(id);

        // Assert
        await Assert.That(result.FullName).IsEqualTo("Published");
    }

    [Test]
    public async Task GetEventStatusByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 2;
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.EventstatusAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.EventstatusAsync(Arg.Any<int>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetEventStatusByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
