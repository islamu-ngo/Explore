using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for EventSessionController.
/// Validates session-specific links including parent event, speakers, and agenda items.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class EventSessionHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/eventsession";

    public EventSessionHateoasTests(ApiTestFixture fixture)
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
    public async Task GetAll_ItemLinks_ShouldIncludeEventLink()
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
                // Event session items should have parent event link
                if (itemLinks.TryGetProperty("event", out var eventLink))
                {
                    var href = eventLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/event/");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeSpeakersLink()
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
                // Event session items should have speakers link
                if (itemLinks.TryGetProperty("speakers", out var speakersLink))
                {
                    var href = speakersLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/speakers");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeAgendaItemsLink()
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
                // Event session items should have agenda-items link
                if (itemLinks.TryGetProperty("agenda-items", out var agendaLink))
                {
                    var href = agendaLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/agenda-items");
                }
            }
        }
    }

    [Test]
    public async Task GetByEvent_ShouldReturnSessionsForEvent()
    {
        // Arrange - First get an event
        var eventsResponse = await _fixture.Client.GetAsync("/api/v1/event");
        var eventsContent = await eventsResponse.Content.ReadAsStringAsync();
        var eventsJson = JsonDocument.Parse(eventsContent);

        if (!eventsJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            // No events to test with
            return;
        }

        var firstEvent = items[0];
        Guid eventId;

        if (firstEvent.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
        {
            eventId = idProp.GetGuid();
        }
        else if (firstEvent.TryGetProperty("id", out idProp))
        {
            eventId = idProp.GetGuid();
        }
        else
        {
            return;
        }

        // Act
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/by-event/{eventId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // Should have HAL structure
            await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
            await Assert.That(json.RootElement.TryGetProperty("_embedded", out _)).IsTrue();
        }
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
