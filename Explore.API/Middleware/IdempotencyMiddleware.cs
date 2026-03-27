// ABOUTME: Middleware that implements Idempotency-Key header support for write operations (POST/PUT/PATCH/DELETE).
// ABOUTME: Caches responses by (Key, TenantId) and replays them on duplicate requests within a 24-hour window.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.IO;

namespace Explore.API.Middleware;

/// <summary>
/// Processes the <c>Idempotency-Key</c> header on write operations (POST, PUT, PATCH, DELETE).
/// When a key is provided, the middleware checks for an existing cached response and replays it.
/// When no cached response exists, the response is captured and persisted for future replay.
/// GET and HEAD requests are always passed through without processing.
/// The header is opt-in — requests without the header are processed normally.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string ReplayHeader = "X-Idempotency-Replay";
    private const int MaxKeyLength = 128;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly RequestDelegate _next;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public IdempotencyMiddleware(RequestDelegate next, RecyclableMemoryStreamManager streamManager)
    {
        _next = next;
        _streamManager = streamManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip non-write methods entirely
        if (!WriteMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Opt-in: skip if no Idempotency-Key header
        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var keyValues)
            || string.IsNullOrEmpty(keyValues.FirstOrDefault()))
        {
            await _next(context);
            return;
        }

        var key = keyValues.FirstOrDefault()!;

        // Validate key: max length, no whitespace
        if (key.Length > MaxKeyLength || key.AsSpan().ContainsAny(" \t\r\n"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"Idempotency-Key must be at most 128 characters and contain no whitespace."}""");
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        var repository = context.RequestServices.GetRequiredService<IIdempotencyRepository>();
        var tenantId = tenantContext.TenantId;

        // Check for existing cached response
        var existing = await repository.FindAsync(key, tenantId, context.RequestAborted);
        if (existing is not null)
        {
            context.Response.StatusCode = existing.StatusCode;
            context.Response.Headers[ReplayHeader] = "true";

            if (!string.IsNullOrEmpty(existing.ContentType))
            {
                context.Response.ContentType = existing.ContentType;
            }

            if (!string.IsNullOrEmpty(existing.ResponseBody))
            {
                await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
            }

            return;
        }

        // Capture the response body
        var originalBodyStream = context.Response.Body;
        using var bufferStream = _streamManager.GetStream("idempotency-middleware");
        context.Response.Body = bufferStream;

        await _next(context);

        // Read captured response
        bufferStream.Position = 0;
        string? responseBody = null;
        if (bufferStream.Length > 0)
        {
            using var reader = new StreamReader(bufferStream, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync(context.RequestAborted);
        }

        // Persist the idempotency record
        var now = DateTime.UtcNow;
        var record = new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            TenantId = tenantId,
            UserId = context.User?.FindFirst("sub")?.Value
                     ?? context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                     ?? context.User?.FindFirst("sid")?.Value,
            StatusCode = context.Response.StatusCode,
            ResponseBody = responseBody,
            ContentType = context.Response.ContentType,
            CreatedAt = now,
            ExpiresAt = now.Add(DefaultExpiration)
        };

        try
        {
            await repository.SaveAsync(record, CancellationToken.None);
        }
        catch (Exception)
        {
            // Duplicate key race condition — another request with the same key was persisted concurrently.
            // The response has already been written to the buffer, so we proceed normally.
        }

        // Write the captured response to the original stream
        bufferStream.Position = 0;
        await bufferStream.CopyToAsync(originalBodyStream, context.RequestAborted);
        context.Response.Body = originalBodyStream;
    }
}
