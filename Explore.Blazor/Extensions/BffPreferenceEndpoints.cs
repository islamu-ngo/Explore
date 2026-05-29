// ABOUTME: Preference BFF endpoints: appearance, theme mode, language, direction, and current-user info.
// ABOUTME: For authenticated users the API is authoritative; cookies mirror the server state for anonymous SSR.

namespace Explore.Blazor.Extensions;

using System.Net.Http.Json;
using Explore.Application.DTOs.Appearance;
using Explore.Blazor.Services.Preferences;

public static class BffPreferenceEndpoints
{
    public static WebApplication MapPreferenceEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/theme", HandleThemePreference)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapGet("/bff/theme", HandleGetThemePreferenceAsync)
            .ExcludeFromDescription();

        app.MapPost("/bff/language", HandleLanguagePreference)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/direction", HandleDirectionPreference)
            .ValidateAntiforgery()
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
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/appearance/profiles/from-preset/{presetId:guid}", HandleClonePresetAsync)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/appearance/profiles", HandleCreateProfileAsync)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/profiles/{profileId:guid}", HandleUpdateProfileAsync)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/mode", HandleSetThemeModeAsync)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapGet("/bff/appearance/generate-palette", HandleGeneratePaletteAsync)
            .ExcludeFromDescription();

        app.MapPut("/bff/appearance/profiles/{profileId:guid}/archive", HandleArchiveProfileAsync)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/appearance/profiles/{profileId:guid}/duplicate", HandleDuplicateProfileAsync)
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleGetResolvedAppearanceAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        var fallback = GetPreferenceCookies(ctx).BuildDefaultResolvedAppearance(ctx);

        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(fallback);
        }

        using var response = await GetPreferenceForwarding(ctx).GetAppearanceAsync(cancellationToken);
        return await BffForwardingResults.JsonStreamOrFallbackAsync(response, fallback, cancellationToken);
    }

    private static async Task<IResult> HandleGetPresetsAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(Array.Empty<AvailablePresetDto>());
        }

        using var response = await GetPreferenceForwarding(ctx).GetPresetsAsync(cancellationToken);
        return await BffForwardingResults.JsonStreamOrFallbackAsync(response, Array.Empty<AvailablePresetDto>(), cancellationToken);
    }

    private static async Task<IResult> HandleGetProfilesAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(Array.Empty<UserAppearanceProfileDto>());
        }

        using var response = await GetPreferenceForwarding(ctx).GetProfilesAsync(cancellationToken);
        return await BffForwardingResults.JsonStreamOrFallbackAsync(response, Array.Empty<UserAppearanceProfileDto>(), cancellationToken);
    }

    private static async Task<IResult> HandleSetActiveProfileAsync(HttpContext ctx, Guid profileId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).SetActiveProfileAsync(profileId, cancellationToken);
        return BffForwardingResults.OkOrProblem(response, "Could not set active profile.", "Active profile update failed");
    }

    private static async Task<IResult> HandleClonePresetAsync(HttpContext ctx, Guid presetId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).ClonePresetAsync(presetId, cancellationToken);
        return await BffForwardingResults.JsonStreamOrProblemAsync(response, "Could not clone preset.", "Preset clone failed", cancellationToken);
    }

    private static async Task<IResult> HandleCreateProfileAsync(HttpContext ctx, CreateCustomProfileRequestDto request, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).CreateProfileAsync(request, cancellationToken);
        return await BffForwardingResults.JsonStreamOrProblemAsync(response, "Could not create custom profile.", "Profile creation failed", cancellationToken);
    }

    private static async Task<IResult> HandleUpdateProfileAsync(HttpContext ctx, Guid profileId, UpdateAppearanceProfileRequestDto request, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).UpdateProfileAsync(profileId, request, cancellationToken);
        return await BffForwardingResults.JsonStreamOrProblemAsync(response, "Could not update profile.", "Profile update failed", cancellationToken);
    }

    private static async Task<IResult> HandleSetThemeModeAsync(HttpContext ctx, SetThemeModeRequestDto request, CancellationToken cancellationToken)
    {
        var preferenceValidation = GetPreferenceValidation(ctx);
        var mode = preferenceValidation.NormalizeThemeMode(request.ThemeMode);
        if (mode is null)
        {
            return Results.Problem(
                detail: preferenceValidation.ThemeModeValidationMessage,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid theme mode");
        }

        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            GetPreferenceCookies(ctx).PersistThemeCookie(ctx, mode);
            return Results.Ok(new { themeMode = mode });
        }

        var normalizedRequest = new SetThemeModeRequestDto { ThemeMode = mode };
        using var response = await GetPreferenceForwarding(ctx).SetThemeModeAsync(normalizedRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return BffForwardingResults.Problem(response, "Could not set theme mode.", "Theme mode update failed");
        }

        GetPreferenceCookies(ctx).PersistThemeCookie(ctx, mode);
        return await BffForwardingResults.JsonStreamOrProblemAsync(response, "Could not set theme mode.", "Theme mode update failed", cancellationToken);
    }

    private static async Task<IResult> HandleGeneratePaletteAsync(HttpContext ctx, string naturalColor, string brandColor, bool isDark, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).GeneratePaletteAsync(naturalColor, brandColor, isDark, cancellationToken);
        return await BffForwardingResults.JsonStreamOrProblemAsync(response, "Could not generate palette.", "Palette generation failed", cancellationToken);
    }

    private static async Task<IResult> HandleArchiveProfileAsync(HttpContext ctx, Guid profileId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).ArchiveProfileAsync(profileId, cancellationToken);
        return BffForwardingResults.OkOrProblem(response, "Could not archive profile.", "Archive profile failed");
    }

    private static async Task<IResult> HandleDuplicateProfileAsync(HttpContext ctx, Guid profileId, CancellationToken cancellationToken)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        using var response = await GetPreferenceForwarding(ctx).DuplicateProfileAsync(profileId, cancellationToken);
        return await BffForwardingResults.JsonStreamOrProblemAsync(response, "Could not duplicate profile.", "Duplicate profile failed", cancellationToken);
    }

    private static async Task<IResult> HandleThemePreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var preferenceValidation = GetPreferenceValidation(ctx);
        var themeMode = preferenceValidation.NormalizeThemeMode(ctx.Request.Query["theme"].ToString());

        if (themeMode is null)
        {
            return Results.Problem(
                detail: preferenceValidation.ThemeModeValidationMessage,
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

        GetPreferenceCookies(ctx).PersistThemeCookie(ctx, themeMode);
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

        return Results.Ok(GetPreferenceCookies(ctx).ReadCookiePreferences(ctx));
    }

    private static async Task<IResult> HandleLanguagePreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var normalizedLang = GetPreferenceValidation(ctx).NormalizeLanguage(ctx.Request.Query["lang"].ToString());

        if (normalizedLang is null)
        {
            return Results.Problem(
                detail: "Language must be a supported culture code registered in CultureRegistry.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid language preference");
        }

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

        var preferenceCookies = GetPreferenceCookies(ctx);
        preferenceCookies.PersistLanguageCookie(ctx, normalizedLang);
        preferenceCookies.PersistAspNetCoreCultureCookie(ctx, normalizedLang);
        return Results.Ok(updated);
    }

    private static async Task<IResult> HandleDirectionPreference(HttpContext ctx, CancellationToken cancellationToken)
    {
        var direction = GetPreferenceValidation(ctx).NormalizeDirection(ctx.Request.Query["dir"].ToString());

        if (direction is null)
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

        GetPreferenceCookies(ctx).PersistDirectionCookie(ctx, direction);
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

        using var response = await GetPreferenceForwarding(ctx).GetAvailableThemesAsync(cancellationToken);
        return await BffForwardingResults.ContentOrProblemAsync(
            response,
            "Available themes could not be fetched from the API.",
            "Theme catalog unavailable",
            cancellationToken);
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

        return GetPreferenceCookies(ctx).ReadCookiePreferences(ctx);
    }

    private static async Task<UserAppearancePreferencesDto?> ReadAuthenticatedAsync(HttpContext ctx, CancellationToken cancellationToken)
    {
        using var response = await GetPreferenceForwarding(ctx).GetAppearanceAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserAppearancePreferencesDto>(cancellationToken: cancellationToken);
    }

    private static async Task<IResult?> PersistAuthenticatedAsync(
        HttpContext ctx,
        UserAppearancePreferencesDto preferences,
        string failureTitle,
        CancellationToken cancellationToken)
    {
        using var response = await GetPreferenceForwarding(ctx).PersistPreferencesAsync(preferences, cancellationToken);

        return BffForwardingResults.ProblemOrNull(response, "Authenticated preference could not be persisted.", failureTitle);
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

    private static IBffPreferenceCookieService GetPreferenceCookies(HttpContext ctx)
    {
        return ctx.RequestServices.GetRequiredService<IBffPreferenceCookieService>();
    }

    private static IBffPreferenceValidationService GetPreferenceValidation(HttpContext ctx)
    {
        return ctx.RequestServices.GetRequiredService<IBffPreferenceValidationService>();
    }

    private static IBffPreferenceForwardingService GetPreferenceForwarding(HttpContext ctx)
    {
        return ctx.RequestServices.GetRequiredService<IBffPreferenceForwardingService>();
    }
}
