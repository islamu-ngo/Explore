// ABOUTME: Unit tests for LanguageService covering list and by-id pass-through behavior.
// Verifies direct API returns and exception propagation without service-level recovery.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for LanguageService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetLanguagesAsync returns API collection on success
/// - GetLanguagesAsync throws when API fails
/// - GetLanguageByIdAsync returns DTO on success
/// - GetLanguageByIdAsync throws when API fails
/// </remarks>
public class LanguageServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly LanguageService _service;

    public LanguageServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new LanguageService(_client);
    }

    // ========== GetLanguagesAsync ==========

    #region GetLanguagesAsync Tests

    [Test]
    public async Task GetLanguagesAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<LanguageListDto> { new() { FullName = "English", MasterCode = "EN" } };
        _client.LanguageAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.LanguageAllAsync().Returns(list);

        // Act
        var result = await _service.GetLanguagesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetLanguagesAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.LanguageAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.LanguageAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetLanguagesAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetLanguageByIdAsync ==========

    #region GetLanguageByIdAsync Tests

    [Test]
    public async Task GetLanguageByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        const int id = 8;
        var dto = new LanguageDto { FullName = "French", MasterCode = "FR" };
        _client.LanguageAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(dto);
        _client.LanguageAsync(Arg.Any<int>()).Returns(dto);

        // Act
        var result = await _service.GetLanguageByIdAsync(id);

        // Assert
        await Assert.That(result.MasterCode).IsEqualTo("FR");
    }

    [Test]
    public async Task GetLanguageByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 8;
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.LanguageAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.LanguageAsync(Arg.Any<int>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetLanguageByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
