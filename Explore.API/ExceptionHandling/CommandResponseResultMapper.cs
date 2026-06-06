// ABOUTME: Maps Application command response failures to API-owned RFC 7807 results.
// ABOUTME: Keeps controllers thin while avoiding HTTP concerns inside Application handlers.

using Explore.Application.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.ExceptionHandling;

internal static class CommandResponseResultMapper
{
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

    public static ActionResult ToNotFoundProblem(
        this ControllerBase controller,
        ApiNotFoundProblemDescriptor descriptor)
    {
        var problemDetails = ApiProblemFactory.CreateNotFoundProblem(controller.HttpContext, descriptor);
        return ApiProblemFactory.ToProblemResult(problemDetails);
    }
}
