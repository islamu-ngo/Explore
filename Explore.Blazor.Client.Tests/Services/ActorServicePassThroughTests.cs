// ABOUTME: Unit tests for ActorService covering HAL list/resource mapping and null-safe behavior.
// Verifies null fallback contracts and exception propagation for API failures.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for ActorService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetActorsAsync returns mapped items from HAL response
/// - GetActorsAsync returns empty when API returns null
/// - GetActorsAsync throws when API fails
/// - GetActorByIdAsync returns mapped DTO for HAL resource
/// - GetActorByIdAsync returns null for null resource and throws on API failure
/// </remarks>
public class ActorServicePassThroughTests
{
    private readonly IEventApiClient _client;
    private readonly ActorService _service;

    public ActorServicePassThroughTests()
    {
        _client = Substitute.For<IEventApiClient>();
        _service = new ActorService(_client);
    }

    // ========== GetActorsAsync ==========

    #region GetActorsAsync Tests

    [Test]
    public async Task GetActorsAsync_ReturnsCollection_WhenApiSucceeds()
    {
        // Arrange
        var actors = new List<ActorListDto>
        {
            new() { DisplayName = "Ali" },
            new() { DisplayName = "Zayd" }
        };

        var halResponse = new HalCollectionResourceOfActorListDto
        {
            _embedded = new HalCollectionEmbeddedOfActorListDto
            {
                Items = actors.Cast<object>().ToList()
            }
        };

        _client.GetActorsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(halResponse);
        _client.GetActorsAsync(Arg.Any<int?>(), Arg.Any<int?>()).Returns(halResponse);

        // Act
        var result = await _service.GetActorsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetActorsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _client.GetActorsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfActorListDto?)null);
        _client.GetActorsAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns((HalCollectionResourceOfActorListDto?)null);

        // Act
        var result = await _service.GetActorsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetActorsAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.GetActorsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.GetActorsAsync(Arg.Any<int?>(), Arg.Any<int?>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetActorsAsync();

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetActorByIdAsync ==========

    #region GetActorByIdAsync Tests

    [Test]
    public async Task GetActorByIdAsync_ReturnsDto_WhenApiSucceeds()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hal = new HalResourceOfActorDto { Id = id, DisplayName = "Ustadh Kareem" };
        _client.GetActorByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(hal);
        _client.GetActorByIdAsync(Arg.Any<Guid>()).Returns(hal);

        // Act
        var result = await _service.GetActorByIdAsync(id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(id);
    }

    [Test]
    public async Task GetActorByIdAsync_ReturnsNull_WhenApiReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _client.GetActorByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HalResourceOfActorDto?)null);
        _client.GetActorByIdAsync(Arg.Any<Guid>()).Returns((HalResourceOfActorDto?)null);

        // Act
        var result = await _service.GetActorByIdAsync(id);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetActorByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ex = new ApiException("Server Error", 500, null, null, null);
        _client.GetActorByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(ex);
        _client.GetActorByIdAsync(Arg.Any<Guid>()).ThrowsAsync(ex);

        // Act
        var act = async () => await _service.GetActorByIdAsync(id);

        // Assert
        await Assert.That(act).ThrowsExactly<ApiException>();
    }

    #endregion
}
