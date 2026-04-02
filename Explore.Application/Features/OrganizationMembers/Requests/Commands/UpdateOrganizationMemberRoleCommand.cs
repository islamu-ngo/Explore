// ABOUTME: MediatR command for updating a member's organization role.
// ABOUTME: Carries the member ID and new role ID.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

[AuthorizeResource("organization_member", AuthorizationActions.Update)]
public class UpdateOrganizationMemberRoleCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateOrganizationMemberRoleDto UpdateOrganizationMemberRoleDto { get; set; }
    public required string RequesterUserId { get; set; }

    string? ISecureRequest.ResourceId => UpdateOrganizationMemberRoleDto.Id.ToString();
}
