// ABOUTME: Bounds and rewinds private request bytes after early timeout and rate admission, before crypto or storage.
// ABOUTME: Guards credential ambiguity and dependency responses without logging assertion, body or provider diagnostics.

using Microsoft.AspNetCore.Http.Features;

namespace Explore.API.Authentication;

public sealed class AtprotoTransientRequestBoundary(RequestDelegate next)
{
    public static async Task GuardAsync(HttpContext context, Func<Task> next)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        if (!HasOnlyTransientCredential(context.Request)
            || AtprotoTransientAuthenticationDefaults.Operation(context.Request) is null)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized);
            return;
        }
        try
        {
            await next();
        }
        catch (Exception) when (!context.RequestAborted.IsCancellationRequested && !context.Response.HasStarted)
        {
            context.Response.Clear();
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        const int limit = AtprotoTransientAuthenticationDefaults.MaximumBodyBytes;
        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false }) sizeFeature.MaxRequestBodySize = limit;
        if (context.Request.ContentLength > limit)
        {
            await WriteProblemAsync(context, StatusCodes.Status413PayloadTooLarge);
            return;
        }
        context.Request.EnableBuffering(bufferThreshold: limit + 1, bufferLimit: limit + 1);
        var buffer = new byte[limit + 1];
        int count = 0;
        try
        {
            while (count < buffer.Length)
            {
                int read = await context.Request.Body.ReadAsync(buffer.AsMemory(count), context.RequestAborted);
                if (read == 0) break;
                count += read;
            }
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            await WriteProblemAsync(context, StatusCodes.Status413PayloadTooLarge);
            return;
        }
        if (count > limit)
        {
            await WriteProblemAsync(context, StatusCodes.Status413PayloadTooLarge);
            return;
        }
        context.Request.Body.Position = 0;
        context.Items[AtprotoTransientAuthenticationDefaults.BufferedBodyKey] = buffer[..count];
        await next(context);
    }

    internal static bool HasOnlyTransientCredential(HttpRequest request)
    {
        var values = request.Headers[AtprotoTransientAuthenticationDefaults.HeaderName];
        return values.Count == 1 && values[0] is { Length: > 0 and <= AtprotoTransientAuthenticationDefaults.MaximumAssertionBytes }
            && !values[0]!.Contains(',', StringComparison.Ordinal)
            && !new[] { "Authorization", "Proxy-Authorization", "X-API-Key", "X-Control-Plane-Key", "X-Setup-Secret",
                AtprotoJwtOptions.BootstrapHeaderName, AtprotoJwtOptions.SessionBridgeHeaderName, "X-Test-Auth" }
                .Any(request.Headers.ContainsKey);
    }

    internal static Task WriteProblemAsync(HttpContext context, int status)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Problem(statusCode: status, title: status switch
        {
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status413PayloadTooLarge => "Content Too Large",
            StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
            _ => "Service Unavailable"
        }).ExecuteAsync(context);
    }
}
