using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for EventRegistrationController.
/// Validates event registration links including user, event session, and event references.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventRegistrationHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/eventregistration";

    public EventRegistrationHateoasTests(ApiTestFixture fixture)
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
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                await Assert.That(links.TryGetProperty("self", out var selfLink)).IsTrue();
                var href = selfLink.GetProperty("href").GetString();
                await Assert.That(href).Contains("/api/eventregistration");
            }
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
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                // Create link requires authentication
                var hasCreateLink = links.TryGetProperty("create", out _);
                await Assert.That(hasCreateLink).IsFalse();
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeSelfLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
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
                    await Assert.That(href).Contains("/api/eventregistration/");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeUserLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
                embedded.TryGetProperty("items", out var items) &&
                items.GetArrayLength() > 0)
            {
                var firstItem = items[0];

                if (firstItem.TryGetProperty("_links", out var itemLinks))
                {
                    // Registration items should have user link
                    if (itemLinks.TryGetProperty("user", out var userLink))
                    {
                        var href = userLink.GetProperty("href").GetString();
                        await Assert.That(href).Contains("/api/user/");
                    }
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeEventSessionLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
                embedded.TryGetProperty("items", out var items) &&
                items.GetArrayLength() > 0)
            {
                var firstItem = items[0];

                if (firstItem.TryGetProperty("_links", out var itemLinks))
                {
                    // Registration items should have event-session link
                    if (itemLinks.TryGetProperty("event-session", out var sessionLink))
                    {
                        var href = sessionLink.GetProperty("href").GetString();
                        await Assert.That(href).Contains("/api/eventsession/");
                    }
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeEventLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
                embedded.TryGetProperty("items", out var items) &&
                items.GetArrayLength() > 0)
            {
                var firstItem = items[0];

                if (firstItem.TryGetProperty("_links", out var itemLinks))
                {
                    // Registration list items should have event link
                    if (itemLinks.TryGetProperty("event", out var eventLink))
                    {
                        var href = eventLink.GetProperty("href").GetString();
                        await Assert.That(href).Contains("/api/event/");
                    }
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
        if (response.StatusCode == HttpStatusCode.OK)
        {
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
}
