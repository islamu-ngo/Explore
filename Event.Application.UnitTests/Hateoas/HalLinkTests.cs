using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Hateoas;

/// <summary>
/// Unit tests for HalLink model.
/// </summary>
public class HalLinkTests
{
    [Test]
    public async Task Create_ShouldCreateLinkWithHrefOnly()
    {
        // Arrange
        var href = "/api/v1/resource/123";

        // Act
        var link = HalLink.Create(href);

        // Assert
        await Assert.That(link.Href).IsEqualTo(href);
        await Assert.That(link.Method).IsNull();
        await Assert.That(link.Templated).IsNull();
        await Assert.That(link.Title).IsNull();
    }

    [Test]
    public async Task CreateAction_ShouldCreateLinkWithMethod()
    {
        // Arrange
        var href = "/api/v1/resource/123";
        var method = "DELETE";

        // Act
        var link = HalLink.CreateAction(href, method);

        // Assert
        await Assert.That(link.Href).IsEqualTo(href);
        await Assert.That(link.Method).IsEqualTo(method);
    }

    [Test]
    public async Task CreateTemplated_ShouldCreateTemplatedLink()
    {
        // Arrange
        var hrefTemplate = "/api/v1/resource{?page,size}";
        var title = "Search resources";

        // Act
        var link = HalLink.CreateTemplated(hrefTemplate, title);

        // Assert
        await Assert.That(link.Href).IsEqualTo(hrefTemplate);
        await Assert.That(link.Templated).IsEqualTo(true);
        await Assert.That(link.Title).IsEqualTo(title);
    }

    [Test]
    public async Task CreateTemplated_WithoutTitle_ShouldHaveNullTitle()
    {
        // Arrange
        var hrefTemplate = "/api/v1/resource{?page}";

        // Act
        var link = HalLink.CreateTemplated(hrefTemplate);

        // Assert
        await Assert.That(link.Templated).IsEqualTo(true);
        await Assert.That(link.Title).IsNull();
    }

    [Test]
    public async Task HalLink_ShouldSupportAllOptionalProperties()
    {
        // Arrange & Act
        var link = new HalLink
        {
            Href = "/api/v1/resource",
            Method = "POST",
            Templated = false,
            Title = "Create Resource",
            Type = "application/json",
            Hreflang = "en-US",
            Name = "create-resource"
        };

        // Assert
        await Assert.That(link.Href).IsEqualTo("/api/v1/resource");
        await Assert.That(link.Method).IsEqualTo("POST");
        await Assert.That(link.Templated).IsEqualTo(false);
        await Assert.That(link.Title).IsEqualTo("Create Resource");
        await Assert.That(link.Type).IsEqualTo("application/json");
        await Assert.That(link.Hreflang).IsEqualTo("en-US");
        await Assert.That(link.Name).IsEqualTo("create-resource");
    }
}
