// ABOUTME: Describes resource-specific API ProblemDetails text for command response mapping.
// ABOUTME: Replaces repeated controller string tuples with named, reusable error contracts.

namespace Explore.API.ExceptionHandling;

internal sealed record ApiValidationProblemDescriptor(
    string ErrorKey,
    string Title,
    string FallbackDetail);

internal sealed record ApiNotFoundProblemDescriptor(
    string Title,
    string Detail,
    string Code = ApiProblemCodes.ResourceNotFound);
