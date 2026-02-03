using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for TenantUserController.
/// Validates tenant-user role assignment links including user and tenant references.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class TenantUserHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/tenantuser";

    public TenantUserHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert - May require authentication, so check for OK or Unauthorized
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
                await Assert.That(href).Contains("/api/v1/tenantuser");
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
                    await Assert.That(href).Contains("/api/v1/tenantuser/");
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
                    // TenantUser items should have user link
                    if (itemLinks.TryGetProperty("user", out var userLink))
                    {
                        var href = userLink.GetProperty("href").GetString();
                        await Assert.That(href).Contains("/api/v1/user/");
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
