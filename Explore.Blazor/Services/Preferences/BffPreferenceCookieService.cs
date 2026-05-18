// ABOUTME: BFF preference cookie helper for anonymous SSR appearance, language, and direction state.
// ABOUTME: Centralizes cookie defaults so preference endpoints stay focused on routing and API forwarding.

namespace Explore.Blazor.Services.Preferences;

using System.Globalization;
using Explore.Application.DTOs.Appearance;
using Explore.Domain.Common.Localization;
using Microsoft.AspNetCore.Localization;

public interface IBffPreferenceCookieService
{
    ResolvedAppearanceDto BuildDefaultResolvedAppearance(HttpContext context);

    UserAppearancePreferencesDto ReadCookiePreferences(HttpContext context);

    void PersistThemeCookie(HttpContext context, string themeMode);

    void PersistLanguageCookie(HttpContext context, string languageCode);

    void PersistAspNetCoreCultureCookie(HttpContext context, string languageCode);

    void PersistDirectionCookie(HttpContext context, string direction);
}

public sealed class BffPreferenceCookieService(IWebHostEnvironment environment) : IBffPreferenceCookieService
{
    private static readonly string[] ValidThemeModes =
    [
        "system",
        "light",
        "dark",
        "lighthighcontrast",
        "darkhighcontrast",
        "custom"
    ];

    private static readonly TimeSpan PreferenceCookieLifetime = TimeSpan.FromDays(365);

    public ResolvedAppearanceDto BuildDefaultResolvedAppearance(HttpContext context)
    {
        var theme = context.Request.Cookies["theme"];
        var direction = context.Request.Cookies["direction"];
        var lang = context.Request.Cookies["lang"];
        var resolvedMode = ValidThemeModes.Contains(theme) ? theme! : "system";

        return new ResolvedAppearanceDto
        {
            ThemeMode = resolvedMode,
            Direction = direction is "ltr" or "rtl" ? direction : "auto",
            Language = CultureRegistry.TryGetEntry(lang ?? string.Empty, out var entry) ? entry.Code : "en",
            ServerEffectiveDarkMode = resolvedMode switch
            {
                "dark" => true,
                "lighthighcontrast" => false,
                "darkhighcontrast" => true,
                "light" => false,
                _ => null
            }
        };
    }

    public UserAppearancePreferencesDto ReadCookiePreferences(HttpContext context)
    {
        var theme = context.Request.Cookies["theme"];
        var direction = context.Request.Cookies["direction"];
        var lang = context.Request.Cookies["lang"];

        return new UserAppearancePreferencesDto
        {
            ThemeMode = theme is "dark" or "light" ? theme : "system",
            Direction = direction is "ltr" or "rtl" ? direction : "auto",
            Language = CultureRegistry.TryGetEntry(lang ?? string.Empty, out var entry) ? entry.Code : "en",
            DefaultThemeId = null
        };
    }

    public void PersistThemeCookie(HttpContext context, string themeMode)
    {
        if (themeMode == "system")
        {
            context.Response.Cookies.Delete("theme", CreateTransientCookieOptions());
            return;
        }

        context.Response.Cookies.Append("theme", themeMode, CreatePersistentCookieOptions());
    }

    public void PersistLanguageCookie(HttpContext context, string languageCode)
    {
        context.Response.Cookies.Append("lang", languageCode, CreatePersistentCookieOptions());
    }

    public void PersistAspNetCoreCultureCookie(HttpContext context, string languageCode)
    {
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(new CultureInfo(languageCode)));

        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            CreatePersistentCookieOptions());
    }

    public void PersistDirectionCookie(HttpContext context, string direction)
    {
        if (direction == "auto")
        {
            context.Response.Cookies.Delete("direction", CreateTransientCookieOptions());
            return;
        }

        context.Response.Cookies.Append("direction", direction, CreatePersistentCookieOptions());
    }

    private CookieOptions CreatePersistentCookieOptions()
    {
        var options = CreateBaseCookieOptions();
        options.MaxAge = PreferenceCookieLifetime;
        return options;
    }

    private CookieOptions CreateTransientCookieOptions()
    {
        return CreateBaseCookieOptions();
    }

    private CookieOptions CreateBaseCookieOptions()
    {
        return new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = false,
            Secure = !environment.IsDevelopment()
        };
    }
}
