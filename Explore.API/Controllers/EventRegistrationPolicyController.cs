// ABOUTME: API controller for event registration policy lookup table (read-only enumeration).
// ABOUTME: Provides registration policy options (Open, ApprovalRequired, InvitationOnly) for events.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.Application.DTOs.EventRegistrationPolicy;
using Explore.Application.Features.EventRegistrationPolicies.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class EventRegistrationPolicyController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventRegistrationPolicyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/eventregistrationpolicy
    [HttpGet]
    [EndpointSummary("Get all Event Registration Policies")]
    [EndpointDescription("Retrieve a list of all registration policies (Open, ApprovalRequired, InvitationOnly)")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventRegistrationPolicyListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<EventRegistrationPolicyListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var policies = await _mediator.Send(new GetEventRegistrationPolicyListRequest(), cancellationToken);
        return Ok(policies);
    }
}
