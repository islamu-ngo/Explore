// ABOUTME: API controller for approval status lookup table (read-only enumeration).
// ABOUTME: Provides approval status values for event and organization verification workflows.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.StatusType;
using Explore.Application.Features.StatusTypes.Requests.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class ApprovalStatusController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApprovalStatusController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/<StatusTypeController>
    [HttpGet(Name = RouteNames.GetApprovalStatusOptions)]
    [EndpointSummary("Get all Status Types")]
    [EndpointDescription("Get A List of all the Status Type Options")]
    [AllowAnonymous] //allow anonymous in case i want in beginning to let unverified org publish programs en ban them if necessery, then when there is lot's then change this business logic
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<StatusTypeListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var statusTypes = await _mediator.Send(new GetStatusTypeListRequest { FullName = string.Empty }, cancellationToken);
        return Ok(statusTypes);
    }

    // GET api/<StatusTypeController>/5
    [HttpGet("{id}", Name = RouteNames.GetApprovalStatusOptionById)]
    [OutputCache(PolicyName = "DetailData")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<StatusTypeController>
    [HttpPost(Name = RouteNames.CreateApprovalStatusOption)]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<StatusTypeController>/5
    [HttpPut("{id}", Name = RouteNames.UpdateApprovalStatusOption)]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<StatusTypeController>/5
    [HttpDelete("{id}", Name = RouteNames.DeleteApprovalStatusOption)]
    public void Delete(int id)
    {
    }
}
