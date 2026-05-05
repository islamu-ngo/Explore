// ABOUTME: Unit tests for LandingPageService covering featured events retrieval, member count,
// and upcoming events count with proper error handling verification.

using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Tests LandingPageService across three areas:
/// 1. GetFeaturedEventsAsync - sorting, filtering, count limiting, error handling
/// 2. GetTotalMembersCountAsync - TotalCount extraction, error handling
/// 3. GetUpcomingEventsCountAsync - TotalCount extraction, error handling
/// </summary>
public class LandingPageServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly LandingPageService _service;

    public LandingPageServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        var logger = Substitute.For<ILogger<LandingPageService>>();
        _service = new LandingPageService(_apiClient, logger);
    }

    // ========== GetFeaturedEventsAsync ==========

    #region GetFeaturedEventsAsync Tests

    [Test]
    public async Task GetFeaturedEventsAsync_ReturnsSortedByTotalViewsDescending()
    {
        // Arrange
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Low Views", Description = "Desc", TotalViews = 100 },
            new() { Id = Guid.NewGuid(), Title = "High Views", Description = "Desc", TotalViews = 500 },
            new() { Id = Guid.NewGuid(), Title = "Medium Views", Description = "Desc", TotalViews = 200 }
        };
        var halResponse = CreateEventHalResponse(events, totalCount: 3);

        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

        // Act
        var result = await _service.GetFeaturedEventsAsync(3);

        // Assert
        var resultList = result.ToList();
        await Assert.That(resultList.Count).IsEqualTo(3);
        await Assert.That(resultList[0].TotalViews).IsEqualTo(500);
        await Assert.That(resultList[1].TotalViews).IsEqualTo(200);
        await Assert.That(resultList[2].TotalViews).IsEqualTo(100);
    }

    [Test]
    public async Task GetFeaturedEventsAsync_FiltersOutEventsWithEmptyTitleOrDescription()
    {
        // Arrange
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Valid", Description = "Valid Desc", TotalViews = 100 },
            new() { Id = Guid.NewGuid(), Title = "", Description = "Has Desc", TotalViews = 200 },
            new() { Id = Guid.NewGuid(), Title = "Has Title", Description = "", TotalViews = 300 }
        };
        var halResponse = CreateEventHalResponse(events, totalCount: 3);

        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

        // Act
        var result = await _service.GetFeaturedEventsAsync(10);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.First().Title).IsEqualTo("Valid");
    }

    [Test]
    public async Task GetFeaturedEventsAsync_LimitsToRequestedCount()
    {
        // Arrange
        var events = Enumerable.Range(0, 20).Select(i => new EventListDto
        {
            Id = Guid.NewGuid(),
            Title = $"Event {i}",
            Description = $"Description {i}",
            TotalViews = i * 10
        }).ToList();
        var halResponse = CreateEventHalResponse(events, totalCount: 20);

        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

        // Act
        var result = await _service.GetFeaturedEventsAsync(6);

        // Assert
        await Assert.That(result.Count).IsEqualTo(6);
    }

    [Test]
    public async Task GetFeaturedEventsAsync_ReturnsEmptyCollection_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetEventsAsync().ThrowsAsyncForAnyArgs(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetFeaturedEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    #endregion

    // ========== GetTotalMembersCountAsync ==========

    #region GetTotalMembersCountAsync Tests

    [Test]
    public async Task GetTotalMembersCountAsync_ReturnsTotalCountFromActorApi()
    {
        // Arrange
        var halResponse = new HalCollectionResourceOfActorListDto { TotalCount = 1247 };

        _apiClient.GetActorsAsync().ReturnsForAnyArgs(halResponse);

        // Act
        var result = await _service.GetTotalMembersCountAsync();

        // Assert
        await Assert.That(result).IsEqualTo(1247);
    }

    [Test]
    public async Task GetTotalMembersCountAsync_ReturnsZero_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetActorsAsync().ThrowsAsyncForAnyArgs(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetTotalMembersCountAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    #endregion

    // ========== GetUpcomingEventsCountAsync ==========

    #region GetUpcomingEventsCountAsync Tests

    [Test]
    public async Task GetUpcomingEventsCountAsync_ReturnsTotalCountFromApi()
    {
        // Arrange
        var halResponse = CreateEventHalResponse(new List<EventListDto>(), totalCount: 42);

        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

        // Act
        var result = await _service.GetUpcomingEventsCountAsync();

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task GetUpcomingEventsCountAsync_ReturnsZero_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetEventsAsync().ThrowsAsyncForAnyArgs(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetUpcomingEventsCountAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    #endregion

    // ========== Helper Methods ==========

    #region HAL Response Helpers

    private static HalCollectionResourceOfEventListDto CreateEventHalResponse(
        IList<EventListDto> items, int totalCount)
    {
        return new HalCollectionResourceOfEventListDto
        {
            _embedded = new HalCollectionEmbeddedOfEventListDto
            {
                Items = items.Cast<object>().ToList()
            },
            TotalCount = totalCount
        };
    }

    #endregion
}
