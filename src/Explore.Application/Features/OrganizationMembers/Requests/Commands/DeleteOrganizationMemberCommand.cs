// ABOUTME: MediatR command for removing a member from an organization.
// ABOUTME: Carries the organization member ID.
using System;
using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Requests.Commands;

[AuthorizeResource(ResourceKinds.OrganizationMember, AuthorizationActions.Delete)]
public class DeleteOrganizationMemberCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid MemberId { get; set; }
    public required string RequesterUserId { get; set; }

    string? ISecureRequest.ResourceId => MemberId.ToString();
}
