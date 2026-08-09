// ABOUTME: Verifies the generic command-response mapper's success and typed failure HTTP results.
// ABOUTME: Pins response body identity while covering known, unknown, blank, and missing failure codes.

using Explore.API.ExceptionHandling;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Event.Api.IntegrationTests.ExceptionHandling;

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
    public async Task MapCommandResponse_WhenNotFound_ReturnsNotFoundWithOriginalResponse()
    {
        var response = Failure(FailureCodes.NotFound);

        var result = CreateController().MapCommandResponse(response);

        await Assert.That(result).IsTypeOf<NotFoundObjectResult>();
        await Assert.That(((NotFoundObjectResult)result).Value).IsSameReferenceAs(response);
    }

    [Test]
    public async Task MapCommandResponse_WhenAdminRequired_ReturnsForbiddenWithOriginalResponse()
    {
        var response = Failure(FailureCodes.AdminRequired);

        var result = CreateController().MapCommandResponse(response);

        await Assert.That(result).IsTypeOf<ObjectResult>();
        await Assert.That(((ObjectResult)result).StatusCode).IsEqualTo(StatusCodes.Status403Forbidden);
        await Assert.That(((ObjectResult)result).Value).IsSameReferenceAs(response);
    }

    [Test]
    public async Task MapCommandResponse_WhenConcurrencyConflict_ReturnsConflictWithOriginalResponse()
    {
        var response = Failure(FailureCodes.ConcurrencyConflict);

        var result = CreateController().MapCommandResponse(response);

        await Assert.That(result).IsTypeOf<ConflictObjectResult>();
        await Assert.That(((ConflictObjectResult)result).Value).IsSameReferenceAs(response);
    }

    [Test]
    [Arguments("unexpected_failure")]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task MapCommandResponse_WhenFailureCodeIsNotMapped_ReturnsBadRequestWithOriginalResponse(
        string? failureCode)
    {
        var response = Failure(failureCode);

        var result = CreateController().MapCommandResponse(response);

        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
        await Assert.That(((BadRequestObjectResult)result).Value).IsSameReferenceAs(response);
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
