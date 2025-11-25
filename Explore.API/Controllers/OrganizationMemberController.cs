using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Features.OrganizationMembers.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrganizationMemberController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationMemberController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{organizationId}")]
        public async Task<ActionResult<List<OrganizationMemberDto>>> Get(Guid organizationId)
        {
            var members = await _mediator.Send(new GetOrganizationMembersRequest { OrganizationId = organizationId });
            return Ok(members);
        }

        [HttpPost]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Post([FromBody] AddOrganizationMemberDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new AddOrganizationMemberCommand { AddOrganizationMemberDto = dto, RequesterUserId = userId };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPut("role")]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateRole([FromBody] UpdateOrganizationMemberRoleDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new UpdateOrganizationMemberRoleCommand { UpdateOrganizationMemberRoleDto = dto, RequesterUserId = userId };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("invitations")]
        public async Task<ActionResult<List<OrganizationInvitationDto>>> GetMyInvitations()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            // If email claim is missing (e.g. using NameIdentifier only), we might need to fetch user details.
            // Assuming Email claim is present.
            if (string.IsNullOrEmpty(email))
            {
                // Fallback: try to get email from user service or similar if needed.
                // For now, let's assume it's in the claims.
                // If using IdentityServer/Keycloak, ensure "email" scope is requested and mapped.
                return BadRequest("Email claim not found.");
            }

            var response = await _mediator.Send(new GetMyInvitationsRequest { Email = email });
            return Ok(response);
        }

        [HttpPost("invitations/{id}/accept")]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> AcceptInvitation(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return BadRequest("Invalid User ID.");
            }

            var command = new AcceptInvitationCommand { InvitationId = id, UserId = userGuid };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("invitations/{id}/decline")]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> DeclineInvitation(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
             if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return BadRequest("Invalid User ID.");
            }
            var command = new DeclineInvitationCommand { InvitationId = id, UserId = userGuid };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseCommandResponse<Guid>>> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new DeleteOrganizationMemberCommand { MemberId = id, RequesterUserId = userId };
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
