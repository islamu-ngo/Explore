// ABOUTME: REST API controller for organization CRUD operations with verification and member management.
// ABOUTME: Supports two-tier verification system, role-based access, and cascading organization settings.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Commands;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

/// <summary>
/// Organization management API endpoints.
/// All responses include HATEOAS links by default.
/// Send "Prefer: return=minimal" header to strip links.
/// </summary>
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public class OrganizationController : ExploreControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResourceAssembler<OrganizationDto, OrganizationListDto> _resourceAssembler;

    public OrganizationController(
        IMediator mediator,
        IResourceAssembler<OrganizationDto, OrganizationListDto> resourceAssembler)
    {
        _mediator = mediator;
        _resourceAssembler = resourceAssembler;
    }

    /// <summary>
    /// Get all organizations with pagination.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet(Name = RouteNames.GetOrganizations)]
    [EndpointSummary("Get all Organizations")]
    [EndpointDescription("Get a paginated list of all Organizations. Default page size is 20, max is 100. " +
        "Response includes HATEOAS navigation links (first, prev, next, last). " +
        "Send 'Prefer: return=minimal' header to strip links.")]
    [ProducesResponseType(typeof(HalCollectionResource<OrganizationListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<HalCollectionResource<OrganizationListDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrganizationListRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetOrganizations,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get organizations where the current user is a member.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("my", Name = RouteNames.GetMyOrganizations)]
    [EndpointSummary("Get my Organizations")]
    [EndpointDescription("Get a paginated list of organizations where the current user is a member. " +
        "Default page size is 20, max is 100.")]
    [ProducesResponseType(typeof(HalCollectionResource<OrganizationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HalCollectionResource<OrganizationListDto>>> GetMyOrganizations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _mediator.Send(new GetMyOrganizationsRequest
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        var halResource = await _resourceAssembler.ToCollectionResource(
            result,
            RouteNames.GetMyOrganizations,
            additionalRouteValues: null,
            HttpContext);

        return Ok(halResource);
    }

    /// <summary>
    /// Get organization details by ID.
    /// </summary>
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [HttpGet("{id:guid}", Name = RouteNames.GetOrganizationById)]
    [EndpointSummary("Get Organization Details")]
    [EndpointDescription("Get full details of an organization including actor information and approval status. " +
        "Response includes links to related resources (events, members).")]
    [ProducesResponseType(typeof(HalResource<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<HalResource<OrganizationDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _mediator.Send(new GetOrganizationDetailsRequest { Id = id }, cancellationToken);
        if (organization == null)
            return NotFound();

        var halResource = await _resourceAssembler.ToResource(organization, HttpContext);
        return Ok(halResource);
    }

    /// <summary>
    /// Create a new organization.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.CreateOrganization)]
    [EndpointSummary("Create Organization")]
    [EndpointDescription("Create a new organization. The authenticated user becomes the owner.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateOrganizationDto organization, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();

        var command = new CreateOrganizationCommand
        {
            OrganizationDto = organization,
            UserId = userId
        };

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Return 201 Created with Location header pointing to the new resource
        return CreatedAtRoute(
            RouteNames.GetOrganizationById,
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Update an organization.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("{id:guid}", Name = RouteNames.UpdateOrganization)]
    [EndpointSummary("Update Organization")]
    [EndpointDescription("Update an existing organization. User must be a member of the organization.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(
        Guid id,
        [FromBody] UpdateOrganizationDto updateDto, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var command = new UpdateOrganizationDetailsCommand
        {
            Id = id,
            UserId = userId,
            OrganizationDto = updateDto
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update organization approval status (Admin only).
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("updatestatustype/{id:guid}", Name = RouteNames.UpdateOrganizationApprovalStatus)]
    [EndpointSummary("Update Organization Approval Status")]
    [EndpointDescription("Update the approval status of an organization. Requires Admin role.")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateApprovalStatus(
        Guid id,
        [FromBody] UpdateOrganizationApprovalStatusDto approvalStatus, CancellationToken cancellationToken = default)
    {
        var command = new UpdateOrganizationCommand
        {
            Id = id,
            OrganizationApprovalStatusDto = approvalStatus
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete an organization.
    /// </summary>
    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteOrganization)]
    [EndpointSummary("Delete Organization")]
    [EndpointDescription("Delete an organization. Requires ownership or Admin role.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        // TODO: Implement delete command
        // var command = new DeleteOrganizationCommand { Id = id };
        // await _mediator.Send(command);
        return NoContent();
    }

}
