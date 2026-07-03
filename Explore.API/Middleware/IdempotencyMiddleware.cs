// ABOUTME: Middleware that implements Idempotency-Key header support for write operations (POST/PUT/PATCH/DELETE).
// ABOUTME: Caches responses by (Key, TenantId) and replays them on duplicate requests within a 24-hour window.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.AspNetCore.Mvc;
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
    private const int MaxStoredResponseBodyBytes = 1024 * 1024;
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
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        RecyclableMemoryStreamManager streamManager,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _streamManager = streamManager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip non-write methods entirely
        if (!WriteMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // AI message sends persist run-level idempotency inside the Application handler.
        // Let that domain-specific record own replay/conflict semantics instead of
        // caching the HTTP response with a different request fingerprint.
        if (IsApplicationManagedAiMessageSend(context.Request)
            || IsShortLivedWebhookPortalAccess(context.Request))
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
            var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Idempotency-Key must be at most 128 characters and contain no whitespace.",
                    Instance = context.Request.Path
                }
            });
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        var repository = context.RequestServices.GetRequiredService<IIdempotencyRepository>();
        var tenantId = tenantContext.TenantId;
        var requestIdentity = await IdempotencyRequestIdentityFactory.CreateAsync(
            context,
            _streamManager,
            context.RequestAborted);

        // Check for existing cached response
        var existing = await repository.FindAsync(key, tenantId, context.RequestAborted);
        if (existing is not null)
        {
            if (!MatchesRequestIdentity(existing, requestIdentity))
            {
                await WriteKeyReuseConflictAsync(context);
                return;
            }

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

        try
        {
            await _next(context);
        }
        catch
        {
            context.Response.Body = originalBodyStream;
            throw;
        }

        context.Response.Body = originalBodyStream;

        // Read captured response
        bufferStream.Position = 0;
        string? responseBody = null;
        if (bufferStream.Length > 0)
        {
            using var reader = new StreamReader(bufferStream, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync(context.RequestAborted);
        }

        if (ShouldPersistResponse(context.Response, bufferStream.Length))
        {
            // Persist the idempotency record
            var now = DateTime.UtcNow;
            var record = new IdempotencyRecord
            {
                Id = Guid.CreateVersion7(),
                Key = key,
                TenantId = tenantId,
                UserId = requestIdentity.UserId,
                RequestMethod = requestIdentity.Method,
                RequestTarget = requestIdentity.RequestTarget,
                RequestContentType = requestIdentity.ContentType,
                RequestBodyHash = requestIdentity.BodyHash,
                PrincipalFingerprint = requestIdentity.PrincipalFingerprint,
                StatusCode = context.Response.StatusCode,
                ResponseBody = responseBody,
                ContentType = context.Response.ContentType,
                CreatedAt = now,
                ExpiresAt = now.Add(DefaultExpiration)
            };

            try
            {
                await repository.SaveAsync(record, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Duplicate-key races are non-fatal for the original request, but must still be observable.
                _logger.LogWarning(
                    ex,
                    "Unable to persist idempotency record for tenant {TenantId}, key hash {KeyHash}, and path {Path}.",
                    tenantId,
                    key.GetHashCode(StringComparison.Ordinal),
                    context.Request.Path);
            }
        }

        // Write the captured response to the original stream
        bufferStream.Position = 0;
        await bufferStream.CopyToAsync(originalBodyStream, context.RequestAborted);
    }

    private static bool MatchesRequestIdentity(
        IdempotencyRecord record,
        IdempotencyRequestIdentity requestIdentity)
    {
        return string.Equals(record.RequestMethod, requestIdentity.Method, StringComparison.Ordinal)
               && string.Equals(record.RequestTarget, requestIdentity.RequestTarget, StringComparison.Ordinal)
               && string.Equals(record.RequestContentType, requestIdentity.ContentType, StringComparison.Ordinal)
               && string.Equals(record.RequestBodyHash, requestIdentity.BodyHash, StringComparison.Ordinal)
               && string.Equals(record.PrincipalFingerprint, requestIdentity.PrincipalFingerprint, StringComparison.Ordinal);
    }

    private static bool IsApplicationManagedAiMessageSend(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            && request.Path.StartsWithSegments("/api/ai/assistant/conversations", StringComparison.OrdinalIgnoreCase)
            && request.Path.Value?.EndsWith("/messages", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsShortLivedWebhookPortalAccess(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            && request.Path.Equals("/api/webhooks/svix/app-portal", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteKeyReuseConflictAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = "Idempotency-Key has already been used with a different request.",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["code"] = "idempotency_key_reuse";

        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });
    }

    private static bool ShouldPersistResponse(HttpResponse response, long responseBodyLength)
    {
        if (response.StatusCode < StatusCodes.Status200OK || response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return false;
        }

        if (response.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status415UnsupportedMediaType)
        {
            return false;
        }

        if (responseBodyLength > MaxStoredResponseBodyBytes)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(response.ContentType)
            || response.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
            || response.ContentType.StartsWith("application/problem+json", StringComparison.OrdinalIgnoreCase);
    }
}
