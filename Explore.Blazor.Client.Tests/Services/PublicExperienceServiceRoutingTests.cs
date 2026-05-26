// ABOUTME: Unit tests for PublicExperienceService covering settings retrieval and route decision logic.
// ABOUTME: Verifies typed BFF exception fallback to null and deterministic route resolution for home page modes.

using System.Net;
using System.Net.Http.Json;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for PublicExperienceService.
/// </summary>
/// <remarks>
/// These tests verify:
/// - GetSettingsAsync returns model when the typed BFF API returns valid JSON
/// - GetSettingsAsync returns null when the HTTP pipeline throws
/// - ResolveHomeRoute returns /home for LandingPage
/// - ResolveHomeRoute returns /events for EventList and null settings
/// </remarks>
public class PublicExperienceServiceRoutingTests
{
    private Func<HttpRequestMessage, HttpResponseMessage> _bffHandler = _ => new HttpResponseMessage(HttpStatusCode.NotFound);
    private readonly ILogger<PublicExperienceService> _logger;
    private readonly PublicExperienceService _service;

    public PublicExperienceServiceRoutingTests()
    {
        _logger = Substitute.For<ILogger<PublicExperienceService>>();
        var client = new HttpClient(new StubHttpMessageHandler(request => _bffHandler(request)))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var api = RestService.For<IPublicExperienceApi>(client);
        _service = new PublicExperienceService(api, _logger);
    }

    // ========== GetSettingsAsync ==========

    #region GetSettingsAsync Tests

    [Test]
    public async Task GetSettingsAsync_ReturnsSettings_WhenHttpSucceeds()
    {
        // Arrange
        var settings = new PublicExperienceSettingsModel
        {
            TenantId = Guid.NewGuid(),
            Mode = "DiscoveryCentric",
            PreferredHomePage = "LandingPage",
            BrandDisplayName = "Explore"
        };

        SetupBffClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(settings)
        });

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Mode).IsEqualTo("DiscoveryCentric");
        await Assert.That(result!.PreferredHomePage).IsEqualTo("LandingPage");
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsNull_WhenFactoryThrows()
    {
        // Arrange
        SetupBffClient(_ => throw new HttpRequestException("factory unavailable"));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== ResolveHomeRoute ==========

    #region ResolveHomeRoute Tests

    [Test]
    public async Task ResolveHomeRoute_ReturnsHome_WhenPreferredHomePageIsLandingPage()
    {
        // Arrange
        var settings = new PublicExperienceSettingsModel { PreferredHomePage = "LandingPage" };

        // Act
        var route = _service.ResolveHomeRoute(settings);

        // Assert
        await Assert.That(route).IsEqualTo("/home");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsEvents_WhenPreferredHomePageIsEventList()
    {
        // Arrange
        var settings = new PublicExperienceSettingsModel { PreferredHomePage = "EventList" };

        // Act
        var route = _service.ResolveHomeRoute(settings);

        // Assert
        await Assert.That(route).IsEqualTo("/events");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsEvents_WhenSettingsAreNull()
    {
        // Act
        var route = _service.ResolveHomeRoute((PublicExperienceSettingsModel?)null);

        // Assert
        await Assert.That(route).IsEqualTo("/events");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsHome_WhenShellPreferredHomePageIsLandingPage()
    {
        // Arrange
        var shell = new PublicExperienceShellModel
        {
            Mode = "DiscoveryCentric",
            Home = new PublicExperienceHomeModel
            {
                PreferredHomePage = "LandingPage"
            }
        };

        // Act
        var route = _service.ResolveHomeRoute(shell);

        // Assert
        await Assert.That(route).IsEqualTo("/home");
    }

    #endregion

    private void SetupBffClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _bffHandler = handler;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_factory(request));
        }
    }
}
