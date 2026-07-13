// ABOUTME: Authenticated webhook endpoint operations that change persisted delivery-control state.
// ABOUTME: Exposes manual resume while CQRS authorization and endpoint state enforce eligibility.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/webhooks/endpoints")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType)]
public sealed class WebhookEndpointOperationsController(
    IMediator mediator,
    ITenantContext tenantContext) : ExploreControllerBase
{
    [HttpPost("{endpointId:guid}/resume", Name = RouteNames.ResumeWebhookEndpoint)]
    [EndpointSummary("Resume webhook endpoint")]
    [EndpointDescription("Resumes a tenant-scoped Local webhook endpoint after its sustained-failure circuit auto-paused delivery.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequestTimeout(RequestTimeoutExtensions.DefaultPolicy)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Resume(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new ResumeWebhookEndpointCommand
            {
                TenantId = tenantContext.TenantId,
                EndpointId = endpointId,
                ActorUserId = RequiredUserId
            },
            cancellationToken);

        if (response.Success)
        {
            return Ok(response);
        }

        return response.FailureCode switch
        {
            "webhook_endpoint_not_found" => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Webhook endpoint not found",
                detail: response.Message),
            "webhook_endpoint_not_auto_paused" or "webhook_endpoint_resume_conflict" => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Webhook endpoint cannot be resumed",
                detail: response.Message),
            _ => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["webhookEndpoint"] = response.Errors.ToArray()
            }))
        };
    }
}
