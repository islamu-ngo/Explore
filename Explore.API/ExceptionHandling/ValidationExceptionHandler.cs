// ABOUTME: Handles validation-related exceptions and returns RFC 7807 responses.
// ABOUTME: Emits safe validation payloads without stack traces or internal details.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using ApplicationValidationException = Explore.Application.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace Explore.API.ExceptionHandling;

internal sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errors = exception switch
        {
            ApplicationValidationException appValidationException => new Dictionary<string, string[]>
            {
                ["validation"] = appValidationException.Errors.ToArray()
            },
            FluentValidationException fluentValidationException => fluentValidationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key.ToLowerInvariant(),
                    group => group.Select(error => error.ErrorMessage).ToArray()),
            _ => null
        };

        if (errors is null)
        {
            return false;
        }

        logger.LogWarning(exception, "Validation failure for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errors"] = errors;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
