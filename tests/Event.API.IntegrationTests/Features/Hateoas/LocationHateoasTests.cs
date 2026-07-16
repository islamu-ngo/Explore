using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for LocationController.
/// Validates location-specific links.
/// </summary>
[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
public class LocationHateoasTests
{
    private readonly ContractApiFixture _fixture;
    private const string BaseUrl = "/api/location";

    public LocationHateoasTests(ContractApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        // Act
        var response = await GetAuthenticatedAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Verify HAL structure
        await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("_embedded", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("pageNumber", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("totalCount", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeSelfLink()
    {
        // Act
        var response = await GetAuthenticatedAsync(BaseUrl);

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
                await Assert.That(href).Contains("/api/location/");
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeCollectionLink()
    {
        // Act
        var response = await GetAuthenticatedAsync(BaseUrl);

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
                await Assert.That(itemLinks.TryGetProperty("collection", out var collectionLink)).IsTrue();
                var href = collectionLink.GetProperty("href").GetString();
                await Assert.That(href).IsEqualTo("/api/location");
            }
        }
    }

    [Test]
    public async Task GetById_ShouldIncludeDetailLinks()
    {
        // Arrange - First get list to find a location
        var listResponse = await GetAuthenticatedAsync(BaseUrl);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        if (!listJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            return;
        }

        var firstItem = items[0];
        Guid locationId;

        if (firstItem.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
        {
            locationId = idProp.GetGuid();
        }
        else if (firstItem.TryGetProperty("id", out idProp))
        {
            locationId = idProp.GetGuid();
        }
        else
        {
            return;
        }

        // Act
        var response = await GetAuthenticatedAsync($"{BaseUrl}/{locationId}");

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
    public async Task GetAll_WithPreferMinimal_ShouldExcludeItemLinks()
    {
        // Arrange
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, BaseUrl);
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

    [Test]
    public async Task GetAll_PaginationLinks_ShouldBeCorrect()
    {
        // Act
        var response = await GetAuthenticatedAsync($"{BaseUrl}?pageNumber=1&pageSize=5");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_links", out var links))
        {
            // Self link should have correct pagination parameters
            if (links.TryGetProperty("self", out var selfLink))
            {
                var href = selfLink.GetProperty("href").GetString()!;
                await Assert.That(href).Contains("pageNumber=1");
                await Assert.That(href).Contains("pageSize=5");
            }

            // First link should have pageNumber=1
            if (links.TryGetProperty("first", out var firstLink))
            {
                var href = firstLink.GetProperty("href").GetString()!;
                await Assert.That(href).Contains("pageNumber=1");
            }
        }
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string url)
    {
        using var request = _fixture.CreateAuthenticatedRequest(HttpMethod.Get, url);
        return await _fixture.Client.SendAsync(request);
    }
}
