// ABOUTME: Unit tests for AudienceAgeService covering list and by-id pass-through behavior.
// Verifies that successful API results are returned and API exceptions are not swallowed.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for AudienceAgeService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetAudienceAgesAsync returns API collection on success
/// - GetAudienceAgesAsync throws when API fails
/// - GetAudienceAgeByIdAsync returns DTO on success
/// - GetAudienceAgeByIdAsync throws when API fails
/// </remarks>
public class AudienceAgeServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly AudienceAgeService _service;

    public AudienceAgeServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new AudienceAgeService(_client);
    }

    // ========== GetAudienceAgesAsync ==========

    #region GetAudienceAgesAsync Tests

    [Test]
    public async Task GetAudienceAgesAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<AudienceAgeListDto> { new() { FullName = "Adults", MasterCode = "ADULT" } };
        _client.AudienceageAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.AudienceageAllAsync().Returns(list);

        // Act
        var result = await _service.GetAudienceAgesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetAudienceAgesAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.AudienceageAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.AudienceageAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetAudienceAgesAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetAudienceAgeByIdAsync ==========

    #region GetAudienceAgeByIdAsync Tests

    [Test]
    public async Task GetAudienceAgeByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        const int id = 3;
        var dto = new AudienceAgeDto { FullName = "Youth", MasterCode = "YTH" };
        _client.AudienceageAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(dto);
        _client.AudienceageAsync(Arg.Any<int>()).Returns(dto);

        // Act
        var result = await _service.GetAudienceAgeByIdAsync(id);

        // Assert
        await Assert.That(result.FullName).IsEqualTo("Youth");
    }

    [Test]
    public async Task GetAudienceAgeByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 3;
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.AudienceageAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.AudienceageAsync(Arg.Any<int>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetAudienceAgeByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
