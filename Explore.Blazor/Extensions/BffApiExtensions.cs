using Microsoft.AspNetCore.Http.HttpResults;
using Explore.Blazor.Client.Clients;
using System.Text.Json;

namespace Explore.Blazor.Extensions;

/// <summary>
/// Extension methods for mapping NSwag API client responses to BFF endpoint results.
/// Provides centralized error handling, logging, and response transformation.
/// </summary>
public static class BffApiExtensions
{
    /// <summary>
    /// Executes an async API call and maps the result to an IResult with proper error handling.
    /// </summary>
    public static async Task<IResult> ExecuteAsync<T>(
        Func<Task<T>> apiCall,
        ILogger logger,
        string operationName,
        HttpContext? ctx = null)
    {
        try
        {
            logger.LogInformation("BFF: Executing {Operation}", operationName);
            var result = await apiCall();

            logger.LogInformation("BFF: {Operation} completed successfully", operationName);
            return Results.Ok(result);
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "BFF: {Operation} failed with status {StatusCode}", operationName, ex.StatusCode);

            return ex.StatusCode switch
            {
                401 => Results.Unauthorized(),
                403 => Results.Problem("Access denied", statusCode: 403),
                404 => Results.NotFound(),
                _ => Results.Problem(
                    detail: ex.Response ?? ex.Message,
                    statusCode: ex.StatusCode
                )
            };
        }
        catch (Exception ex)
        {
            // Inspect exception chain to determine if this was a token/refresh failure
            if (IsTokenRefreshFailure(ex))
            {
                logger.LogWarning(ex, "BFF: Token refresh or token retrieval failed during {Operation} - returning 401", operationName);
                return Results.Unauthorized();
            }

            logger.LogError(ex, "BFF: Unexpected error in {Operation}", operationName);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    /// <summary>
    /// Executes an async API call that returns void/Task and maps to IResult.
    /// </summary>
    public static async Task<IResult> ExecuteVoidAsync(
        Func<Task> apiCall,
        ILogger logger,
        string operationName,
        HttpContext? ctx = null)
    {
        try
        {
            logger.LogInformation("BFF: Executing {Operation}", operationName);
            await apiCall();

            logger.LogInformation("BFF: {Operation} completed successfully", operationName);
            return Results.NoContent();
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "BFF: {Operation} failed with status {StatusCode}", operationName, ex.StatusCode);

            return ex.StatusCode switch
            {
                401 => Results.Unauthorized(),
                403 => Results.Problem("Access denied", statusCode: 403),
                404 => Results.NotFound(),
                _ => Results.Problem(
                    detail: ex.Response ?? ex.Message,
                    statusCode: ex.StatusCode
                )
            };
        }
        catch (Exception ex)
        {
            if (IsTokenRefreshFailure(ex))
            {
                logger.LogWarning(ex, "BFF: Token refresh or token retrieval failed during {Operation} - returning 401", operationName);
                return Results.Unauthorized();
            }

            logger.LogError(ex, "BFF: Unexpected error in {Operation}", operationName);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    /// <summary>
    /// Creates a scoped IEventApiClient for the current request.
    /// Required because IEventApiClient is scoped and endpoints are singletons.
    /// </summary>
    public static IEventApiClient GetApiClient(this HttpContext context) =>
        context.RequestServices.GetRequiredService<IEventApiClient>();

    private static bool IsTokenRefreshFailure(Exception ex)
    {
        while (ex != null)
        {
            var msg = ex.Message ?? string.Empty;

            // Common indicators of token refresh or retrieval problems
            if (msg.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("invalid_token", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("refresh", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("no tokens", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("expired", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }
}
