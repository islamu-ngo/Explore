using System.Net;
using Event.Api.IntegrationTests.Fixtures;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

/// <summary>
/// Unit tests for PreferHeaderMiddleware.
/// Tests RFC 7240 Prefer header processing behavior.
/// </summary>
[ClassDataSource<ApiTestFixture>(Shared = SharedType.PerAssembly)]
public class PreferHeaderMiddlewareTests
{
    private readonly ApiTestFixture _fixture;

    public PreferHeaderMiddlewareTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region Prefer Header Parsing

    [Test]
    public async Task Middleware_WithReturnMinimal_ShouldSetPreferenceAppliedHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=minimal");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsTrue();

        var preferenceApplied = response.Headers.GetValues("Preference-Applied").FirstOrDefault();
        await Assert.That(preferenceApplied).IsEqualTo("return=minimal");
    }

    [Test]
    public async Task Middleware_WithReturnRepresentation_ShouldNotSetPreferenceAppliedHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=representation");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        // Preference-Applied should NOT be set when using default (representation)
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsFalse();
    }

    [Test]
    public async Task Middleware_WithoutPreferHeader_ShouldNotSetPreferenceAppliedHeader()
    {
        // Arrange & Act
        var response = await _fixture.Client.GetAsync("/api/v1/organization");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsFalse();
    }

    [Test]
    public async Task Middleware_WithMultiplePreferences_ShouldParseReturnMinimal()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=minimal, respond-async, wait=100");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsTrue();

        var preferenceApplied = response.Headers.GetValues("Preference-Applied").FirstOrDefault();
        await Assert.That(preferenceApplied).IsEqualTo("return=minimal");
    }

    [Test]
    public async Task Middleware_WithCaseInsensitiveMinimal_ShouldRecognizeMinimal()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=MINIMAL");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsTrue();
    }

    [Test]
    public async Task Middleware_WithSpacesInPreferHeader_ShouldParseCorrectly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "  return=minimal  ,  wait=100  ");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsTrue();
    }

    #endregion

    #region Response Content Validation

    [Test]
    public async Task Middleware_WithReturnMinimal_ItemsShouldNotHaveLinks()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=minimal");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
            embedded.TryGetProperty("items", out var items) &&
            items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            var hasLinks = firstItem.TryGetProperty("_links", out _);
            await Assert.That(hasLinks).IsFalse();
        }
    }

    [Test]
    public async Task Middleware_WithReturnMinimal_CollectionShouldStillHavePaginationMetadata()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=minimal");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(content);

        // Pagination metadata should still be present
        await Assert.That(json.RootElement.TryGetProperty("pageNumber", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("pageSize", out _)).IsTrue();
        await Assert.That(json.RootElement.TryGetProperty("totalCount", out _)).IsTrue();
    }

    [Test]
    public async Task Middleware_WithoutPreferHeader_ItemsShouldHaveLinks()
    {
        // Arrange & Act
        var response = await _fixture.Client.GetAsync("/api/v1/organization");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("_embedded", out var embedded) &&
            embedded.TryGetProperty("items", out var items) &&
            items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            var hasLinks = firstItem.TryGetProperty("_links", out _);
            await Assert.That(hasLinks).IsTrue();
        }
    }

    #endregion

    #region Boolean Preferences

    [Test]
    public async Task Middleware_WithBooleanPreference_ShouldNotAffectResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "respond-async");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        // respond-async is not supported, so no Preference-Applied
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsFalse();

        // Response should have links (default behavior)
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("_links");
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Middleware_WithEmptyPreferHeader_ShouldNotAffectResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsFalse();
    }

    [Test]
    public async Task Middleware_WithUnknownPreferValue_ShouldIgnore()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organization");
        request.Headers.Add("Prefer", "return=unknown");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        // Unknown values should be ignored
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsFalse();

        // Response should have links (default behavior)
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("_links");
    }

    #endregion

    #region Multiple Endpoints

    [Test]
    [Arguments("/api/v1/organization")]
    [Arguments("/api/v1/event")]
    [Arguments("/api/v1/eventsession")]
    [Arguments("/api/v1/actor")]
    [Arguments("/api/v1/location")]
    [Arguments("/api/v1/category")]
    [Arguments("/api/v1/tag")]
    public async Task Middleware_AllEndpoints_ShouldRespectPreferMinimal(string endpoint)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Prefer", "return=minimal");

        // Act
        var response = await _fixture.Client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("Preference-Applied")).IsTrue();
    }

    #endregion
}
