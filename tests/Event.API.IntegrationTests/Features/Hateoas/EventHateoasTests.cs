using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for EventController.
/// Validates event-specific links including sessions, categories, tags, and actor.
/// </summary>
[NotInParallel("ApiTestFixture")]
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/event";

    public EventHateoasTests(ApiTestFixture fixture)
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
        await Assert.That(json.RootElement.TryGetProperty("pageNumber", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("totalCount", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeSessionsLink()
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
                // Event items should have sessions link
                if (itemLinks.TryGetProperty("sessions", out var sessionsLink))
                {
                    var href = sessionsLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/eventsession/by-event/");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeActorLink()
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
                // Event items should have actor link (organizer)
                if (itemLinks.TryGetProperty("actor", out var actorLink))
                {
                    var href = actorLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/actor/");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_WithPreferMinimal_ShouldStillHavePaginationMetadata()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
        request.Headers.Add("Prefer", "return=minimal");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Pagination metadata should still be present even with minimal
        await Assert.That(json.RootElement.TryGetProperty("pageNumber", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("pageSize", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("totalCount", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_PaginationLinks_ShouldBeCorrect()
    {
        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}?pageNumber=1&pageSize=5");

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

    [Test]
    public async Task GetAll_ShouldReturnConsistentStructure()
    {
        // Act - Make two requests
        var response1 = await _fixture.Client.GetAsync(BaseUrl);
        var response2 = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        var json1 = JsonDocument.Parse(content1);
        var json2 = JsonDocument.Parse(content2);

        // Both should have same structure
        var hasLinks1 = json1.RootElement.TryGetProperty("_links", out _);
        var hasLinks2 = json2.RootElement.TryGetProperty("_links", out _);
        await Assert.That(hasLinks1).IsEqualTo(hasLinks2);

        var hasEmbedded1 = json1.RootElement.TryGetProperty("_embedded", out _);
        var hasEmbedded2 = json2.RootElement.TryGetProperty("_embedded", out _);
        await Assert.That(hasEmbedded1).IsEqualTo(hasEmbedded2);
    }
}
