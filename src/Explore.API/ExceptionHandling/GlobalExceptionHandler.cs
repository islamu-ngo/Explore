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
            [StatusCodes.Status400BadRequest] = ApiProblemTypes.BadRequest,
            [StatusCodes.Status401Unauthorized] = ApiProblemTypes.Unauthorized,
            [StatusCodes.Status403Forbidden] = ApiProblemTypes.Forbidden,
            [StatusCodes.Status404NotFound] = ApiProblemTypes.NotFound,
            [StatusCodes.Status409Conflict] = ApiProblemTypes.Conflict,
            [StatusCodes.Status422UnprocessableEntity] = ApiProblemTypes.UnprocessableEntity,
            [StatusCodes.Status429TooManyRequests] = ApiProblemTypes.TooManyRequests,
            [StatusCodes.Status500InternalServerError] = ApiProblemTypes.InternalServerError,
        };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, code) = exception switch
        {
            BadRequestException badRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                badRequestException.Message,
                ApiProblemCodes.ValidationFailed),
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFoundException.Message,
                ApiProblemCodes.ResourceNotFound),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                ApiProblemCodes.AuthenticationRequired),
            AuthorizationException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this action.",
                ApiProblemCodes.Forbidden),
            ConcurrencyConflictException concurrencyConflictException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                concurrencyConflictException.Message,
                ApiProblemCodes.ConcurrencyConflict),
            QuotaExceededException quotaExceededException => (
                QuotaProblemDetailsFactory.StatusCode,
                QuotaProblemDetailsFactory.Title,
                quotaExceededException.Message,
                "quota_exceeded"),
            AdmissionRecoveryRateLimitExceededException => (
                StatusCodes.Status429TooManyRequests,
                "Too many recovery requests",
                "The admission recovery request budget has been exhausted.",
                ApiProblemCodes.RateLimited),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                hostEnvironment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                ApiProblemCodes.UnexpectedError)
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
        if (exception is AdmissionRecoveryRateLimitExceededException recoveryRateLimit)
        {
            httpContext.Response.Headers.RetryAfter =
                recoveryRateLimit.RetryAfterSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        var typeUri = exception is QuotaExceededException
            ? QuotaProblemDetailsFactory.Type
            : exception is ConcurrencyConflictException concurrencyConflictExceptionForType
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

        if (exception is not ConcurrencyConflictException and not QuotaExceededException)
        {
            problemDetails.Extensions["code"] = code;
        }

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

        if (exception is QuotaExceededException quotaExceeded)
        {
            QuotaProblemDetailsFactory.AddExtensions(problemDetails, quotaExceeded.Details);
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
