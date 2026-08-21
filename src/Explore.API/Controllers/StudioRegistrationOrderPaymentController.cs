// ABOUTME: Exposes bounded Studio payment status under exact event commercial authority.
// ABOUTME: The response shares the privacy-safe payment projection and offers no purchaser action links.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Hateoas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/events/{eventId:guid}/registration-orders")]
[ApiController]
public sealed class StudioRegistrationOrderPaymentController(IMediator mediator) : ControllerBase
{
    private static readonly Explore.API.ExceptionHandling.ApiNotFoundProblemDescriptor PaymentNotFoundProblem = new(
        "Registration payment not found", "Registration payment was not found.");

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [PrivateNoStore]
    [HttpGet("{orderId:guid}/payment/studio", Name = RouteNames.GetStudioRegistrationPayment)]
    [ProducesResponseType(typeof(HalResource<RegistrationPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<RegistrationPaymentDto>>> GetStatus(
        Guid eventId, Guid orderId, CancellationToken cancellationToken = default)
    {
        RegistrationPaymentDto? payment = await mediator.Send(new GetStudioRegistrationPaymentQuery(eventId, orderId), cancellationToken);
        if (payment is null)
        {
            return this.ToNotFoundProblem(PaymentNotFoundProblem);
        }

        var resource = new HalResource<RegistrationPaymentDto>(payment)
            .WithLink(LinkRelations.Self, HalLink.Create(Url.Link(RouteNames.GetStudioRegistrationPayment, new { eventId, orderId })!));
        return Ok(resource);
    }
}
