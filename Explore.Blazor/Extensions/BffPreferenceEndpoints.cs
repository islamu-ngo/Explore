// ABOUTME: Preference BFF endpoints: theme, language, and current-user info.
// ABOUTME: Persists user preferences via cookies and returns safe claim subsets.

namespace Explore.Blazor.Extensions;

using System.Net.Http.Json;
using Explore.Application.DTOs.Appearance;

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

        app.MapGet("/bff/me", HandleGetCurrentUser);

        return app;
    }

    private static async Task<IResult> HandleThemePreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var request = new UpdateUserAppearancePreferencesDto
        {
            ThemeMode = ctx.Request.Query["theme"].ToString().Trim().ToLowerInvariant()
        };

        if (request.ThemeMode is not "dark" and not "light" and not "system")
        {
            return Results.Problem(
                detail: "Theme must be 'system', 'dark', or 'light'.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid theme preference");
        }

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
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

        PersistThemeCookie(ctx, request.ThemeMode);

        return Results.Ok(new UserAppearancePreferencesDto
        {
            ThemeMode = request.ThemeMode
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
        return Results.Ok(new UserAppearancePreferencesDto
        {
            ThemeMode = theme is "dark" or "light" ? theme : "system"
        });
    }

    private static IResult HandleLanguagePreference(HttpContext ctx)
    {
        var lang = ctx.Request.Query["lang"].ToString().Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(lang) && lang.Length is >= 2 and <= 5)
        {
            var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            ctx.Response.Cookies.Append("lang", lang, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Secure = !isDev
            });
            return Results.Ok();
        }

        return Results.Problem(
            detail: "Language must be a normalized code between 2 and 5 characters.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid language preference");
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
