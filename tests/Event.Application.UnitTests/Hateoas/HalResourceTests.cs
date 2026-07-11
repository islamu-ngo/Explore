using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Hateoas;

/// <summary>
/// Unit tests for HalResource model.
/// </summary>
public class HalResourceTests
{
    private record TestDto(Guid Id, string Name, string Description);

    [Test]
    public async Task Constructor_WithData_ShouldSetData()
    {
        // Arrange
        var dto = new TestDto(Guid.NewGuid(), "Test", "Description");

        // Act
        var resource = new HalResource<TestDto>(dto);

        // Assert
        await Assert.That(resource.Data).IsEqualTo(dto);
        await Assert.That(resource.Links).IsEmpty();
        await Assert.That(resource.Embedded).IsNull();
    }

    [Test]
    public async Task Constructor_WithDataAndLinks_ShouldSetBoth()
    {
        // Arrange
        var dto = new TestDto(Guid.NewGuid(), "Test", "Description");
        var links = new Dictionary<string, HalLink>
        {
            [LinkRelations.Self] = HalLink.Create("/api/test/123")
        };

        // Act
        var resource = new HalResource<TestDto>(dto, links);

        // Assert
        await Assert.That(resource.Data).IsEqualTo(dto);
        await Assert.That(resource.Links.Count).IsEqualTo(1);
        await Assert.That(resource.Links.ContainsKey(LinkRelations.Self)).IsTrue();
    }

    [Test]
    public async Task WithLink_ShouldAddLink()
    {
        // Arrange
        var dto = new TestDto(Guid.NewGuid(), "Test", "Description");
        var resource = new HalResource<TestDto>(dto);

        // Act
        resource.WithLink("custom", HalLink.Create("/api/custom"));

        // Assert
        await Assert.That(resource.Links.Count).IsEqualTo(1);
        await Assert.That(resource.Links.ContainsKey("custom")).IsTrue();
    }

    [Test]
    public async Task WithSelfLink_ShouldAddSelfLink()
    {
        // Arrange
        var dto = new TestDto(Guid.NewGuid(), "Test", "Description");
        var resource = new HalResource<TestDto>(dto);

        // Act
        resource.WithSelfLink("/api/test/123");

        // Assert
        await Assert.That(resource.Links.Count).IsEqualTo(1);
        await Assert.That(resource.Links.ContainsKey(LinkRelations.Self)).IsTrue();
        await Assert.That(resource.Links[LinkRelations.Self].Href).IsEqualTo("/api/test/123");
    }

    [Test]
    public async Task WithEmbedded_ShouldAddEmbeddedResource()
    {
        // Arrange
        var dto = new TestDto(Guid.NewGuid(), "Test", "Description");
        var resource = new HalResource<TestDto>(dto);
        var embeddedItems = new[] { new TestDto(Guid.NewGuid(), "Child", "Child Desc") };

        // Act
        var result = resource.WithEmbedded("children", embeddedItems);

        // Assert
        await Assert.That(result.Embedded).IsNotNull();
        await Assert.That(result.Embedded!.ContainsKey("children")).IsTrue();
    }

    [Test]
    public async Task WithMultipleLinks_ShouldAccumulateLinks()
    {
        // Arrange
        var dto = new TestDto(Guid.NewGuid(), "Test", "Description");
        var resource = new HalResource<TestDto>(dto);

        // Act
        resource
            .WithSelfLink("/api/test/123")
            .WithLink(LinkRelations.Collection, HalLink.Create("/api/test"))
            .WithLink(LinkRelations.Edit, HalLink.CreateAction("/api/test/123", "PUT"));

        // Assert
        await Assert.That(resource.Links.Count).IsEqualTo(3);
        await Assert.That(resource.Links.ContainsKey(LinkRelations.Self)).IsTrue();
        await Assert.That(resource.Links.ContainsKey(LinkRelations.Collection)).IsTrue();
        await Assert.That(resource.Links.ContainsKey(LinkRelations.Edit)).IsTrue();
    }

    [Test]
    public async Task EmptyResource_ShouldHaveEmptyLinksAndNullEmbedded()
    {
        // Arrange & Act
        var resource = new HalResource<TestDto>();

        // Assert
        await Assert.That(resource.Links).IsEmpty();
        await Assert.That(resource.Embedded).IsNull();
    }
}
