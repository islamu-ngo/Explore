// ABOUTME: Unit tests for MadhabService covering list and by-id pass-through behavior.
// Verifies no internal error handling exists and API exceptions are propagated.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for MadhabService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetMadhabsAsync returns API collection on success
/// - GetMadhabsAsync throws when API fails
/// - GetMadhabByIdAsync returns DTO on success
/// - GetMadhabByIdAsync throws when API fails
/// </remarks>
public class MadhabServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly MadhabService _service;

    public MadhabServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new MadhabService(_client);
    }

    // ========== GetMadhabsAsync ==========

    #region GetMadhabsAsync Tests

    [Test]
    public async Task GetMadhabsAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<MadhabListDto> { new() { FullName = "Shafi", MasterCode = "SHAF" } };
        _client.MadhabAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.MadhabAllAsync().Returns(list);

        // Act
        var result = await _service.GetMadhabsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetMadhabsAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.MadhabAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.MadhabAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetMadhabsAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetMadhabByIdAsync ==========

    #region GetMadhabByIdAsync Tests

    [Test]
    public async Task GetMadhabByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        const int id = 7;
        var dto = new MadhabDto { FullName = "Hanafi", MasterCode = "HANA" };
        _client.MadhabAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(dto);
        _client.MadhabAsync(Arg.Any<int>()).Returns(dto);

        // Act
        var result = await _service.GetMadhabByIdAsync(id);

        // Assert
        await Assert.That(result.FullName).IsEqualTo("Hanafi");
    }

    [Test]
    public async Task GetMadhabByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 7;
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.MadhabAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.MadhabAsync(Arg.Any<int>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetMadhabByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
