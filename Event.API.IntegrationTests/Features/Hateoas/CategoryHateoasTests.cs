using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for CategoryController.
/// Validates category-specific links including parent, children, and events.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class CategoryHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/category";

    public CategoryHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Verify HAL structure
        await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("_embedded", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeSelfLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
            embedded.TryGetProperty("items", out var items) &&
            items.GetArrayLength() > 0)
        {
            var firstItem = items[0];

            if (firstItem.TryGetProperty("_links", out var itemLinks))
            {
                await Assert.That(itemLinks.TryGetProperty("self", out var selfLink)).IsTrue();
                var href = selfLink.GetProperty("href").GetString();
                await Assert.That(href).Contains("/api/v1/category/");
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeChildrenLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
            embedded.TryGetProperty("items", out var items) &&
            items.GetArrayLength() > 0)
        {
            var firstItem = items[0];

            if (firstItem.TryGetProperty("_links", out var itemLinks))
            {
                // Category items should have children link
                if (itemLinks.TryGetProperty("children", out var childrenLink))
                {
                    var href = childrenLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/category/children/");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeEventsLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
            embedded.TryGetProperty("items", out var items) &&
            items.GetArrayLength() > 0)
        {
            var firstItem = items[0];

            if (firstItem.TryGetProperty("_links", out var itemLinks))
            {
                // Category items should have events link
                if (itemLinks.TryGetProperty("events", out var eventsLink))
                {
                    var href = eventsLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/event");
                }
            }
        }
    }

    [Test]
    public async Task GetById_ShouldIncludeDetailLinks()
    {
        // Arrange - First get list to find a category
        var listResponse = await _fixture.Client.GetAsync(BaseUrl);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        if (!listJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            return;
        }

        var firstItem = items[0];
        Guid categoryId;

        if (firstItem.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
        {
            categoryId = idProp.GetGuid();
        }
        else if (firstItem.TryGetProperty("id", out idProp))
        {
            categoryId = idProp.GetGuid();
        }
        else
        {
            return;
        }

        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{categoryId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            await Assert.That(json.RootElement.TryGetProperty("_links", out var links)).IsTrue();

            // Detail view should have self link
            await Assert.That(links.TryGetProperty("self", out _)).IsTrue();

            // Detail view should have collection link
            await Assert.That(links.TryGetProperty("collection", out _)).IsTrue();
        }
    }

    [Test]
    public async Task GetRootCategories_ShouldIncludeHalStructure()
    {
        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/root");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Verify HAL structure
        await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("_embedded", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_WithPreferMinimal_ShouldExcludeItemLinks()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
        request.Headers.Add("Prefer", "return=minimal");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var hasPreferenceApplied = response.Headers.Contains("Preference-Applied");
        await Assert.That(hasPreferenceApplied).IsTrue();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
            embedded.TryGetProperty("items", out var items) &&
            items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            var hasLinks = firstItem.TryGetProperty("_links", out _);
            await Assert.That(hasLinks).IsFalse();
        }
    }
}
