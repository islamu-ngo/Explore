// ABOUTME: Defines canonical RFC 9110 ProblemDetails type URIs for API responses.
// ABOUTME: Prevents controllers and mappers from duplicating status-code URI strings.

namespace Explore.API.ExceptionHandling;

internal static class ApiProblemTypes
{
    public const string BadRequest = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
    public const string NotFound = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
}
