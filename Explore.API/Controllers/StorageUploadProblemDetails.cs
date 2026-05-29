// ABOUTME: Controller helper for local-first storage upload command failures.
// ABOUTME: Maps Application failure codes to stable RFC 7807 responses without moving HTTP concerns into handlers.

using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

internal static class StorageUploadProblemDetails
{
    public static ActionResult ToStorageUploadProblem(
        this ControllerBase controller,
        BaseCommandResponse<StorageUploadSessionDto> response)
    {
        var statusCode = response.FailureCode switch
        {
            FailureCodes.StorageUploadTooLarge => StatusCodes.Status413PayloadTooLarge,
            FailureCodes.QuotaExceeded => StatusCodes.Status422UnprocessableEntity,
            FailureCodes.StorageUploadSessionNotFound => StatusCodes.Status404NotFound,
            FailureCodes.StorageUploadSessionFinalized => StatusCodes.Status409Conflict,
            FailureCodes.StorageUploadSessionExpired => StatusCodes.Status409Conflict,
            FailureCodes.StorageUploadSessionInvalidState => StatusCodes.Status409Conflict,
            FailureCodes.StorageUploadSizeMismatch => StatusCodes.Status400BadRequest,
            FailureCodes.StorageUploadContentTypeMismatch => StatusCodes.Status400BadRequest,
            FailureCodes.StorageUploadWriteFailed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };

        ProblemDetails problemDetails = statusCode == StatusCodes.Status400BadRequest
            ? new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["storageUpload"] = (response.Errors is { Count: > 0 }
                    ? response.Errors
                    : [response.Message ?? "Storage upload command failed."]).ToArray()
            })
            : new ProblemDetails();

        problemDetails.Status = statusCode;
        problemDetails.Title = statusCode switch
        {
            StatusCodes.Status404NotFound => "Storage upload session not found",
            StatusCodes.Status409Conflict => "Storage upload session conflict",
            StatusCodes.Status413PayloadTooLarge => "Storage upload is too large",
            StatusCodes.Status422UnprocessableEntity => "Storage quota exceeded",
            StatusCodes.Status503ServiceUnavailable => "Storage provider unavailable",
            _ => "Storage upload validation failed"
        };
        problemDetails.Type = response.FailureCode == FailureCodes.QuotaExceeded
            ? "/problems/quota_exceeded"
            : $"https://tools.ietf.org/html/rfc9110#section-{ResolveRfcSection(statusCode)}";
        problemDetails.Detail = response.Message ?? "Storage upload command failed.";
        problemDetails.Instance = controller.HttpContext.Request.Path;

        if (!string.IsNullOrWhiteSpace(response.FailureCode))
        {
            problemDetails.Extensions["code"] = response.FailureCode;
        }

        problemDetails.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["correlationId"] = controller.HttpContext.Items["CorrelationId"] as string;

        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/problem+json",
            Content = JsonSerializer.Serialize(
                problemDetails,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
        };
    }

    private static string ResolveRfcSection(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status404NotFound => "15.5.5",
            StatusCodes.Status409Conflict => "15.5.10",
            StatusCodes.Status413PayloadTooLarge => "15.5.14",
            StatusCodes.Status422UnprocessableEntity => "15.5.21",
            StatusCodes.Status503ServiceUnavailable => "15.6.4",
            _ => "15.5.1"
        };
}
