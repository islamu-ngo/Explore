using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

[AuthorizeResource("organization_member", PermissionAction.Update)]
public class UpdateOrganizationMemberRoleCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateOrganizationMemberRoleDto UpdateOrganizationMemberRoleDto { get; set; }
    public required string RequesterUserId { get; set; }

    string? ISecureRequest.ResourceId => UpdateOrganizationMemberRoleDto.Id.ToString();
}
