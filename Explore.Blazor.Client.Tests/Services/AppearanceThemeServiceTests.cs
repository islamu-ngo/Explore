// ABOUTME: Unit tests for AppearanceThemeService covering theme composition and preference persistence.
// ABOUTME: Verifies the extracted runtime seam preserves existing palette/appbar behavior and BFF preference writes.

using System.Net;
using System.Net.Http.Json;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Services;

public class AppearanceThemeServiceTests
{
    [Test]
    public async Task CreateTheme_SetsExpectedAppbarHeightAndPalette()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var theme = service.CreateTheme("112px");

        await Assert.That(theme.LayoutProperties.AppbarHeight).IsEqualTo("112px");
        await Assert.That(theme.LayoutProperties.DefaultBorderRadius).IsEqualTo("8px");
        await Assert.That(theme.Typography?.Default?.FontFamily?.FirstOrDefault()).IsEqualTo("Inter");
    }

    [Test]
    public async Task ResolveInitialDarkModeAsync_ReturnsServerHintWhenPresent()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await service.ResolveInitialDarkModeAsync(true, null!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task PersistThemeModeAsync_PostsExpectedThemeValue()
    {
        var postedBody = string.Empty;
        var requestedPath = string.Empty;
        var service = CreateService(request =>
        {
            requestedPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            postedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await service.PersistThemeModeAsync(true);

        await Assert.That(requestedPath).IsEqualTo("/bff/theme");
        await Assert.That(postedBody).Contains("dark");
    }

    [Test]
    public async Task ResolveInitialDarkModeAsync_UsesBffPreferenceBeforeSystemFallback()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { themeMode = "dark" })
        });

        var result = await service.ResolveInitialDarkModeAsync(null, null!);

        await Assert.That(result).IsTrue();
    }

    private static AppearanceThemeService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var logger = Substitute.For<ILogger<AppearanceThemeService>>();
        var client = new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return new AppearanceThemeService(client, logger);
    }

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
