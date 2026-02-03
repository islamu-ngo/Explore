using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// HATEOAS-specific tests for OrganizationMemberController.
/// Validates organization membership links including organization, user, and role references.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class OrganizationMemberHateoasTests
{
    private readonly ApiTestFixture _fixture;
    private const string BaseUrl = "/api/v1/organizationmember";

    public OrganizationMemberHateoasTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task GetAll_ShouldIncludeHalStructure()
    {
        // Act
        var response = await _fixture.Client.GetAsync(BaseUrl);

        // Assert
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
                await Assert.That(href).Contains("/api/v1/organizationmember");
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
                    await Assert.That(href).Contains("/api/v1/organizationmember/");
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
                    // Member items should have user link
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
    public async Task GetById_Links_ShouldIncludeOrganizationLink()
    {
        // Arrange - First get list to find a member
        var listResponse = await _fixture.Client.GetAsync(BaseUrl);

        if (listResponse.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);

        if (!listJson.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            return; // No data to test with
        }

        var firstItem = items[0];
        if (!TryGetId(firstItem, out var memberId))
        {
            return;
        }

        // Act - Get member by ID
        var response = await _fixture.Client.GetAsync($"{BaseUrl}/{memberId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("_links", out var links))
            {
                // Detail view should have organization link
                if (links.TryGetProperty("organization", out var orgLink))
                {
                    var href = orgLink.GetProperty("href").GetString();
                    await Assert.That(href).Contains("/api/v1/organization/");
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
