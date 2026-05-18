// ABOUTME: Unit-style tests for anonymous BFF preference cookie defaults and persistence.
// ABOUTME: Protects preference endpoint decomposition from changing SSR cookie behavior.

using Explore.Blazor.Services.Preferences;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffPreferenceCookieServiceTests
{
    [Test]
    public async Task BuildDefaultResolvedAppearance_WithCookieValues_ReturnsResolvedAppearance()
    {
        var service = CreateService();
        var context = CreateContext();
        context.Request.Headers.Cookie = "theme=darkhighcontrast; direction=rtl; lang=fr";

        var result = service.BuildDefaultResolvedAppearance(context);

        result.ThemeMode.Should().Be("darkhighcontrast");
        result.Direction.Should().Be("rtl");
        result.Language.Should().Be("fr");
        result.ServerEffectiveDarkMode.Should().BeTrue();
        await Assert.That(result.Theme).IsNull();
    }

    [Test]
    public async Task ReadCookiePreferences_WithUnsupportedTheme_FallsBackToSystem()
    {
        var service = CreateService();
        var context = CreateContext();
        context.Request.Headers.Cookie = "theme=custom; direction=ltr; lang=fr";

        var result = service.ReadCookiePreferences(context);

        result.ThemeMode.Should().Be("system");
        result.Direction.Should().Be("ltr");
        result.Language.Should().Be("fr");
        await Assert.That(result.DefaultThemeId).IsNull();
    }

    [Test]
    public async Task PersistLanguageCookies_AppendsLanguageAndAspNetCoreCultureCookies()
    {
        var service = CreateService();
        var context = CreateContext();

        service.PersistLanguageCookie(context, "fr");
        service.PersistAspNetCoreCultureCookie(context, "fr");

        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().Contain(header => header.StartsWith("lang=fr", StringComparison.Ordinal));
        setCookieHeaders.Should().Contain(header => header.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
        setCookieHeaders.Should().HaveCount(2);
    }

    [Test]
    public async Task PersistDirectionCookie_WithAuto_DeletesDirectionCookie()
    {
        var service = CreateService();
        var context = CreateContext();

        service.PersistDirectionCookie(context, "auto");

        var setCookieHeader = context.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain("direction=");
        setCookieHeader.Should().Contain("expires=", Exactly.Once());
        setCookieHeader.ToLowerInvariant().Should().Contain("samesite=lax");
    }

    private static BffPreferenceCookieService CreateService()
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Development");

        return new BffPreferenceCookieService(environment);
    }

    private static DefaultHttpContext CreateContext() => new();
}
