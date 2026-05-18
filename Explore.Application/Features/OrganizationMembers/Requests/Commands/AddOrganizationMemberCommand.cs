// ABOUTME: MediatR command for adding a member to an organization.
// ABOUTME: Carries the target organization ID and user/actor ID.
using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.Create)]
public class AddOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required AddOrganizationMemberDto AddOrganizationMemberDto { get; set; }
    public required string RequesterUserId { get; set; } // To check permissions

    string? ISecureRequest.ResourceId => AddOrganizationMemberDto.OrganizationId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        new Dictionary<string, object>
        {
            ["organizationId"] = AddOrganizationMemberDto.OrganizationId.ToString()
        };
}
