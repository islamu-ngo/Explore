// ABOUTME: Unit tests for AudienceAgeService covering age lookup list and by-id retrieval behavior.
// Verifies successful pass-through and exception propagation from the API client.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for AudienceAgeService.
/// </summary>
public class AudienceAgeServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly AudienceAgeService _service;

    public AudienceAgeServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _service = new AudienceAgeService(_apiClient);
    }

    // ========== GetAudienceAgesAsync ==========

    #region GetAudienceAgesAsync Tests

    [Test]
    public async Task GetAudienceAgesAsync_ReturnsData_WhenApiSucceeds()
    {
        // Arrange
        var items = new List<AudienceAgeListDto> { new() };
        _apiClient.AudienceageAllAsync(Arg.Any<CancellationToken>()).Returns(items);

        // Act
        var result = await _service.GetAudienceAgesAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetAudienceAgesAsync_Throws_WhenApiThrows()
    {
        // Arrange
        _apiClient.AudienceageAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act & Assert
        await Assert.That(() => _service.GetAudienceAgesAsync()).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetAudienceAgeByIdAsync ==========

    #region GetAudienceAgeByIdAsync Tests

    [Test]
    public async Task GetAudienceAgeByIdAsync_ReturnsItem_WhenApiSucceeds()
    {
        // Arrange
        const int id = 1;
        var item = new AudienceAgeDto();

        _apiClient.AudienceageAsync(id, Arg.Any<CancellationToken>()).Returns(item);
        _apiClient.AudienceageAsync(id).Returns(item);

        // Act
        var result = await _service.GetAudienceAgeByIdAsync(id);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task GetAudienceAgeByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 1;

        _apiClient.AudienceageAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));
        _apiClient.AudienceageAsync(id)
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act & Assert
        await Assert.That(() => _service.GetAudienceAgeByIdAsync(id)).ThrowsExactly<ApiException>();
    }

    #endregion
}
