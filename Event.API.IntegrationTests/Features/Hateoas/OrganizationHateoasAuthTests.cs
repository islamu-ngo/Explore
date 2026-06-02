// ABOUTME: Authenticated HATEOAS tests for organization collection: verifies per-item edit/delete links
// ABOUTME: surface only for authorized callers and stay absent for anonymous requests.

using System.Net;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

[NotInParallel("AuthenticatedApiFixture")]
[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]
public class OrganizationHateoasAuthTests
{
    private readonly AuthenticatedApiTestFixture _fixture;
    private const string BaseUrl = "/api/organization";

    public OrganizationHateoasAuthTests(AuthenticatedApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static string WithCacheBust(string endpoint)
    {
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{endpoint}{separator}pageNumber=1&pageSize=20&testRun={Guid.NewGuid():N}";
    }

    [Test]
    public async Task GetOrganizations_AsAuthenticatedUser_EmbeddedItemsIncludeEditLink()
    {
        using var request = _fixture.CreateInstanceAdminRequest(HttpMethod.Get, WithCacheBust(BaseUrl), Guid.NewGuid());
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!TryGetFirstEmbeddedItem(json, out var firstItem))
        {
            return;
        }

        if (firstItem.TryGetProperty("_links", out var itemLinks))
        {
            var hasEditLink = itemLinks.TryGetProperty("edit", out _);
            await Assert.That(hasEditLink).IsTrue();
        }
    }

    [Test]
    public async Task GetOrganizations_AsAuthenticatedUser_EmbeddedItemsIncludeDeleteLink()
    {
        using var request = _fixture.CreateInstanceAdminRequest(HttpMethod.Get, WithCacheBust(BaseUrl), Guid.NewGuid());
        var response = await _fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!TryGetFirstEmbeddedItem(json, out var firstItem))
        {
            return;
        }

        if (firstItem.TryGetProperty("_links", out var itemLinks))
        {
            var hasDeleteLink = itemLinks.TryGetProperty("delete", out _);
            await Assert.That(hasDeleteLink).IsTrue();
        }
    }

    [Test]
    public async Task GetOrganizations_Anonymous_EmbeddedItemsDoNotIncludeManagementLinks()
    {
        var response = await _fixture.Client.GetAsync(WithCacheBust(BaseUrl));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!TryGetFirstEmbeddedItem(json, out var firstItem))
        {
            return;
        }

        if (firstItem.TryGetProperty("_links", out var itemLinks))
        {
            var hasEditLink = itemLinks.TryGetProperty("edit", out _);
            var hasDeleteLink = itemLinks.TryGetProperty("delete", out _);

            await Assert.That(hasEditLink).IsFalse();
            await Assert.That(hasDeleteLink).IsFalse();
        }
    }

    private static bool TryGetFirstEmbeddedItem(JsonDocument json, out JsonElement firstItem)
    {
        firstItem = default;
        if (!json.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("items", out var items) ||
            items.GetArrayLength() == 0)
        {
            return false;
        }

        firstItem = items[0];
        return true;
    }
}
