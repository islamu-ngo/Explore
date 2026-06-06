// ABOUTME: Defines stable machine-readable API ProblemDetails codes.
// ABOUTME: Keeps fallback error codes centralized across controller and exception mappings.

namespace Explore.API.ExceptionHandling;

internal static class ApiProblemCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string ResourceNotFound = "resource_not_found";
}
