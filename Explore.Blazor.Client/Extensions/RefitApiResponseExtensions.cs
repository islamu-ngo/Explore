using Explore.Blazor.Client.Exceptions;
using Explore.Blazor.Client.Services.Http;
using Refit;

namespace Explore.Blazor.Client.Extensions;

public static class RefitApiResponseExtensions
{
    /// <summary>
    /// Converts a Refit IApiResponse to the application's ApiResult type.
    /// Safely handles API ProblemDetails extraction on failure.
    /// </summary>
    public static ApiResult<T> ToApiResult<T>(this IApiResponse<T> response, string serviceName = "HTTP API")
    {
        if (response.IsSuccessStatusCode && response.Content is not null)
        {
            return ApiResult<T>.Success(response.Content);
        }

        if (response.Error is not null)
        {
            var problemException = ApiProblemException.FromRefitException(response.Error, serviceName);
            return ApiResult<T>.Failure(problemException);
        }

        // Fallback for non-success without an ApiException (rare in Refit unless configured differently)
        var fallbackProblem = new ApiProblemException(
            response.StatusCode,
            response.ReasonPhrase ?? "API Request Failed",
            null,
            null,
            serviceName
        );

        return ApiResult<T>.Failure(fallbackProblem);
    }

    /// <summary>
    /// Converts a Refit IApiResponse (no content) to the application's ApiResult type.
    /// </summary>
    public static ApiResult ToApiResult(this IApiResponse response, string serviceName = "HTTP API")
    {
        if (response.IsSuccessStatusCode)
        {
            return ApiResult.Success();
        }

        if (response.Error is not null)
        {
            var problemException = ApiProblemException.FromRefitException(response.Error, serviceName);
            return ApiResult.Failure(problemException);
        }

        var fallbackProblem = new ApiProblemException(
            response.StatusCode,
            response.ReasonPhrase ?? "API Request Failed",
            null,
            null,
            serviceName
        );

        return ApiResult.Failure(fallbackProblem);
    }
}
