// ABOUTME: Maps AI assistant command failures to stable RFC 7807 API responses.
// ABOUTME: Keeps HTTP status selection in the API layer while handlers stay transport-agnostic.

using Explore.Application.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

internal static class AiAssistantProblemDetails
{
    public static ActionResult ToAiAssistantProblem(
        this ControllerBase controller,
        BaseCommandResponse<Guid> response)
    {
        var statusCode = ResolveStatusCode(response.FailureCode);
        ProblemDetails problemDetails = statusCode == StatusCodes.Status400BadRequest
            ? new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["aiAssistant"] = (response.Errors is { Count: > 0 }
                    ? response.Errors
                    : [response.Message ?? "AI assistant request failed."]).ToArray()
            })
            : new ProblemDetails();

        problemDetails.Status = statusCode;
        problemDetails.Title = ResolveTitle(statusCode, response.FailureCode);
        problemDetails.Type = response.FailureCode == FailureCodes.QuotaExceeded
            ? "/problems/quota_exceeded"
            : $"https://tools.ietf.org/html/rfc9110#section-{ResolveRfcSection(statusCode)}";
        problemDetails.Detail = response.Message ?? "AI assistant request failed.";
        problemDetails.Instance = controller.HttpContext.Request.Path;

        if (!string.IsNullOrWhiteSpace(response.FailureCode))
        {
            problemDetails.Extensions["code"] = response.FailureCode;
        }

        if (response.QuotaExceeded is not null)
        {
            problemDetails.Extensions["quota"] = response.QuotaExceeded;
        }

        problemDetails.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["correlationId"] = controller.HttpContext.Items["CorrelationId"] as string;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static int ResolveStatusCode(string? failureCode)
        => failureCode switch
        {
            "unauthenticated" => StatusCodes.Status401Unauthorized,
            "conversation_not_found" => StatusCodes.Status404NotFound,
            "run_not_found" => StatusCodes.Status404NotFound,
            "proposed_action_not_found" => StatusCodes.Status404NotFound,
            "conversation_not_active" => StatusCodes.Status409Conflict,
            "run_not_cancellable" => StatusCodes.Status409Conflict,
            "proposed_action_rejected" => StatusCodes.Status409Conflict,
            "proposed_action_failed" => StatusCodes.Status409Conflict,
            "invalid_proposed_action_state" => StatusCodes.Status409Conflict,
            "idempotency_key_conflict" => StatusCodes.Status409Conflict,
            "idempotency_replay_failed" => StatusCodes.Status409Conflict,
            FailureCodes.QuotaExceeded => StatusCodes.Status422UnprocessableEntity,
            "provider_not_ready" => StatusCodes.Status503ServiceUnavailable,
            "provider_timeout" => StatusCodes.Status503ServiceUnavailable,
            "provider_unreachable" => StatusCodes.Status503ServiceUnavailable,
            "http_429" => StatusCodes.Status429TooManyRequests,
            "disabled" => StatusCodes.Status403Forbidden,
            "provider_not_configured" => StatusCodes.Status403Forbidden,
            "provider_unsupported" => StatusCodes.Status403Forbidden,
            "endpoint_not_configured" => StatusCodes.Status403Forbidden,
            "api_key_not_configured" => StatusCodes.Status403Forbidden,
            "model_not_configured" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

    private static string ResolveTitle(int statusCode, string? failureCode)
        => statusCode switch
        {
            StatusCodes.Status401Unauthorized => "AI assistant authentication required",
            StatusCodes.Status403Forbidden => "AI assistant unavailable",
            StatusCodes.Status404NotFound => "AI conversation not found",
            StatusCodes.Status409Conflict => "AI conversation conflict",
            StatusCodes.Status422UnprocessableEntity => "AI assistant quota exceeded",
            StatusCodes.Status429TooManyRequests => "AI provider rate limited",
            StatusCodes.Status503ServiceUnavailable => "AI provider unavailable",
            _ when failureCode == "validation_failed" => "AI assistant validation failed",
            _ => "AI assistant request failed"
        };

    private static string ResolveRfcSection(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status401Unauthorized => "15.5.2",
            StatusCodes.Status403Forbidden => "15.5.4",
            StatusCodes.Status404NotFound => "15.5.5",
            StatusCodes.Status409Conflict => "15.5.10",
            StatusCodes.Status422UnprocessableEntity => "15.5.21",
            StatusCodes.Status429TooManyRequests => "15.5.30",
            StatusCodes.Status503ServiceUnavailable => "15.6.4",
            _ => "15.5.1"
        };
}
