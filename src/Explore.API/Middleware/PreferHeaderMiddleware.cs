namespace Explore.API.Middleware;

using Explore.API.Hateoas;

/// <summary>
/// Middleware that processes the RFC 7240 Prefer header for HATEOAS responses.
/// When client sends "Prefer: return=minimal", links are stripped from responses.
/// </summary>
/// <remarks>
/// RFC 7240 defines the Prefer header for expressing client preferences.
/// This middleware specifically handles the "return" preference:
/// - return=minimal: Strip _links and _embedded from HAL responses
/// - return=representation: Include full HAL representation (default)
///
/// The preference is stored in HttpContext.Items for resource assemblers to check.
/// </remarks>
public sealed class PreferHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PreferHeaderMiddleware> _logger;

    public PreferHeaderMiddleware(RequestDelegate next, ILogger<PreferHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Parse Prefer header
        var preferHeader = context.Request.Headers[HateoasConstants.PreferHeader].FirstOrDefault();
        var returnMinimal = false;

        if (!string.IsNullOrEmpty(preferHeader))
        {
            // Parse preferences (can be comma-separated)
            var preferences = ParsePreferHeader(preferHeader);

            if (preferences.TryGetValue("return", out var returnValue))
            {
                returnMinimal = returnValue.Equals("minimal", StringComparison.OrdinalIgnoreCase);

                _logger.LogDebug(
                    "Prefer header parsed: return={ReturnValue}, minimal={IsMinimal}",
                    returnValue,
                    returnMinimal);
            }
        }

        // Store preference in HttpContext.Items for assemblers to access
        context.Items[HateoasConstants.MinimalResponseKey] = returnMinimal;

        // Register callback to add Preference-Applied header
        if (returnMinimal)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HateoasConstants.PreferenceAppliedHeader] =
                    HateoasConstants.ReturnMinimal;
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    /// <summary>
    /// Parses the Prefer header value into key-value pairs.
    /// Handles format: "return=minimal, respond-async, wait=100"
    /// </summary>
    private static Dictionary<string, string> ParsePreferHeader(string preferHeader)
    {
        var preferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var parts = preferHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);

            if (keyValue.Length == 2)
            {
                // Key-value preference: return=minimal
                preferences[keyValue[0]] = keyValue[1];
            }
            else if (keyValue.Length == 1)
            {
                // Boolean preference: respond-async
                preferences[keyValue[0]] = "true";
            }
        }

        return preferences;
    }
}

/// <summary>
/// Extension methods for registering the PreferHeaderMiddleware.
/// </summary>
public static class PreferHeaderMiddlewareExtensions
{
    /// <summary>
    /// Adds the Prefer header middleware to the application pipeline.
    /// Should be called early in the pipeline, before endpoint execution.
    /// </summary>
    public static IApplicationBuilder UsePreferHeader(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PreferHeaderMiddleware>();
    }
}
