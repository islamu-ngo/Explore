// ABOUTME: Unit tests for PublicExperienceService covering settings fetch and home route resolution.
// Verifies HTTP fallback behavior and route selection rules for preferred home page configuration.

using System.Net;
using System.Net.Http.Json;

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for PublicExperienceService.
/// </summary>
public class PublicExperienceServiceTests
{
    private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicExperienceService> _logger;
    private readonly PublicExperienceService _service;

    public PublicExperienceServiceTests()
    {
        _httpClientFactory = Substitute.For<System.Net.Http.IHttpClientFactory>();
        _logger = Substitute.For<ILogger<PublicExperienceService>>();
        _service = new PublicExperienceService(_httpClientFactory, _logger);
    }

    // ========== GetSettingsAsync ==========

    #region GetSettingsAsync Tests

    [Test]
    public async Task GetSettingsAsync_ReturnsSettings_WhenHttpSucceeds()
    {
        // Arrange
        var expected = new PublicExperienceSettingsModel
        {
            TenantId = Guid.NewGuid(),
            Mode = "OrganizationCentric",
            PreferredHomePage = "LandingPage"
        };

        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        _httpClientFactory.CreateClient("BffClient").Returns(client);

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Mode).IsEqualTo("OrganizationCentric");
        await Assert.That(result!.PreferredHomePage).IsEqualTo("LandingPage");
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsNull_WhenFactoryThrows()
    {
        // Arrange
        _httpClientFactory.CreateClient("BffClient")
            .Returns(_ => throw new HttpRequestException("factory failure"));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsNull_WhenHttpFails()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        _httpClientFactory.CreateClient("BffClient").Returns(client);

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    // ========== GetShellAsync ==========

    #region GetShellAsync Tests

    [Test]
    public async Task GetShellAsync_ReturnsShell_WhenHttpSucceeds()
    {
        // Arrange
        var expected = new PublicExperienceShellModel
        {
            Mode = "OrganizationCentric",
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationModel
            {
                State = "Available",
                DisplayName = "Community Center"
            },
            EventCatalog = new PublicExperienceEventCatalogModel
            {
                Label = "Programs",
                Url = "/events?ActorId=11111111-1111-1111-1111-111111111111"
            }
        };

        string? requestedPath = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedPath = request.RequestUri?.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            };
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        _httpClientFactory.CreateClient("BffClient").Returns(client);

        // Act
        var result = await _service.GetShellAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Mode).IsEqualTo("OrganizationCentric");
        await Assert.That(result.PrimaryOrganization.State).IsEqualTo("Available");
        await Assert.That(result.EventCatalog.Label).IsEqualTo("Programs");
        await Assert.That(requestedPath).IsEqualTo("/api/PublicExperience/shell");
    }

    [Test]
    public async Task GetShellAsync_ReturnsNull_WhenHttpFails()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        _httpClientFactory.CreateClient("BffClient").Returns(client);

        // Act
        var result = await _service.GetShellAsync();

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
        var settings = new PublicExperienceSettingsModel
        {
            PreferredHomePage = "LandingPage"
        };

        // Act
        var route = _service.ResolveHomeRoute(settings);

        // Assert
        await Assert.That(route).IsEqualTo("/home");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsHome_WhenPreferredHomePageIsLandingPageCaseInsensitive()
    {
        // Arrange
        var settings = new PublicExperienceSettingsModel
        {
            PreferredHomePage = "landingpage"
        };

        // Act
        var route = _service.ResolveHomeRoute(settings);

        // Assert
        await Assert.That(route).IsEqualTo("/home");
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
    public async Task ResolveHomeRoute_ReturnsHome_WhenOrganizationCentricPrimaryOrganizationIsAvailable()
    {
        // Arrange
        var shell = new PublicExperienceShellModel
        {
            Mode = "OrganizationCentric",
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationModel
            {
                State = "Available"
            }
        };

        // Act
        var route = _service.ResolveHomeRoute(shell);

        // Assert
        await Assert.That(route).IsEqualTo("/home");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsEvents_WhenOrganizationCentricPrimaryOrganizationIsUnavailable()
    {
        // Arrange
        var shell = new PublicExperienceShellModel
        {
            Mode = "OrganizationCentric",
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationModel
            {
                State = "Missing"
            }
        };

        // Act
        var route = _service.ResolveHomeRoute(shell);

        // Assert
        await Assert.That(route).IsEqualTo("/events");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsEvents_WhenShellIsNull()
    {
        // Act
        var route = _service.ResolveHomeRoute((PublicExperienceShellModel?)null);

        // Assert
        await Assert.That(route).IsEqualTo("/events");
    }

    [Test]
    public async Task ResolveHomeRoute_ReturnsEvents_WhenPreferredHomePageIsNotLandingPage()
    {
        // Arrange
        var settings = new PublicExperienceSettingsModel
        {
            PreferredHomePage = "EventList"
        };

        // Act
        var route = _service.ResolveHomeRoute(settings);

        // Assert
        await Assert.That(route).IsEqualTo("/events");
    }

    #endregion

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
