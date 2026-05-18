// ABOUTME: Controller helper for program/session command validation failures.
// ABOUTME: Converts existing command responses into RFC 7807 validation payloads without changing handlers.

using Explore.Application.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Explore.API.Controllers;

internal static class ProgramValidationProblemDetails
{
    public static ActionResult ToProgramValidationProblem<TKey>(
        this ControllerBase controller,
        BaseCommandResponse<TKey> response,
        string fallbackMessage)
    {
        var errors = response.Errors is { Count: > 0 }
            ? response.Errors
            : [response.Message ?? fallbackMessage];

        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["program"] = errors.ToArray()
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Program validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = response.Message ?? fallbackMessage,
            Instance = controller.HttpContext.Request.Path
        };

        if (!string.IsNullOrWhiteSpace(response.FailureCode))
        {
            problemDetails.Extensions["code"] = response.FailureCode;
        }

        return new ContentResult
        {
            StatusCode = StatusCodes.Status400BadRequest,
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
}
