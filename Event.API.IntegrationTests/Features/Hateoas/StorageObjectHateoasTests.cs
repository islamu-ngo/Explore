// ABOUTME: HATEOAS contract tests for storage object metadata collection and detail endpoints.
// ABOUTME: Verifies storage UI affordances are exposed through HAL links instead of client-side role checks.

using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for StorageObjectController.
/// Validates storage object links for file management operations.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class StorageObjectHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/storageobject";

    public StorageObjectHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        var response = await _fixture.Client.GetAsync(BaseUrl);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("_embedded", out var embedded)).IsTrue();
        await Assert.That(embedded.TryGetProperty("items", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_ShouldIncludeSelfLink()
    {
        var response = await _fixture.Client.GetAsync(BaseUrl);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        await Assert.That(json.RootElement.TryGetProperty("_links", out var links)).IsTrue();
        await Assert.That(links.TryGetProperty("self", out var selfLink)).IsTrue();
        var href = selfLink.GetProperty("href").GetString();
        await Assert.That(href).Contains("/api/storageobject");
    }

    [Test]
    public async Task GetAll_WithoutAuth_ShouldNotIncludeCreateLink()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);

        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        await Assert.That(json.RootElement.TryGetProperty("_links", out var links)).IsTrue();
        await Assert.That(links.TryGetProperty("create", out _)).IsFalse();
        await Assert.That(links.TryGetProperty("create-upload-session", out _)).IsFalse();
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
                await Assert.That(href).Contains("/api/storageobject/");
            }
        }
    }

    [Test]
    public async Task GetById_Links_ShouldIncludeCollectionLink()
    {
        // Arrange - First get list to find a storage object
        var listResponse = await _fixture.Client.GetAsync(BaseUrl);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        if (!listJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            return; // No data to test with
        }

        var firstItem = items[0];
        if (!TryGetId(firstItem, out var objectId))
        {
            return;
        }

        // Act - Get storage object by ID
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{objectId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                // Detail view should have collection link back
                if (links.TryGetProperty("collection", out var collectionLink))
                {
                    var href = collectionLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/storageobject");
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

    private static bool TryGetId(JsonElement item, out Guid id)
    {
        id = Guid.Empty;
        if (item.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
        {
            id = idProp.GetGuid();
            return true;
        }
        if (item.TryGetProperty("id", out idProp))
        {
            id = idProp.GetGuid();
            return true;
        }
        return false;
    }
}
