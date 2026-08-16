// ABOUTME: Verifies the generic command-response mapper's success body and RFC 7807 failure mapping.
// ABOUTME: Pins the status and problem shape for each shared failure code, including unmapped ones.

using Explore.API.ExceptionHandling;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Event.Api.IntegrationTests.ExceptionHandling;

/// <summary>
/// Success still returns the command response as the body; failures return ProblemDetails.
/// <para>
/// These assertions changed deliberately. The mapper used to serialize the raw
/// <see cref="BaseCommandResponse{TKey}"/> on failure, which meant a caller had to know *which* endpoint it
/// hit before it could parse an error — some endpoints answered with ProblemDetails and some with a command
/// object. Every failure path in the API now answers in the one shape <c>[ApiController]</c> already promises.
/// </para>
/// </summary>
public sealed class CommandResponseResultMapperTests
{
    [Test]
    public async Task MapCommandResponse_WhenSuccessful_ReturnsOkWithOriginalResponse()
    {
        var controller = CreateController();
        var response = new BaseCommandResponse<string>
        {
            Id = "created-id",
            Success = true,
            Message = "Created"
        };

        var result = controller.MapCommandResponse(response);

        await Assert.That(result).IsTypeOf<OkObjectResult>();
        await Assert.That(((OkObjectResult)result).Value).IsSameReferenceAs(response);
    }

    [Test]
    public async Task MapCommandResponse_WhenNotFound_ReturnsNotFoundProblemDetails()
    {
        var result = CreateController().MapCommandResponse(Failure(FailureCodes.NotFound));

        await AssertProblem(result, StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task MapCommandResponse_WhenAdminRequired_ReturnsForbiddenProblemDetails()
    {
        var result = CreateController().MapCommandResponse(Failure(FailureCodes.AdminRequired));

        await AssertProblem(result, StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task MapCommandResponse_WhenAuthenticationRequired_ReturnsUnauthorizedProblemDetails()
    {
        var result = CreateController().MapCommandResponse(Failure(FailureCodes.AuthenticationRequired));

        await AssertProblem(result, StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task MapCommandResponse_WhenConcurrencyConflict_ReturnsConflictProblemDetails()
    {
        var result = CreateController().MapCommandResponse(Failure(FailureCodes.ConcurrencyConflict));

        await AssertProblem(result, StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// An unrecognized, blank, or absent failure code falls through to a validation problem rather than being
    /// collapsed into an untyped 400 body, so a client can still read why the command failed.
    /// </summary>
    [Test]
    [Arguments("unexpected_failure")]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task MapCommandResponse_WhenFailureCodeIsNotMapped_ReturnsValidationProblemDetails(
        string? failureCode)
    {
        var result = CreateController().MapCommandResponse(Failure(failureCode));

        await AssertProblem(result, StatusCodes.Status400BadRequest);
    }

    private static async Task AssertProblem(ActionResult result, int expectedStatusCode)
    {
        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(expectedStatusCode);

        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Status).IsEqualTo(expectedStatusCode);
    }

    private static TestController CreateController() => new()
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        }
    };

    private static BaseCommandResponse<Guid> Failure(string? failureCode) => new()
    {
        Success = false,
        Message = "Command failed",
        FailureCode = failureCode
    };

    private sealed class TestController : ControllerBase;
}
