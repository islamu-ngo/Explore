// ABOUTME: Unit tests for AppearanceThemeService covering theme composition, mode resolution, HC modes, and persistence.
// ABOUTME: Verifies the IAppearanceThemeService API surface with AppearanceState, profile management, and preset operations.

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
        await Assert.That(theme.LayoutProperties.DefaultBorderRadius).IsEqualTo("12px");
        await Assert.That(theme.Typography?.Default?.FontFamily?.FirstOrDefault()).IsEqualTo("Inter");
    }

    [Test]
    public async Task ResolveEffectiveDarkModeAsync_ReturnsDarkForDarkMode()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        service.Current.ThemeMode = "dark";

        var result = await service.ResolveEffectiveDarkModeAsync(null!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ResolveEffectiveDarkModeAsync_ReturnsLightForLightMode()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        service.Current.ThemeMode = "light";

        var result = await service.ResolveEffectiveDarkModeAsync(null!);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ResolveEffectiveDarkModeAsync_ReturnsDarkForDarkHighContrastMode()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        service.Current.ThemeMode = "darkhighcontrast";

        var result = await service.ResolveEffectiveDarkModeAsync(null!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ResolveEffectiveDarkModeAsync_ReturnsLightForLightHighContrastMode()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        service.Current.ThemeMode = "lighthighcontrast";

        var result = await service.ResolveEffectiveDarkModeAsync(null!);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ResolveEffectiveDarkModeAsync_ReturnsServerHintForSystemMode()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));
        service.Current.ThemeMode = "system";
        service.Current.ServerEffectiveDarkMode = true;

        var result = await service.ResolveEffectiveDarkModeAsync(null!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Current_InitialState_HasSystemThemeMode()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.That(service.Current.ThemeMode).IsEqualTo("system");
    }

    [Test]
    public async Task GeneratePalettePreview_ReturnsFallbackOnFailure()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var palette = service.GeneratePalettePreview("#475569", "#3B82F6", false);

        await Assert.That(palette).IsNotNull();
        await Assert.That(palette.Primary).IsEqualTo("#0F62FE");
    }

    [Test]
    public async Task GeneratePalettePreview_ReturnsFallbackForDarkOnFailure()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var palette = service.GeneratePalettePreview("#475569", "#3B82F6", true);

        await Assert.That(palette).IsNotNull();
        await Assert.That(palette.Primary).IsEqualTo("#3B82F6");
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