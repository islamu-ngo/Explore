// ABOUTME: Computes ETag headers for GET responses and handles If-None-Match conditional requests.
// ABOUTME: Returns 304 Not Modified when the client already has the current representation.

using System.Security.Cryptography;

namespace Explore.API.Middleware;

/// <summary>
/// Handles ETag generation and conditional request processing for GET endpoints.
/// Computes a weak ETag from the response body hash and returns 304 Not Modified
/// when the client sends a matching If-None-Match header.
/// </summary>
public sealed class ETagMiddleware
{
    private readonly RequestDelegate _next;

    public ETagMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var bufferStream = new MemoryStream();
        context.Response.Body = bufferStream;

        await _next(context);

        // Only process successful JSON responses
        if (context.Response.StatusCode is not (>= 200 and < 300))
        {
            bufferStream.Position = 0;
            await bufferStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
            return;
        }

        var contentType = context.Response.ContentType;
        if (contentType is null ||
            (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
             !contentType.Contains("application/hal+json", StringComparison.OrdinalIgnoreCase)))
        {
            bufferStream.Position = 0;
            await bufferStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
            return;
        }

        bufferStream.Position = 0;
        var hash = SHA256.HashData(bufferStream.ToArray());
        var etag = $"W/\"{Convert.ToBase64String(hash[..8])}\"";

        context.Response.Headers.ETag = etag;

        var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etag)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            context.Response.Body = originalBodyStream;
            context.Response.ContentLength = 0;
            return;
        }

        bufferStream.Position = 0;
        await bufferStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }
}

public static class ETagMiddlewareExtensions
{
    public static IApplicationBuilder UseETag(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ETagMiddleware>();
    }
}
