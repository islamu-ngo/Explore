// ABOUTME: Creates RFC 7807 ProblemDetails payloads for API-owned response mapping.
// ABOUTME: Centralizes standard trace, timestamp, correlation, and content-type behavior.

using Microsoft.AspNetCore.Mvc;

namespace Explore.API.ExceptionHandling;

internal static class ApiProblemFactory
{
    private const string ProblemJsonContentType = "application/problem+json";

    public static ValidationProblemDetails CreateValidationProblem(
        HttpContext httpContext,
        ApiValidationProblemDescriptor descriptor,
        IReadOnlyCollection<string> errors,
        string detail,
        string code)
    {
        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [descriptor.ErrorKey] = errors.ToArray()
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = descriptor.Title,
            Type = ApiProblemTypes.BadRequest,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = string.IsNullOrWhiteSpace(code)
            ? ApiProblemCodes.ValidationFailed
            : code;
        AddStandardExtensions(httpContext, problemDetails);

        return problemDetails;
    }

    public static ProblemDetails CreateNotFoundProblem(
        HttpContext httpContext,
        ApiNotFoundProblemDescriptor descriptor)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = descriptor.Title,
            Type = ApiProblemTypes.NotFound,
            Detail = descriptor.Detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = descriptor.Code;
        AddStandardExtensions(httpContext, problemDetails);

        return problemDetails;
    }

    public static ObjectResult ToProblemResult(ProblemDetails problemDetails)
    {
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status,
            ContentTypes = { ProblemJsonContentType }
        };
    }

    private static void AddStandardExtensions(HttpContext httpContext, ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (httpContext.Items["CorrelationId"] is string correlationId)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }
    }
}
