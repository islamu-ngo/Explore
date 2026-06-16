// ABOUTME: Maps Application command response failures to API-owned RFC 7807 results.
// ABOUTME: Keeps controllers thin while avoiding HTTP concerns inside Application handlers.

using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.ExceptionHandling;

internal static class CommandResponseResultMapper
{
    private static readonly ApiValidationProblemDescriptor StorageUploadValidationProblem = new(
        "storageUpload",
        "Storage upload validation failed",
        "Storage upload command failed.");

    private static readonly ApiValidationProblemDescriptor EmailDispatchValidationProblem = new(
        "emailDispatch",
        "Email dispatch validation failed",
        "Email dispatch command failed.");

    private static readonly ApiValidationProblemDescriptor AiAssistantValidationProblem = new(
        "aiAssistant",
        "AI assistant request failed",
        "AI assistant request failed.");

    public static ActionResult ToCommandValidationProblem<TKey>(
        this ControllerBase controller,
        BaseCommandResponse<TKey> response,
        ApiValidationProblemDescriptor descriptor)
    {
        var errors = response.Errors is { Count: > 0 }
            ? response.Errors.ToArray()
            : [response.Message ?? descriptor.FallbackDetail];
        var detail = response.Message ?? descriptor.FallbackDetail;
        var code = string.IsNullOrWhiteSpace(response.FailureCode)
            ? ApiProblemCodes.ValidationFailed
            : response.FailureCode;

        var problemDetails = ApiProblemFactory.CreateValidationProblem(
            controller.HttpContext,
            descriptor,
            errors,
            detail,
            code);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToValidationProblem(
        this ControllerBase controller,
        ApiValidationProblemDescriptor descriptor,
        string detail,
        string code = ApiProblemCodes.ValidationFailed)
    {
        var problemDetails = ApiProblemFactory.CreateValidationProblem(
            controller.HttpContext,
            descriptor,
            [detail],
            detail,
            code);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }


    public static ActionResult ToAuthenticationRequiredProblem(
        this ControllerBase controller,
        string title = "User ID not found in token",
        string detail = "The authenticated principal does not include a supported user identifier claim.")
    {
        var problemDetails = ApiProblemFactory.CreateAuthenticationRequiredProblem(
            controller.HttpContext,
            title,
            detail);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToForbiddenProblem(
        this ControllerBase controller,
        string title = "Forbidden",
        string detail = "The authenticated principal is not authorized to perform this operation.")
    {
        var problemDetails = ApiProblemFactory.CreateForbiddenProblem(
            controller.HttpContext,
            title,
            detail);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToCommandConflictProblem<TKey>(
        this ControllerBase controller,
        BaseCommandResponse<TKey> response,
        string title,
        string fallbackDetail)
    {
        var problemDetails = ApiProblemFactory.CreateConflictProblem(
            controller.HttpContext,
            title,
            response.Message ?? fallbackDetail,
            string.IsNullOrWhiteSpace(response.FailureCode)
                ? ApiProblemCodes.ResourceConflict
                : response.FailureCode);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToNotFoundProblem(
        this ControllerBase controller,
        ApiNotFoundProblemDescriptor descriptor)
    {
        var problemDetails = ApiProblemFactory.CreateNotFoundProblem(controller.HttpContext, descriptor);
        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToGoneProblem(
        this ControllerBase controller,
        string title,
        string detail,
        string code = ApiProblemCodes.ResourceConflict)
    {
        var problemDetails = ApiProblemFactory.CreateGoneProblem(
            controller.HttpContext,
            title,
            detail,
            code);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToBadGatewayProblem(
        this ControllerBase controller,
        string title,
        string detail,
        string code = ApiProblemCodes.ProviderGateway)
    {
        var problemDetails = ApiProblemFactory.CreateBadGatewayProblem(
            controller.HttpContext,
            title,
            detail,
            code);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToServiceUnavailableProblem(
        this ControllerBase controller,
        string title,
        string detail,
        string code = ApiProblemCodes.UnexpectedError)
    {
        var problemDetails = ApiProblemFactory.CreateServiceUnavailableProblem(
            controller.HttpContext,
            title,
            detail,
            code);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToStorageUploadProblem(
        this ControllerBase controller,
        BaseCommandResponse<StorageUploadSessionDto> response)
    {
        var statusCode = ResolveStorageUploadStatusCode(response.FailureCode);

        if (statusCode == StatusCodes.Status400BadRequest)
        {
            return controller.ToCommandValidationProblem(response, StorageUploadValidationProblem);
        }

        var problemDetails = ApiProblemFactory.CreateProblem(
            controller.HttpContext,
            statusCode,
            ResolveStorageUploadTitle(statusCode),
            ResolveStorageUploadType(statusCode, response.FailureCode),
            response.Message ?? StorageUploadValidationProblem.FallbackDetail,
            response.FailureCode ?? ApiProblemCodes.UnexpectedError);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    private static int ResolveStorageUploadStatusCode(string? failureCode)
        => failureCode switch
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

    private static string ResolveStorageUploadTitle(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status404NotFound => "Storage upload session not found",
            StatusCodes.Status409Conflict => "Storage upload session conflict",
            StatusCodes.Status413PayloadTooLarge => "Storage upload is too large",
            StatusCodes.Status422UnprocessableEntity => "Storage quota exceeded",
            StatusCodes.Status503ServiceUnavailable => "Storage provider unavailable",
            _ => StorageUploadValidationProblem.Title
        };

    private static string ResolveStorageUploadType(int statusCode, string? failureCode)
        => failureCode == FailureCodes.QuotaExceeded
            ? "/problems/quota_exceeded"
            : statusCode switch
            {
                StatusCodes.Status404NotFound => ApiProblemTypes.NotFound,
                StatusCodes.Status409Conflict => ApiProblemTypes.Conflict,
                StatusCodes.Status413PayloadTooLarge => ApiProblemTypes.PayloadTooLarge,
                StatusCodes.Status422UnprocessableEntity => ApiProblemTypes.UnprocessableEntity,
                StatusCodes.Status503ServiceUnavailable => ApiProblemTypes.ServiceUnavailable,
                _ => ApiProblemTypes.BadRequest
            };

    public static ActionResult ToEmailDispatchValidationProblem(
        this ControllerBase controller,
        string detail,
        IReadOnlyCollection<string>? errors)
    {
        var problemDetails = ApiProblemFactory.CreateValidationProblem(
            controller.HttpContext,
            EmailDispatchValidationProblem,
            (errors is { Count: > 0 } ? errors : [detail]).ToArray(),
            detail,
            EmailDispatchFailureCodes.ValidationFailed);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    public static ActionResult ToEmailDispatchProblem(
        this ControllerBase controller,
        BaseCommandResponse<Guid> response)
    {
        var statusCode = ResolveEmailDispatchStatusCode(response.FailureCode);

        if (statusCode == StatusCodes.Status400BadRequest)
        {
            return controller.ToCommandValidationProblem(response, EmailDispatchValidationProblem);
        }

        var problemDetails = ApiProblemFactory.CreateProblem(
            controller.HttpContext,
            statusCode,
            ResolveEmailDispatchTitle(statusCode),
            ResolveEmailDispatchType(statusCode),
            response.Message ?? EmailDispatchValidationProblem.FallbackDetail,
            response.FailureCode ?? ApiProblemCodes.UnexpectedError);

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    private static int ResolveEmailDispatchStatusCode(string? failureCode)
        => failureCode switch
        {
            EmailDispatchFailureCodes.NotFound => StatusCodes.Status404NotFound,
            EmailDispatchFailureCodes.InvalidTransition => StatusCodes.Status409Conflict,
            EmailDispatchFailureCodes.ConcurrentTransition => StatusCodes.Status409Conflict,
            EmailDispatchFailureCodes.Misconfigured => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest
        };

    private static string ResolveEmailDispatchTitle(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status404NotFound => "Email dispatch row not found",
            StatusCodes.Status409Conflict => "Email dispatch state transition conflict",
            StatusCodes.Status503ServiceUnavailable => "Email dispatch is misconfigured",
            _ => EmailDispatchValidationProblem.Title
        };

    private static string ResolveEmailDispatchType(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status404NotFound => ApiProblemTypes.NotFound,
            StatusCodes.Status409Conflict => ApiProblemTypes.Conflict,
            StatusCodes.Status503ServiceUnavailable => ApiProblemTypes.ServiceUnavailable,
            _ => ApiProblemTypes.BadRequest
        };

    public static ActionResult ToAiAssistantProblem(
        this ControllerBase controller,
        BaseCommandResponse<Guid> response)
    {
        var statusCode = ResolveAiAssistantStatusCode(response.FailureCode);
        var title = ResolveAiAssistantTitle(statusCode, response.FailureCode);
        var detail = ResolveAiAssistantDetail(response, statusCode);
        ProblemDetails problemDetails;

        if (statusCode == StatusCodes.Status400BadRequest)
        {
            var errors = response.Errors is { Count: > 0 }
                ? response.Errors.ToArray()
                : [response.Message ?? AiAssistantValidationProblem.FallbackDetail];
            var descriptor = new ApiValidationProblemDescriptor(
                AiAssistantValidationProblem.ErrorKey,
                title,
                AiAssistantValidationProblem.FallbackDetail);

            problemDetails = ApiProblemFactory.CreateValidationProblem(
                controller.HttpContext,
                descriptor,
                errors,
                detail,
                response.FailureCode ?? ApiProblemCodes.ValidationFailed);
        }
        else
        {
            problemDetails = ApiProblemFactory.CreateProblem(
                controller.HttpContext,
                statusCode,
                title,
                ResolveAiAssistantType(statusCode, response.FailureCode),
                detail,
                response.FailureCode ?? ApiProblemCodes.UnexpectedError);
        }

        if (response.QuotaExceeded is not null)
        {
            problemDetails.Extensions["quota"] = response.QuotaExceeded;
        }

        return ApiProblemFactory.ToProblemResult(problemDetails);
    }

    private static int ResolveAiAssistantStatusCode(string? failureCode)
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

    private static string ResolveAiAssistantDetail(BaseCommandResponse<Guid> response, int statusCode)
    {
        if ((statusCode == StatusCodes.Status400BadRequest || statusCode == StatusCodes.Status503ServiceUnavailable)
            && TryGetFirstAiAssistantError(response) is { } firstError)
        {
            return firstError;
        }

        return response.Message ?? AiAssistantValidationProblem.FallbackDetail;
    }

    private static string? TryGetFirstAiAssistantError(BaseCommandResponse<Guid> response)
    {
        if (response.Errors is null)
        {
            return null;
        }

        foreach (var error in response.Errors)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                return error.Trim();
            }
        }

        return null;
    }

    private static string ResolveAiAssistantTitle(int statusCode, string? failureCode)
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
            _ => AiAssistantValidationProblem.Title
        };

    private static string ResolveAiAssistantType(int statusCode, string? failureCode)
        => failureCode == FailureCodes.QuotaExceeded
            ? "/problems/quota_exceeded"
            : statusCode switch
            {
                StatusCodes.Status401Unauthorized => ApiProblemTypes.Unauthorized,
                StatusCodes.Status403Forbidden => ApiProblemTypes.Forbidden,
                StatusCodes.Status404NotFound => ApiProblemTypes.NotFound,
                StatusCodes.Status409Conflict => ApiProblemTypes.Conflict,
                StatusCodes.Status422UnprocessableEntity => ApiProblemTypes.UnprocessableEntity,
                StatusCodes.Status429TooManyRequests => ApiProblemTypes.TooManyRequests,
                StatusCodes.Status503ServiceUnavailable => ApiProblemTypes.ServiceUnavailable,
                _ => ApiProblemTypes.BadRequest
            };
}
