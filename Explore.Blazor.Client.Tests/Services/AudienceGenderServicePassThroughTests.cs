// ABOUTME: Unit tests for AudienceGenderService covering list and by-id pass-through behavior.
// Verifies direct API passthrough behavior and expected exception bubbling.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for AudienceGenderService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetAudienceGendersAsync returns API collection on success
/// - GetAudienceGendersAsync throws when API fails
/// - GetAudienceGenderByIdAsync returns DTO on success
/// - GetAudienceGenderByIdAsync throws when API fails
/// </remarks>
public class AudienceGenderServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly AudienceGenderService _service;

    public AudienceGenderServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new AudienceGenderService(_client);
    }

    // ========== GetAudienceGendersAsync ==========

    #region GetAudienceGendersAsync Tests

    [Test]
    public async Task GetAudienceGendersAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var list = new List<AudienceGenderListDto> { new() { FullName = "Men", MasterCode = "M" } };
        _client.AudiencegenderAllAsync(Arg.Any<CancellationToken>()).Returns(list);
        _client.AudiencegenderAllAsync().Returns(list);

        // Act
        var result = await _service.GetAudienceGendersAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetAudienceGendersAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.AudiencegenderAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.AudiencegenderAllAsync().ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetAudienceGendersAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetAudienceGenderByIdAsync ==========

    #region GetAudienceGenderByIdAsync Tests

    [Test]
    public async Task GetAudienceGenderByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        const int id = 4;
        var dto = new AudienceGenderDto { FullName = "Women", MasterCode = "F" };
        _client.AudiencegenderAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(dto);
        _client.AudiencegenderAsync(Arg.Any<int>()).Returns(dto);

        // Act
        var result = await _service.GetAudienceGenderByIdAsync(id);

        // Assert
        await Assert.That(result.MasterCode).IsEqualTo("F");
    }

    [Test]
    public async Task GetAudienceGenderByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        const int id = 4;
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.AudiencegenderAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.AudiencegenderAsync(Arg.Any<int>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetAudienceGenderByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
