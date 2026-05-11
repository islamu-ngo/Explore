// ABOUTME: Unit tests for ActorService covering HAL-based actor reads and conversion behavior.
// Verifies pagination-based list retrieval, HAL mapping, null handling, and exception propagation.

using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for ActorService.
/// </summary>
public class ActorServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ActorService _service;

    public ActorServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _service = new ActorService(_apiClient);
    }

    // ========== GetActorsAsync ==========

    #region GetActorsAsync Tests

    [Test]
    public async Task GetActorsAsync_ReturnsActors_WhenApiSucceeds()
    {
        // Arrange
        var actors = new List<ActorListDto> { new(), new() };
        var halResponse = CreateActorCollectionResponse(actors);

        _apiClient.GetActorsAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetActorsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetActorsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.GetActorsAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.GetActorsAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act & Assert
        await Assert.That(() => _service.GetActorsAsync()).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== GetActorByIdAsync ==========

    #region GetActorByIdAsync Tests

    [Test]
    public async Task GetActorByIdAsync_ReturnsActor_WhenApiSucceeds()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var halResponse = new HalResourceOfActorDto { Id = actorId };

        _apiClient.GetActorByIdAsync(actorId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(halResponse);
        _apiClient.GetActorByIdAsync(actorId).Returns(halResponse);

        // Act
        var result = await _service.GetActorByIdAsync(actorId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(actorId);
    }

    [Test]
    public async Task GetActorByIdAsync_ReturnsNull_WhenApiReturnsNull()
    {
        // Arrange
        var actorId = Guid.NewGuid();

        _apiClient.GetActorByIdAsync(actorId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns((HalResourceOfActorDto?)null);
        _apiClient.GetActorByIdAsync(actorId).Returns((HalResourceOfActorDto?)null);

        // Act
        var result = await _service.GetActorByIdAsync(actorId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetActorByIdAsync_Throws_WhenApiThrows()
    {
        // Arrange
        var actorId = Guid.NewGuid();

        _apiClient.GetActorByIdAsync(actorId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));
        _apiClient.GetActorByIdAsync(actorId)
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act & Assert
        await Assert.That(() => _service.GetActorByIdAsync(actorId)).ThrowsExactly<ApiException>();
    }

    #endregion

    // ========== HAL Response Helpers ==========

    #region HAL Response Helpers

    private static HalCollectionResourceOfActorListDto CreateActorCollectionResponse(IList<ActorListDto> items)
    {
        return new HalCollectionResourceOfActorListDto
        {
            _embedded = new HalCollectionEmbeddedOfActorListDto
            {
                Items = items.Select(ToHalResource).ToList()
            }
        };
    }

    private static HalResourceOfActorListDto ToHalResource(ActorListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfActorListDto>(json)
               ?? new HalResourceOfActorListDto();
    }

    #endregion
}
