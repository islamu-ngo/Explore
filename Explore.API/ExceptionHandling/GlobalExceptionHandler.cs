// ABOUTME: Handles non-validation exceptions and produces safe RFC 7807 responses.
// ABOUTME: Maps known application exceptions to stable HTTP status codes.

using Explore.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.ExceptionHandling;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment hostEnvironment) : IExceptionHandler
{
    /// <summary>
    /// Maps HTTP status codes to stable IANA RFC 9110 type URIs for ProblemDetails.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, string> ProblemTypeUris =
        new Dictionary<int, string>
        {
            [StatusCodes.Status400BadRequest] = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            [StatusCodes.Status401Unauthorized] = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            [StatusCodes.Status403Forbidden] = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            [StatusCodes.Status404NotFound] = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            [StatusCodes.Status409Conflict] = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            [StatusCodes.Status422UnprocessableEntity] = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
            [StatusCodes.Status500InternalServerError] = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            BadRequestException badRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                badRequestException.Message),
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFoundException.Message),
            AuthorizationException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this action."),
            ConcurrencyConflictException concurrencyConflictException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                concurrencyConflictException.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                hostEnvironment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Handled application exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        var typeUri = exception is ConcurrencyConflictException concurrencyConflictExceptionForType
            ? concurrencyConflictExceptionForType.Code switch
            {
                ConcurrencyConflictException.StaleSyncBase => "/problems/stale_sync_base",
                ConcurrencyConflictException.ConcurrentUpdate => "/problems/concurrent_update",
                _ => ProblemTypeUris.TryGetValue(statusCode, out var concurrencyUri)
                    ? concurrencyUri
                    : $"https://tools.ietf.org/html/rfc9110#status.{statusCode}"
            }
            : ProblemTypeUris.TryGetValue(statusCode, out var uri)
                ? uri
                : $"https://tools.ietf.org/html/rfc9110#status.{statusCode}";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = typeUri,
            Instance = httpContext.Request.Path
        };

        if (exception is ConcurrencyConflictException concurrencyConflict)
        {
            problemDetails.Extensions["code"] = concurrencyConflict.Code;
            if (concurrencyConflict.EntityType is not null)
            {
                problemDetails.Extensions["entityType"] = concurrencyConflict.EntityType;
            }
            if (concurrencyConflict.EntityId is not null)
            {
                problemDetails.Extensions["entityId"] = concurrencyConflict.EntityId;
            }
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
