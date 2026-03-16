using Explore.Application.Specifications.Events;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Specifications;

public class EventSubqueryFilterTests
{
    [Test]
    public async Task TagsIncludedAll_ShouldCreateCorrectFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var filter = EventSubqueryFilter.TagsIncludedAll(tagIds);

        // Assert
        await Assert.That(filter.FilterType).IsEqualTo(EventSubqueryFilterType.TagsIncludedAll);
        await Assert.That(filter.Value).IsEqualTo(tagIds);
    }

    [Test]
    public async Task TagsIncludedAny_ShouldCreateCorrectFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var filter = EventSubqueryFilter.TagsIncludedAny(tagIds);

        // Assert
        await Assert.That(filter.FilterType).IsEqualTo(EventSubqueryFilterType.TagsIncludedAny);
        await Assert.That(filter.Value).IsEqualTo(tagIds);
    }

    [Test]
    public async Task TagsExcludedAny_ShouldCreateCorrectFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var filter = EventSubqueryFilter.TagsExcludedAny(tagIds);

        // Assert
        await Assert.That(filter.FilterType).IsEqualTo(EventSubqueryFilterType.TagsExcludedAny);
        await Assert.That(filter.Value).IsEqualTo(tagIds);
    }

    [Test]
    public async Task TagsExcludedAll_ShouldCreateCorrectFilter()
    {
        // Arrange
        var tagIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var filter = EventSubqueryFilter.TagsExcludedAll(tagIds);

        // Assert
        await Assert.That(filter.FilterType).IsEqualTo(EventSubqueryFilterType.TagsExcludedAll);
        await Assert.That(filter.Value).IsEqualTo(tagIds);
    }
}
