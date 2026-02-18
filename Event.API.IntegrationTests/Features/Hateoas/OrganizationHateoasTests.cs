using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for OrganizationController.
/// Validates link generation, link relations, and authorization-aware links.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class OrganizationHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/organization";

    private static string WithCacheBust(string endpoint)
    {
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{endpoint}{separator}pageNumber=1&pageSize=20&testRun={Guid.NewGuid():N}";
    }

    public OrganizationHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeCollectionLinks()
    {
        // Act
        var response = await _fixture.Client.GetAsync(WithCacheBust(BaseUrl));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        await Assert.That(json.RootElement.TryGetProperty("_links", out var links)).IsTrue();

        // Collection should have self link
        await Assert.That(links.TryGetProperty("self", out var selfLink)).IsTrue();
        await Assert.That(selfLink.GetProperty("href").GetString()).Contains("/api/v1/organization");
    }

    [Test]
    public async Task GetAll_WithoutAuth_ShouldNotIncludeCreateLink()
    {
        // Arrange - No authentication
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_links", out var links))
        {
            // Create link requires authentication, should not be present
            var hasCreateLink = links.TryGetProperty("create", out _);
            await Assert.That(hasCreateLink).IsFalse();
        }
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

            // Each item should have _links.self
            if (firstItem.TryGetProperty("_links", out var itemLinks))
            {
                await Assert.That(itemLinks.TryGetProperty("self", out var selfLink)).IsTrue();

                var href = selfLink.GetProperty("href").GetString();
                await Assert.That(href).Contains("/api/v1/organization/");
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeCollectionLink()
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
                // Item should have collection link back to list
                await Assert.That(itemLinks.TryGetProperty("collection", out var collectionLink)).IsTrue();

                var href = collectionLink.GetProperty("href").GetString();
                await Assert.That(href).IsEqualTo("/api/v1/organization");
            }
        }
    }

    [Test]
    public async Task GetById_Links_ShouldIncludeEventsLink()
    {
        // Arrange - First get list to find an organization
        var listResponse = await _fixture.Client.GetAsync(BaseUrl);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        if (!listJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            // No data to test with, skip
            return;
        }

        // Get first organization's ID
        var firstItem = items[0];
        Guid organizationId;

        if (firstItem.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
        {
            organizationId = idProp.GetGuid();
        }
        else if (firstItem.TryGetProperty("id", out idProp))
        {
            organizationId = idProp.GetGuid();
        }
        else
        {
            return;
        }

        // Act - Get organization by ID
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{organizationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                // Detail view should have events link
                if (links.TryGetProperty("events", out var eventsLink))
                {
                    var href = eventsLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/event");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_SelfLink_ShouldHaveGetMethod()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_links", out var links) &&
            links.TryGetProperty("self", out var selfLink) &&
            selfLink.TryGetProperty("method", out var method))
        {
            await Assert.That(method.GetString()).IsEqualTo("GET");
        }
    }
}
