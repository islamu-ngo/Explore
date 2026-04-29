// ABOUTME: Preference BFF endpoints: appearance, theme mode, language, direction, and current-user info.
// ABOUTME: For authenticated users the API is authoritative; cookies mirror the server state for anonymous SSR.

namespace Explore.Blazor.Extensions;

using System.Globalization;
using System.Net.Http.Json;
using Explore.Application.DTOs.Appearance;
using Explore.Domain.Common.Localization;
using Microsoft.AspNetCore.Localization;

public static class BffPreferenceEndpoints
{
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

        app.MapGet("/bff/ui-themes", HandleGetAvailableThemesAsync)
            .ExcludeFromDescription();

        app.MapGet("/bff/me", HandleGetCurrentUser);

        app.MapGet("/bff/appearance", HandleGetResolvedAppearanceAsync)
            .ExcludeFromDescription();

        app.MapGet("/bff/appearance/presets", HandleGetPresetsAsync)
            .ExcludeFromDescription();

        app.MapGet("/bff/appearance/profiles", HandleGetProfilesAsync)
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/active-profile", HandleSetActiveProfileAsync)
            .ExcludeFromDescription();

        app.MapPost("/bff/appearance/profiles/from-preset/{presetId:guid}", HandleClonePresetAsync)
            .ExcludeFromDescription();

        app.MapPost("/bff/appearance/profiles", HandleCreateProfileAsync)
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/profiles/{profileId:guid}", HandleUpdateProfileAsync)
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/mode", HandleSetThemeModeAsync)
            .ExcludeFromDescription();

        app.MapGet("/bff/appearance/generate-palette", HandleGeneratePaletteAsync)
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/profiles/{profileId:guid}/archive", HandleArchiveProfileAsync)
            .ExcludeFromDescription();

        app.MapPost("/bff/appearance/profiles/{profileId:guid}/duplicate", HandleDuplicateProfileAsync)
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleGetResolvedAppearanceAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(BuildDefaultResolvedAppearance(ctx));
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .GetAsync("api/user/appearance", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Ok(BuildDefaultResolvedAppearance(ctx));
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleGetPresetsAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(Array.Empty<AvailablePresetDto>());
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .GetAsync("api/user/appearance/presets", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Ok(Array.Empty<AvailablePresetDto>());
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleGetProfilesAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(Array.Empty<UserAppearanceProfileDto>());
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .GetAsync("api/user/appearance/profiles", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Ok(Array.Empty<UserAppearanceProfileDto>());
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleSetActiveProfileAsync(HttpContext ctx, Guid profileId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        var request = new SetActiveProfileRequestDto { ProfileId = profileId };
        using var response = await clientFactory.CreateClient("BffClient")
            .PutAsJsonAsync("api/user/appearance/active-profile", request, cancellationToken);

        return response.IsSuccessStatusCode
            ? Results.Ok()
            : Results.Problem(detail: "Could not set active profile.", statusCode: (int)response.StatusCode, title: "Active profile update failed");
    }

    private static async Task<IResult> HandleClonePresetAsync(HttpContext ctx, Guid presetId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PostAsJsonAsync($"api/user/appearance/profiles/from-preset/{presetId}", (object?)null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not clone preset.", statusCode: (int)response.StatusCode, title: "Preset clone failed");
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleCreateProfileAsync(HttpContext ctx, CreateCustomProfileRequestDto request, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PostAsJsonAsync("api/user/appearance/profiles", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not create custom profile.", statusCode: (int)response.StatusCode, title: "Profile creation failed");
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleUpdateProfileAsync(HttpContext ctx, Guid profileId, UpdateAppearanceProfileRequestDto request, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PutAsJsonAsync($"api/user/appearance/profiles/{profileId}", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not update profile.", statusCode: (int)response.StatusCode, title: "Profile update failed");
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleSetThemeModeAsync(HttpContext ctx, SetThemeModeRequestDto request, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
var mode = request.ThemeMode?.Trim().ToLowerInvariant();
        var validModes = new[] { "system", "light", "dark", "lighthighcontrast", "darkhighcontrast", "custom" };
        if (mode is null || !validModes.Contains(mode))
        {
            return Results.Problem(detail: "Theme mode must be one of: system, light, dark, lighthighcontrast, darkhighcontrast, custom.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid theme mode");
        }

            PersistThemeCookie(ctx, mode);
            return Results.Ok(new { themeMode = mode });
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PutAsJsonAsync("api/user/appearance/mode", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not set theme mode.", statusCode: (int)response.StatusCode, title: "Theme mode update failed");
        }

        PersistThemeCookie(ctx, request.ThemeMode ?? "system");
        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleGeneratePaletteAsync(HttpContext ctx, string naturalColor, string brandColor, bool isDark, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .GetAsync($"api/user/appearance/generate-palette?naturalColor={Uri.EscapeDataString(naturalColor)}&brandColor={Uri.EscapeDataString(brandColor)}&isDark={isDark}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not generate palette.", statusCode: (int)response.StatusCode, title: "Palette generation failed");
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static async Task<IResult> HandleArchiveProfileAsync(HttpContext ctx, Guid profileId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PutAsync($"api/user/appearance/profiles/{profileId}/archive", null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not archive profile.", statusCode: (int)response.StatusCode, title: "Archive profile failed");
        }

        return Results.Ok();
    }

    private static async Task<IResult> HandleDuplicateProfileAsync(HttpContext ctx, Guid profileId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PostAsync($"api/user/appearance/profiles/{profileId}/duplicate", null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(detail: "Could not duplicate profile.", statusCode: (int)response.StatusCode, title: "Duplicate profile failed");
        }

        return Results.Stream(await response.Content.ReadAsStreamAsync(cancellationToken), "application/json");
    }

    private static ResolvedAppearanceDto BuildDefaultResolvedAppearance(HttpContext ctx)
    {
        var theme = ctx.Request.Cookies["theme"];
        var direction = ctx.Request.Cookies["direction"];
        var lang = ctx.Request.Cookies["lang"];

        var validModes = new[] { "system", "light", "dark", "lighthighcontrast", "darkhighcontrast", "custom" };
        var resolvedMode = validModes.Contains(theme) ? theme! : "system";

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

    private static async Task<IResult> HandleThemePreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var themeMode = ctx.Request.Query["theme"].ToString().Trim().ToLowerInvariant();

        var validModes = new[] { "system", "light", "dark", "lighthighcontrast", "darkhighcontrast", "custom" };
        if (!validModes.Contains(themeMode))
        {
            return Results.Problem(
                detail: "Theme mode must be one of: system, light, dark, lighthighcontrast, darkhighcontrast, custom.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid theme preference");
        }

        var current = await ReadCurrentPreferencesAsync(ctx, cancellationToken);
        var updated = current with { ThemeMode = themeMode };

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var persistResult = await PersistAuthenticatedAsync(ctx, updated, "Theme preference update failed", cancellationToken);
            if (persistResult is { } problem)
            {
                return problem;
            }
        }

        PersistThemeCookie(ctx, themeMode);
        return Results.Ok(updated);
    }

    private static async Task<IResult> HandleGetThemePreferenceAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var preferences = await ReadAuthenticatedAsync(ctx, cancellationToken);
            if (preferences is not null)
            {
                return Results.Ok(preferences);
            }
        }

        return Results.Ok(ReadCookiePreferences(ctx));
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
        var current = await ReadCurrentPreferencesAsync(ctx, cancellationToken);
        var updated = current with { Language = normalizedLang };

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var persistResult = await PersistAuthenticatedAsync(ctx, updated, "Language preference update failed", cancellationToken);
            if (persistResult is { } problem)
            {
                return problem;
            }
        }

        PersistLanguageCookie(ctx, normalizedLang);
        PersistAspNetCoreCultureCookie(ctx, normalizedLang);
        return Results.Ok(updated);
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

        var current = await ReadCurrentPreferencesAsync(ctx, cancellationToken);
        var updated = current with { Direction = direction };

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var persistResult = await PersistAuthenticatedAsync(ctx, updated, "Direction preference update failed", cancellationToken);
            if (persistResult is { } problem)
            {
                return problem;
            }
        }

        PersistDirectionCookie(ctx, direction);
        return Results.Ok(updated);
    }

    private static async Task<IResult> HandleGetAvailableThemesAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                detail: "Authentication is required to list available themes.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required");
        }

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .GetAsync("api/user/appearance/themes", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(
                detail: "Available themes could not be fetched from the API.",
                statusCode: (int)response.StatusCode,
                title: "Theme catalog unavailable");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return Results.Content(payload, response.Content.Headers.ContentType?.MediaType ?? "application/json");
    }

    private static async Task<UserAppearancePreferencesDto> ReadCurrentPreferencesAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var authenticated = await ReadAuthenticatedAsync(ctx, cancellationToken);
            if (authenticated is not null)
            {
                return authenticated;
            }
        }

        return ReadCookiePreferences(ctx);
    }

    private static async Task<UserAppearancePreferencesDto?> ReadAuthenticatedAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .GetAsync("api/user/appearance", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserAppearancePreferencesDto>(cancellationToken: cancellationToken);
    }

    private static UserAppearancePreferencesDto ReadCookiePreferences(HttpContext ctx)
    {
        var theme = ctx.Request.Cookies["theme"];
        var direction = ctx.Request.Cookies["direction"];
        var lang = ctx.Request.Cookies["lang"];

        return new UserAppearancePreferencesDto
        {
            ThemeMode = theme is "dark" or "light" ? theme : "system",
            Direction = direction is "ltr" or "rtl" ? direction : "auto",
            Language = CultureRegistry.TryGetEntry(lang ?? string.Empty, out var entry) ? entry.Code : "en",
            DefaultThemeId = null
        };
    }

    private static async Task<IResult?> PersistAuthenticatedAsync(
        HttpContext ctx,
        UserAppearancePreferencesDto preferences,
        string failureTitle,
        CancellationToken cancellationToken)
    {
        var request = new UpdateUserAppearancePreferencesDto
        {
            ThemeMode = preferences.ThemeMode,
            Direction = preferences.Direction,
            Language = preferences.Language,
            DefaultThemeId = preferences.DefaultThemeId
        };

        var clientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var response = await clientFactory.CreateClient("BffClient")
            .PutAsJsonAsync("api/user/appearance", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        return Results.Problem(
            detail: "Authenticated preference could not be persisted.",
            statusCode: (int)response.StatusCode,
            title: failureTitle);
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