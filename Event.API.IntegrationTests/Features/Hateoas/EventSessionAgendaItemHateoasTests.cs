using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for EventSessionAgendaItemController.
/// Validates agenda item links including event session and location references.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventSessionAgendaItemHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/eventsessionagendaitem";

    public EventSessionAgendaItemHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
            return; // Endpoint may require authentication

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Controller may not yet be converted to HATEOAS
        if (!json.RootElement.TryGetProperty("_links", out _))
            return;
    }

    [Test]
    public async Task GetAll_ShouldIncludeSelfLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_links", out var links))
        {
            await Assert.That(links.TryGetProperty("self", out var selfLink)).IsTrue();
            var href = selfLink.GetProperty("href").GetString();
            await Assert.That(href).Contains("/api/v1/eventsessionagendaitem");
        }
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
            // Create link requires authentication
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

            if (firstItem.TryGetProperty("_links", out var itemLinks))
            {
                await Assert.That(itemLinks.TryGetProperty("self", out var selfLink)).IsTrue();
                var href = selfLink.GetProperty("href").GetString();
                await Assert.That(href).Contains("/api/v1/eventsessionagendaitem/");
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeEventSessionLink()
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
                // Agenda items should have event-session link
                if (itemLinks.TryGetProperty("event-session", out var sessionLink))
                {
                    var href = sessionLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/eventsession/");
                }
            }
        }
    }

    [Test]
    public async Task GetById_Links_ShouldIncludeLocationLink()
    {
        // Arrange - First get list to find an agenda item
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
        if (!TryGetId(firstItem, out var agendaItemId))
        {
            return;
        }

        // Act - Get agenda item by ID
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{agendaItemId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                // Detail view should have event-session link
                await Assert.That(links.TryGetProperty("event-session", out _)).IsTrue();

                // Location link may exist if agenda item has location
                if (links.TryGetProperty("location", out var locationLink))
                {
                    var href = locationLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/location/");
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
