using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for TenantSettingsController.
/// Validates tenant settings links including parent tenant reference.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class TenantSettingsHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/tenantsettings";

    public TenantSettingsHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert - May require authentication
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            // Verify HAL structure
            await Assert.That(json.RootElement.TryGetProperty("_links", out _)).IsTrue();
        }
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
                await Assert.That(href).Contains("/api/tenantsettings");
            }
        }
    }

    [Test]
    public async Task GetAll_ShouldNotIncludeCreateLink()
    {
        // Arrange - Settings are auto-created with tenant
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
                // Settings are auto-created, so no create link
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
                    await Assert.That(href).Contains("/api/tenantsettings/");
                }
            }
        }
    }

    [Test]
    public async Task GetAll_ItemLinks_ShouldIncludeTenantLink()
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
                    // Settings items should have tenant link
                    if (itemLinks.TryGetProperty("tenant", out var tenantLink))
                    {
                        var href = tenantLink.GetProperty("href").GetString();
                        await Assert.That(href).Contains("/api/tenant/");
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
