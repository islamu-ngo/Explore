// ABOUTME: REST API controller for organization member CRUD operations with role-based access control.
// ABOUTME: Manages user-role assignments within organizations via CQRS/MediatR.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Features.OrganizationMembers.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrganizationMemberController : ExploreControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationMemberController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{organizationId:guid}", Name = RouteNames.GetOrganizationMembersByOrganization)]
    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<OrganizationMemberDto>>> Get(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var members = await _mediator.Send(new GetOrganizationMembersRequest { OrganizationId = organizationId }, cancellationToken);
        return Ok(members);
    }

    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost(Name = RouteNames.AddOrganizationMember)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Post([FromBody] AddOrganizationMemberDto dto, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedProblem();
        }

        var command = new AddOrganizationMemberCommand { AddOrganizationMemberDto = dto, RequesterUserId = userId };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPut("role", Name = RouteNames.UpdateOrganizationMemberRole)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateRole([FromBody] UpdateOrganizationMemberRoleDto dto, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedProblem();
        }

        var command = new UpdateOrganizationMemberRoleCommand { UpdateOrganizationMemberRoleDto = dto, RequesterUserId = userId };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpGet("invitations", Name = RouteNames.GetMyOrganizationInvitations)]
    public async Task<ActionResult<List<OrganizationInvitationDto>>> GetMyInvitations(CancellationToken cancellationToken = default)
    {
        var email = UserContext.Email;
        if (string.IsNullOrEmpty(email))
        {
            return Problem(
                title: "Email claim not found",
                detail: "The authenticated principal does not include an email claim required to list invitations.",
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.2");
        }

        var response = await _mediator.Send(new GetMyInvitationsRequest { Email = email }, cancellationToken);
        return Ok(response);
    }

    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("invitations/{id:guid}/accept", Name = RouteNames.AcceptOrganizationInvitation)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> AcceptInvitation(Guid id, CancellationToken cancellationToken = default)
    {
        var userGuid = CurrentUserId;
        if (!userGuid.HasValue)
        {
            return UnauthorizedProblem();
        }

        var command = new AcceptInvitationCommand { InvitationId = id, UserId = userGuid.Value };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpPost("invitations/{id:guid}/decline", Name = RouteNames.DeclineOrganizationInvitation)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> DeclineInvitation(Guid id, CancellationToken cancellationToken = default)
    {
        var userGuid = CurrentUserId;
        if (!userGuid.HasValue)
        {
            return UnauthorizedProblem();
        }
        var command = new DeclineInvitationCommand { InvitationId = id, UserId = userGuid.Value };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [EndpointClassification(EndpointClass.Authenticated)]
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteOrganizationMember)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedProblem();
        }

        var command = new DeleteOrganizationMemberCommand { MemberId = id, RequesterUserId = userId };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    private ObjectResult UnauthorizedProblem()
    {
        return Problem(
            title: "User ID not found in token",
            detail: "The authenticated principal does not include a supported user identifier claim.",
            statusCode: StatusCodes.Status401Unauthorized,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.2");
    }
}
