using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Hateoas;

/// <summary>
/// Unit tests for HalCollectionResource model.
/// </summary>
public class HalCollectionResourceTests
{
    private record TestListDto(Guid Id, string Name);

    [Test]
    public async Task FromPagination_ShouldCreateCollectionWithMetadata()
    {
        // Arrange
        var items = new List<HalResource<TestListDto>>
        {
            new(new TestListDto(Guid.NewGuid(), "Item 1")),
            new(new TestListDto(Guid.NewGuid(), "Item 2")),
            new(new TestListDto(Guid.NewGuid(), "Item 3"))
        };
        var links = new Dictionary<string, HalLink>
        {
            [LinkRelations.Self] = HalLink.Create("/api/test?pageNumber=1&pageSize=10")
        };

        // Act
        var collection = HalCollectionResource<TestListDto>.FromPagination(
            items,
            pageNumber: 1,
            pageSize: 10,
            totalCount: 25,
            totalPages: 3,
            links);

        // Assert
        await Assert.That(collection.PageNumber).IsEqualTo(1);
        await Assert.That(collection.PageSize).IsEqualTo(10);
        await Assert.That(collection.TotalCount).IsEqualTo(25);
        await Assert.That(collection.TotalPages).IsEqualTo(3);
        await Assert.That(collection.Embedded).IsNotNull();
        await Assert.That(collection.Embedded!.Items.Count).IsEqualTo(3);
        await Assert.That(collection.Links.Count).IsEqualTo(1);
    }

    [Test]
    public async Task EmptyCollection_ShouldHaveCorrectMetadata()
    {
        // Arrange
        var items = new List<HalResource<TestListDto>>();
        var links = new Dictionary<string, HalLink>
        {
            [LinkRelations.Self] = HalLink.Create("/api/test")
        };

        // Act
        var collection = HalCollectionResource<TestListDto>.FromPagination(
            items,
            pageNumber: 1,
            pageSize: 10,
            totalCount: 0,
            totalPages: 0,
            links);

        // Assert
        await Assert.That(collection.TotalCount).IsEqualTo(0);
        await Assert.That(collection.TotalPages).IsEqualTo(0);
        await Assert.That(collection.Embedded!.Items).IsEmpty();
    }

    [Test]
    public async Task Collection_ShouldSupportMultipleLinks()
    {
        // Arrange
        var items = new List<HalResource<TestListDto>>
        {
            new(new TestListDto(Guid.NewGuid(), "Item 1"))
        };
        var links = new Dictionary<string, HalLink>
        {
            [LinkRelations.Self] = HalLink.Create("/api/test?pageNumber=2&pageSize=10"),
            [LinkRelations.First] = HalLink.Create("/api/test?pageNumber=1&pageSize=10"),
            [LinkRelations.Prev] = HalLink.Create("/api/test?pageNumber=1&pageSize=10"),
            [LinkRelations.Next] = HalLink.Create("/api/test?pageNumber=3&pageSize=10"),
            [LinkRelations.Last] = HalLink.Create("/api/test?pageNumber=5&pageSize=10")
        };

        // Act
        var collection = HalCollectionResource<TestListDto>.FromPagination(
            items,
            pageNumber: 2,
            pageSize: 10,
            totalCount: 50,
            totalPages: 5,
            links);

        // Assert
        await Assert.That(collection.Links.Count).IsEqualTo(5);
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Self)).IsTrue();
        await Assert.That(collection.Links.ContainsKey(LinkRelations.First)).IsTrue();
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Prev)).IsTrue();
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Next)).IsTrue();
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Last)).IsTrue();
    }

    [Test]
    public async Task FirstPage_ShouldNotHavePrevLink()
    {
        // Arrange
        var items = new List<HalResource<TestListDto>>
        {
            new(new TestListDto(Guid.NewGuid(), "Item 1"))
        };
        var links = new Dictionary<string, HalLink>
        {
            [LinkRelations.Self] = HalLink.Create("/api/test?pageNumber=1&pageSize=10"),
            [LinkRelations.First] = HalLink.Create("/api/test?pageNumber=1&pageSize=10"),
            [LinkRelations.Next] = HalLink.Create("/api/test?pageNumber=2&pageSize=10"),
            [LinkRelations.Last] = HalLink.Create("/api/test?pageNumber=3&pageSize=10")
            // No "prev" link for first page
        };

        // Act
        var collection = HalCollectionResource<TestListDto>.FromPagination(
            items,
            pageNumber: 1,
            pageSize: 10,
            totalCount: 30,
            totalPages: 3,
            links);

        // Assert
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Prev)).IsFalse();
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Next)).IsTrue();
    }

    [Test]
    public async Task LastPage_ShouldNotHaveNextLink()
    {
        // Arrange
        var items = new List<HalResource<TestListDto>>
        {
            new(new TestListDto(Guid.NewGuid(), "Item 1"))
        };
        var links = new Dictionary<string, HalLink>
        {
            [LinkRelations.Self] = HalLink.Create("/api/test?pageNumber=3&pageSize=10"),
            [LinkRelations.First] = HalLink.Create("/api/test?pageNumber=1&pageSize=10"),
            [LinkRelations.Prev] = HalLink.Create("/api/test?pageNumber=2&pageSize=10"),
            [LinkRelations.Last] = HalLink.Create("/api/test?pageNumber=3&pageSize=10")
            // No "next" link for last page
        };

        // Act
        var collection = HalCollectionResource<TestListDto>.FromPagination(
            items,
            pageNumber: 3,
            pageSize: 10,
            totalCount: 30,
            totalPages: 3,
            links);

        // Assert
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Next)).IsFalse();
        await Assert.That(collection.Links.ContainsKey(LinkRelations.Prev)).IsTrue();
    }

    [Test]
    public async Task DefaultConstructor_ShouldCreateValidInstance()
    {
        // Act
        var collection = new HalCollectionResource<TestListDto>();

        // Assert
        await Assert.That(collection.PageNumber).IsEqualTo(0);
        await Assert.That(collection.PageSize).IsEqualTo(0);
        await Assert.That(collection.TotalCount).IsEqualTo(0);
        await Assert.That(collection.TotalPages).IsEqualTo(0);
    }
}
