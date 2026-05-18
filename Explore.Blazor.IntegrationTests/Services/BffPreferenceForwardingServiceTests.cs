// ABOUTME: Unit-style tests for BFF preference API forwarding route construction.
// ABOUTME: Protects endpoint decomposition from drifting authenticated BffClient API calls.

using System.Net;
using System.Net.Http.Json;
using Explore.Application.DTOs.Appearance;
using Explore.Blazor.Services.Preferences;
using FluentAssertions;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffPreferenceForwardingServiceTests
{
    [Test]
    public async Task GetAppearanceAsync_UsesBffClientAndExpectedApiRoute()
    {
        using var handler = new CapturingHandler();
        var factory = new CapturingHttpClientFactory(handler);
        var service = new BffPreferenceForwardingService(factory);

        using var response = await service.GetAppearanceAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.ClientName.Should().Be("BffClient");
        handler.Requests.Should().ContainSingle();
        var request = handler.Requests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri?.ToString().Should().Be("https://bff.test/api/user/appearance");
    }

    [Test]
    public async Task PersistPreferencesAsync_MapsPreferenceDtoToApiUpdateRequest()
    {
        using var handler = new CapturingHandler();
        var service = new BffPreferenceForwardingService(new CapturingHttpClientFactory(handler));
        var defaultThemeId = Guid.NewGuid();
        var preferences = new UserAppearancePreferencesDto
        {
            ThemeMode = "dark",
            Direction = "rtl",
            Language = "fr",
            DefaultThemeId = defaultThemeId
        };

        using var response = await service.PersistPreferencesAsync(preferences, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri?.ToString().Should().Be("https://bff.test/api/user/appearance");

        var body = await request.Content!.ReadFromJsonAsync<UpdateUserAppearancePreferencesDto>();
        body.Should().NotBeNull();
        body!.ThemeMode.Should().Be("dark");
        body.Direction.Should().Be("rtl");
        body.Language.Should().Be("fr");
        body.DefaultThemeId.Should().Be(defaultThemeId);
    }

    [Test]
    public async Task GeneratePaletteAsync_EscapesPaletteQueryValues()
    {
        using var handler = new CapturingHandler();
        var service = new BffPreferenceForwardingService(new CapturingHttpClientFactory(handler));

        using var response = await service.GeneratePaletteAsync("blue green", "#ff/00", isDark: true, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri?.AbsoluteUri.Should().Be("https://bff.test/api/user/appearance/generate-palette?naturalColor=blue%20green&brandColor=%23ff%2F00&isDark=True");
    }

    private sealed class CapturingHttpClientFactory(CapturingHandler handler) : IHttpClientFactory
    {
        public string? ClientName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            ClientName = name;
            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://bff.test/")
            };
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
