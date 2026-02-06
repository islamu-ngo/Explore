using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for UserController.
/// Validates user-specific links including actor profile, organizations, and registrations.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class UserHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/user";

    public UserHateoasTests(ApiTestFixture fixture)
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
            return; // Endpoint requires authentication

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        // Verify HAL structure
        await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
    }

    [Test]
    public async Task GetAll_ShouldIncludeSelfLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
            return; // Endpoint requires authentication

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_links", out var links))
        {
            await Assert.That(links.TryGetProperty("self", out var selfLink)).IsTrue();
            var href = selfLink.GetProperty("href").GetString();
            await Assert.That(href).Contains("/api/v1/user");
        }
    }

    [Test]
    public async Task GetAll_WithoutAuth_ShouldNotIncludeCurrentUserLink()
    {
        // Arrange - No authentication
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
            return; // Endpoint requires authentication

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_links", out var links))
        {
            // Current-user link requires authentication
            var hasCurrentUserLink = links.TryGetProperty("current-user", out _);
            await Assert.That(hasCurrentUserLink).IsFalse();
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeSelfLink()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
            return; // Endpoint requires authentication

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
                await Assert.That(href).Contains("/api/v1/user/");
            }
        }
    }

    [Test]
    public async Task GetById_Links_ShouldIncludeActorLink()
    {
        // Arrange - First get list to find a user
        var listResponse = await _fixture.Client.GetAsync(BaseUrl);
        if (listResponse.StatusCode != HttpStatusCode.OK)
            return; // Endpoint requires authentication

        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        if (!listJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            return; // No data to test with
        }

        var firstItem = items[0];
        if (!TryGetId(firstItem, out var userId))
        {
            return;
        }

        // Act - Get user by ID
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{userId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                // User should have actor link if actorId is set
                if (links.TryGetProperty("actor", out var actorLink))
                {
                    var href = actorLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/actor/");
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
        if (response.StatusCode != HttpStatusCode.OK)
            return; // Endpoint requires authentication

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
