// ABOUTME: API contract tests for the read-only AT Protocol record discovery surface.
// ABOUTME: Proves GET/HAL discovery remains while direct record mutations stay unreachable.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;

namespace Event.Api.IntegrationTests.Features;

[ClassDataSource<ContractApiFixture>(Shared = SharedType.PerAssembly)]
public sealed class AtprotoRecordControllerTests(ContractApiFixture fixture)
{
    private const string CollectionPath = "/api/atprotorecord";
    private const string OpenApiPath = "/openapi/islamu-event.json";

    [Test]
    public async Task GetAll_WhenAnonymous_ReturnsPublicReadOnlyHalDiscovery()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, CollectionPath);
        request.Headers.Accept.ParseAdd("application/hal+json");

        using var response = await fixture.Client.SendAsync(request);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var links = document.RootElement.GetProperty("_links");
        await Assert.That(links.GetProperty("self").GetProperty("href").GetString())
            .Contains(CollectionPath);
        await Assert.That(links.TryGetProperty("create", out _)).IsFalse();
    }

    [Test]
    public async Task OpenApiDocument_ExposesOnlyAtprotoRecordReads()
    {
        using var response = await fixture.Client.GetAsync(OpenApiPath);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var paths = document.RootElement.GetProperty("paths");
        var collection = paths.GetProperty(CollectionPath);
        var detail = paths.GetProperty($"{CollectionPath}/{{id}}");

        await Assert.That(collection.TryGetProperty("get", out _)).IsTrue();
        await Assert.That(collection.TryGetProperty("post", out _)).IsFalse();
        await Assert.That(detail.TryGetProperty("get", out _)).IsTrue();
        await Assert.That(detail.TryGetProperty("put", out _)).IsFalse();
        await Assert.That(detail.TryGetProperty("delete", out _)).IsFalse();
        await Assert.That(collection.GetProperty("get").GetProperty("x-endpoint-class").GetString())
            .IsEqualTo("Public");
        await Assert.That(detail.GetProperty("get").GetProperty("x-endpoint-class").GetString())
            .IsEqualTo("Public");

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        await Assert.That(schemas.TryGetProperty("CreateAtprotoRecordDto", out _)).IsFalse();
        await Assert.That(schemas.TryGetProperty("UpdateAtprotoRecordDto", out _)).IsFalse();
        await Assert.That(schemas.GetProperty("AtprotoRecordDto").GetProperty("properties")
                .EnumerateObject().Select(property => property.Name))
            .IsEquivalentTo(["id", "did", "collection", "recordKey", "cid", "uri", "indexedAt"]);
        await Assert.That(schemas.GetProperty("AtprotoRecordListDto").GetProperty("properties")
                .EnumerateObject().Select(property => property.Name))
            .IsEquivalentTo(["id", "did", "collection", "recordKey", "indexedAt"]);
    }

    [Test]
    [Arguments("POST", CollectionPath)]
    [Arguments("PUT", CollectionPath + "/00000000-0000-0000-0000-000000000001")]
    [Arguments("DELETE", CollectionPath + "/00000000-0000-0000-0000-000000000001")]
    public async Task MutationRoutes_AreNotMapped(string method, string path)
    {
        using var request = fixture.CreateAuthenticatedRequest(new HttpMethod(method), path);
        request.Content = JsonContent.Create(new { });

        using var response = await fixture.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.MethodNotAllowed);
    }
}
