// ABOUTME: Preference BFF endpoints: theme, language, and current-user info.
// ABOUTME: Persists user preferences via cookies and returns safe claim subsets.

namespace Explore.Blazor.Extensions;

public static class BffPreferenceEndpoints
{
    /// <summary>
    /// Maps preference endpoints: POST /bff/theme, POST /bff/language, GET /bff/me.
    /// </summary>
    public static WebApplication MapPreferenceEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/theme", HandleThemePreference)
            .ExcludeFromDescription();

        app.MapPost("/bff/language", HandleLanguagePreference)
            .ExcludeFromDescription();

        app.MapGet("/bff/me", HandleGetCurrentUser);

        return app;
    }

    private static IResult HandleThemePreference(HttpContext ctx)
    {
        var theme = ctx.Request.Query["theme"].ToString();
        if (theme is "dark" or "light")
        {
            var isDev = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            ctx.Response.Cookies.Append("theme", theme, new CookieOptions
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
            detail: "Theme must be 'dark' or 'light'.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid theme preference");
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
}
