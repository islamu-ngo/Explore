// ABOUTME: Preference BFF endpoints: theme, language, and current-user info.
// ABOUTME: Persists user preferences via cookies and returns safe claim subsets.

namespace Explore.Blazor.Extensions;

using System.Globalization;
using System.Net.Http.Json;
using Explore.Application.DTOs.Appearance;
using Explore.Domain.Common.Localization;
using Microsoft.AspNetCore.Localization;

public static class BffPreferenceEndpoints
{
    /// <summary>
    /// Maps preference endpoints: POST /bff/theme, POST /bff/language, GET /bff/me.
    /// </summary>
    public static WebApplication MapPreferenceEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/theme", HandleThemePreference)
            .ExcludeFromDescription();

        app.MapGet("/bff/theme", HandleGetThemePreferenceAsync)
            .ExcludeFromDescription();

        app.MapPost("/bff/language", HandleLanguagePreference)
            .ExcludeFromDescription();

        app.MapPost("/bff/direction", HandleDirectionPreference)
            .ExcludeFromDescription();

        app.MapGet("/bff/me", HandleGetCurrentUser);

        return app;
    }

    private static async Task<IResult> HandleThemePreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var themeMode = ctx.Request.Query["theme"].ToString().Trim().ToLowerInvariant();

        if (themeMode is not "dark" and not "light" and not "system")
        {
            return Results.Problem(
                detail: "Theme must be 'system', 'dark', or 'light'.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid theme preference");
        }

        // Preserve current direction when only updating theme
        var currentDirection = ctx.Request.Cookies["direction"] ?? "auto";

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var request = new UpdateUserAppearancePreferencesDto
            {
                ThemeMode = themeMode,
                Direction = currentDirection
            };

            var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var response = await clientFactory.CreateClient("BffClient")
                .PutAsJsonAsync("api/user/appearance", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem(
                    detail: "Authenticated theme preference could not be persisted.",
                    statusCode: (int)response.StatusCode,
                    title: "Theme preference update failed");
            }
        }

        PersistThemeCookie(ctx, themeMode);

        return Results.Ok(new UserAppearancePreferencesDto
        {
            ThemeMode = themeMode,
            Direction = currentDirection
        });
    }

    private static async Task<IResult> HandleGetThemePreferenceAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var response = await clientFactory.CreateClient("BffClient")
                .GetAsync("api/user/appearance", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var preferences = await response.Content.ReadFromJsonAsync<UserAppearancePreferencesDto>(cancellationToken: cancellationToken);
                if (preferences is not null)
                {
                    return Results.Ok(preferences);
                }
            }
        }

        var theme = ctx.Request.Cookies["theme"];
        var direction = ctx.Request.Cookies["direction"];
        return Results.Ok(new UserAppearancePreferencesDto
        {
            ThemeMode = theme is "dark" or "light" ? theme : "system",
            Direction = direction is "ltr" or "rtl" ? direction : "auto"
        });
    }

    private static async Task<IResult> HandleLanguagePreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var rawLang = ctx.Request.Query["lang"].ToString();

        if (!CultureRegistry.TryGetEntry(rawLang, out var entry))
        {
            return Results.Problem(
                detail: "Language must be a supported culture code registered in CultureRegistry.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid language preference");
        }

        var normalizedLang = entry.Code;
        var currentTheme = ctx.Request.Cookies["theme"] ?? "system";
        var currentDirection = ctx.Request.Cookies["direction"] ?? "auto";

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var request = new UpdateUserAppearancePreferencesDto
            {
                ThemeMode = currentTheme is "dark" or "light" ? currentTheme : "system",
                Direction = currentDirection is "ltr" or "rtl" ? currentDirection : "auto",
                Language = normalizedLang
            };

            var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var response = await clientFactory.CreateClient("BffClient")
                .PutAsJsonAsync("api/user/appearance", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem(
                    detail: "Authenticated language preference could not be persisted.",
                    statusCode: (int)response.StatusCode,
                    title: "Language preference update failed");
            }
        }

        PersistLanguageCookie(ctx, normalizedLang);
        PersistAspNetCoreCultureCookie(ctx, normalizedLang);

        return Results.Ok(new UserAppearancePreferencesDto
        {
            ThemeMode = currentTheme is "dark" or "light" ? currentTheme : "system",
            Direction = currentDirection is "ltr" or "rtl" ? currentDirection : "auto",
            Language = normalizedLang
        });
    }

    private static void PersistLanguageCookie(HttpContext ctx, string languageCode)
    {
        var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        ctx.Response.Cookies.Append("lang", languageCode, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = false,
            Secure = !isDev
        });
    }

    private static void PersistAspNetCoreCultureCookie(HttpContext ctx, string languageCode)
    {
        var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(new CultureInfo(languageCode)));

        ctx.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = !isDev
            });
    }

    private static IResult HandleGetCurrentUser(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                detail: "Authentication is required to access the current-user BFF endpoint.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required");
        }

        var safeClaims = new[]
        {
            "preferred_username", "email", "name", "given_name", "family_name", "sub"
        };

        return Results.Ok(new
        {
            Name = ctx.User.Identity?.Name,
            Claims = ctx.User.Claims
                .Where(c => safeClaims.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
                .Select(c => new { c.Type, c.Value })
        });
    }

    private static async Task<IResult> HandleDirectionPreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var direction = ctx.Request.Query["dir"].ToString().Trim().ToLowerInvariant();

        if (direction is not "auto" and not "ltr" and not "rtl")
        {
            return Results.Problem(
                detail: "Direction must be 'auto', 'ltr', or 'rtl'.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid direction preference");
        }

        // Preserve current theme when only updating direction
        var currentTheme = ctx.Request.Cookies["theme"] ?? "system";

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var request = new UpdateUserAppearancePreferencesDto
            {
                ThemeMode = currentTheme is "dark" or "light" ? currentTheme : "system",
                Direction = direction
            };

            var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var response = await clientFactory.CreateClient("BffClient")
                .PutAsJsonAsync("api/user/appearance", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem(
                    detail: "Authenticated direction preference could not be persisted.",
                    statusCode: (int)response.StatusCode,
                    title: "Direction preference update failed");
            }
        }

        PersistDirectionCookie(ctx, direction);

        return Results.Ok(new UserAppearancePreferencesDto
        {
            ThemeMode = currentTheme is "dark" or "light" ? currentTheme : "system",
            Direction = direction
        });
    }

    private static void PersistDirectionCookie(HttpContext ctx, string direction)
    {
        var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        if (direction == "auto")
        {
            ctx.Response.Cookies.Delete("direction", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = !isDev
            });
            return;
        }

        ctx.Response.Cookies.Append("direction", direction, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = false,
            Secure = !isDev
        });
    }

    private static void PersistThemeCookie(HttpContext ctx, string themeMode)
    {
        var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        if (themeMode == "system")
        {
            ctx.Response.Cookies.Delete("theme", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = !isDev
            });
            return;
        }

        ctx.Response.Cookies.Append("theme", themeMode, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = false,
            Secure = !isDev
        });
    }
}
