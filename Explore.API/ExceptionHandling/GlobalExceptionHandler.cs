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

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.com/{statusCode}",
                Instance = httpContext.Request.Path
            }
        });
    }
}
